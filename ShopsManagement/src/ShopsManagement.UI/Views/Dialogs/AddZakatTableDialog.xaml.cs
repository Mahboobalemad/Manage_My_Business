using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddZakatTableDialog : Window
{
    private readonly ZakatTable? _existingTable;
    public ZakatTable? TableResult { get; private set; }

    public AddZakatTableDialog(ZakatTable? existingTable = null)
    {
        InitializeComponent();
        _existingTable = existingTable;

        DpDate.SelectedDate = DateTime.Today;

        if (_existingTable != null)
        {
            Title = "تعديل جدول الزكاة";
            TxtTitle.Text = "✏️ تعديل بيانات جدول الزكاة";
            BtnSave.Content = "حفظ التعديلات";

            DpDate.SelectedDate = _existingTable.Date.ToDateTime(TimeOnly.MinValue);
            TxtName.Text = _existingTable.Name;
            TxtNotes.Text = _existingTable.Notes ?? string.Empty;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم جدول الزكاة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TableResult = new ZakatTable
        {
            Id = _existingTable?.Id ?? 0,
            Date = DateOnly.FromDateTime(DpDate.SelectedDate ?? DateTime.Today),
            Name = TxtName.Text.Trim(),
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
