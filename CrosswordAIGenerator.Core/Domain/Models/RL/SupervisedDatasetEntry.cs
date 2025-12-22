namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Wpis w datasecie supervised (Behavior Cloning) - para prompt/response
/// </summary>
public class SupervisedDatasetEntry
{
    /// <summary>
    /// Prompt (stan krzyżówki z instrukcją)
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Response (akcja jako JSON)
    /// </summary>
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// Opcjonalna waga przykładu (dla ważonego treningu)
    /// </summary>
    public double Weight { get; set; } = 1.0;
    
    /// <summary>
    /// Reward z oryginalnego RL datasetu (dla debugowania)
    /// </summary>
    public double? Reward { get; set; }
    
    /// <summary>
    /// Episode ID z oryginalnego RL datasetu (dla śledzenia)
    /// </summary>
    public int? EpisodeId { get; set; }
    
    /// <summary>
    /// Krok t z oryginalnego RL datasetu (dla śledzenia)
    /// </summary>
    public int? T { get; set; }
}
