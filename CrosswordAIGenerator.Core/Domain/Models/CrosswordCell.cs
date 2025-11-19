namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Reprezentuje pojedynczą kratkę w siatce krzyżówki
/// </summary>
public enum CrosswordCellType
{
    /// <summary>
    /// Pusta kratka (można wpisać literę)
    /// </summary>
    Empty,
    
    /// <summary>
    /// Kratka z literą
    /// </summary>
    Letter,
    
    /// <summary>
    /// Ściana (czarna kratka, nie można wpisać litery)
    /// </summary>
    Wall
}

/// <summary>
/// Reprezentuje pojedynczą kratkę w siatce krzyżówki
/// </summary>
public class CrosswordCell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public CrosswordCellType Type { get; set; }
    public char? Letter { get; set; }
    
    public CrosswordCell(int row, int column, CrosswordCellType type = CrosswordCellType.Empty, char? letter = null)
    {
        Row = row;
        Column = column;
        Type = type;
        Letter = letter;
    }
    
    public bool IsEmpty => Type == CrosswordCellType.Empty;
    public bool HasLetter => Type == CrosswordCellType.Letter && Letter.HasValue;
    public bool IsWall => Type == CrosswordCellType.Wall;
}

