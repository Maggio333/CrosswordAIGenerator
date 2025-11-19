using System.Globalization;
using System.Windows.Data;

namespace CrosswordAIGenerator.WPF.Converters;

/// <summary>
/// Konwerter który konwertuje bool na tekst ("Ze słowami" / "Pusta siatka")
/// </summary>
public class BooleanToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Ze słowami" : "Pusta siatka";
        }
        return "Nieznany";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

