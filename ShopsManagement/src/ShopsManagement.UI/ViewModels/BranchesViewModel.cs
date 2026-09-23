using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class BranchesViewModel : ViewModelBase
{
    private readonly IBranchService        _branchService;
    private readonly IDailyActivityService _dailyActivityService;
    private readonly IInvoiceService       _invoiceService;
    private readonly IEmployeeService      _employeeService;
    private readonly IBackupService?       _backupService;
    private string _statusMessage = string.Empty;

    public BranchesViewModel(
        IBranchService branchService,
        IDailyActivityService dailyActivityService,
        IInvoiceService invoiceService,
        IEmployeeService employeeService,
        IBackupService? backupService = null)
    {
        _branchService        = branchService;
        _dailyActivityService = dailyActivityService;
        _invoiceService       = invoiceService;
        _employeeService      = employeeService;
        _backupService        = backupService;

        BranchSummaries = new ObservableCollection<BranchSummaryDto>();

        OpenAddDialogCommand = new RelayCommand(async () => await OpenAddDialogAsync());
        OpenEditDialogCommand = new RelayCommand<BranchSummaryDto>(async (b) => await OpenEditDialogAsync(b));
        OpenDetailCommand = new RelayCommand<BranchSummaryDto>(async (b) => await OpenDetailDialogAsync(b));
        DeleteBranchCommand = new RelayCommand<BranchSummaryDto>(async (b) => await DeleteBranchAsync(b));
    }

    public ObservableCollection<BranchSummaryDto> BranchSummaries { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddDialogCommand { get; }
    public ICommand OpenEditDialogCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand DeleteBranchCommand { get; }

    public async Task LoadBranchesAsync()
    {
        try
        {
            StatusMessage = "جارٍ تحميل بيانات الفروع...";
            var summaries = await _branchService.GetAllBranchesSummaryAsync();
            BranchSummaries.Clear();
            foreach (var s in summaries)
                BranchSummaries.Add(s);

            StatusMessage = $"تم تحميل {BranchSummaries.Count} فرعًا";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء التحميل: {ex.Message}";
        }
    }

    private async Task OpenAddDialogAsync()
    {
        var dialog = new AddBranchDialog
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.BranchResult != null)
        {
            try
            {
                await _branchService.AddBranchAsync(dialog.BranchResult);
                StatusMessage = $"✅ تم إضافة الفرع ({dialog.BranchResult.Name}) بنجاح";
                await LoadBranchesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء الإضافة: {ex.Message}";
            }
        }
    }

    private async Task OpenEditDialogAsync(BranchSummaryDto? branchSummary)
    {
        if (branchSummary == null) return;

        var existingBranch = await _branchService.GetBranchByIdAsync(branchSummary.BranchId);
        if (existingBranch == null) return;

        var dialog = new AddBranchDialog(existingBranch)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.BranchResult != null)
        {
            try
            {
                await _branchService.UpdateBranchAsync(dialog.BranchResult);
                StatusMessage = $"✅ تم تعديل بيانات الفرع ({dialog.BranchResult.Name}) بنجاح";
                await LoadBranchesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء التعديل: {ex.Message}";
            }
        }
    }

    private async Task OpenDetailDialogAsync(BranchSummaryDto? branchSummary)
    {
        if (branchSummary == null) return;

        try
        {
            var dialog = new BranchDetailDialog(
                branchSummary.BranchId,
                branchSummary.BranchName,
                branchSummary.Address,
                branchSummary.RecipientName,
                branchSummary.IsActive,
                _branchService,
                _dailyActivityService,
                _invoiceService,
                _employeeService)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء فتح تفاصيل الفرع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteBranchAsync(BranchSummaryDto? branchSummary)
    {
        if (branchSummary == null) return;

        // First confirmation
        var firstConfirm = MessageBox.Show(
            $"هل تريد حذف الفرع \"{branchSummary.BranchName}\"؟\n\nسيتم التحقق من عدم وجود بيانات مرتبطة قبل الحذف.",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (firstConfirm != MessageBoxResult.Yes) return;

        // Check if deletion is possible
        bool canDelete;
        try
        {
            canDelete = await _branchService.CanDeleteBranchAsync(branchSummary.BranchId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء التحقق: {ex.Message}";
            return;
        }

        if (!canDelete)
        {
            MessageBox.Show(
                $"⛔ لا يمكن حذف الفرع \"{branchSummary.BranchName}\"\n\nالفرع مرتبط بموظفين أو فواتير مسجلة.\nيرجى نقل الموظفين وحذف الفواتير المرتبطة بالفرع أولاً.",
                "تعذر الحذف",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusMessage = $"⛔ لا يمكن حذف الفرع \"{branchSummary.BranchName}\" — توجد بيانات مرتبطة.";
            return;
        }

        // Second confirmation (final)
        var secondConfirm = MessageBox.Show(
            $"⚠️ تحذير نهائي: سيتم حذف الفرع \"{branchSummary.BranchName}\" بشكل دائم.\n\nسيتم حفظ نسخة احتياطية تلقائياً في مجلد المستندات.\n\nهل أنت متأكد تماماً؟ لا يمكن التراجع عن هذا الإجراء.",
            "تأكيد الحذف النهائي",
            MessageBoxButton.YesNo,
            MessageBoxImage.Stop);

        if (secondConfirm != MessageBoxResult.Yes) return;

        try
        {
            // ── Create safety backup BEFORE deletion ──────────────
            if (_backupService != null)
            {
                StatusMessage = "⏳ جارٍ إنشاء نسخة احتياطية قبل الحذف...";
                await _backupService.CreateBackupAsync(BackupType.Automatic);
            }

            await _branchService.DeleteBranchAsync(branchSummary.BranchId);
            StatusMessage = $"✅ تم حذف الفرع \"{branchSummary.BranchName}\" وحفظ نسخة احتياطية.";
            await LoadBranchesAsync();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "تعذر الحذف", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = $"❌ {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء الحذف: {ex.Message}";
        }
    }
}
