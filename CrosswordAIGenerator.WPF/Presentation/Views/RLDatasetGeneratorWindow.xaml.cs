using System.Windows;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for RLDatasetGeneratorWindow.xaml
/// </summary>
public partial class RLDatasetGeneratorWindow : Window
{
    public RLDatasetGeneratorWindow(RLDatasetGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
