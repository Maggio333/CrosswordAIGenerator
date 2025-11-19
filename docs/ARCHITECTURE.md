# Architektura - Crossword AI Generator

## 📐 Przegląd architektury

Projekt wykorzystuje **Clean Architecture** z wyraźnym podziałem na warstwy i zasadami SOLID. Core jest niezależny od UI, co pozwala na użycie go w różnych frameworkach (WPF, MAUI, Blazor, Console).

## 🏛️ Warstwy architektury

### 1. Domain Layer (`CrosswordAIGenerator.Core/Domain/`)

**Cel:** Zawiera logikę biznesową i interfejsy - **niezależna od infrastruktury**

#### Models (`Domain/Models/`)
- `CrosswordGrid` - reprezentacja siatki krzyżówki
- `CrosswordCell` - pojedyncza kratka (Empty, Letter, Wall)
- `CrosswordWord` - słowo w krzyżówce (pozycja, kierunek, długość)
- `CrosswordSpecification` - specyfikacja dla LLM (prompt)

#### Services (`Domain/Services/`)
**Tylko interfejsy:**
- `IEmptyGridGenerator` - generowanie pustych siatek
- `IWordDictionary` - słownik słów
- `ICursorLogger` - logger dla debugowania
- `CrosswordWordPlacer` - logika układania słów (konkretna implementacja, ale w Domain bo to logika biznesowa)

#### Common (`Domain/Common/`)
- `Result<TValue, TError>` - Railway Oriented Programming pattern

**Zasada:** Domain nie wie o Infrastructure - tylko interfejsy!

### 2. Infrastructure Layer (`CrosswordAIGenerator.Core/Infrastructure/`)

**Cel:** Konkretne implementacje infrastrukturalne (I/O, zewnętrzne serwisy)

#### Services (`Infrastructure/Services/`)
- `EmptyGridGenerator` - implementacja `IEmptyGridGenerator`
- `WordDictionary` - implementacja `IWordDictionary` (wczytuje cały słownik do pamięci)
- `LazyWordDictionary` - optymalizowana implementacja `IWordDictionary` (leniwe ładowanie)
- `XamlGenerator` - generowanie XAML z `CrosswordGrid`
- `CrossGridGenerator` - generowanie i parsowanie formatu CrossGrid (ASCII art)
- `CrossGridValidationResult` - wynik walidacji formatu CrossGrid
- `CursorLogger` - implementacja `ICursorLogger`

**Zasada:** Infrastructure implementuje interfejsy z Domain

### 3. Application Layer (`CrosswordAIGenerator.Core/Application_/`)

**Cel:** Orkiestracja - łączy Domain i Infrastructure

#### Services (`Application_/Services/`)
- `DatasetGenerator` - główny orchestrator generowania datasetów
  - Używa `IEmptyGridGenerator`, `IXamlGenerator`, `IWordDictionary`, `ICrossGridGenerator`
  - Generuje `DatasetEntry` z XAML, CrossGrid, opisami, metadanymi
  - Eksport do JSON (z filtrowaniem zgodnie z ustawieniami)
  - Eksport do JSONL (format gotowy do finetunowania)
- `DatasetSettings` - ustawienia kontrolujące które elementy są zawarte w eksportowanych datasetach
- `IConfigService` / `ConfigService` - centralizacja stałych ("magic numbers")

**Zasada:** Application koordynuje Domain i Infrastructure

### 4. Presentation Layer (`CrosswordAIGenerator.WPF/`)

**Cel:** UI - **można wymienić na inną warstwę prezentacji**

#### MVVM Pattern
- **Model:** `DatasetEntry`, `CrosswordGrid` (z Core)
- **View:** `MainWindow.xaml`, `CrosswordView.xaml`
- **ViewModel:** `MainWindowViewModel`, `DatasetGeneratorViewModel`

#### Infrastructure (`WPF/Infrastructure/`)
- `IScreenshotService` / `ScreenshotService` - screenshoty WPF Controls
- `DependencyInjection` - rejestracja WPF-specific services

**Zasada:** Presentation zależy od Core, ale Core nie wie o Presentation

## 🔌 Dependency Injection

### Konfiguracja Core (`Core/DependencyInjection.cs`)

```csharp
public static IServiceCollection AddCrosswordAIGeneratorCore(this IServiceCollection services)
{
    // Logger
    services.AddSingleton<ICursorLogger, CursorLogger>();
    
    // Application Services
    services.AddSingleton<IConfigService, ConfigService>();
    
    // Domain Services (interfejsy)
    services.AddSingleton<IEmptyGridGenerator, EmptyGridGenerator>();
    
    // Infrastructure Services
    services.AddSingleton<IXamlGenerator>(sp => 
        new XamlGenerator(sp.GetService<ICursorLogger>()));
    
    // Factory pattern dla IWordDictionary
    services.AddSingleton<IWordDictionary>(sp => 
    {
        var logger = sp.GetService<ICursorLogger>();
        var path = DatasetGenerator.FindDictionaryFile();
        return path != null 
            ? new LazyWordDictionary(path, logger: logger)
            : WordDictionary.CreateDefaultFallback();
    });
    
    // Factory pattern dla DatasetGenerator
    services.AddSingleton<DatasetGenerator>(sp => 
    {
        var gridGen = sp.GetRequiredService<IEmptyGridGenerator>();
        var xamlGen = sp.GetRequiredService<IXamlGenerator>();
        var wordDict = sp.GetRequiredService<IWordDictionary>();
        var logger = sp.GetService<ICursorLogger>();
        var wordPlacer = new CrosswordWordPlacer(wordDict, logger: logger);
        return new DatasetGenerator(gridGen, xamlGen, wordDict, wordPlacer, logger);
    });
    
    return services;
}
```

