using CrosswordAIGenerator.Core;
using CrosswordAIGenerator.WPF.Presentation.ViewModels;
using CrosswordAIGenerator.WPF.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CrosswordAIGenerator.WPF.Infrastructure;

/// <summary>
/// Konfiguracja Dependency Injection dla WPF (tylko rzeczy specyficzne dla WPF)
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Rejestruje serwisy specyficzne dla WPF (screenshoty, ViewModele, Views)
    /// </summary>
    public static IServiceCollection AddWpfInfrastructure(this IServiceCollection services)
    {
        // WPF Infrastructure Services - specyficzne dla WPF (screenshoty)
        services.AddSingleton<IScreenshotService, ScreenshotService>();

        // WPF Presentation Services - ViewModele i Views
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<CustomWordsViewModel>();
        services.AddTransient<DatasetGeneratorViewModel>();
        services.AddSingleton<SettingsViewModel>(); // Singleton - ustawienia są współdzielone
        services.AddTransient<CrosswordAIGenerator.WPF.Presentation.ViewModels.CrossGridPreviewViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<DatasetGeneratorWindow>();
        services.AddTransient<CrosswordAIGenerator.WPF.Presentation.Views.CrossGridPreviewWindow>();

        return services;
    }

}

