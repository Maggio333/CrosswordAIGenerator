using System.Text;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Application.Services;

/// <summary>
/// Generator opisów, searchable text i embedding text dla datasetów
/// </summary>
public class DatasetDescriptionGenerator : IDatasetDescriptionGenerator
{
    public string GenerateDescription(CrosswordGrid grid, bool hasWalls, int wallCount)
    {
        var sb = new StringBuilder();
        sb.Append($"Pusta siatka krzyżówki {grid.Rows}x{grid.Columns}. ");
        sb.Append($"Grid z {grid.Rows} wierszami i {grid.Columns} kolumnami. ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"Zawiera {wallCount} ścian (czarne kratki z Background=\"Black\"). ");
        }
        else
        {
            sb.Append("Brak ścian. ");
        }
        
        int emptyCount = grid.Cells.Values.Count(c => c.IsEmpty);
        sb.Append($"Wszystkie pozostałe kratki są puste (Border bez TextBlock, BorderBrush=\"Black\", BorderThickness=\"1\"). ");
        sb.Append($"Łącznie {emptyCount} pustych kratek.");
        
        return sb.ToString();
    }

    public string GenerateSearchableText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount, string xaml)
    {
        var sb = new StringBuilder();
        sb.Append($"XAML WPF Grid {rows} wierszy {cols} kolumn krzyżówka ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"ściany Background Black {wallCount} ścian ");
        }
        
        sb.Append($"puste kratki Border Black BorderThickness 1 ");
        sb.Append($"BorderBrush Black ");
        
        // Dodaj fragmenty XAML dla lepszego wyszukiwania
        if (xaml.Contains("TextBlock"))
        {
            sb.Append("TextBlock FontSize 20 HorizontalAlignment Center VerticalAlignment Center ");
        }
        
        sb.Append($"{grid.Cells.Values.Count(c => c.IsEmpty)} pustych kratek ");
        
        return sb.ToString().Trim();
    }

    public string GenerateEmbeddingText(CrosswordGrid grid, int rows, int cols, bool hasWalls, int wallCount)
    {
        var sb = new StringBuilder();
        sb.Append($"Krzyżówka WPF XAML Grid {rows}x{cols} ");
        
        if (hasWalls && wallCount > 0)
        {
            sb.Append($"z {wallCount} ścianami ");
        }
        else
        {
            sb.Append("bez ścian ");
        }
        
        sb.Append($"puste kratki Border Black ");
        sb.Append($"przykład XAML dla nauki generowania layoutu krzyżówek");
        
        return sb.ToString().Trim();
    }

    public string GenerateWordsDescription(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new StringBuilder();
        sb.Append($"Krzyżówka {rows}x{cols} z {placedWords.Count} słowami. ");
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"Hasło główne: {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))}. ");
        }
        
        sb.Append($"Słowa w krzyżówce: ");
        for (int i = 0; i < placedWords.Count; i++)
        {
            var word = placedWords[i];
            sb.Append($"{word.Word}");
            if (i < placedWords.Count - 1)
                sb.Append(", ");
        }
        sb.Append(". ");
        
        // Znajdź przecięcia
        var intersections = WordIntersectionFinder.FindIntersections(placedWords);
        if (intersections.Count > 0)
        {
            sb.Append($"Przecięcia: ");
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                sb.Append($"{intersection.Word1} i {intersection.Word2} przecinają się w literze '{intersection.Letter}' na pozycji ({intersection.Row}, {intersection.Column})");
                if (i < intersections.Count - 1)
                    sb.Append("; ");
            }
            sb.Append(". ");
        }
        
        sb.Append($"Zawiera {letterCount} kratek z literami. Grid z białym tłem, czarne ramki wokół kratek.");
        return sb.ToString();
    }

    public string GenerateWordsSearchableText(CrosswordGrid grid, int rows, int cols, int letterCount, string xaml, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new StringBuilder();
        sb.Append($"XAML WPF Grid {rows} wierszy {cols} kolumn krzyżówka ze słowami ");
        
        // Dodaj wszystkie słowa
        foreach (var word in placedWords)
        {
            sb.Append($"{word.Word} ");
        }
        
        // Dodaj przecięcia
        var intersections = WordIntersectionFinder.FindIntersections(placedWords);
        foreach (var intersection in intersections)
        {
            sb.Append($"przecięcie {intersection.Word1} {intersection.Word2} litera {intersection.Letter} pozycja {intersection.Row} {intersection.Column} ");
        }
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"hasło główne {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))} ");
        }
        
        sb.Append($"{letterCount} liter TextBlock FontSize 20 ");
        sb.Append($"Border Black BorderThickness 1 Background White ");
        sb.Append($"słowa przecinają się przecięcia");
        return sb.ToString();
    }

    public string GenerateWordsEmbeddingText(CrosswordGrid grid, int rows, int cols, int letterCount, List<CrosswordWord> placedWords, string? highlightedWord)
    {
        var sb = new StringBuilder();
        sb.Append($"Krzyżówka WPF XAML Grid {rows}x{cols} z {placedWords.Count} rzeczywistymi słowami. ");
        
        if (!string.IsNullOrWhiteSpace(highlightedWord))
        {
            sb.Append($"Hasło główne: {highlightedWord.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"))}. ");
        }
        
        sb.Append($"Słowa: ");
        foreach (var word in placedWords)
        {
            sb.Append($"{word.Word} ({word.Direction}), ");
        }
        
        // Dodaj informacje o przecięciach
        var intersections = WordIntersectionFinder.FindIntersections(placedWords);
        if (intersections.Count > 0)
        {
            sb.Append($"Przecięcia: ");
            foreach (var intersection in intersections)
            {
                sb.Append($"{intersection.Word1}×{intersection.Word2} w '{intersection.Letter}', ");
            }
        }
        
        sb.Append($"{letterCount} liter słowa przecinają się przykład XAML dla nauki generowania layoutu krzyżówek ze słowami");
        return sb.ToString();
    }

    public string GenerateCustomWordsDescription(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Krzyżówka z hasłem głównym: {highlightedWord}");
        sb.AppendLine($"Liczba słów w krzyżówce: {placedWords.Count} (z {customWords.Count} dostępnych)");
        sb.AppendLine();
        sb.AppendLine("Słowa w krzyżówce:");
        for (int i = 0; i < placedWords.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {placedWords[i].Word}");
        }
        if (placedWords.Count < customWords.Count)
        {
            sb.AppendLine();
            sb.AppendLine($"Nieużyte słowa ({customWords.Count - placedWords.Count}):");
            var usedWords = placedWords.Select(w => w.Word).ToHashSet();
            var unusedWords = customWords.Where(w => !usedWords.Contains(w.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Trim())).ToList();
            for (int i = 0; i < unusedWords.Count; i++)
            {
                sb.AppendLine($"- {unusedWords[i]}");
            }
        }
        return sb.ToString();
    }

    public string GenerateSearchableTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns, string xaml)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hasło główne: {highlightedWord}");
        sb.AppendLine($"Rozmiar siatki: {rows}x{columns}");
        sb.AppendLine();
        sb.AppendLine("Słowa:");
        foreach (var word in customWords)
        {
            sb.AppendLine($"- {word}");
        }
        sb.AppendLine();
        sb.AppendLine("Umieszczone słowa:");
        foreach (var word in placedWords)
        {
            sb.AppendLine($"- {word.Word} ({word.Direction})");
        }
        return sb.ToString();
    }

    public string GenerateEmbeddingTextForCustomWords(string highlightedWord, List<string> customWords, List<CrosswordWord> placedWords, int rows, int columns)
    {
        var sb = new StringBuilder();
        sb.Append($"Krzyżówka hasło {highlightedWord} ");
        sb.Append($"słowa {string.Join(" ", customWords)} ");
        sb.Append($"rozmiar {rows}x{columns}");
        return sb.ToString();
    }
}

