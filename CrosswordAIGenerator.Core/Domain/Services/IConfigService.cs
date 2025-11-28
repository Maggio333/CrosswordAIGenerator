namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla serwisu konfiguracji (stałe, magic numbers)
/// </summary>
public interface IConfigService
{
    // Grid constraints
    int MinGridSize { get; }
    int MaxGridSize { get; }
    
    // Word constraints
    int MinWordCount { get; }
    int MaxWordCount { get; }
    int MinWordLength { get; }
    int MaxWordLength { get; }
    
    // Dataset constraints
    int MinDatasetCount { get; }
    int MaxDatasetCount { get; }
    
    // Wall probability
    double MinWallProbability { get; }
    double MaxWallProbability { get; }
    
    // Render delays (ms)
    int RenderDelayMs { get; }
    int ExtendedRenderDelayMs { get; }
    
    // Default values
    int DefaultGridSize { get; }
    int DefaultDatasetCount { get; }
    double DefaultWallProbability { get; }
    
    // XAML defaults
    int DefaultXamlWidth { get; }
    int DefaultXamlHeight { get; }
}

