using System.Linq;
using System.Text;
using System.Text.Json;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Services.RL;
using CrosswordAIGenerator.Core.Infrastructure.Services.RL;

namespace CrosswordAIGenerator.Core.Application.Services.RL;

/// <summary>
/// Generator datasetów RL
/// </summary>
public class CrosswordRLDatasetGenerator : ICrosswordRLDatasetGenerator
{
    private readonly ICrosswordSelfPlayGenerator _selfPlayGenerator;
    private readonly ICrosswordRLAdapter _adapter;
    private readonly Domain.Services.ICrossGridGenerator _crossGridGenerator;
    private readonly CrosswordRLDatasetExporter _exporter;
    private readonly ICursorLogger? _logger;
    
    public CrosswordRLDatasetGenerator(
        ICrosswordSelfPlayGenerator selfPlayGenerator,
        ICrosswordRLAdapter adapter,
        Domain.Services.ICrossGridGenerator crossGridGenerator,
        ICursorLogger? logger = null)
    {
        _selfPlayGenerator = selfPlayGenerator ?? throw new ArgumentNullException(nameof(selfPlayGenerator));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _crossGridGenerator = crossGridGenerator ?? throw new ArgumentNullException(nameof(crossGridGenerator));
        _exporter = new CrosswordRLDatasetExporter(logger);
        _logger = logger;
    }
    
    public List<CrosswordRLDatasetEntry> GenerateDataset(
        int entryCount,
        int rows,
        int columns,
        int wordCount,
        SelfPlayStrategy strategy)
    {
        var entries = new List<CrosswordRLDatasetEntry>();
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Generating {entryCount} dataset entries. Grid: {rows}x{columns}, Words: {wordCount}, Strategy: {strategy}");
        
        // Oblicz ile gier potrzebujemy (każda gra może mieć wiele kroków)
        // Szacujemy średnio 3-5 kroków na grę
        int estimatedStepsPerGame = 4;
        int gameCount = Math.Max(1, (int)Math.Ceiling((double)entryCount / estimatedStepsPerGame));
        
        // Generuj gry
        var trajectories = _selfPlayGenerator.GenerateSelfPlayGames(
            gameCount,
            rows,
            columns,
            null, // Losowe słowa
            strategy);
        
        // Konwertuj trajektorie na wpisy datasetu
        int episodeId = 0;
        foreach (var trajectory in trajectories)
        {
            int t = 0; // Indeks kroku w epizodzie
            foreach (var step in trajectory.Steps)
            {
                // Sprawdź czy to ostatni krok w epizodzie
                bool isLastStep = (t == trajectory.Steps.Count - 1);
                var entry = ConvertStepToEntry(step, episodeId, t, trajectory.IsNaturalTermination && isLastStep);
                entries.Add(entry);
                t++;
                
                // Jeśli mamy wystarczająco wpisów, przerwij
                if (entries.Count >= entryCount)
                {
                    break;
                }
            }
            
            episodeId++;
            
            if (entries.Count >= entryCount)
            {
                break;
            }
        }
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Generated {entries.Count} dataset entries from {trajectories.Count} games");
        
        return entries.Take(entryCount).ToList();
    }
    
    public void ExportToJsonl(List<CrosswordRLDatasetEntry> entries, string filePath)
    {
        _exporter.ExportToJsonl(entries, filePath);
    }
    
    /// <summary>
    /// Konwertuje krok trajektorii na wpis datasetu
    /// </summary>
    private CrosswordRLDatasetEntry ConvertStepToEntry(CrosswordRLStep step, int episodeId, int t, bool isNaturalTermination)
    {
        var entry = new CrosswordRLDatasetEntry
        {
            EpisodeId = episodeId,
            T = t,
            Action = step.Action,
            Reward = step.Reward.TotalReward, // Reward to zawsze skalar z TotalReward
            RewardDetails = step.Reward, // RewardDetails to tylko logi/debug info
            IsTerminal = step.IsTerminal,
            IsNaturalTermination = isNaturalTermination, // Tylko dla ostatniego kroku w epizodzie
            RemainingWords = new List<string>(step.State.RemainingWords),
            PlacedWords = new List<PlacedWordInfo>(step.State.PlacedWords)
        };
        
        // WAŻNE: Używamy StateToPrompt zamiast tylko CrossGrid, aby state w datasecie
        // był dokładnie tym samym, co model widzi jako prompt (z instrukcją, remaining_words, etc.)
        // To zapewnia synchronizację między datasetem RL a supervised/BC
        entry.State = _adapter.StateToPrompt(step.State);
        entry.NextState = _adapter.StateToPrompt(step.NextState);
        
        return entry;
    }
    
    /// <summary>
    /// Konwertuje RL dataset na format supervised (prompt/response) dla Behavior Cloning
    /// </summary>
    public List<SupervisedDatasetEntry> ConvertToSupervisedFormat(List<CrosswordRLDatasetEntry> rlEntries)
    {
        _logger?.Info($"CrosswordRLDatasetGenerator: Converting {rlEntries.Count} RL entries to supervised format");
        
        var supervisedEntries = rlEntries.Select(entry =>
        {
            // Konwertuj akcję na JSON
            var actionJson = JsonSerializer.Serialize(new
            {
                word = entry.Action.Word,
                row = entry.Action.Row,
                col = entry.Action.Column,
                direction = entry.Action.Direction.ToString()
            });
            
            return new SupervisedDatasetEntry
            {
                Prompt = entry.State, // Już zawiera pełny prompt z instrukcją
                Response = actionJson,
                Weight = 1.0,
                Reward = entry.Reward,
                EpisodeId = entry.EpisodeId,
                T = entry.T
            };
        }).ToList();
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Converted to {supervisedEntries.Count} supervised entries");
        
        return supervisedEntries;
    }
    
