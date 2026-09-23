using System.Globalization;
using System.Windows.Data;

namespace ShopsManagement.UI.Converters;

public class NetBalanceTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            if (amount > 0)
                return $"+{amount:N2} ر.ي (له)";
            else if (amount < 0)
                return $"{amount:N2} ر.ي (عليه)";
            else
                return "0.00 ر.ي";
        }
        return "0.00 ر.ي";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
