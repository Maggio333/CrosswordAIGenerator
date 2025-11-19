namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla słownika słów - abstrakcja domenowa
/// Implementacje (z I/O) są w Infrastructure
/// </summary>
public interface IWordDictionary
{
    /// <summary>
    /// Zwraca listę słów zawierających określoną literę
    /// </summary>
    List<string> GetWordsContaining(char letter, int minLength = 6, int maxLength = 20, int maxResults = 1000);

    /// <summary>
    /// Losuje słowo z całego słownika
    /// </summary>
    string? GetRandomWord(int minLength = 6, int maxLength = 20);

    /// <summary>
    /// Losuje słowo zawierające określoną literę
    /// </summary>
    string? GetRandomWordContaining(char letter, int minLength = 6, int maxLength = 20);

    /// <summary>
    /// Losuje słowo o określonej długości
    /// </summary>
    string? GetRandomWordOfLength(int minLength = 6, int maxLength = 12);
}

