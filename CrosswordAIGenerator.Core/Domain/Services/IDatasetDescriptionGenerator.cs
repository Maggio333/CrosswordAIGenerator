using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora opisów, searchable text i embedding text dla datasetów
/// </summary>
public interface IDatasetDescriptionGenerator
{
    /// <summary>
    /// Generuje opis dla pustej siatki
    /// </summary>
    string GenerateDescription(CrosswordGrid grid, bool hasWalls, int wallCount);

    /// <summary>
    /// Generuje searchable text dla pustej siatki
    /// </summary>
    string GenerateSearchableText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount, string xaml);

    /// <summary>
    /// Generuje embedding text dla pustej siatki
    /// </summary>
    string GenerateEmbeddingText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount);

    /// <summary>
    /// Generuje opis dla krzyżówki ze słowami
    /// </summary>
    string GenerateWordsDescription(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord);

    /// <summary>
    /// Generuje searchable text dla krzyżówki ze słowami
    /// </summary>
    string GenerateWordsSearchableText(CrosswordGrid grid, int rows, int cols, int letterCount, string xaml, List<CrosswordWord> placedWords, string? highlightedWord);

    /// <summary>
    /// Generuje embedding text dla krzyżówki ze słowami
    /// </summary>
    string GenerateWordsEmbeddingText(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord);

    /// <summary>
    /// Generuje opis dla krzyżówki z własnymi słowami
    /// </summary>
    string GenerateCustomWordsDescription(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords);

    /// <summary>
    /// Generuje searchable text dla krzyżówki z własnymi słowami
    /// </summary>
    string GenerateSearchableTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns, string xaml);

    /// <summary>
    /// Generuje embedding text dla krzyżówki z własnymi słowami
    /// </summary>
    string GenerateEmbeddingTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns);
}

