namespace CrosswordAIGenerator.Core.Application_.Services;

/// <summary>
/// Implementacja serwisu konfiguracji - zawiera wszystkie stałe i magic numbers
/// </summary>
public class ConfigService : IConfigService
{
    // Grid constraints
    public int MinGridSize => 5;
    public int MaxGridSize => 30;
    
    // Word constraints
    public int MinWordCount => 3;
    public int MaxWordCount => 20;
    public int MinWordLength => 6;
    public int MaxWordLength => 20;
    
    // Dataset constraints
    public int MinDatasetCount => 1;
    public int MaxDatasetCount => 10000;
    
    // Wall probability
    public double MinWallProbability => 0.0;
    public double MaxWallProbability => 1.0;
    
    // Render delays (ms)
    public int RenderDelayMs => 200;
    public int ExtendedRenderDelayMs => 300;
    
    // Default values
    public int DefaultGridSize => 15;
    public int DefaultDatasetCount => 100;
    public double DefaultWallProbability => 0.1;
    
    // XAML defaults
    public int DefaultXamlWidth => 500;
    public int DefaultXamlHeight => 500;
}

