using System.Windows;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddEmployeeDialog : Window
{
    public Employee? CreatedEmployee { get; private set; }

    public AddEmployeeDialog(List<Branch> branches)
    {
        InitializeComponent();

        CmbBranch.ItemsSource = branches;
        if (branches.Count > 0)
            CmbBranch.SelectedIndex = 0;

        DpHireDate.SelectedDate = DateTime.Today;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("يرجى إدخال اسم الموظف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbBranch.SelectedItem is not Branch selectedBranch)
        {
            MessageBox.Show("يرجى اختيار الفرع", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtSalary.Text, out var salary);
        decimal.TryParse(TxtOpeningBalance.Text, out var openingBalance);

        CreatedEmployee = new Employee
        {
            Name = TxtName.Text.Trim(),
            HireDate = DateOnly.FromDateTime(DpHireDate.SelectedDate ?? DateTime.Today),
            Salary = salary,
            OpeningBalance = openingBalance,
            Position = string.IsNullOrWhiteSpace(TxtPosition.Text) ? null : TxtPosition.Text.Trim(),
            BranchId = selectedBranch.Id,
            Status = EmployeeStatus.Active,
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
