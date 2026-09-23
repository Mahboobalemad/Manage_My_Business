using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class DailyActivityViewModel : ViewModelBase
{
    private readonly IDailyActivityService _activityService;
    private readonly IBranchService _branchService;

    private List<Branch> _actualBranches = [];
    private Branch? _selectedBranch;
    private int _selectedYear = DateTime.Now.Year;
    private MonthTabItemViewModel? _selectedMonthTab;

    private decimal _monthlyTotalSales;
    private decimal _monthlyTotalExpenses;
    private decimal _monthlyNetSales;
    private decimal _monthlyTotalProfits;

    private DailyActivity? _selectedActivity;
    private string _statusMessage = string.Empty;

    public DailyActivityViewModel(IDailyActivityService activityService, IBranchService branchService)
    {
        _activityService = activityService;
        _branchService = branchService;

        Branches = new ObservableCollection<Branch>();
        Activities = new ObservableCollection<DailyActivity>();
        MonthTabs = new ObservableCollection<MonthTabItemViewModel>();

        InitializeMonthTabs();
        AvailableYears = new ObservableCollection<int> { DateTime.Now.Year - 1, DateTime.Now.Year, DateTime.Now.Year + 1 };

        OpenAddDialogCommand = new RelayCommand(async () => await OpenAddDialogAsync());
        EditRecordCommand = new RelayCommand<DailyActivity>(async (a) => await OpenEditDialogAsync(a));
        DeleteRecordCommand = new RelayCommand<DailyActivity>(async (a) => await DeleteRecordAsync(a));
        SelectMonthTabCommand = new RelayCommand<MonthTabItemViewModel>(async (tab) => await SelectMonthAsync(tab));
    }

    public ObservableCollection<Branch> Branches { get; }
    public ObservableCollection<DailyActivity> Activities { get; }
    public ObservableCollection<MonthTabItemViewModel> MonthTabs { get; }
    public ObservableCollection<int> AvailableYears { get; }

    public Branch? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (SetProperty(ref _selectedBranch, value))
            {
                _ = LoadActivitiesAsync();
            }
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                _ = LoadActivitiesAsync();
            }
        }
    }

    public MonthTabItemViewModel? SelectedMonthTab
    {
        get => _selectedMonthTab;
        set
        {
            if (SetProperty(ref _selectedMonthTab, value) && value != null)
            {
                foreach (var tab in MonthTabs)
                    tab.IsSelected = (tab.MonthNumber == value.MonthNumber);

                _ = LoadActivitiesAsync();
            }
        }
    }

    public decimal MonthlyTotalSales
    {
        get => _monthlyTotalSales;
        set => SetProperty(ref _monthlyTotalSales, value);
    }

    public decimal MonthlyTotalExpenses
    {
        get => _monthlyTotalExpenses;
        set => SetProperty(ref _monthlyTotalExpenses, value);
    }

    public decimal MonthlyNetSales
    {
        get => _monthlyNetSales;
        set => SetProperty(ref _monthlyNetSales, value);
    }

    public decimal MonthlyTotalProfits
    {
        get => _monthlyTotalProfits;
        set => SetProperty(ref _monthlyTotalProfits, value);
    }

    public DailyActivity? SelectedActivity
    {
        get => _selectedActivity;
        set => SetProperty(ref _selectedActivity, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddDialogCommand { get; }
    public ICommand EditRecordCommand { get; }
    public ICommand DeleteRecordCommand { get; }
    public ICommand SelectMonthTabCommand { get; }

    public async Task InitializeAsync()
    {
        _actualBranches = await _branchService.GetAllBranchesAsync();

        Branches.Clear();
        // Add "All Branches" option first
        Branches.Add(new Branch { Id = 0, Name = "جميع المحلات" });

        foreach (var b in _actualBranches)
            Branches.Add(b);

        if (Branches.Count > 0 && SelectedBranch == null)
            SelectedBranch = Branches[0];
    }

    private void InitializeMonthTabs()
    {
        string[] arabicMonths = ["يناير", "فبراير", "مارس", "إبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];
        MonthTabs.Clear();
        int currentMonth = DateTime.Now.Month;
        MonthTabItemViewModel? defaultTab = null;

        for (int i = 0; i < 12; i++)
        {
            var tab = new MonthTabItemViewModel(i + 1, arabicMonths[i]);
            if (i + 1 == currentMonth)
            {
                tab.IsSelected = true;
                defaultTab = tab;
            }
            MonthTabs.Add(tab);
        }
        _selectedMonthTab = defaultTab ?? MonthTabs[0];
    }

    public async Task LoadActivitiesAsync()
    {
        if (SelectedMonthTab == null) return;

        try
        {
            int? branchId = (SelectedBranch == null || SelectedBranch.Id == 0) ? null : SelectedBranch.Id;

            var list = await _activityService.GetActivitiesByBranchAndMonthAsync(branchId, SelectedYear, SelectedMonthTab.MonthNumber);
            Activities.Clear();
            foreach (var act in list)
                Activities.Add(act);

            var summary = await _activityService.GetMonthlySummaryAsync(branchId, SelectedYear, SelectedMonthTab.MonthNumber);
            MonthlyTotalSales = summary.TotalSales;
            MonthlyTotalExpenses = summary.TotalExpenses;
            MonthlyNetSales = summary.NetSales;
            MonthlyTotalProfits = summary.TotalProfits;

            string branchFilterText = branchId.HasValue ? SelectedBranch?.Name ?? "الفرع المحدد" : "جميع المحلات";
            StatusMessage = $"تم تحميل سجلات {branchFilterText} لشهر {SelectedMonthTab.MonthName} {SelectedYear} ({Activities.Count} يوم)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء تحميل اليوميات: {ex.Message}";
        }
    }

    private async Task SelectMonthAsync(MonthTabItemViewModel? tab)
    {
        if (tab == null) return;
        SelectedMonthTab = tab;
        await LoadActivitiesAsync();
    }

    private async Task OpenAddDialogAsync()
    {
        if (_actualBranches.Count == 0)
        {
            MessageBox.Show("لا يوجد فروع مسجلة، يرجى إضافة فرع أولاً من شاشة إدارة الفروع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int? preselectedBranchId = SelectedBranch != null && SelectedBranch.Id > 0 ? SelectedBranch.Id : null;

        var dialog = new AddDailyActivityDialog(_actualBranches, null, preselectedBranchId)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.ActivityResult != null)
        {
            var activity = dialog.ActivityResult;

            // Prevent recording more than one entry for the same branch on the same date
            var existing = await _activityService.GetActivityByBranchAndDateAsync(activity.BranchId, activity.Date);
            if (existing != null)
            {
                MessageBox.Show($"تنبيه: يوجد بالفعل قيد مبيعات مسجل لفرع ({existing.Branch?.Name ?? "المحدد"}) في تاريخ ({existing.Date:yyyy/MM/dd}).\n\nلا يمكن تسجيل أكثر من مبيعات لنفس اليوم للفرع الواحد.", "تكرار قيد اليومية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _activityService.SaveOrUpdateActivityAsync(activity);
                StatusMessage = $"تم إضافة مبيعات يوم {activity.Date:yyyy/MM/dd} بنجاح";
                await LoadActivitiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ المبيعات اليومية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task OpenEditDialogAsync(DailyActivity? activity)
    {
        if (activity == null) return;

        if (_actualBranches.Count == 0)
        {
            _actualBranches = await _branchService.GetAllBranchesAsync();
        }

        var dialog = new AddDailyActivityDialog(_actualBranches, activity)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.ActivityResult != null)
        {
            var updated = dialog.ActivityResult;

            // Check duplicate if branch or date changed
            var existing = await _activityService.GetActivityByBranchAndDateAsync(updated.BranchId, updated.Date);
            if (existing != null && existing.Id != updated.Id)
            {
                MessageBox.Show($"تنبيه: يوجد بالفعل قيد مبيعات آخر مسجل لفرع ({existing.Branch?.Name ?? "المحدد"}) في تاريخ ({existing.Date:yyyy/MM/dd}).\n\nلا يمكن تكرار تسجيل المبيعات لنفس اليوم.", "تكرار قيد اليومية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _activityService.SaveOrUpdateActivityAsync(updated);
                StatusMessage = $"تم تعديل بيانات مبيعات يوم {updated.Date:yyyy/MM/dd} بنجاح";
                await LoadActivitiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تعديل اليومية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task DeleteRecordAsync(DailyActivity? activity)
    {
        if (activity == null) return;

        string branchName = activity.Branch?.Name ?? "الفرع";
        var result = MessageBox.Show($"هل أنت تأكد من رغبتك في حذف قيد مبيعات يوم ({activity.Date:yyyy/MM/dd}) لفرع ({branchName})؟\n\nملاحظة: لا يمكن التراجع عن الحذف بعد تنفيذه.", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var ok = await _activityService.DeleteActivityAsync(activity.Id);
                if (ok)
                {
                    Activities.Remove(activity);
                    StatusMessage = $"تم حذف قيد مبيعات يوم {activity.Date:yyyy/MM/dd} بنجاح";
                    await LoadActivitiesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