    /// <summary>
    /// Pobiera ważone próbki z datasetu (oversample przykładów z wyższym reward)
    /// </summary>
    public List<CrosswordRLDatasetEntry> GetWeightedSamples(
        List<CrosswordRLDatasetEntry> entries,
        int count,
        double minReward = 0.0)
    {
        _logger?.Info($"CrosswordRLDatasetGenerator: Getting {count} weighted samples (minReward: {minReward})");
        
        // Filtruj przykłady z minimalnym rewardem
        var filtered = entries
            .Where(e => e.Reward >= minReward)
            .ToList();
        
        if (filtered.Count == 0)
        {
            _logger?.Warning($"CrosswordRLDatasetGenerator: No entries with reward >= {minReward}");
            return new List<CrosswordRLDatasetEntry>();
        }
        
        // Sortuj według reward (malejąco) i weź najlepsze
        var weighted = filtered
            .OrderByDescending(e => e.Reward)
            .Take(count)
            .ToList();
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Selected {weighted.Count} weighted samples (reward range: {weighted.Min(e => e.Reward):F2} - {weighted.Max(e => e.Reward):F2})");
        
        return weighted;
    }
    
    /// <summary>
    /// Eksportuje dataset do formatu gotowego do treningu (supervised lub RL)
    /// </summary>
    public void ExportForTraining(
        List<CrosswordRLDatasetEntry> entries,
        string filePath,
        bool supervisedFormat = false)
    {
        if (supervisedFormat)
        {
            // Format dla BC: prompt/response
            var supervised = ConvertToSupervisedFormat(entries);
            ExportSupervisedToJsonl(supervised, filePath);
        }
        else
        {
            // Format RL: pełny transition
            ExportToJsonl(entries, filePath);
        }
    }
    
    /// <summary>
    /// Eksportuje supervised dataset do JSONL
    /// </summary>
    public void ExportSupervisedToJsonl(List<SupervisedDatasetEntry> entries, string filePath)
    {
        if (entries == null || entries.Count == 0)
        {
            _logger?.Warning("CrosswordRLDatasetGenerator: No supervised entries to export");
            return;
        }
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Exporting {entries.Count} supervised entries to {filePath}");
        
        var jsonLines = new List<string>();
        
        foreach (var entry in entries)
        {
            var jsonObject = new
            {
                prompt = entry.Prompt,
                response = entry.Response,
                weight = entry.Weight,
                reward = entry.Reward,
                episode_id = entry.EpisodeId,
                t = entry.T
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            string jsonLine = JsonSerializer.Serialize(jsonObject, options);
            jsonLines.Add(jsonLine);
        }
        
        // Zapisz do pliku (UTF-8 bez BOM)
        var utf8NoBom = new UTF8Encoding(false);
        File.WriteAllLines(filePath, jsonLines, utf8NoBom);
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Exported {entries.Count} supervised entries to {filePath}");
    }
    
    /// <summary>
    /// Oblicza statystyki datasetu
    /// </summary>
    public DatasetStatistics GetStatistics(List<CrosswordRLDatasetEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return new DatasetStatistics();
        }
        
        var rewards = entries.Select(e => e.Reward).ToList();
        var sortedRewards = rewards.OrderBy(r => r).ToList();
        
        var stats = new DatasetStatistics
        {
            TotalEntries = entries.Count,
            UniqueEpisodes = entries.Select(e => e.EpisodeId).Distinct().Count(),
            MeanReward = rewards.Average(),
            MedianReward = sortedRewards.Count % 2 == 0
                ? (sortedRewards[sortedRewards.Count / 2 - 1] + sortedRewards[sortedRewards.Count / 2]) / 2.0
                : sortedRewards[sortedRewards.Count / 2],
            MinReward = rewards.Min(),
            MaxReward = rewards.Max(),
            TerminalStates = entries.Count(e => e.IsTerminal),
            NaturalTerminations = entries.Count(e => e.IsNaturalTermination),
            PositiveRewardPercentage = rewards.Count(r => r >= 0) * 100.0 / rewards.Count,
            NegativeRewardPercentage = rewards.Count(r => r < 0) * 100.0 / rewards.Count
        };
        
        // Oblicz odchylenie standardowe
        double variance = rewards.Sum(r => Math.Pow(r - stats.MeanReward, 2)) / rewards.Count;
        stats.StdDevReward = Math.Sqrt(variance);
        
        // Oblicz średnią liczbę kroków na epizod
        var episodeSteps = entries
            .GroupBy(e => e.EpisodeId)
            .Select(g => g.Count())
            .ToList();
        
        stats.AverageStepsPerEpisode = episodeSteps.Any() ? episodeSteps.Average() : 0;
        
        _logger?.Info($"CrosswordRLDatasetGenerator: Statistics - Entries: {stats.TotalEntries}, Episodes: {stats.UniqueEpisodes}, Mean Reward: {stats.MeanReward:F2}, Terminal: {stats.TerminalStates}");
        
        return stats;
    }
}
