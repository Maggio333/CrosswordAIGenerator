namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Statystyki datasetu RL
/// </summary>
public class DatasetStatistics
{
    /// <summary>
    /// Całkowita liczba wpisów
    /// </summary>
    public int TotalEntries { get; set; }
    
    /// <summary>
    /// Liczba unikalnych epizodów
    /// </summary>
    public int UniqueEpisodes { get; set; }
    
    /// <summary>
    /// Średnia nagroda
    /// </summary>
    public double MeanReward { get; set; }
    
    /// <summary>
    /// Mediana nagrody
    /// </summary>
    public double MedianReward { get; set; }
    
    /// <summary>
    /// Minimalna nagroda
    /// </summary>
    public double MinReward { get; set; }
    
    /// <summary>
    /// Maksymalna nagroda
    /// </summary>
    public double MaxReward { get; set; }
    
    /// <summary>
    /// Odchylenie standardowe nagrody
    /// </summary>
    public double StdDevReward { get; set; }
    
    /// <summary>
    /// Liczba terminalnych stanów
    /// </summary>
    public int TerminalStates { get; set; }
    
    /// <summary>
    /// Liczba naturalnych zakończeń epizodów
    /// </summary>
    public int NaturalTerminations { get; set; }
    
    /// <summary>
    /// Procent przykładów z nagrodą >= 0
    /// </summary>
    public double PositiveRewardPercentage { get; set; }
    
    /// <summary>
    /// Procent przykładów z nagrodą < 0 (kary)
    /// </summary>
    public double NegativeRewardPercentage { get; set; }
    
    /// <summary>
    /// Średnia liczba kroków na epizod
    /// </summary>
    public double AverageStepsPerEpisode { get; set; }
}
