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

