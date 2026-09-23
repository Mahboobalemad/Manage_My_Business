using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class EmployeesViewModel : ViewModelBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IBranchService _branchService;

    private EmployeeSummaryDto? _selectedEmployeeSummary;

    // Filters
    private Branch? _selectedFilterBranch;
    private EmployeeStatus? _selectedFilterStatus;

    private string _statusMessage = string.Empty;

    public EmployeesViewModel(IEmployeeService employeeService, IBranchService branchService)
    {
        _employeeService = employeeService;
        _branchService = branchService;

        EmployeesSummaries = new ObservableCollection<EmployeeSummaryDto>();
        Branches = new ObservableCollection<Branch>();

        Statuses = new ObservableCollection<EmployeeStatus>
        {
            EmployeeStatus.Active,
            EmployeeStatus.Inactive
        };

        OpenAddEmployeeDialogCommand = new RelayCommand(async () => await OpenAddEmployeeDialogAsync());
        OpenEmployeeDetailDialogCommand = new RelayCommand<EmployeeSummaryDto>(async (emp) => await OpenEmployeeDetailDialogAsync(emp));
        EditEmployeeCommand = new RelayCommand<EmployeeSummaryDto>(async (emp) => await EditEmployeeAsync(emp));
        DeleteEmployeeCommand = new RelayCommand<EmployeeSummaryDto>(async (emp) => await DeleteEmployeeAsync(emp));
    }

    public ObservableCollection<EmployeeSummaryDto> EmployeesSummaries { get; }
    public ObservableCollection<Branch> Branches { get; }
    public ObservableCollection<EmployeeStatus> Statuses { get; }

    public Branch? SelectedFilterBranch
    {
        get => _selectedFilterBranch;
        set
        {
            if (SetProperty(ref _selectedFilterBranch, value))
                _ = LoadEmployeesAsync();
        }
    }

    public EmployeeStatus? SelectedFilterStatus
    {
        get => _selectedFilterStatus;
        set
        {
            if (SetProperty(ref _selectedFilterStatus, value))
                _ = LoadEmployeesAsync();
        }
    }

    public EmployeeSummaryDto? SelectedEmployeeSummary
    {
        get => _selectedEmployeeSummary;
        set => SetProperty(ref _selectedEmployeeSummary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddEmployeeDialogCommand { get; }
    public ICommand OpenEmployeeDetailDialogCommand { get; }
    public ICommand EditEmployeeCommand { get; }
    public ICommand DeleteEmployeeCommand { get; }

    public async Task InitializeAsync()
    {
        var branchList = await _branchService.GetAllBranchesAsync();
        Branches.Clear();

        // Add "All" option for filtering all shops/branches
        Branches.Add(new Branch { Id = 0, Name = "الكل (جميع المحلات)" });

        foreach (var b in branchList)
            Branches.Add(b);

        SelectedFilterBranch = Branches[0];
        await LoadEmployeesAsync();
    }

    public async Task LoadEmployeesAsync()
    {
        try
        {
            int? branchId = SelectedFilterBranch?.Id > 0 ? SelectedFilterBranch.Id : null;
            var list = await _employeeService.GetAllEmployeesAsync(branchId, SelectedFilterStatus);

            EmployeesSummaries.Clear();
            foreach (var emp in list)
            {
                var summary = await _employeeService.GetEmployeeSummaryAsync(emp.Id);
                EmployeesSummaries.Add(summary);
            }

            string branchText = branchId.HasValue ? SelectedFilterBranch?.Name ?? "المحل المحدد" : "جميع المحلات";
            StatusMessage = $"تم تحميل {EmployeesSummaries.Count} موظفاً لـ ({branchText}) بنجاح";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء تحميل الموظفين: {ex.Message}";
        }
    }

    private async Task OpenAddEmployeeDialogAsync()
    {
        var branches = await _branchService.GetAllBranchesAsync();
        var dialog = new AddEmployeeDialog(branches)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.CreatedEmployee != null)
        {
            try
            {
                await _employeeService.AddEmployeeAsync(dialog.CreatedEmployee);
                await LoadEmployeesAsync();
                StatusMessage = $"تم إضافة الموظف الجديد ({dialog.CreatedEmployee.Name}) بنجاح إلى القائمة";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ أثناء إضافة الموظف: {ex.Message}";
            }
        }
    }

    private async Task OpenEmployeeDetailDialogAsync(EmployeeSummaryDto? dto)
    {
        if (dto == null) return;

        var dialog = new EmployeeDetailDialog(dto.EmployeeId, _employeeService)
        {
            Owner = Application.Current.MainWindow
        };

        dialog.ShowDialog();
        await LoadEmployeesAsync();
    }

    private async Task EditEmployeeAsync(EmployeeSummaryDto? dto)
    {
        if (dto == null) return;

        var emp = await _employeeService.GetEmployeeByIdAsync(dto.EmployeeId);
        if (emp == null) return;

        var branches = await _branchService.GetAllBranchesAsync();
        var dialog = new AddEmployeeDialog(branches)
        {
            Owner = Application.Current.MainWindow,
            Title = $"تعديل بيانات الموظف: {emp.Name}"
        };

        // Pre-fill fields
        dialog.TxtName.Text = emp.Name;
        dialog.TxtSalary.Text = emp.Salary.ToString("0.##");
        dialog.TxtOpeningBalance.Text = emp.OpeningBalance.ToString("0.##");
        dialog.TxtPosition.Text = emp.Position ?? "";
        dialog.TxtNotes.Text = emp.Notes ?? "";
        dialog.DpHireDate.SelectedDate = emp.HireDate.ToDateTime(TimeOnly.MinValue);

        if (dialog.ShowDialog() == true && dialog.CreatedEmployee != null)
        {
            try
            {
                emp.Name = dialog.CreatedEmployee.Name;
                emp.HireDate = dialog.CreatedEmployee.HireDate;
                emp.Salary = dialog.CreatedEmployee.Salary;
                emp.OpeningBalance = dialog.CreatedEmployee.OpeningBalance;
                emp.Position = dialog.CreatedEmployee.Position;
                emp.BranchId = dialog.CreatedEmployee.BranchId;
                emp.Notes = dialog.CreatedEmployee.Notes;

                await _employeeService.UpdateEmployeeAsync(emp);
                await LoadEmployeesAsync();
                StatusMessage = $"تم تعديل بيانات الموظف ({emp.Name}) بنجاح";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ أثناء التعديل: {ex.Message}";
            }
        }
    }

    private async Task DeleteEmployeeAsync(EmployeeSummaryDto? dto)
    {
        if (dto == null) return;

        var confirm = MessageBox.Show(
            $"⚠️ تحذير حساس:\nهل أنت أصلًا متأكد من حذف الموظف ({dto.EmployeeName}) وكافة سجلاّته وحركاته الماليّة من النظام؟\nلا يمكن التراجع عن هذا الإجراء!",
            "تأكيد التحذير الحساس لحذف موظف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            try
            {
                var ok = await _employeeService.DeleteEmployeeAsync(dto.EmployeeId);
                if (ok)
                {
                    StatusMessage = $"تم حذف الموظف ({dto.EmployeeName}) نهائياً";
                    await LoadEmployeesAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ أثناء الحذف: {ex.Message}";
            }
        }
    }
}
