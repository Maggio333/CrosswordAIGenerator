using System.Windows;
using System.Windows.Controls;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for CustomWordsView.xaml
/// </summary>
public partial class CustomWordsView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(CustomWordsViewModel),
            typeof(CustomWordsView),
            new PropertyMetadata(null, OnViewModelChanged));

    public CustomWordsViewModel? ViewModel
    {
        get => (CustomWordsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomWordsView view && e.NewValue is CustomWordsViewModel viewModel)
        {
            view.DataContext = viewModel;
            
            // Debug - szczegółowe logowanie
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: DataContext set to {viewModel.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: LoadExampleDataCommand = {viewModel.LoadExampleDataCommand}");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: AddWordCommand = {viewModel.AddWordCommand}");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: GenerateSingleCommand = {viewModel.GenerateSingleCommand}");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: GenerateDatasetCommand = {viewModel.GenerateDatasetCommand}");
            
            // Sprawdź czy commands są null
            if (viewModel.LoadExampleDataCommand == null)
            {
                System.Diagnostics.Debug.WriteLine("[CURSOR] OnViewModelChanged: UWAGA! LoadExampleDataCommand jest NULL!");
            }
            if (viewModel.GenerateSingleCommand == null)
            {
                System.Diagnostics.Debug.WriteLine("[CURSOR] OnViewModelChanged: UWAGA! GenerateSingleCommand jest NULL!");
            }
            
            // NIE ustawiamy CrosswordView z CustomWordsView - używamy tego z MainWindow (pierwszej zakładki)
            // CrosswordView będzie ustawiony w MainWindow.xaml.cs
            System.Diagnostics.Debug.WriteLine("[CURSOR] OnViewModelChanged: CrosswordView będzie ustawiony w MainWindow (używa tego samego co pierwsza zakładka)");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] OnViewModelChanged: BŁĄD! view = {d?.GetType().Name}, viewModel = {e.NewValue?.GetType().Name ?? "null"}");
        }
    }

    public CustomWordsView()
    {
        InitializeComponent();
        Loaded += CustomWordsView_Loaded;
    }

    private void CustomWordsView_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CURSOR] CustomWordsView_Loaded: ViewModel = {ViewModel}, DataContext = {DataContext}");
        
        // Jeśli ViewModel został już ustawiony, upewnij się że DataContext jest poprawny
        if (ViewModel != null && DataContext != ViewModel)
        {
            System.Diagnostics.Debug.WriteLine("[CURSOR] CustomWordsView_Loaded: Ustawiam DataContext na ViewModel");
            DataContext = ViewModel;
        }
        
        // NIE ustawiamy CrosswordView z CustomWordsView - używamy tego z MainWindow (pierwszej zakładki)
        // CrosswordView będzie ustawiony w MainWindow.xaml.cs
        System.Diagnostics.Debug.WriteLine("[CURSOR] CustomWordsView_Loaded: CrosswordView będzie ustawiony w MainWindow (używa tego samego co pierwsza zakładka)");
    }

    public void SetViewModel(CustomWordsViewModel viewModel)
    {
        ViewModel = viewModel; // Użyj właściwości zależności
    }

    private void RemoveWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomWordEntry wordEntry && DataContext is CustomWordsViewModel viewModel)
        {
            viewModel.RemoveWordCommand.Execute(wordEntry);
        }
    }

    private void LoadExampleDataButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[CURSOR] LoadExampleDataButton_Click: Kliknięto przycisk");
        if (DataContext is CustomWordsViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] LoadExampleDataButton_Click: DataContext OK, Command = {viewModel.LoadExampleDataCommand}");
            viewModel.LoadExampleDataCommand?.Execute(null);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] LoadExampleDataButton_Click: DataContext = {DataContext?.GetType().Name ?? "null"}");
        }
    }

    private void AddWordButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomWordsViewModel viewModel)
        {
            viewModel.AddWordCommand?.Execute(null);
        }
    }

    private void GenerateSingleButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[CURSOR] GenerateSingleButton_Click: Kliknięto przycisk");
        if (DataContext is CustomWordsViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateSingleButton_Click: DataContext OK, Command = {viewModel.GenerateSingleCommand}");
            viewModel.GenerateSingleCommand?.Execute(null);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] GenerateSingleButton_Click: DataContext = {DataContext?.GetType().Name ?? "null"}");
        }
    }

    private void GenerateDatasetButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomWordsViewModel viewModel)
        {
            viewModel.GenerateDatasetCommand?.Execute(null);
        }
    }
}

