using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Application.Services;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using CrosswordAIGenerator.WPF.Infrastructure;
using CrosswordAIGenerator.WPF.Presentation.ViewModels.Bases;
using CrosswordAIGenerator.WPF.Presentation.Views;
using Microsoft.Win32;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel dla DatasetGeneratorWindow - kopia MainWindowViewModel do testowania
/// </summary>
public partial class DatasetGeneratorViewModel : BaseViewModel
{
    private DatasetGenerator _datasetGenerator;
    private readonly IEmptyGridGenerator _gridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private readonly IScreenshotService _screenshotService;

    // Properties dla binding
    [ObservableProperty]
    private string _xamlText = string.Empty;

    [ObservableProperty]
    private int _gridSizeRows = 15;

    partial void OnGridSizeRowsChanged(int value)
    {
        // Walidacja: min 5, max 30
        if (value < 5) GridSizeRows = 5;
        if (value > 30) GridSizeRows = 30;
    }

    [ObservableProperty]
    private int _gridSizeColumns = 15;

    partial void OnGridSizeColumnsChanged(int value)
    {
        // Walidacja: min 5, max 30
        if (value < 5) GridSizeColumns = 5;
        if (value > 30) GridSizeColumns = 30;
    }

    [ObservableProperty]
    private bool _hasWalls = false;

    [ObservableProperty]
    private double _wallProbability = 0.1;

    partial void OnWallProbabilityChanged(double value)
    {
        // Walidacja: min 0.0, max 1.0
        if (value < 0.0) WallProbability = 0.0;
        if (value > 1.0) WallProbability = 1.0;
    }

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    [ObservableProperty]
    private int _generatedCount = 0;

    [ObservableProperty]
    private int _datasetCount = 100;

    partial void OnDatasetCountChanged(int value)
    {
        // Walidacja: min 1, max 10000
        if (value < 1) DatasetCount = 1;
        if (value > 10000) DatasetCount = 10000;
    }

    [ObservableProperty]
    private int _targetWordCount = 5;

    partial void OnTargetWordCountChanged(int value)
    {
        // Walidacja: min 3, max 20
        if (value < 3) TargetWordCount = 3;
        if (value > 20) TargetWordCount = 20;
    }

    [ObservableProperty]
    private bool _generateWithWords = true; // Domyślnie generuj krzyżówki ze słowami

