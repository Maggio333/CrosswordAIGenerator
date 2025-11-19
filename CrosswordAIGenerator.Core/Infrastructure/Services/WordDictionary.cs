using System.Linq;
using System.IO.Compression;
using System.Text;
using System.Globalization;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Słownik słów do generowania krzyżówek - implementacja z I/O
/// </summary>
public class WordDictionary : IWordDictionary
{
    private readonly List<string> _words;
    private readonly Dictionary<char, List<string>> _wordsByFirstLetter;
    private readonly Dictionary<char, List<string>> _wordsByLetter; // Słowa zawierające daną literę
    private readonly Random _random;

    public WordDictionary(IEnumerable<string> words, int? seed = null, int minWordLength = 6)
    {
        _words = words.Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= minWordLength)
                     .Select(w => w.ToUpper(CultureInfo.GetCultureInfo("pl-PL")).Trim())
                     .Distinct()
                     .ToList();
        
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        
        // Indeksuj słowa według pierwszej litery
        _wordsByFirstLetter = new Dictionary<char, List<string>>();
        foreach (var word in _words)
        {
            if (word.Length > 0)
            {
                var firstLetter = char.ToUpper(word[0], CultureInfo.GetCultureInfo("pl-PL"));
                if (!_wordsByFirstLetter.ContainsKey(firstLetter))
                {
                    _wordsByFirstLetter[firstLetter] = new List<string>();
                }
                _wordsByFirstLetter[firstLetter].Add(word);
            }
        }
        
