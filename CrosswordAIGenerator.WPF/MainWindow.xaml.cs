using System.Windows;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;
using CrosswordAIGenerator.WPF.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CrosswordAIGenerator.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        
        // Ustaw ViewModel z DI jako DataContext
        DataContext = viewModel;
        
        // Ustaw referencję do CrosswordView w ViewModel
        viewModel.SetCrosswordView(CrosswordViewControl);
    }
}
