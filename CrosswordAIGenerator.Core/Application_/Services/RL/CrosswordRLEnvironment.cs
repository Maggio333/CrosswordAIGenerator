using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;

namespace CrosswordAIGenerator.Core.Application.Services.RL;

/// <summary>
/// Środowisko RL dla krzyżówek
/// </summary>
public class CrosswordRLEnvironment : ICrosswordRLEnvironment
{
    private readonly ICrosswordRLRewardCalculator _rewardCalculator;
    private readonly ICursorLogger? _logger;
    
    public CrosswordRLEnvironment(
        ICrosswordRLRewardCalculator rewardCalculator,
        ICursorLogger? logger = null)
    {
        _rewardCalculator = rewardCalculator ?? throw new ArgumentNullException(nameof(rewardCalculator));
        _logger = logger;
    }
    
    public CrosswordRLState GetInitialState(int rows, int columns, List<string> targetWords)
    {
        if (targetWords == null || targetWords.Count == 0)
        {
            throw new ArgumentException("Target words cannot be empty", nameof(targetWords));
        }
        
        var grid = new CrosswordGrid(rows, columns);
        var state = new CrosswordRLState(grid, new List<string>(targetWords));
        
        _logger?.Debug($"CrosswordRLEnvironment: Created initial state with {targetWords.Count} words, grid size {rows}x{columns}");
        
        return state;
    }
    
    public List<CrosswordRLAction> GetValidActions(CrosswordRLState state)
    {
        var validActions = new List<CrosswordRLAction>();
        
        // Dla każdego pozostałego słowa, spróbuj znaleźć wszystkie możliwe pozycje
        foreach (var word in state.RemainingWords)
        {
            // Sprawdź wszystkie możliwe pozycje i kierunki
            for (int row = 0; row < state.Grid.Rows; row++)
            {
                for (int col = 0; col < state.Grid.Columns; col++)
                {
                    // Sprawdź poziomo (Across)
                    if (CanPlaceWord(state, word, row, col, WordDirection.Across))
                    {
                        validActions.Add(new CrosswordRLAction(word, row, col, WordDirection.Across));
                    }
                    
                    // Sprawdź pionowo (Down)
                    if (CanPlaceWord(state, word, row, col, WordDirection.Down))
                    {
                        validActions.Add(new CrosswordRLAction(word, row, col, WordDirection.Down));
                    }
                }
            }
        }
        
        _logger?.Debug($"CrosswordRLEnvironment: Found {validActions.Count} valid actions for state with {state.RemainingWords.Count} remaining words");
        
        return validActions;
    }
    
    public (CrosswordRLState nextState, double reward, bool isTerminal) Step(
        CrosswordRLState state,
        CrosswordRLAction action)
    {
        // Utwórz kopię stanu
        var nextState = state.Clone();
        
        // Sprawdź czy akcja jest poprawna
        if (!CanPlaceWord(state, action.Word, action.Row, action.Column, action.Direction))
        {
            // Niepoprawna akcja - zwróć stan bez zmian z karą
            double penaltyReward = _rewardCalculator.GetPenalty();
            _logger?.Warning($"CrosswordRLEnvironment: Invalid action {action}, applying penalty {penaltyReward}");
            
            return (nextState, penaltyReward, false);
        }
        
        // Umieść słowo w siatce
        PlaceWord(nextState, action);
        
        // Usuń słowo z listy pozostałych
        nextState.RemainingWords.RemoveAll(w => w.Equals(action.Word, StringComparison.OrdinalIgnoreCase));
        
        // Dodaj do listy umieszczonych słów
        int intersectionCount = CountIntersections(state, action);
        nextState.PlacedWords.Add(new PlacedWordInfo(
            action.Word,
            action.Row,
            action.Column,
            action.Direction,
            intersectionCount));
        
        nextState.StepCount++;
        
        // Sprawdź czy stan jest terminalny
        bool isTerminal = nextState.IsTerminal();
        
        // Oblicz nagrodę
        var rewardDetails = _rewardCalculator.CalculateReward(state, nextState, action, isTerminal);
        double calculatedReward = rewardDetails.TotalReward;
        
        _logger?.Debug($"CrosswordRLEnvironment: Step completed. Action: {action}, Reward: {calculatedReward:F2}, Terminal: {isTerminal}, Remaining words: {nextState.RemainingWords.Count}");
        
        return (nextState, calculatedReward, isTerminal);
    }
    
    public bool IsTerminal(CrosswordRLState state)
    {
        return state.IsTerminal();
    }
    
