# Dokumentacja API - Crossword AI Generator

## 📚 Spis treści

1. [Domain Layer](#domain-layer)
2. [Infrastructure Layer](#infrastructure-layer)
3. [Application Layer](#application-layer)
4. [WPF Layer](#wpf-layer)

## 🏛️ Domain Layer

### Models

#### `CrosswordGrid`

Reprezentacja siatki krzyżówki.

```csharp
public class CrosswordGrid
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<(int row, int col), CrosswordCell> Cells { get; set; }
    
    public CrosswordCell GetCell(int row, int col);
    public void SetCell(int row, int col, CrosswordCellType type);
    public bool IsValidPosition(int row, int col);
}
```

**Właściwości:**
- `Rows` - Liczba wierszy
- `Columns` - Liczba kolumn
- `Cells` - Słownik komórek (klucz: `(row, col)`, wartość: `CrosswordCell`)

**Metody:**
- `GetCell(int row, int col)` - Zwraca komórkę na pozycji
- `SetCell(int row, int col, CrosswordCellType type)` - Ustawia typ komórki
- `IsValidPosition(int row, int col)` - Sprawdza czy pozycja jest poprawna

#### `CrosswordCell`

Pojedyncza komórka w siatce.

```csharp
public class CrosswordCell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public CrosswordCellType Type { get; set; }
    public char? Letter { get; set; }
    
    public bool IsEmpty => Type == CrosswordCellType.Empty;
    public bool HasLetter => Type == CrosswordCellType.Letter && Letter.HasValue;
    public bool IsWall => Type == CrosswordCellType.Wall;
}
```

**Typy komórek:**
- `Empty` - Pusta komórka
- `Letter` - Komórka z literą
- `Wall` - Czarna ściana

#### `CrosswordWord`

Słowo w krzyżówce.

```csharp
public class CrosswordWord
{
    public int Id { get; set; }
    public string Word { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public WordDirection Direction { get; set; }
    public string Clue { get; set; }
    
    public int Length => Word?.Length ?? 0;
    public bool IsHorizontal => Direction == WordDirection.Across;
    public bool IsVertical => Direction == WordDirection.Down;
    
    public IEnumerable<(int row, int col)> GetCellPositions();
}
```

**Kierunki:**
- `Across` - Poziomo (→)
- `Down` - Pionowo (↓)

### Services (Interfejsy)

#### `IEmptyGridGenerator`

Generator pustych siatek krzyżówek.

```csharp
public interface IEmptyGridGenerator
{
    CrosswordGrid GenerateEmptyGrid(int rows, int columns);
    CrosswordGrid GenerateEmptyGridWithWalls(int rows, int columns, double wallProbability = 0.1);
    CrosswordGrid GenerateEmptyGridWithWallCount(int rows, int columns, int wallCount);
}
```

**Metody:**
- `GenerateEmptyGrid(int rows, int columns)` - Generuje pustą siatkę
- `GenerateEmptyGridWithWalls(int rows, int columns, double wallProbability)` - Generuje siatkę z losowymi ścianami
- `GenerateEmptyGridWithWallCount(int rows, int columns, int wallCount)` - Generuje siatkę z określoną liczbą ścian

#### `IWordDictionary`

Słownik słów do generowania krzyżówek.

```csharp
public interface IWordDictionary
{
    List<string> GetWordsContaining(char letter, int minLength = 6, int maxLength = 20, int maxResults = 1000);
    string? GetRandomWord(int minLength = 6, int maxLength = 20);
    string? GetRandomWordContaining(char letter, int minLength = 6, int maxLength = 20);
    string? GetRandomWordOfLength(int minLength = 6, int maxLength = 12);
}
```

**Metody:**
- `GetWordsContaining(char letter, ...)` - Zwraca listę słów zawierających literę
- `GetRandomWord(...)` - Losuje słowo z całego słownika
- `GetRandomWordContaining(char letter, ...)` - Losuje słowo zawierające literę
- `GetRandomWordOfLength(...)` - Losuje słowo o określonej długości

#### `IHighlightedWordGenerator`

Generator haseł (słów wyróżnionych) z cache'owaniem.

```csharp
public interface IHighlightedWordGenerator
{
    Result<string, string> GetRandomWord(int minLength = 6, int maxLength = 8);
    Result<List<string>, string> GenerateWords(int count, int minLength = 6, int maxLength = 8);
    Result<bool, string> PreloadWords(int count, int minLength = 6, int maxLength = 8);
}
```

**Metody:**
- `GetRandomWord(...)` - Losuje pojedyncze słowo (używa cache)
- `GenerateWords(int count, ...)` - Generuje listę słów
- `PreloadWords(int count, ...)` - Pre-ładuje słowa do cache

#### `ICursorLogger`

Logger dla debugowania przez AI asystenta.

```csharp
public interface ICursorLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    
    void DebugFormat(string format, params object[] args);
    void InfoFormat(string format, params object[] args);
    void WarningFormat(string format, params object[] args);
    void ErrorFormat(string format, Exception? exception, params object[] args);
}
```

**Poziomy logowania:**
- `Debug` - Szczegółowe informacje debugowania
- `Info` - Informacje ogólne
- `Warning` - Ostrzeżenia
- `Error` - Błędy

### Common

#### `Result<TValue, TError>`

Railway Oriented Programming pattern dla spójnej obsługi błędów.

```csharp
public class Result<TValue, TError>
{
    public TValue? Value { get; }
    public TError? Error { get; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => Error != null;
    
    public static Result<TValue, TError> Success(TValue value);
    public static Result<TValue, TError> Failure(TError error);
    
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> func);
    public Result<TNewValue, TError> Bind<TNewValue>(Func<TValue, Result<TNewValue, TError>> func);
    public Result<TValue, TError> OnSuccess(Action<TValue> action);
    public Result<TValue, TError> OnFailure(Action<TError> action);
}
```

**Przykład użycia:**
```csharp
var result = GetWord();
if (result.IsSuccess)
{
    Console.WriteLine(result.Value);
}
else
{
    Console.WriteLine($"Błąd: {result.Error}");
}
```

## 🔧 Infrastructure Layer

### Services

#### `XamlGenerator` (implements `IXamlGenerator`)

Generator XAML z `CrosswordGrid`.

```csharp
public class XamlGenerator : IXamlGenerator
{
    public XamlGenerator(ICursorLogger? logger = null);
    
    public string GenerateXaml(
        CrosswordGrid grid, 
        int width = 500, 
        int height = 500, 
        Dictionary<(int row, int col), int>? highlightedCellsWithIndices = null);
}
```

**Parametry:**
- `grid` - Siatka krzyżówki
- `width` - Szerokość (domyślnie 500)
- `height` - Wysokość (domyślnie 500)
- `highlightedCellsWithIndices` - Pozycje wyróżnionych komórek z indeksami (hasło główne)

**Zwraca:** String XAML

**Przykład:**
```csharp
var generator = new XamlGenerator(logger);
var xaml = generator.GenerateXaml(grid, 500, 500, highlightedCells);
```

#### `LazyWordDictionary` (implements `IWordDictionary`)

Optymalizowana implementacja słownika z leniwym ładowaniem.

```csharp
public class LazyWordDictionary : IWordDictionary
{
    public LazyWordDictionary(string filePath, int? seed = null, int minWordLength = 6, ICursorLogger? logger = null);
    
    public void LoadIndex();
    public void PreloadWordsForLetters(IEnumerable<char> letters, int wordsPerLetter = 200);
}
```

**Zalety:**
- Szybki start (tylko indeksowanie, nie cały plik)
- Niskie użycie pamięci (tylko cache)
- Obsługa `.gz` (GZip)

**Metody dodatkowe:**
- `LoadIndex()` - Ładuje indeks (automatycznie przy pierwszym użyciu)
- `PreloadWordsForLetters(...)` - Pre-ładuje słowa dla liter

#### `EmptyGridGenerator` (implements `IEmptyGridGenerator`)

Implementacja generatora pustych siatek.

```csharp
public class EmptyGridGenerator : IEmptyGridGenerator
{
    public EmptyGridGenerator(int? seed = null);
    
    public CrosswordGrid GenerateEmptyGrid(int rows, int columns);
    public CrosswordGrid GenerateEmptyGridWithWalls(int rows, int columns, double wallProbability = 0.1);
    public CrosswordGrid GenerateEmptyGridWithWallCount(int rows, int columns, int wallCount);
}
```

**Parametry:**
- `seed` - Ziarno losowe (dla determinizmu)

#### `HighlightedWordGenerator` (implements `IHighlightedWordGenerator`)

Generator haseł z cache'owaniem.

```csharp
public class HighlightedWordGenerator : IHighlightedWordGenerator
{
    public HighlightedWordGenerator(IWordDictionary wordDictionary, int? seed = null, ICursorLogger? logger = null);
    
    public Result<string, string> GetRandomWord(int minLength = 6, int maxLength = 8);
    public Result<List<string>, string> GenerateWords(int count, int minLength = 6, int maxLength = 8);
    public Result<bool, string> PreloadWords(int count, int minLength = 6, int maxLength = 8);
}
```

**Zalety:**
- Cache dla szybkiego dostępu
- Pre-loading dla wydajności
- Używa `IWordDictionary` (może być `LazyWordDictionary`)

#### `CursorLogger` (implements `ICursorLogger`)

Implementacja loggera.

```csharp
public class CursorLogger : ICursorLogger
{
    public CursorLogger();
    
    // Implementuje wszystkie metody z ICursorLogger
}
```

**Lokalizacja logów:**
```
bin/Debug/net8.0-windows/logs/cursor_YYYY-MM-DD.log
```

## 🎯 Application Layer

### Services

#### `DatasetGenerator`

Główny orchestrator generowania datasetów.

```csharp
public class DatasetGenerator
{
    public DatasetGenerator(
        IEmptyGridGenerator gridGenerator,
        IXamlGenerator xamlGenerator,
        IWordDictionary? wordDictionary,
        CrosswordWordPlacer wordPlacer,
        IHighlightedWordGenerator? wordGenerator = null,
        ICursorLogger? logger = null);
    
    public static string? FindDictionaryFile();
    
    public DatasetEntry GenerateEmptyGridExample(
        int rows, int columns, bool hasWalls = false, double wallProbability = 0.1);
    
    public Result<DatasetEntry, string> GenerateWithWordsExample(
        int rows, int columns, int targetWordCount, int? seed = null, string? highlightedWord = null);
    
    public List<DatasetEntry> GenerateEmptyGridDataset(
        int count, int minSize = 5, int maxSize = 15, bool includeWithWalls = true, double wallProbability = 0.1);
    
    public List<DatasetEntry> GenerateWithWordsDataset(
        int count,
        int minSize = 8,
        int maxSize = 15,
        int targetWordCount = 5,
        string? highlightedWord = null,
        Action<int, int>? onProgress = null);
}
```

**Metody:**
- `GenerateEmptyGridExample(...)` - Generuje pojedynczy przykład pustej siatki
- `GenerateWithWordsExample(...)` - Generuje pojedynczy przykład krzyżówki ze słowami
- `GenerateEmptyGridDataset(...)` - Generuje dataset pustych siatek
- `GenerateWithWordsDataset(...)` - Generuje dataset krzyżówek ze słowami
  - `onProgress` - Callback dla raportowania postępu `(current, total)`

**Przykład:**
```csharp
var generator = new DatasetGenerator(gridGen, xamlGen, wordDict, wordPlacer, wordGen, logger);

// Pojedynczy przykład
var entry = generator.GenerateEmptyGridExample(15, 15, hasWalls: true, wallProbability: 0.1);

// Dataset z postępem
var entries = generator.GenerateWithWordsDataset(
    count: 100,
    minSize: 12,
    maxSize: 20,
    highlightedWord: "KOT",
    onProgress: (current, total) => Console.WriteLine($"{current}/{total}"));
```

#### `IConfigService` / `ConfigService`

Centralizacja stałych ("magic numbers").

```csharp
public interface IConfigService
{
    int MinGridSize { get; }
    int MaxGridSize { get; }
    int MinWordCount { get; }
    int MaxWordCount { get; }
    int MinWordLength { get; }
    int MaxWordLength { get; }
    // ... więcej stałych
}

public class ConfigService : IConfigService
{
    // Implementacja z domyślnymi wartościami
}
```

## 🖥️ WPF Layer

### ViewModels

#### `MainWindowViewModel`

ViewModel dla głównego okna.

```csharp
public partial class MainWindowViewModel : BaseViewModel
{
    public MainWindowViewModel(
        IEmptyGridGenerator gridGenerator,
        IXamlGenerator xamlGenerator,
        IScreenshotService screenshotService,
        DatasetGenerator datasetGenerator);
    
    [RelayCommand]
    private async Task GenerateSingleAsync();
    
    [RelayCommand]
    private async Task GenerateDatasetAsync();
    
    [RelayCommand]
    private async Task SaveSingleScreenshotAsync();
    
    [RelayCommand]
    private async Task SaveAllScreenshotsAsync();
    
    [RelayCommand]
    private void ExportToJson();
    
    [RelayCommand]
    private void ClearData();
}
```

**Właściwości:**
- `GridSizeRows` - Wysokość siatki
- `GridSizeColumns` - Szerokość siatki
- `GenerateWithWords` - Tryb (pusta siatka / ze słowami)
- `HighlightedWord` - Hasło główne
- `DatasetCount` - Liczba przykładów w datasecie
- `XamlText` - Wygenerowany XAML
- `DatasetEntries` - Lista wygenerowanych przykładów
- `StatusMessage` - Status operacji
- `GeneratedCount` - Liczba wygenerowanych przykładów

### Services

#### `IScreenshotService` / `ScreenshotService`

Serwis do robienia screenshotów z WPF Controls.

```csharp
public interface IScreenshotService
{
    string CaptureToBase64(FrameworkElement element, int? width = null, int? height = null);
    void CaptureToJpg(FrameworkElement element, string filePath, int? width = null, int? height = null, int quality = 90);
}

public class ScreenshotService : IScreenshotService
{
    // Implementacja
}
```

**Metody:**
- `CaptureToBase64(...)` - Robi screenshot i zwraca jako base64 string
- `CaptureToJpg(...)` - Robi screenshot i zapisuje jako JPG

**Przykład:**
```csharp
var service = new ScreenshotService();
service.CaptureToJpg(grid, "output.jpg", width: 500, height: 500, quality: 90);
```

## 🔌 Dependency Injection

### Konfiguracja Core

```csharp
services.AddCrosswordAIGeneratorCore();
```

**Rejestruje:**
- `ICursorLogger` → `CursorLogger` (Singleton)
- `IConfigService` → `ConfigService` (Singleton)
- `IEmptyGridGenerator` → `EmptyGridGenerator` (Singleton)
- `IXamlGenerator` → `XamlGenerator` (Singleton, factory)
- `IWordDictionary` → `LazyWordDictionary` (Singleton, factory)
- `IHighlightedWordGenerator` → `HighlightedWordGenerator` (Singleton, factory)
- `DatasetGenerator` → `DatasetGenerator` (Singleton, factory)

### Konfiguracja WPF

```csharp
services.AddWpfInfrastructure();
```

**Rejestruje:**
- `IScreenshotService` → `ScreenshotService` (Singleton)
- `MainWindowViewModel` (Transient)
- `MainWindow` (Transient)

## 📝 Przykłady użycia

### Przykład 1: Generowanie pustej siatki

```csharp
var gridGenerator = new EmptyGridGenerator();
var xamlGenerator = new XamlGenerator();

var grid = gridGenerator.GenerateEmptyGridWithWalls(15, 15, wallProbability: 0.1);
var xaml = xamlGenerator.GenerateXaml(grid, 500, 500);
```

### Przykład 2: Generowanie krzyżówki ze słowami

```csharp
var wordDict = new LazyWordDictionary("dictionaries/slowa.txt", logger: logger);
wordDict.LoadIndex();

var wordPlacer = new CrosswordWordPlacer(wordDict, logger: logger);
var result = wordPlacer.GenerateWithWords(15, 15, targetWordCount: 5, highlightedWord: "KOT");

if (result.IsSuccess)
{
    var (grid, words, highlightedCells) = result.Value;
    var xaml = xamlGenerator.GenerateXaml(grid, 500, 500, highlightedCells);
}
```

### Przykład 3: Generowanie datasetu

```csharp
var datasetGenerator = new DatasetGenerator(
    gridGenerator, xamlGenerator, wordDict, wordPlacer, wordGenerator, logger);

var entries = datasetGenerator.GenerateWithWordsDataset(
    count: 100,
    minSize: 12,
    maxSize: 20,
    highlightedWord: "KOT",
    onProgress: (current, total) => 
    {
        Console.WriteLine($"Progress: {current}/{total}");
    });
```

---

**Ostatnia aktualizacja:** 2025-11-19

