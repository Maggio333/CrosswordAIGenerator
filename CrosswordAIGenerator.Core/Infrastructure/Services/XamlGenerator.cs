using System.Linq;
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
    /// <param name="placedWords">Lista słów w krzyżówce (dla ramek i numeracji)</param>
    /// <param name="wordDefinitions">Mapowanie słowo -> definicja (dla wyświetlania definicji przy numeracji)</param>
    public string GenerateXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, List<CrosswordWord>? placedWords = null, Dictionary<string, string>? wordDefinitions = null)
    {
        int highlightedCount = highlightedCellsWithIndices?.Count ?? 0;
        _logger?.InfoFormat("XamlGenerator.GenerateXaml: Rozpoczynam generowanie XAML. Grid: {0}x{1}, Highlighted cells: {2}", 
            grid.Rows, grid.Columns, highlightedCount);
        System.Diagnostics.Debug.WriteLine($"[CURSOR] XamlGenerator.GenerateXaml: Rozpoczynam generowanie XAML. Grid: {grid.Rows}x{grid.Columns}, Highlighted cells: {highlightedCount}");
        
        if (highlightedCellsWithIndices != null && highlightedCellsWithIndices.Count > 0)
        {
            var indices = string.Join(", ", highlightedCellsWithIndices.Values.OrderBy(x => x));
            _logger?.InfoFormat("XamlGenerator.GenerateXaml: Indeksy oznaczonych liter: {0}", indices);
            System.Diagnostics.Debug.WriteLine($"[CURSOR] XamlGenerator.GenerateXaml: Indeksy oznaczonych liter: {indices}");
        }
        
        // Utwórz mapy dla ramek i numeracji słów
        var cellToWords = new Dictionary<(int row, int col), List<CrosswordWord>>();
        var firstLetterPositions = new Dictionary<(int row, int col), int>(); // Pozycja -> numer słowa (Id)
        
        if (placedWords != null && placedWords.Count > 0)
        {
            _logger?.InfoFormat("XamlGenerator.GenerateXaml: Przetwarzam {0} słów dla ramek i numeracji", placedWords.Count);
            
            foreach (var word in placedWords.OrderBy(w => w.Id))
            {
                var positions = word.GetCellPositions().ToList();
                var firstPos = positions.First();
                
                // Zapisz pierwszą literę dla numeracji
                firstLetterPositions[firstPos] = word.Id;
                
                // Zapisz wszystkie pozycje dla ramek
                foreach (var pos in positions)
                {
                    if (!cellToWords.ContainsKey(pos))
                    {
                        cellToWords[pos] = new List<CrosswordWord>();
                    }
                    cellToWords[pos].Add(word);
                }
            }
        }
        
        var sb = new StringBuilder(capacity: 10000); // Wstępna pojemność dla wydajności
        
        // Oblicz rozmiar Grid (szerokość i wysokość)
        const int cellSize = 35; // Rozmiar komórki w pikselach (kwadrat)
        bool hasDefinitions = wordDefinitions != null && wordDefinitions.Count > 0 && placedWords != null && placedWords.Count > 0;
        int totalRows = hasDefinitions ? grid.Rows + 2 : grid.Rows;
        int gridWidth = grid.Columns * cellSize + (hasDefinitions ? 300 + 10 : 0); // Szerokość krzyżówki + definicje + margines
        int gridHeight = totalRows * cellSize; // Wysokość wszystkich wierszy
        
        // Minimalny XAML - tylko to co konieczne dla finetune
        // Używamy Style w Grid.Resources aby uniknąć powtórzeń FontFamily i FontSize
        // ScrollViewer wokół Grid, żeby można było przewijać jeśli nie mieści się
        sb.AppendLine("<ScrollViewer xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\" Background=\"White\">");
        sb.AppendLine($"<Grid Width=\"{gridWidth}\" Height=\"{gridHeight}\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\" Background=\"White\">");
        sb.AppendLine("<Grid.Resources>");
        sb.AppendLine("<Style TargetType=\"TextBlock\">");
        sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
        sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
        sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
        sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
        sb.AppendLine("</Style>");
        sb.AppendLine("</Grid.Resources>");
        // Ustaw stałą wysokość dla wierszy (kwadratowe komórki)
        // cellSize i hasDefinitions/totalRows są już zdefiniowane wyżej
        
        sb.AppendLine("<Grid.RowDefinitions>");
        for (int r = 0; r < grid.Rows; r++)
        {
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>");
        }
        // Dodatkowe wiersze dla obszaru z definicjami (jeśli są definicje)
        if (hasDefinitions)
        {
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>"); // Dodatkowy wiersz 1
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>"); // Dodatkowy wiersz 2
        }
        sb.AppendLine("</Grid.RowDefinitions>");
        sb.AppendLine("<Grid.ColumnDefinitions>");
        for (int c = 0; c < grid.Columns; c++)
        {
            sb.AppendLine($"<ColumnDefinition Width=\"{cellSize}\"/>");
        }
        // Dodatkowa kolumna dla definicji słów (jeśli są definicje)
        if (hasDefinitions)
        {
            sb.AppendLine("<ColumnDefinition Width=\"300\"/>"); // Szerokość kolumny z definicjami
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
                    
                    // Sprawdź czy to pierwsza litera jakiegoś słowa (dla numeracji)
                    bool isFirstLetter = firstLetterPositions.ContainsKey((r, c));
                    int wordNumber = isFirstLetter ? firstLetterPositions[(r, c)] : 0;
                    
                    // Sprawdź które słowa przechodzą przez tę komórkę (dla ramek)
                    var wordsAtCell = cellToWords.ContainsKey((r, c)) ? cellToWords[(r, c)] : new List<CrosswordWord>();
                    
                    // Oblicz które krawędzie są zewnętrzne dla słów (gdzie są ramki)
                    int borderTop = 1, borderBottom = 1, borderLeft = 1, borderRight = 1; // Domyślnie cienka ramka
                    if (wordsAtCell.Count > 0)
                    {
                        foreach (var word in wordsAtCell)
                        {
                            var positions = word.GetCellPositions().ToList();
                            int posIndex = positions.FindIndex(p => p.row == r && p.col == c);
                            
                            // Góra - czy poprzednia komórka nie należy do tego słowa?
                            if (word.IsVertical && (posIndex == 0 || !positions.Any(p => p.row == r - 1 && p.col == c)))
                            {
                                borderTop = 3; // Grubsza ramka dla zewnętrznej krawędzi słowa
                            }
                            // Dół - czy następna komórka nie należy do tego słowa?
                            if (word.IsVertical && (posIndex == positions.Count - 1 || !positions.Any(p => p.row == r + 1 && p.col == c)))
                            {
                                borderBottom = 3;
                            }
                            // Lewo - czy poprzednia komórka nie należy do tego słowa?
                            if (word.IsHorizontal && (posIndex == 0 || !positions.Any(p => p.row == r && p.col == c - 1)))
                            {
                                borderLeft = 3;
                            }
                            // Prawo - czy następna komórka nie należy do tego słowa?
                            if (word.IsHorizontal && (posIndex == positions.Count - 1 || !positions.Any(p => p.row == r && p.col == c + 1)))
                            {
                                borderRight = 3;
                            }
                        }
                    }
                    string borderThickness = $"{borderTop},{borderRight},{borderBottom},{borderLeft}";
                    
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
                        _logger?.DebugFormat("XamlGenerator.GenerateXaml: Oznaczam komórkę ({0}, {1}) z literą '{2}' i indeksem {3}", 
                            r, c, letter, letterIndex);
                        sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" Background=\"LightCoral\" BorderBrush=\"Black\" BorderThickness=\"{borderThickness}\">");
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
                        // Numer indeksu hasła (czerwony, lewy górny róg)
                        sb.AppendLine($"<TextBlock Text=\"{letterIndex}\" FontSize=\"10\" Foreground=\"DarkRed\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\"/>");
                        // Numer słowa (niebieski, prawy górny róg) - tylko jeśli to pierwsza litera słowa
                        // Definicje są wyświetlane w osobnym obszarze po prawej stronie
                        if (wordNumber > 0)
                        {
                            sb.AppendLine($"<TextBlock Text=\"{wordNumber}\" FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" Margin=\"0,0,2,0\"/>");
                        }
                        sb.AppendLine("</Grid>");
                        sb.AppendLine("</Border>");
                    }
                    else
                    {
                        // Zwykła litera - z ramką jeśli jest częścią słowa
                        if (wordsAtCell.Count > 0)
                        {
                            sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" BorderBrush=\"Black\" BorderThickness=\"{borderThickness}\">");
                            sb.AppendLine("<Grid>");
                            sb.AppendLine("<Grid.Resources>");
                            sb.AppendLine("<Style TargetType=\"TextBlock\">");
                            sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
                            sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
                            sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
                            sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
                            sb.AppendLine("</Style>");
                            sb.AppendLine("</Grid.Resources>");
                            sb.AppendLine($"<TextBlock Text=\"{letter}\"/>");
                            // Numer słowa (niebieski, prawy górny róg) - tylko jeśli to pierwsza litera słowa
                            if (wordNumber > 0)
                            {
                                // Tylko numer - definicje są w osobnym obszarze po prawej stronie
                                sb.AppendLine($"<TextBlock Text=\"{wordNumber}\" FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" Margin=\"0,0,2,0\"/>");
                            }
                            sb.AppendLine("</Grid>");
                            sb.AppendLine("</Border>");
                        }
                        else
                        {
                            // Zwykła litera bez ramki - dziedziczy wszystkie właściwości z Grid.Resources Style
                            sb.AppendLine($"<TextBlock Grid.Row=\"{r}\" Grid.Column=\"{c}\" Text=\"{letter}\"/>");
                        }
                    }
                }
                // Pusta kratka - pomijamy (domyślnie pusta)
            }
        }
        
        int actualHighlightedCount = highlightedCellsWithIndices?.Count ?? 0;
        _logger?.InfoFormat("XamlGenerator.GenerateXaml: Zakończono. Oznaczono {0} komórek w XAML", actualHighlightedCount);
        System.Diagnostics.Debug.WriteLine($"[CURSOR] XamlGenerator.GenerateXaml: Zakończono. Oznaczono {actualHighlightedCount} komórek w XAML");
        
        // Dodaj obszar z definicjami słów (jeśli są definicje)
        if (hasDefinitions)
        {
            sb.AppendLine($"<Border Grid.Column=\"{grid.Columns}\" Grid.Row=\"0\" Grid.RowSpan=\"{totalRows}\" BorderBrush=\"Gray\" BorderThickness=\"2\" Background=\"LightGray\" Margin=\"10,0,0,0\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\">");
            sb.AppendLine("<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Disabled\">");
            sb.AppendLine("<StackPanel Margin=\"10\">");
            sb.AppendLine("<TextBlock Text=\"Definicje słów:\" FontSize=\"14\" FontWeight=\"Bold\" Margin=\"0,0,0,10\"/>");
            
            // Sortuj słowa według Id
            var sortedWords = placedWords.OrderBy(w => w.Id).ToList();
            
            foreach (var word in sortedWords)
            {
                // Próbuj znaleźć definicję - sprawdź zarówno oryginalne słowo jak i znormalizowane (wielkie litery)
                string wordKey = word.Word.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Trim();
                string definition = "";
                
                // Sprawdź różne warianty klucza
                if (wordDefinitions!.ContainsKey(word.Word))
                {
                    definition = wordDefinitions[word.Word];
                }
                else if (wordDefinitions.ContainsKey(wordKey))
                {
                    definition = wordDefinitions[wordKey];
                }
                else
                {
                    // Spróbuj znaleźć przez case-insensitive porównanie
                    var matchingKey = wordDefinitions.Keys.FirstOrDefault(k => 
                        string.Equals(k, word.Word, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(k, wordKey, StringComparison.OrdinalIgnoreCase));
                    if (matchingKey != null)
                    {
                        definition = wordDefinitions[matchingKey];
                    }
                }
                
                _logger?.DebugFormat("XamlGenerator.GenerateXaml: Słowo '{0}' (key: '{1}') -> definicja: '{2}'", 
                    word.Word, wordKey, definition?.Substring(0, Math.Min(50, definition?.Length ?? 0)) ?? "BRAK");
                
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    // Escapuj definicję dla XML
                    string escapedDefinition = definition
                        .Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&apos;");
                    
                    sb.AppendLine("<Border BorderBrush=\"DarkBlue\" BorderThickness=\"1\" Margin=\"0,0,0,5\" Padding=\"5\" Background=\"White\">");
                    sb.AppendLine("<StackPanel>");
                    sb.AppendLine($"<TextBlock FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" Margin=\"0,0,0,3\">");
                    sb.AppendLine($"<Run Text=\"{word.Id}. \"/>");
                    sb.AppendLine($"<Run Text=\"{word.Word}\"/>");
                    sb.AppendLine("</TextBlock>");
                    sb.AppendLine($"<TextBlock Text=\"{escapedDefinition}\" FontSize=\"11\" TextWrapping=\"Wrap\" Foreground=\"Black\"/>");
                    sb.AppendLine("</StackPanel>");
                    sb.AppendLine("</Border>");
                }
            }
            
            sb.AppendLine("</StackPanel>");
            sb.AppendLine("</ScrollViewer>");
            sb.AppendLine("</Border>");
        }
        
        sb.AppendLine("</Grid>");
        sb.AppendLine("</ScrollViewer>");
        return sb.ToString();
    }
    
    /// <summary>
    /// Generuje pustą wersję XAML (bez liter, tylko ramki i definicje) - do wypełnienia ręcznie
    /// </summary>
    public string GenerateEmptyXaml(CrosswordGrid grid, int width = 500, int height = 500, Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null, List<CrosswordWord>? placedWords = null, Dictionary<string, string>? wordDefinitions = null)
    {
        int highlightedCount = highlightedCellsWithIndices?.Count ?? 0;
        _logger?.InfoFormat("XamlGenerator.GenerateEmptyXaml: Rozpoczynam generowanie pustej wersji XAML. Grid: {0}x{1}, Highlighted cells: {2}", 
            grid.Rows, grid.Columns, highlightedCount);
        System.Diagnostics.Debug.WriteLine($"[CURSOR] XamlGenerator.GenerateEmptyXaml: Rozpoczynam generowanie pustej wersji XAML. Grid: {grid.Rows}x{grid.Columns}, Highlighted cells: {highlightedCount}");
        
        // Utwórz mapy dla ramek i numeracji słów (identycznie jak w GenerateXaml)
        var cellToWords = new Dictionary<(int row, int col), List<CrosswordWord>>();
        var firstLetterPositions = new Dictionary<(int row, int col), int>();
        
        if (placedWords != null && placedWords.Count > 0)
        {
            foreach (var word in placedWords.OrderBy(w => w.Id))
            {
                var positions = word.GetCellPositions().ToList();
                var firstPos = positions.First();
                firstLetterPositions[firstPos] = word.Id;
                
                foreach (var pos in positions)
                {
                    if (!cellToWords.ContainsKey(pos))
                    {
                        cellToWords[pos] = new List<CrosswordWord>();
                    }
                    cellToWords[pos].Add(word);
                }
            }
        }
        
        var sb = new StringBuilder(capacity: 10000);
        
        // Oblicz rozmiar Grid
        const int cellSize = 35;
        bool hasDefinitions = wordDefinitions != null && wordDefinitions.Count > 0 && placedWords != null && placedWords.Count > 0;
        int totalRows = hasDefinitions ? grid.Rows + 2 : grid.Rows;
        int gridWidth = grid.Columns * cellSize + (hasDefinitions ? 300 + 10 : 0);
        int gridHeight = totalRows * cellSize;
        
        sb.AppendLine("<ScrollViewer xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\" Background=\"White\">");
        sb.AppendLine($"<Grid Width=\"{gridWidth}\" Height=\"{gridHeight}\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\" Background=\"White\">");
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
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>");
        }
        if (hasDefinitions)
        {
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>");
            sb.AppendLine($"<RowDefinition Height=\"{cellSize}\"/>");
        }
        sb.AppendLine("</Grid.RowDefinitions>");
        sb.AppendLine("<Grid.ColumnDefinitions>");
        for (int c = 0; c < grid.Columns; c++)
        {
            sb.AppendLine($"<ColumnDefinition Width=\"{cellSize}\"/>");
        }
        if (hasDefinitions)
        {
            sb.AppendLine("<ColumnDefinition Width=\"300\"/>");
        }
        sb.AppendLine("</Grid.ColumnDefinitions>");
        
        // Kratki - TYLKO ramki i numery, BEZ liter
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
                    // Komórka z literą - ale nie wyświetlamy litery, tylko ramkę i numery
                    bool isHighlighted = highlightedCellsWithIndices != null && highlightedCellsWithIndices.ContainsKey((r, c));
                    int letterIndex = isHighlighted ? highlightedCellsWithIndices[(r, c)] : 0;
                    
                    bool isFirstLetter = firstLetterPositions.ContainsKey((r, c));
                    int wordNumber = isFirstLetter ? firstLetterPositions[(r, c)] : 0;
                    
                    var wordsAtCell = cellToWords.ContainsKey((r, c)) ? cellToWords[(r, c)] : new List<CrosswordWord>();
                    
                    // Oblicz ramki (identycznie jak w GenerateXaml)
                    int borderTop = 1, borderBottom = 1, borderLeft = 1, borderRight = 1;
                    if (wordsAtCell.Count > 0)
                    {
                        foreach (var word in wordsAtCell)
                        {
                            var positions = word.GetCellPositions().ToList();
                            int posIndex = positions.FindIndex(p => p.row == r && p.col == c);
                            
                            if (word.IsVertical && (posIndex == 0 || !positions.Any(p => p.row == r - 1 && p.col == c)))
                            {
                                borderTop = 3;
                            }
                            if (word.IsVertical && (posIndex == positions.Count - 1 || !positions.Any(p => p.row == r + 1 && p.col == c)))
                            {
                                borderBottom = 3;
                            }
                            if (word.IsHorizontal && (posIndex == 0 || !positions.Any(p => p.row == r && p.col == c - 1)))
                            {
                                borderLeft = 3;
                            }
                            if (word.IsHorizontal && (posIndex == positions.Count - 1 || !positions.Any(p => p.row == r && p.col == c + 1)))
                            {
                                borderRight = 3;
                            }
                        }
                    }
                    string borderThickness = $"{borderTop},{borderRight},{borderBottom},{borderLeft}";
                    
                    if (isHighlighted && letterIndex > 0)
                    {
                        // Highlighted - Background + numer indeksu (BEZ litery)
                        sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" Background=\"LightCoral\" BorderBrush=\"Black\" BorderThickness=\"{borderThickness}\">");
                        sb.AppendLine("<Grid>");
                        sb.AppendLine("<Grid.Resources>");
                        sb.AppendLine("<Style TargetType=\"TextBlock\">");
                        sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
                        sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
                        sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
                        sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
                        sb.AppendLine("</Style>");
                        sb.AppendLine("</Grid.Resources>");
                        // Numer indeksu hasła (czerwony, lewy górny róg) - BEZ litery
                        sb.AppendLine($"<TextBlock Text=\"{letterIndex}\" FontSize=\"10\" Foreground=\"DarkRed\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\"/>");
                        // Numer słowa (niebieski, prawy górny róg)
                        if (wordNumber > 0)
                        {
                            sb.AppendLine($"<TextBlock Text=\"{wordNumber}\" FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" Margin=\"0,0,2,0\"/>");
                        }
                        sb.AppendLine("</Grid>");
                        sb.AppendLine("</Border>");
                    }
                    else
                    {
                        // Zwykła komórka - tylko ramka i numer słowa (BEZ litery)
                        if (wordsAtCell.Count > 0)
                        {
                            sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" BorderBrush=\"Black\" BorderThickness=\"{borderThickness}\">");
                            sb.AppendLine("<Grid>");
                            sb.AppendLine("<Grid.Resources>");
                            sb.AppendLine("<Style TargetType=\"TextBlock\">");
                            sb.AppendLine("<Setter Property=\"FontFamily\" Value=\"Segoe UI\"/>");
                            sb.AppendLine("<Setter Property=\"FontSize\" Value=\"20\"/>");
                            sb.AppendLine("<Setter Property=\"HorizontalAlignment\" Value=\"Center\"/>");
                            sb.AppendLine("<Setter Property=\"VerticalAlignment\" Value=\"Center\"/>");
                            sb.AppendLine("</Style>");
                            sb.AppendLine("</Grid.Resources>");
                            // Numer słowa (niebieski, prawy górny róg)
                            if (wordNumber > 0)
                            {
                                sb.AppendLine($"<TextBlock Text=\"{wordNumber}\" FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" Margin=\"0,0,2,0\"/>");
                            }
                            sb.AppendLine("</Grid>");
                            sb.AppendLine("</Border>");
                        }
                        else
                        {
                            // Pusta komórka - tylko ramka (jeśli była litera, ale nie ma słów)
                            sb.AppendLine($"<Border Grid.Row=\"{r}\" Grid.Column=\"{c}\" BorderBrush=\"LightGray\" BorderThickness=\"1\"/>");
                        }
                    }
                }
            }
        }
        
        // Dodaj obszar z definicjami słów (identycznie jak w GenerateXaml)
        if (hasDefinitions)
        {
            sb.AppendLine($"<Border Grid.Column=\"{grid.Columns}\" Grid.Row=\"0\" Grid.RowSpan=\"{totalRows}\" BorderBrush=\"Gray\" BorderThickness=\"2\" Background=\"LightGray\" Margin=\"10,0,0,0\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Top\">");
            sb.AppendLine("<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Disabled\">");
            sb.AppendLine("<StackPanel Margin=\"10\">");
            sb.AppendLine("<TextBlock Text=\"Definicje słów:\" FontSize=\"14\" FontWeight=\"Bold\" Margin=\"0,0,0,10\"/>");
            
            var sortedWords = placedWords.OrderBy(w => w.Id).ToList();
            
            foreach (var word in sortedWords)
            {
                string wordKey = word.Word.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Trim();
                string definition = "";
                
                if (wordDefinitions!.ContainsKey(word.Word))
                {
                    definition = wordDefinitions[word.Word];
                }
                else if (wordDefinitions.ContainsKey(wordKey))
                {
                    definition = wordDefinitions[wordKey];
                }
                else
                {
                    var matchingKey = wordDefinitions.Keys.FirstOrDefault(k => 
                        string.Equals(k, word.Word, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(k, wordKey, StringComparison.OrdinalIgnoreCase));
                    if (matchingKey != null)
                    {
                        definition = wordDefinitions[matchingKey];
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    string escapedDefinition = definition
                        .Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&apos;");
                    
                    sb.AppendLine("<Border BorderBrush=\"DarkBlue\" BorderThickness=\"1\" Margin=\"0,0,0,5\" Padding=\"5\" Background=\"White\">");
                    sb.AppendLine("<StackPanel>");
                    sb.AppendLine($"<TextBlock FontSize=\"12\" FontWeight=\"Bold\" Foreground=\"Blue\" Margin=\"0,0,0,3\">");
                    sb.AppendLine($"<Run Text=\"{word.Id}. \"/>");
                    sb.AppendLine($"<Run Text=\"{word.Word}\"/>");
                    sb.AppendLine("</TextBlock>");
                    sb.AppendLine($"<TextBlock Text=\"{escapedDefinition}\" FontSize=\"11\" TextWrapping=\"Wrap\" Foreground=\"Black\"/>");
                    sb.AppendLine("</StackPanel>");
                    sb.AppendLine("</Border>");
                }
            }
            
            sb.AppendLine("</StackPanel>");
            sb.AppendLine("</ScrollViewer>");
            sb.AppendLine("</Border>");
        }
        
        sb.AppendLine("</Grid>");
        sb.AppendLine("</ScrollViewer>");
        return sb.ToString();
    }
}

