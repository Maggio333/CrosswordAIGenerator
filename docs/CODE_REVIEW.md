# Code Review - Analiza jakości kodu

## ✅ Co działa dobrze:

1. **Architektura warstwowa** - wyraźny podział na Domain, Application, Infrastructure, Presentation
2. **MVVM Pattern** - poprawnie użyty z CommunityToolkit.Mvvm
3. **Dependency Injection** - kontener DI skonfigurowany w Application_
4. **Async/Await** - poprawne użycie w operacjach asynchronicznych
5. **Naming Conventions** - spójne nazewnictwo zgodne z konwencjami C#

## ⚠️ Problemy do naprawienia:

### 1. **KRYTYCZNE: ViewModele tworzą zależności ręcznie zamiast używać DI**

**Problem:**
```csharp
// MainWindowViewModel.cs linia 135-137
_gridGenerator = new EmptyGridGenerator();
_xamlGenerator = new XamlGenerator();
_screenshotService = new ScreenshotService();
```

**Rekomendacja:**
- Wstrzykiwać zależności przez konstruktor
- Zarejestrować w DI i używać w ViewModelach

**Impact:** Narusza Dependency Inversion Principle, utrudnia testowanie

---

### 2. **Duplikacja kodu - MainWindowViewModel i DatasetGeneratorViewModel**

**Problem:**
- Prawie identyczny kod w obu ViewModelach (~650 linii każdy)
- Każda zmiana wymaga modyfikacji w dwóch miejscach

**Rekomendacja:**
- Utworzyć bazowy `DatasetGeneratorViewModelBase`
- Albo użyć jednego ViewModelu dla obu okien
- Albo usunąć DatasetGeneratorWindow jeśli nie jest potrzebny

**Impact:** Narusza DRY (Don't Repeat Yourself), zwiększa koszt utrzymania

---

### 3. **Brak interfejsów dla serwisów**

**Problem:**
- Wszystkie serwisy to konkretne klasy
- Brak abstrakcji utrudnia testowanie i wymianę implementacji

**Rekomendacja:**
```csharp
public interface IEmptyGridGenerator { ... }
public interface IXamlGenerator { ... }
public interface IScreenshotService { ... }
public interface IDatasetGenerator { ... }
```

**Impact:** Trudne testowanie jednostkowe, tight coupling

---

### 4. **ViewModele mają zbyt dużo odpowiedzialności (SRP violation)**

**Problem:**
- ViewModele zarządzają: UI state, generowaniem, screenshotowaniem, ładowaniem słownika, walidacją
- ~650 linii kodu w każdym ViewModelu

**Rekomendacja:**
- Wydzielić serwisy:
  - `IDictionaryLoadingService` - ładowanie słownika
  - `ICrosswordRenderingService` - renderowanie i screenshoty
  - `IValidationService` - walidacja danych wejściowych

**Impact:** Trudne testowanie, trudna zmiana, narusza Single Responsibility Principle

---

### 5. **Hardcoded wartości (Magic Numbers)**

**Problem:**
```csharp
await Task.Delay(200);  // Dlaczego 200ms?
await Task.Delay(300);  // Dlaczego 300ms?
if (value < 5) GridSizeRows = 5;  // Dlaczego 5?
if (value > 30) GridSizeRows = 30;  // Dlaczego 30?
```

**Rekomendacja:**
```csharp
public static class Constants
{
    public const int MinGridSize = 5;
    public const int MaxGridSize = 30;
    public const int RenderDelayMs = 200;
    public const int ExtendedRenderDelayMs = 300;
}
```

**Impact:** Trudne utrzymanie, brak jasności intencji

---

### 6. **Brak obsługi błędów w niektórych miejscach**

**Problem:**
```csharp
catch (Exception ex)
{
    // Ignoruj błędy screenshotu, kontynuuj
    System.Diagnostics.Debug.WriteLine($"Błąd screenshot dla {entry.Id}: {ex.Message}");
}
```

**Rekomendacja:**
- Dodać logging (ILogger)
- Rozważyć strategię obsługi błędów (retry, fallback, user notification)

**Impact:** Błędy mogą być cicho ignorowane

---

### 7. **Nieużywany plik Class1.cs**

**Problem:**
- `CrosswordAIGenerator.Core/Class1.cs` - pusty plik

**Rekomendacja:**
- Usunąć plik

**Impact:** Zanieczyszcza kod, mylące

---

### 8. **DatasetGenerator tworzony dynamicznie w ViewModel**

**Problem:**
- DatasetGenerator nie jest w DI, tworzony ręcznie w ViewModel
- Logika ładowania słownika w ViewModel (powinna być w serwisie)

**Rekomendacja:**
- Utworzyć `IDatasetGeneratorFactory`
- Albo zarejestrować DatasetGenerator jako Scoped/Transient z fabryką

**Impact:** Trudne testowanie, narusza DI pattern

---

### 9. **Brak walidacji w niektórych miejscach**

**Problem:**
- Walidacja tylko w partial methods (OnXxxChanged)
- Brak walidacji przy eksporcie, zapisie plików

**Rekomendacja:**
- Dodać walidację przed operacjami I/O
- Rozważyć FluentValidation lub Data Annotations

**Impact:** Możliwe błędy runtime

---

### 10. **Tight coupling z konkretnymi klasami**

**Problem:**
- ViewModele bezpośrednio używają `CrosswordView`, `MessageBox`, `SaveFileDialog`

**Rekomendacja:**
- Utworzyć abstrakcje:
  - `IDialogService` dla MessageBox/SaveFileDialog
  - `ICrosswordViewService` dla operacji na CrosswordView

**Impact:** Trudne testowanie, tight coupling

---

## 📊 Metryki:

- **Duplikacja kodu:** ~650 linii zduplikowanych (MainWindowViewModel vs DatasetGeneratorViewModel)
- **Cyclomatic Complexity:** Wysoka w metodach ViewModeli (GenerateDatasetAsync, SaveScreenshotsToImagesAsync)
- **Liczba odpowiedzialności w ViewModelach:** 5+ (UI, Generation, Screenshot, Dictionary Loading, Validation)
- **Pokrycie interfejsami:** 0% (wszystkie serwisy to konkretne klasy)

---

## 🎯 Priorytety naprawy:

### Wysoki priorytet:
1. ✅ Wstrzykiwanie zależności do ViewModeli (zamiast `new`)
2. ✅ Usunięcie duplikacji kodu (bazowy ViewModel)
3. ✅ Dodanie interfejsów dla serwisów

### Średni priorytet:
4. ✅ Wydzielenie serwisów z ViewModeli (SRP)
5. ✅ Stałe zamiast magic numbers
6. ✅ Poprawa obsługi błędów (logging)

### Niski priorytet:
7. ✅ Usunięcie Class1.cs
8. ✅ Abstrakcje dla dialogów
9. ✅ Walidacja wejść

---

## 💡 Rekomendowane następne kroki:

1. **Refaktoryzacja ViewModeli:**
   - Utworzyć bazowy ViewModel
   - Wstrzykiwać zależności przez konstruktor
   - Wydzielić serwisy

2. **Dodanie interfejsów:**
   - IEmptyGridGenerator
   - IXamlGenerator
   - IScreenshotService
   - IDatasetGenerator (lub IFactory)

3. **Dodanie stałych:**
   - Constants class z magic numbers

4. **Logging:**
   - Dodać ILogger do serwisów i ViewModeli

5. **Testy jednostkowe:**
   - Po dodaniu interfejsów będzie możliwe testowanie

