using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Interfejs dla generatora XAML
/// </summary>
public interface IXamlGenerator
{
    /// <summary>
    /// Generuje XAML dla siatki krzyżówki
    /// </summary>
    /// <param name="grid">Siatka krzyżówki</param>
    /// <param name="width">Szerokość</param>
    /// <param name="height">Wysokość</param>
    /// <param name="highlightedCellsWithIndices">Pozycje kratek do wyróżnienia (hasło główne) z indeksami liter (1, 2, 3...)</param>
    /// <param name="placedWords">Lista słów w krzyżówce (dla ramek i numeracji)</param>
    /// <param name="wordDefinitions">Mapowanie słowo -> definicja (dla wyświetlania definicji przy numeracji)</param>
    string GenerateXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, List<CrosswordWord>? placedWords = null, Dictionary<string, string>? wordDefinitions = null);
    
    /// <summary>
    /// Generuje pustą wersję XAML (bez liter, tylko ramki i definicje) - do wypełnienia ręcznie
    /// </summary>
    /// <param name="grid">Siatka krzyżówki</param>
    /// <param name="width">Szerokość</param>
    /// <param name="height">Wysokość</param>
    /// <param name="highlightedCellsWithIndices">Pozycje kratek do wyróżnienia (hasło główne) z indeksami liter (1, 2, 3...)</param>
    /// <param name="placedWords">Lista słów w krzyżówce (dla ramek i numeracji)</param>
    /// <param name="wordDefinitions">Mapowanie słowo -> definicja (dla wyświetlania definicji przy numeracji)</param>
    string GenerateEmptyXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, List<CrosswordWord>? placedWords = null, Dictionary<string, string>? wordDefinitions = null);
}

