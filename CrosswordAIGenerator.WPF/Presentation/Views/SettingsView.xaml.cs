using System.Windows.Controls;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public SettingsViewModel? ViewModel
    {
        get => DataContext as SettingsViewModel;
        set => DataContext = value;
    }
}

