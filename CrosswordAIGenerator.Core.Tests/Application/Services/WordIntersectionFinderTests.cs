using CrosswordAIGenerator.Core.Application.Services;
using CrosswordAIGenerator.Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Application.Services;

/// <summary>
/// Testy dla WordIntersectionFinder
/// </summary>
public class WordIntersectionFinderTests
{
    [Fact]
    public void FindIntersections_Powinien_Znalezc_Przeciecia()
    {
        // Arrange
        var word1 = new CrosswordWord(1, "TEST", 5, 5, WordDirection.Across);
        var word2 = new CrosswordWord(2, "TEXT", 4, 6, WordDirection.Down);
        var words = new List<CrosswordWord> { word1, word2 };

        // Act
        var intersections = WordIntersectionFinder.FindIntersections(words);

        // Assert
        intersections.Should().NotBeEmpty();
        // "TEST" (5,5) Across i "TEXT" (4,6) Down przecinają się w (5,6) - litera 'T' w obu
    }

    [Fact]
    public void FindIntersections_Powinien_Zwrocic_Pusta_Liste_Dla_Brak_Przeciec()
    {
        // Arrange
        var word1 = new CrosswordWord(1, "TEST", 5, 5, WordDirection.Across);
        var word2 = new CrosswordWord(2, "WORD", 10, 10, WordDirection.Across);
        var words = new List<CrosswordWord> { word1, word2 };

        // Act
        var intersections = WordIntersectionFinder.FindIntersections(words);

        // Assert
        intersections.Should().BeEmpty();
    }

    [Fact]
    public void FindIntersections_Powinien_Zwrocic_Pusta_Liste_Dla_Pustej_Listy()
    {
        // Arrange
        var words = new List<CrosswordWord>();

        // Act
        var intersections = WordIntersectionFinder.FindIntersections(words);

        // Assert
        intersections.Should().BeEmpty();
    }
}

