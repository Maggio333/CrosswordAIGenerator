using System.Text;
using System.Text.RegularExpressions;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator promptów do finetunowania
/// </summary>
public class DatasetPromptGenerator : IDatasetPromptGenerator
{
    public string GenerateFinetunePrompt(DatasetEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ułóż polską krzyżówkę jako CrossGrid.");
        sb.AppendLine($"Rozmiar: {entry.GridSize}");
        
        // Wyciągnij hasło główne z Description lub EmbeddingText
        string? highlightedWord = ExtractHighlightedWord(entry);
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.AppendLine($"Hasło główne: {highlightedWord}");
        }
        
        // Wyciągnij słowa z kierunkami z EmbeddingText
        var wordsWithDirections = ExtractWordsWithDirections(entry);
        if (wordsWithDirections.Any())
        {
            sb.AppendLine("Słowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:");
            foreach (var (word, direction) in wordsWithDirections)
            {
                // Usuń numerki z słowa (np. "WORD[1]" -> "WORD") - tylko w promptcie, nie w response
                string cleanWord = Regex.Replace(word, @"\[\d+\]", "");
                sb.AppendLine($"- {cleanWord} ({direction})");
            }
        }
        
        sb.AppendLine("Zwróć tylko sekcję # GRID.");
        
        return sb.ToString().TrimEnd(); // Usuń końcowy znak nowej linii
    }

    public string? ExtractHighlightedWord(DatasetEntry entry)
    {
        // Szukaj w Description: "Hasło główne: WORD"
        var match = Regex.Match(entry.Description, @"Hasło główne:\s*([A-ZĄĆĘŁŃÓŚŹŻ]+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // Szukaj w EmbeddingText: "Hasło główne: WORD"
        if (entry.RagMetadata != null)
        {
            match = Regex.Match(entry.RagMetadata.EmbeddingText, @"Hasło główne:\s*([A-ZĄĆĘŁŃÓŚŹŻ]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        
        return null;
    }

    public List<(string word, string direction)> ExtractWordsWithDirections(DatasetEntry entry)
    {
        var words = new List<(string, string)>();
        
        // Szukaj w EmbeddingText: "WORD (Direction)" lub "WORD[number] (Direction)"
        if (entry.RagMetadata != null)
        {
            var matches = Regex.Matches(
                entry.RagMetadata.EmbeddingText, 
                @"([A-ZĄĆĘŁŃÓŚŹŻ]+)(?:\[\d+\])?\s*\((\w+)\)");
            
            foreach (Match match in matches)
            {
                string word = match.Groups[1].Value;
                string direction = match.Groups[2].Value;
                
                // Usuń numerki z końca słowa (jeśli są) - dodatkowe zabezpieczenie
                word = Regex.Replace(word, @"\[\d+\]$", "");
                
                // Konwertuj kierunek na polski format
                if (direction.Equals("Across", StringComparison.OrdinalIgnoreCase))
                    direction = "Across";
                else if (direction.Equals("Down", StringComparison.OrdinalIgnoreCase))
                    direction = "Down";
                
                words.Add((word, direction));
            }
        }
        
        return words;
    }
}

