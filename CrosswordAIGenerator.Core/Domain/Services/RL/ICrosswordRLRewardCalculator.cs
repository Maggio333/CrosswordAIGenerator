using CrosswordAIGenerator.Core.Domain.Models.RL;

namespace CrosswordAIGenerator.Core.Domain.Services.RL;

/// <summary>
/// Kalkulator nagród dla środowiska RL
/// </summary>
public interface ICrosswordRLRewardCalculator
{
    /// <summary>
    /// Oblicza nagrodę za akcję
    /// </summary>
    CrosswordRLReward CalculateReward(
        CrosswordRLState previousState,
        CrosswordRLState currentState,
        CrosswordRLAction action,
        bool isTerminal);
    
    /// <summary>
    /// Oblicza nagrodę za ukończenie krzyżówki
    /// </summary>
    double GetCompletionReward();
    
    /// <summary>
    /// Oblicza nagrodę za umieszczenie słowa
    /// </summary>
    double GetPlacementReward();
    
    /// <summary>
    /// Oblicza nagrodę za przecięcia
    /// </summary>
    double GetIntersectionReward(int intersectionCount);
    
    /// <summary>
    /// Oblicza karę za niepoprawną akcję
    /// </summary>
    double GetPenalty();
}
