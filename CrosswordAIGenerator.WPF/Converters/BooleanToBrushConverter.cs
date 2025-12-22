using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CrosswordAIGenerator.WPF.Converters;

/// <summary>
/// Konwerter który konwertuje bool na Brush (kolor)
/// </summary>
public class BooleanToBrushConverter : IValueConverter
{
    public Brush? TrueValue { get; set; }
    public Brush? FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? (TrueValue ?? Brushes.Transparent) : (FalseValue ?? Brushes.Transparent);
        }
        return FalseValue ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

