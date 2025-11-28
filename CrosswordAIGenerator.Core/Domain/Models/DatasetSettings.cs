namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Ustawienia kontrolujące które elementy mają być zawarte w DatasetEntry
/// </summary>
public class DatasetSettings
{
    /// <summary>
    /// Czy zawierać XAML (pełna wersja z literami)
    /// </summary>
    public bool IncludeXaml { get; set; } = true;

    /// <summary>
    /// Czy zawierać pustą wersję XAML (bez liter, tylko ramki i definicje)
    /// </summary>
    public bool IncludeEmptyXaml { get; set; } = true;

    /// <summary>
    /// Czy zawierać format CrossGrid (ASCII art)
    /// </summary>
    public bool IncludeCrossGrid { get; set; } = true;

    /// <summary>
    /// Czy zawierać screenshot (obraz JPG)
    /// </summary>
    public bool IncludeScreenshot { get; set; } = false;

    /// <summary>
    /// Czy zawierać opis tekstowy
    /// </summary>
    public bool IncludeDescription { get; set; } = true;

    /// <summary>
    /// Czy zawierać SearchableText (tekst do wyszukiwania)
    /// </summary>
    public bool IncludeSearchableText { get; set; } = true;

    /// <summary>
    /// Czy zawierać EmbeddingText (tekst do embeddingu dla RAG)
    /// </summary>
    public bool IncludeEmbeddingText { get; set; } = true;
}

