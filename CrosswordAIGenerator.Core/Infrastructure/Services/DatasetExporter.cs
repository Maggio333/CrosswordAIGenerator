using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Eksporter datasetów do plików
/// </summary>
public class DatasetExporter : IDatasetExporter
{
    private readonly IDatasetPromptGenerator _promptGenerator;

    public DatasetExporter(IDatasetPromptGenerator promptGenerator)
    {
        _promptGenerator = promptGenerator ?? throw new ArgumentNullException(nameof(promptGenerator));
    }

    public void SaveDatasetToFile(List<DatasetEntry> entries, string filePath, DatasetSettings? settings = null)
    {
        // Jeśli nie podano settings, eksportuj wszystko (domyślne zachowanie)
        // Uwaga: IncludeScreenshot nie jest obecnie obsługiwane w DatasetEntry - screenshot jest generowany osobno
        settings ??= new DatasetSettings { IncludeXaml = true, IncludeEmptyXaml = true, IncludeCrossGrid = true, IncludeScreenshot = false, IncludeDescription = true, IncludeSearchableText = true, IncludeEmbeddingText = true };
        
        // Utwórz kopie entries z wyfiltrowanymi polami
        var filteredEntries = entries.Select(entry => new DatasetEntry
        {
            Id = entry.Id,
            Type = entry.Type,
            GridSize = entry.GridSize,
            HasWalls = entry.HasWalls,
            Xaml = settings.IncludeXaml ? entry.Xaml : string.Empty,
            EmptyXaml = settings.IncludeEmptyXaml ? entry.EmptyXaml : null,
            CrossGrid = settings.IncludeCrossGrid ? entry.CrossGrid : null,
            Description = settings.IncludeDescription ? entry.Description : string.Empty,
            SearchableText = settings.IncludeSearchableText ? entry.SearchableText : string.Empty,
            Metadata = entry.Metadata,
            RagMetadata = settings.IncludeEmbeddingText ? entry.RagMetadata : null
        }).ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(filteredEntries, options);
        File.WriteAllText(filePath, json);
    }

    public void ExportToFinetuneJsonl(List<DatasetEntry> entries, string filePath)
    {
        var jsonLines = new List<string>();
        
        foreach (var entry in entries)
        {
            // Pomiń wpisy bez CrossGrid lub bez słów
            if (string.IsNullOrWhiteSpace(entry.CrossGrid) || entry.Type == "empty_grid")
            {
                continue;
            }
            
            // Wygeneruj prompt z informacji o krzyżówce
            string prompt = _promptGenerator.GenerateFinetunePrompt(entry);
            
            // Response to CrossGrid (z sekcją # GRID)
            string response = entry.CrossGrid.Trim();
            
            // Usuń numerki z response (np. [1]P -> P, [2]O -> O) - tylko w datasetcie do treningu
            // Numerki są używane do oznaczenia highlighted cells, ale model ma się uczyć bez nich
            response = Regex.Replace(response, @"\[\d+\]", "");
            
            // Upewnij się, że response zaczyna się od # GRID
            if (!response.StartsWith("# GRID"))
            {
                response = "# GRID\n" + response;
            }
            
            // Normalizuj znaki nowej linii do \n
            prompt = prompt.Replace("\r\n", "\n").Replace("\r", "\n");
            response = response.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Utwórz obiekt JSON
            var jsonObject = new
            {
                prompt = prompt,
                response = response
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
        
        // Zapisz jako UTF-8 bez BOM (wymagane dla finetunowania)
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllLines(filePath, jsonLines, utf8NoBom);
    }
}

