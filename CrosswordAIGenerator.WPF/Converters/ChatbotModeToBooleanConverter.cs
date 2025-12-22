using System.Globalization;
using System.Windows.Data;
using CrosswordAIGenerator.Core.Domain.Models;

namespace CrosswordAIGenerator.WPF.Converters;

/// <summary>
/// Konwerter który zwraca true jeśli ChatbotMode == Crossword
/// </summary>
public class ChatbotModeToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ChatbotMode mode)
        {
            return mode == ChatbotMode.Crossword;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

