using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrosswordAIGenerator.Core.Application.Services;
using CrosswordAIGenerator.Core.Domain.Models;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.WPF.Presentation.ViewModels;

/// <summary>
/// ViewModel dla zakładki Ustawień - kontroluje które elementy są zawarte w datasetach
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ICursorLogger? _logger;
    private const string SettingsFileName = "dataset_settings.json";
    
    private DatasetSettings _settings;

    public SettingsViewModel(ICursorLogger? logger = null)
    {
        _logger = logger;
        _settings = LoadSettings();
    }

    /// <summary>
    /// Ustawienia datasetów
    /// </summary>
    public DatasetSettings Settings
    {
        get => _settings;
        set
        {
            if (_settings != value)
            {
                _settings = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IncludeXaml));
                OnPropertyChanged(nameof(IncludeEmptyXaml));
                OnPropertyChanged(nameof(IncludeCrossGrid));
                OnPropertyChanged(nameof(IncludeScreenshot));
                OnPropertyChanged(nameof(IncludeDescription));
                OnPropertyChanged(nameof(IncludeSearchableText));
                OnPropertyChanged(nameof(IncludeEmbeddingText));
            }
        }
    }

    // Właściwości dla binding (proxy do Settings)
    public bool IncludeXaml
    {
        get => Settings.IncludeXaml;
        set
        {
            if (Settings.IncludeXaml != value)
            {
                Settings.IncludeXaml = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeEmptyXaml
    {
        get => Settings.IncludeEmptyXaml;
        set
        {
            if (Settings.IncludeEmptyXaml != value)
            {
                Settings.IncludeEmptyXaml = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeCrossGrid
    {
        get => Settings.IncludeCrossGrid;
        set
        {
            if (Settings.IncludeCrossGrid != value)
            {
                Settings.IncludeCrossGrid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeScreenshot
    {
        get => Settings.IncludeScreenshot;
        set
        {
            if (Settings.IncludeScreenshot != value)
            {
                Settings.IncludeScreenshot = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeDescription
    {
        get => Settings.IncludeDescription;
        set
        {
            if (Settings.IncludeDescription != value)
            {
                Settings.IncludeDescription = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeSearchableText
    {
        get => Settings.IncludeSearchableText;
        set
        {
            if (Settings.IncludeSearchableText != value)
            {
                Settings.IncludeSearchableText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    public bool IncludeEmbeddingText
    {
        get => Settings.IncludeEmbeddingText;
        set
        {
            if (Settings.IncludeEmbeddingText != value)
            {
                Settings.IncludeEmbeddingText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Settings));
                SaveSettings();
            }
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(settingsPath, json);
            _logger?.Info($"SettingsViewModel: Zapisano ustawienia do {settingsPath}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"SettingsViewModel: Błąd zapisywania ustawień: {ex.Message}", ex);
        }
    }

    private DatasetSettings LoadSettings()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<DatasetSettings>(json);
                if (settings != null)
                {
                    _logger?.Info($"SettingsViewModel: Wczytano ustawienia z {settingsPath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"SettingsViewModel: Błąd wczytywania ustawień: {ex.Message}");
        }

        // Domyślne ustawienia
        return new DatasetSettings();
    }

    private string GetSettingsPath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrosswordAIGenerator");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        
        return Path.Combine(appDataPath, SettingsFileName);
    }
}

