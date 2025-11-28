using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Common;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator datasetów z krzyżówkami ze słowami
/// </summary>
public class WordsDatasetGenerator : IWordsDatasetGenerator
{
    private readonly CrosswordWordPlacer _wordPlacer;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly ICrossGridGenerator? _crossGridGenerator;
    private readonly IDatasetDescriptionGenerator _descriptionGenerator;
    private readonly IHighlightedWordGenerator? _wordGenerator;
    private readonly ICursorLogger? _logger;
    private readonly Random _random;

    public WordsDatasetGenerator(
        CrosswordWordPlacer wordPlacer,
        IXamlGenerator xamlGenerator,
        IDatasetDescriptionGenerator descriptionGenerator,
        ICrossGridGenerator? crossGridGenerator = null,
        IHighlightedWordGenerator? wordGenerator = null,
        ICursorLogger? logger = null,
        Random? random = null)
    {
        _wordPlacer = wordPlacer ?? throw new ArgumentNullException(nameof(wordPlacer));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _descriptionGenerator = descriptionGenerator ?? throw new ArgumentNullException(nameof(descriptionGenerator));
        _crossGridGenerator = crossGridGenerator;
        _wordGenerator = wordGenerator;
        _logger = logger;
        _random = random ?? new Random();
    }

