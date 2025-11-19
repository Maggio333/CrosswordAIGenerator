using System.Windows;
using CrosswordAIGenerator.Core.Application_.Services;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;
using CrosswordAIGenerator.WPF.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CrosswordAIGenerator.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
    public partial class MainWindow : Window
    {
        private readonly CustomWordsViewModel _customWordsViewModel;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(
            MainWindowViewModel viewModel, 
            CustomWordsViewModel customWordsViewModel, 
            SettingsViewModel settingsViewModel,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            _customWordsViewModel = customWordsViewModel;
            _mainWindowViewModel = viewModel;
            _settingsViewModel = settingsViewModel;
            _serviceProvider = serviceProvider;
            
            // Ustaw ViewModel z DI jako DataContext dla pierwszej zakładki
            DataContext = viewModel;
            
            // Ustaw referencję do CrosswordView w ViewModel (pierwsza zakładka)
            viewModel.SetCrosswordView(CrosswordViewControl);
            
            // WAŻNE: CustomWordsViewModel też używa tego samego CrosswordView z pierwszej zakładki!
            // Dzięki temu krzyżówki generowane w zakładce "Własne słowa" wyświetlają się na zakładce "Automatyczne"
            _customWordsViewModel.SetCrosswordView(CrosswordViewControl);
            
            // Ustaw referencję do MainWindowViewModel w CustomWordsViewModel, żeby mógł dodawać wpisy
            _customWordsViewModel.SetMainWindowViewModel(viewModel);
            
            // Ustaw CustomWordsViewModel dla drugiej zakładki - bezpośrednio po InitializeComponent
            // CustomWordsViewControl powinien być już dostępny po InitializeComponent
            if (CustomWordsViewControl != null)
            {
                CustomWordsViewControl.SetViewModel(_customWordsViewModel);
            }
            else
            {
                // Fallback: jeśli jeszcze nie jest dostępny, użyj Loaded event
                Loaded += MainWindow_Loaded;
            }
            
            // Ustaw SettingsViewModel dla trzeciej zakładki
            if (SettingsViewControl != null)
            {
                SettingsViewControl.ViewModel = _settingsViewModel;
            }
            
            // Dodatkowo: ustaw też po załadowaniu okna (dla pewności)
            Loaded += MainWindow_Loaded;
        }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Ustaw ViewModel dla CustomWordsView jeśli jeszcze nie został ustawiony
        if (CustomWordsViewControl != null && CustomWordsViewControl.ViewModel == null)
        {
            CustomWordsViewControl.SetViewModel(_customWordsViewModel);
        }
    }

    private void CustomWordsTabItem_Loaded(object sender, RoutedEventArgs e)
    {
        // Ustaw ViewModel gdy zakładka jest załadowana (gdy użytkownik ją wybierze)
        if (CustomWordsViewControl != null && CustomWordsViewControl.ViewModel == null)
        {
            CustomWordsViewControl.SetViewModel(_customWordsViewModel);
        }
    }

    private void SettingsTabItem_Loaded(object sender, RoutedEventArgs e)
    {
        // Ustaw ViewModel gdy zakładka jest załadowana (gdy użytkownik ją wybierze)
        if (SettingsViewControl != null && SettingsViewControl.ViewModel == null)
        {
            SettingsViewControl.ViewModel = _settingsViewModel;
        }
    }

    private void OpenCrossGridPreview_Click(object sender, RoutedEventArgs e)
    {
        var previewWindow = _serviceProvider.GetRequiredService<CrosswordAIGenerator.WPF.Presentation.Views.CrossGridPreviewWindow>();
        previewWindow.Show();
    }
}
