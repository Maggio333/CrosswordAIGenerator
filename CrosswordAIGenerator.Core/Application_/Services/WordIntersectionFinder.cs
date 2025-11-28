using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Klasa pomocnicza do znajdowania przecięć między słowami w krzyżówce
/// </summary>
public static class WordIntersectionFinder
{
    /// <summary>
    /// Znajduje wszystkie przecięcia między słowami w krzyżówce
    /// </summary>
    public static List<WordIntersection> FindIntersections(List<CrosswordWord> placedWords)
    {
        var intersections = new List<WordIntersection>();
        
        for (int i = 0; i < placedWords.Count; i++)
        {
            for (int j = i + 1; j < placedWords.Count; j++)
            {
                var word1 = placedWords[i];
                var word2 = placedWords[j];
                
                // Sprawdź czy słowa się przecinają (muszą być prostopadłe)
                if (word1.Direction == word2.Direction)
                    continue; // Równoległe słowa nie mogą się przecinać
                
                var word1Positions = word1.GetCellPositions().ToList();
                var word2Positions = word2.GetCellPositions().ToList();
                
                // Znajdź wspólne pozycje (przecięcia)
                var commonPositions = word1Positions.Intersect(word2Positions).ToList();
                
                foreach (var (row, col) in commonPositions)
                {
                    // Znajdź literę w obu słowach
                    int letterIndex1 = word1.IsHorizontal 
                        ? col - word1.Column 
                        : row - word1.Row;
                    int letterIndex2 = word2.IsHorizontal 
                        ? col - word2.Column 
                        : row - word2.Row;
                    
                    if (letterIndex1 >= 0 && letterIndex1 < word1.Word.Length &&
                        letterIndex2 >= 0 && letterIndex2 < word2.Word.Length)
                    {
                        char letter1 = word1.Word[letterIndex1];
                        char letter2 = word2.Word[letterIndex2];
                        
                        // W przecięciu litery muszą być takie same
                        if (letter1 == letter2)
                        {
                            intersections.Add(new WordIntersection
                            {
                                Word1 = word1.Word,
                                Word2 = word2.Word,
                                Letter = letter1,
                                Row = row,
                                Column = col,
                                Word1LetterIndex = letterIndex1 + 1, // 1-based dla czytelności
                                Word2LetterIndex = letterIndex2 + 1
                            });
                        }
                    }
                }
            }
        }
        
        return intersections;
    }
}

/// <summary>
/// Reprezentuje przecięcie dwóch słów
/// </summary>
public class WordIntersection
{
    public string Word1 { get; set; } = string.Empty;
    public string Word2 { get; set; } = string.Empty;
    public char Letter { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int Word1LetterIndex { get; set; } // Pozycja litery w pierwszym słowie (1-based)
    public int Word2LetterIndex { get; set; } // Pozycja litery w drugim słowie (1-based)
}