### Konfiguracja WPF (`WPF/Infrastructure/DependencyInjection.cs`)

```csharp
public static IServiceCollection AddWpfInfrastructure(this IServiceCollection services)
{
    // WPF Infrastructure
    services.AddSingleton<IScreenshotService, ScreenshotService>();
    
    // ViewModels i Views
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<DatasetGeneratorViewModel>();
    services.AddTransient<MainWindow>();
    services.AddTransient<DatasetGeneratorWindow>();
    
    return services;
}
```

### Entry Point (`WPF/App.xaml.cs`)

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    var services = new ServiceCollection();
    services.AddCrosswordAIGeneratorCore();  // Core
    services.AddWpfInfrastructure();         // WPF
    _serviceProvider = services.BuildServiceProvider();
    
    var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
    mainWindow.Show();
}
```

## 🎯 Zasady projektowe

### SOLID Principles

1. **Single Responsibility Principle (SRP)**
   - Każda klasa ma jedną odpowiedzialność
   - `XamlGenerator` - tylko generowanie XAML
   - `DatasetGenerator` - tylko orkiestracja

2. **Open/Closed Principle (OCP)**
   - Interfejsy pozwalają na rozszerzanie bez modyfikacji
   - `IWordDictionary` - można dodać nową implementację (np. `DatabaseWordDictionary`)

3. **Liskov Substitution Principle (LSP)**
   - Implementacje są zamienne przez interfejsy
   - `LazyWordDictionary` i `WordDictionary` - oba implementują `IWordDictionary`

4. **Interface Segregation Principle (ISP)**
   - Interfejsy są małe i skupione
   - `IEmptyGridGenerator` - tylko generowanie siatek
   - `IWordDictionary` - tylko operacje na słowniku

5. **Dependency Inversion Principle (DIP)**
   - Zależności przez interfejsy, nie konkretne klasy
   - `DatasetGenerator` zależy od `IWordDictionary`, nie `LazyWordDictionary`

### Railway Oriented Programming (ROP)

Zamiast wyjątków używamy `Result<TValue, TError>`:

```csharp
// Zamiast:
public string GetWord() { throw new Exception("Brak słów"); }

