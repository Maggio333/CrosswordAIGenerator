using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Generator formatu CrossGrid - prosty tekstowy format ASCII art dla LLM
/// Format: # GRID\nR0: ....[1]P..H.......R..\nR1: ....[2]O..I.P.....O..\n...
/// </summary>
public class CrossGridGenerator : ICrossGridGenerator
{
    private readonly ICursorLogger? _logger;

    public CrossGridGenerator(ICursorLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generuje format CrossGrid z CrosswordGrid
    /// </summary>
    public string GenerateCrossGrid(
        CrosswordGrid grid, 
        Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, 
        List<CrosswordWord>? placedWords = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# GRID");
        
        for (int r = 0; r < grid.Rows; r++)
        {
            sb.Append($"R{r}: ");
            
            for (int c = 0; c < grid.Columns; c++)
            {
                var cell = grid.GetCell(r, c);
                
                if (cell.IsWall)
                {
                    // Ściana
                    sb.Append("#");
                }
                else if (cell.HasLetter)
                {
                    char letter = cell.Letter!.Value;
                    
                    // Sprawdź czy to highlighted cell (hasło główne)
                    if (highlightedCellsWithIndices != null && 
                        highlightedCellsWithIndices.TryGetValue((r, c), out int index))
                    {
                        // Format: [1]P, [2]O, etc.
                        sb.Append($"[{index}]{letter}");
                    }
                    else
                    {
                        // Zwykła litera
                        sb.Append(letter);
                    }
                }
                else
                {
                    // Pusta kratka
                    sb.Append(".");
                }
                
                // Dodaj wizualny separator co 5 kolumn dla lepszej czytelności (ale nie na końcu)
                if ((c + 1) % 5 == 0 && c < grid.Columns - 1)
                {
                    sb.Append(" ");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Parsuje format CrossGrid i zwraca CrosswordGrid
    /// </summary>
    public (CrosswordGrid grid, Dictionary<(int row, int col), int> highlightedCellsWithIndices, List<CrosswordWord> placedWords) 
        ParseCrossGrid(string crossGridText)
    {
        if (string.IsNullOrWhiteSpace(crossGridText))
        {
            throw new ArgumentException("CrossGrid text cannot be empty", nameof(crossGridText));
        }

        var lines = crossGridText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .ToList();

        if (lines.Count == 0)
        {
            throw new ArgumentException("No valid grid lines found in CrossGrid text", nameof(crossGridText));
        }

        // Parsuj pierwszy wiersz aby określić rozmiar
        var firstLineMatch = Regex.Match(lines[0], @"^R\d+:\s*(.+)$");
        if (!firstLineMatch.Success)
        {
            throw new ArgumentException($"Invalid format in first line: {lines[0]}", nameof(crossGridText));
        }

        // Usuń spacje (separatory wizualne) przed liczeniem kolumn
        string firstLineContent = firstLineMatch.Groups[1].Value.Replace(" ", "");
        int columns = firstLineContent.Length;
        int rows = lines.Count;

        var grid = new CrosswordGrid(rows, columns);
        var highlightedCellsWithIndices = new Dictionary<(int row, int col), int>();
        var placedWords = new List<CrosswordWord>();

        // Regex do parsowania highlighted cells: [1]P, [2]O, etc.
        var highlightedPattern = new Regex(@"\[(\d+)\]([A-ZĄĆĘŁŃÓŚŹŻ])");

        for (int r = 0; r < rows; r++)
        {
            var lineMatch = Regex.Match(lines[r], @"^R\d+:\s*(.+)$");
            if (!lineMatch.Success)
            {
                _logger?.WarningFormat("Skipping invalid line: {0}", lines[r]);
                continue;
            }

            string content = lineMatch.Groups[1].Value;
            
            // Usuń spacje (separatory wizualne) przed parsowaniem
            content = content.Replace(" ", "");
            
            // Parsuj znak po znaku, obsługując highlighted cells
            int c = 0;
            int i = 0;
            while (i < content.Length && c < columns)
            {
                if (content[i] == '#')
                {
                    // Ściana
                    grid.SetWall(r, c);
                    i++;
                    c++;
                }
                else if (content[i] == '.')
                {
                    // Pusta kratka - już jest domyślnie pusta
                    i++;
                    c++;
                }
                else if (i < content.Length - 1 && content[i] == '[')
                {
                    // Highlighted cell: [1]P
                    var match = highlightedPattern.Match(content, i);
                    if (match.Success)
                    {
                        int index = int.Parse(match.Groups[1].Value);
                        char letter = match.Groups[2].Value[0];
                        
                        grid.SetLetter(r, c, letter);
                        highlightedCellsWithIndices[(r, c)] = index;
                        
                        i += match.Length; // Przeskocz cały match
                        c++;
                    }
                    else
                    {
                        // Niepoprawny format, traktuj jako zwykłą literę
                        if (char.IsLetter(content[i]))
                        {
                            grid.SetLetter(r, c, content[i]);
                            i++;
                            c++;
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                else if (char.IsLetter(content[i]))
                {
                    // Zwykła litera
                    grid.SetLetter(r, c, content[i]);
                    i++;
                    c++;
                }
                else
                {
                    // Nieznany znak, pomiń
                    i++;
                }
            }
        }

        // TODO: W przyszłości można spróbować wykryć placedWords z siatki,
        // ale na razie zwracamy pustą listę
        // (placedWords są opcjonalne i nie są wymagane w CrossGrid)

        return (grid, highlightedCellsWithIndices, placedWords);
    }

    /// <summary>
    /// Konwertuje XAML do formatu CrossGrid (dla walidacji)
    /// </summary>
    public string XamlToCrossGrid(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
        {
            throw new ArgumentException("XAML cannot be empty", nameof(xaml));
        }

        // Parsuj XAML jako XML
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xaml);

        // Znajdź główny Grid (może być w ScrollViewer)
        var gridNode = xmlDoc.SelectSingleNode("//Grid") ?? xmlDoc.SelectSingleNode("//ScrollViewer/Grid");
        if (gridNode == null)
        {
            throw new ArgumentException("XAML does not contain a Grid element", nameof(xaml));
        }

        // Wyciągnij RowDefinitions i ColumnDefinitions
        var rowDefs = gridNode.SelectNodes(".//RowDefinition");
        var colDefs = gridNode.SelectNodes(".//ColumnDefinition");
        
        int rows = rowDefs?.Count ?? 0;
        int columns = colDefs?.Count ?? 0;

        // Odejmij dodatkowe wiersze dla definicji (jeśli są)
        var hasDefinitions = gridNode.SelectSingleNode(".//Border[@Grid.Column]") != null;
        if (hasDefinitions)
        {
            // Sprawdź czy są dodatkowe kolumny dla definicji
            var definitionColumn = gridNode.SelectSingleNode(".//Border[@Grid.Column and @Grid.RowSpan]");
            if (definitionColumn != null)
            {
                // Odejmij jedną kolumnę dla definicji
                columns--;
            }
        }

        if (rows == 0 || columns == 0)
        {
            throw new ArgumentException($"Could not determine grid size from XAML. Rows: {rows}, Columns: {columns}", nameof(xaml));
        }

        var grid = new CrosswordGrid(rows, columns);
        var highlightedCellsWithIndices = new Dictionary<(int row, int col), int>();

        // Parsuj wszystkie Border i TextBlock elementy
        var borders = gridNode.SelectNodes(".//Border[@Grid.Row and @Grid.Column]");
        if (borders != null)
        {
            foreach (XmlNode border in borders)
            {
                var rowAttr = border.Attributes?["Grid.Row"];
                var colAttr = border.Attributes?["Grid.Column"];
                var backgroundAttr = border.Attributes?["Background"];

                if (rowAttr == null || colAttr == null) continue;

                int row = int.Parse(rowAttr.Value);
                int col = int.Parse(colAttr.Value);

                // Sprawdź czy to ściana (Background="Black")
                if (backgroundAttr?.Value == "Black")
                {
                    grid.SetWall(row, col);
                    continue;
                }

                // Sprawdź czy to highlighted cell (Background="LightCoral")
                bool isHighlighted = backgroundAttr?.Value == "LightCoral";
                int letterIndex = 0;

                // Znajdź TextBlock wewnątrz Border
                var textBlock = border.SelectSingleNode(".//TextBlock[@Text]");
                if (textBlock != null)
                {
                    var textAttr = textBlock.Attributes?["Text"];
                    if (textAttr != null && textAttr.Value.Length == 1)
                    {
                        char letter = textAttr.Value[0];
                        grid.SetLetter(row, col, letter);

                        // Sprawdź czy jest numer indeksu (mały TextBlock z FontSize="10")
                        var indexTextBlock = border.SelectSingleNode(".//TextBlock[@FontSize='10' and @Foreground='DarkRed']");
                        if (indexTextBlock != null)
                        {
                            var indexTextAttr = indexTextBlock.Attributes?["Text"];
                            if (indexTextAttr != null && int.TryParse(indexTextAttr.Value, out int index))
                            {
                                letterIndex = index;
                                highlightedCellsWithIndices[(row, col)] = index;
                            }
                        }
                    }
                }
            }
        }

        // Generuj CrossGrid z utworzonego grid
        return GenerateCrossGrid(grid, highlightedCellsWithIndices, null);
    }

    /// <summary>
    /// Konwertuje CrossGrid do XAML (dla walidacji)
    /// </summary>
    public string CrossGridToXaml(string crossGridText, IXamlGenerator xamlGenerator)
    {
        if (string.IsNullOrWhiteSpace(crossGridText))
        {
            throw new ArgumentException("CrossGrid text cannot be empty", nameof(crossGridText));
        }

        if (xamlGenerator == null)
        {
            throw new ArgumentNullException(nameof(xamlGenerator));
        }

        // Parsuj CrossGrid
        var (grid, highlightedCellsWithIndices, placedWords) = ParseCrossGrid(crossGridText);

        // Generuj XAML
        return xamlGenerator.GenerateXaml(grid, 500, 500, highlightedCellsWithIndices, placedWords);
    }

    /// <summary>
    /// Waliduje poprawność formatu CrossGrid
    /// </summary>
    public CrossGridValidationResult ValidateCrossGrid(
        string crossGridText,
        CrosswordGrid? originalGrid = null,
        Dictionary<(int row, int col), int>? originalHighlightedCells = null)
    {
        var result = new CrossGridValidationResult { IsValid = true };

        try
        {
            // 1. Podstawowa walidacja - czy można sparsować
            (CrosswordGrid parsedGrid, Dictionary<(int row, int col), int> parsedHighlightedCells, List<CrosswordWord> _) parsedResult;
            try
            {
                parsedResult = ParseCrossGrid(crossGridText);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Błąd parsowania CrossGrid: {ex.Message}");
                return result;
            }

            var parsedGrid = parsedResult.parsedGrid;
            var parsedHighlightedCells = parsedResult.parsedHighlightedCells;

            // 2. Sprawdź podstawowe właściwości
            result.Details["ParsedRows"] = parsedGrid.Rows;
            result.Details["ParsedColumns"] = parsedGrid.Columns;
            result.Details["ParsedLetterCount"] = parsedGrid.Cells.Values.Count(c => c.HasLetter);
            result.Details["ParsedWallCount"] = parsedGrid.Cells.Values.Count(c => c.IsWall);
            result.Details["ParsedHighlightedCellsCount"] = parsedHighlightedCells.Count;

            // 3. Sprawdź spójność - czy wszystkie highlighted cells mają litery
            foreach (var (pos, index) in parsedHighlightedCells)
            {
                var cell = parsedGrid.GetCell(pos.row, pos.col);
                if (!cell.HasLetter)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Highlighted cell [{index}] na pozycji ({pos.row}, {pos.col}) nie ma litery");
                }
            }

            // 4. Sprawdź czy highlighted indices są ciągłe (1, 2, 3, ...)
            var indices = parsedHighlightedCells.Values.OrderBy(i => i).ToList();
            if (indices.Count > 0)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] != i + 1)
                    {
                        result.Warnings.Add($"Highlighted indices nie są ciągłe. Oczekiwano {i + 1}, znaleziono {indices[i]}");
                    }
                }
            }

            // 5. Porównanie z oryginalnym gridem (jeśli podano)
            if (originalGrid != null)
            {
                if (parsedGrid.Rows != originalGrid.Rows)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Liczba wierszy się nie zgadza: parsowane={parsedGrid.Rows}, oryginalne={originalGrid.Rows}");
                }

                if (parsedGrid.Columns != originalGrid.Columns)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Liczba kolumn się nie zgadza: parsowane={parsedGrid.Columns}, oryginalne={originalGrid.Columns}");
                }

                // Porównaj litery
                int differences = 0;
                for (int r = 0; r < Math.Min(parsedGrid.Rows, originalGrid.Rows); r++)
                {
                    for (int c = 0; c < Math.Min(parsedGrid.Columns, originalGrid.Columns); c++)
                    {
                        var parsedCell = parsedGrid.GetCell(r, c);
                        var originalCell = originalGrid.GetCell(r, c);

                        if (parsedCell.HasLetter != originalCell.HasLetter)
                        {
                            differences++;
                            if (differences <= 5) // Pokaż tylko pierwsze 5 różnic
                            {
                                result.Errors.Add($"Różnica na pozycji ({r}, {c}): parsowane={parsedCell.HasLetter}, oryginalne={originalCell.HasLetter}");
                            }
                        }
                        else if (parsedCell.HasLetter && originalCell.HasLetter)
                        {
                            if (parsedCell.Letter != originalCell.Letter)
                            {
                                differences++;
                                if (differences <= 5)
                                {
                                    result.Errors.Add($"Różnica litery na pozycji ({r}, {c}): parsowane={parsedCell.Letter}, oryginalne={originalCell.Letter}");
                                }
                            }
                        }
                    }
                }

                if (differences > 5)
                {
                    result.Errors.Add($"Znaleziono {differences} różnic między parsowanym a oryginalnym gridem");
                }

                if (differences > 0)
                {
                    result.IsValid = false;
                }
            }

            // 6. Porównanie highlighted cells (jeśli podano)
            if (originalHighlightedCells != null)
            {
                if (parsedHighlightedCells.Count != originalHighlightedCells.Count)
                {
                    result.Warnings.Add($"Liczba highlighted cells się nie zgadza: parsowane={parsedHighlightedCells.Count}, oryginalne={originalHighlightedCells.Count}");
                }

                foreach (var (pos, index) in originalHighlightedCells)
                {
                    if (!parsedHighlightedCells.TryGetValue(pos, out int parsedIndex))
                    {
                        result.Warnings.Add($"Brakuje highlighted cell na pozycji ({pos.row}, {pos.col}) z indeksem {index}");
                    }
                    else if (parsedIndex != index)
                    {
                        result.Warnings.Add($"Różnica indeksu na pozycji ({pos.row}, {pos.col}): parsowane={parsedIndex}, oryginalne={index}");
                    }
                }
            }

            // 7. Sprawdź czy są jakieś nieprawidłowe znaki
            var invalidChars = crossGridText.Where(c => !char.IsLetterOrDigit(c) && 
                c != '#' && c != '.' && c != '[' && c != ']' && c != ' ' && 
                c != '\r' && c != '\n' && c != ':' && c != 'R' && c != 'G' && c != 'I' && c != 'D').ToList();
            if (invalidChars.Any())
            {
                result.Warnings.Add($"Znaleziono nieprawidłowe znaki: {string.Join(", ", invalidChars.Distinct().Take(10))}");
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Nieoczekiwany błąd podczas walidacji: {ex.Message}");
        }

        return result;
    }
}

