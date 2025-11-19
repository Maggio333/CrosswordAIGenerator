using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

namespace CrosswordAIGenerator.WPF.Presentation.Views;

/// <summary>
/// Interaction logic for CrosswordView.xaml
/// </summary>
public partial class CrosswordView : UserControl
{
    public CrosswordView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ładuje XAML z stringa i renderuje krzyżówkę
    /// </summary>
    public void LoadXaml(string xamlString)
    {
        // Loguj do pliku przez System.Diagnostics.Debug (CursorLogger zapisuje to do pliku)
        System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: Rozpoczęcie, XAML długość: {xamlString?.Length ?? 0}");
        
        try
        {
            if (string.IsNullOrWhiteSpace(xamlString))
            {
                System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: XAML jest pusty!");
                return;
            }
            
            // Parsuj XAML string do obiektu
            // Użyj MemoryStream z UTF-8 encoding dla poprawnych polskich znaków
            var xamlBytes = System.Text.Encoding.UTF8.GetBytes(xamlString);
            using (var memoryStream = new MemoryStream(xamlBytes))
            using (var xmlReader = XmlReader.Create(memoryStream))
            {
                System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: Parsuję XAML...");
                var loadedObject = XamlReader.Load(xmlReader);
                System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: XAML sparsowany, Typ: {loadedObject.GetType().Name}");
                
                // Sprawdź czy to ScrollViewer (nowy format) czy Grid (stary format)
                System.Windows.UIElement? contentToSet = null;
                if (loadedObject is System.Windows.Controls.ScrollViewer scrollViewer)
                {
                    System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: Załadowano ScrollViewer");
                    contentToSet = scrollViewer;
                }
                else if (loadedObject is System.Windows.Controls.Grid grid)
                {
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: Załadowano Grid, Children: {grid.Children.Count}");
                    // Ustaw wymiary jeśli nie są ustawione
                    if (double.IsNaN(grid.Width) || grid.Width <= 0)
                    {
                        grid.Width = 500;
                    }
                    if (double.IsNaN(grid.Height) || grid.Height <= 0)
                    {
                        grid.Height = 500;
                    }
                    contentToSet = grid;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: Nieoczekiwany typ: {loadedObject.GetType().Name}");
                    contentToSet = loadedObject as System.Windows.UIElement;
                }
                
                if (contentToSet == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: BŁĄD - nie udało się załadować jako UIElement!");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: Content wymiary: {contentToSet.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: CrosswordContent = {CrosswordContent}");
                
                // Wyczyść poprzednią zawartość
                CrosswordContent.Content = null;
                
                // Ustaw nową zawartość
                CrosswordContent.Content = contentToSet;
                System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: Content ustawiony, CrosswordContent.Content = {CrosswordContent.Content?.GetType().Name ?? "null"}");
                
                // Wymuś aktualizację layoutu
                UpdateLayout();
                System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: UpdateLayout wywołane");
                
                // Wymuś renderowanie
                InvalidateVisual();
                System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: InvalidateVisual wywołane");
                
                // Wymuś odświeżenie wizualne
                InvalidateArrange();
                InvalidateMeasure();
                System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: InvalidateArrange i InvalidateMeasure wywołane");
            }
            
            System.Diagnostics.Debug.WriteLine("[CURSOR] CrosswordView.LoadXaml: Zakończono pomyślnie");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: BŁĄD! {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CURSOR] CrosswordView.LoadXaml: StackTrace: {ex.StackTrace}");
            MessageBox.Show($"Błąd podczas ładowania XAML: {ex.Message}\n\n{ex.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Zwraca wewnętrzny Grid (dla screenshotowania)
    /// </summary>
    public System.Windows.Controls.Grid? GetInnerGrid()
    {
        // Jeśli Content to ScrollViewer, znajdź Grid wewnątrz
        if (CrosswordContent.Content is System.Windows.Controls.ScrollViewer scrollViewer)
        {
            return scrollViewer.Content as System.Windows.Controls.Grid;
        }
        return CrosswordContent.Content as System.Windows.Controls.Grid;
    }
    
    /// <summary>
    /// Zwraca ScrollViewer (dla screenshotowania)
    /// </summary>
    public System.Windows.Controls.ScrollViewer? GetScrollViewer()
    {
        return CrosswordContent.Content as System.Windows.Controls.ScrollViewer;
    }

    /// <summary>
    /// Czyści zawartość widoku
    /// </summary>
    public void Clear()
    {
        CrosswordContent.Content = null;
    }
}

