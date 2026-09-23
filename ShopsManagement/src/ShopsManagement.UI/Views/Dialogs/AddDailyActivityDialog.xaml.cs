using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class AddDailyActivityDialog : Window
{
    private readonly DailyActivity? _existingActivity;

    public DailyActivity? ActivityResult { get; private set; }

    public AddDailyActivityDialog(List<Branch> branches, DailyActivity? existingActivity = null, int? preselectedBranchId = null)
    {
        InitializeComponent();
        _existingActivity = existingActivity;

        CmbBranch.ItemsSource = branches;

        if (_existingActivity != null)
        {
            Title = "تعديل المبيعات اليومية";
            TxtTitle.Text = "✏️ تعديل بيانات المبيعات اليومية";
            BtnSave.Content = "حفظ التعديلات";

            CmbBranch.SelectedItem = branches.FirstOrDefault(b => b.Id == _existingActivity.BranchId);
            DpDate.SelectedDate = _existingActivity.Date.ToDateTime(TimeOnly.MinValue);
            TxtTotalSales.Text = _existingActivity.TotalSales.ToString("F2");
            TxtTotalExpenses.Text = _existingActivity.TotalExpenses.ToString("F2");
            TxtNetProfit.Text = _existingActivity.NetProfit.HasValue ? _existingActivity.NetProfit.Value.ToString("F2") : string.Empty;
            TxtNotes.Text = _existingActivity.Notes ?? string.Empty;
        }
        else
        {
            Title = "إضافة مبيعات يومية";
            TxtTitle.Text = "📊 إضافة مبيعات يومية جديدة";
            BtnSave.Content = "إضافة إلى القائمة";

            if (preselectedBranchId.HasValue && preselectedBranchId.Value > 0)
            {
                CmbBranch.SelectedItem = branches.FirstOrDefault(b => b.Id == preselectedBranchId.Value);
            }

            if (CmbBranch.SelectedItem == null && branches.Count > 0)
            {
                CmbBranch.SelectedIndex = 0;
            }

            DpDate.SelectedDate = DateTime.Today;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CmbBranch.SelectedItem is not Branch selectedBranch)
        {
            MessageBox.Show("يرجى اختيار الفرع أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DpDate.SelectedDate.HasValue)
        {
            MessageBox.Show("يرجى تحديد تاريخ الحركة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtTotalSales.Text, out decimal totalSales) || totalSales < 0)
        {
            MessageBox.Show("يرجى إدخال إجمالي مبيعات صحيح (رقم أكبر من أو يساوي صفر).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtTotalExpenses.Text, out decimal totalExpenses) || totalExpenses < 0)
        {
            MessageBox.Show("يرجى إدخال إجمالي مصروفات صحيح (رقم أكبر من أو يساوي صفر).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal? netProfit = null;
        if (!string.IsNullOrWhiteSpace(TxtNetProfit.Text))
        {
            if (decimal.TryParse(TxtNetProfit.Text, out decimal profitVal))
            {
                netProfit = profitVal;
            }
            else
            {
                MessageBox.Show("قيمة صافي الأرباح غير صحيحة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        ActivityResult = new DailyActivity
        {
            Id = _existingActivity?.Id ?? 0,
            BranchId = selectedBranch.Id,
            Date = DateOnly.FromDateTime(DpDate.SelectedDate.Value),
            TotalSales = totalSales,
            TotalExpenses = totalExpenses,
            NetProfit = netProfit,
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
