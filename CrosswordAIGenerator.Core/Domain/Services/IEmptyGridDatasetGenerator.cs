using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora datasetów z pustymi siatkami
/// </summary>
public interface IEmptyGridDatasetGenerator
{
    /// <summary>
    /// Generuje pojedynczy przykład pustej siatki
    /// </summary>
    DatasetEntry GenerateEmptyGridExample(int rows, int columns, bool withWalls = false, double wallProbability = 0.1, int? seed = null);

    /// <summary>
    /// Generuje wiele przykładów pustych siatek
    /// </summary>
    List<DatasetEntry> GenerateEmptyGridDataset(
        int count,
        int minSize = 5,
        int maxSize = 15,
        bool includeWithWalls = true,
        double wallProbability = 0.1);
}

