using System.Windows;
using ShopsManagement.Domain.Enums;

namespace ShopsManagement.UI.Views.Dialogs;

public class TransactionTypeDisplayItem
{
    public EmployeeTransactionType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}

public partial class AddTransactionDialog : Window
{
    public EmployeeTransactionType SelectedType { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }

    public AddTransactionDialog()
    {
        InitializeComponent();

        var types = new List<TransactionTypeDisplayItem>
        {
            new() { Type = EmployeeTransactionType.Withdrawal, DisplayName = "سحبية (-)" },
            new() { Type = EmployeeTransactionType.Salary, DisplayName = "راتب (+)" },
            new() { Type = EmployeeTransactionType.Deduction, DisplayName = "غياب / خصم (-)" },
            new() { Type = EmployeeTransactionType.Bonus, DisplayName = "مكافأة (+)" },
            new() { Type = EmployeeTransactionType.Payment, DisplayName = "دفعة ماليّة (-)" }
        };

        CmbType.ItemsSource = types;
        CmbType.SelectedIndex = 0;
        DpDate.SelectedDate = DateTime.Today;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbType.SelectedItem is not TransactionTypeDisplayItem selected)
        {
            MessageBox.Show("يرجى اختيار نوع الحركة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtAmount.Text, out var amt) || amt <= 0)
        {
            MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedType = selected.Type;
        Amount = amt;
        TransactionDate = DpDate.SelectedDate ?? DateTime.Today;
        Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
