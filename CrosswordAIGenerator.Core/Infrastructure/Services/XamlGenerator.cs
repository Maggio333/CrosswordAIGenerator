using System.Text;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Generator XAML dla krzyżówek - konwertuje CrosswordGrid do XAML string
/// </summary>
public class XamlGenerator : IXamlGenerator
{
    private readonly ICursorLogger? _logger;

    public XamlGenerator(ICursorLogger? logger = null)
    {
        _logger = logger;
    }
    /// <summary>
    /// Generuje XAML dla siatki krzyżówki
    /// </summary>
    /// <param name="grid">Siatka krzyżówki</param>
    /// <param name="width">Szerokość</param>
    /// <param name="height">Wysokość</param>
    /// <param name="highlightedCellsWithIndices">Pozycje kratek do wyróżnienia (hasło główne) z indeksami liter (1, 2, 3...)</param>
    public string GenerateXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null)
    {
        var sb = new StringBuilder(capacity: 10000); // Wstępna pojemność dla wydajności
        
        // Minimalny XAML - tylko to co konieczne dla finetune
        // Używamy Style w Grid.Resources aby uniknąć powtórzeń FontFamily i FontSize
        sb.AppendLine("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">");
        sb.AppendLine("<Grid.Resources>");
        sb.AppendLine("<Style TargetType=\"TextBlock\">");
        sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
        sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
        sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
        sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
        sb.AppendLine("</Style>");
        sb.AppendLine("</Grid.Resources>");
        sb.AppendLine("<Grid.RowDefinitions>");
        for (int r = 0; r < grid.Rows; r++)
        {
            sb.AppendLine("<RowDefinition/>");
        }
        sb.AppendLine("</Grid.RowDefinitions>");
        sb.AppendLine("<Grid.ColumnDefinitions>");
        for (int c = 0; c < grid.Columns; c++)
        {
            sb.AppendLine("<ColumnDefinition/>");
        }
        sb.AppendLine("</Grid.ColumnDefinitions>");
        
        // Kratki - minimalna reprezentacja
        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Columns; c++)
            {
                var cell = grid.GetCell(r, c);
                
                if (cell.IsWall)
                {
                    // Ściana
                    sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" Background=\"Black\"/>");
                }
                else if (cell.HasLetter)
                {
                    bool isHighlighted = highlightedCellsWithIndices != null && highlightedCellsWithIndices.ContainsKey((r, c));
                    int letterIndex = isHighlighted ? highlightedCellsWithIndices[(r, c)] : 0;
                    
                    // Polskie znaki działają bezpośrednio w XAML, ale escapujmy znaki specjalne XML
                    char letterChar = cell.Letter.Value;
                    string letter = letterChar.ToString();
                    
                    // DEBUG: Sprawdź czy litera ma polskie znaki
                    if ("ĄĆĘŁŃÓŚŹŻ".Contains(letterChar))
                    {
                        _logger?.DebugFormat("Generowanie XAML dla polskiej litery: '{0}' (Unicode: U+{1:X4})", letterChar, (int)letterChar);
                    }
                    
                    // Escapuj tylko znaki specjalne XML (&, <, >, ", ')
                    // WAŻNE: Polskie znaki (Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż) NIE wymagają escapowania
                    letter = letter.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
                    
                    if (isHighlighted && letterIndex > 0)
                    {
                        // Highlighted - Background + litera + numer (Grid wewnątrz Border dla dwóch TextBlocków)
                        // Style z głównego Grid.Resources może nie działać przez Border, więc dodajemy style do wewnętrznego Grid
                        sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" Background=\"LightCoral\">");
                        sb.AppendLine("<Grid>");
                        sb.AppendLine("<Grid.Resources>");
                        sb.AppendLine("<Style TargetType=\"TextBlock\">");
                        sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
                        sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
                        sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
                        sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
                        sb.AppendLine("</Style>");
                        sb.AppendLine("</Grid.Resources>");
                        // Litera - dziedziczy wszystkie właściwości z wewnętrznego Grid.Resources Style
                        sb.AppendLine($"<TextBlock Text=\"{letter}\"/>");
                        // Numer indeksu ma mniejszy rozmiar (10 zamiast 20) i inne wyrównanie
                        sb.AppendLine($"<TextBlock Text=\"{letterIndex}\" FontSize=\"10\" Foreground=\"DarkRed\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\"/>");
                        sb.AppendLine("</Grid>");
                        sb.AppendLine("</Border>");
                    }
                    else
                    {
                        // Zwykła litera - dziedziczy wszystkie właściwości z Grid.Resources Style
                        sb.AppendLine($"<TextBlock Grid.Row=\"{r}\" Grid.Column=\"{c}\" Text=\"{letter}\"/>");
                    }
                }
                // Pusta kratka - pomijamy (domyślnie pusta)
            }
        }
        
        sb.AppendLine("</Grid>");
        return sb.ToString();
    }
}

