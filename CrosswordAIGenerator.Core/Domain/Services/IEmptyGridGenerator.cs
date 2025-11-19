using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora pustych siatek krzyżówek
/// </summary>
public interface IEmptyGridGenerator
{
    /// <summary>
    /// Generuje pustą siatkę bez ścian
    /// </summary>
    CrosswordGrid GenerateEmptyGrid(int rows, int columns);

    /// <summary>
    /// Generuje pustą siatkę z losowymi ścianami
    /// </summary>
    CrosswordGrid GenerateEmptyGridWithWalls(int rows, int columns, double wallProbability = 0.1);

    /// <summary>
    /// Generuje pustą siatkę z określoną liczbą ścian
    /// </summary>
    CrosswordGrid GenerateEmptyGridWithWallCount(int rows, int columns, int wallCount);
}

