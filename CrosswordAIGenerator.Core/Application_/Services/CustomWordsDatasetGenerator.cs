using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Common;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator datasetów z krzyżówkami z własnymi słowami
/// </summary>
public class CustomWordsDatasetGenerator : ICustomWordsDatasetGenerator
{
    private readonly CrosswordWordPlacer _wordPlacer;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly ICrossGridGenerator? _crossGridGenerator;
    private readonly IDatasetDescriptionGenerator _descriptionGenerator;
    private readonly ICursorLogger? _logger;
    private readonly Random _random;

    public CustomWordsDatasetGenerator(
        CrosswordWordPlacer wordPlacer,
        IXamlGenerator xamlGenerator,
        IDatasetDescriptionGenerator descriptionGenerator,
        ICrossGridGenerator? crossGridGenerator = null,
        ICursorLogger? logger = null,
        Random? random = null)
    {
        _wordPlacer = wordPlacer ?? throw new ArgumentNullException(nameof(wordPlacer));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _descriptionGenerator = descriptionGenerator ?? throw new ArgumentNullException(nameof(descriptionGenerator));
        _crossGridGenerator = crossGridGenerator;
        _logger = logger;
        _random = random ?? new Random();
    }

    public Result<DatasetEntry, string> GenerateWithCustomWords(
        int rows, 
        int columns, 
        string highlightedWord, 
        List<string> customWords, 
        int minWordsCount = 0, 
        Dictionary<string, string>? wordDefinitions = null)
    {
        if (string.IsNullOrWhiteSpace(highlightedWord))
        {
            return Result<DatasetEntry, string>.Failure("Hasło główne nie może być puste.");
        }

        if (customWords == null || customWords.Count == 0)
        {
            return Result<DatasetEntry, string>.Failure("Lista słów nie może być pusta.");
        }

        var result = _wordPlacer.GenerateWithCustomWords(rows, columns, highlightedWord, customWords, minWordsCount: minWordsCount);

        if (result.IsFailure)
        {
            return Result<DatasetEntry, string>.Failure(result.Error!);
        }

        var (grid, placedWords, highlightedCellsWithIndices) = result.Value;

        // Zawsze generuj wszystkie elementy (Settings kontrolują tylko eksport)
        string xaml = _xamlGenerator.GenerateXaml(grid, width: Constants.DefaultXamlWidth, height: Constants.DefaultXamlHeight, highlightedCellsWithIndices, placedWords, wordDefinitions);
        string? emptyXaml = _xamlGenerator.GenerateEmptyXaml(grid, width: Constants.DefaultXamlWidth, height: Constants.DefaultXamlHeight, highlightedCellsWithIndices, placedWords, wordDefinitions);
        string? crossGrid = _crossGridGenerator?.GenerateCrossGrid(grid, highlightedCellsWithIndices, placedWords);
        
        string description = _descriptionGenerator.GenerateCustomWordsDescription(highlightedWord, customWords, placedWords);
        string searchableText = _descriptionGenerator.GenerateSearchableTextForCustomWords(highlightedWord, customWords, placedWords, rows, columns, xaml);
        string embeddingText = _descriptionGenerator.GenerateEmbeddingTextForCustomWords(highlightedWord, customWords, placedWords, rows, columns);

        var entry = new DatasetEntry
        {
            Id = GenerateId("custom_words", rows, columns, false),
            Type = "custom_words",
            GridSize = $"{rows}x{columns}",
            HasWalls = false,
            Xaml = xaml,
            EmptyXaml = emptyXaml,
            CrossGrid = crossGrid,
            Description = description,
            SearchableText = searchableText,
            Metadata = new DatasetMetadata
            {
                Rows = rows,
                Columns = columns,
                WallCount = 0,
                EmptyCellCount = grid.Cells.Values.Count(c => c.IsEmpty),
                LetterCount = grid.Cells.Values.Count(c => c.HasLetter)
            },
            RagMetadata = new RagMetadata
            {
                EmbeddingText = embeddingText,
                Category = "custom_words",
                Timestamp = DateTime.UtcNow
            }
        };

        return Result<DatasetEntry, string>.Success(entry);
    }

    public List<DatasetEntry> GenerateCustomWordsDataset(
        int count,
        int rows,
        int columns,
        string highlightedWord,
        List<string> customWords,
        int minWordsCount = 0,
        Action<int, int>? onProgress = null,
        Dictionary<string, string>? wordDefinitions = null)
    {
        _logger?.InfoFormat("GenerateCustomWordsDataset: Rozpoczynam generowanie {0} krzyżówek. Hasło: '{1}', Rozmiar: {2}x{3}, Słowa: {4}, MinWordsCount: {5}", 
            count, highlightedWord, rows, columns, string.Join(", ", customWords), minWordsCount);
        
        var results = new List<DatasetEntry>();

        for (int i = 0; i < count; i++)
        {
            onProgress?.Invoke(i + 1, count);
            
            _logger?.InfoFormat("GenerateCustomWordsDataset: Próba {0}/{1}, minWordsCount={2}", i + 1, count, minWordsCount);

            // Można próbować z różnymi rozmiarami siatki dla większej różnorodności
            int currentRows = rows;
            int currentCols = columns;

            var result = GenerateWithCustomWords(currentRows, currentCols, highlightedWord, customWords, minWordsCount: minWordsCount, wordDefinitions: wordDefinitions);

            if (result.IsSuccess)
            {
                results.Add(result.Value);
                _logger?.InfoFormat("GenerateCustomWordsDataset: Sukces {0}/{1}", results.Count, count);
            }
            else
            {
                _logger?.WarningFormat("GenerateCustomWordsDataset: Niepowodzenie {0}/{1}: {2}", i + 1, count, result.Error);
                // Jeśli nie udało się, spróbuj z większą siatką
                if (i < count - 1)
                {
                    currentRows = Math.Min(rows + 3, 25);
                    currentCols = Math.Min(columns + 3, 25);
                    _logger?.InfoFormat("GenerateCustomWordsDataset: Retry z większą siatką {0}x{1}", currentRows, currentCols);
                    var retryResult = GenerateWithCustomWords(currentRows, currentCols, highlightedWord, customWords, minWordsCount: minWordsCount, wordDefinitions: wordDefinitions);
                    if (retryResult.IsSuccess)
                    {
                        results.Add(retryResult.Value);
                        _logger?.InfoFormat("GenerateCustomWordsDataset: Retry sukces {0}/{1}", results.Count, count);
                    }
                    else
                    {
                        _logger?.WarningFormat("GenerateCustomWordsDataset: Retry niepowodzenie: {0}", retryResult.Error);
                    }
                }
            }
        }

        _logger?.InfoFormat("GenerateCustomWordsDataset: Zakończono. Wygenerowano {0}/{1} krzyżówek", results.Count, count);
        
        if (results.Count == 0)
        {
            _logger?.Error("GenerateCustomWordsDataset: Nie udało się wygenerować żadnej krzyżówki!");
        }
        else if (results.Count < count)
        {
            _logger?.WarningFormat("GenerateCustomWordsDataset: Wygenerowano tylko {0} z {1} krzyżówek", results.Count, count);
        }

        return results;
    }

    private string GenerateId(string type, int rows, int cols, bool withWalls)
    {
        var wallSuffix = withWalls ? "_walls" : "";
        return $"{type}_{rows}x{cols}{wallSuffix}_{Guid.NewGuid():N}";
    }
}

