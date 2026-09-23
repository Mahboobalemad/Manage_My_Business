using System.Windows.Input;

namespace ShopsManagement.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentView;

    public MainWindowViewModel(
        DashboardViewModel dashboardVm,
        BranchesViewModel branchesVm, 
        DailyActivityViewModel dailyActivityVm,
        InvoicesViewModel invoicesVm,
        TradersViewModel tradersVm,
        EmployeesViewModel employeesVm,
        ZakatViewModel zakatVm,
        ReportsViewModel reportsVm,
        BackupViewModel backupVm,
        SettingsViewModel settingsVm)
    {
        DashboardVm    = dashboardVm;
        BranchesVm     = branchesVm;
        DailyActivityVm = dailyActivityVm;
        InvoicesVm     = invoicesVm;
        TradersVm      = tradersVm;
        EmployeesVm    = employeesVm;
        ZakatVm        = zakatVm;
        ReportsVm      = reportsVm;
        BackupVm       = backupVm;
        SettingsVm     = settingsVm;

        _currentView = DashboardVm;

        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    public DashboardViewModel    DashboardVm     { get; }
    public BranchesViewModel     BranchesVm      { get; }
    public DailyActivityViewModel DailyActivityVm { get; }
    public InvoicesViewModel     InvoicesVm      { get; }
    public TradersViewModel      TradersVm       { get; }
    public EmployeesViewModel    EmployeesVm     { get; }
    public ZakatViewModel        ZakatVm         { get; }
    public ReportsViewModel      ReportsVm       { get; }
    public BackupViewModel       BackupVm        { get; }
    public SettingsViewModel     SettingsVm      { get; }

    public ViewModelBase CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateCommand { get; }

    public async Task InitializeAsync()
    {
        await DashboardVm.InitializeAsync();
        await BranchesVm.LoadBranchesAsync();
        await DailyActivityVm.InitializeAsync();
        await InvoicesVm.InitializeAsync();
        await TradersVm.InitializeAsync();
        await EmployeesVm.InitializeAsync();
        await ZakatVm.InitializeAsync();
        await ReportsVm.InitializeAsync();
        await BackupVm.InitializeAsync();
        await SettingsVm.InitializeAsync();
    }

    private void Navigate(string? destination)
    {
        switch (destination)
        {
            case "Dashboard":
                CurrentView = DashboardVm;
                _ = DashboardVm.LoadDashboardAsync();
                break;
            case "Branches":
                CurrentView = BranchesVm;
                _ = BranchesVm.LoadBranchesAsync();
                break;
            case "DailyActivity":
                CurrentView = DailyActivityVm;
                _ = DailyActivityVm.InitializeAsync();
                break;
            case "Invoices":
                CurrentView = InvoicesVm;
                _ = InvoicesVm.InitializeAsync();
                break;
            case "Traders":
                CurrentView = TradersVm;
                _ = TradersVm.InitializeAsync();
                break;
            case "Employees":
                CurrentView = EmployeesVm;
                _ = EmployeesVm.InitializeAsync();
                break;
            case "Zakat":
                CurrentView = ZakatVm;
                _ = ZakatVm.InitializeAsync();
                break;
            case "Reports":
                CurrentView = ReportsVm;
                _ = ReportsVm.InitializeAsync();
                break;
            case "Backup":
                CurrentView = BackupVm;
                _ = BackupVm.InitializeAsync();
                break;
            case "Settings":
                CurrentView = SettingsVm;
                _ = SettingsVm.InitializeAsync();
                break;
            default:
                CurrentView = DashboardVm;
                break;
        }
    }
}
