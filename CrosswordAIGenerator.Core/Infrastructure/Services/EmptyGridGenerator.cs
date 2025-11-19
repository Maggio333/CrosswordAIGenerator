using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Generator pustych siatek krzyżówek (bez liter, opcjonalnie z losowymi ścianami)
/// Implementacja infrastruktury dla IEmptyGridGenerator
/// </summary>
public class EmptyGridGenerator : IEmptyGridGenerator
{
    private readonly Random _random;

    public EmptyGridGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Generuje pustą siatkę bez ścian
    /// </summary>
    public CrosswordGrid GenerateEmptyGrid(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
        {
            throw new ArgumentException("Rows and columns must be greater than 0");
        }

        var grid = new CrosswordGrid(rows, columns);
        
        // Wszystkie kratki są już puste (Empty) po inicjalizacji w konstruktorze CrosswordGrid
        // Nie trzeba nic zmieniać - wszystkie są już ustawione na Empty
        
        return grid;
    }

    /// <summary>
    /// Generuje pustą siatkę z losowymi ścianami
    /// </summary>
    /// <param name="rows">Liczba wierszy</param>
    /// <param name="columns">Liczba kolumn</param>
    /// <param name="wallProbability">Prawdopodobieństwo że kratka będzie ścianą (0.0 - 1.0)</param>
    public CrosswordGrid GenerateEmptyGridWithWalls(int rows, int columns, double wallProbability = 0.1)
    {
        if (rows <= 0 || columns <= 0)
        {
            throw new ArgumentException("Rows and columns must be greater than 0");
        }

        if (wallProbability < 0 || wallProbability > 1)
        {
            throw new ArgumentException("Wall probability must be between 0.0 and 1.0");
        }

        var grid = new CrosswordGrid(rows, columns);
        
        // Losowo ustaw ściany
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (_random.NextDouble() < wallProbability)
                {
                    grid.SetWall(r, c);
                }
                // Pozostałe kratki pozostają puste (Empty)
            }
        }
        
        return grid;
    }

    /// <summary>
    /// Generuje pustą siatkę z określoną liczbą ścian
    /// </summary>
    public CrosswordGrid GenerateEmptyGridWithWallCount(int rows, int columns, int wallCount)
    {
        if (rows <= 0 || columns <= 0)
        {
            throw new ArgumentException("Rows and columns must be greater than 0");
        }

        int totalCells = rows * columns;
        if (wallCount < 0 || wallCount > totalCells)
        {
            throw new ArgumentException($"Wall count must be between 0 and {totalCells}");
        }

        var grid = new CrosswordGrid(rows, columns);
        
        // Losowo wybierz pozycje dla ścian
        var positions = new List<(int row, int col)>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                positions.Add((r, c));
            }
        }
        
        // Wymieszaj i wybierz pierwsze wallCount pozycji
        var shuffled = positions.OrderBy(x => _random.Next()).Take(wallCount);
        
        foreach (var (row, col) in shuffled)
        {
            grid.SetWall(row, col);
        }
        
        return grid;
    }
}