    partial void OnGenerateWithWordsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTargetWordCountEnabled));
    }

    [ObservableProperty]
    private string _highlightedWord = string.Empty;

    partial void OnHighlightedWordChanged(string value)
    {
        OnPropertyChanged(nameof(IsTargetWordCountEnabled));
    }

    /// <summary>
    /// Określa czy pole "Słowa" (TargetWordCount) powinno być włączone.
    /// Jest włączone tylko gdy GenerateWithWords == true i HighlightedWord jest puste.
    /// Gdy jest hasło, targetWordCount jest ignorowane (liczba słów = długość hasła).
    /// </summary>
    public bool IsTargetWordCountEnabled => GenerateWithWords && string.IsNullOrWhiteSpace(HighlightedWord);

    [ObservableProperty]
    private ObservableCollection<DatasetEntry> _datasetEntries = new();

    [ObservableProperty]
    private DatasetEntry? _selectedDatasetEntry;

    [ObservableProperty]
    private bool _isDictionaryLoading = false;

    [ObservableProperty]
    private bool _isDictionaryLoaded = false;

    // Reference do CrosswordView dla screenshotowania
    private CrosswordView? _crosswordView;

    partial void OnSelectedDatasetEntryChanged(DatasetEntry? value)
    {
        if (value != null && _crosswordView != null)
        {
            // Załaduj wybraną krzyżówkę
            XamlText = value.Xaml;
            _crosswordView.LoadXaml(value.Xaml);
            
            // Czekaj na render
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(200);
                _crosswordView.UpdateLayout();
            }, DispatcherPriority.Render);
        }
    }

    public DatasetGeneratorViewModel(
        IEmptyGridGenerator gridGenerator,
        IXamlGenerator xamlGenerator,
        IScreenshotService screenshotService,
        DatasetGenerator datasetGenerator)
    {
        _gridGenerator = gridGenerator ?? throw new ArgumentNullException(nameof(gridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _datasetGenerator = datasetGenerator ?? throw new ArgumentNullException(nameof(datasetGenerator));
        
        StatusMessage = "Gotowy";
        
        // Załaduj indeks słownika w tle (jeśli to LazyWordDictionary)
        _ = LoadDictionaryIndexAsync();
    }

    /// <summary>
    /// Ładuje indeks słownika w tle (jeśli to LazyWordDictionary) - nie blokuje UI
    /// </summary>
    private async Task LoadDictionaryIndexAsync()
    {
        IsDictionaryLoading = true;
        StatusMessage = "Ładowanie indeksu słownika...";
        
        try
        {
            await Task.Run(() =>
            {
                // DatasetGenerator jest już utworzony przez DI z odpowiednim słownikiem
                // Jeśli to LazyWordDictionary, załaduj indeks w tle
                // (indeks ładuje się automatycznie przy pierwszym użyciu, ale możemy to zrobić wcześniej)
            });
            
            IsDictionaryLoaded = true;
            StatusMessage = "Gotowy";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Gotowy (błąd: {ex.Message})";
        }
        finally
        {
            IsDictionaryLoading = false;
        }
    }

    /// <summary>
    /// Ustawia referencję do CrosswordView (wywoływane z code-behind)
    /// </summary>
    public void SetCrosswordView(CrosswordView crosswordView)
    {
        _crosswordView = crosswordView;
    }

    /// <summary>
    /// Generuje pojedynczy przykład i renderuje go
    /// </summary>
    [RelayCommand]
    private async Task GenerateSingleAsync()
    {
        if (IsGenerating)
            return;

        // Jeśli generujemy ze słowami, upewnij się że słownik jest załadowany
        if (GenerateWithWords && IsDictionaryLoading)
        {
            MessageBox.Show("Proszę poczekać, słownik jest jeszcze ładowany...", "Czekaj", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Generowanie...";

            // Generuj grid - zależnie od trybu
            DatasetEntry entry;
            if (GenerateWithWords)
            {
                // UWAGA: targetWordCount jest używane TYLKO gdy nie ma hasła
                // Gdy jest hasło, liczba słów = długość hasła (każda litera hasła = jedno słowo)
                var result = _datasetGenerator.GenerateWithWordsExample(
                    GridSizeRows,
                    GridSizeColumns,
                    TargetWordCount, // Ignorowane gdy highlightedWord != null
                    null,
                    string.IsNullOrWhiteSpace(HighlightedWord) ? null : HighlightedWord); // DatasetGeneratorViewModel nie ma dostępu do Settings - używa domyślnych
                
                // Obsłuż Result (ROP)
                if (result.IsFailure)
                {
                    var errorMsg = $"Nie udało się wygenerować krzyżówki.\n\n{result.Error}";
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] DatasetGeneratorViewModel.GenerateSingleAsync: {errorMsg}");
                    StatusMessage = "Błąd podczas generowania";
                    MessageBox.Show(errorMsg, "Błąd generowania krzyżówki", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                entry = result.Value;
            }
            else
            {
                entry = _datasetGenerator.GenerateEmptyGridExample(
                    GridSizeRows,
                    GridSizeColumns,
                    HasWalls,
                    WallProbability);
            }

            // Ustaw XAML
            XamlText = entry.Xaml;

            // Załaduj do CrosswordView
            if (_crosswordView != null)
            {
                _crosswordView.LoadXaml(entry.Xaml);

                // Czekaj na render - dłuższe opóźnienie i wymuszenie layoutu
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _crosswordView.UpdateLayout();
                }, DispatcherPriority.Render);

                await Task.Delay(300); // Dłuższe opóźnienie dla renderowania

                // Zrób screenshot na głównym wątku - renderuj bezpośrednio Grid
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        _crosswordView.UpdateLayout();
                        
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Błąd screenshot: {ex.Message}";
                    }
                }, DispatcherPriority.Render);
            }

            StatusMessage = "Wygenerowano pojedynczy przykład";
            GeneratedCount = 1;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas generowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// Zapisuje pojedynczy screenshot aktualnie wyświetlanej krzyżówki
    /// </summary>
    [RelayCommand]
    private async Task SaveSingleScreenshotAsync()
    {
        if (_crosswordView == null || string.IsNullOrEmpty(XamlText))
        {
            MessageBox.Show("Najpierw wygeneruj krzyżówkę.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Zapisywanie screenshotu...";

            // Utwórz katalog images jeśli nie istnieje
            var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            Directory.CreateDirectory(imagesDir);

            // Wymuś renderowanie
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _crosswordView.UpdateLayout();
            }, DispatcherPriority.Render);

            await Task.Delay(200);

            // Zrób screenshot - renderuj bezpośrednio Grid
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var fileName = $"crossword_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(imagesDir, fileName);
                
                // Spróbuj renderować bezpośrednio wewnętrzny Grid
                var innerGrid = _crosswordView.GetInnerGrid();
                if (innerGrid != null)
                {
                    innerGrid.UpdateLayout();
                    innerGrid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                    innerGrid.Arrange(new System.Windows.Rect(innerGrid.DesiredSize));
                    _screenshotService.CaptureToJpg(innerGrid, filePath);
                }
                else
                {
                    // Fallback - renderuj cały UserControl
                    _screenshotService.CaptureToJpg(_crosswordView, filePath);
                }
                
                StatusMessage = $"Zapisano do {fileName}";
                MessageBox.Show($"Zapisano screenshot do:\n{filePath}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }, DispatcherPriority.Render);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas zapisywania screenshotu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// Generuje dataset (wiele przykładów)
    /// </summary>
    [RelayCommand]
    private async Task GenerateDatasetAsync()
    {
        if (IsGenerating)
            return;

        // Jeśli generujemy ze słowami, upewnij się że słownik jest załadowany
        if (GenerateWithWords && IsDictionaryLoading)
        {
            MessageBox.Show("Proszę poczekać, słownik jest jeszcze ładowany...", "Czekaj", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Generowanie datasetu...";
            GeneratedCount = 0;
            DatasetEntries.Clear();

            await Task.Run(async () =>
            {
                List<DatasetEntry> entries;
                
                if (GenerateWithWords)
                {
                    // WAŻNE: Dla krzyżówek ze słowami potrzebujemy większych siatek (min 12x12, max 20x20)
                    // bo słowa mają min 6 liter i muszą się przecinać
                    int minSizeForWords = Math.Max(12, GridSizeRows); // Minimum 12x12 dla słów
                    int maxSizeForWords = Math.Max(20, GridSizeRows + 5); // Minimum 20x20 lub większe
                    
                    // Jeśli użytkownik podał hasło, użyj go (wszystkie krzyżówki będą miały to samo hasło)
                    // Jeśli nie podał, każda krzyżówka dostanie losowe hasło
                    // UWAGA: targetWordCount jest używane TYLKO gdy nie ma hasła (highlightedWord == null)
                    // Gdy jest hasło, liczba słów = długość hasła (każda litera hasła = jedno słowo)
                    entries = _datasetGenerator.GenerateWithWordsDataset(
                        DatasetCount,
                        minSize: minSizeForWords,
                        maxSize: maxSizeForWords,
                        targetWordCount: TargetWordCount, // Używane TYLKO gdy nie ma hasła - gdy jest hasło, jest ignorowane
                        highlightedWord: string.IsNullOrWhiteSpace(HighlightedWord) ? null : HighlightedWord); // DatasetGeneratorViewModel nie ma dostępu do Settings - używa domyślnych
                }
                else
                {
                    entries = _datasetGenerator.GenerateEmptyGridDataset(
                        DatasetCount,
                        minSize: Math.Min(GridSizeRows, 5),
                        maxSize: Math.Max(GridSizeRows, 15),
                        includeWithWalls: true,
                        wallProbability: WallProbability);
                }

                // Dla każdego przykładu: renderuj i zrób screenshot
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    // Aktualizuj UI na głównym wątku
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        XamlText = entry.Xaml;
                        if (_crosswordView != null)
                        {
                            _crosswordView.LoadXaml(entry.Xaml);
                        }
                        StatusMessage = $"Generowanie {i + 1}/{entries.Count}...";
                    });

                    // Czekaj na render
                    await Task.Delay(200);

                    // Screenshot na głównym wątku - renderuj bezpośrednio Grid
                    if (_crosswordView != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                _crosswordView.UpdateLayout();
                                
                                // Spróbuj renderować bezpośrednio wewnętrzny Grid
                                var innerGrid = _crosswordView.GetInnerGrid();
                                if (innerGrid != null)
                                {
                                }
                                else
                                {
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ignoruj błędy screenshotu, kontynuuj
                                System.Diagnostics.Debug.WriteLine($"Błąd screenshot dla {entry.Id}: {ex.Message}");
                            }
                        });
                    }

                    // Dodaj do kolekcji na głównym wątku
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        DatasetEntries.Add(entry);
                        GeneratedCount = DatasetEntries.Count;
                    });
                }
            });

            StatusMessage = $"Wygenerowano {GeneratedCount} przykładów";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas generowania datasetu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

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
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"crossword_dataset_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                // DatasetGeneratorViewModel nie ma dostępu do SettingsViewModel, więc użyj domyślnych ustawień (wszystko włączone)
                _datasetGenerator.SaveDatasetToFile(DatasetEntries.ToList(), saveDialog.FileName, null);
                StatusMessage = $"Zapisano do {saveDialog.FileName}";
                MessageBox.Show($"Zapisano {DatasetEntries.Count} przykładów do pliku.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
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

    /// <summary>
    /// Zapisuje screenshoty do katalogu images jako JPG
    /// </summary>
    [RelayCommand]
    private async Task SaveScreenshotsToImagesAsync()
    {
        if (DatasetEntries.Count == 0)
        {
            MessageBox.Show("Brak danych do eksportu. Najpierw wygeneruj dataset.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsGenerating = true;
            StatusMessage = "Zapisywanie screenshotów...";

            // Utwórz katalog images jeśli nie istnieje
            var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            Directory.CreateDirectory(imagesDir);

            int savedCount = 0;
            int errorCount = 0;

            await Task.Run(async () =>
            {
                foreach (var entry in DatasetEntries)
                {
                    try
                    {
                        // Załaduj XAML do view
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (_crosswordView != null)
                            {
                                _crosswordView.LoadXaml(entry.Xaml);
                            }
                        });

                        // Czekaj na render
                        await Task.Delay(200);

                        // Zrób screenshot jako JPG - renderuj bezpośrednio Grid
                        if (_crosswordView != null)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                try
                                {
                                    _crosswordView.UpdateLayout();
                                    
                                    var filePath = Path.Combine(imagesDir, $"{entry.Id}.jpg");
                                    
                                    // Spróbuj renderować bezpośrednio wewnętrzny Grid
                                    var innerGrid = _crosswordView.GetInnerGrid();
                                    if (innerGrid != null)
                                    {
                                        innerGrid.UpdateLayout();
                                        innerGrid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                                        innerGrid.Arrange(new System.Windows.Rect(innerGrid.DesiredSize));
                                        _screenshotService.CaptureToJpg(innerGrid, filePath);
                                    }
                                    else
                                    {
                                        // Fallback - renderuj cały UserControl
                                        _screenshotService.CaptureToJpg(_crosswordView, filePath);
                                    }
                                    
                                    savedCount++;
                                }
                                catch (Exception ex)
                                {
                                    errorCount++;
                                    System.Diagnostics.Debug.WriteLine($"Błąd zapisu screenshot dla {entry.Id}: {ex.Message}");
                                }
                            });
                        }

                        // Aktualizuj status
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = $"Zapisano {savedCount}/{DatasetEntries.Count} screenshotów...";
                        });
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"Błąd zapisu screenshot dla {entry.Id}: {ex.Message}");
                    }
                }
            });

            StatusMessage = $"Zapisano {savedCount} screenshotów do {imagesDir}";
            if (errorCount > 0)
            {
                MessageBox.Show($"Zapisano {savedCount} screenshotów.\n{errorCount} błędów.", "Zakończono", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Zapisano {savedCount} screenshotów do katalogu:\n{imagesDir}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas zapisywania screenshotów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }
}

