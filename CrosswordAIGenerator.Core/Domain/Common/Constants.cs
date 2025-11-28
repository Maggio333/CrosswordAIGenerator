namespace CrosswordAIGenerator.Core.Domain.Common;

/// <summary>
/// Stałe używane w całej aplikacji
/// </summary>
public static class Constants
{
    // Rozmiary siatek
    public const int MinGridSize = 5;
    public const int MaxGridSize = 30;
    public const int DefaultGridSize = 15;
    
    // Rozmiary dla generowania datasetów
    public const int MinDatasetSize = 5;
    public const int MaxDatasetSize = 15;
    public const int DefaultDatasetCount = 100;
    public const int MaxDatasetCount = 10000;
    
    // Rozmiary dla słów
    public const int MinWordLength = 6;
    public const int MaxWordLength = 20;
    public const int DefaultTargetWordCount = 5;
    public const int MinTargetWordCount = 3;
    public const int MaxTargetWordCount = 20;
    
    // Prawdopodobieństwa
    public const double DefaultWallProbability = 0.1;
    public const double MinWallProbability = 0.0;
    public const double MaxWallProbability = 1.0;
    
    // Opóźnienia renderowania (ms)
    public const int RenderDelayMs = 200;
    public const int ExtendedRenderDelayMs = 300;
    
    // Rozmiary XAML
    public const int DefaultXamlWidth = 500;
    public const int DefaultXamlHeight = 500;
    
    // Próby generowania
    public const int DefaultMaxAttempts = 50;
    public const int DefaultMaxRetries = 10;
}

