using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;

namespace CrosswordAIGenerator.Core.Application.Services.RL;

/// <summary>
/// Kalkulator nagród dla środowiska RL
/// </summary>
public class CrosswordRLRewardCalculator : ICrosswordRLRewardCalculator
{
    private const double CompletionRewardValue = 100.0;
    private const double PlacementRewardValue = 10.0;
    private const double IntersectionRewardPerIntersection = 5.0;
    private const double PenaltyValue = -5.0;
    
    // Duże kary za nieprawidłowe krzyżówki
    private const double NoIntersectionPenalty = -50.0; // Słowo bez przecięć (samotne)
    private const double AdjacentWithoutIntersectionPenalty = -30.0; // Słowa obok siebie bez przecięć
    private const double DisconnectedWordsPenalty = -100.0; // Słowa nie są połączone w jedną sieć
    
    private readonly ICursorLogger? _logger;
    
    public CrosswordRLRewardCalculator(ICursorLogger? logger = null)
    {
        _logger = logger;
    }
    
    public CrosswordRLReward CalculateReward(
        CrosswordRLState previousState,
        CrosswordRLState currentState,
        CrosswordRLAction action,
        bool isTerminal)
    {
        var reward = new CrosswordRLReward();
        
        // Nagroda za ukończenie (tylko jeśli wszystkie słowa są połączone)
        if (isTerminal && currentState.IsTerminal())
        {
            if (AreAllWordsConnected(currentState))
            {
                reward.CompletionReward = GetCompletionReward();
            }
            else
            {
                // Duża kara - krzyżówka nie jest spójna
                reward.Penalty += DisconnectedWordsPenalty;
            }
        }
        
        // Nagroda za umieszczenie słowa (jeśli akcja była poprawna)
        // UWAGA: IsActionValid sprawdza tylko podstawowe rzeczy, nie sprawdza reguł krzyżówki
        // Więc sprawdzamy reguły tutaj i karzemy za naruszenia
        
        int intersectionCount = CountIntersections(previousState, action);
        
        // WAŻNE: Każde słowo oprócz pierwszego MUSI mieć przecięcie
        if (previousState.PlacedWords.Count > 0 && intersectionCount == 0)
        {
            // To nie jest poprawne umieszczenie - duża kara
            reward.Penalty += NoIntersectionPenalty;
            _logger?.Warning($"CrosswordRLRewardCalculator: Word '{action.Word}' placed without intersections. Penalty: {NoIntersectionPenalty}");
            reward.TotalReward = reward.CompletionReward + reward.PlacementReward + 
                                reward.IntersectionReward + reward.Penalty;
            return reward; // Zwróć od razu - nie ma nagrody za umieszczenie
        }
        
        // Sprawdź czy słowo nie jest obok innego bez przecięć
        if (HasAdjacentWordsWithoutIntersection(previousState, action))
        {
            reward.Penalty += AdjacentWithoutIntersectionPenalty;
            _logger?.Warning($"CrosswordRLRewardCalculator: Word '{action.Word}' adjacent to other word without intersection. Penalty: {AdjacentWithoutIntersectionPenalty}");
        }
        
        // Sprawdź podstawową poprawność (czy słowo może być umieszczone)
        if (IsActionValid(previousState, action))
        {
            // Jeśli wszystko OK, daj nagrodę
            reward.PlacementReward = GetPlacementReward();
            reward.IntersectionReward = GetIntersectionReward(intersectionCount);
        }
        else
        {
            // Kara za niepoprawną akcję (np. słowo poza granicami, konflikt liter)
            reward.Penalty = GetPenalty();
        }
        
        reward.TotalReward = reward.CompletionReward + reward.PlacementReward + 
                            reward.IntersectionReward + reward.Penalty;
        
        return reward;
    }
    
    public double GetCompletionReward()
    {
        return CompletionRewardValue;
    }
    
    public double GetPlacementReward()
    {
        return PlacementRewardValue;
    }
    
    public double GetIntersectionReward(int intersectionCount)
    {
        return intersectionCount * IntersectionRewardPerIntersection;
    }
    
    public double GetPenalty()
    {
        return PenaltyValue;
    }
    