    public double CalculateReward(CrosswordRLState previousState, CrosswordRLState currentState, CrosswordRLAction action)
    {
        bool isTerminal = currentState.IsTerminal();
        var rewardDetails = _rewardCalculator.CalculateReward(previousState, currentState, action, isTerminal);
        return rewardDetails.TotalReward;
    }
    
    /// <summary>
    /// Sprawdza czy słowo może być umieszczone na danej pozycji (zgodnie z regułami krzyżówki)
    /// </summary>
    private bool CanPlaceWord(CrosswordRLState state, string word, int row, int col, WordDirection direction)
    {
        // Sprawdź czy słowo jest w liście pozostałych
        if (!state.RemainingWords.Any(w => w.Equals(word, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        
        // Sprawdź czy pozycja jest w granicach
        if (!state.Grid.IsValidPosition(row, col))
        {
            return false;
        }
        
        // Sprawdź czy słowo zmieści się w siatce
        int endRow = direction == WordDirection.Down ? row + word.Length - 1 : row;
        int endCol = direction == WordDirection.Across ? col + word.Length - 1 : col;
        
        if (endRow >= state.Grid.Rows || endCol >= state.Grid.Columns)
        {
            return false;
        }
        
        // WAŻNE: Wymagaj przecięcia dla wszystkich słów oprócz pierwszego
        if (state.PlacedWords.Count > 0)
        {
            bool hasIntersection = false;
            for (int i = 0; i < word.Length; i++)
            {
                int checkRow = direction == WordDirection.Down ? row + i : row;
                int checkCol = direction == WordDirection.Across ? col + i : col;
                var cell = state.Grid.GetCell(checkRow, checkCol);
                
                if (cell.HasLetter && char.ToUpperInvariant(cell.Letter!.Value) == char.ToUpperInvariant(word[i]))
                {
                    // Sprawdź czy to jest przecięcie z istniejącym słowem
                    foreach (var placedWord in state.PlacedWords)
                    {
                        var wordPositions = GetWordPositions(placedWord.Row, placedWord.Column, placedWord.Word.Length, 
                            placedWord.Direction == WordDirection.Across);
                        if (wordPositions.Contains((checkRow, checkCol)))
                        {
                            hasIntersection = true;
                            break;
                        }
                    }
                    if (hasIntersection) break;
                }
            }
            
            if (!hasIntersection)
            {
                return false; // Słowo musi mieć przecięcie z istniejącym słowem
            }
        }
        
        // Sprawdź reguły umieszczania (jak w IsValidPlacement)
        if (direction == WordDirection.Across)
        {
            // Sprawdź czy przed słowem jest pusta kratka lub ściana (lub granica)
            if (col > 0)
            {
                var beforeCell = state.Grid.GetCell(row, col - 1);
                if (beforeCell.HasLetter && !beforeCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź czy po słowie jest pusta kratka lub ściana (lub granica)
            if (col + word.Length < state.Grid.Columns)
            {
                var afterCell = state.Grid.GetCell(row, col + word.Length);
                if (afterCell.HasLetter && !afterCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź każdą komórkę słowa
            for (int i = 0; i < word.Length; i++)
            {
                var cell = state.Grid.GetCell(row, col + i);
                
                if (cell.IsWall)
                    return false;
                
                if (cell.HasLetter && char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                    return false;
                
                // Sprawdź czy nie ma liter bezpośrednio obok (prostopadle) - poza przecięciem
                // Górna kratka
                if (row > 0)
                {
                    var topCell = state.Grid.GetCell(row - 1, col + i);
                    if (topCell.HasLetter && !topCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
                
                // Dolna kratka
                if (row < state.Grid.Rows - 1)
                {
                    var bottomCell = state.Grid.GetCell(row + 1, col + i);
                    if (bottomCell.HasLetter && !bottomCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
            }
        }
        else // Down
        {
            // Sprawdź czy przed słowem jest pusta kratka lub ściana (lub granica)
            if (row > 0)
            {
                var beforeCell = state.Grid.GetCell(row - 1, col);
                if (beforeCell.HasLetter && !beforeCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź czy po słowie jest pusta kratka lub ściana (lub granica)
            if (row + word.Length < state.Grid.Rows)
            {
                var afterCell = state.Grid.GetCell(row + word.Length, col);
                if (afterCell.HasLetter && !afterCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź każdą komórkę słowa
            for (int i = 0; i < word.Length; i++)
            {
                var cell = state.Grid.GetCell(row + i, col);
                
                if (cell.IsWall)
                    return false;
                
                if (cell.HasLetter && char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                    return false;
                
                // Sprawdź czy nie ma liter bezpośrednio obok (prostopadle) - poza przecięciem
                // Lewa kratka
                if (col > 0)
                {
                    var leftCell = state.Grid.GetCell(row + i, col - 1);
                    if (leftCell.HasLetter && !leftCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
                
                // Prawa kratka
                if (col < state.Grid.Columns - 1)
                {
                    var rightCell = state.Grid.GetCell(row + i, col + 1);
                    if (rightCell.HasLetter && !rightCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(word[i]))
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
            }
        }
        
        // Sprawdź odstępy (HasProperSpacing) - słowa w tym samym kierunku nie mogą być obok siebie
        if (state.PlacedWords.Count > 0)
        {
            var newWordPositions = GetWordPositions(row, col, word.Length, direction == WordDirection.Across).ToHashSet();
            
            foreach (var placedWord in state.PlacedWords)
            {
                // Jeśli słowa są w tym samym kierunku, sprawdź czy nie są obok siebie
                if (placedWord.Direction == direction)
                {
                    var placedPositions = GetWordPositions(placedWord.Row, placedWord.Column, placedWord.Word.Length, 
                        placedWord.Direction == WordDirection.Across).ToHashSet();
                    
                    // Sprawdź czy słowa się przecinają (to jest OK)
                    if (newWordPositions.Intersect(placedPositions).Any())
                        continue; // Przecięcie jest OK
                    
                    // Sprawdź czy słowa są bezpośrednio obok siebie
                    if (direction == WordDirection.Across && placedWord.Direction == WordDirection.Across && row == placedWord.Row)
                    {
                        int newStart = col;
                        int newEnd = col + word.Length - 1;
                        int placedStart = placedWord.Column;
                        int placedEnd = placedWord.Column + placedWord.Word.Length - 1;
                        
                        // Sprawdź czy są bezpośrednio obok (bez pustej kratki między)
                        if (Math.Abs(newStart - placedEnd) == 1 || Math.Abs(placedStart - newEnd) == 1)
                        {
                            return false; // Słowa są bezpośrednio obok siebie
                        }
                    }
                    else if (direction == WordDirection.Down && placedWord.Direction == WordDirection.Down && col == placedWord.Column)
                    {
                        int newStart = row;
                        int newEnd = row + word.Length - 1;
                        int placedStart = placedWord.Row;
                        int placedEnd = placedWord.Row + placedWord.Word.Length - 1;
                        
                        // Sprawdź czy są bezpośrednio obok (bez pustej kratki między)
                        if (Math.Abs(newStart - placedEnd) == 1 || Math.Abs(placedStart - newEnd) == 1)
                        {
                            return false; // Słowa są bezpośrednio obok siebie
                        }
                    }
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Umieszcza słowo w siatce
    /// </summary>
    private void PlaceWord(CrosswordRLState state, CrosswordRLAction action)
    {
        for (int i = 0; i < action.Word.Length; i++)
        {
            int row = action.Direction == WordDirection.Down ? action.Row + i : action.Row;
            int col = action.Direction == WordDirection.Across ? action.Column + i : action.Column;
            
            char letter = char.ToUpperInvariant(action.Word[i]);
            state.Grid.SetLetter(row, col, letter);
        }
    }
    
    /// <summary>
    /// Liczy przecięcia z istniejącymi słowami
    /// </summary>
    private int CountIntersections(CrosswordRLState state, CrosswordRLAction action)
    {
        int intersections = 0;
        
        // Sprawdź każdą komórkę, którą zajmie nowe słowo
        for (int i = 0; i < action.Word.Length; i++)
        {
            int row = action.Direction == WordDirection.Down ? action.Row + i : action.Row;
            int col = action.Direction == WordDirection.Across ? action.Column + i : action.Column;
            
            var cell = state.Grid.GetCell(row, col);
            
            if (cell.HasLetter)
            {
                // Sprawdź czy ta komórka jest częścią innego słowa
                bool isIntersection = state.PlacedWords.Any(pw =>
                {
                    var wordPositions = GetWordPositions(pw.Row, pw.Column, pw.Word.Length, 
                        pw.Direction == WordDirection.Across);
                    return wordPositions.Contains((row, col));
                });
                
                if (isIntersection)
                {
                    intersections++;
                }
            }
        }
        
        return intersections;
    }
    
    /// <summary>
    /// Zwraca pozycje zajmowane przez słowo
    /// </summary>
    private HashSet<(int row, int col)> GetWordPositions(int startRow, int startCol, int length, bool isHorizontal)
    {
        var positions = new HashSet<(int row, int col)>();
        
        for (int i = 0; i < length; i++)
        {
            if (isHorizontal)
            {
                positions.Add((startRow, startCol + i));
            }
            else
            {
                positions.Add((startRow + i, startCol));
            }
        }
        
        return positions;
    }
}
