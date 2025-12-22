using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Stan gry w środowisku RL
/// </summary>
public class CrosswordRLState
{
    /// <summary>
    /// Aktualna siatka krzyżówki
    /// </summary>
    public CrosswordGrid Grid { get; set; }
    
    /// <summary>
    /// Lista umieszczonych słów
    /// </summary>
    public List<PlacedWordInfo> PlacedWords { get; set; }
    
    /// <summary>
    /// Lista pozostałych słów do umieszczenia
    /// </summary>
    public List<string> RemainingWords { get; set; }
    
    /// <summary>
    /// Liczba kroków wykonanych w grze
    /// </summary>
    public int StepCount { get; set; }
    
    public CrosswordRLState(CrosswordGrid grid, List<string> remainingWords)
    {
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        RemainingWords = remainingWords ?? throw new ArgumentNullException(nameof(remainingWords));
        PlacedWords = new List<PlacedWordInfo>();
        StepCount = 0;
    }
    
    /// <summary>
    /// Tworzy kopię stanu
    /// </summary>
    public CrosswordRLState Clone()
    {
        // Tworzenie kopii siatki
        var clonedGrid = new CrosswordGrid(Grid.Rows, Grid.Columns);
        foreach (var kvp in Grid.Cells)
        {
            var cell = kvp.Value;
            clonedGrid.SetCell(cell.Row, cell.Column, cell.Type, cell.Letter);
        }
        
        // Kopia list
        var clonedState = new CrosswordRLState(clonedGrid, new List<string>(RemainingWords))
        {
            StepCount = StepCount
        };
        
        clonedState.PlacedWords = new List<PlacedWordInfo>(PlacedWords);
        
        return clonedState;
    }
    
    /// <summary>
    /// Sprawdza czy stan jest terminalny (wszystkie słowa umieszczone)
    /// </summary>
    public bool IsTerminal()
    {
        return RemainingWords.Count == 0;
    }
}

/// <summary>
/// Informacja o umieszczonym słowie
/// </summary>
public class PlacedWordInfo
{
    public string Word { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public WordDirection Direction { get; set; }
    public int IntersectionCount { get; set; }
    
    public PlacedWordInfo(string word, int row, int column, WordDirection direction, int intersectionCount = 0)
    {
        Word = word ?? throw new ArgumentNullException(nameof(word));
        Row = row;
        Column = column;
        Direction = direction;
        IntersectionCount = intersectionCount;
    }
}
