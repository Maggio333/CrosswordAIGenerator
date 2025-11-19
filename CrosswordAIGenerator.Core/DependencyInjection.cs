using CrosswordAIGenerator.Core.Application_.Services;
using CrosswordAIGenerator.Core.Domain.Services;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CrosswordAIGenerator.Core;

/// <summary>
/// Konfiguracja Dependency Injection dla Core (Application, Domain, Infrastructure)
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Rejestruje wszystkie serwisy Core (Application, Domain, Infrastructure)
    /// </summary>
    public static IServiceCollection AddCrosswordAIGeneratorCore(this IServiceCollection services)
    {
        // Logger dla Cursora (AI) - zawsze dostępny
        services.AddSingleton<ICursorLogger, CursorLogger>();

        // Application Services - konfiguracja aplikacji
        services.AddSingleton<IConfigService, ConfigService>();

        // Domain Services - rejestrujemy jako interfejsy
        // Implementacje są w Infrastructure
        services.AddSingleton<IEmptyGridGenerator, Infrastructure.Services.EmptyGridGenerator>();

        // Infrastructure Services - rejestrujemy jako interfejsy
        services.AddSingleton<IXamlGenerator>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ICursorLogger>();
            return new XamlGenerator(logger);
        });

        // CrossGridGenerator - generator formatu CrossGrid (ASCII art)
        services.AddSingleton<ICrossGridGenerator>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ICursorLogger>();
            return new CrossGridGenerator(logger);
        });

        // HighlightedWordGenerator - generator haseł z cache'owaniem
        services.AddSingleton<IHighlightedWordGenerator>(serviceProvider =>
        {
            var wordDictionary = serviceProvider.GetRequiredService<IWordDictionary>();
            var logger = serviceProvider.GetService<ICursorLogger>();
            return new HighlightedWordGenerator(wordDictionary, seed: null, logger);
        });

        // IWordDictionary - factory pattern (tylko slowa.txt)
        services.AddSingleton<IWordDictionary>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ICursorLogger>();
            var dictionaryPath = Application_.Services.DatasetGenerator.FindDictionaryFile();
            if (dictionaryPath != null)
            {
                // Użyj lazy dictionary - nie ładuje całego pliku do pamięci
                logger?.InfoFormat("Tworzenie LazyWordDictionary z pliku: {0}", dictionaryPath);
                return new Infrastructure.Services.LazyWordDictionary(dictionaryPath, seed: null, minWordLength: 6, logger);
            }
            else
            {
                // Błąd - nie znaleziono slowa.txt
                var errorMsg = "Nie znaleziono pliku slowa.txt w katalogu dictionaries/. Aplikacja wymaga tego pliku do działania.";
                logger?.Error(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }
        });

        // DatasetGenerator - factory pattern (używa IWordDictionary z DI)
        services.AddSingleton<DatasetGenerator>(serviceProvider =>
        {
            var gridGenerator = serviceProvider.GetRequiredService<IEmptyGridGenerator>();
            var xamlGenerator = serviceProvider.GetRequiredService<IXamlGenerator>();
            var crossGridGenerator = serviceProvider.GetService<ICrossGridGenerator>();
            var wordDictionary = serviceProvider.GetRequiredService<IWordDictionary>();
            var wordGenerator = serviceProvider.GetService<IHighlightedWordGenerator>();
            var logger = serviceProvider.GetService<ICursorLogger>();
            var wordPlacer = new Domain.Services.CrosswordWordPlacer(wordDictionary, seed: null, logger);
            return new DatasetGenerator(gridGenerator, xamlGenerator, wordDictionary, wordPlacer, wordGenerator, logger, crossGridGenerator);
        });

        return services;
    }
}

