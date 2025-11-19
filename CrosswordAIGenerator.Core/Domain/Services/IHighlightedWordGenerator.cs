using CrosswordAIGenerator.Core.Domain.Common;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora haseł (słów do wyróżnienia w krzyżówkach)
/// </summary>
public interface IHighlightedWordGenerator
{
    /// <summary>
    /// Losuje jedno hasło o określonej długości
    /// </summary>
    Result<string, string> GetRandomWord(int minLength = 6, int maxLength = 8);
    
    /// <summary>
    /// Generuje listę unikalnych haseł
    /// </summary>
    Result<List<string>, string> GenerateWords(int count, int minLength = 6, int maxLength = 8);
    
    /// <summary>
    /// Pre-generuje i cache'uje hasła dla szybszego dostępu
    /// </summary>
    Result<bool, string> PreloadWords(int count, int minLength = 6, int maxLength = 8);
}

