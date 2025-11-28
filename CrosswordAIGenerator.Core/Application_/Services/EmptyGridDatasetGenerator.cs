using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Common;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator datasetów z pustymi siatkami
/// </summary>
public class EmptyGridDatasetGenerator : IEmptyGridDatasetGenerator
{
    private readonly IEmptyGridGenerator _gridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly IDatasetDescriptionGenerator _descriptionGenerator;
    private readonly Random _random;

    public EmptyGridDatasetGenerator(
        IEmptyGridGenerator gridGenerator,
        IXamlGenerator xamlGenerator,
        IDatasetDescriptionGenerator descriptionGenerator,
        Random? random = null)
    {
        _gridGenerator = gridGenerator ?? throw new ArgumentNullException(nameof(gridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _descriptionGenerator = descriptionGenerator ?? throw new ArgumentNullException(nameof(descriptionGenerator));
        _random = random ?? new Random();
    }

    public DatasetEntry GenerateEmptyGridExample(int rows, int columns, bool withWalls = false, double wallProbability = Constants.DefaultWallProbability, int? seed = null)
    {
        // Uwaga: parametr seed nie jest obecnie obsługiwany - IEmptyGridGenerator nie przyjmuje seed
        // Jeśli potrzebny jest seed, można rozważyć utworzenie IEmptyGridGeneratorFactory
        // Na razie używamy wstrzykiwanego gridGenerator
        var gridGenerator = _gridGenerator;
        
        CrosswordGrid grid;
        int wallCount = 0;
        
        if (withWalls)
        {
            grid = gridGenerator.GenerateEmptyGridWithWalls(rows, columns, wallProbability);
            wallCount = grid.Cells.Values.Count(c => c.IsWall);
        }
        else
        {
            grid = gridGenerator.GenerateEmptyGrid(rows, columns);
        }

        var xaml = _xamlGenerator.GenerateXaml(grid, Constants.DefaultXamlWidth, Constants.DefaultXamlHeight, null, null);
        var description = _descriptionGenerator.GenerateDescription(grid, withWalls, wallCount);
        var searchableText = _descriptionGenerator.GenerateSearchableText(grid, rows, columns, withWalls, wallCount, xaml);
        var embeddingText = _descriptionGenerator.GenerateEmbeddingText(grid, rows, columns, withWalls, wallCount);

        var entry = new DatasetEntry
        {
            Id = GenerateId("empty_grid", rows, columns, withWalls),
            Type = "empty_grid",
            GridSize = $"{rows}x{columns}",
            HasWalls = withWalls,
            Xaml = xaml,
            Description = description,
            SearchableText = searchableText,
            Metadata = new DatasetMetadata
            {
                Rows = rows,
                Columns = columns,
                WallCount = wallCount,
                EmptyCellCount = grid.Cells.Values.Count(c => c.IsEmpty),
                LetterCount = grid.Cells.Values.Count(c => c.HasLetter)
            },
            RagMetadata = new RagMetadata
            {
                EmbeddingText = embeddingText,
                Category = "crossword_empty_grid",
                Timestamp = DateTime.UtcNow
            }
        };
        
        return entry;
    }

    public List<DatasetEntry> GenerateEmptyGridDataset(
        int count,
        int minSize = Constants.MinDatasetSize,
        int maxSize = Constants.MaxDatasetSize,
        bool includeWithWalls = true,
        double wallProbability = Constants.DefaultWallProbability)
    {
        var results = new List<DatasetEntry>();

        for (int i = 0; i < count; i++)
        {
            int rows = _random.Next(minSize, maxSize + 1);
            int cols = _random.Next(minSize, maxSize + 1);
            bool withWalls = includeWithWalls && _random.NextDouble() < 0.5;

            var entry = GenerateEmptyGridExample(rows, cols, withWalls, wallProbability, _random.Next());
            results.Add(entry);
        }

        return results;
    }

    private string GenerateId(string type, int rows, int cols, bool withWalls)
    {
        var wallSuffix = withWalls ? "_walls" : "";
        return $"{type}_{rows}x{cols}{wallSuffix}_{Guid.NewGuid():N}";
    }
}

