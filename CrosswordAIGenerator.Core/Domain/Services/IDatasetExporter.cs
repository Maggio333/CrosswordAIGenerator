using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla eksportera datasetów do plików
/// </summary>
public interface IDatasetExporter
{
    /// <summary>
    /// Zapisuje dataset do pliku JSON, filtrując pola zgodnie z Settings
    /// </summary>
    void SaveDatasetToFile(List<DatasetEntry> entries, string filePath, DatasetSettings? settings = null);

    /// <summary>
    /// Eksportuje dataset do JSONL (JSON Lines) w formacie gotowym do finetunowania
    /// </summary>
    void ExportToFinetuneJsonl(List<DatasetEntry> entries, string filePath);
}

