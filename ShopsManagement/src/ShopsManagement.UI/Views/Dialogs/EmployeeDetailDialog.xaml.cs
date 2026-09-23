using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class EmployeeDetailDialog : Window
{
    private readonly int _employeeId;
    private readonly IEmployeeService _employeeService;
    private EmployeeSummaryDto? _summary;
    private List<EmployeeTransaction> _allTransactions = [];

    public EmployeeDetailDialog(int employeeId, IEmployeeService employeeService)
    {
        InitializeComponent();
        _employeeId = employeeId;
        _employeeService = employeeService;

        SetupFilterDropdowns();
        Loaded += async (s, e) => await LoadDataAsync();
    }

    private void SetupFilterDropdowns()
    {
        // Years
        var years = new List<string> { "الكل" };
        var currentYear = DateTime.Now.Year;
        for (int y = currentYear - 2; y <= currentYear + 1; y++)
            years.Add(y.ToString());
        CmbFilterYear.ItemsSource = years;
        CmbFilterYear.SelectedIndex = 0;

        // Months
        var months = new List<string> { "الكل", "1 - يناير", "2 - فبراير", "3 - مارس", "4 - أبريل", "5 - مايو", "6 - يونيو", "7 - يوليو", "8 - أغسطس", "9 - سبتمبر", "10 - أكتوبر", "11 - نوفمبر", "12 - ديسمبر" };
        CmbFilterMonth.ItemsSource = months;
        CmbFilterMonth.SelectedIndex = 0;

        // Types
        var types = new List<string> { "الكل", "سحبية", "راتب", "غياب / خصم", "مكافأة", "دفعة" };
        CmbFilterType.ItemsSource = types;
        CmbFilterType.SelectedIndex = 0;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _summary = await _employeeService.GetEmployeeSummaryAsync(_employeeId);
            _allTransactions = await _employeeService.GetEmployeeTransactionsAsync(_employeeId);

            TxtEmployeeName.Text = _summary.EmployeeName;
            TxtPosition.Text = string.IsNullOrWhiteSpace(_summary.Position) ? "" : $"({_summary.Position})";
            TxtSubInfo.Text = $"الفرع: {_summary.BranchName} | الراتب الشهري: {_summary.BaseSalary:N2} ر.ي | الرصيد الافتتاحي: {_summary.OpeningBalance:N2} ر.ي";

            TxtNetBalance.Text = _summary.NetEntitlementDisplayText;
            TxtNetBalance.Foreground = _summary.NetEntitlement > 0 
                ? new SolidColorBrush(Color.FromRgb(5, 150, 105)) 
                : _summary.NetEntitlement < 0 
                    ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) 
                    : new SolidColorBrush(Color.FromRgb(15, 23, 42));

            ApplyFilters();
            TxtStatus.Text = $"تم جلب كشف الحساب بنجاح ({_allTransactions.Count} حركة)";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"خطأ أثناء تحميل البيانات: {ex.Message}";
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allTransactions.AsEnumerable();

        // Filter Year
        if (CmbFilterYear.SelectedIndex > 0 && int.TryParse(CmbFilterYear.SelectedItem?.ToString(), out var year))
        {
            filtered = filtered.Where(t => t.Date.Year == year);
        }

        // Filter Month
        if (CmbFilterMonth.SelectedIndex > 0)
        {
            var month = CmbFilterMonth.SelectedIndex; // 1..12
            filtered = filtered.Where(t => t.Date.Month == month);
        }

        // Filter Type
        if (CmbFilterType.SelectedIndex > 0)
        {
            var selectedTypeStr = CmbFilterType.SelectedItem?.ToString();
            switch (selectedTypeStr)
            {
                case "سحبية":
                    filtered = filtered.Where(t => t.Type == EmployeeTransactionType.Withdrawal);
                    break;
                case "راتب":
                    filtered = filtered.Where(t => t.Type == EmployeeTransactionType.Salary);
                    break;
                case "غياب / خصم":
                    filtered = filtered.Where(t => t.Type == EmployeeTransactionType.Deduction);
                    break;
                case "مكافأة":
                    filtered = filtered.Where(t => t.Type == EmployeeTransactionType.Bonus);
                    break;
                case "دفعة":
                    filtered = filtered.Where(t => t.Type == EmployeeTransactionType.Payment);
                    break;
            }
        }

        DgTransactions.ItemsSource = filtered.ToList();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private async void BtnAddTransaction_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddTransactionDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _employeeService.AddTransactionAsync(_employeeId, dialog.SelectedType, dialog.Amount, dialog.TransactionDate, dialog.Notes);
                await LoadDataAsync();
                TxtStatus.Text = "تم إضافة الحركة الماليّة بنجاح";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء إضافة الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnEditTx_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is EmployeeTransaction tx)
        {
            var dialog = new AddTransactionDialog { Owner = this };
            dialog.TransactionDate = tx.Date.ToDateTime(TimeOnly.MinValue);

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _employeeService.DeleteTransactionAsync(tx.Id);
                    await _employeeService.AddTransactionAsync(_employeeId, dialog.SelectedType, dialog.Amount, dialog.TransactionDate, dialog.Notes);
                    await LoadDataAsync();
                    TxtStatus.Text = "تم تعديل الحركة المالية بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void BtnDeleteTx_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is EmployeeTransaction tx)
        {
            var confirm = MessageBox.Show(
                $"هل أنت أصلًا متأكد من حذف هذه الحركة الماليّة ({tx.Type} بقيمة {tx.Amount:N2} ر.ي بتاريخ {tx.Date:yyyy/MM/dd})؟\nلا يمكن التراجع عن هذا الإجراء!",
                "تأكيد التحذير الحساس",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _employeeService.DeleteTransactionAsync(tx.Id);
                    await LoadDataAsync();
                    TxtStatus.Text = "تم حذف الحركة الماليّة بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
