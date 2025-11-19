using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CrosswordAIGenerator.Core;
using CrosswordAIGenerator.WPF.Infrastructure;

namespace CrosswordAIGenerator.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Ustaw kulturę na polską dla poprawnego przetwarzania polskich znaków
        Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");
        
        base.OnStartup(e);

        // Konfiguruj kontener DI (Core + WPF)
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Utwórz MainWindow z DI
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Rejestruj wszystkie serwisy Core (Application, Domain, Infrastructure)
        services.AddCrosswordAIGeneratorCore();
        
        // Rejestruj serwisy specyficzne dla WPF (screenshoty, ViewModele, Views)
        services.AddWpfInfrastructure();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