    public Result<DatasetEntry, string> GenerateWithWordsExample(
        int rows, 
        int columns, 
        int targetWordCount = Constants.DefaultTargetWordCount, 
        int? seed = null, 
        string? highlightedWord = null)
    {
        // Sprawdź hasło przed przekazaniem do CrosswordWordPlacer
        _logger?.Debug($"WordsDatasetGenerator.GenerateWithWordsExample: Otrzymałem hasło: '{highlightedWord}'");
        if (highlightedWord != null && highlightedWord.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c)))
        {
            _logger?.Debug($"WordsDatasetGenerator.GenerateWithWordsExample: Hasło MA polskie znaki: '{highlightedWord}'");
        }

        _logger?.Debug($"WordsDatasetGenerator.GenerateWithWordsExample: Przekazuję hasło do wordPlacer.GenerateWithWords: '{highlightedWord}'");
        var result = _wordPlacer.GenerateWithWords(rows, columns, targetWordCount, Constants.DefaultMaxAttempts, highlightedWord);
        
        // Sprawdź czy generowanie się powiodło (ROP)
        if (result.IsFailure)
        {
            var errorMsg = result.Error;
            _logger?.Error($"WordsDatasetGenerator.GenerateWithWordsExample: {errorMsg} (rozmiar: {rows}x{columns}, hasło: '{highlightedWord ?? "brak"}')", null);
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        var (grid, placedWords, highlightedCellsWithIndices) = result.Value;
        
        if (grid == null || placedWords == null || placedWords.Count == 0)
        {
            var errorMsg = $"Nie udało się wygenerować krzyżówki. Rozmiar: {rows}x{columns}, Hasło: {highlightedWord ?? "brak"}, Słowa: {placedWords?.Count ?? 0}";
            _logger?.Error($"WordsDatasetGenerator.GenerateWithWordsExample: {errorMsg}", null);
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        // Sprawdź czy są litery w siatce
        int letterCount = grid.Cells.Values.Count(c => c.HasLetter);
        if (letterCount == 0)
        {
            var errorMsg = $"Krzyżówka nie zawiera liter. Rozmiar: {rows}x{columns}, Hasło: {highlightedWord ?? "brak"}";
            _logger?.Error($"WordsDatasetGenerator.GenerateWithWordsExample: {errorMsg}", null);
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        // Zawsze generuj wszystkie elementy (Settings kontrolują tylko eksport)
        string xaml = _xamlGenerator.GenerateXaml(grid, Constants.DefaultXamlWidth, Constants.DefaultXamlHeight, highlightedCellsWithIndices, placedWords);
        string? emptyXaml = _xamlGenerator.GenerateEmptyXaml(grid, Constants.DefaultXamlWidth, Constants.DefaultXamlHeight, highlightedCellsWithIndices, placedWords);
        string? crossGrid = _crossGridGenerator?.GenerateCrossGrid(grid, highlightedCellsWithIndices, placedWords);
        
        // Zlicz litery i ściany (letterCount już policzone wyżej)
        int wallCount = grid.Cells.Values.Count(c => c.IsWall);
        int emptyCount = grid.Cells.Values.Count(c => c.IsEmpty);
        
        string description = _descriptionGenerator.GenerateWordsDescription(grid, rows, columns, letterCount, placedWords, highlightedWord);
        string searchableText = _descriptionGenerator.GenerateWordsSearchableText(grid, rows, columns, letterCount, xaml, placedWords, highlightedWord);
        string embeddingText = _descriptionGenerator.GenerateWordsEmbeddingText(grid, rows, columns, letterCount, placedWords, highlightedWord);

        var entry = new DatasetEntry
        {
            Id = GenerateId("crossword_words", rows, columns, false),
            Type = "crossword_with_words",
            GridSize = $"{rows}x{columns}",
            HasWalls = wallCount > 0,
            Xaml = xaml,
            EmptyXaml = emptyXaml,
            CrossGrid = crossGrid,
            Description = description,
            SearchableText = searchableText,
            Metadata = new DatasetMetadata
            {
                Rows = rows,
                Columns = columns,
                WallCount = wallCount,
                EmptyCellCount = emptyCount,
                LetterCount = letterCount
            },
            RagMetadata = new RagMetadata
            {
                EmbeddingText = embeddingText,
                Category = "crossword_with_words",
                Timestamp = DateTime.UtcNow
            }
        };

        return Result<DatasetEntry, string>.Success(entry);
    }

    public List<DatasetEntry> GenerateWithWordsDataset(
        int count,
        int minSize = 8,
        int maxSize = Constants.MaxDatasetSize,
        int targetWordCount = Constants.DefaultTargetWordCount,
        string? highlightedWord = null,
        Action<int, int>? onProgress = null)
    {
        _logger?.Info($"GenerateWithWordsDataset: Rozpoczynam generowanie {count} krzyżówek. Hasło: '{highlightedWord ?? "losowe"}', Rozmiar: {minSize}-{maxSize}");
        
        var results = new List<DatasetEntry>();
        
        // Jeśli nie ma podanego hasła, użyj HighlightedWordGenerator (szybszy, z cache)
        if (string.IsNullOrWhiteSpace(highlightedWord))
        {
            _logger?.Info("GenerateWithWordsDataset: Brak hasła - używam HighlightedWordGenerator");
            
            if (_wordGenerator == null)
            {
                _logger?.Error("GenerateWithWordsDataset: HighlightedWordGenerator nie jest dostępny!");
                return results;
            }
            
            // Zoptymalizowane: generuj hasła na bieżąco zamiast pre-generować wszystkie
            int totalAttempts = 0;
            int maxTotalAttempts = count * 20;
            
            // Pre-generuj tylko mały cache (szybsze start)
            var preloadResult = _wordGenerator.PreloadWords(Math.Min(count, 50), Constants.MinWordLength, 8);
            if (preloadResult.IsFailure)
            {
                _logger?.Warning($"GenerateWithWordsDataset: Nie udało się pre-generować haseł: {preloadResult.Error}");
            }
            
            while (results.Count < count && totalAttempts < maxTotalAttempts)
            {
                totalAttempts++;
                
                // Raportuj postęp co kilka prób
                if (totalAttempts % 5 == 0 || totalAttempts == 1)
                {
                    onProgress?.Invoke(results.Count, count);
                }
                
                // Generuj hasło na bieżąco (używa cache jeśli dostępny)
                var wordResult = _wordGenerator.GetRandomWord(6, 8);
                if (wordResult.IsFailure)
                {
                    _logger?.Warning($"GenerateWithWordsDataset: Nie udało się pobrać hasła (próba {totalAttempts}): {wordResult.Error}");
                    continue;
                }
                
                var word = wordResult.Value;
                _logger?.Debug($"GenerateWithWordsDataset: Próba {totalAttempts} z hasłem '{word}' ({word.Length} liter)");
                
                // Użyj większych siatek dla łatwiejszego układania
                int rows = _random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                int cols = _random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                
                // Używamy targetWordCount zamiast word.Length - targetWordCount to liczba słów w krzyżówce
                int seed = _random.Next();
                var result = GenerateWithWordsExample(rows, cols, targetWordCount, seed, word);
                
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    _logger?.Info($"GenerateWithWordsDataset: Sukces! Wygenerowano {results.Count}/{count} krzyżówek. Hasło: '{word}', Rozmiar: {rows}x{cols}");
                    onProgress?.Invoke(results.Count, count);
                }
                else
                {
                    // Jeśli nie udało się, spróbuj z większą siatką (tylko raz)
                    if (totalAttempts % 3 == 0)
                    {
                        rows = _random.Next(Math.Max(minSize, 18), Math.Min(maxSize + 8, 25));
                        cols = _random.Next(Math.Max(minSize, 18), Math.Min(maxSize + 8, 25));
                        var retryResult = GenerateWithWordsExample(rows, cols, targetWordCount, _random.Next(), word);
                        if (retryResult.IsSuccess)
                        {
                            results.Add(retryResult.Value);
                            _logger?.Info($"GenerateWithWordsDataset: Sukces po retry! Wygenerowano {results.Count}/{count} krzyżówek. Hasło: '{word}', Rozmiar: {rows}x{cols}");
                            onProgress?.Invoke(results.Count, count);
                        }
                    }
                }
                
                // Loguj progress co 10 prób
                if (totalAttempts % 10 == 0)
                {
                    _logger?.Info($"GenerateWithWordsDataset: Progress - {results.Count}/{count} krzyżówek, próba {totalAttempts}/{maxTotalAttempts}");
                }
            }
        }
        else
        {
            // Jeśli hasło jest podane, użyj dla wszystkich krzyżówek
            _logger?.Info($"GenerateWithWordsDataset: Używam podanego hasła '{highlightedWord}' dla wszystkich krzyżówek");
            
            int maxRetries = count * 10;
            int attempts = 0;
            
            while (results.Count < count && attempts < maxRetries)
            {
                attempts++;
                
                // Raportuj postęp co kilka prób
                if (attempts % 5 == 0 || attempts == 1)
                {
                    onProgress?.Invoke(results.Count, count);
                }
                
                if (attempts % 10 == 0)
                {
                    _logger?.Info($"GenerateWithWordsDataset: Progress - {results.Count}/{count} krzyżówek, próba {attempts}/{maxRetries}");
                }
                
                int rows = _random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                int cols = _random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                
                var result = GenerateWithWordsExample(rows, cols, targetWordCount, _random.Next(), highlightedWord);
                
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    _logger?.Info($"GenerateWithWordsDataset: Sukces! Wygenerowano {results.Count}/{count} krzyżówek. Hasło: '{highlightedWord}', Rozmiar: {rows}x{cols}");
                    onProgress?.Invoke(results.Count, count);
                }
            }
        }

        _logger?.Info($"GenerateWithWordsDataset: Wygenerowano {results.Count}/{count} krzyżówek");
        
        if (results.Count == 0)
        {
            _logger?.Warning($"GenerateWithWordsDataset: Nie udało się wygenerować żadnej krzyżówki! Hasło: '{highlightedWord ?? "losowe"}', Rozmiar: {minSize}-{maxSize}");
        }
        else if (results.Count < count)
        {
            _logger?.Warning($"GenerateWithWordsDataset: Wygenerowano tylko {results.Count} z {count} krzyżówek. Hasło: '{highlightedWord ?? "losowe"}'");
        }
        
        return results;
    }

    private string GenerateId(string type, int rows, int cols, bool withWalls)
    {
        var wallSuffix = withWalls ? "_walls" : "";
        return $"{type}_{rows}x{cols}{wallSuffix}_{Guid.NewGuid():N}";
    }
}

