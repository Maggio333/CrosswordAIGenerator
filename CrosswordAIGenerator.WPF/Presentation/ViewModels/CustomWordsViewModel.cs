using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Application_.Services;
using CrosswordAIGenerator.Core.Domain.Common;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using CrosswordAIGenerator.WPF.Infrastructure;
using CrosswordAIGenerator.WPF.Presentation.ViewModels.Bases;
using CrosswordAIGenerator.WPF.Presentation.Views;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel dla zakładki z własnymi słowami i definicjami
/// </summary>
public partial class CustomWordsViewModel : BaseViewModel
{
    private readonly DatasetGenerator _datasetGenerator;
    private readonly IEmptyGridGenerator _gridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly IScreenshotService _screenshotService;
    private readonly ICursorLogger? _logger;
    private CrosswordView? _crosswordView;
    private MainWindowViewModel? _mainWindowViewModel;

    [ObservableProperty]
    private string _highlightedWord = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CustomWordEntry> _words = new();

    [ObservableProperty]
    private int _gridSizeRows = 15;

    partial void OnGridSizeRowsChanged(int value)
    {
        if (value < 5) GridSizeRows = 5;
        if (value > 30) GridSizeRows = 30;
    }

    [ObservableProperty]
    private int _gridSizeColumns = 15;

    partial void OnGridSizeColumnsChanged(int value)
    {
        if (value < 5) GridSizeColumns = 5;
        if (value > 30) GridSizeColumns = 30;
    }

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    [ObservableProperty]
    private string _xamlText = string.Empty;

    [ObservableProperty]
    private int _datasetCount = 10;

    partial void OnDatasetCountChanged(int value)
    {
        if (value < 1) DatasetCount = 1;
        if (value > 1000) DatasetCount = 1000;
    }

    [ObservableProperty]
    private int _minWordsCount = 0; // 0 oznacza użycie wszystkich słów (długość hasła)

    partial void OnMinWordsCountChanged(int value)
    {
        // Walidacja: min 0 (wszystkie), max = długość hasła
        if (value < 0) MinWordsCount = 0;
        if (HighlightedWord.Length > 0 && value > HighlightedWord.Length)
        {
            MinWordsCount = HighlightedWord.Length;
        }
    }

    partial void OnHighlightedWordChanged(string value)
    {
        // Gdy hasło się zmienia, upewnij się że MinWordsCount nie przekracza długości
        if (MinWordsCount > 0 && value.Length > 0 && MinWordsCount > value.Length)
        {
            MinWordsCount = value.Length;
        }
    }

    /// <summary>
    /// Maksymalna liczba słów (długość hasła)
    /// </summary>
    public int MaxWordsCount => HighlightedWord?.Length ?? 0;

