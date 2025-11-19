using System.Text.Json;
using System.IO;
using System.Linq;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Infrastructure.Services;

namespace CrosswordAIGenerator.Core.Application_.Services;

/// <summary>
/// Generator datasetów - orchestrator który generuje przykłady z XAML, screenshotami i opisami
/// </summary>
public class DatasetGenerator
{
    private readonly IEmptyGridGenerator _gridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly IWordDictionary? _wordDictionary;
    private readonly CrosswordWordPlacer? _wordPlacer;
    private readonly IHighlightedWordGenerator? _wordGenerator;
    private readonly ICursorLogger? _logger;

    
    /// <summary>
    /// Znajduje plik słownika slowa.txt w różnych lokalizacjach
    /// </summary>
    public static string? FindDictionaryFile()
    {
        // Sprawdź różne możliwe lokalizacje - TYLKO slowa.txt
        var possiblePaths = new List<string>();
        var currentDir = Directory.GetCurrentDirectory();
        
        // 1. Względna ścieżka z katalogu bin (dla WPF)
        var binPathSlowa = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "dictionaries", "slowa.txt");
        possiblePaths.Add(Path.GetFullPath(binPathSlowa));
        
        // 2. Względna ścieżka z katalogu Core (dla testów)
        var corePathSlowa = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "dictionaries", "slowa.txt");
        possiblePaths.Add(Path.GetFullPath(corePathSlowa));
        
        // 3. W katalogu rozwiązania (root projektu)
        var solutionPathSlowa = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "dictionaries", "slowa.txt");
        possiblePaths.Add(Path.GetFullPath(solutionPathSlowa));
        
        // 4. W katalogu roboczym
        possiblePaths.Add(Path.Combine(currentDir, "dictionaries", "slowa.txt"));
        
        // 5. W katalogu nadrzędnym
        var parentDir = Directory.GetParent(currentDir)?.FullName;
        if (!string.IsNullOrEmpty(parentDir))
        {
            possiblePaths.Add(Path.Combine(parentDir, "dictionaries", "slowa.txt"));
        }
        
        // 6. W katalogu rozwiązania (alternatywna ścieżka)
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (!string.IsNullOrEmpty(assemblyDir))
        {
            var solutionPath2Slowa = Path.Combine(assemblyDir, "..", "..", "..", "..", "dictionaries", "slowa.txt");
            possiblePaths.Add(Path.GetFullPath(solutionPath2Slowa));
        }
        
        // DEBUG: Loguj wszystkie sprawdzane ścieżki
        System.Diagnostics.Debug.WriteLine($"[CURSOR] FindDictionaryFile: Sprawdzam {possiblePaths.Count} ścieżek dla slowa.txt...");
        
        foreach (var path in possiblePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] FindDictionaryFile: ZNALEZIONO: {path}");
                    return path;
                }
            }
            catch (Exception ex)
            {
                // Ignoruj błędy ścieżek, ale loguj
                System.Diagnostics.Debug.WriteLine($"[CURSOR] FindDictionaryFile: Błąd sprawdzania {path}: {ex.Message}");
                continue;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[CURSOR] FindDictionaryFile: NIE ZNALEZIONO slowa.txt!");
        return null;
    }

    public DatasetGenerator(
        IEmptyGridGenerator gridGenerator, 
        IXamlGenerator xamlGenerator, 
        IWordDictionary? wordDictionary, 
        CrosswordWordPlacer wordPlacer, 
        IHighlightedWordGenerator? wordGenerator = null,
        ICursorLogger? logger = null)
    {
        _gridGenerator = gridGenerator ?? throw new ArgumentNullException(nameof(gridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _wordDictionary = wordDictionary;
        _wordPlacer = wordPlacer ?? throw new ArgumentNullException(nameof(wordPlacer));
        _wordGenerator = wordGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Generuje pojedynczy przykład pustej siatki
    /// </summary>
    public DatasetEntry GenerateEmptyGridExample(int rows, int columns, bool withWalls = false, double wallProbability = 0.1, int? seed = null)
    {
        var gridGenerator = seed.HasValue ? new Infrastructure.Services.EmptyGridGenerator(seed) : _gridGenerator;
        
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

        var xaml = _xamlGenerator.GenerateXaml(grid, 500, 500, null, null);
        var description = GenerateDescription(grid, withWalls, wallCount);
        var searchableText = GenerateSearchableText(grid, rows, columns, withWalls, wallCount, xaml);
        var embeddingText = GenerateEmbeddingText(grid, rows, columns, withWalls, wallCount);

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


    /// <summary>
    /// Generuje wiele przykładów pustych siatek
    /// </summary>
    public List<DatasetEntry> GenerateEmptyGridDataset(
        int count,
        int minSize = 5,
        int maxSize = 15,
        bool includeWithWalls = true,
        double wallProbability = 0.1)
    {
        var results = new List<DatasetEntry>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            int rows = random.Next(minSize, maxSize + 1);
            int cols = random.Next(minSize, maxSize + 1);
            bool withWalls = includeWithWalls && random.NextDouble() < 0.5;

            var entry = GenerateEmptyGridExample(rows, cols, withWalls, wallProbability, random.Next());
            results.Add(entry);
        }

        return results;
    }

    /// <summary>
    /// Generuje krzyżówkę z rzeczywistymi słowami i przecięciami
    /// </summary>
    public Result<DatasetEntry, string> GenerateWithWordsExample(int rows, int columns, int targetWordCount = 5, int? seed = null, string? highlightedWord = null)
    {
        // DEBUG: Sprawdź hasło przed przekazaniem do CrosswordWordPlacer
        System.Diagnostics.Debug.WriteLine($"[CURSOR] DatasetGenerator.GenerateWithWordsExample: Otrzymałem hasło: '{highlightedWord}'");
        if (highlightedWord != null && highlightedWord.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c)))
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] DatasetGenerator.GenerateWithWordsExample: Hasło MA polskie znaki: '{highlightedWord}'");
        }
        
        if (_wordPlacer == null)
        {
            throw new InvalidOperationException("WordPlacer nie jest zainicjalizowany. Użyj konstruktora z WordDictionary.");
        }

        // Jeśli mamy seed, utwórz nowy placer z tym seedem
        CrosswordWordPlacer wordPlacer;
        if (seed.HasValue && _wordDictionary != null)
        {
            wordPlacer = new CrosswordWordPlacer(_wordDictionary, seed, _logger);
        }
        else
        {
            wordPlacer = _wordPlacer ?? throw new InvalidOperationException("WordPlacer nie jest zainicjalizowany.");
        }

        System.Diagnostics.Debug.WriteLine($"[CURSOR] DatasetGenerator.GenerateWithWordsExample: Przekazuję hasło do wordPlacer.GenerateWithWords: '{highlightedWord}'");
        var result = wordPlacer.GenerateWithWords(rows, columns, targetWordCount, 50, highlightedWord);
        
        // Sprawdź czy generowanie się powiodło (ROP)
        if (result.IsFailure)
        {
            // result.Error już zawiera pełny komunikat, więc nie duplikujemy
            var errorMsg = result.Error;
            _logger?.Error($"DatasetGenerator.GenerateWithWordsExample: {errorMsg} (rozmiar: {rows}x{columns}, hasło: '{highlightedWord ?? "brak"}')", null);
            System.Diagnostics.Debug.WriteLine($"[CURSOR] DatasetGenerator.GenerateWithWordsExample: {errorMsg}");
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        var (grid, placedWords, highlightedCellsWithIndices) = result.Value;
        
        if (grid == null || placedWords == null || placedWords.Count == 0)
        {
            var errorMsg = $"Nie udało się wygenerować krzyżówki. Rozmiar: {rows}x{columns}, Hasło: {highlightedWord ?? "brak"}, Słowa: {placedWords?.Count ?? 0}";
            _logger?.Error($"DatasetGenerator.GenerateWithWordsExample: {errorMsg}", null);
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        // Sprawdź czy są litery w siatce
        int letterCount = grid.Cells.Values.Count(c => c.HasLetter);
        if (letterCount == 0)
        {
            var errorMsg = $"Krzyżówka nie zawiera liter. Rozmiar: {rows}x{columns}, Hasło: {highlightedWord ?? "brak"}";
            _logger?.Error($"DatasetGenerator.GenerateWithWordsExample: {errorMsg}", null);
            return Result<DatasetEntry, string>.Failure(errorMsg);
        }
        
        var xaml = _xamlGenerator.GenerateXaml(grid, 500, 500, highlightedCellsWithIndices, placedWords);
        
        // Generuj również pustą wersję (bez liter, tylko ramki i definicje)
        var emptyXaml = _xamlGenerator.GenerateEmptyXaml(grid, 500, 500, highlightedCellsWithIndices, placedWords);
        
        // Zlicz litery i ściany (letterCount już policzone wyżej)
        int wallCount = grid.Cells.Values.Count(c => c.IsWall);
        int emptyCount = grid.Cells.Values.Count(c => c.IsEmpty);
        
        var description = GenerateWordsDescription(grid, rows, columns, letterCount, placedWords, highlightedWord);
        var searchableText = GenerateWordsSearchableText(grid, rows, columns, letterCount, xaml, placedWords, highlightedWord);
        var embeddingText = GenerateWordsEmbeddingText(grid, rows, columns, letterCount, placedWords, highlightedWord);

        var entry = new DatasetEntry
        {
            Id = GenerateId("crossword_words", rows, columns, false),
            Type = "crossword_with_words",
            GridSize = $"{rows}x{columns}",
            HasWalls = wallCount > 0,
            Xaml = xaml,
            EmptyXaml = emptyXaml,
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

    /// <summary>
    /// Generuje wiele przykładów krzyżówek ze słowami
    /// 
    /// Jeśli highlightedWord jest podane: wszystkie krzyżówki będą miały to samo hasło
    /// Jeśli highlightedWord jest null: wygeneruje listę haseł i dla każdego wywoła GenerateWithWordsExample
    /// </summary>
    public List<DatasetEntry> GenerateWithWordsDataset(
        int count,
        int minSize = 8,
        int maxSize = 15,
        int targetWordCount = 5,
        string? highlightedWord = null,
        Action<int, int>? onProgress = null)
    {
        if (_wordPlacer == null)
        {
            throw new InvalidOperationException("WordPlacer nie jest zainicjalizowany.");
        }

        _logger?.Info($"GenerateWithWordsDataset: Rozpoczynam generowanie {count} krzyżówek. Hasło: '{highlightedWord ?? "losowe"}', Rozmiar: {minSize}-{maxSize}");
        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWordsDataset: Rozpoczynam generowanie {count} krzyżówek. Hasło: '{highlightedWord ?? "losowe"}', Rozmiar: {minSize}-{maxSize}");
        
        var results = new List<DatasetEntry>();
        var random = new Random();
        
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
            // To jest szybsze - nie czekamy na wygenerowanie wszystkich haseł przed rozpoczęciem
            int totalAttempts = 0;
            int maxTotalAttempts = count * 20; // Zmniejszona liczba prób
            
            // Pre-generuj tylko mały cache (szybsze start)
            var preloadResult = _wordGenerator.PreloadWords(Math.Min(count, 50), 6, 8);
            if (preloadResult.IsFailure)
            {
                _logger?.Warning($"GenerateWithWordsDataset: Nie udało się pre-generować haseł: {preloadResult.Error}");
            }
            
            while (results.Count < count && totalAttempts < maxTotalAttempts)
            {
                totalAttempts++;
                
                // Raportuj postęp co kilka prób (żeby użytkownik widział że coś się dzieje)
                if (totalAttempts % 5 == 0 || totalAttempts == 1)
                {
                    onProgress?.Invoke(results.Count, count); // Raportuj aktualny stan
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
                int rows = random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                int cols = random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                
                // Szybka próba (tylko 1 raz) - jeśli się nie uda, przejdź do następnego hasła
                int seed = random.Next();
                var result = GenerateWithWordsExample(rows, cols, word.Length, seed, word);
                
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    _logger?.Info($"GenerateWithWordsDataset: Sukces! Wygenerowano {results.Count}/{count} krzyżówek. Hasło: '{word}', Rozmiar: {rows}x{cols}");
                    onProgress?.Invoke(results.Count, count); // Raportuj postęp przy sukcesie
                }
                else
                {
                    // Jeśli nie udało się, spróbuj z większą siatką (tylko raz)
                    if (totalAttempts % 3 == 0) // Co 3 próbę użyj większej siatki
                    {
                        rows = random.Next(Math.Max(minSize, 18), Math.Min(maxSize + 8, 25));
                        cols = random.Next(Math.Max(minSize, 18), Math.Min(maxSize + 8, 25));
                        var retryResult = GenerateWithWordsExample(rows, cols, word.Length, random.Next(), word);
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
            // Jeśli hasło jest podane, użyj starej metody (działa dobrze)
            _logger?.Info($"GenerateWithWordsDataset: Używam podanego hasła '{highlightedWord}' dla wszystkich krzyżówek");
            
            int maxRetries = count * 10; // Więcej prób dla jednego hasła
            int attempts = 0;
            
            while (results.Count < count && attempts < maxRetries)
            {
                attempts++;
                
                // Raportuj postęp co kilka prób (żeby użytkownik widział że coś się dzieje)
                if (attempts % 5 == 0 || attempts == 1)
                {
                    onProgress?.Invoke(results.Count, count); // Raportuj aktualny stan
                }
                
                if (attempts % 10 == 0)
                {
                    _logger?.Info($"GenerateWithWordsDataset: Progress - {results.Count}/{count} krzyżówek, próba {attempts}/{maxRetries}");
                }
                
                int rows = random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                int cols = random.Next(Math.Max(minSize, 15), Math.Min(maxSize + 5, 20));
                
                var result = GenerateWithWordsExample(rows, cols, highlightedWord.Length, random.Next(), highlightedWord);
                
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    _logger?.Info($"GenerateWithWordsDataset: Sukces! Wygenerowano {results.Count}/{count} krzyżówek. Hasło: '{highlightedWord}', Rozmiar: {rows}x{cols}");
                    onProgress?.Invoke(results.Count, count); // Raportuj postęp przy sukcesie
                }
            }
        }

        _logger?.Info($"GenerateWithWordsDataset: Wygenerowano {results.Count}/{count} krzyżówek");
        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWordsDataset: Wygenerowano {results.Count}/{count} krzyżówek");
        
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

    /// <summary>
    /// Generuje pojedynczą krzyżówkę z podanymi przez użytkownika słowami
    /// </summary>
    public Result<DatasetEntry, string> GenerateWithCustomWords(
        int rows, int columns, string highlightedWord, List<string> customWords, int minWordsCount = 0, Dictionary<string, string>? wordDefinitions = null)
    {
        if (_wordPlacer == null)
        {
            return Result<DatasetEntry, string>.Failure("WordPlacer nie jest zainicjalizowany.");
        }

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

        var xaml = _xamlGenerator.GenerateXaml(grid, width: 500, height: 500, highlightedCellsWithIndices, placedWords, wordDefinitions);
        
        // Generuj również pustą wersję (bez liter, tylko ramki i definicje)
        var emptyXaml = _xamlGenerator.GenerateEmptyXaml(grid, width: 500, height: 500, highlightedCellsWithIndices, placedWords, wordDefinitions);
        
        // Generuj opis z definicjami
        var description = GenerateCustomWordsDescription(highlightedWord, customWords, placedWords);
        var searchableText = GenerateSearchableTextForCustomWords(highlightedWord, customWords, placedWords, rows, columns, xaml);
        var embeddingText = GenerateEmbeddingTextForCustomWords(highlightedWord, customWords, placedWords, rows, columns);

        var entry = new DatasetEntry
        {
            Id = GenerateId("custom_words", rows, columns, false),
            Type = "custom_words",
            GridSize = $"{rows}x{columns}",
            HasWalls = false,
            Xaml = xaml,
            EmptyXaml = emptyXaml,
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
                Category = "crossword_custom_words",
                Timestamp = DateTime.UtcNow
            }
        };

        return Result<DatasetEntry, string>.Success(entry);
    }

    /// <summary>
    /// Generuje dataset z podanymi przez użytkownika słowami
    /// </summary>
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
        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateCustomWordsDataset: Rozpoczynam generowanie {count} krzyżówek. Hasło: '{highlightedWord}', Rozmiar: {rows}x{columns}, MinWordsCount: {minWordsCount}");
        
        var results = new List<DatasetEntry>();
        var random = new Random();

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
                if (i < count - 1) // Nie próbuj na ostatniej iteracji
                {
                    currentRows = Math.Min(rows + 3, 25);
                    currentCols = Math.Min(columns + 3, 25);
                    _logger?.InfoFormat("GenerateCustomWordsDataset: Retry z większą siatką {0}x{1}", currentRows, currentCols);
                    var retryResult = GenerateWithCustomWords(currentRows, currentCols, highlightedWord, customWords, minWordsCount);
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
        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateCustomWordsDataset: Zakończono. Wygenerowano {results.Count}/{count} krzyżówek");
        
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

    private string GenerateCustomWordsDescription(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Krzyżówka z hasłem głównym: {highlightedWord}");
        sb.AppendLine($"Liczba słów w krzyżówce: {placedWords.Count} (z {customWords.Count} dostępnych)");
        sb.AppendLine();
        sb.AppendLine("Słowa w krzyżówce:");
        for (int i = 0; i < placedWords.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {placedWords[i].Word}");
        }
        if (placedWords.Count < customWords.Count)
        {
            sb.AppendLine();
            sb.AppendLine($"Nieużyte słowa ({customWords.Count - placedWords.Count}):");
            var usedWords = placedWords.Select(w => w.Word).ToHashSet();
            var unusedWords = customWords.Where(w => !usedWords.Contains(w.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Trim())).ToList();
            for (int i = 0; i < unusedWords.Count; i++)
            {
                sb.AppendLine($"- {unusedWords[i]}");
            }
        }
        return sb.ToString();
    }

    private string GenerateSearchableTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns, string xaml)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Hasło główne: {highlightedWord}");
        sb.AppendLine($"Rozmiar siatki: {rows}x{columns}");
        sb.AppendLine();
        sb.AppendLine("Słowa:");
        foreach (var word in customWords)
        {
            sb.AppendLine($"- {word}");
        }
        sb.AppendLine();
        sb.AppendLine("Umieszczone słowa:");
        foreach (var word in placedWords)
        {
            sb.AppendLine($"- {word.Word} ({word.Direction})");
        }
        return sb.ToString();
    }

    private string GenerateEmbeddingTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Krzyżówka hasło {highlightedWord} ");
        sb.Append($"słowa {string.Join(" ", customWords)} ");
        sb.Append($"rozmiar {rows}x{columns}");
        return sb.ToString();
    }

    /// <summary>
    /// Losuje hasło (słowo) z słownika o określonej długości
    /// DEPRECATED: Użyj IHighlightedWordGenerator zamiast tego
    /// </summary>
    [Obsolete("Użyj IHighlightedWordGenerator zamiast tego")]
    private string? GetRandomHighlightedWord(int minLength = 6, int maxLength = 10)
    {
        if (_wordGenerator != null)
        {
            var result = _wordGenerator.GetRandomWord(minLength, maxLength);
            return result.IsSuccess ? result.Value : null;
        }
        
        if (_wordDictionary != null)
        {
            // Jeśli to LazyWordDictionary, upewnij się że indeks jest załadowany
            if (_wordDictionary is Infrastructure.Services.LazyWordDictionary lazyDict)
            {
                lazyDict.LoadIndex();
            }
            var word = _wordDictionary.GetRandomWordOfLength(minLength, maxLength);
            return word;
        }
        
        System.Diagnostics.Debug.WriteLine("BŁĄD: Brak słownika do losowania hasła!");
        return null;
    }

    private string GenerateWordsDescription(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Krzyżówka {rows}x{cols} z {placedWords.Count} słowami. ");
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"Hasło główne: {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))}. ");
        }
        
        sb.Append($"Słowa w krzyżówce: ");
        for (int i = 0; i < placedWords.Count; i++)
        {
            var word = placedWords[i];
            sb.Append($"{word.Word}");
            if (i < placedWords.Count - 1)
                sb.Append(", ");
        }
        sb.Append(". ");
        
        // Znajdź przecięcia
        var intersections = FindIntersections(placedWords);
        if (intersections.Count > 0)
        {
            sb.Append($"Przecięcia: ");
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                sb.Append($"{intersection.Word1} i {intersection.Word2} przecinają się w literze '{intersection.Letter}' na pozycji ({intersection.Row}, {intersection.Column})");
                if (i < intersections.Count - 1)
                    sb.Append("; ");
            }
            sb.Append(". ");
        }
        
        sb.Append($"Zawiera {letterCount} kratek z literami. Grid z białym tłem, czarne ramki wokół kratek.");
        return sb.ToString();
    }

    private string GenerateWordsSearchableText(CrosswordGrid grid, int rows, int cols, int letterCount, string xaml, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"XAML WPF Grid {rows} wierszy {cols} kolumn krzyżówka ze słowami ");
        
        // Dodaj wszystkie słowa
        foreach (var word in placedWords)
        {
            sb.Append($"{word.Word} ");
        }
        
        // Dodaj przecięcia
        var intersections = FindIntersections(placedWords);
        foreach (var intersection in intersections)
        {
            sb.Append($"przecięcie {intersection.Word1} {intersection.Word2} litera {intersection.Letter} pozycja {intersection.Row} {intersection.Column} ");
        }
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"hasło główne {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))} ");
        }
        
        sb.Append($"{letterCount} liter TextBlock FontSize 20 ");
        sb.Append($"Border Black BorderThickness 1 Background White ");
        sb.Append($"słowa przecinają się przecięcia");
        return sb.ToString();
    }

    private string GenerateWordsEmbeddingText(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Krzyżówka WPF XAML Grid {rows}x{cols} z {placedWords.Count} rzeczywistymi słowami. ");
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"Hasło główne: {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))}. ");
        }
        
        sb.Append($"Słowa: ");
        foreach (var word in placedWords)
        {
            sb.Append($"{word.Word} ({word.Direction}), ");
        }
        
        // Dodaj informacje o przecięciach
        var intersections = FindIntersections(placedWords);
        if (intersections.Count > 0)
        {
            sb.Append($"Przecięcia: ");
            foreach (var intersection in intersections)
            {
                sb.Append($"{intersection.Word1}×{intersection.Word2} w '{intersection.Letter}', ");
            }
        }
        
        sb.Append($"{letterCount} liter słowa przecinają się przykład XAML dla nauki generowania layoutu krzyżówek ze słowami");
        return sb.ToString();
    }
    
    /// <summary>
    /// Znajduje wszystkie przecięcia między słowami w krzyżówce
    /// </summary>
    private List<WordIntersection> FindIntersections(List<CrosswordWord> placedWords)
    {
        var intersections = new List<WordIntersection>();
        
        for (int i = 0; i < placedWords.Count; i++)
        {
            for (int j = i + 1; j < placedWords.Count; j++)
            {
                var word1 = placedWords[i];
                var word2 = placedWords[j];
                
                // Sprawdź czy słowa się przecinają (muszą być prostopadłe)
                if (word1.Direction == word2.Direction)
                    continue; // Równoległe słowa nie mogą się przecinać
                
                var word1Positions = word1.GetCellPositions().ToList();
                var word2Positions = word2.GetCellPositions().ToList();
                
                // Znajdź wspólne pozycje (przecięcia)
                var commonPositions = word1Positions.Intersect(word2Positions).ToList();
                
                foreach (var (row, col) in commonPositions)
                {
                    // Znajdź literę w obu słowach
                    int letterIndex1 = word1.IsHorizontal 
                        ? col - word1.Column 
                        : row - word1.Row;
                    int letterIndex2 = word2.IsHorizontal 
                        ? col - word2.Column 
                        : row - word2.Row;
                    
                    if (letterIndex1 >= 0 && letterIndex1 < word1.Word.Length &&
                        letterIndex2 >= 0 && letterIndex2 < word2.Word.Length)
                    {
                        char letter1 = word1.Word[letterIndex1];
                        char letter2 = word2.Word[letterIndex2];
                        
                        // W przecięciu litery muszą być takie same
                        if (letter1 == letter2)
                        {
                            intersections.Add(new WordIntersection
                            {
                                Word1 = word1.Word,
                                Word2 = word2.Word,
                                Letter = letter1,
                                Row = row,
                                Column = col,
                                Word1LetterIndex = letterIndex1 + 1, // 1-based dla czytelności
                                Word2LetterIndex = letterIndex2 + 1
                            });
                        }
                    }
                }
            }
        }
        
        return intersections;
    }
    
    /// <summary>
    /// Reprezentuje przecięcie dwóch słów
    /// </summary>
    private class WordIntersection
    {
        public string Word1 { get; set; } = string.Empty;
        public string Word2 { get; set; } = string.Empty;
        public char Letter { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public int Word1LetterIndex { get; set; } // Pozycja litery w pierwszym słowie (1-based)
        public int Word2LetterIndex { get; set; } // Pozycja litery w drugim słowie (1-based)
    }

    /// <summary>
    /// Zapisuje dataset do pliku JSON
    /// </summary>
    public void SaveDatasetToFile(List<DatasetEntry> entries, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(entries, options);
        File.WriteAllText(filePath, json);
    }

    private string GenerateDescription(CrosswordGrid grid, bool hasWalls, int wallCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Pusta siatka krzyżówki {grid.Rows}x{grid.Columns}. ");
        sb.Append($"Grid z {grid.Rows} wierszami i {grid.Columns} kolumnami. ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"Zawiera {wallCount} ścian (czarne kratki z Background=\"Black\"). ");
        }
        else
        {
            sb.Append("Brak ścian. ");
        }
        
        int emptyCount = grid.Cells.Values.Count(c => c.IsEmpty);
        sb.Append($"Wszystkie pozostałe kratki są puste (Border bez TextBlock, BorderBrush=\"Black\", BorderThickness=\"1\"). ");
        sb.Append($"Łącznie {emptyCount} pustych kratek.");
        
        return sb.ToString();
    }

    private string GenerateId(string type, int rows, int cols, bool withWalls)
    {
        var wallSuffix = withWalls ? "_walls" : "";
        return $"{type}_{rows}x{cols}{wallSuffix}_{Guid.NewGuid():N}";
    }

    private string GenerateSearchableText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount, string xaml)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"XAML WPF Grid {rows} wierszy {cols} kolumn krzyżówka ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"ściany Background Black {wallCount} ścian ");
        }
        
        sb.Append($"puste kratki Border Black BorderThickness 1 ");
        sb.Append($"BorderBrush Black ");
        
        // Dodaj fragmenty XAML dla lepszego wyszukiwania
        if (xaml.Contains("TextBlock"))
        {
            sb.Append("TextBlock FontSize 20 HorizontalAlignment Center VerticalAlignment Center ");
        }
        
        sb.Append($"{grid.Cells.Values.Count(c => c.IsEmpty)} pustych kratek ");
        
        return sb.ToString().Trim();
    }

    private string GenerateEmbeddingText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Krzyżówka WPF XAML Grid {rows}x{cols} ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"z {wallCount} ścianami ");
        }
        else
        {
            sb.Append("bez ścian ");
        }
        
        sb.Append($"puste kratki Border Black ");
        sb.Append($"przykład XAML dla nauki generowania layoutu krzyżówek");
        
        return sb.ToString().Trim();
    }
}

