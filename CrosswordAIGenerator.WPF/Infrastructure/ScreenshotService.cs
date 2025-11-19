using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrosswordAIGenerator.WPF.Infrastructure;

/// <summary>
/// Serwis do robienia screenshotów z WPF Controls
/// </summary>
public class ScreenshotService : IScreenshotService
{
    /// <summary>
    /// Robi screenshot z WPF Control i zwraca jako base64 string
    /// </summary>
    public string CaptureToBase64(FrameworkElement element, int? width = null, int? height = null)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        // Wymuś renderowanie
        element.UpdateLayout();
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));

        // Jeśli element nie ma wymiarów, użyj DesiredSize
        double actualWidth = element.ActualWidth > 0 ? element.ActualWidth : element.DesiredSize.Width;
        double actualHeight = element.ActualHeight > 0 ? element.ActualHeight : element.DesiredSize.Height;

        // Jeśli nadal nie ma wymiarów, użyj domyślnych
        if (actualWidth <= 0) actualWidth = 500;
        if (actualHeight <= 0) actualHeight = 500;

        // Jeśli podano wymiary, użyj ich
        int renderWidth = width ?? (int)actualWidth;
        int renderHeight = height ?? (int)actualHeight;

        if (renderWidth <= 0 || renderHeight <= 0)
        {
            throw new InvalidOperationException($"Cannot capture screenshot: element has no valid size (Width: {actualWidth}, Height: {actualHeight})");
        }

        // Utwórz RenderTargetBitmap
        var renderTarget = new RenderTargetBitmap(
            renderWidth,
            renderHeight,
            96, // DPI X
            96, // DPI Y
            PixelFormats.Pbgra32);

        // Renderuj element - jeśli to UserControl, spróbuj renderować bezpośrednio zawartość
        if (element is System.Windows.Controls.UserControl userControl)
        {
            // Spróbuj znaleźć wewnętrzny Grid
            var innerGrid = userControl.Content as System.Windows.Controls.Grid;
            if (innerGrid != null && innerGrid.ActualWidth > 0 && innerGrid.ActualHeight > 0)
            {
                renderTarget.Render(innerGrid);
            }
            else
            {
                renderTarget.Render(element);
            }
        }
        else
        {
            renderTarget.Render(element);
        }

        // Konwertuj do PNG
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        // Zapisz do MemoryStream
        using (var memoryStream = new MemoryStream())
        {
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            // Konwertuj do base64
            byte[] imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }

    /// <summary>
    /// Robi screenshot i zapisuje do pliku (PNG)
    /// </summary>
    public void CaptureToFile(FrameworkElement element, string filePath, int? width = null, int? height = null)
    {
        var base64 = CaptureToBase64(element, width, height);
        byte[] imageBytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(filePath, imageBytes);
    }

    /// <summary>
    /// Robi screenshot i zapisuje jako JPG
    /// </summary>
    public void CaptureToJpg(FrameworkElement element, string filePath, int? width = null, int? height = null, int quality = 90)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        // Wymuś renderowanie
        element.UpdateLayout();
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(element.DesiredSize));

        // Jeśli element nie ma wymiarów, użyj DesiredSize
        double actualWidth = element.ActualWidth > 0 ? element.ActualWidth : element.DesiredSize.Width;
        double actualHeight = element.ActualHeight > 0 ? element.ActualHeight : element.DesiredSize.Height;

        // Jeśli nadal nie ma wymiarów, użyj domyślnych
        if (actualWidth <= 0) actualWidth = 500;
        if (actualHeight <= 0) actualHeight = 500;

        // Jeśli podano wymiary, użyj ich
        int renderWidth = width ?? (int)actualWidth;
        int renderHeight = height ?? (int)actualHeight;

        if (renderWidth <= 0 || renderHeight <= 0)
        {
            throw new InvalidOperationException($"Cannot capture screenshot: element has no valid size (Width: {actualWidth}, Height: {actualHeight})");
        }

        // Utwórz RenderTargetBitmap z białym tłem
        var renderTarget = new RenderTargetBitmap(
            renderWidth,
            renderHeight,
            96, // DPI X
            96, // DPI Y
            PixelFormats.Pbgra32);

        // Wypełnij białym tłem
        var whiteBrush = new SolidColorBrush(Colors.White);
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(whiteBrush, null, new Rect(0, 0, renderWidth, renderHeight));
        }
        renderTarget.Render(drawingVisual);

        // Renderuj element - jeśli to UserControl, spróbuj renderować bezpośrednio zawartość
        if (element is System.Windows.Controls.UserControl userControl)
        {
            // Spróbuj znaleźć wewnętrzny Grid lub ScrollViewer
            if (userControl.Content is System.Windows.Controls.ScrollViewer scrollViewer)
            {
                var grid = scrollViewer.Content as System.Windows.Controls.Grid;
                if (grid != null && grid.ActualWidth > 0 && grid.ActualHeight > 0)
                {
                    renderTarget.Render(grid);
                }
                else
                {
                    renderTarget.Render(scrollViewer);
                }
            }
            else if (userControl.Content is System.Windows.Controls.Grid innerGrid)
            {
                if (innerGrid.ActualWidth > 0 && innerGrid.ActualHeight > 0)
                {
                    renderTarget.Render(innerGrid);
                }
                else
                {
                    renderTarget.Render(element);
                }
            }
            else
            {
                renderTarget.Render(element);
            }
        }
        else if (element is System.Windows.Controls.ScrollViewer scrollViewer)
        {
            // Jeśli element to ScrollViewer, renderuj jego zawartość (Grid)
            var grid = scrollViewer.Content as System.Windows.Controls.Grid;
            if (grid != null && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                renderTarget.Render(grid);
            }
            else
            {
                renderTarget.Render(scrollViewer);
            }
        }
        else
        {
            renderTarget.Render(element);
        }

        // Konwertuj do JPG
        var encoder = new JpegBitmapEncoder();
        encoder.QualityLevel = Math.Clamp(quality, 1, 100);
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        // Zapisz do pliku
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            encoder.Save(fileStream);
        }
    }
}

