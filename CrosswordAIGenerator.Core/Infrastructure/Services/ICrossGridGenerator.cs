using CrosswordAIGenerator.Core.Domain.Models;
using System.Collections.Generic;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Interfejs dla generatora formatu CrossGrid (ASCII art representation)
/// </summary>
public interface ICrossGridGenerator
{
    /// <summary>
    /// Generuje format CrossGrid z CrosswordGrid
    /// </summary>
    /// <param name="grid">Siatka krzyżówki</param>
    /// <param name="highlightedCellsWithIndices">Pozycje kratek do wyróżnienia (hasło główne) z indeksami liter (1, 2, 3...)</param>
    /// <param name="placedWords">Lista słów w krzyżówce (opcjonalnie, dla informacji)</param>
    /// <returns>String w formacie CrossGrid</returns>
    string GenerateCrossGrid(
        CrosswordGrid grid, 
        Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, 
        List<CrosswordWord>? placedWords = null);

    /// <summary>
    /// Parsuje format CrossGrid i zwraca CrosswordGrid
    /// </summary>
    /// <param name="crossGridText">Tekst w formacie CrossGrid</param>
    /// <returns>Krotka: (CrosswordGrid, highlightedCellsWithIndices, placedWords)</returns>
    (CrosswordGrid grid, Dictionary<(int row, int col), int> highlightedCellsWithIndices, List<CrosswordWord> placedWords) 
        ParseCrossGrid(string crossGridText);

    /// <summary>
    /// Konwertuje XAML do formatu CrossGrid (dla walidacji)
    /// </summary>
    /// <param name="xaml">XAML string</param>
    /// <returns>String w formacie CrossGrid</returns>
    string XamlToCrossGrid(string xaml);

    /// <summary>
    /// Konwertuje CrossGrid do XAML (dla walidacji)
    /// </summary>
    /// <param name="crossGridText">Tekst w formacie CrossGrid</param>
    /// <param name="xamlGenerator">Generator XAML do użycia</param>
    /// <returns>XAML string</returns>
    string CrossGridToXaml(string crossGridText, IXamlGenerator xamlGenerator);

    /// <summary>
    /// Waliduje poprawność formatu CrossGrid
    /// </summary>
    /// <param name="crossGridText">Tekst w formacie CrossGrid do walidacji</param>
    /// <param name="originalGrid">Oryginalny grid do porównania (opcjonalnie)</param>
    /// <param name="originalHighlightedCells">Oryginalne highlighted cells do porównania (opcjonalnie)</param>
    /// <returns>Wynik walidacji z listą błędów i ostrzeżeń</returns>
    CrossGridValidationResult ValidateCrossGrid(
        string crossGridText,
        CrosswordGrid? originalGrid = null,
        Dictionary<(int row, int col), int>? originalHighlightedCells = null);
}

