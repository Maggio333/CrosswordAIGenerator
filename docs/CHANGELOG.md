# Changelog - Historia zmian

Wszystkie znaczące zmiany w projekcie będą dokumentowane w tym pliku.

Format bazuje na [Keep a Changelog](https://keepachangelog.com/pl/1.0.0/),
a projekt używa [Semantic Versioning](https://semver.org/lang/pl/).

## [Unreleased]

### Planowane
- DSL Format (pośredni format dla LLM)
- DSL Parser (DSL → XAML)
- Integracja z LLM (generowanie XAML przez model)
- Eksport do Qdrant (integracja z ChatElioraSystem)
- LoRA Dataset Exporter

## [1.2.0] - 2025-11-19

### ✨ Dodano

#### Nowe funkcjonalności
- **CrossGrid Format** - prosty format ASCII art dla LLM (alternatywa dla XAML)
  - Format: `# GRID\nR0: ....[1]P..H.......R..\nR1: ....[2]O..I.P.....O..\n...`
  - Wizualne separatory co 5 kolumn dla lepszej czytelności
  - Wsparcie dla highlighted cells z numerowanymi indeksami `[1]`, `[2]`, etc.
- **Okno podglądu CrossGrid** - narzędzie do testowania i walidacji formatu CrossGrid
  - Menu "Narzędzia" → "Podgląd CrossGrid"
  - Wklejanie CrossGrid (obsługa escape sequences `\r\n` i rzeczywistych znaków nowej linii)
  - Konwersja CrossGrid → XAML z podglądem wizualnym
  - Automatyczna walidacja przed konwersją
- **Walidacja CrossGrid** - automatyczna walidacja formatu
  - Sprawdzanie poprawności parsowania
  - Weryfikacja spójności (highlighted cells mają litery)
  - Sprawdzanie ciągłości indeksów (1, 2, 3...)
  - Porównanie z oryginalnym gridem (opcjonalnie)
- **Eksport do JSONL (Finetune)** - format gotowy do finetunowania
  - Format JSONL (JSON Lines) - jeden JSON na linię
  - Pola: `prompt` (instrukcja) i `response` (CrossGrid)
  - UTF-8 bez BOM (dla polskich znaków)
  - Kompatybilny z: TRL SFTTrainer, Axolotl, LLaMA-Factory
  - Prompt kończy się `\n` (model uczy się, że po tym zaczyna się odpowiedź)
- **Ustawienia datasetów** - kontrola które elementy są zawarte w eksportowanych datasetach
  - Zakładka "Ustawienia" w MainWindow
  - Checkboxy dla: XAML, EmptyXaml, CrossGrid, Screenshot, Description, SearchableText, EmbeddingText
  - Automatyczne zapisywanie ustawień do JSON
  - Ustawienia kontrolują tylko eksport (generowanie zawsze tworzy wszystkie elementy)

#### UI/UX
- **Menu "Narzędzia"** - dostęp do narzędzi pomocniczych
- **Zakładka "Ustawienia"** - konfiguracja eksportu datasetów
- **Przycisk "Eksport JSONL (Finetune)"** - eksport w formacie gotowym do finetunowania

### 🔧 Zmieniono

#### Dataset Generator
- **Generowanie zawsze tworzy wszystkie elementy** - Settings kontrolują tylko eksport, nie generowanie
- **Eksport JSON** - filtruje pola zgodnie z ustawieniami przed serializacją
- **Eksport JSONL** - nowy format eksportu gotowy do finetunowania
- **CrossGrid w DatasetEntry** - dodano pole `CrossGrid` do `DatasetEntry`

#### CrossGrid Generator
- **Wizualne separatory** - spacje co 5 kolumn dla lepszej czytelności w JSON
- **Parser ignoruje separatory** - spacje są usuwane przed parsowaniem
- **Mapper XAML ↔ CrossGrid** - dwukierunkowa konwersja dla walidacji

#### XAML Generator
- **Style w Grid.Resources** - domyślne style dla TextBlock i Border (FontFamily, FontSize, BorderBrush, BorderThickness)
- **Uproszczone BorderThickness** - jednolity `BorderThickness="1"` wszędzie (bez różnicowania początku/końca słowa)

### 🐛 Naprawiono

- **Settings nie były stosowane** - naprawiono synchronizację Settings między ViewModels przez DI
- **Powtarzające się Grid.Resources** - konsolidacja do jednego bloku w root Grid
- **Powtarzające się BorderBrush/BorderThickness** - użycie domyślnych stylów

### 📝 Dokumentacja

- Zaktualizowano README.md o nowe funkcjonalności
- Dodano sekcję o CrossGrid Format
- Dodano sekcję o eksporcie do finetunowania
- Zaktualizowano CHANGELOG.md

## [1.1.0] - 2025-11-19

### ✨ Dodano

#### Nowe funkcjonalności
- **Zakładka "Własne słowa"** - generowanie krzyżówek z własnymi słowami i definicjami
- **Puste wersje krzyżówek** - automatyczne generowanie pustych wersji (bez liter, tylko ramki i definicje) do wypełnienia ręcznie
- **Obszar z definicjami** - wyświetlanie definicji słów po prawej stronie krzyżówki w osobnym obszarze
- **Ramki wokół słów** - czarne ramki wokół słów z numeracją (1, 2, 3...)
- **ScrollViewer** - przewijanie dla dużych krzyżówek i obszaru z definicjami
- **Eksport pełnych i pustych screenshotów** - jednoczesny eksport obu wersji do osobnych katalogów
- **Min. liczba słów** - opcja wyboru minimalnej liczby słów w krzyżówce (mniej niż liczba liter w haśle)

#### UI/UX
- **Dodatkowa zakładka** - "Własne słowa" dla generowania krzyżówek z własnymi słowami
- **Synchronizacja datasetów** - krzyżówki z zakładki "Własne słowa" są widoczne w zakładce "Automatyczne"
- **Białe tło w screenshotach** - poprawione renderowanie z białym tłem zamiast czarnego
- **Kwadratowe komórki** - stały rozmiar komórek (35x35px) dla lepszej czytelności

### 🔧 Zmieniono

#### XAML Generator
- **ScrollViewer wokół Grid** - możliwość przewijania dużych krzyżówek
- **Białe tło** - dodano `Background="White"` do ScrollViewer i Grid
- **Czarne ramki** - zmieniono `BorderBrush="Blue"` na `BorderBrush="Black"`
- **Obszar z definicjami** - definicje wyświetlane w osobnym obszarze po prawej stronie zamiast w komórkach
- **Pozycjonowanie** - krzyżówka pozycjonowana w lewym górnym rogu (`HorizontalAlignment="Left"`, `VerticalAlignment="Top"`)

#### Dataset Generator
- **Automatyczne generowanie pustych wersji** - każda krzyżówka ze słowami ma teraz również pustą wersję w `DatasetEntry.EmptyXaml`
- **Eksport do dwóch katalogów** - pełne wersje do `images/`, puste do `images_empty/`

#### Screenshot Service
- **Białe tło** - wypełnianie białym tłem przed renderowaniem
- **Obsługa ScrollViewer** - poprawne renderowanie ScrollViewer z zawartością

### 🐛 Naprawiono

- **Czarne tło w screenshotach** - dodano białe tło do XAML i ScreenshotService
- **Ucięte krzyżówki** - dodano ScrollViewer i stałe rozmiary komórek
- **Błąd STA przy parsowaniu XAML** - usunięto problematyczną metodę parsowania XAML na wątku tła
- **Pozycjonowanie krzyżówek** - krzyżówki są teraz pozycjonowane w lewym górnym rogu

## [1.0.0] - 2025-11-19

### ✨ Dodano

#### Funkcjonalności
- **Generowanie pustych siatek** - siatki krzyżówek z opcjonalnymi ścianami
- **Generowanie krzyżówek ze słowami** - automatyczne układanie słów z przecięciami
- **Wyróżnianie hasła głównego** - czerwone tło z numerowanymi literami (1, 2, 3, ...)
- **XAML Generator** - minimalny, zoptymalizowany XAML dla LLM
- **Screenshot Service** - zapisywanie krzyżówek jako obrazy JPG
- **Dataset Generator** - masowe generowanie przykładów
- **Eksport do JSON** - zapisywanie datasetów do pliku JSON

#### Architektura
- **Clean Architecture** - podział na Domain, Application, Infrastructure, Presentation
- **MVVM Pattern** - użycie CommunityToolkit.Mvvm
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Railway Oriented Programming** - Result<TValue, TError> pattern
- **SOLID Principles** - Single Responsibility, Dependency Inversion, etc.

#### Optymalizacje
- **LazyWordDictionary** - leniwe ładowanie słownika z indeksem linii
- **Cache dla słów** - przyspieszenie wyszukiwania
- **Batch loading** - ładowanie słów w partiach
- **Pre-loading** - pre-generowanie haseł do cache

#### Wsparcie dla polskich znaków
- **Pełna obsługa diakrytyków** - Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż
- **Fallback mechanism** - automatyczne zamiany (Ł→L, Ą→A) jeśli nie znajdzie polskich znaków
- **UTF-8 encoding** - poprawne kodowanie w XAML i plikach

#### UI/UX
- **Real-time progress** - licznik aktualizuje się w czasie rzeczywistym
- **Status messages** - informacje o postępie operacji
- **Dataset browsing** - przeglądanie wygenerowanych przykładów
- **Screenshot preview** - podgląd krzyżówek przed zapisem

#### Logowanie
- **CursorLogger** - logger specjalnie dla AI asystenta
- **Szczegółowe logi** - wszystkie operacje są logowane
- **Format logów** - `[CURSOR] [timestamp] [LEVEL] message`
- **Lokalizacja** - `logs/cursor_YYYY-MM-DD.log`

#### Dokumentacja
- **README.md** - przegląd projektu
- **USER_GUIDE.md** - szczegółowy przewodnik użytkownika
- **API_DOCUMENTATION.md** - dokumentacja API dla deweloperów
- **ARCHITECTURE.md** - szczegółowa architektura systemu
- **CONTRIBUTING.md** - jak współtworzyć projekt
- **CHANGELOG.md** - historia zmian (ten plik)

### 🔧 Zmieniono

#### XAML Generator
- **Style w Grid.Resources** - uniknięcie powtórzeń FontFamily i FontSize
- **Minimalny XAML** - tylko niezbędne atrybuty
- **Wycentrowany tekst** - domyślne HorizontalAlignment i VerticalAlignment w stylu
- **Optymalizacja** - krótszy kod XAML dla lepszego finetuningu LLM

#### Dataset Generator
- **Generowanie na bieżąco** - hasła generowane podczas tworzenia datasetu (szybsze)
- **Callback progress** - raportowanie postępu w czasie rzeczywistym
- **Optymalizacja słownika** - szybsze wyszukiwanie słów

#### Word Dictionary
- **Priorytet slowa.txt** - aplikacja używa tylko `slowa.txt` jako głównego słownika
- **Lazy loading** - indeksowanie zamiast ładowania całego pliku
- **Buffer optimization** - zwiększony rozmiar bufora (64KB) dla szybszego odczytu

### 🐛 Naprawiono

- **Błąd parsowania XAML** - usunięto FontFamily/FontSize z Grid (Panel nie ma tych właściwości)
- **Brak aktualizacji licznika** - dodano callback progress dla real-time updates
- **Wolne generowanie losowych haseł** - optymalizacja algorytmu wyszukiwania
- **Polskie znaki w słowniku** - poprawne filtrowanie i wyszukiwanie
- **Błędy kompilacji** - naprawiono konflikty nazw zmiennych i typów

### 🔒 Bezpieczeństwo

- **Walidacja wejść** - sprawdzanie rozmiarów siatek, liczby przykładów
- **Obsługa błędów** - Result pattern zamiast wyjątków
- **Logowanie błędów** - wszystkie błędy są logowane z kontekstem

### 📦 Zależności

- **.NET 8.0** - framework
- **CommunityToolkit.Mvvm 8.2.2** - MVVM helpers
- **Microsoft.Extensions.DependencyInjection 9.0.8** - DI container

---

## Typy zmian

- `✨ Dodano` - nowe funkcjonalności
- `🔧 Zmieniono` - zmiany w istniejących funkcjonalnościach
- `🐛 Naprawiono` - naprawione błędy
- `🔒 Bezpieczeństwo` - zmiany związane z bezpieczeństwem
- `📦 Zależności` - zmiany w zależnościach
- `🗑️ Usunięto` - usunięte funkcjonalności (jeśli dotyczy)
- `📝 Dokumentacja` - zmiany w dokumentacji

---

**Ostatnia aktualizacja:** 2025-11-19

