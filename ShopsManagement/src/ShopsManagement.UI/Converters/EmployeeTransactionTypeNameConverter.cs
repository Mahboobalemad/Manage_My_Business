using System.Globalization;
using System.Windows.Data;
using ShopsManagement.Domain.Enums;

namespace ShopsManagement.UI.Converters;

public class EmployeeTransactionTypeNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EmployeeTransactionType type)
        {
            return type switch
            {
                EmployeeTransactionType.Salary => "راتب (+)",
                EmployeeTransactionType.Bonus => "مكافأة (+)",
                EmployeeTransactionType.Withdrawal => "سحبية (-)",
                EmployeeTransactionType.Deduction => "غياب / خصم (-)",
                EmployeeTransactionType.Payment => "دفعة ماليّة (-)",
                _ => value.ToString() ?? ""
            };
        }

        if (value is string str)
        {
            return str switch
            {
                "Salary" => "راتب (+)",
                "Bonus" => "مكافأة (+)",
                "Withdrawal" => "سحبية (-)",
                "Deduction" => "غياب / خصم (-)",
                "Payment" => "دفعة ماليّة (-)",
                _ => str
            };
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
