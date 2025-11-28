using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora datasetów z krzyżówkami z własnymi słowami
/// </summary>
public interface ICustomWordsDatasetGenerator
{
    /// <summary>
    /// Generuje pojedynczą krzyżówkę z podanymi przez użytkownika słowami
    /// </summary>
    Result<DatasetEntry, string> GenerateWithCustomWords(
        int rows, 
        int columns, 
        string highlightedWord, 
        List<string> customWords, 
        int minWordsCount = 0, 
        Dictionary<string, string>? wordDefinitions = null);

    /// <summary>
    /// Generuje dataset z podanymi przez użytkownika słowami
    /// </summary>
    List<DatasetEntry> GenerateCustomWordsDataset(
        int count,
        int rows,
        int columns,
        string highlightedWord,
        List<string> customWords,
        int minWordsCount = 0,
        Action<int, int>? onProgress = null,
        Dictionary<string, string>? wordDefinitions = null);
}

