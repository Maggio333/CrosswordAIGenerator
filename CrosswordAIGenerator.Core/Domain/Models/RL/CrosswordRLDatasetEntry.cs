namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Wpis w datasecie RL - para (stan, akcja, nagroda)
/// </summary>
public class CrosswordRLDatasetEntry
{
    /// <summary>
    /// Identyfikator epizodu (rozgrywki)
    /// </summary>
    public int EpisodeId { get; set; }
    
    /// <summary>
    /// Indeks kroku w epizodzie (t = 0, 1, 2, ...)
    /// </summary>
    public int T { get; set; }
    
    /// <summary>
    /// Stan przed akcją (jako tekst CrossGrid)
    /// </summary>
    public string State { get; set; } = string.Empty;
    
    /// <summary>
    /// Wykonana akcja
    /// </summary>
    public CrosswordRLAction Action { get; set; }
    
    /// <summary>
    /// Nagroda za akcję (skalar - zawsze używać tego do RL, nie reward_details.total)
    /// </summary>
    public double Reward { get; set; }
    
    /// <summary>
    /// Stan po akcji (jako tekst CrossGrid)
    /// </summary>
    public string NextState { get; set; } = string.Empty;
    
    /// <summary>
    /// Czy stan jest terminalny
    /// </summary>
    public bool IsTerminal { get; set; }
    
    /// <summary>
    /// Alias dla IsTerminal (dla spójności z bibliotekami RL jak Gym)
    /// </summary>
    public bool Done => IsTerminal;
    
    /// <summary>
    /// Czy epizod zakończył się naturalnie (wszystkie słowa umieszczone) czy przez cutoff (max_steps)
    /// Dotyczy tylko ostatniego kroku w epizodzie (gdy IsTerminal == true)
    /// </summary>
    public bool IsNaturalTermination { get; set; }
    
    /// <summary>
    /// Szczegółowe komponenty nagrody
    /// </summary>
    public CrosswordRLReward RewardDetails { get; set; }
    
    /// <summary>
    /// Lista pozostałych słów w stanie
    /// </summary>
    public List<string> RemainingWords { get; set; }
    
    /// <summary>
    /// Lista umieszczonych słów w stanie
    /// </summary>
    public List<PlacedWordInfo> PlacedWords { get; set; }
    
    public CrosswordRLDatasetEntry()
    {
        Action = new CrosswordRLAction();
        RewardDetails = new CrosswordRLReward();
        RemainingWords = new List<string>();
        PlacedWords = new List<PlacedWordInfo>();
    }
}