    /// <summary>
    /// Sprawdza czy akcja jest poprawna (słowo może być umieszczone)
    /// </summary>
    private bool IsActionValid(CrosswordRLState state, CrosswordRLAction action)
    {
        // Sprawdź czy słowo jest w liście pozostałych słów
        if (!state.RemainingWords.Any(w => w.Equals(action.Word, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        
        // Sprawdź czy pozycja jest w granicach siatki
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
                return false; // Nie można umieścić słowa na ścianie
            }
            
            if (cell.HasLetter)
            {
                // Jeśli komórka ma literę, musi być zgodna z literą w słowie
                if (char.ToUpperInvariant(cell.Letter!.Value) != char.ToUpperInvariant(action.Word[i]))
                {
                    return false;
                }
            }
        }
        
        return true;
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
    
    /// <summary>
    /// Sprawdza czy nowe słowo jest obok innego słowa bez przecięcia
    /// </summary>
    private bool HasAdjacentWordsWithoutIntersection(CrosswordRLState state, CrosswordRLAction action)
    {
        var newWordPositions = GetWordPositions(
            action.Row, 
            action.Column, 
            action.Word.Length, 
            action.Direction == WordDirection.Across).ToHashSet();
        
        foreach (var placedWord in state.PlacedWords)
        {
            var placedPositions = GetWordPositions(
                placedWord.Row, 
                placedWord.Column, 
                placedWord.Word.Length, 
                placedWord.Direction == WordDirection.Across).ToHashSet();
            
            // Jeśli słowa się przecinają, to OK
            if (newWordPositions.Intersect(placedPositions).Any())
                continue;
            
            // Sprawdź czy słowa są w tym samym kierunku i w tym samym wierszu/kolumnie
            bool sameDirection = action.Direction == placedWord.Direction;
            bool sameRow = action.Direction == WordDirection.Across && action.Row == placedWord.Row;
            bool sameCol = action.Direction == WordDirection.Down && action.Column == placedWord.Column;
            
            if (sameDirection && (sameRow || sameCol))
            {
                // Sprawdź czy są bezpośrednio obok siebie (bez pustej kratki)
                if (action.Direction == WordDirection.Across && sameRow)
                {
                    int actionStart = action.Column;
                    int actionEnd = action.Column + action.Word.Length - 1;
                    int placedStart = placedWord.Column;
                    int placedEnd = placedWord.Column + placedWord.Word.Length - 1;
                    
                    if (Math.Abs(actionStart - placedEnd) == 1 || Math.Abs(placedStart - actionEnd) == 1)
                    {
                        return true; // Słowa są obok siebie bez przecięcia
                    }
                }
                else if (action.Direction == WordDirection.Down && sameCol)
                {
                    int actionStart = action.Row;
                    int actionEnd = action.Row + action.Word.Length - 1;
                    int placedStart = placedWord.Row;
                    int placedEnd = placedWord.Row + placedWord.Word.Length - 1;
                    
                    if (Math.Abs(actionStart - placedEnd) == 1 || Math.Abs(placedStart - actionEnd) == 1)
                    {
                        return true; // Słowa są obok siebie bez przecięcia
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Sprawdza czy wszystkie słowa są połączone w jedną sieć (każde słowo ma przynajmniej jedno przecięcie)
    /// </summary>
    private bool AreAllWordsConnected(CrosswordRLState state)
    {
        if (state.PlacedWords.Count <= 1)
            return true; // Jedno słowo lub brak - zawsze spójne
        
        // Sprawdź czy każde słowo ma przynajmniej jedno przecięcie z innym
        foreach (var word in state.PlacedWords)
        {
            bool hasIntersection = false;
            var wordPositions = GetWordPositions(
                word.Row, 
                word.Column, 
                word.Word.Length, 
                word.Direction == WordDirection.Across).ToHashSet();
            
            foreach (var otherWord in state.PlacedWords)
            {
                if (word == otherWord)
                    continue;
                
                var otherPositions = GetWordPositions(
                    otherWord.Row, 
                    otherWord.Column, 
                    otherWord.Word.Length, 
                    otherWord.Direction == WordDirection.Across).ToHashSet();
                
                if (wordPositions.Intersect(otherPositions).Any())
                {
                    hasIntersection = true;
                    break;
                }
            }
            
            if (!hasIntersection)
            {
                return false; // Znaleziono słowo bez przecięć
            }
        }
        
        return true;
    }
}
