using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Infrastructure.Services;

/// <summary>
/// Testy dla EmptyGridGenerator
/// </summary>
public class EmptyGridGeneratorTests
{
    [Fact]
    public void GenerateEmptyGrid_Powinien_Utworzyc_Pusta_Siatke()
    {
        // Arrange
        var generator = new EmptyGridGenerator();

        // Act
        var grid = generator.GenerateEmptyGrid(10, 15);

        // Assert
        grid.Should().NotBeNull();
        grid.Rows.Should().Be(10);
        grid.Columns.Should().Be(15);
        
        // Wszystkie komórki powinny być puste
        foreach (var cell in grid.Cells.Values)
        {
            cell.IsEmpty.Should().BeTrue();
            cell.IsWall.Should().BeFalse();
        }
    }

    [Fact]
    public void GenerateEmptyGridWithWalls_Powinien_Utworzyc_Siatke_Ze_Scianami()
    {
        // Arrange
        var generator = new EmptyGridGenerator(seed: 12345);
        double wallProbability = 0.3;

        // Act
        var grid = generator.GenerateEmptyGridWithWalls(10, 10, wallProbability);

        // Assert
        grid.Should().NotBeNull();
        grid.Rows.Should().Be(10);
        grid.Columns.Should().Be(10);
        
        // Powinny być jakieś ściany (z prawdopodobieństwem 0.3)
        var wallCount = grid.Cells.Values.Count(c => c.IsWall);
        wallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateEmptyGridWithWalls_Powinien_Obslugiwac_Zero_Prawdopodobienstwa()
    {
        // Arrange
        var generator = new EmptyGridGenerator(seed: 12345);

        // Act
        var grid = generator.GenerateEmptyGridWithWalls(10, 10, 0.0);

        // Assert
        grid.Should().NotBeNull();
        var wallCount = grid.Cells.Values.Count(c => c.IsWall);
        wallCount.Should().Be(0);
    }

    [Fact]
    public void GenerateEmptyGrid_Powinien_Rzucic_Wyjatek_Dla_Nieprawidlowych_Parametrow()
    {
        // Arrange
        var generator = new EmptyGridGenerator();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => generator.GenerateEmptyGrid(0, 10));
        Assert.Throws<ArgumentException>(() => generator.GenerateEmptyGrid(10, 0));
        Assert.Throws<ArgumentException>(() => generator.GenerateEmptyGrid(-1, 10));
    }
}

