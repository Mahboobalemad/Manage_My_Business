using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly IReportExportService _exportService;
    private readonly IBranchService _branchService;
    private readonly ITraderService _traderService;
    private readonly IEmployeeService _employeeService;

    // Report Kind: DailySales, Invoices, Trader, Employee, Branch
    private string _selectedReportKind = string.Empty;

    // ── Single-branch for Trader/Employee/Branch reports
    private Branch?   _selectedBranch;
    private Trader?   _selectedTrader;
    private Employee? _selectedEmployee;

    // ── Date filter
    private string    _dateFilterMode  = "All"; // All | Year | Month | SpecificDay
    private int       _selectedYear    = DateTime.Now.Year;
    private int       _selectedMonth   = DateTime.Now.Month;
    private DateTime? _selectedDate    = DateTime.Today;

    private string _statusMessage = string.Empty;

    public ReportsViewModel(
        IReportExportService exportService,
        IBranchService branchService,
        ITraderService traderService,
        IEmployeeService employeeService)
    {
        _exportService    = exportService;
        _branchService    = branchService;
        _traderService    = traderService;
        _employeeService  = employeeService;

        // Collections
        Branches          = new ObservableCollection<Branch>();
        CheckableBranches = new ObservableCollection<CheckableBranch>();
        Traders           = new ObservableCollection<Trader>();
        Employees         = new ObservableCollection<Employee>();

        AvailableYears = new ObservableCollection<int>();
        for (int y = DateTime.Now.Year + 1; y >= DateTime.Now.Year - 5; y--)
            AvailableYears.Add(y);

        AvailableMonths = new ObservableCollection<int> { 1,2,3,4,5,6,7,8,9,10,11,12 };

        SelectReportKindCommand = new RelayCommand<string>(SelectReportKind);
        PrintReportCommand      = new RelayCommand(async () => await PrintReportAsync());
    }

    // ── Collections exposed to view ─────────────────────────────────
    /// <summary>All branches with checkboxes (for DailySales / Invoices)</summary>
    public ObservableCollection<CheckableBranch> CheckableBranches { get; }
    /// <summary>All branches flat list (for Branch/Employee single-select)</summary>
    public ObservableCollection<Branch>   Branches  { get; }
    public ObservableCollection<Trader>   Traders   { get; }
    public ObservableCollection<Employee> Employees { get; }

    public ObservableCollection<int> AvailableYears  { get; }
    public ObservableCollection<int> AvailableMonths { get; }

    // ── Report kind ─────────────────────────────────────────────────
    public string SelectedReportKind
    {
        get => _selectedReportKind;
        set
        {
            if (SetProperty(ref _selectedReportKind, value))
            {
                OnPropertyChanged(nameof(ShowDailySalesFilters));
                OnPropertyChanged(nameof(ShowInvoicesFilters));
                OnPropertyChanged(nameof(ShowTraderFilters));
                OnPropertyChanged(nameof(ShowEmployeeFilters));
                OnPropertyChanged(nameof(ShowBranchFilters));
                OnPropertyChanged(nameof(ShowFiltersArea));
                OnPropertyChanged(nameof(ShowNoSelectionHint));

                // Reset date mode when switching
                DateFilterMode = "All";
            }
        }
    }

    // Visibility flags per report kind
    public bool ShowDailySalesFilters => SelectedReportKind == "DailySales";
    public bool ShowInvoicesFilters   => SelectedReportKind == "Invoices";
    public bool ShowTraderFilters     => SelectedReportKind == "Trader";
    public bool ShowEmployeeFilters   => SelectedReportKind == "Employee";
    public bool ShowBranchFilters     => SelectedReportKind == "Branch";
    public bool ShowFiltersArea       => !string.IsNullOrEmpty(SelectedReportKind);
    public bool ShowNoSelectionHint   => string.IsNullOrEmpty(SelectedReportKind);


    // ── Entity selectors ────────────────────────────────────────────
    public Branch? SelectedBranch
    {
        get => _selectedBranch;
        set => SetProperty(ref _selectedBranch, value);
    }

    public Trader? SelectedTrader
    {
        get => _selectedTrader;
        set => SetProperty(ref _selectedTrader, value);
    }

    public Employee? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetProperty(ref _selectedEmployee, value);
    }

    // ── Date filter ─────────────────────────────────────────────────
    public string DateFilterMode
    {
        get => _dateFilterMode;
        set
        {
            if (SetProperty(ref _dateFilterMode, value))
            {
                OnPropertyChanged(nameof(IsYearFilterVisible));
                OnPropertyChanged(nameof(IsMonthFilterVisible));
                OnPropertyChanged(nameof(IsDayFilterVisible));
            }
        }
    }

    public bool IsYearFilterVisible  => DateFilterMode == "Year"  || DateFilterMode == "Month";
    public bool IsMonthFilterVisible => DateFilterMode == "Month";
    public bool IsDayFilterVisible   => DateFilterMode == "SpecificDay";

    public int SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public int SelectedMonth
    {
        get => _selectedMonth;
        set => SetProperty(ref _selectedMonth, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ── Commands ────────────────────────────────────────────────────
    public ICommand SelectReportKindCommand { get; }
    public ICommand PrintReportCommand      { get; }

    // ── Initialization ──────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        try
        {
            var branchList = await _branchService.GetAllBranchesAsync();

            // For single-select (Branch reports)
            Branches.Clear();
            foreach (var b in branchList) Branches.Add(b);
            if (Branches.Count > 0) SelectedBranch = Branches[0];

            // For multi-select checkboxes
            CheckableBranches.Clear();
            foreach (var b in branchList)
                CheckableBranches.Add(new CheckableBranch { Branch = b });

            var traderList = await _traderService.GetAllTradersAsync();
            Traders.Clear();
            foreach (var t in traderList) Traders.Add(t);
            if (Traders.Count > 0) SelectedTrader = Traders[0];

            var empList = await _employeeService.GetAllEmployeesAsync();
            Employees.Clear();
            foreach (var e in empList) Employees.Add(e);
            if (Employees.Count > 0) SelectedEmployee = Employees[0];
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء تحميل البيانات: {ex.Message}";
        }
    }

    // ── Private helpers ─────────────────────────────────────────────
    private void SelectReportKind(string? kind)
    {
        if (!string.IsNullOrEmpty(kind))
            SelectedReportKind = kind;
    }

    private (int? singleBranchId, List<int> branchIds, int? traderId, int? empId,
             int? year, int? month, DateOnly? date) GetFilterParams()
    {
        // Multi-branch from checkboxes
        var checkedBranchIds = CheckableBranches
            .Where(cb => cb.IsChecked)
            .Select(cb => cb.Branch.Id)
            .ToList();

        int? singleBranchId = SelectedBranch?.Id > 0 ? SelectedBranch.Id : null;
        int? traderId  = SelectedTrader?.Id  > 0 ? SelectedTrader.Id  : null;
        int? empId     = SelectedEmployee?.Id;

        int? year  = null;
        int? month = null;
        DateOnly? date = null;

        switch (DateFilterMode)
        {
            case "Year":        year = SelectedYear; break;
            case "Month":       year = SelectedYear; month = SelectedMonth; break;
            case "SpecificDay" when SelectedDate.HasValue:
                date = DateOnly.FromDateTime(SelectedDate.Value); break;
        }

        return (singleBranchId, checkedBranchIds, traderId, empId, year, month, date);
    }

    private async Task PrintReportAsync()
    {
        if (string.IsNullOrEmpty(SelectedReportKind))
        {
            StatusMessage = "⚠️ يرجى اختيار نوع التقرير أولاً";
            return;
        }

        string tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"Report_{SelectedReportKind}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        try
        {
            StatusMessage = "⏳ جارٍ تجهيز التقرير...";
            var p = GetFilterParams();

            // Use first checked branch or null for "all"
            int? branchId = p.branchIds.Count == 1 ? p.branchIds[0] : (p.branchIds.Count == 0 ? null : p.branchIds[0]);

            switch (SelectedReportKind)
            {
                case "DailySales":
                    await _exportService.ExportDailyActivitiesToPdfAsync(
                        branchId, p.year, p.month, p.date, tempPath);
                    break;

                case "Invoices":
                    await _exportService.ExportInvoicesToPdfAsync(
                        branchId, p.traderId, p.year, p.month, p.date, null, null, tempPath);
                    break;

                case "Trader":
                    if (SelectedTrader == null)
                    {
                        StatusMessage = "⚠️ يرجى اختيار التاجر أولاً";
                        return;
                    }
                    await _exportService.ExportTraderStatementToPdfAsync(
                        SelectedTrader.Id, p.year, p.month, p.date, null, null, tempPath);
                    break;

                case "Employee":
                    if (SelectedEmployee == null)
                    {
                        StatusMessage = "⚠️ يرجى اختيار الموظف أولاً";
                        return;
                    }
                    await _exportService.ExportEmployeePayrollToPdfAsync(
                        SelectedEmployee.Id, p.year, p.month, p.date, null, null, tempPath);
                    break;

                case "Branch":
                    if (SelectedBranch == null)
                    {
                        StatusMessage = "⚠️ يرجى اختيار الفرع أولاً";
                        return;
                    }
                    await _exportService.ExportBranchStatementToPdfAsync(
                        SelectedBranch.Id, p.year, p.month, p.date, null, null, tempPath);
                    break;
            }

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });

            StatusMessage = "✅ تم فتح التقرير للطباعة والمعاينة";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء تجهيز التقرير: {ex.Message}";
        }
    }
}

/// <summary>Wrapper for Branch enabling checkbox selection in multi-branch lists.</summary>
public class CheckableBranch : ViewModelBase
{
    private bool _isChecked;

    public Branch Branch { get; set; } = null!;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    public string Name => Branch?.Name ?? string.Empty;
}
