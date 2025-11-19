using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Common;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Generator krzyżówek z rzeczywistymi słowami i przecięciami
/// </summary>
public class CrosswordWordPlacer
{
    private readonly IWordDictionary _dictionary;
    private readonly Random _random;
    private readonly ICursorLogger? _logger;

    public CrosswordWordPlacer(IWordDictionary dictionary, int? seed = null, ICursorLogger? logger = null)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _logger = logger;
    }

    /// <summary>
    /// Helper method - zwraca listę słów zawierających literę
    /// </summary>
    private List<string> GetWordsContaining(char letter, int minLength, int maxLength, int maxResults = 1000)
    {
        return _dictionary.GetWordsContaining(letter, minLength, maxLength, maxResults);
    }

    /// <summary>
    /// Helper method - zwraca losowe słowo
    /// </summary>
    private string? GetRandomWord(int minLength = 6, int maxLength = 20)
    {
        return _dictionary.GetRandomWord(minLength, maxLength);
    }

    /// <summary>
    /// Helper method - zwraca losowe słowo zawierające literę
    /// </summary>
    private string? GetRandomWordContaining(char letter, int minLength = 6, int maxLength = 20)
    {
        return _dictionary.GetRandomWordContaining(letter, minLength, maxLength);
    }

    /// <summary>
    /// Helper method - zwraca losowe hasło o określonej długości
    /// </summary>
    public string? GetRandomWordOfLength(int minLength = 6, int maxLength = 12)
    {
        return _dictionary.GetRandomWordOfLength(minLength, maxLength);
    }

    /// <summary>
    /// Generuje krzyżówkę z słowami układającymi się z przecięciami
    /// </summary>
    /// <param name="rows">Liczba wierszy</param>
    /// <param name="columns">Liczba kolumn</param>
    /// <param name="targetWordCount">Docelowa liczba słów (4-6)</param>
    /// <param name="maxAttempts">Maksymalna liczba prób znalezienia słowa</param>
    /// <param name="highlightedWord">Słowo do wyróżnienia (czerwone tło + numerki) - null jeśli brak</param>
    /// <returns>Result z tuple: (grid, placedWords, highlightedCellsWithIndices) lub błąd</returns>
    public Result<(CrosswordGrid grid, List<CrosswordWord> placedWords, Dictionary<(int row, int col), int> highlightedCellsWithIndices), string> GenerateWithWords(
        int rows, int columns, int targetWordCount = 5, int maxAttempts = 50, string? highlightedWord = null)
    {
        var grid = new CrosswordGrid(rows, columns);
        var placedWords = new List<CrosswordWord>();
        var highlightedCellsWithIndices = new Dictionary<(int row, int col), int>(); // Pozycja -> indeks litery (1, 2, 3...)
        
        const int minWordLength = 6;
        string? targetHighlightedWord = !string.IsNullOrWhiteSpace(highlightedWord) 
            ? highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Trim() 
            : null;
        
        // DEBUG: Sprawdź czy hasło ma polskie znaki przed i po ToUpper
        if (!string.IsNullOrWhiteSpace(highlightedWord) && targetHighlightedWord != null)
        {
            var hasPolishBefore = highlightedWord.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c));
            var hasPolishAfter = targetHighlightedWord.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c));
            
            System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Hasło przed ToUpper: '{highlightedWord}' (ma polskie: {hasPolishBefore})");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Hasło po ToUpper: '{targetHighlightedWord}' (ma polskie: {hasPolishAfter})");
            
            if (hasPolishBefore && !hasPolishAfter)
            {
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: UWAGA! ToUpper stracił polskie znaki w haśle!");
            }
        }
        
        if (targetHighlightedWord == null)
        {
            // Brak hasła - stary algorytm
            var legacyResult = GenerateWithoutHighlightedWord(rows, columns, targetWordCount, maxAttempts);
            if (legacyResult.IsFailure)
            {
                var legacyErrorMsg = $"GenerateWithoutHighlightedWord zwrócił błąd: {legacyResult.Error}";
                _logger?.Error($"CrosswordWordPlacer.GenerateWithWords: {legacyErrorMsg} (metoda: GenerateWithoutHighlightedWord, rows={rows}, columns={columns}, targetWordCount={targetWordCount})", null);
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: {legacyErrorMsg}");
                return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Failure(legacyResult.Error!);
            }
            var (legacyGrid, legacyWords, legacyHighlighted) = legacyResult.Value;
            return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Success((legacyGrid, legacyWords, legacyHighlighted));
        }
        
        // NOWY ALGORYTM: Losuj słowa i próbuj układać krzyżówkę - powtarzaj do skutku
        // Warunki:
        // 1. Wszystkie litery hasła muszą być w słowach
        // 2. Każde słowo musi zawierać przynajmniej jedną literę z hasła
        // 3. Słowa muszą dać się ułożyć w krzyżówkę (z przecięciami)
        
        var highlightedWordLetters = targetHighlightedWord.ToHashSet();
        
        // Pre-load słowa dla wszystkich liter hasła (przyspiesza generowanie) - tylko dla LazyWordDictionary
        if (_dictionary is Infrastructure.Services.LazyWordDictionary lazyDict)
        {
            lazyDict.PreloadWordsForLetters(highlightedWordLetters, wordsPerLetter: 200);
        }
        
        int maxRetries = 100; // Maksymalna liczba prób losowania słów
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            retryCount++;
            
            // Krok 1: Dla każdej litery hasła znajdź słowo które ją zawiera
            // Priorytetyzujemy słowa które zawierają więcej liter z hasła
            var wordsForLetters = new Dictionary<int, string>(); // Indeks litery w haśle -> słowo
            var usedWords = new HashSet<string>(); // Unikamy duplikatów słów
            
            for (int i = 0; i < targetHighlightedWord.Length; i++)
            {
                char requiredLetter = targetHighlightedWord[i];
                
                // DEBUG: Sprawdź literę
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Szukam słów dla litery '{requiredLetter}' (indeks {i} w haśle '{targetHighlightedWord}')");
                if ("ĄĆĘŁŃÓŚŹŻ".Contains(requiredLetter))
                {
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Litera '{requiredLetter}' to polska litera (Unicode: U+{(int)requiredLetter:X4})");
                }
                
                // Znajdź słowa zawierające tę literę (min 6 liter, max 15)
                // WAŻNE: Słowa muszą się zmieścić w siatce (zostawiamy margines 2 kratki z każdej strony)
                int maxWordLength = Math.Min(columns - 2, rows - 2); // Zostaw margines
                maxWordLength = Math.Min(maxWordLength, 15); // Max 15 liter
                
                // Użyj helper method - działa z oboma typami słowników
                var allCandidates = GetWordsContaining(requiredLetter, minWordLength, maxWordLength, maxResults: 200)
                    .Where(w => w.Length <= maxWordLength) // Mieści się w siatce z marginesem
                    .Where(w => !usedWords.Contains(w)) // Unikaj duplikatów
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Znaleziono {allCandidates.Count} kandydatów dla litery '{requiredLetter}'");
                
                if (allCandidates.Count == 0)
                {
                    // Jeśli nie ma nowych słów, pozwól na duplikaty (lepsze niż brak słowa)
                    allCandidates = GetWordsContaining(requiredLetter, minWordLength, maxWordLength, maxResults: 200)
                        .Where(w => w.Length <= maxWordLength)
                        .ToList();
                }
                
                if (allCandidates.Count == 0)
                {
                    // Ostateczny fallback - użyj losowego słowa zawierającego literę
                    var fallback = GetRandomWordContaining(requiredLetter, minWordLength, maxWordLength);
                    
                    if (fallback != null && fallback.Length >= minWordLength && fallback.Length <= maxWordLength)
                    {
                        wordsForLetters[i] = fallback;
                        usedWords.Add(fallback);
                        continue;
                    }
                    else
                    {
                        // Jeśli fallback nie pasuje, spróbuj znaleźć jakiekolwiek słowo
                        var anyWord = GetRandomWordContaining(requiredLetter, minWordLength, maxWordLength);
                        
                        if (anyWord != null && anyWord.Length <= maxWordLength)
                        {
                            wordsForLetters[i] = anyWord;
                            usedWords.Add(anyWord);
                            continue;
                        }
                    }
                }
                
                // Priorytetyzuj słowa które zawierają więcej liter z hasła, ale z większą losowością
                // Licz ile liter z hasła zawiera każde słowo
                // WAŻNE: Każde słowo MUSI zawierać przynajmniej jedną literę z hasła
                var scoredCandidates = allCandidates.Select(word => new
                {
                    Word = word,
                    Score = word.Count(c => highlightedWordLetters.Contains(c)), // Ile liter z hasła
                    ContainsRequiredLetter = word.Contains(requiredLetter), // Zawiera wymaganą literę dla tej pozycji
                    ContainsAnyHighlightedLetter = word.Any(c => highlightedWordLetters.Contains(c)), // Zawiera przynajmniej jedną literę z hasła
                    RandomWeight = _random.NextDouble() // Dodaj losową wagę dla większej różnorodności
                })
                .Where(x => x.ContainsRequiredLetter && x.ContainsAnyHighlightedLetter) // MUSI zawierać wymaganą literę I przynajmniej jedną literę z hasła
                .OrderByDescending(x => x.Score) // Najpierw słowa z większą liczbą liter z hasła
                .ThenByDescending(x => x.RandomWeight) // Potem losowo (używamy RandomWeight zamiast ThenBy z Next())
                .ToList();
                
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: scoredCandidates.Count = {scoredCandidates.Count} dla litery '{requiredLetter}'");
                
                if (scoredCandidates.Count > 0)
                {
                    // Użyj ważonego losowego wyboru - preferuj słowa z wyższym score, ale nie zawsze te same
                    // Wybierz z top 30% najlepszych kandydatów (lub minimum 3, jeśli jest mniej)
                    int topCount = Math.Max(3, (int)(scoredCandidates.Count * 0.3));
                    var topCandidates = scoredCandidates.Take(topCount).ToList();
                    
                    // Jeśli wszystkie mają ten sam score, wybierz losowo z całej listy
                    if (scoredCandidates.All(x => x.Score == scoredCandidates[0].Score))
                    {
                        topCandidates = scoredCandidates;
                    }
                    
                    var selectedWord = topCandidates[_random.Next(topCandidates.Count)].Word;
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Wybrano słowo '{selectedWord}' dla litery '{requiredLetter}'");
                    wordsForLetters[i] = selectedWord;
                    usedWords.Add(selectedWord);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: scoredCandidates jest puste dla litery '{requiredLetter}', używam fallback z allCandidates (Count={allCandidates.Count})");
                    // Fallback - użyj losowego słowa z dostępnych
                    if (allCandidates.Count > 0)
                    {
                        var fallbackWord = allCandidates[_random.Next(allCandidates.Count)];
                        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: Fallback wybrano słowo '{fallbackWord}' dla litery '{requiredLetter}'");
                        wordsForLetters[i] = fallbackWord;
                        usedWords.Add(fallbackWord);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: BŁĄD! Nie znaleziono żadnego słowa dla litery '{requiredLetter}' (indeks {i} w haśle '{targetHighlightedWord}')");
                    }
                }
            }
            
                // WALIDACJA: Sprawdź czy wszystkie litery hasła są obecne w wybranych słowach
            // Liczymy wystąpienia każdej litery w hasle i w wybranych słowach
            var allSelectedWords = string.Join("", wordsForLetters.Values);
            var highlightedWordLetterCounts = new Dictionary<char, int>();
            var selectedWordsLetterCounts = new Dictionary<char, int>();
            
            // Policz wystąpienia każdej litery w haśle
            foreach (char letter in targetHighlightedWord)
            {
                if (!highlightedWordLetterCounts.ContainsKey(letter))
                    highlightedWordLetterCounts[letter] = 0;
                highlightedWordLetterCounts[letter]++;
            }
            
            // Policz wystąpienia każdej litery w wybranych słowach
            foreach (char letter in allSelectedWords)
            {
                if (!selectedWordsLetterCounts.ContainsKey(letter))
                    selectedWordsLetterCounts[letter] = 0;
                selectedWordsLetterCounts[letter]++;
            }
            
            // Znajdź brakujące litery (których jest za mało w wybranych słowach)
            var missingLetters = new List<(char letter, int needed, int current)>();
            foreach (var kvp in highlightedWordLetterCounts)
            {
                char letter = kvp.Key;
                int needed = kvp.Value;
                int current = selectedWordsLetterCounts.ContainsKey(letter) ? selectedWordsLetterCounts[letter] : 0;
                
                if (current < needed)
                {
                    missingLetters.Add((letter, needed - current, current));
                }
            }
            
            // Jeśli brakuje liter, spróbuj je dodać
            if (missingLetters.Count > 0)
            {
                foreach (var (missingLetter, neededCount, currentCount) in missingLetters)
                {
                    // Znajdź wszystkie pozycje tej litery w haśle (może być kilka)
                    var letterIndices = new List<int>();
                    for (int i = 0; i < targetHighlightedWord.Length; i++)
                    {
                        if (targetHighlightedWord[i] == missingLetter)
                        {
                            letterIndices.Add(i);
                        }
                    }
                    
                    // Dla każdej brakującej litery, spróbuj znaleźć słowo które ją zawiera
                    int addedCount = 0;
                    foreach (var letterIndex in letterIndices)
                    {
                        if (addedCount >= neededCount)
                            break;
                        
                        // Jeśli już mamy słowo dla tego indeksu, sprawdź czy zawiera wymaganą literę
                        if (wordsForLetters.ContainsKey(letterIndex))
                        {
                            if (wordsForLetters[letterIndex].Contains(missingLetter))
                            {
                                addedCount++;
                                continue; // To słowo już zawiera literę
                            }
                        }
                        
                        // Znajdź słowo które zawiera brakującą literę
                        int maxWordLengthForReplacement = Math.Min(columns - 2, rows - 2);
                        maxWordLengthForReplacement = Math.Min(maxWordLengthForReplacement, 15);
                        var replacementCandidates = GetWordsContaining(missingLetter, minWordLength, maxWordLengthForReplacement, maxResults: 200)
                            .Where(w => w.Length <= maxWordLengthForReplacement)
                            .Where(w => !usedWords.Contains(w) || w == wordsForLetters.GetValueOrDefault(letterIndex, ""))
                            .ToList();
                        
                        if (replacementCandidates.Count > 0)
                        {
                            // Wybierz słowo które zawiera najwięcej liter z hasła
                            var bestReplacement = replacementCandidates
                                .Select(word => new
                                {
                                    Word = word,
                                    Score = word.Count(c => highlightedWordLetters.Contains(c)),
                                    LetterCount = word.Count(c => c == missingLetter) // Ile razy zawiera brakującą literę
                                })
                                .OrderByDescending(x => x.LetterCount) // Priorytet: słowa z większą liczbą brakujących liter
                                .ThenByDescending(x => x.Score) // Potem: słowa z większą liczbą liter z hasła
                                .First();
                            
                            wordsForLetters[letterIndex] = bestReplacement.Word;
                            if (!usedWords.Contains(bestReplacement.Word))
                            {
                                usedWords.Add(bestReplacement.Word);
                            }
                            addedCount++;
                        }
                    }
                }
            }
            
            // WALIDACJA KOŃCOWA: Upewnij się że wszystkie warunki są spełnione
            
            // 1. Warunek: Liczba słów = liczba liter w haśle
            if (wordsForLetters.Count != targetHighlightedWord.Length)
            {
                // Jeśli brakuje słów, dodaj je
                for (int i = 0; i < targetHighlightedWord.Length; i++)
                {
                    if (!wordsForLetters.ContainsKey(i))
                    {
                        char requiredLetter = targetHighlightedWord[i];
                        int maxWordLengthForValidation = Math.Min(columns - 2, rows - 2);
                        maxWordLengthForValidation = Math.Min(maxWordLengthForValidation, 15);
                        
                        // Spróbuj znaleźć słowa z wymaganą literą
                        var candidates = GetWordsContaining(requiredLetter, minWordLength, maxWordLengthForValidation, maxResults: 200)
                            .Where(w => w.Length <= maxWordLengthForValidation)
                            .Where(w => w.Any(c => highlightedWordLetters.Contains(c))) // Zawiera przynajmniej jedną literę z hasła
                            .ToList();
                        
                        // FALLBACK: Jeśli nie znaleziono słów z tą literą (np. polskie znaki nie są w słowniku),
                        // użyj zamiennika (Ł->L, Ą->A, etc.) lub losowego słowa
                        if (candidates.Count == 0)
                        {
                            // Mapowanie polskich znaków na podstawowe litery
                            char fallbackLetter = requiredLetter switch
                            {
                                'Ł' => 'L',
                                'Ą' => 'A',
                                'Ć' => 'C',
                                'Ę' => 'E',
                                'Ń' => 'N',
                                'Ó' => 'O',
                                'Ś' => 'S',
                                'Ź' => 'Z',
                                'Ż' => 'Z',
                                _ => requiredLetter
                            };
                            
                            if (fallbackLetter != requiredLetter)
                            {
                                _logger?.Warning($"Nie znaleziono słów z literą '{requiredLetter}', używam zamiennika '{fallbackLetter}'");
                                candidates = GetWordsContaining(fallbackLetter, minWordLength, maxWordLengthForValidation, maxResults: 200)
                                    .Where(w => w.Length <= maxWordLengthForValidation)
                                    .ToList();
                            }
                            
                            // Ostateczny fallback - losowe słowo
                            if (candidates.Count == 0)
                            {
                                var randomWord = GetRandomWord(minWordLength, maxWordLengthForValidation);
                                if (randomWord != null && randomWord.Length >= minWordLength)
                                {
                                    candidates.Add(randomWord);
                                }
                            }
                        }
                        
                        if (candidates.Count > 0)
                        {
                            wordsForLetters[i] = candidates[_random.Next(candidates.Count)];
                        }
                    }
                }
            }
            
            // 2. Warunek: Każde słowo musi zawierać przynajmniej jedną literę z hasła
            var invalidWords = new List<int>();
            foreach (var kvp in wordsForLetters)
            {
                int letterIndex = kvp.Key;
                string word = kvp.Value;
                
                // Sprawdź czy słowo zawiera przynajmniej jedną literę z hasła
                bool containsHighlightedLetter = word.Any(c => highlightedWordLetters.Contains(c));
                if (!containsHighlightedLetter)
                {
                    invalidWords.Add(letterIndex);
                }
            }
            
            // Napraw słowa które nie zawierają liter z hasła
            foreach (var letterIndex in invalidWords)
            {
                char requiredLetter = targetHighlightedWord[letterIndex];
                int maxWordLengthForFix = Math.Min(columns - 2, rows - 2);
                maxWordLengthForFix = Math.Min(maxWordLengthForFix, 15);
                
                var candidates = GetWordsContaining(requiredLetter, minWordLength, maxWordLengthForFix, maxResults: 200)
                    .Where(w => w.Length <= maxWordLengthForFix)
                    .Where(w => w.Any(c => highlightedWordLetters.Contains(c))) // Zawiera przynajmniej jedną literę z hasła
                    .OrderByDescending(w => w.Count(c => highlightedWordLetters.Contains(c))) // Priorytet: więcej liter z hasła
                    .ToList();
                
                // FALLBACK: Jeśli nie znaleziono, użyj zamiennika
                if (candidates.Count == 0)
                {
                    char fallbackLetter = requiredLetter switch
                    {
                        'Ł' => 'L',
                        'Ą' => 'A',
                        'Ć' => 'C',
                        'Ę' => 'E',
                        'Ń' => 'N',
                        'Ó' => 'O',
                        'Ś' => 'S',
                        'Ź' => 'Z',
                        'Ż' => 'Z',
                        _ => requiredLetter
                    };
                    
                    if (fallbackLetter != requiredLetter)
                    {
                        candidates = GetWordsContaining(fallbackLetter, minWordLength, maxWordLengthForFix, maxResults: 200)
                            .Where(w => w.Length <= maxWordLengthForFix)
                            .ToList();
                    }
                }
                
                if (candidates.Count > 0)
                {
                    wordsForLetters[letterIndex] = candidates[0];
                }
            }
            
            // 3. Ostateczna walidacja: upewnij się że mamy dokładnie tyle słów ile liter w haśle
            if (wordsForLetters.Count != targetHighlightedWord.Length)
            {
                // Jeśli nadal nie pasuje, użyj fallback - wybierz losowe słowa które zawierają litery z hasła
                wordsForLetters.Clear();
                for (int i = 0; i < targetHighlightedWord.Length; i++)
                {
                    char requiredLetter = targetHighlightedWord[i];
                    int maxWordLengthForFallback = Math.Min(columns - 2, rows - 2);
                    maxWordLengthForFallback = Math.Min(maxWordLengthForFallback, 15);
                    
                    var candidates = GetWordsContaining(requiredLetter, minWordLength, maxWordLengthForFallback, maxResults: 200)
                        .Where(w => w.Length <= maxWordLengthForFallback)
                        .Where(w => w.Any(c => highlightedWordLetters.Contains(c))) // Zawiera przynajmniej jedną literę z hasła
                        .ToList();
                    
                    // FALLBACK: Jeśli nie znaleziono, użyj zamiennika
                    if (candidates.Count == 0)
                    {
                        char fallbackLetter = requiredLetter switch
                        {
                            'Ł' => 'L',
                            'Ą' => 'A',
                            'Ć' => 'C',
                            'Ę' => 'E',
                            'Ń' => 'N',
                            'Ó' => 'O',
                            'Ś' => 'S',
                            'Ź' => 'Z',
                            'Ż' => 'Z',
                            _ => requiredLetter
                        };
                        
                        if (fallbackLetter != requiredLetter)
                        {
                            candidates = GetWordsContaining(fallbackLetter, minWordLength, maxWordLengthForFallback, maxResults: 200)
                                .Where(w => w.Length <= maxWordLengthForFallback)
                                .ToList();
                        }
                    }
                    
                    if (candidates.Count > 0)
                    {
                        wordsForLetters[i] = candidates[_random.Next(candidates.Count)];
                    }
                    else
                    {
                        // Ostateczny fallback - użyj losowego słowa
                        var fallback = GetRandomWord(minWordLength, maxWordLengthForFallback);
                        if (fallback != null && fallback.Length >= minWordLength && fallback.Length <= Math.Max(columns, rows))
                        {
                            wordsForLetters[i] = fallback;
                        }
                    }
                }
            }
        
            // Krok 2: Ułóż słowa w krzyżówkę - próbuj połączyć je przecięciami
            var arrangeResult = ArrangeWordsInGrid(
                new CrosswordGrid(rows, columns), 
                wordsForLetters, 
                targetHighlightedWord, 
                rows, 
                columns, 
                maxAttempts);
            
            if (arrangeResult.IsFailure)
            {
                _logger?.Warning($"CrosswordWordPlacer.GenerateWithWords: ArrangeWordsInGrid zwrócił błąd (próba {retryCount + 1}/{maxRetries}): {arrangeResult.Error} (rows={rows}, columns={columns}, hasło='{targetHighlightedWord}')");
                System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: ArrangeWordsInGrid zwrócił błąd: {arrangeResult.Error}");
                continue; // Spróbuj ponownie z innymi słowami
            }
            
            var (testGrid, testPlacedWords, testHighlightedCells) = arrangeResult.Value;
            
            // WALIDACJA: Sprawdź czy wszystkie warunki są spełnione
            bool allConditionsMet = true;
            
            // 1. Liczba słów = liczba liter w haśle
            if (testPlacedWords.Count != targetHighlightedWord.Length)
            {
                allConditionsMet = false;
            }
            
            // 2. Wszystkie litery hasła są obecne w siatce
            var allLettersInGrid = new HashSet<char>();
            for (int r = 0; r < testGrid.Rows; r++)
            {
                for (int c = 0; c < testGrid.Columns; c++)
                {
                    var cell = testGrid.GetCell(r, c);
                    if (cell.HasLetter)
                    {
                        allLettersInGrid.Add(cell.Letter!.Value);
                    }
                }
            }
            
            foreach (char letter in targetHighlightedWord)
            {
                if (!allLettersInGrid.Contains(letter))
                {
                    allConditionsMet = false;
                    break;
                }
            }
            
            // 3. Każde słowo zawiera przynajmniej jedną literę z hasła
            foreach (var placedWord in testPlacedWords)
            {
                bool containsHighlightedLetter = placedWord.Word.Any(c => highlightedWordLetters.Contains(c));
                if (!containsHighlightedLetter)
                {
                    allConditionsMet = false;
                    break;
                }
            }
            
            // 4. Wszystkie litery hasła są zaznaczone
            var foundIndices = testHighlightedCells.Values.Distinct().ToHashSet();
            var expectedIndices = Enumerable.Range(1, targetHighlightedWord.Length).ToHashSet();
            if (!expectedIndices.IsSubsetOf(foundIndices))
            {
                allConditionsMet = false;
            }
            
            // Jeśli wszystkie warunki są spełnione - zwróć wynik
            if (allConditionsMet)
            {
                return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Success((testGrid, testPlacedWords, testHighlightedCells));
            }
            
            // Jeśli nie - spróbuj ponownie z nowymi słowami
        }
        
        // Jeśli nie udało się po maxRetries próbach, zwróć błąd
        var errorMsg = $"Nie udało się wygenerować krzyżówki. Nie znaleziono odpowiednich słów dla hasła '{targetHighlightedWord}' po {maxRetries} próbach losowania.";
        _logger?.Error($"CrosswordWordPlacer.GenerateWithWords: {errorMsg} (rows={rows}, columns={columns}, targetWordCount={targetWordCount}, maxAttempts={maxAttempts})", null);
        System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateWithWords: {errorMsg}");
        return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Failure(errorMsg);
    }
    
    /// <summary>
    /// Generuje krzyżówkę bez hasła głównego (stary algorytm)
    /// </summary>
    private Result<(CrosswordGrid grid, List<CrosswordWord> placedWords, Dictionary<(int row, int col), int> highlightedCellsWithIndices), string> GenerateWithoutHighlightedWord(
        int rows, int columns, int targetWordCount, int maxAttempts)
    {
        var grid = new CrosswordGrid(rows, columns);
        var placedWords = new List<CrosswordWord>();
        var highlightedCellsWithIndices = new Dictionary<(int row, int col), int>();
        
        const int minWordLength = 6;
        string firstWord = GetRandomWordWithMinLength(minWordLength);
        
        if (firstWord.Length > columns)
        {
            var shorterWords = GetWordsContaining(firstWord[0], minWordLength, columns, maxResults: 100);
            if (shorterWords.Count > 0)
            {
                firstWord = shorterWords[_random.Next(shorterWords.Count)];
            }
        }
        
        int startRow = rows / 2;
        int startCol = Math.Max(0, (columns - firstWord.Length) / 2);
        
        var firstCrosswordWord = new CrosswordWord(1, firstWord, startRow, startCol, WordDirection.Across);
        PlaceWord(grid, firstCrosswordWord);
        placedWords.Add(firstCrosswordWord);
        
        int wordId = 2;
        int attempts = 0;
        while (placedWords.Count < targetWordCount && attempts < maxAttempts)
        {
            attempts++;
            var baseWord = placedWords[_random.Next(placedWords.Count)];
            int letterIndex = _random.Next(baseWord.Length);
            char letter = baseWord.Word[letterIndex];
            var (letterRow, letterCol) = baseWord.GetCellPositions().ElementAt(letterIndex);
            
            bool hasPerpendicularWord = placedWords.Any(w => 
                w.Direction != baseWord.Direction && 
                w.GetCellPositions().Contains((letterRow, letterCol)));
            
            if (hasPerpendicularWord)
                continue;
            
            var perpendicularWord = FindPerpendicularWord(baseWord, letterRow, letterCol, letter, grid, placedWords);
            
            if (perpendicularWord != null && CanPlaceWord(grid, perpendicularWord, placedWords))
            {
                PlaceWord(grid, perpendicularWord);
                placedWords.Add(perpendicularWord);
                wordId++;
            }
        }
        
        return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Success((grid, placedWords, highlightedCellsWithIndices));
    }
    
    /// <summary>
    /// Układa słowa w siatce krzyżówki, próbując je połączyć przecięciami
    /// </summary>
    private Result<(CrosswordGrid grid, List<CrosswordWord> placedWords, Dictionary<(int row, int col), int> highlightedCellsWithIndices), string> ArrangeWordsInGrid(
        CrosswordGrid grid, Dictionary<int, string> wordsForLetters, string highlightedWord, 
        int rows, int columns, int maxAttempts)
    {
        var placedWords = new List<CrosswordWord>();
        var highlightedCellsWithIndices = new Dictionary<(int row, int col), int>();
        
        // Krok 1: Umieść pierwsze słowo (poziome, w środku)
        if (wordsForLetters.Count == 0)
        {
            var errorMsg = $"Nie udało się znaleźć słów dla hasła '{highlightedWord}'. wordsForLetters jest puste.";
            _logger?.Error($"CrosswordWordPlacer.ArrangeWordsInGrid: {errorMsg} (rows={rows}, columns={columns}, maxAttempts={maxAttempts})", null);
            System.Diagnostics.Debug.WriteLine($"[CURSOR] ArrangeWordsInGrid: BŁĄD - {errorMsg}");
            return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Failure(errorMsg);
        }
        
        System.Diagnostics.Debug.WriteLine($"[CURSOR] ArrangeWordsInGrid: wordsForLetters.Count = {wordsForLetters.Count}, Hasło: '{highlightedWord}'");
        foreach (var kvp in wordsForLetters)
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] ArrangeWordsInGrid: Litera {kvp.Key} ('{highlightedWord[kvp.Key]}') -> Słowo: '{kvp.Value}'");
        }
        
        var firstEntry = wordsForLetters.First();
        string firstWord = firstEntry.Value;
        int firstLetterIndex = firstEntry.Key;
        char firstRequiredLetter = highlightedWord[firstLetterIndex];
        
        // Umieść pierwsze słowo w środku siatki (nie w pierwszym wierszu)
        int startRow = Math.Max(1, rows / 2); // Minimum row 1, żeby nie było w pierwszym wierszu
        int startCol = Math.Max(1, (columns - firstWord.Length) / 2); // Minimum col 1
        
        var firstCrosswordWord = new CrosswordWord(1, firstWord, startRow, startCol, WordDirection.Across);
        PlaceWord(grid, firstCrosswordWord);
        placedWords.Add(firstCrosswordWord);
        
        // Oznacz literę hasła w pierwszym słowie
        int letterPosInWord = firstWord.IndexOf(firstRequiredLetter);
        if (letterPosInWord >= 0)
        {
            var (letterRow, letterCol) = firstCrosswordWord.GetCellPositions().ElementAt(letterPosInWord);
            highlightedCellsWithIndices[(letterRow, letterCol)] = firstLetterIndex + 1;
        }
        
        // Krok 2: Próbuj umieścić pozostałe słowa, łącząc je z już umieszczonymi
        var remainingWords = wordsForLetters.Skip(1).ToList();
        int wordId = 2;
        int attempts = 0;
        
        while (remainingWords.Count > 0 && attempts < maxAttempts)
        {
            attempts++;
            
            // Wybierz losowe słowo do umieszczenia
            var wordEntry = remainingWords[_random.Next(remainingWords.Count)];
            int letterIndex = wordEntry.Key;
            string word = wordEntry.Value;
            char requiredLetter = highlightedWord[letterIndex];
            
            // Spróbuj znaleźć miejsce gdzie można umieścić to słowo (przecięcie z istniejącym)
            var placed = TryPlaceWordForLetter(grid, placedWords, word, requiredLetter, letterIndex, highlightedWord);
            
            if (placed.HasValue)
            {
                var (placedWord, letterPos) = placed.Value;
                PlaceWord(grid, placedWord);
                placedWord.Id = wordId; // Ustaw poprawne ID
                placedWords.Add(placedWord);
                
                // Oznacz literę hasła
                if (letterPos.HasValue)
                {
                    highlightedCellsWithIndices[letterPos.Value] = letterIndex + 1;
                }
                
                remainingWords.Remove(wordEntry);
                wordId++;
            }
        }
        
        // Jeśli nie wszystkie słowa zostały umieszczone, spróbuj jeszcze raz z większą liczbą prób
        // WAŻNE: Wszystkie słowa MUSZĄ mieć przecięcie z już umieszczonymi (nie umieszczamy słów bez przecięć)
        if (remainingWords.Count > 0)
        {
            // Dodatkowe próby dla pozostałych słów - ale tylko z przecięciami
            int additionalAttempts = 0;
            int maxAdditionalAttempts = maxAttempts * 3; // Zwiększona liczba prób
            
            while (remainingWords.Count > 0 && additionalAttempts < maxAdditionalAttempts)
            {
                additionalAttempts++;
                var wordEntry = remainingWords[_random.Next(remainingWords.Count)];
                int letterIndex = wordEntry.Key;
                string word = wordEntry.Value;
                char requiredLetter = highlightedWord[letterIndex];
                
                // WAŻNE: TryPlaceWordForLetter zawsze wymaga przecięcia z już umieszczonym słowem
                var placed = TryPlaceWordForLetter(grid, placedWords, word, requiredLetter, letterIndex, highlightedWord);
                
                if (placed.HasValue)
                {
                    var (placedWord, letterPos) = placed.Value;
                    PlaceWord(grid, placedWord);
                    placedWord.Id = wordId; // Ustaw poprawne ID
                    placedWords.Add(placedWord);
                    wordId++;
                    
                    if (letterPos.HasValue)
                    {
                        highlightedCellsWithIndices[letterPos.Value] = letterIndex + 1;
                    }
                    
                    remainingWords.Remove(wordEntry);
                }
            }
        }
        
        // WALIDACJA: Upewnij się że każde słowo w krzyżówce zawiera przynajmniej jedną literę z hasła
        var highlightedWordLetters = highlightedWord.ToHashSet();
        var invalidPlacedWords = new List<CrosswordWord>();
        
        foreach (var placedWord in placedWords)
        {
            // Sprawdź czy słowo zawiera przynajmniej jedną literę z hasła
            bool containsHighlightedLetter = placedWord.Word.Any(c => highlightedWordLetters.Contains(c));
            if (!containsHighlightedLetter)
            {
                invalidPlacedWords.Add(placedWord);
            }
        }
        
        // Usuń słowa które nie zawierają liter z hasła (nie powinno się zdarzyć, ale na wszelki wypadek)
        foreach (var invalidWord in invalidPlacedWords)
        {
            // Usuń słowo z siatki
            foreach (var (row, col) in invalidWord.GetCellPositions())
            {
                var cell = grid.GetCell(row, col);
                if (cell.HasLetter && cell.Letter == invalidWord.Word[invalidWord.GetCellPositions().ToList().IndexOf((row, col))])
                {
                    // Sprawdź czy ta kratka nie jest częścią innego słowa
                    bool isPartOfOtherWord = placedWords
                        .Where(w => w != invalidWord)
                        .Any(w => w.GetCellPositions().Contains((row, col)));
                    
                    if (!isPartOfOtherWord)
                    {
                        grid.SetCell(row, col, CrosswordCellType.Empty);
                    }
                }
            }
            
            placedWords.Remove(invalidWord);
        }
        
        // WALIDACJA: Upewnij się że liczba słów = liczba liter w haśle
        // Sprawdź które słowa z wordsForLetters nie zostały umieszczone
        var placedWordStrings = placedWords.Select(w => w.Word).ToHashSet();
        var missingWords = wordsForLetters
            .Where(kvp => !placedWordStrings.Contains(kvp.Value))
            .ToList();
        
        // Spróbuj umieścić brakujące słowa - MUSZĄ mieć przecięcie z już umieszczonymi słowami
        // Nie umieszczamy słów bez przecięć, bo to powoduje "odrywanie" słów od reszty krzyżówki
        if (missingWords.Count > 0)
        {
            // Próbuj wielokrotnie umieścić każde brakujące słowo z przecięciem
            int maxRetriesForMissing = 50; // Zwiększona liczba prób dla brakujących słów
            var wordsToPlace = new List<KeyValuePair<int, string>>(missingWords);
            
            foreach (var missingWordEntry in wordsToPlace)
            {
                int letterIndex = missingWordEntry.Key;
                string word = missingWordEntry.Value;
                char requiredLetter = highlightedWord[letterIndex];
                
                bool placed = false;
                int retries = 0;
                
                // Próbuj znaleźć przecięcie z już umieszczonymi słowami
                while (!placed && retries < maxRetriesForMissing)
                {
                    retries++;
                    
                    // Użyj tej samej metody co dla innych słów - wymaga przecięcia
                    var placedResult = TryPlaceWordForLetter(grid, placedWords, word, requiredLetter, letterIndex, highlightedWord);
                    
                    if (placedResult.HasValue)
                    {
                        var (placedWord, letterPos) = placedResult.Value;
                        PlaceWord(grid, placedWord);
                        placedWord.Id = placedWords.Count + 1; // Ustaw poprawne ID
                        placedWords.Add(placedWord);
                        
                        // Oznacz literę hasła
                        if (letterPos.HasValue)
                        {
                            highlightedCellsWithIndices[letterPos.Value] = letterIndex + 1;
                        }
                        
                        placed = true;
                    }
                }
                
                // Jeśli nadal nie udało się umieścić, spróbuj z innymi słowami najpierw
                // (może to pomoże stworzyć więcej możliwości przecięć)
                if (!placed)
                {
                    // Próbuj jeszcze raz z większą liczbą prób, ale tylko jeśli są inne słowa do umieszczenia
                    // To może pomóc, jeśli kolejność ma znaczenie
                    for (int extraAttempt = 0; extraAttempt < 20 && !placed; extraAttempt++)
                    {
                        var placedResult = TryPlaceWordForLetter(grid, placedWords, word, requiredLetter, letterIndex, highlightedWord);
                        
                        if (placedResult.HasValue)
                        {
                            var (placedWord, letterPos) = placedResult.Value;
                            PlaceWord(grid, placedWord);
                            placedWord.Id = placedWords.Count + 1;
                            placedWords.Add(placedWord);
                            
                            if (letterPos.HasValue)
                            {
                                highlightedCellsWithIndices[letterPos.Value] = letterIndex + 1;
                            }
                            
                            placed = true;
                        }
                    }
                }
            }
        }
        
        // Ostateczna walidacja: upewnij się że mamy dokładnie tyle słów ile liter w haśle
        if (placedWords.Count != highlightedWord.Length)
        {
            // Jeśli nadal nie pasuje, to znaczy że nie udało się umieścić wszystkich słów
            // Można dodać logging lub rzucić wyjątek
            // Na razie po prostu zwracamy to co mamy
        }
        
        // Na końcu: przeszukaj całą siatkę i znajdź wszystkie litery hasła
        // To zapewnia, że wszystkie litery (w tym powtarzające się) są poprawnie oznaczone
        FindAndMarkHighlightedWord(grid, highlightedWord, highlightedCellsWithIndices);
        
        return Result<(CrosswordGrid, List<CrosswordWord>, Dictionary<(int, int), int>), string>.Success((grid, placedWords, highlightedCellsWithIndices));
    }
    
    /// <summary>
    /// Próbuje umieścić słowo w siatce, znajdując przecięcie z istniejącym słowem
    /// </summary>
    private (CrosswordWord word, (int row, int col)? letterPosition)? TryPlaceWordForLetter(
        CrosswordGrid grid, List<CrosswordWord> placedWords, string word, 
        char requiredLetter, int letterIndexInHighlightedWord, string highlightedWord)
    {
        // Znajdź wszystkie pozycje gdzie można umieścić to słowo (przecięcie z istniejącym słowem)
        foreach (var placedWord in placedWords)
        {
            for (int i = 0; i < placedWord.Length; i++)
            {
                char placedLetter = placedWord.Word[i];
                
                // Sprawdź czy ta litera występuje w nowym słowie
                if (!word.Contains(placedLetter))
                    continue;
                
                var (letterRow, letterCol) = placedWord.GetCellPositions().ElementAt(i);
                
                // Spróbuj umieścić słowo prostopadle
                WordDirection newDirection = placedWord.Direction == WordDirection.Across 
                    ? WordDirection.Down 
                    : WordDirection.Across;
                
                // Znajdź wszystkie pozycje litery przecięcia w nowym słowie
                var letterPositions = new List<int>();
                for (int j = 0; j < word.Length; j++)
                {
                    if (word[j] == placedLetter)
                    {
                        letterPositions.Add(j);
                    }
                }
                
                foreach (var letterIndex in letterPositions)
                {
                    int startRow, startCol;
                    if (newDirection == WordDirection.Down)
                    {
                        startRow = letterRow - letterIndex;
                        startCol = letterCol;
                    }
                    else
                    {
                        startRow = letterRow;
                        startCol = letterCol - letterIndex;
                    }
                    
                    var candidateWord = new CrosswordWord(0, word, startRow, startCol, newDirection);
                    if (IsValidPlacement(grid, word, startRow, startCol, newDirection) && 
                        HasProperSpacing(grid, candidateWord, placedWords))
                    {
                        // Sprawdź czy słowo zawiera wymaganą literę hasła
                        // Używamy wszystkich wystąpień litery, nie tylko pierwszego
                        var requiredLetterIndices = new List<int>();
                        for (int k = 0; k < word.Length; k++)
                        {
                            if (word[k] == requiredLetter)
                            {
                                requiredLetterIndices.Add(k);
                            }
                        }
                        
                        if (requiredLetterIndices.Count > 0)
                        {
                            // Wybierz pierwsze wystąpienie (lub można losowo)
                            int requiredLetterIndex = requiredLetterIndices[0];
                            var (reqRow, reqCol) = candidateWord.GetCellPositions().ElementAt(requiredLetterIndex);
                            return (candidateWord, (reqRow, reqCol));
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Generuje krzyżówkę z słowami (stara metoda dla kompatybilności)
    /// </summary>
    public CrosswordGrid GenerateWithWordsLegacy(int rows, int columns, int targetWordCount = 5, int maxAttempts = 50)
    {
        var result = GenerateWithWords(rows, columns, targetWordCount, maxAttempts, null);
        if (result.IsFailure)
        {
            // Dla legacy metody zwracamy pustą siatkę w przypadku błędu
            var errorMsg = $"GenerateWithWordsLegacy: Błąd generowania krzyżówki: {result.Error}";
            System.Diagnostics.Debug.WriteLine($"[CURSOR] {errorMsg}");
            return new CrosswordGrid(rows, columns);
        }
        var (grid, _, _) = result.Value;
        return grid;
    }

    /// <summary>
    /// Znajduje słowo prostopadłe dla konkretnej litery (z wieloma próbami)
    /// </summary>
    private CrosswordWord? FindPerpendicularWordForLetter(
        CrosswordWord baseWord, int intersectRow, int intersectCol, char intersectLetter, 
        CrosswordGrid grid, List<CrosswordWord> placedWords, int maxAttempts = 30)
    {
        // Jeśli słowo jest poziome, szukamy pionowego (i odwrotnie)
        WordDirection newDirection = baseWord.Direction == WordDirection.Across 
            ? WordDirection.Down 
            : WordDirection.Across;
        
        // Znajdź wszystkie słowa zawierające literę przecięcia (min 6 liter)
        var candidates = GetWordsContaining(intersectLetter, 6, 15, maxResults: 100);
        if (candidates.Count == 0)
        {
            return null;
        }
        
        // Przetasuj kandydatów dla lepszej losowości
        var shuffledCandidates = candidates.OrderBy(x => _random.Next()).ToList();
        
        // Spróbuj każdego kandydata
        foreach (var word in shuffledCandidates.Take(maxAttempts))
        {
            // Znajdź wszystkie pozycje litery przecięcia w słowie
            var letterPositions = new List<int>();
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == intersectLetter)
                {
                    letterPositions.Add(i);
                }
            }
            
            // Spróbuj każdą pozycję litery w słowie
            foreach (var letterIndex in letterPositions)
            {
                // Oblicz pozycję startową
                int startRow, startCol;
                if (newDirection == WordDirection.Down)
                {
                    startRow = intersectRow - letterIndex;
                    startCol = intersectCol;
                }
                else
                {
                    startRow = intersectRow;
                    startCol = intersectCol - letterIndex;
                }
                
                // Sprawdź czy mieści się w siatce i ma odpowiednie odstępy
                var candidateWord = new CrosswordWord(0, word, startRow, startCol, newDirection);
                if (IsValidPlacement(grid, word, startRow, startCol, newDirection) && 
                    HasProperSpacing(grid, candidateWord, placedWords))
                {
                    return candidateWord;
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Znajduje słowo prostopadłe z ograniczeniami dotyczącymi liter hasła głównego
    /// </summary>
    private CrosswordWord? FindPerpendicularWordWithHighlightedConstraints(
        CrosswordWord baseWord, int intersectRow, int intersectCol, char intersectLetter, 
        CrosswordGrid grid, List<CrosswordWord> placedWords, 
        string? highlightedWord, HashSet<char> highlightedWordLetters)
    {
        if (highlightedWord == null)
        {
            return FindPerpendicularWord(baseWord, intersectRow, intersectCol, intersectLetter, grid, placedWords);
        }
        
        // Jeśli słowo jest poziome, szukamy pionowego (i odwrotnie)
        WordDirection newDirection = baseWord.Direction == WordDirection.Across 
            ? WordDirection.Down 
            : WordDirection.Across;
        
        // Znajdź słowa zawierające literę przecięcia (min 6 liter)
        var candidates = GetWordsContaining(intersectLetter, 6, 15, maxResults: 100);
        if (candidates.Count == 0)
        {
            return null;
        }
        
        // Przetasuj kandydatów
        var shuffledCandidates = candidates.OrderBy(x => _random.Next()).ToList();
        
        // Spróbuj każdego kandydata
        foreach (var word in shuffledCandidates)
        {
            // Sprawdź ile liter hasła zawiera to słowo
            int highlightedLetterCount = word.Count(c => highlightedWordLetters.Contains(c));
            
            // Warunek: przynajmniej 1 litera hasła, maksymalnie 2
            if (highlightedLetterCount < 1 || highlightedLetterCount > 2)
            {
                continue; // Pomiń to słowo
            }
            
            // Znajdź pozycję litery przecięcia w słowie
            int letterIndex = word.IndexOf(intersectLetter);
            if (letterIndex == -1)
            {
                continue;
            }
            
            // Oblicz pozycję startową
            int startRow, startCol;
            if (newDirection == WordDirection.Down)
            {
                startRow = intersectRow - letterIndex;
                startCol = intersectCol;
            }
            else
            {
                startRow = intersectRow;
                startCol = intersectCol - letterIndex;
            }
            
            // Sprawdź czy mieści się w siatce i ma odpowiednie odstępy
            var candidateWord = new CrosswordWord(0, word, startRow, startCol, newDirection);
            if (IsValidPlacement(grid, word, startRow, startCol, newDirection) && 
                HasProperSpacing(grid, candidateWord, placedWords))
            {
                return candidateWord;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Znajduje słowo prostopadłe do istniejącego słowa (pojedyncza próba)
    /// </summary>
    private CrosswordWord? FindPerpendicularWord(CrosswordWord baseWord, int intersectRow, int intersectCol, char intersectLetter, CrosswordGrid grid, List<CrosswordWord> placedWords)
    {
        // Jeśli słowo jest poziome, szukamy pionowego (i odwrotnie)
        WordDirection newDirection = baseWord.Direction == WordDirection.Across 
            ? WordDirection.Down 
            : WordDirection.Across;
        
        // Znajdź słowo zawierające literę przecięcia (min 6 liter)
        var candidates = GetWordsContaining(intersectLetter, 6, 15, maxResults: 100);
        if (candidates.Count == 0)
        {
            return null;
        }
        
        // Losuj z kandydatów
        var word = candidates[_random.Next(candidates.Count)];
        
        // Znajdź pozycję litery przecięcia w nowym słowie
        int letterIndex = word.IndexOf(intersectLetter);
        if (letterIndex == -1)
        {
            return null;
        }
        
        // Oblicz pozycję startową
        int startRow, startCol;
        if (newDirection == WordDirection.Down)
        {
            startRow = intersectRow - letterIndex;
            startCol = intersectCol;
        }
        else
        {
            startRow = intersectRow;
            startCol = intersectCol - letterIndex;
        }
        
        // Sprawdź czy mieści się w siatce i ma odpowiednie odstępy
        var candidateWord = new CrosswordWord(0, word, startRow, startCol, newDirection);
        if (!IsValidPlacement(grid, word, startRow, startCol, newDirection) || 
            !HasProperSpacing(grid, candidateWord, placedWords))
        {
            return null;
        }
        
        return candidateWord;
    }

    /// <summary>
    /// Sprawdza czy słowo może być umieszczone w siatce
    /// </summary>
    private bool IsValidPlacement(CrosswordGrid grid, string word, int row, int col, WordDirection direction)
    {
        if (direction == WordDirection.Across)
        {
            if (col + word.Length > grid.Columns)
                return false;
            
            // Sprawdź czy przed słowem jest pusta kratka lub ściana (lub granica)
            if (col > 0)
            {
                var beforeCell = grid.GetCell(row, col - 1);
                if (beforeCell.HasLetter && !beforeCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź czy po słowie jest pusta kratka lub ściana (lub granica)
            if (col + word.Length < grid.Columns)
            {
                var afterCell = grid.GetCell(row, col + word.Length);
                if (afterCell.HasLetter && !afterCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            for (int i = 0; i < word.Length; i++)
            {
                var cell = grid.GetCell(row, col + i);
                // Może być puste lub mieć tę samą literę (przecięcie)
                if (cell.IsWall)
                    return false;
                if (cell.HasLetter && cell.Letter != word[i])
                    return false;
                
                // Sprawdź czy nie ma liter bezpośrednio obok (prostopadle) - poza przecięciem
                // Górna kratka
                if (row > 0)
                {
                    var topCell = grid.GetCell(row - 1, col + i);
                    if (topCell.HasLetter && !topCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || cell.Letter != word[i])
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
                
                // Dolna kratka
                if (row < grid.Rows - 1)
                {
                    var bottomCell = grid.GetCell(row + 1, col + i);
                    if (bottomCell.HasLetter && !bottomCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || cell.Letter != word[i])
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
            }
        }
        else // Down
        {
            if (row + word.Length > grid.Rows)
                return false;
            
            // Sprawdź czy przed słowem jest pusta kratka lub ściana (lub granica)
            if (row > 0)
            {
                var beforeCell = grid.GetCell(row - 1, col);
                if (beforeCell.HasLetter && !beforeCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            // Sprawdź czy po słowie jest pusta kratka lub ściana (lub granica)
            if (row + word.Length < grid.Rows)
            {
                var afterCell = grid.GetCell(row + word.Length, col);
                if (afterCell.HasLetter && !afterCell.IsWall)
                    return false; // Słowo bezpośrednio obok innego słowa
            }
            
            for (int i = 0; i < word.Length; i++)
            {
                var cell = grid.GetCell(row + i, col);
                if (cell.IsWall)
                    return false;
                if (cell.HasLetter && cell.Letter != word[i])
                    return false;
                
                // Sprawdź czy nie ma liter bezpośrednio obok (prostopadle) - poza przecięciem
                // Lewa kratka
                if (col > 0)
                {
                    var leftCell = grid.GetCell(row + i, col - 1);
                    if (leftCell.HasLetter && !leftCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || cell.Letter != word[i])
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
                
                // Prawa kratka
                if (col < grid.Columns - 1)
                {
                    var rightCell = grid.GetCell(row + i, col + 1);
                    if (rightCell.HasLetter && !rightCell.IsWall)
                    {
                        // To może być przecięcie - sprawdź czy w tym miejscu jest już litera
                        if (!cell.HasLetter || cell.Letter != word[i])
                            return false; // Litera obok, ale nie jest przecięciem
                    }
                }
            }
        }
        
        return true;
    }

    /// <summary>
    /// Sprawdza czy słowo może być umieszczone (z uwzględnieniem przecięć i odstępów)
    /// </summary>
    private bool CanPlaceWord(CrosswordGrid grid, CrosswordWord word, List<CrosswordWord> placedWords)
    {
        return IsValidPlacement(grid, word.Word, word.Row, word.Column, word.Direction) &&
               HasProperSpacing(grid, word, placedWords);
    }

    /// <summary>
    /// Sprawdza czy słowo ma odpowiednie odstępy od innych słów (nie jest bezpośrednio obok)
    /// </summary>
    private bool HasProperSpacing(CrosswordGrid grid, CrosswordWord word, List<CrosswordWord> placedWords)
    {
        var wordPositions = word.GetCellPositions().ToHashSet();
        
        foreach (var placedWord in placedWords)
        {
            // Jeśli słowa są w tym samym kierunku, sprawdź czy nie są obok siebie
            if (placedWord.Direction == word.Direction)
            {
                var placedPositions = placedWord.GetCellPositions().ToHashSet();
                
                // Sprawdź czy słowa się przecinają (to jest OK)
                if (wordPositions.Intersect(placedPositions).Any())
                    continue; // Przecięcie jest OK
                
                // Sprawdź czy słowa są bezpośrednio obok siebie
                if (word.IsHorizontal && placedWord.IsHorizontal && word.Row == placedWord.Row)
                {
                    // Te same wiersze - sprawdź czy są obok siebie
                    int wordStart = word.Column;
                    int wordEnd = word.Column + word.Length - 1;
                    int placedStart = placedWord.Column;
                    int placedEnd = placedWord.Column + placedWord.Length - 1;
                    
                    // Sprawdź czy są bezpośrednio obok (bez pustej kratki między)
                    if (Math.Abs(wordStart - placedEnd) == 1 || Math.Abs(placedStart - wordEnd) == 1)
                    {
                        return false; // Słowa są bezpośrednio obok siebie
                    }
                }
                else if (word.IsVertical && placedWord.IsVertical && word.Column == placedWord.Column)
                {
                    // Te same kolumny - sprawdź czy są obok siebie
                    int wordStart = word.Row;
                    int wordEnd = word.Row + word.Length - 1;
                    int placedStart = placedWord.Row;
                    int placedEnd = placedWord.Row + placedWord.Length - 1;
                    
                    // Sprawdź czy są bezpośrednio obok (bez pustej kratki między)
                    if (Math.Abs(wordStart - placedEnd) == 1 || Math.Abs(placedStart - wordEnd) == 1)
                    {
                        return false; // Słowa są bezpośrednio obok siebie
                    }
                }
            }
        }
        
        return true;
    }

    /// <summary>
    /// Umieszcza słowo w siatce
    /// </summary>
    private void PlaceWord(CrosswordGrid grid, CrosswordWord word)
    {
        // DEBUG: Sprawdź czy słowo ma polskie znaki
        if (word.Word.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)))
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] PlaceWord: Umieszczam słowo z polskimi znakami: '{word.Word}'");
            foreach (var letter in word.Word.Where(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)))
            {
                System.Diagnostics.Debug.WriteLine($"[CURSOR] PlaceWord: Polska litera '{letter}' (Unicode: U+{(int)letter:X4})");
            }
        }
        
        for (int i = 0; i < word.Length; i++)
        {
            int row, col;
            if (word.IsHorizontal)
            {
                row = word.Row;
                col = word.Column + i;
            }
            else
            {
                row = word.Row + i;
                col = word.Column;
            }
            
            grid.SetLetter(row, col, word.Word[i]);
        }
    }

    /// <summary>
    /// Generuje listę słów użytych w krzyżówce (dla datasetu)
    /// </summary>
    public List<CrosswordWord> GetPlacedWords(CrosswordGrid grid)
    {
        // To będzie potrzebne do ekstrakcji słów z już umieszczonej siatki
        // Na razie zwracamy pustą listę - można rozszerzyć później
        return new List<CrosswordWord>();
    }

    /// <summary>
    /// Znajduje litery hasła głównego w siatce i oznacza je numerkami
    /// Hasło może być rozproszone - każda litera może być w dowolnym miejscu
    /// Ważne: obsługuje powtarzające się litery w haśle (np. "SAMOCHÓD" ma dwie litery "O")
    /// </summary>
    private void FindAndMarkHighlightedWord(CrosswordGrid grid, string highlightedWord, Dictionary<(int row, int col), int> highlightedCellsWithIndices)
    {
        // Wyczyść istniejące oznaczenia (na wypadek ponownego wywołania)
        highlightedCellsWithIndices.Clear();
        
        // Znajdź wszystkie pozycje każdej litery hasła w siatce
        var letterPositions = new Dictionary<char, List<(int row, int col)>>();
        
        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Columns; c++)
            {
                var cell = grid.GetCell(r, c);
                if (cell.HasLetter)
                {
                    char letter = cell.Letter!.Value;
                    if (!letterPositions.ContainsKey(letter))
                    {
                        letterPositions[letter] = new List<(int row, int col)>();
                    }
                    letterPositions[letter].Add((r, c));
                }
            }
        }
        
        // Dla każdej litery hasła (w kolejności), znajdź jej pozycję w siatce
        // Używamy HashSet do śledzenia już użytych pozycji, żeby nie używać tej samej pozycji dwa razy
        var usedPositions = new HashSet<(int row, int col)>();
        
        // Przechodzimy przez hasło litera po literze (w kolejności)
        for (int i = 0; i < highlightedWord.Length; i++)
        {
            char letter = highlightedWord[i];
            int letterIndex = i + 1; // Numeracja od 1
            
            // Znajdź pozycję tej litery w siatce (która jeszcze nie jest użyta)
            if (letterPositions.ContainsKey(letter))
            {
                var availablePositions = letterPositions[letter]
                    .Where(pos => !usedPositions.Contains(pos))
                    .ToList();
                
                if (availablePositions.Count > 0)
                {
                    // Wybierz losową pozycję z dostępnych
                    var selectedPos = availablePositions[_random.Next(availablePositions.Count)];
                    highlightedCellsWithIndices[selectedPos] = letterIndex;
                    usedPositions.Add(selectedPos);
                }
                else
                {
                    // Jeśli nie ma dostępnych pozycji dla tej litery, to znaczy że wszystkie wystąpienia są już użyte
                    // To może się zdarzyć gdy litera występuje w haśle więcej razy niż w siatce
                    // W takim przypadku możemy użyć już użytej pozycji, ale z nowym numerkiem
                    // (to oznacza że ta sama litera w siatce reprezentuje dwie różne pozycje w haśle)
                    if (letterPositions[letter].Count > 0)
                    {
                        // Użyj pierwszej dostępnej pozycji (nawet jeśli już jest użyta)
                        // To pozwoli na oznaczenie wszystkich liter hasła, nawet jeśli niektóre pozycje są współdzielone
                        var fallbackPos = letterPositions[letter][0];
                        highlightedCellsWithIndices[fallbackPos] = letterIndex;
                        // Nie dodajemy do usedPositions, żeby kolejne wystąpienia tej samej litery mogły też użyć tej pozycji
                    }
                }
            }
        }
        
        // WALIDACJA: Sprawdź czy wszystkie litery hasła zostały zaznaczone
        // Sprawdzamy czy wszystkie indeksy (1, 2, 3, ...) są obecne w wartościach słownika
        var foundIndices = highlightedCellsWithIndices.Values.Distinct().ToHashSet();
        var expectedIndices = Enumerable.Range(1, highlightedWord.Length).ToHashSet();
        var missingIndices = expectedIndices.Except(foundIndices).ToList();
        
        // Jeśli brakuje indeksów, spróbuj je znaleźć używając fallback
        if (missingIndices.Count > 0)
        {
            foreach (var missingIndex in missingIndices)
            {
                int letterPosition = missingIndex - 1; // Konwersja na indeks (0-based)
                if (letterPosition >= 0 && letterPosition < highlightedWord.Length)
                {
                    char letter = highlightedWord[letterPosition];
                    if (letterPositions.ContainsKey(letter) && letterPositions[letter].Count > 0)
                    {
                        // Użyj pierwszej dostępnej pozycji (nawet jeśli już jest użyta)
                        var fallbackPos = letterPositions[letter][0];
                        highlightedCellsWithIndices[fallbackPos] = missingIndex;
                    }
                }
            }
        }
        
        // Ostateczna walidacja: upewnij się że wszystkie pozycje hasła są zaznaczone
        var finalFoundIndices = highlightedCellsWithIndices.Values.Distinct().ToHashSet();
        var stillMissing = expectedIndices.Except(finalFoundIndices).ToList();
        
        if (stillMissing.Count > 0)
        {
            // Jeśli nadal brakuje, oznacza to że niektóre litery hasła nie są w siatce
            // To nie powinno się zdarzyć jeśli słowa zostały poprawnie wybrane i umieszczone
            // Można dodać logging lub rzucić wyjątek w przyszłości
        }
    }

    /// <summary>
    /// Zwraca zbiór liter hasła które są już użyte w słowach
    /// </summary>
    private HashSet<char> GetUsedHighlightedLetters(List<CrosswordWord> placedWords, HashSet<char> highlightedWordLetters)
    {
        var usedLetters = new HashSet<char>();
        foreach (var word in placedWords)
        {
            foreach (char wordLetter in word.Word)
            {
                if (highlightedWordLetters.Contains(wordLetter))
                {
                    usedLetters.Add(wordLetter);
                }
            }
        }
        return usedLetters;
    }

    /// <summary>
    /// Próbuje umieścić słowo zawierające określoną literę hasła
    /// </summary>
    private CrosswordWord? TryPlaceWordWithLetter(
        CrosswordGrid grid, List<CrosswordWord> placedWords, string word, 
        char requiredLetter, HashSet<char> highlightedWordLetters)
    {
        // Znajdź wszystkie pozycje gdzie można umieścić to słowo (przecięcie z istniejącym słowem)
        foreach (var placedWord in placedWords)
        {
            for (int i = 0; i < placedWord.Length; i++)
            {
                char placedLetter = placedWord.Word[i];
                
                // Sprawdź czy ta litera występuje w nowym słowie
                if (!word.Contains(placedLetter))
                    continue;
                
                var (letterRow, letterCol) = placedWord.GetCellPositions().ElementAt(i);
                
                // Spróbuj umieścić słowo prostopadle
                WordDirection newDirection = placedWord.Direction == WordDirection.Across 
                    ? WordDirection.Down 
                    : WordDirection.Across;
                
                int letterIndex = word.IndexOf(placedLetter);
                if (letterIndex == -1)
                    continue;
                
                int startRow, startCol;
                if (newDirection == WordDirection.Down)
                {
                    startRow = letterRow - letterIndex;
                    startCol = letterCol;
                }
                else
                {
                    startRow = letterRow;
                    startCol = letterCol - letterIndex;
                }
                
                var candidateWord = new CrosswordWord(0, word, startRow, startCol, newDirection);
                if (IsValidPlacement(grid, word, startRow, startCol, newDirection) && 
                    HasProperSpacing(grid, candidateWord, placedWords))
                {
                    // Sprawdź czy słowo zawiera wymaganą literę
                    if (word.Contains(requiredLetter))
                    {
                        return candidateWord;
                    }
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Znajduje pierwsze słowo które zawiera 1-2 litery hasła głównego
    /// </summary>
    private string FindFirstWordWithHighlightedConstraints(
        string highlightedWord, HashSet<char> highlightedWordLetters, int maxColumns, int minWordLength)
    {
        // Spróbuj znaleźć słowo zawierające 1-2 litery hasła
        var allWords = new List<string>();
        for (int i = 0; i < 200; i++) // Próbuj max 200 razy
        {
            var word = GetRandomWord();
            if (word.Length >= minWordLength && word.Length <= maxColumns)
            {
                int highlightedCount = word.Count(c => highlightedWordLetters.Contains(c));
                if (highlightedCount >= 1 && highlightedCount <= 2)
                {
                    return word;
                }
                allWords.Add(word);
            }
        }
        
        // Jeśli nie znalazło, użyj pierwszego słowa które zawiera przynajmniej jedną literę hasła
        foreach (var word in allWords)
        {
            int highlightedCount = word.Count(c => highlightedWordLetters.Contains(c));
            if (highlightedCount >= 1)
            {
                return word;
            }
        }
        
        // Ostatecznie zwróć losowe słowo
        return GetRandomWordWithMinLength(minWordLength);
    }

    /// <summary>
    /// Losuje słowo o minimalnej długości
    /// </summary>
    private string GetRandomWordWithMinLength(int minLength)
    {
        var candidates = new List<string>();
        for (int i = 0; i < 100; i++) // Próbuj max 100 razy
        {
            var word = GetRandomWord();
            if (word.Length >= minLength)
            {
                return word;
            }
            candidates.Add(word);
        }
        
        // Jeśli nie znalazło w 100 próbach, użyj najdłuższego z prób
        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(w => w.Length).First();
        }
        
        // Ostatecznie zwróć losowe słowo
        return GetRandomWord();
    }
}

