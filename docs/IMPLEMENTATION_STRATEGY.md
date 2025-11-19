# Strategia Implementacji - Generator Datasetów Krzyżówek

## 🎯 Cel główny
Stworzenie deterministycznego generatora datasetów dla treningu LoRA i testowania RAG w ChatElioraSystem:
- **XAML** (kod View) - deterministyczny, poprawny
- **Opis/interpretacja** (co jest w XAML) - dla embeddingów
- **Screenshot** (base64) - dla modeli multimodalnych

## 📋 Workflow: RAG → Finetune

1. **Generujemy dataset** (XAML + opis + screenshot)
2. **Eksportujemy do RAG** (Qdrant w ChatElioraSystem)
3. **Testujemy w chatbotcie** - czy model reaguje zgodnie z datasetem
4. **Jeśli działa** → robimy finetune (LoRA)
5. **Jeśli nie działa** → poprawiamy dataset i wracamy do punktu 2

## 🏗️ Architektura MVVM

### Wzorzec
- **Model**: `CrosswordGrid`, `DatasetEntry` (Core)
- **View**: `MainWindow.xaml`, `CrosswordView.xaml` (WPF)
- **ViewModel**: `MainWindowViewModel` (WPF/Presentation)

### Biblioteki
- **CommunityToolkit.Mvvm** - ObservableObject, AsyncRelayCommand, RelayCommand
- **Dependency Injection** - proste (na razie bez kontenera, później można dodać)

### ViewModel Properties
```csharp
- XamlText (string) - wygenerowany XAML
- GridSizeRows (int) - liczba wierszy
- GridSizeColumns (int) - liczba kolumn
- HasWalls (bool) - czy generować ściany
- WallProbability (double) - prawdopodobieństwo ściany
- IsGenerating (bool) - czy trwa generowanie
- StatusMessage (string) - komunikat statusu
- GeneratedCount (int) - liczba wygenerowanych przykładów
- DatasetEntries (ObservableCollection<DatasetEntry>) - lista wygenerowanych
```

### Commands
```csharp
- GenerateSingleCommand (ICommand) - generuje pojedynczy przykład
- GenerateDatasetCommand (ICommand) - generuje wiele przykładów
- ExportToJsonCommand (ICommand) - eksportuje do JSON
- ExportToQdrantCommand (ICommand) - eksportuje do Qdrant (opcjonalnie)
- LoadXamlCommand (ICommand) - ładuje XAML do CrosswordView
```

## 📦 Format Datasetu dla RAG

