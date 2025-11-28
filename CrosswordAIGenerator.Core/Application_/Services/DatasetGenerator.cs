using System.Text.Json;
using System.IO;
using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Infrastructure.Services;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator datasetów - orchestrator który deleguje do specjalistycznych serwisów
/// </summary>
public class DatasetGenerator
{
    private readonly IEmptyGridDatasetGenerator _emptyGridGenerator;
    private readonly IWordsDatasetGenerator _wordsGenerator;
    private readonly ICustomWordsDatasetGenerator _customWordsGenerator;
    private readonly IDatasetExporter _exporter;
    private readonly ICrossGridGenerator? _crossGridGenerator;

    /// <summary>
    /// Waliduje wszystkie CrossGrid w datasetach
    /// </summary>
    public List<(string entryId, CrossGridValidationResult result)> ValidateCrossGridsInDataset(List<DatasetEntry> entries)
    {
        var results = new List<(string entryId, CrossGridValidationResult result)>();
        
        if (_crossGridGenerator == null)
        {
            return results;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.CrossGrid))
            {
                continue; // Pomijaj wpisy bez CrossGrid
            }

            var validationResult = _crossGridGenerator.ValidateCrossGrid(entry.CrossGrid);
            results.Add((entry.Id, validationResult));
        }

        return results;
    }


    public DatasetGenerator(
        IEmptyGridDatasetGenerator emptyGridGenerator,
        IWordsDatasetGenerator wordsGenerator,
        ICustomWordsDatasetGenerator customWordsGenerator,
        IDatasetExporter exporter,
        ICrossGridGenerator? crossGridGenerator = null)
    {
        _emptyGridGenerator = emptyGridGenerator ?? throw new ArgumentNullException(nameof(emptyGridGenerator));
        _wordsGenerator = wordsGenerator ?? throw new ArgumentNullException(nameof(wordsGenerator));
        _customWordsGenerator = customWordsGenerator ?? throw new ArgumentNullException(nameof(customWordsGenerator));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _crossGridGenerator = crossGridGenerator;
    }

    /// <summary>
    /// Generuje pojedynczy przykład pustej siatki
    /// </summary>
    public DatasetEntry GenerateEmptyGridExample(int rows, int columns, bool withWalls = false, double wallProbability = Constants.DefaultWallProbability, int? seed = null)
    {
        return _emptyGridGenerator.GenerateEmptyGridExample(rows, columns, withWalls, wallProbability, seed);
    }

    /// <summary>
    /// Generuje wiele przykładów pustych siatek
    /// </summary>
    public List<DatasetEntry> GenerateEmptyGridDataset(
        int count,
        int minSize = Constants.MinDatasetSize,
        int maxSize = Constants.MaxDatasetSize,
        bool includeWithWalls = true,
        double wallProbability = Constants.DefaultWallProbability)
    {
        return _emptyGridGenerator.GenerateEmptyGridDataset(count, minSize, maxSize, includeWithWalls, wallProbability);
    }

    /// <summary>
    /// Generuje krzyżówkę z rzeczywistymi słowami i przecięciami
    /// </summary>
    public Result<DatasetEntry, string> GenerateWithWordsExample(int rows, int columns, int targetWordCount = Constants.DefaultTargetWordCount, int? seed = null, string? highlightedWord = null)
    {
        return _wordsGenerator.GenerateWithWordsExample(rows, columns, targetWordCount, seed, highlightedWord);
    }

    /// <summary>
    /// Generuje wiele przykładów krzyżówek ze słowami
    /// </summary>
    public List<DatasetEntry> GenerateWithWordsDataset(
        int count,
        int minSize = 8,
        int maxSize = Constants.MaxDatasetSize,
        int targetWordCount = Constants.DefaultTargetWordCount,
        string? highlightedWord = null,
        Action<int, int>? onProgress = null)
    {
        return _wordsGenerator.GenerateWithWordsDataset(count, minSize, maxSize, targetWordCount, highlightedWord, onProgress);
    }

    /// <summary>
    /// Generuje pojedynczą krzyżówkę z podanymi przez użytkownika słowami
    /// </summary>
    public Result<DatasetEntry, string> GenerateWithCustomWords(
        int rows, int columns, string highlightedWord, List<string> customWords, int minWordsCount = 0, Dictionary<string, string>? wordDefinitions = null)
    {
        return _customWordsGenerator.GenerateWithCustomWords(rows, columns, highlightedWord, customWords, minWordsCount, wordDefinitions);
    }

    /// <summary>
    /// Generuje dataset z podanymi przez użytkownika słowami
    /// </summary>
    public List<DatasetEntry> GenerateCustomWordsDataset(
        int count,
        int rows,
        int columns,
        string highlightedWord,
        List<string> customWords,
        int minWordsCount = 0,
        Action<int, int>? onProgress = null,
        Dictionary<string, string>? wordDefinitions = null)
    {
        return _customWordsGenerator.GenerateCustomWordsDataset(count, rows, columns, highlightedWord, customWords, minWordsCount, onProgress, wordDefinitions);
    }

    /// <summary>
    /// Zapisuje dataset do pliku JSON, filtrując pola zgodnie z Settings
    /// </summary>
    public void SaveDatasetToFile(List<DatasetEntry> entries, string filePath, DatasetSettings? settings = null)
    {
        _exporter.SaveDatasetToFile(entries, filePath, settings);
    }

    /// <summary>
    /// Eksportuje dataset do JSONL (JSON Lines) w formacie gotowym do finetunowania
    /// </summary>
    public void ExportToFinetuneJsonl(List<DatasetEntry> entries, string filePath)
    {
        _exporter.ExportToFinetuneJsonl(entries, filePath);
    }
}

