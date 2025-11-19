# Przewodnik użytkownika - Crossword AI Generator

## 📖 Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Instalacja](#instalacja)
3. [Pierwsze kroki](#pierwsze-kroki)
4. [Generowanie krzyżówek](#generowanie-krzyżówek)
5. [Generowanie datasetów](#generowanie-datasetów)
6. [Eksport i zapisywanie](#eksport-i-zapisywanie)
7. [Rozwiązywanie problemów](#rozwiązywanie-problemów)
8. [Często zadawane pytania](#często-zadawane-pytania)

## 🎯 Wprowadzenie

**Crossword AI Generator** to aplikacja do deterministycznego generowania krzyżówek z automatycznym układaniem słów. System generuje krzyżówki w formacie XAML, gotowe do użycia w treningu modeli AI (LoRA finetuning) oraz w systemach RAG (Retrieval Augmented Generation).

### Główne funkcje

- ✅ Generowanie pustych siatek krzyżówek
- ✅ Automatyczne układanie słów z przecięciami
- ✅ Wyróżnianie hasła głównego (czerwone tło, numerowane litery)
- ✅ Masowe generowanie datasetów
- ✅ Eksport do JSON
- ✅ Zapisywanie screenshotów jako JPG
- ✅ Pełna obsługa polskich znaków

## 📦 Instalacja

### Wymagania

- **Windows 10/11** (WPF wymaga Windows)
- **.NET 8.0 SDK** lub nowszy
- **Visual Studio 2022** (opcjonalnie, do edycji kodu)

### Krok 1: Pobierz projekt

```bash
git clone <repository-url>
cd CrosswordAIGenerator
```

### Krok 2: Przygotuj słownik

Aplikacja wymaga słownika polskich słów do generowania krzyżówek ze słowami.

**Opcja A: Użyj istniejącego słownika**

Jeśli masz plik `slowa.txt` z polskimi słowami:
1. Utwórz katalog `dictionaries/` w głównym katalogu projektu
2. Skopiuj plik `slowa.txt` do `dictionaries/slowa.txt`

**Opcja B: Pobierz słownik automatycznie**

```powershell
cd dictionaries
.\download_dictionary.ps1
```

**Uwaga:** Słownik `slowa.txt` powinien zawierać:
- Jedno słowo na linię
- Kodowanie UTF-8
- Minimum 6 liter na słowo
- Polskie znaki diakrytyczne (Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż)

### Krok 3: Zbuduj projekt

```bash
dotnet restore
dotnet build
```

### Krok 4: Uruchom aplikację

```bash
cd CrosswordAIGenerator.WPF
dotnet run
```

Lub otwórz projekt w Visual Studio i naciśnij F5.

## 🚀 Pierwsze kroki

### Interfejs użytkownika

Po uruchomieniu aplikacji zobaczysz:

```
┌─────────────────────────────────────────────────┐
│  Crossword AI Dataset Generator                 │
├─────────────────────────────────────────────────┤
│  [Parametry]                                    │
│  Rozmiar siatki: [15] x [15]                   │
│  ☐ Ze słowami  ☑ Pusta siatka                  │
│  Hasło: [________]                              │
│                                                 │
│  [Generuj Pojedynczy] [Generuj Dataset]        │
│                                                 │
│  [Podgląd krzyżówki]                           │
│  ┌─────────────────────┐                       │
│  │                     │                       │
│  │   [Krzyżówka]       │                       │
│  │                     │                       │
│  └─────────────────────┘                       │
│                                                 │
│  Status: Gotowy                                 │
│  Wygenerowano: 0                                │
└─────────────────────────────────────────────────┘
```

### Pierwsza krzyżówka

1. **Pusta siatka:**
   - Ustaw rozmiar (np. 10x10)
   - Kliknij "Generuj Pojedynczy"
   - Zobacz pustą siatkę w podglądzie

2. **Krzyżówka ze słowami:**
   - Zaznacz "Ze słowami"
   - Opcjonalnie: wprowadź hasło główne (np. "KOT")
   - Kliknij "Generuj Pojedynczy"
   - Zobacz krzyżówkę z automatycznie ułożonymi słowami

## 🎨 Generowanie krzyżówek

### Pusta siatka

**Parametry:**
- **Rozmiar siatki:** Wysokość x Szerokość (np. 15x15)
- **Ściany:** Opcjonalne czarne kratki (ściany)
- **Prawdopodobieństwo ścian:** 0.0 - 1.0 (np. 0.1 = 10% kratek to ściany)

**Przykład:**
```
Rozmiar: 10x10
Ściany: ☑
Prawdopodobieństwo: 0.15
→ Generuje siatkę 10x10 z ~15% ścian
```

### Krzyżówka ze słowami

**Parametry:**
- **Rozmiar siatki:** Minimum 12x12 (słowa mają min 6 liter)
- **Hasło główne:** Opcjonalne (np. "KOT", "ŁÓDŹ", "ŚWIĘTY")
  - Jeśli puste: system losuje hasło automatycznie
  - Jeśli podane: wszystkie słowa będą zawierać litery z hasła
- **Liczba słów:** Automatyczna (równa liczbie liter w haśle)

**Algorytm:**
1. System wybiera hasło główne (lub używa podanego)
2. Dla każdej litery hasła znajduje słowo zawierające tę literę
3. Układa słowa prostopadle (poziomo/pionowo) z przecięciami
4. Wyróżnia hasło główne (czerwone tło, numerowane litery)

**Przykład:**
```
Hasło: "KOT" (3 litery)
→ System znajdzie 3 słowa:
  - Słowo z "K" (np. "KOT")
  - Słowo z "O" (np. "DOM")
  - Słowo z "T" (np. "STÓŁ")
→ Ułoży je z przecięciami
→ Wyróżni litery K-O-T (czerwone tło, numery 1-2-3)
```

### Wyróżnianie hasła głównego

Hasło główne jest wyróżnione:
- **Czerwone tło** (`LightCoral`)
- **Numerowane litery** (1, 2, 3, ...) w lewym górnym rogu
- **Ciemnoczerwony kolor** numerów (`DarkRed`)

**Przykład:**
```
┌─────┬─────┬─────┐
│  1  │     │     │  K (czerwone tło)
├─────┼─────┼─────┤
│     │  2  │     │  O (czerwone tło)
├─────┼─────┼─────┤
│     │     │  3  │  T (czerwone tło)
└─────┴─────┴─────┘
```

## 📊 Generowanie datasetów

### Podstawowe użycie

1. **Ustaw parametry:**
   - Rozmiar siatki
   - Tryb (pusta siatka / ze słowami)
   - Hasło główne (opcjonalnie)

2. **Ustaw liczbę przykładów:**
   - Wprowadź liczbę (np. 100)
   - Minimum: 1
   - Maksimum: 10000

3. **Kliknij "Generuj Dataset":**
   - Postęp jest wyświetlany w statusie
   - Licznik pokazuje: "Generowanie X/Y..."
   - Po zakończeniu: "Wygenerowano Y przykładów"

### Postęp generowania

Podczas generowania zobaczysz:
- **Status:** "Generowanie X/Y..."
- **Licznik:** Aktualizuje się w czasie rzeczywistym
- **Lista datasetów:** Wypełnia się automatycznie

**Uwaga:** Generowanie może trwać długo dla dużych datasetów (100+ przykładów).

### Optymalizacja

**Dla szybkiego generowania:**
- Użyj **podanego hasła głównego** (szybsze niż losowe)
- Użyj **większych siatek** (18x18+) - łatwiejsze układanie
- Generuj **mniejsze datasety** (10-50) na raz

**Dla losowych haseł:**
- System generuje hasła na bieżąco
- Każde hasło ma 1-2 próby układania
- Jeśli się nie uda, przechodzi do następnego hasła

## 💾 Eksport i zapisywanie

### Eksport do JSON

1. Wygeneruj dataset
2. Kliknij **"Eksport JSON"**
3. Wybierz lokalizację pliku
4. Zapisuje wszystkie przykłady w formacie JSON

**Format JSON:**
```json
[
  {
    "Id": "dataset-001",
    "Type": "crossword_with_words",
    "GridSize": "15x15",
    "Xaml": "<Grid>...</Grid>",
    "CrossGrid": "# GRID\nR0: ....[1]P..H.......R..\n...",
    "Description": "Krzyżówka z hasłem głównym KOT...",
    "Metadata": { ... },
    "RagMetadata": { ... }
  },
  ...
]
```

**Uwaga:** Pola w eksportowanym JSON są filtrowane zgodnie z ustawieniami (zakładka "Ustawienia"). Jeśli odznaczysz "Zawieraj XAML", pole `Xaml` będzie puste.

### Eksport do JSONL (Finetune)

Format gotowy do finetunowania modeli językowych (Bielik 4B, etc.).

1. Wygeneruj dataset z krzyżówkami (zaznacz "Ze słowami")
2. Upewnij się, że **"Zawieraj CrossGrid"** jest zaznaczone w ustawieniach
3. Kliknij **"Eksport JSONL (Finetune)"**
4. Wybierz lokalizację pliku (domyślnie `.jsonl`)
5. Plik będzie gotowy do użycia z:
   - **TRL SFTTrainer** (`input_column="prompt"`, `output_column="response"`)
   - **Axolotl** / **LLaMA-Factory** (format prompt/response)
   - Inne narzędzia SFT

**Format JSONL:**
```jsonl
{"prompt":"Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 16x16\nHasło główne: KONDZE\nSłowa (kierunki w nawiasach):\n- NIEPOKRĘCONĄ (Across)\n- KOSZALIŃSKIEGO (Down)\n...\nZwróć tylko sekcję # GRID.\n","response":"# GRID\nR0: ..... ..... ..... .\nR1: ..... .E.C. ..... .\n..."}
{"prompt":"...","response":"..."}
```

**Wymagania formatu:**
- UTF-8 bez BOM (dla polskich znaków)
- Jeden JSON na linię (bez przecinków między liniami, bez nawiasów `[]`)
- Prompt kończy się `\n` (model uczy się, że po tym zaczyna się odpowiedź)
- Response zawsze zaczyna się od `# GRID\n`
- Spójny format CrossGrid (R0, R1, ... z [1], [2] dla highlighted cells)

### Ustawienia eksportu

1. Przejdź do zakładki **"Ustawienia"**
2. Zaznacz/odznacz elementy, które mają być zawarte w eksportowanych datasetach:
   - **Zawieraj XAML** - pełna wersja z literami
   - **Zawieraj pustą wersję XAML** - bez liter, tylko ramki i definicje
   - **Zawieraj CrossGrid** - format ASCII art
   - **Zawieraj screenshot** - obraz JPG
   - **Zawieraj opis tekstowy** - Description
   - **Zawieraj SearchableText** - tekst do wyszukiwania
   - **Zawieraj EmbeddingText** - tekst do embeddingu dla RAG
3. Ustawienia są automatycznie zapisywane do `dataset_settings.json`

**Uwaga:** Ustawienia kontrolują tylko **eksport** - generowanie zawsze tworzy wszystkie elementy. To pozwala na różne eksporty z tego samego datasetu.

### Podgląd i walidacja CrossGrid

1. Otwórz menu **"Narzędzia"** → **"Podgląd CrossGrid"**
2. Wklej kod CrossGrid do pola tekstowego:
   - Może być z escape sequences: `# GRID\r\nR0: ..... O.... J....\r\n...`
   - Lub z rzeczywistymi znakami nowej linii (z edytora tekstu)
3. Kliknij **"Konwertuj do XAML"**
4. Zobacz:
   - **Wygenerowany XAML** - w lewym panelu
   - **Wizualny podgląd krzyżówki** - w prawym panelu
   - **Wyniki walidacji** - błędy/ostrzeżenia w dolnej części okna

**Funkcje:**
- Automatyczna walidacja przed konwersją
- Normalizacja tekstu (escape sequences → rzeczywiste znaki nowej linii)
- Konwersja CrossGrid → XAML z użyciem mappera
- Podgląd wizualny w CrosswordView

### Zapisywanie screenshotów

**Pojedynczy screenshot:**
1. Wygeneruj krzyżówkę
2. Kliknij "Zapisz Screenshot"
3. Zapisuje do: `images/{dataset-id}.jpg`

**Automatyczne screenshoty:**
- Screenshoty są generowane podczas tworzenia datasetu
- Zapisują się do: `images/{dataset-id}.jpg`
- Jakość: 90% (można zmienić w kodzie)

**Lokalizacja:**
```
CrosswordAIGenerator.WPF/bin/Debug/net8.0-windows/images/
```

### Wyświetlanie wygenerowanych datasetów

Po wygenerowaniu datasetu:
- Lista przykładów jest widoczna w interfejsie
- Kliknij na przykład, aby zobaczyć go w podglądzie
- XAML jest wyświetlany w polu tekstowym

## 🔧 Rozwiązywanie problemów

### Problem: "Nie znaleziono pliku slowa.txt"

**Rozwiązanie:**
1. Sprawdź czy plik istnieje w `dictionaries/slowa.txt`
2. Sprawdź czy plik ma kodowanie UTF-8
3. Sprawdź logi w `logs/cursor_YYYY-MM-DD.log`

**Alternatywa:**
- Użyj `words.polish.txt.gz` (fallback, ale bez polskich znaków)

### Problem: "Bardzo wolne generowanie"

**Przyczyny:**
- Duży słownik (3M+ słów) - pierwsze ładowanie indeksu może trwać
- Losowe hasła - każda próba wymaga wyszukiwania słów
- Małe siatki - trudniejsze układanie

**Rozwiązanie:**
1. Użyj **podanego hasła głównego** (szybsze)
2. Zwiększ **rozmiar siatki** (18x18+)
3. Generuj **mniejsze datasety** (10-50)
4. Poczekaj na **zakończenie indeksowania** (tylko pierwszy raz)

### Problem: "Nie udało się wygenerować krzyżówki"

**Przyczyny:**
- Hasło jest zbyt trudne do ułożenia
- Rozmiar siatki jest zbyt mały
- Słownik nie zawiera odpowiednich słów

**Rozwiązanie:**
1. Zwiększ **rozmiar siatki** (minimum 15x15 dla słów)
2. Użyj **innego hasła** lub zostaw puste (losowe)
3. Sprawdź **logi** w `logs/cursor_YYYY-MM-DD.log`

### Problem: "Polskie znaki nie działają"

**Przyczyny:**
- Słownik nie zawiera polskich znaków
- Błędne kodowanie pliku

**Rozwiązanie:**
1. Użyj słownika `slowa.txt` z polskimi znakami
2. Sprawdź kodowanie pliku (UTF-8)
3. System używa fallback (Ł→L, Ą→A) jeśli nie znajdzie polskich znaków

### Problem: "Błąd podczas ładowania XAML"

**Przyczyny:**
- Nieprawidłowy format XAML
- Błąd parsowania

**Rozwiązanie:**
1. Sprawdź logi w `logs/cursor_YYYY-MM-DD.log`
2. Spróbuj wygenerować nową krzyżówkę
3. Jeśli problem się powtarza, zgłoś issue

## ❓ Często zadawane pytania

### Q: Jakie słowa są używane?

A: System używa słownika `slowa.txt` z minimum 6 literami. Słowa są filtrowane:
- Minimum 6 liter
- Tylko litery (w tym polskie znaki)
- Wielkie litery

### Q: Czy mogę użyć własnego słownika?

A: Tak! Umieść plik `slowa.txt` w katalogu `dictionaries/` z formatem:
- Jedno słowo na linię
- UTF-8
- Minimum 6 liter

### Q: Jak działa wyróżnianie hasła głównego?

A: System znajduje wszystkie litery hasła głównego w krzyżówce i:
- Zmienia tło na czerwone (`LightCoral`)
- Dodaje numer w lewym górnym rogu (1, 2, 3, ...)
- Numery wskazują kolejność liter w haśle

### Q: Czy mogę eksportować do innych formatów?

A: Obecnie dostępne:
- **Eksport do JSON** - pełny dataset z wszystkimi polami
- **Eksport do JSONL (Finetune)** - format gotowy do finetunowania (prompt/response)

W przyszłości planowane:
- Eksport do Qdrant (wektory)
- Eksport do formatu LoRA (finetuning)

### Q: Jak długo trwa generowanie 100 przykładów?

A: Zależy od:
- **Z hasłem:** ~1-2 minuty
- **Losowe hasła:** ~5-10 minut
- **Rozmiar siatki:** Większe = szybsze

### Q: Czy mogę używać aplikacji bez słownika?

A: Tak, ale tylko dla **pustych siatek**. Krzyżówki ze słowami wymagają słownika.

### Q: Gdzie są zapisywane logi?

A: Logi są w:
```
CrosswordAIGenerator.WPF/bin/Debug/net8.0-windows/logs/cursor_YYYY-MM-DD.log
```

Format: `[CURSOR] [timestamp] [LEVEL] message`

### Q: Czy mogę zmienić rozmiar czcionki?

A: Tak, w kodzie `XamlGenerator.cs` zmień:
```csharp
<Setter Property="FontSize" Value="20"/>
```

### Q: Czy mogę zmienić kolor hasła głównego?

A: Tak, w kodzie `XamlGenerator.cs` zmień:
```csharp
Background="LightCoral"  // Czerwone tło
Foreground="DarkRed"      // Ciemnoczerwony numer
```

## 📚 Dodatkowe zasoby

- **[README.md](README.md)** - Przegląd projektu
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Szczegółowa architektura
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Jak współtworzyć
- **[PLAN_PRZYSZŁOŚĆ.md](PLAN_PRZYSZŁOŚĆ.md)** - Plan przyszłości

## 🐛 Zgłaszanie problemów

Jeśli napotkasz problem:
1. Sprawdź **logi** w `logs/cursor_YYYY-MM-DD.log`
2. Sprawdź **często zadawane pytania** powyżej
3. Utwórz **issue** w repozytorium z:
   - Opisem problemu
   - Krokami do reprodukcji
   - Fragmentem logów (jeśli dotyczy)

---

**Ostatnia aktualizacja:** 2025-11-19

