using System.Collections.ObjectModel;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IBranchService _branchService;

    private Branch? _selectedBranch;
    private DashboardSummaryDto? _summary;
    private string _statusMessage = string.Empty;

    public DashboardViewModel(IDashboardService dashboardService, IBranchService branchService)
    {
        _dashboardService = dashboardService;
        _branchService = branchService;

        Branches = new ObservableCollection<Branch>();
        RefreshCommand = new RelayCommand(async () => await LoadDashboardAsync());
    }

    public ObservableCollection<Branch> Branches { get; }

    public Branch? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (SetProperty(ref _selectedBranch, value))
            {
                _ = LoadDashboardAsync();
            }
        }
    }

    public DashboardSummaryDto? Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        var list = await _branchService.GetAllBranchesAsync();
        Branches.Clear();
        
        // Add "All" option for filtering all shops
        Branches.Add(new Branch { Id = 0, Name = "الكل (جميع المحلات)" });

        foreach (var b in list)
            Branches.Add(b);

        SelectedBranch = Branches[0];
        await LoadDashboardAsync();
    }

    public async Task LoadDashboardAsync()
    {
        try
        {
            int? branchId = (SelectedBranch != null && SelectedBranch.Id > 0) ? SelectedBranch.Id : null;
            Summary = await _dashboardService.GetDashboardSummaryAsync(branchId);

            string filterName = branchId.HasValue ? SelectedBranch?.Name ?? "الفرع المحدد" : "جميع المحلات";
            StatusMessage = $"تم تحديث إحصائيات ({filterName}) بنجاح ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء تحميل مؤشرات لوحة التحكم: {ex.Message}";
        }
    }
}
