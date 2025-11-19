using System.Windows;

namespace CrosswordAIGenerator.WPF.Infrastructure;

/// <summary>
/// Interfejs dla serwisu screenshotów
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Robi screenshot z WPF Control i zwraca jako base64 string
    /// </summary>
    string CaptureToBase64(FrameworkElement element, int? width = null, int? height = null);

    /// <summary>
    /// Robi screenshot z WPF Control i zapisuje jako JPG
    /// </summary>
    void CaptureToJpg(FrameworkElement element, string filePath, int? width = null, int? height = null, int quality = 90);
}

