namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Trajektoria gry - sekwencja stanów, akcji i nagród
/// </summary>
public class CrosswordRLTrajectory
{
    /// <summary>
    /// Lista kroków w trajektorii
    /// </summary>
    public List<CrosswordRLStep> Steps { get; set; }
    
    /// <summary>
    /// Stan początkowy
    /// </summary>
    public CrosswordRLState InitialState { get; set; }
    
    /// <summary>
    /// Stan końcowy
    /// </summary>
    public CrosswordRLState? FinalState { get; set; }
    
    /// <summary>
    /// Czy gra zakończyła się sukcesem (wszystkie słowa umieszczone)
    /// </summary>
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Czy epizod zakończył się naturalnie (wszystkie słowa umieszczone) czy przez cutoff (max_steps)
    /// </summary>
    public bool IsNaturalTermination { get; set; }
    
    /// <summary>
    /// Całkowita nagroda
    /// </summary>
    public double TotalReward { get; set; }
    
    public CrosswordRLTrajectory(CrosswordRLState initialState)
    {
        InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Steps = new List<CrosswordRLStep>();
        IsSuccessful = false;
        TotalReward = 0;
    }
}

/// <summary>
/// Pojedynczy krok w trajektorii
/// </summary>
public class CrosswordRLStep
{
    /// <summary>
    /// Stan przed akcją
    /// </summary>
    public CrosswordRLState State { get; set; }
    
    /// <summary>
    /// Wykonana akcja
    /// </summary>
    public CrosswordRLAction Action { get; set; }
    
    /// <summary>
    /// Nagroda za akcję
    /// </summary>
    public CrosswordRLReward Reward { get; set; }
    
    /// <summary>
    /// Stan po akcji
    /// </summary>
    public CrosswordRLState NextState { get; set; }
    
    /// <summary>
    /// Czy stan jest terminalny
    /// </summary>
    public bool IsTerminal { get; set; }
    
    public CrosswordRLStep(
        CrosswordRLState state,
        CrosswordRLAction action,
        CrosswordRLReward reward,
        CrosswordRLState nextState,
        bool isTerminal)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Reward = reward ?? throw new ArgumentNullException(nameof(reward));
        NextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
        IsTerminal = isTerminal;
    }
}
