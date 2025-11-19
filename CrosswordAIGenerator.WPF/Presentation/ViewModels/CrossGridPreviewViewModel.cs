using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using CrosswordAIGenerator.WPF.Presentation.Views;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

public partial class CrossGridPreviewViewModel : ObservableObject
{
    private readonly ICrossGridGenerator _crossGridGenerator;
    private readonly IXamlGenerator _xamlGenerator;
    private CrosswordView? _crosswordView;

    [ObservableProperty]
    private string _crossGridText = string.Empty;

    [ObservableProperty]
    private string _generatedXaml = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isValidating = false;

    public CrossGridPreviewViewModel(
        ICrossGridGenerator crossGridGenerator,
        IXamlGenerator xamlGenerator)
    {
        _crossGridGenerator = crossGridGenerator ?? throw new ArgumentNullException(nameof(crossGridGenerator));
        _xamlGenerator = xamlGenerator ?? throw new ArgumentNullException(nameof(xamlGenerator));
    }

    public void SetCrosswordView(CrosswordView view)
    {
        _crosswordView = view;
    }

    [RelayCommand]
    private void ConvertToXaml()
    {
        if (string.IsNullOrWhiteSpace(CrossGridText))
        {
            StatusMessage = "Wklej kod CrossGrid";
            MessageBox.Show("Wklej kod CrossGrid do pola tekstowego.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsValidating = true;
            StatusMessage = "Konwertowanie...";

            // Normalizuj tekst - zamień escape sequences na rzeczywiste znaki nowej linii
            string normalizedText = CrossGridText
                .Replace("\\r\\n", "\r\n")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r");

            // Waliduj CrossGrid
            var validationResult = _crossGridGenerator.ValidateCrossGrid(normalizedText);
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join("\n", validationResult.Errors);
                ValidationMessage = $"Błędy walidacji:\n{errors}";
                StatusMessage = "Błąd walidacji";
                MessageBox.Show($"Błędy walidacji CrossGrid:\n\n{errors}", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Spróbuj i tak skonwertować (może być częściowo poprawny)
            }
            else
            {
                ValidationMessage = "✓ CrossGrid jest poprawny";
            }

            if (validationResult.Warnings.Any())
            {
                var warnings = string.Join("\n", validationResult.Warnings);
                ValidationMessage += $"\n\nOstrzeżenia:\n{warnings}";
            }

            // Konwertuj CrossGrid do XAML (użyj znormalizowanego tekstu)
            var xaml = _crossGridGenerator.CrossGridToXaml(normalizedText, _xamlGenerator);
            GeneratedXaml = xaml;

            // Załaduj do CrosswordView
            if (_crosswordView != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _crosswordView.LoadXaml(xaml);
                    _crosswordView.UpdateLayout();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }

            StatusMessage = "Skonwertowano pomyślnie";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            ValidationMessage = $"Błąd konwersji: {ex.Message}";
            MessageBox.Show($"Błąd podczas konwersji CrossGrid do XAML:\n\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsValidating = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CrossGridText = string.Empty;
        GeneratedXaml = string.Empty;
        ValidationMessage = string.Empty;
        StatusMessage = "Gotowy";
        
        if (_crosswordView != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _crosswordView.Clear();
            });
        }
    }
}

