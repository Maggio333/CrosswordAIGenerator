using System.Text.Json;
using System.IO;
using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;
using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using CrosswordAIGenerator.Core.Application.Services.RL;

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
    private readonly ICrosswordRLDatasetGenerator? _rlDatasetGenerator;

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
        ICrossGridGenerator? crossGridGenerator = null,
        ICrosswordRLDatasetGenerator? rlDatasetGenerator = null)
    {
        _emptyGridGenerator = emptyGridGenerator ?? throw new ArgumentNullException(nameof(emptyGridGenerator));
        _wordsGenerator = wordsGenerator ?? throw new ArgumentNullException(nameof(wordsGenerator));
        _customWordsGenerator = customWordsGenerator ?? throw new ArgumentNullException(nameof(customWordsGenerator));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _crossGridGenerator = crossGridGenerator;
        _rlDatasetGenerator = rlDatasetGenerator;
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

    /// <summary>
    /// Generuje dataset RL (Reinforcement Learning) - pary (stan, akcja, nagroda) z gier self-play
    /// </summary>
    public List<CrosswordRLDatasetEntry> GenerateRLDataset(
        int entryCount,
        int rows = 15,
        int columns = 15,
        int wordCount = 5,
        SelfPlayStrategy strategy = SelfPlayStrategy.Random)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured. Please register ICrosswordRLDatasetGenerator in dependency injection.");
        }
        
        return _rlDatasetGenerator.GenerateDataset(entryCount, rows, columns, wordCount, strategy);
    }

    /// <summary>
    /// Eksportuje dataset RL do JSONL w formacie gotowym do treningu PPO
    /// </summary>
    public void ExportRLDatasetToJsonl(List<CrosswordRLDatasetEntry> entries, string filePath)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured. Please register ICrosswordRLDatasetGenerator in dependency injection.");
        }
        
        _rlDatasetGenerator.ExportToJsonl(entries, filePath);
    }
    
    /// <summary>
    /// Konwertuje RL dataset na format supervised (prompt/response) dla Behavior Cloning
    /// </summary>
    public List<SupervisedDatasetEntry> ConvertRLToSupervisedFormat(List<CrosswordRLDatasetEntry> rlEntries)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured.");
        }
        
        return _rlDatasetGenerator.ConvertToSupervisedFormat(rlEntries);
    }
    
    /// <summary>
    /// Pobiera ważone próbki z RL datasetu (oversample przykładów z wyższym reward)
    /// </summary>
    public List<CrosswordRLDatasetEntry> GetWeightedRLSamples(
        List<CrosswordRLDatasetEntry> entries,
        int count,
        double minReward = 0.0)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured.");
        }
        
        return _rlDatasetGenerator.GetWeightedSamples(entries, count, minReward);
    }
    
    /// <summary>
    /// Eksportuje dataset do formatu gotowego do treningu (supervised lub RL)
    /// </summary>
    public void ExportRLForTraining(
        List<CrosswordRLDatasetEntry> entries,
        string filePath,
        bool supervisedFormat = false)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured.");
        }
        
        _rlDatasetGenerator.ExportForTraining(entries, filePath, supervisedFormat);
    }
    
    /// <summary>
    /// Oblicza statystyki RL datasetu
    /// </summary>
    public DatasetStatistics GetRLDatasetStatistics(List<CrosswordRLDatasetEntry> entries)
    {
        if (_rlDatasetGenerator == null)
        {
            throw new InvalidOperationException("RL Dataset Generator is not configured.");
        }
        
        return _rlDatasetGenerator.GetStatistics(entries);
    }
}

