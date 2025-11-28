using System.IO;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Implementacja serwisu znajdującego ścieżkę do pliku słownika
/// </summary>
public class DictionaryPathResolver : IDictionaryPathResolver
{
    private readonly ICursorLogger? _logger;

    public DictionaryPathResolver(ICursorLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Znajduje plik słownika slowa.txt w różnych lokalizacjach
    /// </summary>
    public string? FindDictionaryFile()
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
        
        // Loguj wszystkie sprawdzane ścieżki
        _logger?.InfoFormat("FindDictionaryFile: Sprawdzam {0} ścieżek dla slowa.txt...", possiblePaths.Count);
        
        foreach (var path in possiblePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    _logger?.InfoFormat("FindDictionaryFile: ZNALEZIONO: {0}", path);
                    return path;
                }
            }
            catch (Exception ex)
            {
                // Ignoruj błędy ścieżek, ale loguj
                _logger?.WarningFormat("FindDictionaryFile: Błąd sprawdzania {0}: {1}", path, ex.Message);
                continue;
            }
        }
        
        _logger?.Warning("FindDictionaryFile: NIE ZNALEZIONO slowa.txt!");
        return null;
    }
}

