using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Models.RL;

/// <summary>
/// Akcja w środowisku RL - umieszczenie słowa w krzyżówce
/// </summary>
public class CrosswordRLAction
{
    /// <summary>
    /// Słowo do umieszczenia
    /// </summary>
    public string Word { get; set; } = string.Empty;
    
    /// <summary>
    /// Wiersz początkowy (0-based)
    /// </summary>
    public int Row { get; set; }
    
    /// <summary>
    /// Kolumna początkowa (0-based)
    /// </summary>
    public int Column { get; set; }
    
    /// <summary>
    /// Kierunek umieszczenia słowa
    /// </summary>
    public WordDirection Direction { get; set; }
    
    public CrosswordRLAction()
    {
    }
    
    public CrosswordRLAction(string word, int row, int column, WordDirection direction)
    {
        Word = word ?? throw new ArgumentNullException(nameof(word));
        Row = row;
        Column = column;
        Direction = direction;
    }
    
    /// <summary>
    /// Konwertuje akcję na CrosswordWord
    /// </summary>
    public CrosswordWord ToCrosswordWord(int id, string clue = "")
    {
        return new CrosswordWord(id, Word, Row, Column, Direction, clue);
    }
    
    public override string ToString()
    {
        return $"{Word} at ({Row}, {Column}) {Direction}";
    }
}
