namespace CrosswordAIGenerator.Core.Domain.Models;

/// <summary>
/// Reprezentuje definicję (wskazówkę) dla słowa w krzyżówce
/// </summary>
public class CrosswordClue
{
    public int WordId { get; set; }
    public string ClueText { get; set; }
    public WordDirection Direction { get; set; }
    
    public CrosswordClue(int wordId, string clueText, WordDirection direction)
    {
        WordId = wordId;
        ClueText = clueText;
        Direction = direction;
    }
}

