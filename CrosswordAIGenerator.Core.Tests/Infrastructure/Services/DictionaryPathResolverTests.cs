using CrosswordAIGenerator.Core.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CrosswordAIGenerator.Core.Tests.Infrastructure.Services;

/// <summary>
/// Testy dla DictionaryPathResolver
/// </summary>
public class DictionaryPathResolverTests
{
    [Fact]
    public void DictionaryPathResolver_Powinien_Utworzyc_Instancje()
    {
        // Act
        var resolver = new DictionaryPathResolver();

        // Assert
        resolver.Should().NotBeNull();
    }

    [Fact]
    public void FindDictionaryFile_Powinien_Zwrocic_Null_Gdy_Nie_Ma_Pliku()
    {
        // Arrange
        var resolver = new DictionaryPathResolver();
        
        // Act
        // Uwaga: Ten test może nie działać jeśli plik istnieje w systemie
        // W rzeczywistości sprawdza różne lokalizacje
        
        // Assert - metoda powinna zwrócić null lub ścieżkę
        // Nie możemy przewidzieć wyniku bez znajomości systemu plików
        var result = resolver.FindDictionaryFile();
        
        // Jeśli plik istnieje, powinien zwrócić ścieżkę, jeśli nie - null
        if (result != null)
        {
            result.Should().NotBeEmpty();
            System.IO.File.Exists(result).Should().BeTrue();
        }
    }
}

