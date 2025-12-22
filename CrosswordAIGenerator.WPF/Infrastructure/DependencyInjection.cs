using CrosswordAIGenerator.Core;
using CrosswordAIGenerator.Core.Application.Services;
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
        services.AddTransient<RLDatasetGeneratorViewModel>(serviceProvider =>
        {
            var datasetGenerator = serviceProvider.GetRequiredService<DatasetGenerator>();
            var logger = serviceProvider.GetService<CrosswordAIGenerator.Core.Domain.Services.ICursorLogger>();
            return new RLDatasetGeneratorViewModel(datasetGenerator, logger);
        });
        services.AddSingleton<SettingsViewModel>(); // Singleton - ustawienia są współdzielone
        services.AddTransient<CrosswordAIGenerator.WPF.Presentation.ViewModels.CrossGridPreviewViewModel>();
        
        // Chatbot ViewModel i Window
        services.AddTransient<ChatbotViewModel>(serviceProvider =>
        {
            var chatbotService = serviceProvider.GetRequiredService<CrosswordAIGenerator.Core.Domain.Services.IChatbotService>();
            var crossGridGenerator = serviceProvider.GetRequiredService<CrosswordAIGenerator.Core.Domain.Services.ICrossGridGenerator>();
            var xamlGenerator = serviceProvider.GetRequiredService<CrosswordAIGenerator.Core.Domain.Services.IXamlGenerator>();
            var screenshotService = serviceProvider.GetRequiredService<IScreenshotService>();
            var logger = serviceProvider.GetService<CrosswordAIGenerator.Core.Domain.Services.ICursorLogger>();
            return new ChatbotViewModel(chatbotService, crossGridGenerator, xamlGenerator, screenshotService, logger);
        });
        services.AddTransient<ChatbotWindow>(serviceProvider =>
        {
            var viewModel = serviceProvider.GetRequiredService<ChatbotViewModel>();
            return new ChatbotWindow(viewModel);
        });
        
        services.AddTransient<MainWindow>();
        services.AddTransient<DatasetGeneratorWindow>();
        services.AddTransient<RLDatasetGeneratorWindow>(serviceProvider =>
        {
            var viewModel = serviceProvider.GetRequiredService<RLDatasetGeneratorViewModel>();
            return new RLDatasetGeneratorWindow(viewModel);
        });
        services.AddTransient<CrosswordAIGenerator.WPF.Presentation.Views.CrossGridPreviewWindow>();

        return services;
    }

}

