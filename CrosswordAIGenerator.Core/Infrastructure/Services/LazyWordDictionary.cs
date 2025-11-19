using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Globalization;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Leniwe ładowanie słownika - wczytuje słowa z pliku tylko gdy są potrzebne
/// Używa indeksu pozycji w pliku zamiast ładowania całego słownika do pamięci
/// Implementacja z I/O - w Infrastructure
/// </summary>
public class LazyWordDictionary : IWordDictionary
{
    private readonly string _filePath;
    private readonly bool _isGzip;
    private readonly int _minWordLength;
    private readonly Random _random;
    private readonly ICursorLogger? _logger;
    
    // Indeks: litera -> lista offsetów w pliku (pozycje gdzie zaczynają się słowa zawierające tę literę)
    private readonly Dictionary<char, List<long>> _letterOffsets = new();
    private readonly Dictionary<char, List<int>> _letterLineNumbers = new(); // Alternatywa: numer linii zamiast offsetu
    private int _totalLines = 0;
    private bool _indexLoaded = false;
    private readonly object _indexLock = new();
    
    // Cache wczytanych słów (lineNumber -> word) - przyspiesza wielokrotne odczyty
    private readonly Dictionary<int, string> _wordCache = new();
    private readonly object _cacheLock = new();
    
    // Pre-loaded words dla każdej litery (cache większej próbki)
    private readonly Dictionary<char, List<string>> _preloadedWords = new();
    private readonly object _preloadLock = new();

    public LazyWordDictionary(string filePath, int? seed = null, int minWordLength = 6, ICursorLogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Słownik nie istnieje: {filePath}");
        }

        _filePath = filePath;
        _isGzip = filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        _minWordLength = minWordLength;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _logger = logger;
        
