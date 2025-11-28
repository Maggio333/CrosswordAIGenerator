using CrosswordAIGenerator.Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Domain.Models;

/// <summary>
/// Testy dla CrosswordWord
/// </summary>
public class CrosswordWordTests
{
    [Fact]
    public void CrosswordWord_Powinien_Utworzyc_Slowo()
    {
        // Arrange & Act
        var word = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Across);

        // Assert
        word.Id.Should().Be(1);
        word.Word.Should().Be("TEST");
        word.Row.Should().Be(5);
        word.Column.Should().Be(10);
        word.Direction.Should().Be(WordDirection.Across);
        word.Length.Should().Be(4);
    }

    [Fact]
    public void GetCellPositions_Powinien_Zwrocic_Pozycje_Dla_Across()
    {
        // Arrange
        var word = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Across);

        // Act
        var positions = word.GetCellPositions().ToList();

        // Assert
        positions.Should().HaveCount(4);
        positions[0].Should().Be((5, 10));
        positions[1].Should().Be((5, 11));
        positions[2].Should().Be((5, 12));
        positions[3].Should().Be((5, 13));
    }

    [Fact]
    public void GetCellPositions_Powinien_Zwrocic_Pozycje_Dla_Down()
    {
        // Arrange
        var word = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Down);

        // Act
        var positions = word.GetCellPositions().ToList();

        // Assert
        positions.Should().HaveCount(4);
        positions[0].Should().Be((5, 10));
        positions[1].Should().Be((6, 10));
        positions[2].Should().Be((7, 10));
        positions[3].Should().Be((8, 10));
    }

    [Fact]
    public void GetCellPositions_Powinien_Zawierac_Wszystkie_Pozycje_Slowa()
    {
        // Arrange
        var word = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Across);

        // Act
        var positions = word.GetCellPositions().ToHashSet();

        // Assert
        positions.Should().Contain((5, 10));
        positions.Should().Contain((5, 11));
        positions.Should().Contain((5, 12));
        positions.Should().Contain((5, 13));
        positions.Should().NotContain((5, 14));
        positions.Should().NotContain((6, 10));
    }

    [Fact]
    public void IntersectsWith_Powinien_Zwrocic_True_Dla_Przecinajacych_Sie_Slow()
    {
        // Arrange
        var word1 = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Across);
        var word2 = new CrosswordWord(2, "TEXT", 4, 12, WordDirection.Down);

        // Act
        var intersects = word1.IntersectsWith(word2);

        // Assert
        intersects.Should().BeTrue(); // "TEST" (5,10) Across i "TEXT" (4,12) Down przecinają się w (5,12) - litera 'T'
    }

    [Fact]
    public void IntersectsWith_Powinien_Zwrocic_False_Dla_Nieprzecinajacych_Sie_Slow()
    {
        // Arrange
        var word1 = new CrosswordWord(1, "TEST", 5, 10, WordDirection.Across);
        var word2 = new CrosswordWord(2, "WORD", 10, 10, WordDirection.Across);

        // Act
        var intersects = word1.IntersectsWith(word2);

        // Assert
        intersects.Should().BeFalse();
    }
}

