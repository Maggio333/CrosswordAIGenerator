using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora datasetów z krzyżówkami ze słowami
/// </summary>
public interface IWordsDatasetGenerator
{
    /// <summary>
    /// Generuje krzyżówkę z rzeczywistymi słowami i przecięciami
    /// </summary>
    Result<DatasetEntry, string> GenerateWithWordsExample(
        int rows, 
        int columns, 
        int targetWordCount = 5, 
        int? seed = null, 
        string? highlightedWord = null);

    /// <summary>
    /// Generuje wiele przykładów krzyżówek ze słowami
    /// </summary>
    List<DatasetEntry> GenerateWithWordsDataset(
        int count,
        int minSize = 8,
        int maxSize = 15,
        int targetWordCount = 5,
        string? highlightedWord = null,
        Action<int, int>? onProgress = null);
}

