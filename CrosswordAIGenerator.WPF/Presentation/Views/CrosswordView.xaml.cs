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
        try
        {
            // Parsuj XAML string do obiektu
            // Użyj MemoryStream z UTF-8 encoding dla poprawnych polskich znaków
            var xamlBytes = System.Text.Encoding.UTF8.GetBytes(xamlString);
            using (var memoryStream = new MemoryStream(xamlBytes))
            using (var xmlReader = XmlReader.Create(memoryStream))
            {
                var grid = (System.Windows.Controls.Grid)XamlReader.Load(xmlReader);
                
                // Ustaw wymiary jeśli nie są ustawione
                if (double.IsNaN(grid.Width) || grid.Width <= 0)
                {
                    grid.Width = 500;
                }
                if (double.IsNaN(grid.Height) || grid.Height <= 0)
                {
                    grid.Height = 500;
                }
                
                // Wyczyść poprzednią zawartość
                CrosswordContent.Content = null;
                
                // Ustaw nową zawartość
                CrosswordContent.Content = grid;
                
                // Wymuś aktualizację layoutu
                UpdateLayout();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas ładowania XAML: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Zwraca wewnętrzny Grid (dla screenshotowania)
    /// </summary>
    public System.Windows.Controls.Grid? GetInnerGrid()
    {
        return CrosswordContent.Content as System.Windows.Controls.Grid;
    }

    /// <summary>
    /// Czyści zawartość widoku
    /// </summary>
    public void Clear()
    {
        CrosswordContent.Content = null;
    }
}

