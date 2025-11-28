using CrosswordAIGenerator.Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Domain.Models;

/// <summary>
/// Testy dla CrosswordGrid
/// </summary>
public class CrosswordGridTests
{
    [Fact]
    public void CrosswordGrid_Powinien_Utworzyc_Siatke_O_Podanym_Rozmiarze()
    {
        // Arrange & Act
        var grid = new CrosswordGrid(10, 15);

        // Assert
        grid.Rows.Should().Be(10);
        grid.Columns.Should().Be(15);
        grid.Cells.Count.Should().Be(150); // 10 * 15
    }

    [Fact]
    public void CrosswordGrid_Powinien_Miec_Wszystkie_Komorki_Jako_Empty()
    {
        // Arrange & Act
        var grid = new CrosswordGrid(5, 5);

        // Assert
        foreach (var cell in grid.Cells.Values)
        {
            cell.Type.Should().Be(CrosswordCellType.Empty);
            cell.IsEmpty.Should().BeTrue();
            cell.HasLetter.Should().BeFalse();
            cell.IsWall.Should().BeFalse();
        }
    }

    [Fact]
    public void GetCell_Powinien_Zwrocic_Poprawna_Komorke()
    {
        // Arrange
        var grid = new CrosswordGrid(10, 10);

        // Act
        var cell = grid.GetCell(5, 7);

        // Assert
        cell.Should().NotBeNull();
        cell!.Type.Should().Be(CrosswordCellType.Empty);
    }

    [Fact]
    public void GetCell_Powinien_Zwrocic_Wall_Dla_Nieprawidlowych_Wspolrzednych()
    {
        // Arrange
        var grid = new CrosswordGrid(10, 10);

        // Act
        var cell1 = grid.GetCell(-1, 5);
        var cell2 = grid.GetCell(5, -1);
        var cell3 = grid.GetCell(10, 5);
        var cell4 = grid.GetCell(5, 10);

        // Assert
        // GetCell zwraca nową komórkę typu Wall dla nieprawidłowych współrzędnych (nie null)
        cell1.Should().NotBeNull();
        cell1!.Type.Should().Be(CrosswordCellType.Wall);
        cell2.Should().NotBeNull();
        cell2!.Type.Should().Be(CrosswordCellType.Wall);
        cell3.Should().NotBeNull();
        cell3!.Type.Should().Be(CrosswordCellType.Wall);
        cell4.Should().NotBeNull();
        cell4!.Type.Should().Be(CrosswordCellType.Wall);
    }

    [Fact]
    public void SetLetter_Powinien_Ustawic_Litere()
    {
        // Arrange
        var grid = new CrosswordGrid(10, 10);

        // Act
        grid.SetLetter(5, 5, 'A');
        var cell = grid.GetCell(5, 5);

        // Assert
        cell.Should().NotBeNull();
        cell!.HasLetter.Should().BeTrue();
        cell.Letter.Should().Be('A');
        cell.Type.Should().Be(CrosswordCellType.Letter);
    }

    [Fact]
    public void SetWall_Powinien_Ustawic_Sciane()
    {
        // Arrange
        var grid = new CrosswordGrid(10, 10);

        // Act
        grid.SetWall(5, 5);
        var cell = grid.GetCell(5, 5);

        // Assert
        cell.Should().NotBeNull();
        cell!.IsWall.Should().BeTrue();
        cell.Type.Should().Be(CrosswordCellType.Wall);
    }
}

