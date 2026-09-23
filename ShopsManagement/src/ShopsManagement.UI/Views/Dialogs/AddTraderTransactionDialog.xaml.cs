using System.Windows;
using System.Windows.Controls;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddTraderTransactionDialog : Window
{
    private readonly TraderTransaction? _existingTx;
    public TraderTransaction? TransactionResult { get; private set; }

    public AddTraderTransactionDialog(TraderTransaction? existingTx = null)
    {
        InitializeComponent();
        _existingTx = existingTx;
        DpDate.SelectedDate = DateTime.Today;

        if (_existingTx != null)
        {
            Title = "تعديل حركة التاجر";
            TxtTitle.Text = "✏️ تعديل حركة حساب التاجر";
            BtnSave.Content = "حفظ التعديلات";

            DpDate.SelectedDate = _existingTx.Date.ToDateTime(TimeOnly.MinValue);
            TxtAmount.Text = _existingTx.Amount.ToString("F2");
            TxtNotes.Text = _existingTx.Notes ?? string.Empty;

            if (_existingTx.Type == TraderTransactionType.Invoice)
                CmbType.SelectedIndex = 0;
            else
                CmbType.SelectedIndex = 1;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من صفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedTag = (CmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var type = selectedTag == "Payment" ? TraderTransactionType.Payment : TraderTransactionType.Invoice;

        TransactionResult = new TraderTransaction
        {
            Id = _existingTx?.Id ?? 0,
            Date = DateOnly.FromDateTime(DpDate.SelectedDate ?? DateTime.Today),
            Type = type,
            Amount = amount,
            Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