    public CustomWordsViewModel(
        IEmptyGridGenerator gridGenerator,
        IXamlGenerator xamlGenerator,
        IScreenshotService screenshotService,
        DatasetGenerator datasetGenerator,
        ICursorLogger? logger = null)
    {
        _gridGenerator = gridGenerator ?? throw new ArgumentNullException(nameof(gridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _datasetGenerator = datasetGenerator ?? throw new ArgumentNullException(nameof(datasetGenerator));
        _logger = logger;

        _logger?.Info("CustomWordsViewModel: Konstruktor wywołany");

        // Dodaj przykładowe słowa
        LoadExampleData();
    }

    public void SetCrosswordView(CrosswordView view)
    {
        _crosswordView = view;
        _logger?.InfoFormat("CustomWordsViewModel.SetCrosswordView: Ustawiono CrosswordView: {0}", view != null ? "OK" : "NULL");
        
        if (view != null)
        {
            _logger?.InfoFormat("CustomWordsViewModel.SetCrosswordView: CrosswordView z pierwszej zakładki (MainWindow) - krzyżówki będą wyświetlane tam");
        }
    }

    public void SetMainWindowViewModel(MainWindowViewModel viewModel)
    {
        _mainWindowViewModel = viewModel;
        _logger?.InfoFormat("CustomWordsViewModel.SetMainWindowViewModel: Ustawiono MainWindowViewModel: {0}", viewModel != null ? "OK" : "NULL");
    }

    [RelayCommand]
    public void LoadExampleData()
    {
        _logger?.Info("CustomWordsViewModel.LoadExampleData: Rozpoczęcie");
        System.Diagnostics.Debug.WriteLine("[CURSOR] LoadExampleData: Wywołane");
        
        HighlightedWord = "DZIECKO";
        Words.Clear();
        
        var exampleWords = new[]
        {
            ("RODZINA", "Najbliższe otoczenie dziecka, które ma kluczowy wpływ na jego rozwój emocjonalny i poczucie bezpieczeństwa."),
            ("ROZWÓJ", "Proces, w którym dziecko zdobywa nowe umiejętności, poznaje świat i uczy się reagować na emocje oraz relacje."),
            ("PRZYWIĄZANIE", "Silna, emocjonalna więź między dzieckiem a opiekunem, która daje poczucie bezpieczeństwa i wpływa na rozwój psychiczny. Powstaje dzięki czułości, dostępności i przewidywalności dorosłego."),
            ("AGRESJA", "Zachowanie ukierunkowane i intencjonalne na zewnątrz i do wewnątrz, mające na celu spowodowanie szkody fizycznej bądź psychicznej."),
            ("EMOCJE", "Wewnętrzne odczucia, które pomagają dziecku komunikować potrzeby i reagować na otoczenie."),
            ("LĘK", "Naturalna emocja, która pojawia się, gdy dziecko czuje się zagrożone lub niepewne. U najmłodszych może objawiać się płaczem, wycofaniem lub trudnościami w zasypianiu."),
            ("PROPRIOCEPCJA", "Zmysł pozwalający odczuwać położenie i ruch własnego ciała w przestrzeni bez użycia wzroku.")
        };

        for (int i = 0; i < exampleWords.Length; i++)
        {
            Words.Add(new CustomWordEntry
            {
                Index = i + 1,
                Word = exampleWords[i].Item1,
                Definition = exampleWords[i].Item2
            });
        }
        
        _logger?.InfoFormat("CustomWordsViewModel.LoadExampleData: Załadowano {0} słów", Words.Count);
        System.Diagnostics.Debug.WriteLine($"[CURSOR] LoadExampleData: Załadowano {Words.Count} słów");
    }

    [RelayCommand]
    public void AddWord()
    {
        Words.Add(new CustomWordEntry
        {
            Index = Words.Count + 1,
            Word = string.Empty,
            Definition = string.Empty
        });
    }

    [RelayCommand]
    public void RemoveWord(CustomWordEntry? word)
    {
        if (word != null)
        {
            Words.Remove(word);
            // Zaktualizuj indeksy
            for (int i = 0; i < Words.Count; i++)
            {
                Words[i].Index = i + 1;
            }
        }
    }

    [RelayCommand]
    public void GenerateSingle()
    {
        _logger?.Info("CustomWordsViewModel.GenerateSingle: Rozpoczęcie generowania");
        
        if (string.IsNullOrWhiteSpace(HighlightedWord))
        {
            _logger?.Warning("CustomWordsViewModel.GenerateSingle: Brak hasła głównego");
            MessageBox.Show("Wprowadź hasło główne.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var validWords = Words.Where(w => !string.IsNullOrWhiteSpace(w.Word)).ToList();
        _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: Znaleziono {0} ważnych słów", validWords.Count);
        
        if (validWords.Count == 0)
        {
            _logger?.Warning("CustomWordsViewModel.GenerateSingle: Brak słów");
            MessageBox.Show("Dodaj przynajmniej jedno słowo.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Oblicz oczekiwaną liczbę słów (uwzględniając MinWordsCount)
        int expectedWordsCount = MinWordsCount > 0 && MinWordsCount < HighlightedWord.Length 
            ? MinWordsCount 
            : HighlightedWord.Length;
        
        if (validWords.Count < expectedWordsCount)
        {
            _logger?.WarningFormat("CustomWordsViewModel.GenerateSingle: Liczba słów ({0}) < oczekiwana ({1})", validWords.Count, expectedWordsCount);
            MessageBox.Show(
                $"Liczba słów ({validWords.Count}) jest mniejsza niż oczekiwana ({expectedWordsCount}).\n\nDodaj więcej słów lub zmniejsz 'Min. liczba słów'.",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        
        // Jeśli jest więcej słów niż potrzeba, to jest OK - system wybierze odpowiednie
        if (validWords.Count > expectedWordsCount)
        {
            _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: Liczba słów ({0}) > oczekiwana ({1}) - system wybierze odpowiednie słowa", validWords.Count, expectedWordsCount);
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Generowanie...";

            var wordList = validWords.Select(w => w.Word.ToUpper().Trim()).ToList();
            
            // Utwórz mapowanie słowo -> definicja
            var wordDefinitions = validWords.ToDictionary(
                w => w.Word.ToUpper().Trim(), 
                w => w.Definition,
                StringComparer.OrdinalIgnoreCase);
            
            // Oblicz rzeczywistą liczbę słów do użycia
            int actualWordsCount = MinWordsCount > 0 && MinWordsCount < HighlightedWord.Length 
                ? MinWordsCount 
                : HighlightedWord.Length;
            
            _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: Wywołuję GenerateWithCustomWords (rows={0}, cols={1}, hasło='{2}', słowa={3}, MinWordsCount={4}, actualWordsCount={5})", 
                GridSizeRows, GridSizeColumns, HighlightedWord, string.Join(", ", wordList), MinWordsCount, actualWordsCount);
            
            var result = _datasetGenerator.GenerateWithCustomWords(
                GridSizeRows,
                GridSizeColumns,
                HighlightedWord.ToUpper().Trim(),
                wordList,
                minWordsCount: actualWordsCount,
                wordDefinitions: wordDefinitions);

            if (result.IsFailure)
            {
                _logger?.Error($"CustomWordsViewModel.GenerateSingle: Błąd generowania: {result.Error}", null);
                StatusMessage = "Błąd podczas generowania";
                MessageBox.Show($"Nie udało się wygenerować krzyżówki.\n\n{result.Error}", 
                    "Błąd generowania", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var entry = result.Value;
            _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: Sukces! Wygenerowano krzyżówkę, XAML długość: {0}", entry.Xaml.Length);
            
            // Sprawdź ile słów faktycznie zostało użytych (z opisu)
            var descriptionLines = entry.Description.Split('\n');
            var wordsLine = descriptionLines.FirstOrDefault(l => l.Contains("Słowa w krzyżówce:"));
            if (wordsLine != null)
            {
                var wordCount = descriptionLines.Count(l => l.Trim().StartsWith("1.") || l.Trim().StartsWith("2.") || l.Trim().StartsWith("3.") || 
                    l.Trim().StartsWith("4.") || l.Trim().StartsWith("5.") || l.Trim().StartsWith("6.") || l.Trim().StartsWith("7.") || 
                    l.Trim().StartsWith("8.") || l.Trim().StartsWith("9.") || (l.Trim().Length > 2 && char.IsDigit(l.Trim()[0])));
                StatusMessage = $"Wygenerowano krzyżówkę z {wordCount} słowami (użyto {actualWordsCount} z {HighlightedWord.Length} liter hasła)";
                _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: Użyto {0} słów z {1} dostępnych", wordCount, validWords.Count);
            }
            
            XamlText = entry.Xaml;
            
            if (_crosswordView != null)
            {
                _logger?.Info("CustomWordsViewModel.GenerateSingle: Ładuję XAML do CrosswordView");
                _logger?.InfoFormat("CustomWordsViewModel.GenerateSingle: XAML preview (pierwsze 200 znaków): {0}", entry.Xaml.Substring(0, Math.Min(200, entry.Xaml.Length)));
                try
                {
                    // Wywołaj na Dispatcherze UI, żeby upewnić się że jest w odpowiednim wątku
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _crosswordView.LoadXaml(entry.Xaml);
                        _logger?.Info("CustomWordsViewModel.GenerateSingle: XAML załadowany do CrosswordView pomyślnie");
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
                catch (Exception ex)
                {
                    _logger?.Error("CustomWordsViewModel.GenerateSingle: Błąd podczas ładowania XAML do CrosswordView", ex);
                }
            }
            else
            {
                _logger?.Warning("CustomWordsViewModel.GenerateSingle: CrosswordView jest null!");
            }

            StatusMessage = "Gotowy";
            _logger?.Info("CustomWordsViewModel.GenerateSingle: Zakończono pomyślnie");
        }
        catch (Exception ex)
        {
            _logger?.Error("CustomWordsViewModel.GenerateSingle: Wyjątek podczas generowania", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas generowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public async Task GenerateDatasetAsync()
    {
        if (string.IsNullOrWhiteSpace(HighlightedWord))
        {
            MessageBox.Show("Wprowadź hasło główne.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var validWords = Words.Where(w => !string.IsNullOrWhiteSpace(w.Word)).ToList();
        if (validWords.Count == 0)
        {
            MessageBox.Show("Dodaj przynajmniej jedno słowo.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Generowanie datasetu...";
            GeneratedCount = 0;

                    var wordList = validWords.Select(w => w.Word.ToUpper().Trim()).ToList();
                    
                    // Utwórz mapowanie słowo -> definicja
                    var wordDefinitions = validWords.ToDictionary(
                        w => w.Word.ToUpper().Trim(), 
                        w => w.Definition,
                        StringComparer.OrdinalIgnoreCase);
                    
                    _logger?.InfoFormat("CustomWordsViewModel.GenerateDatasetAsync: Rozpoczynam generowanie datasetu. Count={0}, Rows={1}, Cols={2}, Hasło='{3}', Słowa={4}", 
                        DatasetCount, GridSizeRows, GridSizeColumns, HighlightedWord, string.Join(", ", wordList));
                    
                    await Task.Run(() =>
                    {
                        _logger?.Info("CustomWordsViewModel.GenerateDatasetAsync: Task.Run rozpoczęty");
                        try
                        {
                            // Oblicz rzeczywistą liczbę słów do użycia (musi być w tym samym scope co wywołanie)
                            int actualWordsCount = MinWordsCount > 0 && MinWordsCount < HighlightedWord.Length 
                                ? MinWordsCount 
                                : HighlightedWord.Length;
                            
                            _logger?.InfoFormat("CustomWordsViewModel.GenerateDatasetAsync: MinWordsCount={0}, HighlightedWord.Length={1}, actualWordsCount={2}", 
                                MinWordsCount, HighlightedWord.Length, actualWordsCount);
                            System.Diagnostics.Debug.WriteLine($"[CURSOR] CustomWordsViewModel.GenerateDatasetAsync: MinWordsCount={MinWordsCount}, HighlightedWord.Length={HighlightedWord.Length}, actualWordsCount={actualWordsCount}");
                            
                            var entries = _datasetGenerator.GenerateCustomWordsDataset(
                                DatasetCount,
                                GridSizeRows,
                                GridSizeColumns,
                                HighlightedWord.ToUpper().Trim(),
                                wordList,
                                minWordsCount: actualWordsCount,
                                wordDefinitions: wordDefinitions,
                        onProgress: (current, total) =>
                        {
                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    StatusMessage = $"Generowanie {current}/{total}...";
                                    GeneratedCount = current;
                                }, System.Windows.Threading.DispatcherPriority.Send);
                            }
                            catch (Exception ex)
                            {
                                _logger?.Error($"CustomWordsViewModel.GenerateDatasetAsync: Błąd aktualizacji postępu: {ex.Message}", ex);
                            }
                        });
                    
                    _logger?.InfoFormat("CustomWordsViewModel.GenerateDatasetAsync: GenerateCustomWordsDataset zakończone, entries.Count={0}", entries?.Count ?? 0);
                    
                    // Przekaż entries do Dispatcher
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProcessDatasetResults(entries);
                    }, System.Windows.Threading.DispatcherPriority.Send);
                }
                catch (Exception ex)
                {
                    _logger?.Error("CustomWordsViewModel.GenerateDatasetAsync: Wyjątek w Task.Run", ex);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Błąd: {ex.Message}";
                        MessageBox.Show($"Błąd podczas generowania datasetu: {ex.Message}", "Błąd", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.Error("CustomWordsViewModel.GenerateDatasetAsync: Wyjątek podczas generowania datasetu", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas generowania datasetu: {ex.Message}", "Błąd", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
            _logger?.Info("CustomWordsViewModel.GenerateDatasetAsync: Zakończono (finally)");
        }
    }

    private void ProcessDatasetResults(List<DatasetEntry> entries)
    {
        _logger?.InfoFormat("CustomWordsViewModel.ProcessDatasetResults: Przetwarzam {0} wyników", entries?.Count ?? 0);
        
        if (entries == null || entries.Count == 0)
        {
            StatusMessage = "Nie udało się wygenerować żadnych przykładów";
            _logger?.Warning("CustomWordsViewModel.ProcessDatasetResults: Nie udało się wygenerować żadnych przykładów");
            MessageBox.Show("Nie udało się wygenerować żadnych przykładów.", "Błąd", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Dodaj wszystkie wygenerowane przykłady do kolekcji
        foreach (var entry in entries)
        {
            DatasetEntries.Add(entry);
        }
        GeneratedCount = DatasetEntries.Count;
        
        // Dodaj również do MainWindowViewModel, żeby były widoczne w pierwszej zakładce
        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.AddDatasetEntries(entries);
            _logger?.InfoFormat("CustomWordsViewModel.ProcessDatasetResults: Dodano {0} wpisów do MainWindowViewModel", entries.Count);
        }
        else
        {
            _logger?.Warning("CustomWordsViewModel.ProcessDatasetResults: MainWindowViewModel jest null - wpisy nie zostały dodane do pierwszej zakładki");
        }
        
        // Renderuj ostatni przykład dla podglądu
        var lastEntry = entries[entries.Count - 1];
        XamlText = lastEntry.Xaml;
        
        if (_crosswordView != null)
        {
            _logger?.Info("CustomWordsViewModel.ProcessDatasetResults: Ładuję XAML ostatniej krzyżówki do CrosswordView");
            try
            {
                // Wywołaj na Dispatcherze UI z priorytetem Render, żeby upewnić się że jest w odpowiednim wątku
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _crosswordView.LoadXaml(lastEntry.Xaml);
                    
                    // Wymuś aktualizację layoutu
                    _crosswordView.UpdateLayout();
                    _logger?.Info("CustomWordsViewModel.ProcessDatasetResults: XAML załadowany do CrosswordView pomyślnie");
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                _logger?.Error("CustomWordsViewModel.ProcessDatasetResults: Błąd podczas ładowania XAML do CrosswordView", ex);
            }
        }
        else
        {
            _logger?.Warning("CustomWordsViewModel.ProcessDatasetResults: CrosswordView jest null!");
        }
        
        StatusMessage = $"Wygenerowano {entries.Count} przykładów (łącznie: {DatasetEntries.Count})";
        _logger?.InfoFormat("CustomWordsViewModel.ProcessDatasetResults: Zakończono pomyślnie, wygenerowano {0} przykładów, łącznie w kolekcji: {1}", entries.Count, DatasetEntries.Count);
    }

    [ObservableProperty]
    private int _generatedCount = 0;

    [ObservableProperty]
    private ObservableCollection<DatasetEntry> _datasetEntries = new();

    /// <summary>
    /// Eksportuje dataset do pliku JSON
    /// </summary>
    [RelayCommand]
    private void ExportToJson()
    {
        if (DatasetEntries.Count == 0)
        {
            MessageBox.Show("Brak danych do eksportu. Najpierw wygeneruj dataset.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"crossword_custom_words_dataset_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _datasetGenerator.SaveDatasetToFile(DatasetEntries.ToList(), saveDialog.FileName);
                StatusMessage = $"Zapisano do {saveDialog.FileName}";
                MessageBox.Show($"Zapisano {DatasetEntries.Count} przykładów do pliku.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("CustomWordsViewModel.ExportToJson: Błąd eksportu", ex);
            StatusMessage = $"Błąd eksportu: {ex.Message}";
            MessageBox.Show($"Błąd podczas eksportu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Czyści wygenerowane dane
    /// </summary>
    [RelayCommand]
    private void ClearData()
    {
        DatasetEntries.Clear();
        GeneratedCount = 0;
        XamlText = string.Empty;
        if (_crosswordView != null)
        {
            _crosswordView.Clear();
        }
        StatusMessage = "Dane wyczyszczone";
    }
}

