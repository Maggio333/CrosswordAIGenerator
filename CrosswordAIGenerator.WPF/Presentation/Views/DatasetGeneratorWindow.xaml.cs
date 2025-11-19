using System.Windows;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for DatasetGeneratorWindow.xaml
/// </summary>
public partial class DatasetGeneratorWindow : Window
{
    public DatasetGeneratorWindow(DatasetGeneratorViewModel viewModel)
    {
        InitializeComponent();
        
        // Ustaw ViewModel z DI jako DataContext
        DataContext = viewModel;
        
        // Ustaw referencję do CrosswordView w ViewModel
        viewModel.SetCrosswordView(CrosswordViewControl);
    }
}

