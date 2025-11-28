namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Pojedynczy wpis w datasecie
/// </summary>
public class DatasetEntry
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GridSize { get; set; } = string.Empty;
    public bool HasWalls { get; set; }
    public string Xaml { get; set; } = string.Empty;
    
    /// <summary>
    /// Pusta wersja XAML (bez liter, tylko ramki i definicje) - do wypełnienia ręcznie
    /// </summary>
    public string? EmptyXaml { get; set; }
    
    /// <summary>
    /// Format CrossGrid - prosty tekstowy format ASCII art dla LLM
    /// Format: # GRID\nR0: ....[1]P..H.......R..\nR1: ....[2]O..I.P.....O..\n...
    /// </summary>
    public string? CrossGrid { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public DatasetMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Tekst do embeddingu dla RAG - kombinacja XAML, opisu i metadanych
    /// </summary>
    public string SearchableText { get; set; } = string.Empty;
    
    /// <summary>
    /// Metadane dla RAG (embedding, kategoria, timestamp)
    /// </summary>
    public RagMetadata? RagMetadata { get; set; }
}

/// <summary>
/// Metadane dla wpisu w datasecie
/// </summary>
public class DatasetMetadata
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int WallCount { get; set; }
    public int EmptyCellCount { get; set; }
    public int LetterCount { get; set; }
}

/// <summary>
/// Metadane dla RAG (embedding, kategoria, timestamp)
/// </summary>
public class RagMetadata
{
    /// <summary>
    /// Tekst używany do tworzenia embeddingu
    /// </summary>
    public string EmbeddingText { get; set; } = string.Empty;
    
    /// <summary>
    /// Kategoria dla organizacji w RAG
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp utworzenia
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

