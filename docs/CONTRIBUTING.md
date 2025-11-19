# Współtworzenie - Crossword AI Generator

Dziękujemy za zainteresowanie współtworzeniem projektu! 🎉

## 📋 Spis treści

- [Kod postępowania](#kod-postępowania)
- [Jak zgłaszać błędy](#jak-zgłaszać-błędy)
- [Proponowanie funkcji](#proponowanie-funkcji)
- [Pull Requesty](#pull-requesty)
- [Standardy kodu](#standardy-kodu)
- [Struktura commitów](#struktura-commitów)

## 📜 Kod postępowania

Projekt jest otwarty dla wszystkich. Prosimy o:
- **Szacunek** - bądź uprzejmy i konstruktywny
- **Tolerancja** - akceptuj różne opinie i doświadczenia
- **Współpraca** - pomagaj innym i ucz się razem

## 🐛 Jak zgłaszać błędy

### Przed zgłoszeniem

1. **Sprawdź istniejące issues** - może problem już został zgłoszony
2. **Sprawdź logi** - plik `logs/cursor_YYYY-MM-DD.log` może zawierać informacje
3. **Przetestuj na najnowszej wersji** - upewnij się, że używasz aktualnego kodu

### Zgłaszanie błędu

Utwórz issue z następującymi informacjami:

**Tytuł:** Krótki, opisowy tytuł (np. "Polskie znaki nie są wyświetlane w XAML")

**Szczegóły:**
```markdown
## Opis błędu
Krótki opis tego, co się dzieje.

## Kroki do reprodukcji
1. Uruchom aplikację
2. Wprowadź hasło "ŁÓDŹ"
3. Kliknij "Generuj Pojedynczy"
4. Zobacz błąd w XAML

## Oczekiwane zachowanie
Polskie znaki powinny być poprawnie wyświetlane.

## Rzeczywiste zachowanie
W XAML pojawia się "LODZ" zamiast "ŁÓDŹ".

## Środowisko
- OS: Windows 11
- .NET: 8.0
- Wersja: commit abc123

## Logi
```
[CURSOR] [2025-11-19 02:19:20.175] [ERROR] ...
```

## Dodatkowe informacje
Screenshoty, stack trace, etc.
```

## 💡 Proponowanie funkcji

### Przed propozycją

1. **Sprawdź roadmap** - może funkcja jest już planowana
2. **Sprawdź istniejące issues** - może ktoś już to zaproponował
3. **Zastanów się nad użytecznością** - czy funkcja jest naprawdę potrzebna?

### Proponowanie funkcji

Utwórz issue z następującymi informacjami:

```markdown
## Opis funkcji
Szczegółowy opis proponowanej funkcji.

## Problem, który rozwiązuje
Dlaczego ta funkcja jest potrzebna?

## Proponowane rozwiązanie
Jak funkcja powinna działać?

## Alternatywy
Czy są inne sposoby rozwiązania problemu?

## Dodatkowe informacje
Screenshoty, mockupy, przykłady użycia, etc.
```

## 🔀 Pull Requesty

### Przed utworzeniem PR

1. **Zaktualizuj dokumentację** - jeśli dodajesz funkcję, zaktualizuj README
2. **Dodaj testy** - jeśli to możliwe (na razie manualne)
3. **Sprawdź standardy kodu** - zobacz sekcję poniżej
4. **Zbuduj projekt** - upewnij się, że wszystko się kompiluje

### Proces PR

1. **Fork repozytorium** (jeśli nie masz dostępu)
2. **Utwórz branch:**
   ```bash
   git checkout -b feature/nazwa-funkcji
   # lub
   git checkout -b fix/opis-bledu
   ```
3. **Wprowadź zmiany** - zgodnie ze standardami kodu
4. **Commit zmiany:**
   ```bash
   git commit -m "feat: dodaj funkcję X"
   ```
5. **Push do forka:**
   ```bash
   git push origin feature/nazwa-funkcji
   ```
6. **Utwórz Pull Request** na GitHubie

### Opis PR

```markdown
## Opis zmian
Krótki opis tego, co zostało zmienione.

## Typ zmiany
- [ ] Bug fix
- [ ] Nowa funkcja
- [ ] Refaktoryzacja
- [ ] Dokumentacja
- [ ] Inne (opisz)

## Jak przetestować
Kroki do przetestowania zmian:
1. ...
2. ...

## Checklist
- [ ] Kod się kompiluje
- [ ] Zaktualizowano dokumentację
- [ ] Dodano logi (jeśli potrzebne)
- [ ] Sprawdzono polskie znaki (jeśli dotyczy)
- [ ] Sprawdzono ROP (Result pattern) dla błędów
```

## 📐 Standardy kodu

### C# Style Guide

#### Nazewnictwo

- **Klasy:** `PascalCase` - `CrosswordGrid`, `DatasetGenerator`
- **Interfejsy:** `IPascalCase` - `IWordDictionary`, `IEmptyGridGenerator`
- **Metody:** `PascalCase` - `GenerateXaml()`, `GetRandomWord()`
- **Właściwości:** `PascalCase` - `Rows`, `Columns`, `IsGenerating`
- **Pola prywatne:** `_camelCase` - `_random`, `_logger`
- **Lokalne zmienne:** `camelCase` - `wordCount`, `gridSize`

#### Formatowanie

- **Wcięcia:** 4 spacje (nie tabs)
- **Nawiasy klamrowe:** Nowa linia dla klas/metod
- **Maksymalna długość linii:** 120 znaków (preferowane 100)

#### Przykład

```csharp
public class CrosswordWordPlacer
{
    private readonly IWordDictionary _dictionary;
    private readonly Random _random;
    private readonly ICursorLogger? _logger;

    public CrosswordWordPlacer(
        IWordDictionary dictionary, 
        int? seed = null, 
        ICursorLogger? logger = null)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _logger = logger;
    }

    public Result<(CrosswordGrid, List<CrosswordWord>), string> GenerateWithWords(
        int rows, 
        int columns, 
        int targetWordCount = 5)
    {
        // Implementacja
    }
}
```

### Architektura

#### Zasady

1. **Domain nie wie o Infrastructure**
   - Interfejsy w `Domain/Services/`
   - Implementacje w `Infrastructure/Services/`

2. **Używaj Result Pattern (ROP)**
   - Zamiast wyjątków: `Result<TValue, TError>`
   - Spójna obsługa błędów

3. **Dependency Injection**
   - Wszystkie zależności przez konstruktor
   - Rejestracja w `DependencyInjection.cs`

4. **Logowanie**
   - Używaj `ICursorLogger` dla debugowania
   - Loguj błędy ROP z kontekstem

#### Przykład ROP

```csharp
// ❌ Złe (wyjątki)
public string GetWord()
{
    if (words.Count == 0)
        throw new InvalidOperationException("Brak słów");
    return words[0];
}

// ✅ Dobre (ROP)
public Result<string, string> GetWord()
{
    if (words.Count == 0)
        return Result<string, string>.Failure("Brak słów");
    return Result<string, string>.Success(words[0]);
}

// Użycie
var result = dictionary.GetWord();
if (result.IsFailure)
{
    _logger?.Error($"Nie udało się pobrać słowa: {result.Error}");
    return;
}
var word = result.Value;
```

### Polskie znaki

**Ważne:** Zawsze używaj `CultureInfo.GetCultureInfo("pl-PL")` dla operacji na stringach:

```csharp
// ✅ Dobre
var upper = word.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));

// ❌ Złe (może stracić polskie znaki)
var upper = word.ToUpper();
```

### XAML

- **Minimalny XAML** - tylko niezbędne atrybuty
- **Wycentrowany tekst** - `HorizontalAlignment="Center" VerticalAlignment="Center"`
- **Polskie znaki** - bez escapowania (XML je obsługuje)

## 📝 Struktura commitów

Używamy konwencji [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Typy

- `feat:` - Nowa funkcja
- `fix:` - Naprawa błędu
- `docs:` - Zmiany w dokumentacji
- `style:` - Formatowanie (nie zmienia logiki)
- `refactor:` - Refaktoryzacja
- `perf:` - Optymalizacja wydajności
- `test:` - Dodanie testów
- `chore:` - Zmiany w build/config

### Przykłady

```bash
feat(core): dodaj wsparcie dla polskich znaków w XAML
fix(word-placer): napraw błąd układania słów z przecięciami
docs(readme): zaktualizuj instrukcję instalacji
refactor(dataset): przenieś DatasetGenerator do Application layer
perf(lazy-dict): optymalizuj leniwe ładowanie słownika
```

### Scope (opcjonalny)

- `core` - zmiany w Core
- `wpf` - zmiany w WPF
- `word-placer` - zmiany w CrosswordWordPlacer
- `xaml-gen` - zmiany w XamlGenerator
- `dataset` - zmiany w DatasetGenerator

## 🧪 Testowanie

### Przed PR

1. **Zbuduj projekt:**
   ```bash
   dotnet build
   ```

2. **Uruchom aplikację:**
   ```bash
   cd CrosswordAIGenerator.WPF
   dotnet run
   ```

3. **Przetestuj zmiany:**
   - Generowanie pustych siatek
   - Generowanie ze słowami
   - Polskie znaki (jeśli dotyczy)
   - Sprawdź logi

4. **Sprawdź błędy kompilacji:**
   ```bash
   dotnet build --no-restore
   ```

## 📚 Dodatkowe zasoby

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Conventional Commits](https://www.conventionalcommits.org/)

## ❓ Pytania?

Jeśli masz pytania, utwórz issue z tagiem `question` lub skontaktuj się z maintainerami.

---

**Dziękujemy za współtworzenie!** 🚀

