using System.Windows;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for CrossGridPreviewWindow.xaml
/// </summary>
public partial class CrossGridPreviewWindow : Window
{
    private readonly CrossGridPreviewViewModel _viewModel;

    public CrossGridPreviewWindow(CrossGridPreviewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        // Ustaw CrosswordView w ViewModel
        Loaded += (s, e) =>
        {
            _viewModel.SetCrosswordView(CrosswordViewControl);
        };
    }
}

