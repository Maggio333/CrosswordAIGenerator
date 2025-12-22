using System.Text;
using System.Text.RegularExpressions;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;

namespace CrosswordAIGenerator.Core.Application.Services.RL;

/// <summary>
/// Adapter do konwersji między stanami/akcjami a formatem modelu
/// </summary>
public class CrosswordRLAdapter : ICrosswordRLAdapter
{
    private readonly Domain.Services.ICrossGridGenerator _crossGridGenerator;
    private readonly ICursorLogger? _logger;
    
    public CrosswordRLAdapter(
        Domain.Services.ICrossGridGenerator crossGridGenerator,
        ICursorLogger? logger = null)
    {
        _crossGridGenerator = crossGridGenerator ?? throw new ArgumentNullException(nameof(crossGridGenerator));
        _logger = logger;
    }
    
    public string StateToPrompt(CrosswordRLState state)
    {
        var sb = new StringBuilder();
        
        // Sekcja # GRID - zgodnie z formatem CrossGrid
        var crossGrid = _crossGridGenerator.GenerateCrossGrid(state.Grid);
        sb.AppendLine(crossGrid);
        sb.AppendLine();
        
        // Sekcja # DOSTĘPNE SŁOWA - lista pozostałych słów z możliwymi kierunkami
        if (state.RemainingWords.Count > 0)
        {
            sb.AppendLine("# DOSTĘPNE SŁOWA");
            foreach (var word in state.RemainingWords)
            {
                // Dla każdego słowa można umieścić w obu kierunkach (jeśli się zmieści)
                sb.AppendLine($"- {word} (Across|Down)");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("# DOSTĘPNE SŁOWA");
            sb.AppendLine("(wszystkie słowa zostały umieszczone)");
            sb.AppendLine();
        }
        
        // Sekcja # ZADANIE - instrukcja dla modelu
        sb.AppendLine("# ZADANIE");
        sb.AppendLine("Wykonaj dokładnie JEDEN ruch.");
        sb.AppendLine("Zwróć JSON:");
        sb.AppendLine("{\"word\": \"<SŁOWO>\", \"row\": <int>, \"col\": <int>, \"direction\": \"Across|Down\"}");
        
        return sb.ToString();
    }
    
    public CrosswordRLAction? ParseAction(string modelResponse)
    {
        if (string.IsNullOrWhiteSpace(modelResponse))
        {
            return null;
        }
        
        // Próbuj parsować różne formaty odpowiedzi
        // Format 1: "WORD row col DIRECTION"
        var pattern1 = @"(\w+)\s+(\d+)\s+(\d+)\s+(Across|Down)";
        var match1 = Regex.Match(modelResponse, pattern1, RegexOptions.IgnoreCase);
        
        if (match1.Success)
        {
            var word = match1.Groups[1].Value;
            var row = int.Parse(match1.Groups[2].Value);
            var col = int.Parse(match1.Groups[3].Value);
            var directionStr = match1.Groups[4].Value;
            
            var direction = directionStr.Equals("Across", StringComparison.OrdinalIgnoreCase)
                ? WordDirection.Across
                : WordDirection.Down;
            
            return new CrosswordRLAction(word, row, col, direction);
        }
        
        // Format 2: "WORD at (row, col) DIRECTION"
        var pattern2 = @"(\w+)\s+at\s+\((\d+),\s*(\d+)\)\s+(Across|Down)";
        var match2 = Regex.Match(modelResponse, pattern2, RegexOptions.IgnoreCase);
        
        if (match2.Success)
        {
            var word = match2.Groups[1].Value;
            var row = int.Parse(match2.Groups[2].Value);
            var col = int.Parse(match2.Groups[3].Value);
            var directionStr = match2.Groups[4].Value;
            
            var direction = directionStr.Equals("Across", StringComparison.OrdinalIgnoreCase)
                ? WordDirection.Across
                : WordDirection.Down;
            
            return new CrosswordRLAction(word, row, col, direction);
        }
        
        // Format 3: JSON-like format
        var pattern3 = @"""word""\s*:\s*""(\w+)""\s*,\s*""row""\s*:\s*(\d+)\s*,\s*""col""\s*:\s*(\d+)\s*,\s*""direction""\s*:\s*""(Across|Down)""";
        var match3 = Regex.Match(modelResponse, pattern3, RegexOptions.IgnoreCase);
        
        if (match3.Success)
        {
            var word = match3.Groups[1].Value;
            var row = int.Parse(match3.Groups[2].Value);
            var col = int.Parse(match3.Groups[3].Value);
            var directionStr = match3.Groups[4].Value;
            
            var direction = directionStr.Equals("Across", StringComparison.OrdinalIgnoreCase)
                ? WordDirection.Across
                : WordDirection.Down;
            
            return new CrosswordRLAction(word, row, col, direction);
        }
        
        _logger?.Warning($"CrosswordRLAdapter: Could not parse action from response: {modelResponse}");
        
        return null;
    }
    
    public bool ValidateAction(CrosswordRLAction action, CrosswordRLState state)
    {
        if (action == null || state == null)
        {
            return false;
        }
        
        // Sprawdź czy słowo jest w liście pozostałych
        if (!state.RemainingWords.Contains(action.Word, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        
        // Sprawdź czy pozycja jest w granicach
        if (!state.Grid.IsValidPosition(action.Row, action.Column))
        {
            return false;
        }
        
        // Sprawdź czy słowo zmieści się w siatce
        int endRow = action.Direction == WordDirection.Down 
            ? action.Row + action.Word.Length - 1 
            : action.Row;
        int endCol = action.Direction == WordDirection.Across 
            ? action.Column + action.Word.Length - 1 
            : action.Column;
        
        if (endRow >= state.Grid.Rows || endCol >= state.Grid.Columns)
        {
            return false;
        }
        
        // Sprawdź czy wszystkie komórki są puste lub mają zgodne litery
        for (int i = 0; i < action.Word.Length; i++)
        {
            int row = action.Direction == WordDirection.Down ? action.Row + i : action.Row;
            int col = action.Direction == WordDirection.Across ? action.Column + i : action.Column;
            
            var cell = state.Grid.GetCell(row, col);
            
            if (cell.IsWall)
            {
                return false; // Nie można umieścić na ścianie
            }
            
            if (cell.HasLetter)
            {
                // Jeśli komórka ma literę, musi być zgodna
                if (char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(action.Word[i]))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
}