/// <summary>
/// Pojedynczy wpis w datasecie
/// </summary>
public class DatasetEntry
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GridSize { get; set; } = string.Empty;
    public bool HasWalls { get; set; }
    public string Xaml { get; set; } = string.Empty;
    
    /// <summary>
    /// Pusta wersja XAML (bez liter, tylko ramki i definicje) - do wypełnienia ręcznie
    /// </summary>
    public string? EmptyXaml { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public DatasetMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Tekst do embeddingu dla RAG - kombinacja XAML, opisu i metadanych
    /// </summary>
    public string SearchableText { get; set; } = string.Empty;
    
    /// <summary>
    /// Metadane dla RAG (embedding, kategoria, timestamp)
    /// </summary>
    public RagMetadata? RagMetadata { get; set; }
}

/// <summary>
/// Metadane dla wpisu w datasecie
/// </summary>
public class DatasetMetadata
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public int WallCount { get; set; }
    public int EmptyCellCount { get; set; }
    public int LetterCount { get; set; }
}

/// <summary>
/// Metadane dla RAG (embedding, kategoria, timestamp)
/// </summary>
public class RagMetadata
{
    /// <summary>
    /// Tekst używany do tworzenia embeddingu
    /// </summary>
    public string EmbeddingText { get; set; } = string.Empty;
    
    /// <summary>
    /// Kategoria dla organizacji w RAG
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp utworzenia
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


