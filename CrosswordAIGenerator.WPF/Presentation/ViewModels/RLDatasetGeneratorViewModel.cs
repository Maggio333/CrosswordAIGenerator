using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Application.Services;
using CrosswordAIGenerator.Core.Domain.Models.RL;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.WPF.Presentation.Views;
using Microsoft.Win32;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel dla okna generatora datasetów RL
/// </summary>
public partial class RLDatasetGeneratorViewModel : ObservableObject
{
    private readonly DatasetGenerator _datasetGenerator;
    private readonly ICursorLogger? _logger;

    // Properties dla binding
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
    private int _wordCount = 5;

    partial void OnWordCountChanged(int value)
    {
        if (value < 3) WordCount = 3;
        if (value > 20) WordCount = 20;
    }

    [ObservableProperty]
    private int _entryCount = 100;

    partial void OnEntryCountChanged(int value)
    {
        if (value < 1) EntryCount = 1;
        if (value > 10000) EntryCount = 10000;
    }

    [ObservableProperty]
    private SelfPlayStrategy _selectedStrategy = SelfPlayStrategy.Random;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    [ObservableProperty]
    private int _generatedCount = 0;

    [ObservableProperty]
    private ObservableCollection<CrosswordRLDatasetEntry> _rlEntries = new();

    [ObservableProperty]
    private CrosswordRLDatasetEntry? _selectedRLEntry;

    [ObservableProperty]
    private string _selectedEntryDetails = string.Empty;

    partial void OnSelectedRLEntryChanged(CrosswordRLDatasetEntry? value)
    {
        if (value != null)
        {
            SelectedEntryDetails = FormatEntryDetails(value);
        }
        else
        {
            SelectedEntryDetails = string.Empty;
        }
    }

    public RLDatasetGeneratorViewModel(
        DatasetGenerator datasetGenerator,
        ICursorLogger? logger = null)
    {
        _datasetGenerator = datasetGenerator ?? throw new ArgumentNullException(nameof(datasetGenerator));
        _logger = logger;
    }