// Używamy:
public Result<string, string> GetWord() 
{ 
    return words.Count == 0 
        ? Result<string, string>.Failure("Brak słów")
        : Result<string, string>.Success(words[0]);
}
```

**Korzyści:**
- Spójna obsługa błędów
- Brak ukrytych wyjątków
- Łatwiejsze testowanie
- Kompozycja operacji (`.Map()`, `.Bind()`)

## 📦 Modele danych

### CrosswordGrid

```csharp
public class CrosswordGrid
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<(int row, int col), CrosswordCell> Cells { get; set; }
    
    public CrosswordCell GetCell(int row, int col) { ... }
    public void SetCell(int row, int col, CrosswordCellType type) { ... }
}
```

### CrosswordWord

```csharp
public class CrosswordWord
{
    public int Id { get; set; }
    public string Word { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public WordDirection Direction { get; set; }
    
    public IEnumerable<(int row, int col)> GetCellPositions() { ... }
}
```

### DatasetEntry

```csharp
public class DatasetEntry
{
    public string Id { get; set; }
    public string Type { get; set; }  // "empty_grid" | "crossword_with_words" | "custom_words"
    public string GridSize { get; set; }
    public bool HasWalls { get; set; }
    public string Xaml { get; set; }
    public string? EmptyXaml { get; set; }  // Pusta wersja (bez liter, tylko ramki i definicje)
    public string? CrossGrid { get; set; }  // Format ASCII art dla LLM
    public string Description { get; set; }
    public string SearchableText { get; set; }
    public DatasetMetadata Metadata { get; set; }
    public RagMetadata? RagMetadata { get; set; }
}
```

## 🔄 Flow generowania krzyżówki

### 1. Generowanie pustej siatki

```
User → MainWindowViewModel
  → DatasetGenerator.GenerateEmptyGridExample()
    → IEmptyGridGenerator.GenerateEmptyGrid()
      → CrosswordGrid
    → IXamlGenerator.GenerateXaml()
      → XAML string
  → DatasetEntry
  → UI Update
```

### 2. Generowanie ze słowami

```
User → MainWindowViewModel
  → DatasetGenerator.GenerateWithWordsExample()
    → CrosswordWordPlacer.GenerateWithWords()
      → IWordDictionary.GetRandomWordContaining()
        → LazyWordDictionary (leniwe ładowanie)
      → ArrangeWordsInGrid()
        → CrosswordGrid z literami
    → IXamlGenerator.GenerateXaml()
      → XAML string (z highlighted cells)
  → DatasetEntry
  → UI Update
```

## 🧩 Kluczowe komponenty

### LazyWordDictionary

**Problem:** Duże słowniki (100k+ słów) zajmują dużo pamięci.

**Rozwiązanie:** Leniwe ładowanie z indeksem linii.

```csharp
// Indeks: litera → lista numerów linii
Dictionary<char, List<int>> _letterLineNumbers;

// Cache: numer linii → słowo
Dictionary<int, string> _wordCache;

// Wczytuje tylko potrzebne słowa
public Result<string, string> GetRandomWordContaining(char letter)
{
    // 1. Znajdź numer linii z indeksu
    // 2. Wczytaj linię z pliku (lub z cache)
    // 3. Zwróć słowo
}
```

**Korzyści:**
- Szybki start (tylko indeksowanie, nie cały plik)
- Niskie użycie pamięci (tylko cache)
- Obsługa `.gz` (GZip)

### CrosswordWordPlacer

**Algorytm układania słów:**

1. **Wybór hasła głównego** (jeśli podane)
2. **Dla każdej litery hasła:**
   - Znajdź słowo zawierające tę literę
   - Spróbuj ułożyć prostopadle do istniejących słów
3. **Walidacja:**
   - Wszystkie słowa są połączone (connected graph)
   - Brak konfliktów (litery się zgadzają)
   - Minimalna odległość między słowami (1 kratka)

**Retry logic:** Jeśli układanie się nie powiedzie, losuje nowe słowa i próbuje ponownie.

### XamlGenerator

**Cel:** Generowanie minimalnego, jednoznacznego XAML dla LLM.

**Zasady:**
- Tylko niezbędne atrybuty
- Brak zagnieżdżeń (gdzie możliwe)
- Wycentrowany tekst
- Polskie znaki bez escapowania (XML je obsługuje)

**Przykład:**
```xml
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <Grid.RowDefinitions>
    <RowDefinition/>
    <RowDefinition/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition/>
    <ColumnDefinition/>
  </Grid.ColumnDefinitions>
  <TextBlock Grid.Row="0" Grid.Column="0" Text="K" FontSize="20" 
             FontFamily="Segoe UI" HorizontalAlignment="Center" 
             VerticalAlignment="Center"/>
</Grid>
```

## 🔍 Logowanie

### CursorLogger

Logger specjalnie dla debugowania przez AI asystenta:

```csharp
public interface ICursorLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    
    // Format versions
    void DebugFormat(string format, params object[] args);
    // ...
}
```

**Lokalizacja logów:**
```
bin/Debug/net8.0-windows/logs/cursor_YYYY-MM-DD.log
```

**Format:**
```
[CURSOR] [2025-11-19 02:19:20.175] [INFO] LoadIndexFromText: Polskie litery w indeksie: Ó, Ą, Ż, ...
```

## 🧪 Testowanie

### Unit Testy (planowane)

- `EmptyGridGeneratorTests` - testy generowania siatek
- `XamlGeneratorTests` - testy generowania XAML
- `CrosswordWordPlacerTests` - testy układania słów
- `LazyWordDictionaryTests` - testy leniwego ładowania

### Integration Testy (planowane)

- `DatasetGeneratorTests` - testy end-to-end generowania datasetów
- `ResultPatternTests` - testy ROP

## 🚀 Rozszerzalność

### Dodanie nowej implementacji IWordDictionary

```csharp
public class DatabaseWordDictionary : IWordDictionary
{
    // Implementacja z bazy danych
}

// W DI:
services.AddSingleton<IWordDictionary, DatabaseWordDictionary>();
```

### Dodanie nowej warstwy prezentacji

Core jest niezależny - można dodać:
- **MAUI** - `CrosswordAIGenerator.MAUI`
- **Blazor** - `CrosswordAIGenerator.Blazor`
- **Console** - `CrosswordAIGenerator.Console`

Wszystkie używają tego samego Core!

## 📚 Zależności

### Core
- Brak zależności zewnętrznych (tylko .NET 8.0)

### WPF
- `CommunityToolkit.Mvvm` - MVVM helpers
- `Microsoft.Extensions.DependencyInjection` - DI container

## 🔗 Integracja z ChatElioraSystem

**Planowana integracja:**
- Eksport datasetów do Qdrant (wektory)
- Format zgodny z MCP (Message Control Protocol)
- Embeddingi z `rag_metadata.embedding_text`

Zobacz [PLAN_PRZYSZŁOŚĆ.md](PLAN_PRZYSZŁOŚĆ.md) dla szczegółów.

---

**Ostatnia aktualizacja:** 2025-11-19

