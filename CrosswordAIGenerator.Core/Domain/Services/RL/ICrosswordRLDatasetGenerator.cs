using CrosswordAIGenerator.Core.Domain.Models.RL;

namespace CrosswordAIGenerator.Core.Domain.Services.RL;

/// <summary>
/// Generator datasetów RL
/// </summary>
public interface ICrosswordRLDatasetGenerator
{
    /// <summary>
    /// Generuje dataset RL
    /// </summary>
    List<CrosswordRLDatasetEntry> GenerateDataset(
        int entryCount,
        int rows,
        int columns,
        int wordCount,
        SelfPlayStrategy strategy);
    
    /// <summary>
    /// Eksportuje dataset do JSONL
    /// </summary>
    void ExportToJsonl(List<CrosswordRLDatasetEntry> entries, string filePath);
    
    /// <summary>
    /// Konwertuje RL dataset na format supervised (prompt/response) dla Behavior Cloning
    /// </summary>
    List<SupervisedDatasetEntry> ConvertToSupervisedFormat(List<CrosswordRLDatasetEntry> rlEntries);
    
    /// <summary>
    /// Pobiera ważone próbki z datasetu (oversample przykładów z wyższym reward)
    /// </summary>
    List<CrosswordRLDatasetEntry> GetWeightedSamples(
        List<CrosswordRLDatasetEntry> entries,
        int count,
        double minReward = 0.0);
    
    /// <summary>
    /// Eksportuje dataset do formatu gotowego do treningu (supervised lub RL)
    /// </summary>
    void ExportForTraining(
        List<CrosswordRLDatasetEntry> entries,
        string filePath,
        bool supervisedFormat = false);
    
    /// <summary>
    /// Eksportuje supervised dataset do JSONL
    /// </summary>
    void ExportSupervisedToJsonl(List<SupervisedDatasetEntry> entries, string filePath);
    
    /// <summary>
    /// Oblicza statystyki datasetu
    /// </summary>
    DatasetStatistics GetStatistics(List<CrosswordRLDatasetEntry> entries);
}