    [RelayCommand]
    private async Task GenerateRLDatasetAsync()
    {
        if (IsGenerating)
            return;

        IsGenerating = true;
        StatusMessage = "Generowanie datasetu RL...";
        GeneratedCount = 0;
        RlEntries.Clear();

        try
        {
            await Task.Run(async () =>
            {
                var entries = _datasetGenerator.GenerateRLDataset(
                    EntryCount,
                    GridSizeRows,
                    GridSizeColumns,
                    WordCount,
                    SelectedStrategy);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var entry in entries)
                    {
                        RlEntries.Add(entry);
                    }
                    GeneratedCount = RlEntries.Count;
                    StatusMessage = $"Wygenerowano {GeneratedCount} wpisów";
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show($"Błąd podczas generowania datasetu RL:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void ExportToJsonl()
    {
        if (RlEntries.Count == 0)
        {
            MessageBox.Show("Brak danych do eksportu. Najpierw wygeneruj dataset.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "JSONL files (*.jsonl)|*.jsonl|All files (*.*)|*.*",
            FileName = "rl_dataset.jsonl",
            DefaultExt = "jsonl"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                _datasetGenerator.ExportRLDatasetToJsonl(RlEntries.ToList(), saveDialog.FileName);
                StatusMessage = $"Eksportowano {RlEntries.Count} wpisów do {Path.GetFileName(saveDialog.FileName)}";
                MessageBox.Show($"Pomyślnie eksportowano {RlEntries.Count} wpisów do pliku:\n{saveDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd eksportu: {ex.Message}";
                MessageBox.Show($"Błąd podczas eksportu:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    [RelayCommand]
    private void ExportForTraining()
    {
        if (RlEntries.Count == 0)
        {
            MessageBox.Show("Brak danych do eksportu. Najpierw wygeneruj dataset.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Zapytaj użytkownika o format
        var result = MessageBox.Show(
            "Wybierz format eksportu:\n\n" +
            "Tak - Supervised format (prompt/response) dla Behavior Cloning\n" +
            "Nie - RL format (pełny transition) dla PPO",
            "Format eksportu",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
            return;

        bool supervisedFormat = (result == MessageBoxResult.Yes);

        var saveDialog = new SaveFileDialog
        {
            Filter = "JSONL files (*.jsonl)|*.jsonl|All files (*.*)|*.*",
            FileName = supervisedFormat ? "supervised_dataset.jsonl" : "rl_dataset.jsonl",
            DefaultExt = "jsonl"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                _datasetGenerator.ExportRLForTraining(RlEntries.ToList(), saveDialog.FileName, supervisedFormat);
                string formatName = supervisedFormat ? "Supervised (BC)" : "RL (PPO)";
                StatusMessage = $"Eksportowano {RlEntries.Count} wpisów ({formatName}) do {Path.GetFileName(saveDialog.FileName)}";
                MessageBox.Show($"Pomyślnie eksportowano {RlEntries.Count} wpisów w formacie {formatName} do pliku:\n{saveDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd eksportu: {ex.Message}";
                MessageBox.Show($"Błąd podczas eksportu:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    [RelayCommand]
    private void ShowStatistics()
    {
        if (RlEntries.Count == 0)
        {
            MessageBox.Show("Brak danych. Najpierw wygeneruj dataset.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var stats = _datasetGenerator.GetRLDatasetStatistics(RlEntries.ToList());
            
            var statsText = new System.Text.StringBuilder();
            statsText.AppendLine("=== Statystyki Datasetu RL ===");
            statsText.AppendLine();
            statsText.AppendLine($"Całkowita liczba wpisów: {stats.TotalEntries}");
            statsText.AppendLine($"Liczba epizodów: {stats.UniqueEpisodes}");
            statsText.AppendLine($"Średnia liczba kroków/epizod: {stats.AverageStepsPerEpisode:F2}");
            statsText.AppendLine();
            statsText.AppendLine("=== Nagrody ===");
            statsText.AppendLine($"Średnia: {stats.MeanReward:F2}");
            statsText.AppendLine($"Mediana: {stats.MedianReward:F2}");
            statsText.AppendLine($"Min: {stats.MinReward:F2}");
            statsText.AppendLine($"Max: {stats.MaxReward:F2}");
            statsText.AppendLine($"Odchylenie std: {stats.StdDevReward:F2}");
            statsText.AppendLine();
            statsText.AppendLine($"Pozytywne nagrody (≥0): {stats.PositiveRewardPercentage:F1}%");
            statsText.AppendLine($"Kary (<0): {stats.NegativeRewardPercentage:F1}%");
            statsText.AppendLine();
            statsText.AppendLine("=== Epizody ===");
            statsText.AppendLine($"Stany terminalne: {stats.TerminalStates}");
            statsText.AppendLine($"Naturalne zakończenia: {stats.NaturalTerminations}");
            
            MessageBox.Show(statsText.ToString(), "Statystyki Datasetu", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas obliczania statystyk:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearData()
    {
        RlEntries.Clear();
        SelectedRLEntry = null;
        GeneratedCount = 0;
        StatusMessage = "Gotowy";
    }

    private string FormatEntryDetails(CrosswordRLDatasetEntry entry)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Epizod: {entry.EpisodeId}, Krok: {entry.T}");
        sb.AppendLine($"Akcja: {entry.Action.Word} na ({entry.Action.Row}, {entry.Action.Column}) {entry.Action.Direction}");
        sb.AppendLine($"Nagroda: {entry.Reward:F2}");
        sb.AppendLine($"  - Completion: {entry.RewardDetails.CompletionReward:F2}");
        sb.AppendLine($"  - Placement: {entry.RewardDetails.PlacementReward:F2}");
        sb.AppendLine($"  - Intersection: {entry.RewardDetails.IntersectionReward:F2}");
        sb.AppendLine($"  - Penalty: {entry.RewardDetails.Penalty:F2}");
        sb.AppendLine($"Terminal: {entry.IsTerminal} (done: {entry.Done})");
        if (entry.IsTerminal)
        {
            sb.AppendLine($"Naturalne zakończenie: {entry.IsNaturalTermination} {(entry.IsNaturalTermination ? "(wszystkie słowa umieszczone)" : "(cutoff/max_steps)")}");
        }
        sb.AppendLine($"Pozostałe słowa: {string.Join(", ", entry.RemainingWords)}");
        sb.AppendLine($"Umieszczone słowa: {entry.PlacedWords.Count}");
        sb.AppendLine();
        sb.AppendLine("Stan przed akcją:");
        sb.AppendLine(entry.State);
        sb.AppendLine();
        sb.AppendLine("Stan po akcji:");
        sb.AppendLine(entry.NextState);
        return sb.ToString();
    }
}
