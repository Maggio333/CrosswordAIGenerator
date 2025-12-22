using System.Linq;
using System.Text;
using System.Text.Json;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services.RL;

/// <summary>
/// Eksporter datasetów RL do JSONL
/// </summary>
public class CrosswordRLDatasetExporter
{
    private readonly ICursorLogger? _logger;
    
    public CrosswordRLDatasetExporter(ICursorLogger? logger = null)
    {
        _logger = logger;
    }
    
    public void ExportToJsonl(List<CrosswordRLDatasetEntry> entries, string filePath)
    {
        if (entries == null || entries.Count == 0)
        {
            _logger?.Warning("CrosswordRLDatasetExporter: No entries to export");
            return;
        }
        
        _logger?.Info($"CrosswordRLDatasetExporter: Exporting {entries.Count} entries to {filePath}");
        
        var jsonLines = new List<string>();
        
        foreach (var entry in entries)
        {
            // Utwórz obiekt JSON
            var jsonObject = new
            {
                episode_id = entry.EpisodeId,
                t = entry.T,
                state = entry.State,
                action = new
                {
                    word = entry.Action.Word,
                    row = entry.Action.Row,
                    col = entry.Action.Column,
                    direction = entry.Action.Direction.ToString()
                },
                reward = entry.Reward, // Zawsze skalar - używać tego do RL
                next_state = entry.NextState,
                is_terminal = entry.IsTerminal,
                done = entry.Done, // Alias dla spójności z bibliotekami RL (Gym, etc.)
                is_natural_termination = entry.IsNaturalTermination, // Czy epizod zakończył się naturalnie (nie przez cutoff)
                reward_details = new
                {
                    // reward_details to tylko logi/debug - nie używać do RL
                    total = entry.RewardDetails.TotalReward,
                    completion = entry.RewardDetails.CompletionReward,
                    placement = entry.RewardDetails.PlacementReward,
                    intersection = entry.RewardDetails.IntersectionReward,
                    penalty = entry.RewardDetails.Penalty
                },
                remaining_words = entry.RemainingWords,
                placed_words = entry.PlacedWords.Select(pw => new
                {
                    word = pw.Word,
                    row = pw.Row,
                    col = pw.Column,
                    direction = pw.Direction.ToString(),
                    intersection_count = pw.IntersectionCount
                }).ToList()
            };
            
            // Serializuj do JSON (bez formatowania, jeden wiersz)
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
        
        _logger?.Info($"CrosswordRLDatasetExporter: Exported {entries.Count} entries to {filePath}");
    }
}
