using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddBranchDialog : Window
{
    private readonly Branch? _existingBranch;
    public Branch? BranchResult { get; private set; }

    public AddBranchDialog(Branch? existingBranch = null)
    {
        InitializeComponent();
        _existingBranch = existingBranch;

        if (_existingBranch != null)
        {
            Title = "تعديل بيانات الفرع";
            TxtTitle.Text = "✏️ تعديل بيانات الفرع التجاري";
            BtnSave.Content = "حفظ التعديلات";

            TxtName.Text = _existingBranch.Name;
            TxtAddress.Text = _existingBranch.Address ?? string.Empty;
            TxtRecipientName.Text = _existingBranch.RecipientName ?? string.Empty;
            TxtPhone.Text = _existingBranch.Phone ?? string.Empty;
            TxtOpeningBalance.Text = _existingBranch.OpeningBalance.ToString("F2");
            ChkIsActive.IsChecked = _existingBranch.IsActive;
            TxtNotes.Text = _existingBranch.Notes ?? string.Empty;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم الفرع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtOpeningBalance.Text, out decimal openingBalance);

        BranchResult = new Branch
        {
            Id = _existingBranch?.Id ?? 0,
            Name = TxtName.Text.Trim(),
            Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim(),
            RecipientName = string.IsNullOrWhiteSpace(TxtRecipientName.Text) ? null : TxtRecipientName.Text.Trim(),
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