### DatasetEntry (rozszerzony)
```json
{
  "id": "empty_grid_10x10_walls_abc123",
  "type": "empty_grid",
  "grid_size": "10x10",
  "has_walls": true,
  "xaml": "<Grid>...</Grid>",
  "description": "Pusta siatka krzyżówki 10x10...",
  "searchable_text": "XAML Grid 10 wierszy 10 kolumn puste kratki Border Black ściany Background Black...",
  "screenshot_base64": "iVBORw0KGgo...",
  "metadata": {
    "rows": 10,
    "columns": 10,
    "wall_count": 15,
    "empty_cell_count": 85,
    "letter_count": 0
  },
  "rag_metadata": {
    "embedding_text": "Krzyżówka WPF XAML Grid 10x10 puste kratki...",
    "category": "crossword_empty_grid",
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

### SearchableText
Kombinacja:
- Fragmenty XAML (Grid, Border, TextBlock)
- Opis w języku naturalnym
- Metadane (rozmiar, typ, liczba ścian)
- Słowa kluczowe dla embeddingu

**Przykład:**
```
"XAML WPF Grid 10 wierszy 10 kolumn krzyżówka puste kratki Border Black BorderThickness 1 
ściany Background Black 15 ścian 85 pustych kratek BorderBrush Black TextBlock FontSize 20 
HorizontalAlignment Center VerticalAlignment Center"
```

## 🔌 Integracja z ChatElioraSystem (Qdrant)

### Format MCP (inspirowany ChatElioraSystem)
```json
{
  "Akcja": {
    "Typ": "Zapis",
    "Temat": "Krzyżówka - pusta siatka 10x10",
    "Payload": "XAML WPF Grid 10x10 puste kratki Border...",
    "Metadata": {
      "Źródło": "CrosswordAIGenerator",
      "Wnioski": "Przykład pustej siatki krzyżówki do nauki XAML",
      "Confidence": 1.0,
      "Timestamp": "2025-01-15T10:30:00Z",
      "GridSize": "10x10",
      "Type": "empty_grid"
    },
    "Extra": {
      "Xaml": "<Grid>...</Grid>",
      "ScreenshotBase64": "iVBORw0KGgo...",
      "DatasetId": "empty_grid_10x10_walls_abc123"
    }
  }
}
```

### Eksport do Qdrant
1. Dla każdego `DatasetEntry`:
   - Tworzymy embedding z `rag_metadata.embedding_text`
   - Tworzymy payload w formacie MCP
   - Wstawiamy do Qdrant przez `VectorDbHelper.InsertTopic()`

2. Kolekcja w Qdrant:
   - Nazwa: `CrosswordDataset` (lub konfigurowalna)
   - Wektor: 1024 wymiarów (zgodnie z ChatElioraSystem)
   - Metryka: Cosine

## 🎨 UI Layout (MainWindow)

```
┌─────────────────────────────────────────────────────────┐
│  Panel Kontrolny                                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                │
│  │ Rozmiar: │ │ Ściany:  │ │ Prawdop.: │                │
│  │ [10] x   │ │ [✓] Tak  │ │ [0.1]     │                │
│  │ [10]     │ │          │ │           │                │
│  └──────────┘ └──────────┘ └──────────┘                │
│                                                           │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                │
│  │ Generuj  │ │ Dataset   │ │ Eksport   │                │
│  │ Pojedynczy│ │ (100)     │ │ JSON/Qdrant│               │
│  └──────────┘ └──────────┘ └──────────┘                │
├─────────────────────────────────────────────────────────┤
│  CrosswordView (renderowana krzyżówka)                  │
│  ┌─────────────────────────────────────────────┐        │
│  │                                             │        │
│  │     [Renderowana krzyżówka z XAML]          │        │
│  │                                             │        │
│  └─────────────────────────────────────────────┘        │
├─────────────────────────────────────────────────────────┤
│  XAML Preview                                           │
│  ┌─────────────────────────────────────────────┐        │
│  │ <Grid>                                      │        │
│  │   <Grid.RowDefinitions>                    │        │
│  │     ...                                     │        │
│  │ </Grid>                                     │        │
│  └─────────────────────────────────────────────┘        │
├─────────────────────────────────────────────────────────┤
│  Status: [Generowanie...] | Wygenerowano: 0             │
└─────────────────────────────────────────────────────────┘
```

## 🔄 Workflow generowania

### Pojedynczy przykład
1. Użytkownik ustawia parametry (rozmiar, ściany)
2. Klik "Generuj Pojedynczy"
3. ViewModel:
   - `EmptyGridGenerator.GenerateEmptyGrid()` → `CrosswordGrid`
   - `XamlGenerator.GenerateXaml()` → XAML string
   - `CrosswordView.LoadXaml()` → renderowanie
   - Czeka na render (Dispatcher.BeginInvoke z Render priority)
   - `ScreenshotService.CaptureToBase64()` → screenshot
   - `DatasetGenerator.GenerateEmptyGridExample()` → `DatasetEntry`
   - Aktualizuje UI (XamlText, status)

### Dataset (wiele przykładów)
1. Użytkownik ustawia parametry + liczbę przykładów
2. Klik "Generuj Dataset"
3. ViewModel (async):
   - Pętla: generuje N przykładów
   - Dla każdego: generuje, renderuje, screenshotuje
   - Zapisuje do `DatasetEntries` (ObservableCollection)
   - Aktualizuje progress (GeneratedCount)
4. Po zakończeniu: możliwość eksportu

## 📝 Implementacja krok po kroku

### 1. MVVM Setup
- [x] Dodać CommunityToolkit.Mvvm do WPF.csproj
- [ ] Stworzyć BaseViewModel (dziedziczy z ObservableObject)
- [ ] Stworzyć MainWindowViewModel

### 2. ViewModel Properties & Commands
- [ ] Zaimplementować wszystkie properties z notyfikacją
- [ ] Zaimplementować wszystkie commands (AsyncRelayCommand/RelayCommand)
- [ ] Integracja z DatasetGenerator, XamlGenerator, etc.

### 3. UI (MainWindow.xaml)
- [ ] Panel kontrolny z TextBox/CheckBox dla parametrów
- [ ] Przyciski z Command binding
- [ ] CrosswordView z binding do XamlText
- [ ] TextBox dla XAML preview (IsReadOnly)
- [ ] Status bar

### 4. RAG Format
- [ ] Rozszerzyć DatasetEntry o `SearchableText`
- [ ] Rozszerzyć DatasetEntry o `RagMetadata`
- [ ] Zaktualizować DatasetGenerator.GenerateDescription() o SearchableText

### 5. Eksport Qdrant (opcjonalnie)
- [ ] Stworzyć QdrantExporter service
- [ ] Format MCP payload
- [ ] Integracja z ChatElioraSystem (jeśli potrzebna)

## 🧪 Testowanie

### Testy manualne
1. Generowanie pojedynczego przykładu
2. Sprawdzenie czy XAML się renderuje
3. Sprawdzenie czy screenshot się robi
4. Generowanie datasetu (10 przykładów)
5. Eksport do JSON
6. Import do Qdrant (jeśli zaimplementowane)

### Testy automatyczne (później)
- Unit testy dla EmptyGridGenerator
- Unit testy dla XamlGenerator
- Testy integracyjne dla DatasetGenerator

## 🚀 Następne kroki po MVP 1

1. **Etap 2**: Siatki z literami (bez haseł)
2. **Etap 3**: Siatki z uzupełnionymi hasłami
3. **Etap 4**: Integracja z LLM do generowania opisów
4. **Etap 5**: Testowanie RAG w ChatElioraSystem
5. **Etap 6**: Finetune (LoRA) jeśli RAG działa dobrze

