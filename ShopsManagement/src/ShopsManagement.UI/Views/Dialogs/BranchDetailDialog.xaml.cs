using System.Windows;
using System.Windows.Controls;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class BranchDetailDialog : Window
{
    private readonly int _branchId;
    private readonly IBranchService _branchService;
    private readonly IDailyActivityService _dailyActivityService;
    private readonly IInvoiceService _invoiceService;
    private readonly IEmployeeService _employeeService;

    private List<DailyActivity> _allSalesData = [];
    private List<Invoice> _allInvoicesData = [];
    private List<EmployeeSummaryDto> _allEmployeesData = [];

    private string _currentCategory = "Sales";
    private int? _selectedYear = null;
    private int? _selectedMonth = null;
    private bool _isInitialized = false;

    public BranchDetailDialog(
        int branchId,
        string branchName,
        string? address,
        string? recipientName,
        bool isActive,
        IBranchService branchService,
        IDailyActivityService dailyActivityService,
        IInvoiceService invoiceService,
        IEmployeeService employeeService)
    {
        InitializeComponent();
        _branchId = branchId;
        _branchService = branchService;
        _dailyActivityService = dailyActivityService;
        _invoiceService = invoiceService;
        _employeeService = employeeService;

        TxtBranchTitle.Text = $"🏪 {branchName}";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address)) parts.Add($"العنوان: {address}");
        if (!string.IsNullOrWhiteSpace(recipientName)) parts.Add($"المسؤول: {recipientName}");
        TxtBranchSubtitle.Text = string.Join("   |   ", parts);
        TxtBranchStatus.Text = isActive ? "✅ نشط" : "⛔ غير نشط";
        TxtBranchStatus.Foreground = isActive
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));

        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load all data from services
            _allSalesData = await _dailyActivityService.GetActivitiesByBranchAsync(_branchId, null, null) ?? [];

            var allInvoices = await _invoiceService.GetAllInvoicesAsync(branchId: _branchId);
            _allInvoicesData = allInvoices ?? [];

            var employees = await _employeeService.GetAllEmployeesAsync(branchId: _branchId) ?? [];
            _allEmployeesData = [];
            foreach (var emp in employees)
            {
                try
                {
                    var summary = await _employeeService.GetEmployeeSummaryAsync(emp.Id);
                    if (summary != null)
                        _allEmployeesData.Add(summary);
                }
                catch
                {
                    // Ignore error for specific employee to prevent crash
                }
            }

            // Populate Year filter from all data
            var years = new HashSet<int>();
            foreach (var s in _allSalesData) years.Add(s.Date.Year);
            foreach (var i in _allInvoicesData) years.Add(i.Date.Year);
            var sortedYears = years.OrderByDescending(y => y).ToList();

            CmbYear.Items.Clear();
            CmbYear.Items.Add(new ComboBoxItem { Content = "جميع السنوات", Tag = 0 });
            foreach (var yr in sortedYears)
                CmbYear.Items.Add(new ComboBoxItem { Content = yr.ToString(), Tag = yr });
            CmbYear.SelectedIndex = 0;

            // Populate Month filter
            CmbMonth.Items.Clear();
            CmbMonth.Items.Add(new ComboBoxItem { Content = "جميع الأشهر", Tag = 0 });
            string[] monthNames = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                                    "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];
            for (int m = 1; m <= 12; m++)
                CmbMonth.Items.Add(new ComboBoxItem { Content = monthNames[m - 1], Tag = m });
            CmbMonth.SelectedIndex = 0;

            _isInitialized = true;
            RefreshView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء تحميل بيانات الفرع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();
    private void CmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();
    private void CmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();

    private void RefreshView()
    {
        if (!_isInitialized) return;
        if (CmbCategory.SelectedItem is not ComboBoxItem catItem) return;
        _currentCategory = catItem.Tag?.ToString() ?? "Sales";

        _selectedYear = CmbYear.SelectedItem is ComboBoxItem yearItem
                        ? (int)yearItem.Tag == 0 ? null : (int?)((int)yearItem.Tag)
                        : null;

        _selectedMonth = CmbMonth.SelectedItem is ComboBoxItem monthItem
                         ? (int)monthItem.Tag == 0 ? null : (int?)((int)monthItem.Tag)
                         : null;

        // Show/hide grids and summaries based on category
        switch (_currentCategory)
        {
            case "Sales":
                ShowSalesView();
                break;
            case "Invoices":
                ShowInvoicesView();
                break;
            case "Employees":
                ShowEmployeesView();
                break;
        }
    }

    private void ShowSalesView()
    {
        GridSalesSummary.Visibility = Visibility.Visible;
        BorderInvoiceTotal.Visibility = Visibility.Collapsed;

        BorderSalesGrid.Visibility = Visibility.Visible;
        BorderInvoicesGrid.Visibility = Visibility.Collapsed;
        BorderEmployeesGrid.Visibility = Visibility.Collapsed;

        var filtered = _allSalesData.AsEnumerable();
        if (_selectedYear.HasValue) filtered = filtered.Where(s => s.Date.Year == _selectedYear.Value);
        if (_selectedMonth.HasValue) filtered = filtered.Where(s => s.Date.Month == _selectedMonth.Value);

        var list = filtered.OrderByDescending(s => s.Date).ToList();
        GridSales.ItemsSource = list;

        TxtTotalSales.Text = $"{list.Sum(s => s.TotalSales):N2} ر.ي";
        TxtTotalExpenses.Text = $"{list.Sum(s => s.TotalExpenses):N2} ر.ي";
        TxtNetSales.Text = $"{list.Sum(s => s.NetSales):N2} ر.ي";
        TxtNetProfits.Text = $"{list.Sum(s => s.DisplayProfit):N2} ر.ي";
    }

    private void ShowInvoicesView()
    {
        GridSalesSummary.Visibility = Visibility.Collapsed;
        BorderInvoiceTotal.Visibility = Visibility.Visible;

        BorderSalesGrid.Visibility = Visibility.Collapsed;
        BorderInvoicesGrid.Visibility = Visibility.Visible;
        BorderEmployeesGrid.Visibility = Visibility.Collapsed;

        var filtered = _allInvoicesData.AsEnumerable();
        if (_selectedYear.HasValue) filtered = filtered.Where(i => i.Date.Year == _selectedYear.Value);
        if (_selectedMonth.HasValue) filtered = filtered.Where(i => i.Date.Month == _selectedMonth.Value);

        var list = filtered.OrderByDescending(i => i.Date).ToList();
        GridInvoices.ItemsSource = list;

        TxtTotalInvoices.Text = $"{list.Sum(i => i.Amount):N2} ر.ي";
    }

    private void ShowEmployeesView()
    {
        GridSalesSummary.Visibility = Visibility.Collapsed;
        BorderInvoiceTotal.Visibility = Visibility.Collapsed;

        BorderSalesGrid.Visibility = Visibility.Collapsed;
        BorderInvoicesGrid.Visibility = Visibility.Collapsed;
        BorderEmployeesGrid.Visibility = Visibility.Visible;

        // Employees don't have year/month filters, just show all
        GridEmployees.ItemsSource = _allEmployeesData;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
