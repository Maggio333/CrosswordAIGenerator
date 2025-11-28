using CrosswordAIGenerator.Core.Application.Services;
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

        // IDictionaryPathResolver - serwis do znajdowania ścieżki słownika
        services.AddSingleton<IDictionaryPathResolver>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ICursorLogger>();
            return new Infrastructure.Services.DictionaryPathResolver(logger);
        });

        // IWordDictionary - factory pattern (tylko slowa.txt)
        services.AddSingleton<IWordDictionary>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ICursorLogger>();
            var pathResolver = serviceProvider.GetRequiredService<IDictionaryPathResolver>();
            var dictionaryPath = pathResolver.FindDictionaryFile();
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

        // Random - singleton dla spójności seedów (opcjonalnie można użyć factory z seed)
        services.AddSingleton<Random>(serviceProvider => new Random());

        // DatasetDescriptionGenerator - generator opisów, searchable text i embedding text
        services.AddSingleton<IDatasetDescriptionGenerator>(serviceProvider =>
        {
            return new Application.Services.DatasetDescriptionGenerator();
        });

        // DatasetPromptGenerator - generator promptów do finetunowania
        services.AddSingleton<IDatasetPromptGenerator>(serviceProvider =>
        {
            return new Application.Services.DatasetPromptGenerator();
        });

        // DatasetExporter - eksporter datasetów do plików (Infrastructure - operacje I/O)
        services.AddSingleton<IDatasetExporter>(serviceProvider =>
        {
            var promptGenerator = serviceProvider.GetRequiredService<IDatasetPromptGenerator>();
            return new Infrastructure.Services.DatasetExporter(promptGenerator);
        });

        // EmptyGridDatasetGenerator - generator datasetów z pustymi siatkami
        services.AddSingleton<IEmptyGridDatasetGenerator>(serviceProvider =>
        {
            var gridGenerator = serviceProvider.GetRequiredService<IEmptyGridGenerator>();
            var xamlGenerator = serviceProvider.GetRequiredService<IXamlGenerator>();
            var descriptionGenerator = serviceProvider.GetRequiredService<IDatasetDescriptionGenerator>();
            var random = serviceProvider.GetRequiredService<Random>();
            return new Application.Services.EmptyGridDatasetGenerator(gridGenerator, xamlGenerator, descriptionGenerator, random);
        });

        // WordsDatasetGenerator - generator datasetów z krzyżówkami ze słowami
        services.AddSingleton<IWordsDatasetGenerator>(serviceProvider =>
        {
            var wordDictionary = serviceProvider.GetRequiredService<IWordDictionary>();
            var logger = serviceProvider.GetService<ICursorLogger>();
            var wordPlacer = new Application.Services.CrosswordWordPlacer(wordDictionary, seed: null, logger);
            var xamlGenerator = serviceProvider.GetRequiredService<IXamlGenerator>();
            var descriptionGenerator = serviceProvider.GetRequiredService<IDatasetDescriptionGenerator>();
            var crossGridGenerator = serviceProvider.GetService<ICrossGridGenerator>();
            var wordGenerator = serviceProvider.GetService<IHighlightedWordGenerator>();
            var random = serviceProvider.GetRequiredService<Random>();
            return new Application.Services.WordsDatasetGenerator(wordPlacer, xamlGenerator, descriptionGenerator, crossGridGenerator, wordGenerator, logger, random);
        });

        // CustomWordsDatasetGenerator - generator datasetów z krzyżówkami z własnymi słowami
        services.AddSingleton<ICustomWordsDatasetGenerator>(serviceProvider =>
        {
            var wordDictionary = serviceProvider.GetRequiredService<IWordDictionary>();
            var logger = serviceProvider.GetService<ICursorLogger>();
            var wordPlacer = new Application.Services.CrosswordWordPlacer(wordDictionary, seed: null, logger);
            var xamlGenerator = serviceProvider.GetRequiredService<IXamlGenerator>();
            var descriptionGenerator = serviceProvider.GetRequiredService<IDatasetDescriptionGenerator>();
            var crossGridGenerator = serviceProvider.GetService<ICrossGridGenerator>();
            var random = serviceProvider.GetRequiredService<Random>();
            return new Application.Services.CustomWordsDatasetGenerator(wordPlacer, xamlGenerator, descriptionGenerator, crossGridGenerator, logger, random);
        });

        // DatasetGenerator - orchestrator który deleguje do specjalistycznych serwisów
        services.AddSingleton<DatasetGenerator>(serviceProvider =>
        {
            var emptyGridGenerator = serviceProvider.GetRequiredService<IEmptyGridDatasetGenerator>();
            var wordsGenerator = serviceProvider.GetRequiredService<IWordsDatasetGenerator>();
            var customWordsGenerator = serviceProvider.GetRequiredService<ICustomWordsDatasetGenerator>();
            var exporter = serviceProvider.GetRequiredService<IDatasetExporter>();
            var crossGridGenerator = serviceProvider.GetService<ICrossGridGenerator>();
            return new DatasetGenerator(emptyGridGenerator, wordsGenerator, customWordsGenerator, exporter, crossGridGenerator);
        });

        return services;
    }
}

