using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddZakatEntryDialog : Window
{
    private readonly ZakatEntry? _existingEntry;
    public ZakatEntry? EntryResult { get; private set; }

    public AddZakatEntryDialog(ZakatEntry? existingEntry = null)
    {
        InitializeComponent();
        _existingEntry = existingEntry;
        DpDate.SelectedDate = DateTime.Today;

        if (_existingEntry != null)
        {
            Title = "تعديل بند الزكاة المنفقة";
            TxtTitle.Text = "✏️ تعديل بند الزكاة المنفقة";
            BtnSave.Content = "حفظ التعديلات";

            DpDate.SelectedDate = _existingEntry.Date.ToDateTime(TimeOnly.MinValue);
            TxtAmount.Text = _existingEntry.Amount.ToString("F2");
            CmbZakatType.Text = _existingEntry.ZakatType;
            TxtNotes.Text = _existingEntry.Notes ?? string.Empty;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CmbZakatType.Text))
        {
            MessageBox.Show("يرجى إدخال نوع الزكاة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Amount is optional and can be zero
        decimal amount = 0m;
        if (!string.IsNullOrWhiteSpace(TxtAmount.Text))
        {
            if (!decimal.TryParse(TxtAmount.Text, out amount) || amount < 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ زكاة صحيح (يمكن أن يكون 0 أو أكبر).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        EntryResult = new ZakatEntry
        {
            Id = _existingEntry?.Id ?? 0,
            ZakatTableId = _existingEntry?.ZakatTableId,
            Date = DateOnly.FromDateTime(DpDate.SelectedDate ?? DateTime.Today),
            Amount = amount,
            ZakatType = CmbZakatType.Text.Trim(),
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
