using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddTraderDialog : Window
{
    private readonly Trader? _existingTrader;
    public Trader? TraderResult { get; private set; }

    public AddTraderDialog(Trader? existingTrader = null)
    {
        InitializeComponent();
        _existingTrader = existingTrader;

        if (_existingTrader != null)
        {
            Title = "تعديل بيانات التاجر";
            TxtTitle.Text = "✏️ تعديل بيانات التاجر";
            BtnSave.Content = "حفظ التعديلات";

            TxtName.Text = _existingTrader.Name;
            TxtLocation.Text = _existingTrader.Location ?? string.Empty;
            TxtBusinessName.Text = _existingTrader.BusinessName ?? string.Empty;
            TxtPhone.Text = _existingTrader.Phone ?? string.Empty;
            TxtOpeningBalance.Text = _existingTrader.OpeningBalance.ToString("F2");
            ChkIsActive.IsChecked = _existingTrader.IsActive;
            TxtNotes.Text = _existingTrader.Notes ?? string.Empty;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم التاجر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtOpeningBalance.Text, out decimal openingBalance);

        TraderResult = new Trader
        {
            Id = _existingTrader?.Id ?? 0,
            Name = TxtName.Text.Trim(),
            Location = string.IsNullOrWhiteSpace(TxtLocation.Text) ? null : TxtLocation.Text.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(TxtBusinessName.Text) ? null : TxtBusinessName.Text.Trim(),
            Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim(),
            OpeningBalance = openingBalance,
            IsActive = ChkIsActive.IsChecked ?? true,
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
