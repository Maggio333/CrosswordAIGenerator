namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla serwisu znajdującego ścieżkę do pliku słownika
/// </summary>
public interface IDictionaryPathResolver
{
    /// <summary>
    /// Znajduje plik słownika slowa.txt w różnych lokalizacjach
    /// </summary>
    /// <returns>Ścieżka do pliku słownika lub null jeśli nie znaleziono</returns>
    string? FindDictionaryFile();
}