        // Indeksuj słowa według zawartych liter (dla przecięć)
        _wordsByLetter = new Dictionary<char, List<string>>();
        foreach (var word in _words)
        {
            var uniqueLetters = word.Distinct();
            foreach (var letter in uniqueLetters)
            {
                var upperLetter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
                if (!_wordsByLetter.ContainsKey(upperLetter))
                {
                    _wordsByLetter[upperLetter] = new List<string>();
                }
                if (!_wordsByLetter[upperLetter].Contains(word))
                {
                    _wordsByLetter[upperLetter].Add(word);
                }
            }
        }
    }

    /// <summary>
    /// Ładuje słownik z pliku tekstowego (jedno słowo na linię) - obsługuje również pliki .gz
    /// </summary>
    public static WordDictionary FromFile(string filePath, int? seed = null, int minWordLength = 6)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Słownik nie istnieje: {filePath}");
        }

        IEnumerable<string> lines;
        
        // Sprawdź czy to plik .gz
        if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            // Wczytaj z pliku .gz - używamy buforowanego odczytu dla lepszej wydajności
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream, Encoding.UTF8, bufferSize: 8192))
            {
                var allLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    allLines.Add(line);
                    // Co 50000 linii zwolnij trochę pamięci (dla bardzo dużych plików)
                    // Uwaga: GC.Collect może spowolnić, więc robimy to rzadko
                    if (allLines.Count % 50000 == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                }
                lines = allLines;
            }
        }
        else
        {
            // Zwykły plik tekstowy
            lines = File.ReadAllLines(filePath, Encoding.UTF8);
        }

        // Wczytaj wszystkie linie, usuń puste, usuń białe znaki, konwertuj na wielkie litery
        var words = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim().ToUpper(CultureInfo.GetCultureInfo("pl-PL")))
            .Where(word => word.Length >= minWordLength)
            // WAŻNE: Po ToUpper wszystkie litery są wielkie, więc sprawdzamy tylko wielkie polskie znaki
            // char.IsLetter() powinno rozpoznawać polskie znaki, ale dla pewności dodajemy też explicit check
            .Where(word => word.All(c => char.IsLetter(c) || "ĄĆĘŁŃÓŚŹŻ".Contains(c))) // Tylko litery (w tym polskie znaki)
            .Distinct()
            .ToList();
        
        if (words.Count == 0)
        {
            throw new InvalidOperationException($"Słownik jest pusty po filtrowaniu (min {minWordLength} liter): {filePath}");
        }
        
        return new WordDictionary(words, seed, minWordLength);
    }

    /// <summary>
    /// Tworzy słownik - próbuje załadować z pliku, jeśli nie istnieje używa domyślnego
    /// </summary>
    public static WordDictionary CreateDefault(int? seed = null, string? dictionaryPath = null)
    {
        // Spróbuj załadować z pliku
        if (dictionaryPath != null && File.Exists(dictionaryPath))
        {
            try
            {
                return FromFile(dictionaryPath, seed, minWordLength: 6);
            }
            catch
            {
                // Jeśli nie udało się, użyj domyślnego
            }
        }
        
        // Spróbuj załadować większy słownik words.polish.txt.gz (priorytet)
        var gzPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "dictionaries", "words.polish.txt.gz");
        if (File.Exists(gzPath))
        {
            try
            {
                return FromFile(gzPath, seed, minWordLength: 6);
            }
            catch
            {
                // Jeśli nie udało się, spróbuj innych
            }
        }
        
        // Spróbuj załadować domyślny plik słownika polish_words.txt
        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "dictionaries", "polish_words.txt");
        if (File.Exists(defaultPath))
        {
            try
            {
                return FromFile(defaultPath, seed, minWordLength: 6);
            }
            catch
            {
                // Jeśli nie udało się, użyj domyślnego
            }
        }
        
        // Użyj wbudowanego małego słownika
        return CreateDefaultFallback(seed);
    }
    
    /// <summary>
    /// Tworzy słownik z domyślnymi słowami (dla testów) - tylko słowa o min 6 literach
    /// </summary>
    public static WordDictionary CreateDefaultFallback(int? seed = null)
    {
        var defaultWords = new[]
        {
            // Słowa o min 6 literach
            "SAMOCHÓD", "AUTOBUS", "POCIĄG", "SAMOLOT",
            "KSIĄŻKA", "DŁUGOPIS", "ZESZYT", "TABLICA", "KREDKA",
            "KRZEŚŁO", "MORZE", "RZEKA", "MIASTO",
            "KROWA", "DRZEWO", "KWIAT", "KORZEŃ", "GAŁĄŹ",
            "ŚCIANA", "SAMOLOT", "AUTOBUS", "POCIĄG",
            "KSIĄŻKA", "DŁUGOPIS", "ZESZYT", "TABLICA", "KREDKA",
            "SAMOCHÓD", "AUTOBUS", "POCIĄG", "SAMOLOT",
            "KSIĄŻKA", "DŁUGOPIS", "ZESZYT", "TABLICA", "KREDKA",
            "TELEFON", "KOMPUTER", "KLAMATURA", "MONITOR", "MYSZKA",
            "STOLIK", "KRZESŁO", "ŚCIANA", "OKIENKO", "DRZWIEC"
        };
        
        return new WordDictionary(defaultWords, seed, minWordLength: 6);
    }

    // Implementacja IWordDictionary

    /// <summary>
    /// Losuje słowo z całego słownika
    /// </summary>
    public string? GetRandomWord(int minLength = 6, int maxLength = 20)
    {
        if (_words.Count == 0)
            return null;
        
        var candidates = _words.Where(w => w.Length >= minLength && w.Length <= maxLength).ToList();
        if (candidates.Count == 0)
            return null;
        
        return candidates[_random.Next(candidates.Count)];
    }

    /// <summary>
    /// Losuje słowo zawierające określoną literę (dla przecięć)
    /// </summary>
    public string? GetRandomWordContaining(char letter, int minLength = 6, int maxLength = 20)
    {
        letter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
        if (!_wordsByLetter.ContainsKey(letter))
        {
            return null;
        }
        
        var candidates = _wordsByLetter[letter]
            .Where(w => w.Length >= minLength && w.Length <= maxLength)
            .ToList();
        
        if (candidates.Count == 0)
        {
            return null;
        }
        
        return candidates[_random.Next(candidates.Count)];
    }

    /// <summary>
    /// Znajduje wszystkie słowa zawierające określoną literę
    /// </summary>
    public List<string> GetWordsContaining(char letter, int minLength = 6, int maxLength = 20, int maxResults = 1000)
    {
        letter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
        if (!_wordsByLetter.ContainsKey(letter))
        {
            return new List<string>();
        }
        
        return _wordsByLetter[letter]
            .Where(w => w.Length >= minLength && w.Length <= maxLength)
            .OrderBy(x => _random.Next()) // Wymieszaj dla lepszej losowości
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Losuje hasło (słowo) z słownika o określonej długości
    /// </summary>
    public string? GetRandomWordOfLength(int minLength = 6, int maxLength = 12)
    {
        if (_words.Count == 0)
            return null;

        var candidates = _words.Where(w => w.Length >= minLength && w.Length <= maxLength).ToList();
        if (candidates.Count == 0)
            return null;

        return candidates[_random.Next(candidates.Count)];
    }

    // Dodatkowe metody (nie w interfejsie, ale używane w kodzie)

    /// <summary>
    /// Losuje słowo zaczynające się od określonej litery
    /// </summary>
    public string? GetRandomWordStartingWith(char letter)
    {
        letter = char.ToUpper(letter, CultureInfo.GetCultureInfo("pl-PL"));
        if (!_wordsByFirstLetter.ContainsKey(letter) || _wordsByFirstLetter[letter].Count == 0)
        {
            return null;
        }
        
        var words = _wordsByFirstLetter[letter];
        return words[_random.Next(words.Count)];
    }

    /// <summary>
    /// Sprawdza czy słowo istnieje w słowniku
    /// </summary>
    public bool Contains(string word)
    {
        return _words.Contains(word.ToUpper(CultureInfo.GetCultureInfo("pl-PL")).Trim());
    }

    /// <summary>
    /// Zwraca liczbę słów w słowniku
    /// </summary>
    public int Count => _words.Count;
}

