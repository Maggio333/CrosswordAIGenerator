using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora promptów do finetunowania
/// </summary>
public interface IDatasetPromptGenerator
{
    /// <summary>
    /// Generuje prompt dla finetunowania na podstawie DatasetEntry
    /// </summary>
    string GenerateFinetunePrompt(DatasetEntry entry);

    /// <summary>
    /// Wyciąga hasło główne z DatasetEntry
    /// </summary>
    string? ExtractHighlightedWord(DatasetEntry entry);

    /// <summary>
    /// Wyciąga słowa z kierunkami z DatasetEntry
    /// </summary>
    List<(string word, string direction)> ExtractWordsWithDirections(DatasetEntry entry);
}