        _logger?.InfoFormat("LazyWordDictionary utworzony: {0}, GZip: {1}, MinLength: {2}", filePath, _isGzip, minWordLength);
    }

    /// <summary>
    /// Ładuje indeks pozycji w pliku (szybko, bez wczytywania całego pliku)
    /// </summary>
    public void LoadIndex()
    {
        if (_indexLoaded)
            return;

        lock (_indexLock)
        {
            if (_indexLoaded)
                return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger?.Info("LoadIndex: Rozpoczynam ładowanie indeksu...");
            
            if (_isGzip)
            {
                LoadIndexFromGzip();
            }
            else
            {
                LoadIndexFromText();
            }

            stopwatch.Stop();
            _indexLoaded = true;
            _logger?.InfoFormat("LoadIndex: Zakończono ładowanie indeksu w {0}ms. Znaleziono {1} różnych liter, {2} linii", 
                stopwatch.ElapsedMilliseconds, _letterLineNumbers.Keys.Count, _totalLines);
            System.Diagnostics.Debug.WriteLine($"[CURSOR] LoadIndex: Zakończono w {stopwatch.ElapsedMilliseconds}ms. Litery: {_letterLineNumbers.Keys.Count}, Linie: {_totalLines}");
        }
    }

    private void LoadIndexFromText()
    {
        // Zwiększ buffer size dla szybszego odczytu
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: 65536);
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            var word = line.Trim().ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
            
            if (string.IsNullOrWhiteSpace(word) || word.Length < _minWordLength)
                continue;

            // Sprawdź czy wszystkie znaki to litery (w tym polskie znaki diakretyczne)
            // WAŻNE: Po ToUpper wszystkie litery są wielkie, więc sprawdzamy tylko wielkie polskie znaki
            // char.IsLetter() powinno rozpoznawać polskie znaki, ale dla pewności dodajemy też explicit check
            if (!word.All(c => char.IsLetter(c) || "ĄĆĘŁŃÓŚŹŻ".Contains(c)))
            {
                _logger?.DebugFormat("Odrzucono słowo (nie wszystkie znaki to litery): '{0}'", word);
                continue;
            }

            // Zapisz numer linii dla każdej litery w słowie
            var uniqueLetters = word.Distinct();
            bool hasPolishLetters = uniqueLetters.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c));
            
            if (hasPolishLetters && lineNumber <= 100) // Loguj pierwsze 100 słów z polskimi znakami
            {
                _logger?.DebugFormat("LoadIndexFromText: Słowo '{0}' (linia {1}) zawiera polskie znaki: {2}", 
                    word, lineNumber, string.Join(", ", uniqueLetters.Where(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c))));
            }
            
            foreach (var letter in uniqueLetters)
            {
                if (!_letterLineNumbers.ContainsKey(letter))
                {
                    _letterLineNumbers[letter] = new List<int>();
                    if ("ĄĆĘŁŃÓŚŹŻ".Contains(letter))
                    {
                        _logger?.InfoFormat("LoadIndexFromText: Dodano polską literę '{0}' (Unicode: U+{1:X4}) do indeksu", 
                            letter, (int)letter);
                    }
                }
                _letterLineNumbers[letter].Add(lineNumber);
            }
        }

        // DEBUG: Pokaż wszystkie polskie litery w indeksie
        var polishLettersInIndex = _letterLineNumbers.Keys.Where(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)).ToList();
        if (polishLettersInIndex.Count > 0)
        {
            _logger?.InfoFormat("LoadIndexFromText: Polskie litery w indeksie: {0} (łącznie {1} różnych liter)", 
                string.Join(", ", polishLettersInIndex), _letterLineNumbers.Keys.Count);
        }
        else
        {
            _logger?.Warning("LoadIndexFromText: BRAK polskich liter w indeksie! Dostępne litery: " + 
                string.Join(", ", _letterLineNumbers.Keys.OrderBy(k => k).Take(30)));
        }

        _totalLines = lineNumber;
    }

    private void LoadIndexFromGzip()
    {
        // Zwiększ buffer size dla szybszego odczytu
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8, bufferSize: 65536);
        
        string? line;
        int lineNumber = 0;
        int polishWordsFound = 0;
        int totalLinesProcessed = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            var originalLine = line.Trim();
            totalLinesProcessed++;
            
            // DEBUG: Loguj pierwsze 10 linii niezależnie od zawartości
            if (lineNumber <= 10)
            {
                _logger?.InfoFormat("LoadIndexFromGzip: Linia {0}: '{1}' (długość={2})", 
                    lineNumber, originalLine, originalLine.Length);
            }
            
            // DEBUG: Sprawdź czy oryginalna linia ma polskie znaki PRZED jakimikolwiek filtrami
            bool originalHasPolish = originalLine.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c));
            if (originalHasPolish)
            {
                polishWordsFound++;
                if (polishWordsFound <= 20)
                {
                    _logger?.InfoFormat("LoadIndexFromGzip: Linia {0}: Oryginał='{1}' (długość={2}) - MA POLSKIE ZNAKI!", 
                        lineNumber, originalLine, originalLine.Length);
                }
            }
            
            // DEBUG: Sprawdź też co 10000 linii, żeby zobaczyć czy w ogóle są polskie znaki
            if (lineNumber % 10000 == 0)
            {
                _logger?.InfoFormat("LoadIndexFromGzip: Przetworzono {0} linii, znaleziono {1} słów z polskimi znakami", 
                    lineNumber, polishWordsFound);
            }
            
            var word = originalLine.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
            
            if (originalHasPolish && polishWordsFound <= 20)
            {
                _logger?.InfoFormat("LoadIndexFromGzip: Linia {0}: Po ToUpper='{1}' (długość={2})", 
                    lineNumber, word, word.Length);
            }
            
            if (string.IsNullOrWhiteSpace(word) || word.Length < _minWordLength)
            {
                if (originalHasPolish && polishWordsFound <= 20)
                {
                    _logger?.WarningFormat("LoadIndexFromGzip: Odrzucono słowo z polskimi znakami (puste lub za krótkie): '{0}' -> '{1}' (długość={2}, minLength={3})", 
                        originalLine, word, word.Length, _minWordLength);
                }
                continue;
            }

            // Sprawdź czy wszystkie znaki to litery (w tym polskie znaki diakretyczne)
            // WAŻNE: Po ToUpper wszystkie litery są wielkie, więc sprawdzamy tylko wielkie polskie znaki
            // char.IsLetter() powinno rozpoznawać polskie znaki, ale dla pewności dodajemy też explicit check
            bool allLetters = word.All(c => char.IsLetter(c) || "ĄĆĘŁŃÓŚŹŻ".Contains(c));
            if (!allLetters)
            {
                if (originalHasPolish && polishWordsFound <= 20)
                {
                    _logger?.WarningFormat("LoadIndexFromGzip: Odrzucono słowo z polskimi znakami (nie wszystkie znaki to litery): '{0}' -> '{1}'", 
                        originalLine, word);
                }
                _logger?.DebugFormat("Odrzucono słowo (nie wszystkie znaki to litery): '{0}'", word);
                continue;
            }

            // Zapisz numer linii dla każdej litery w słowie
            var uniqueLetters = word.Distinct();
            bool hasPolishLetters = uniqueLetters.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c));
            
            if (hasPolishLetters && lineNumber <= 100) // Loguj pierwsze 100 słów z polskimi znakami
            {
                _logger?.DebugFormat("LoadIndexFromGzip: Słowo '{0}' (linia {1}) zawiera polskie znaki: {2}", 
                    word, lineNumber, string.Join(", ", uniqueLetters.Where(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c))));
            }
            
            foreach (var letter in uniqueLetters)
            {
                if (!_letterLineNumbers.ContainsKey(letter))
                {
                    _letterLineNumbers[letter] = new List<int>();
                    if ("ĄĆĘŁŃÓŚŹŻ".Contains(letter))
                    {
                        _logger?.InfoFormat("LoadIndexFromGzip: Dodano polską literę '{0}' (Unicode: U+{1:X4}) do indeksu", 
                            letter, (int)letter);
                    }
                }
                _letterLineNumbers[letter].Add(lineNumber);
            }
        }

        // DEBUG: Pokaż wszystkie polskie litery w indeksie
        var polishLettersInIndex = _letterLineNumbers.Keys.Where(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)).ToList();
        _logger?.InfoFormat("LoadIndexFromGzip: Przetworzono {0} linii, znaleziono {1} słów z polskimi znakami", 
            totalLinesProcessed, polishWordsFound);
        
        if (polishLettersInIndex.Count > 0)
        {
            _logger?.InfoFormat("LoadIndexFromGzip: Polskie litery w indeksie: {0} (łącznie {1} różnych liter)", 
                string.Join(", ", polishLettersInIndex), _letterLineNumbers.Keys.Count);
        }
        else
        {
            _logger?.Warning("LoadIndexFromGzip: BRAK polskich liter w indeksie! Dostępne litery: " + 
                string.Join(", ", _letterLineNumbers.Keys.OrderBy(k => k).Take(30)));
        }

        _totalLines = lineNumber;
    }

    /// <summary>
    /// Wczytuje konkretną linię z pliku (bez wczytywania całego pliku) - z cache
    /// </summary>
    private string? ReadLineAt(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _totalLines)
            return null;

        // Sprawdź cache
        lock (_cacheLock)
        {
            if (_wordCache.TryGetValue(lineNumber, out var cachedWord))
            {
                return cachedWord;
            }
        }

        // Wczytaj z pliku
        string? word = null;
        if (_isGzip)
        {
            word = ReadLineAtFromGzip(lineNumber);
        }
        else
        {
            word = ReadLineAtFromText(lineNumber);
        }

        // Zapisz w cache
        if (word != null)
        {
            lock (_cacheLock)
            {
                if (!_wordCache.ContainsKey(lineNumber))
                {
                    _wordCache[lineNumber] = word;
                }
            }
        }

        return word;
    }
    
    /// <summary>
    /// Wczytuje wiele linii na raz (batch loading) - szybsze niż pojedyncze odczyty
    /// </summary>
    private Dictionary<int, string> ReadLinesBatch(List<int> lineNumbers)
    {
        var results = new Dictionary<int, string>();
        
        // Sprawdź cache dla wszystkich linii
        var linesToRead = new List<int>();
        lock (_cacheLock)
        {
            foreach (var lineNumber in lineNumbers)
            {
                if (_wordCache.TryGetValue(lineNumber, out var cachedWord))
                {
                    results[lineNumber] = cachedWord;
                }
                else
                {
                    linesToRead.Add(lineNumber);
                }
            }
        }

        if (linesToRead.Count == 0)
            return results;

        // Posortuj numery linii dla efektywnego wczytywania
        var sortedLines = linesToRead.OrderBy(x => x).ToList();

        if (_isGzip)
        {
            ReadLinesBatchFromGzip(sortedLines, results);
        }
        else
        {
            ReadLinesBatchFromText(sortedLines, results);
        }

        // Zapisz w cache
        lock (_cacheLock)
        {
            foreach (var kvp in results)
            {
                if (!_wordCache.ContainsKey(kvp.Key))
                {
                    _wordCache[kvp.Key] = kvp.Value;
                }
            }
        }

        return results;
    }

    private void ReadLinesBatchFromText(List<int> sortedLineNumbers, Dictionary<int, string> results)
    {
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: 8192);
        
        int currentLine = 1;
        int nextTargetIndex = 0;

        while (nextTargetIndex < sortedLineNumbers.Count)
        {
            int targetLine = sortedLineNumbers[nextTargetIndex];
            
            // Przeskocz do żądanej linii
            while (currentLine < targetLine)
            {
                reader.ReadLine();
                currentLine++;
            }

            // Wczytaj linię
            var line = reader.ReadLine();
            if (line != null)
            {
                var originalLine = line.Trim();
                var word = originalLine.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
                
                // DEBUG: Sprawdź czy polskie znaki są zachowane
                if (originalLine.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c)))
                {
                    _logger?.DebugFormat("Wczytano słowo z polskimi znakami - Oryginał: '{0}', Po ToUpper: '{1}'", originalLine, word);
                    
                    // Sprawdź czy ToUpper zachował polskie znaki
                    var hasPolishAfter = word.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c));
                    if (!hasPolishAfter && originalLine.Any(c => "łąćęńóśźż".Contains(c)))
                    {
                        _logger?.WarningFormat("UWAGA: ToUpper stracił polskie znaki! Oryginał: '{0}', Po ToUpper: '{1}'", originalLine, word);
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(word))
                {
                    results[targetLine] = word;
                }
                currentLine++;
            }

            nextTargetIndex++;
        }
    }

    private void ReadLinesBatchFromGzip(List<int> sortedLineNumbers, Dictionary<int, string> results)
    {
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8, bufferSize: 8192);
        
        int currentLine = 1;
        int nextTargetIndex = 0;

        while (nextTargetIndex < sortedLineNumbers.Count)
        {
            int targetLine = sortedLineNumbers[nextTargetIndex];
            
            // Przeskocz do żądanej linii
            while (currentLine < targetLine)
            {
                reader.ReadLine();
                currentLine++;
            }

            // Wczytaj linię
            var line = reader.ReadLine();
            if (line != null)
            {
                var originalLine = line.Trim();
                var word = originalLine.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
                
                // DEBUG: Sprawdź czy polskie znaki są zachowane
                if (originalLine.Any(c => "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(c)))
                {
                    _logger?.DebugFormat("Wczytano słowo z polskimi znakami - Oryginał: '{0}', Po ToUpper: '{1}'", originalLine, word);
                    
                    // Sprawdź czy ToUpper zachował polskie znaki
                    var hasPolishAfter = word.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c));
                    if (!hasPolishAfter && originalLine.Any(c => "łąćęńóśźż".Contains(c)))
                    {
                        _logger?.WarningFormat("UWAGA: ToUpper stracił polskie znaki! Oryginał: '{0}', Po ToUpper: '{1}'", originalLine, word);
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(word))
                {
                    results[targetLine] = word;
                }
                currentLine++;
            }

            nextTargetIndex++;
        }
    }

    private string? ReadLineAtFromText(int lineNumber)
    {
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: 8192);
        
        for (int i = 1; i < lineNumber; i++)
        {
            reader.ReadLine(); // Przeskocz do żądanej linii
        }

        return reader.ReadLine()?.Trim().ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
    }

    private string? ReadLineAtFromGzip(int lineNumber)
    {
        using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8, bufferSize: 8192);
        
        for (int i = 1; i < lineNumber; i++)
        {
            reader.ReadLine(); // Przeskocz do żądanej linii
        }

        return reader.ReadLine()?.Trim().ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
    }

    // Implementacja IWordDictionary

    /// <summary>
    /// Losuje słowo zawierające określoną literę
    /// Używa pre-loaded cache dla lepszej wydajności
    /// </summary>
    public string? GetRandomWordContaining(char letter, int minLength = 6, int maxLength = 20)
    {
        LoadIndex(); // Upewnij się że indeks jest załadowany

        letter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
        if (!_letterLineNumbers.ContainsKey(letter) || _letterLineNumbers[letter].Count == 0)
        {
            return null;
        }

        // Najpierw sprawdź pre-loaded cache
        lock (_preloadLock)
        {
            if (_preloadedWords.ContainsKey(letter) && _preloadedWords[letter].Count > 0)
            {
                var cached = _preloadedWords[letter]
                    .Where(w => w.Length >= minLength && w.Length <= maxLength)
                    .ToList();
                
                if (cached.Count > 0)
                {
                    var selected = cached[_random.Next(cached.Count)];
                    // DEBUG: Sprawdź czy wybrane słowo ma polskie znaki
                    if (selected.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)))
                    {
                        _logger?.DebugFormat("GetRandomWordContaining (cache): Wybrano słowo z polskimi znakami: '{0}' dla litery '{1}'", selected, letter);
                    }
                    return selected;
                }
            }
        }

        // Jeśli nie ma w cache, wczytaj z pliku (ale użyj batch loading)
        var lineNumbers = _letterLineNumbers[letter];
        int maxAttempts = Math.Min(50, lineNumbers.Count); // Zmniejszona liczba prób
        
        // Wczytaj próbkę linii na raz (batch) - użyj większej próbki dla lepszej różnorodności
        var sampleSize = Math.Min(100, lineNumbers.Count); // Zwiększona z 50 do 100
        var sampleLineNumbers = ShuffleList(lineNumbers).Take(sampleSize).ToList();
        var wordsDict = ReadLinesBatch(sampleLineNumbers);
        
        // Znajdź pasujące słowo
        var candidates = new List<string>();
        foreach (var lineNumber in sampleLineNumbers)
        {
            if (wordsDict.TryGetValue(lineNumber, out var word) && 
                word.Length >= minLength && word.Length <= maxLength)
            {
                candidates.Add(word);
            }
        }

        if (candidates.Count > 0)
        {
            // Użyj lepszego losowania - wymieszaj kandydatów przed wyborem
            var shuffledCandidates = ShuffleList(candidates);
            var selected = shuffledCandidates[0];
            
            // DEBUG: Sprawdź czy wybrane słowo ma polskie znaki
            if (selected.Any(c => "ĄĆĘŁŃÓŚŹŻ".Contains(c)))
            {
                _logger?.DebugFormat("GetRandomWordContaining (file): Wybrano słowo z polskimi znakami: '{0}' dla litery '{1}'", selected, letter);
            }
            
            // Zapisz w pre-loaded cache
            lock (_preloadLock)
            {
                if (!_preloadedWords.ContainsKey(letter))
                {
                    _preloadedWords[letter] = new List<string>();
                }
                if (!_preloadedWords[letter].Contains(selected) && _preloadedWords[letter].Count < 500)
                {
                    _preloadedWords[letter].Add(selected);
                }
            }
            
            return selected;
        }

        return null;
    }

    /// <summary>
    /// Zwraca listę słów zawierających określoną literę (lazy - wczytuje tylko próbkę)
    /// Używa batch loading i cache dla lepszej wydajności
    /// </summary>
    public List<string> GetWordsContaining(char letter, int minLength = 6, int maxLength = 20, int maxResults = 100)
    {
        LoadIndex(); // Upewnij się że indeks jest załadowany

        var originalLetter = letter;
        letter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
        
        // DEBUG: Logowanie dla polskich znaków
        if ("ĄĆĘŁŃÓŚŹŻ".Contains(letter) || "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(originalLetter))
        {
            _logger?.DebugFormat("GetWordsContaining: Litera '{0}' (Unicode: U+{1:X4}) -> po ToUpper: '{2}' (Unicode: U+{3:X4})", 
                originalLetter, (int)originalLetter, letter, (int)letter);
        }
        
        if (!_letterLineNumbers.ContainsKey(letter))
        {
            _logger?.WarningFormat("GetWordsContaining: Litera '{0}' (po ToUpper: '{1}') NIE ZNALEZIONA w indeksie. Dostępne litery w indeksie: {2}", 
                originalLetter, letter, string.Join(", ", _letterLineNumbers.Keys.OrderBy(k => k).Take(20)));
            return new List<string>();
        }
        
        if (_letterLineNumbers[letter].Count == 0)
        {
            _logger?.WarningFormat("GetWordsContaining: Litera '{0}' (po ToUpper: '{1}') jest w indeksie, ale ma 0 linii", 
                originalLetter, letter);
            return new List<string>();
        }
        
        _logger?.DebugFormat("GetWordsContaining: Litera '{0}' (po ToUpper: '{1}') -> {2} linii w indeksie", 
            originalLetter, letter, _letterLineNumbers[letter].Count);

        // Sprawdź pre-loaded cache
        lock (_preloadLock)
        {
            if (_preloadedWords.ContainsKey(letter))
            {
                var cached = _preloadedWords[letter]
                    .Where(w => w.Length >= minLength && w.Length <= maxLength)
                    .ToList();
                
                if (cached.Count > 0)
                {
                    // Użyj Fisher-Yates shuffle dla lepszej losowości
                    var shuffled = ShuffleList(cached);
                    return shuffled.Take(maxResults).ToList();
                }
            }
        }

        var results = new List<string>();
        var lineNumbers = _letterLineNumbers[letter];
        
        // Wymieszaj numery linii losowo (Fisher-Yates shuffle) i weź większą próbkę (3x maxResults dla lepszej różnorodności)
        var shuffledLineNumbers = ShuffleList(lineNumbers).Take(Math.Min(maxResults * 3, lineNumbers.Count)).ToList();
        
        // Wczytaj wszystkie linie na raz (batch loading)
        var wordsDict = ReadLinesBatch(shuffledLineNumbers);
        
        // Filtruj i dodaj do wyników
        foreach (var lineNumber in shuffledLineNumbers)
        {
            if (wordsDict.TryGetValue(lineNumber, out var word) && 
                word.Length >= minLength && word.Length <= maxLength)
            {
                results.Add(word);
                if (results.Count >= maxResults)
                    break;
            }
        }
        
        // DEBUG: Logowanie wyników
        if (results.Count == 0 && ("ĄĆĘŁŃÓŚŹŻ".Contains(letter) || "łąćęńóśźżŁĄĆĘŃÓŚŹŻ".Contains(originalLetter)))
        {
            _logger?.WarningFormat("GetWordsContaining: Litera '{0}' (po ToUpper: '{1}') -> 0 wyników po filtrowaniu (minLength={2}, maxLength={3})", 
                originalLetter, letter, minLength, maxLength);
        }
        else if (results.Count > 0)
        {
            _logger?.DebugFormat("GetWordsContaining: Litera '{0}' (po ToUpper: '{1}') -> {2} wyników", 
                originalLetter, letter, results.Count);
        }

        // Zapisz w pre-loaded cache (dla przyszłych wywołań)
        if (results.Count > 0)
        {
            lock (_preloadLock)
            {
                if (!_preloadedWords.ContainsKey(letter))
                {
                    _preloadedWords[letter] = new List<string>();
                }
                // Dodaj nowe słowa do cache (max 500 słów na literę)
                foreach (var word in results)
                {
                    if (!_preloadedWords[letter].Contains(word) && _preloadedWords[letter].Count < 500)
                    {
                        _preloadedWords[letter].Add(word);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Losuje dowolne słowo z całego słownika
    /// </summary>
    public string? GetRandomWord(int minLength = 6, int maxLength = 20)
    {
        LoadIndex(); // Upewnij się że indeks jest załadowany

        if (_totalLines == 0)
            return null;

        // Losuj numer linii
        int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int randomLineNumber = _random.Next(1, _totalLines + 1);
            var word = ReadLineAt(randomLineNumber);
            
            if (word != null && word.Length >= minLength && word.Length <= maxLength)
            {
                return word;
            }
        }

        return null;
    }

    /// <summary>
    /// Pre-loaduje słowa dla podanych liter (przyspiesza pierwsze generowanie)
    /// </summary>
    public void PreloadWordsForLetters(IEnumerable<char> letters, int wordsPerLetter = 200)
    {
        LoadIndex(); // Upewnij się że indeks jest załadowany
        
        var uniqueLetters = letters.Select(c => char.ToUpper(c, CultureInfo.GetCultureInfo("pl-PL"))).Distinct().ToList();
        
        foreach (var letter in uniqueLetters)
        {
            if (!_letterLineNumbers.ContainsKey(letter) || _letterLineNumbers[letter].Count == 0)
                continue;
            
            // Sprawdź czy już mamy wystarczająco dużo słów w cache
            lock (_preloadLock)
            {
                if (_preloadedWords.ContainsKey(letter) && _preloadedWords[letter].Count >= wordsPerLetter)
                    continue;
            }
            
            // Wczytaj próbkę słów dla tej litery
            var lineNumbers = _letterLineNumbers[letter];
            var sampleSize = Math.Min(wordsPerLetter * 2, lineNumbers.Count);
            var sampleLineNumbers = ShuffleList(lineNumbers).Take(sampleSize).ToList();
            
            // Wczytaj batch
            var wordsDict = ReadLinesBatch(sampleLineNumbers);
            
            // Zapisz w pre-loaded cache
            lock (_preloadLock)
            {
                if (!_preloadedWords.ContainsKey(letter))
                {
                    _preloadedWords[letter] = new List<string>();
                }
                
                foreach (var kvp in wordsDict)
                {
                    var word = kvp.Value;
                    if (!string.IsNullOrWhiteSpace(word) && 
                        word.Length >= _minWordLength &&
                        !_preloadedWords[letter].Contains(word) &&
                        _preloadedWords[letter].Count < 500)
                    {
                        _preloadedWords[letter].Add(word);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Losuje hasło (słowo) z słownika o określonej długości
    /// Zoptymalizowane: używa wszystkich liter z indeksu i wczytuje większą próbkę na raz
    /// </summary>
    public string? GetRandomWordOfLength(int minLength = 6, int maxLength = 12)
    {
        LoadIndex(); // Upewnij się że indeks jest załadowany

        if (_totalLines == 0)
            return null;

        // Zoptymalizowane: użyj prostszego podejścia - losuj linie bezpośrednio
        // To jest szybsze niż batch loading dla małych próbek
        int maxAttempts = 30; // Zmniejszona liczba prób (było 50)
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int randomLineNumber = _random.Next(1, _totalLines + 1);
            var word = ReadLineAt(randomLineNumber);
            
            if (word != null && word.Length >= minLength && word.Length <= maxLength)
            {
                return word;
            }
        }

        // Fallback: jeśli nie znaleziono, spróbuj z cache (jeśli dostępny)
        // Sprawdź pre-loaded words dla różnych liter
        lock (_preloadLock)
        {
            var allCandidates = new List<string>();
            foreach (var kvp in _preloadedWords)
            {
                foreach (var word in kvp.Value)
                {
                    if (word != null && word.Length >= minLength && word.Length <= maxLength)
                    {
                        allCandidates.Add(word);
                    }
                }
            }
            
            if (allCandidates.Count > 0)
            {
                return allCandidates[_random.Next(allCandidates.Count)];
            }
        }

        return null;
    }

    /// <summary>
    /// Zwraca przybliżoną liczbę słów w słowniku
    /// </summary>
    public int Count => _totalLines;

    /// <summary>
    /// Fisher-Yates shuffle - efektywny algorytm losowego mieszania listy
    /// </summary>
    private List<T> ShuffleList<T>(List<T> list)
    {
        var shuffled = new List<T>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }
}

