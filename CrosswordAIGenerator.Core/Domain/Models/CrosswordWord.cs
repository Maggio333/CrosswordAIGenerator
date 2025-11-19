namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Kierunek słowa w krzyżówce
/// </summary>
public enum WordDirection
{
    /// <summary>
    /// Poziome (w prawo)
    /// </summary>
    Across,
    
    /// <summary>
    /// Pionowe (w dół)
    /// </summary>
    Down
}

/// <summary>
/// Reprezentuje słowo w krzyżówce
/// </summary>
public class CrosswordWord
{
    public int Id { get; set; }
    public string Word { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public WordDirection Direction { get; set; }
    public string Clue { get; set; }
    
    public CrosswordWord(int id, string word, int row, int column, WordDirection direction, string clue = "")
    {
        Id = id;
        Word = word;
        Row = row;
        Column = column;
        Direction = direction;
        Clue = clue;
    }
    
    public int Length => Word?.Length ?? 0;
    
    public bool IsHorizontal => Direction == WordDirection.Across;
    public bool IsVertical => Direction == WordDirection.Down;
    
    /// <summary>
    /// Zwraca pozycje wszystkich kratek zajmowanych przez to słowo
    /// </summary>
    public IEnumerable<(int row, int col)> GetCellPositions()
    {
        for (int i = 0; i < Length; i++)
        {
            if (IsHorizontal)
            {
                yield return (Row, Column + i);
            }
            else
            {
                yield return (Row + i, Column);
            }
        }
    }
    
    /// <summary>
    /// Sprawdza czy słowo koliduje z innym słowem
    /// </summary>
    public bool IntersectsWith(CrosswordWord other)
    {
        var thisPositions = GetCellPositions().ToHashSet();
        var otherPositions = other.GetCellPositions().ToHashSet();
        return thisPositions.Intersect(otherPositions).Any();
    }
}

