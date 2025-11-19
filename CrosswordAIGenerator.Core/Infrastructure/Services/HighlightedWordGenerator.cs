using System.Collections.Concurrent;
using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Generator haseł z cache'owaniem dla wydajności
/// </summary>
public class HighlightedWordGenerator : IHighlightedWordGenerator
{
    private readonly IWordDictionary _wordDictionary;
    private readonly Random _random;
    private readonly ICursorLogger? _logger;
    
    // Cache pre-generowanych haseł (długość -> lista haseł)
    private readonly ConcurrentDictionary<(int min, int max), Queue<string>> _wordCache = new();
    private readonly object _cacheLock = new();

    public HighlightedWordGenerator(IWordDictionary wordDictionary, int? seed = null, ICursorLogger? logger = null)
    {
        _wordDictionary = wordDictionary ?? throw new ArgumentNullException(nameof(wordDictionary));
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _logger = logger;
    }

    public Result<string, string> GetRandomWord(int minLength = 6, int maxLength = 8)
    {
        // Najpierw sprawdź cache
        var cacheKey = (minLength, maxLength);
        if (_wordCache.TryGetValue(cacheKey, out var cachedWords) && cachedWords.Count > 0)
        {
            lock (_cacheLock)
            {
                if (cachedWords.TryDequeue(out var cachedWord))
                {
                    _logger?.Debug($"GetRandomWord: Użyto z cache: '{cachedWord}' ({cachedWord.Length} liter)");
                    return Result<string, string>.Success(cachedWord);
                }
            }
        }
        
        // Jeśli nie ma w cache, pobierz ze słownika
        var randomWord = _wordDictionary.GetRandomWordOfLength(minLength, maxLength);
        if (string.IsNullOrWhiteSpace(randomWord))
        {
            return Result<string, string>.Failure($"Nie znaleziono słowa o długości {minLength}-{maxLength}");
        }
        
        _logger?.Debug($"GetRandomWord: Pobrano ze słownika: '{randomWord}' ({randomWord.Length} liter)");
        return Result<string, string>.Success(randomWord);
    }

    public Result<List<string>, string> GenerateWords(int count, int minLength = 6, int maxLength = 8)
    {
        var words = new List<string>();
        int attempts = 0;
        int maxAttempts = count * 10; // Zmniejszona liczba prób (było 20)
        
        _logger?.Info($"GenerateWords: Generowanie {count} haseł (długość: {minLength}-{maxLength})");
        
        while (words.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            var wordResult = GetRandomWord(minLength, maxLength);
            if (wordResult.IsFailure)
            {
                _logger?.Warning($"GenerateWords: Nie udało się pobrać hasła (próba {attempts}): {wordResult.Error}");
                continue;
            }
            
            var word = wordResult.Value;
            if (!string.IsNullOrWhiteSpace(word) && !words.Contains(word))
            {
                words.Add(word);
                if (words.Count % 10 == 0)
                {
                    _logger?.Debug($"GenerateWords: Wygenerowano {words.Count}/{count} haseł");
                }
            }
        }
        
        _logger?.Info($"GenerateWords: Wygenerowano {words.Count}/{count} haseł (próby: {attempts})");
        
        if (words.Count == 0)
        {
            return Result<List<string>, string>.Failure($"Nie udało się wygenerować żadnych haseł po {attempts} próbach");
        }
        
        return Result<List<string>, string>.Success(words);
    }

    public Result<bool, string> PreloadWords(int count, int minLength = 6, int maxLength = 8)
    {
        var cacheKey = (minLength, maxLength);
        
        _logger?.Info($"PreloadWords: Pre-generowanie {count} haseł do cache (długość: {minLength}-{maxLength})");
        
        var wordsResult = GenerateWords(count, minLength, maxLength);
        if (wordsResult.IsFailure)
        {
            return Result<bool, string>.Failure(wordsResult.Error!);
        }
        
        var words = wordsResult.Value;
        var queue = new Queue<string>(words);
        
        _wordCache.AddOrUpdate(cacheKey, queue, (key, oldQueue) =>
        {
            // Dodaj nowe słowa do istniejącej kolejki
            foreach (var word in words)
            {
                if (!oldQueue.Contains(word))
                {
                    oldQueue.Enqueue(word);
                }
            }
            return oldQueue;
        });
        
        _logger?.Info($"PreloadWords: Załadowano {words.Count} haseł do cache");
        return Result<bool, string>.Success(true);
    }
}

