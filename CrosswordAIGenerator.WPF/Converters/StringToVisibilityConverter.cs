using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CrosswordAIGenerator.WPF.Converters;

/// <summary>
/// Konwerter który konwertuje string na Visibility (null/empty -> Collapsed, inaczej -> Visible)
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

