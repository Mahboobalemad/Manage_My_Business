using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShopsManagement.UI.Converters;

public class NetBalanceColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));  // #059669 Green for له
    private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));   // #DC2626 Red for عليه
    private static readonly SolidColorBrush DarkBrush = new SolidColorBrush(Color.FromRgb(15, 23, 42));   // #0F172A Dark

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            if (amount > 0) return GreenBrush;
            if (amount < 0) return RedBrush;
        }
        return DarkBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
