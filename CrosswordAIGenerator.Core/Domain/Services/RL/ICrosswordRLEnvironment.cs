using CrosswordAIGenerator.Core.Domain.Models.RL;

namespace CrosswordAIGenerator.Core.Domain.Services.RL;

/// <summary>
/// Środowisko RL dla krzyżówek
/// </summary>
public interface ICrosswordRLEnvironment
{
    /// <summary>
    /// Tworzy początkowy stan gry
    /// </summary>
    CrosswordRLState GetInitialState(int rows, int columns, List<string> targetWords);
    
    /// <summary>
    /// Zwraca listę poprawnych akcji dla danego stanu
    /// </summary>
    List<CrosswordRLAction> GetValidActions(CrosswordRLState state);
    
    /// <summary>
    /// Wykonuje akcję i zwraca nowy stan, nagrodę i informację o zakończeniu
    /// </summary>
    (CrosswordRLState nextState, double reward, bool isTerminal) Step(CrosswordRLState state, CrosswordRLAction action);
    
    /// <summary>
    /// Sprawdza czy stan jest terminalny
    /// </summary>
    bool IsTerminal(CrosswordRLState state);
    
    /// <summary>
    /// Oblicza nagrodę za akcję
    /// </summary>
    double CalculateReward(CrosswordRLState previousState, CrosswordRLState currentState, CrosswordRLAction action);
}
