using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class TradersViewModel : ViewModelBase
{
    private readonly ITraderService  _traderService;
    private readonly IBackupService? _backupService;

    private TradersOverallSummaryDto _overallSummary = new();
    private ComboBoxItem? _selectedStatusFilter;
    private string _statusMessage = string.Empty;

    public TradersViewModel(ITraderService traderService, IBackupService? backupService = null)
    {
        _traderService = traderService;
        _backupService = backupService;

        TradersSummaries = new ObservableCollection<TraderSummaryDto>();

        OpenAddTraderDialogCommand = new RelayCommand(async () => await OpenAddTraderDialogAsync());
        OpenEditTraderDialogCommand = new RelayCommand<TraderSummaryDto>(async (t) => await OpenEditTraderDialogAsync(t));
        OpenTraderDetailCommand = new RelayCommand<TraderSummaryDto>(async (t) => await OpenTraderDetailDialogAsync(t));
        DeleteTraderCommand = new RelayCommand<TraderSummaryDto>(async (t) => await DeleteTraderAsync(t));
    }

    public ObservableCollection<TraderSummaryDto> TradersSummaries { get; }

    public TradersOverallSummaryDto OverallSummary
    {
        get => _overallSummary;
        set => SetProperty(ref _overallSummary, value);
    }

    public ComboBoxItem? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                _ = LoadTradersAsync();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddTraderDialogCommand { get; }
    public ICommand OpenEditTraderDialogCommand { get; }
    public ICommand OpenTraderDetailCommand { get; }
    public ICommand DeleteTraderCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadTradersAsync();
    }

    public async Task LoadTradersAsync()
    {
        try
        {
            StatusMessage = "جارٍ تحميل بيانات التجار...";

            OverallSummary = await _traderService.GetOverallSummaryAsync();

            bool? isActiveFilter = null;
            string filterText = SelectedStatusFilter?.Content?.ToString() ?? string.Empty;
            if (filterText.Contains("النشطون"))
                isActiveFilter = true;
            else if (filterText.Contains("غير النشطين"))
                isActiveFilter = false;

            var list = await _traderService.GetAllTradersSummaryAsync(isActiveFilter);
            TradersSummaries.Clear();
            foreach (var t in list)
                TradersSummaries.Add(t);

            StatusMessage = $"تم تحميل {TradersSummaries.Count} تجار بنجاح";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء تحميل التجار: {ex.Message}";
        }
    }

    private async Task OpenAddTraderDialogAsync()
    {
        var dialog = new AddTraderDialog
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.TraderResult != null)
        {
            try
            {
                await _traderService.AddTraderAsync(dialog.TraderResult);
                StatusMessage = $"✅ تم إضافة التاجر ({dialog.TraderResult.Name}) بنجاح";
                await LoadTradersAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء الإضافة: {ex.Message}";
            }
        }
    }

    private async Task OpenEditTraderDialogAsync(TraderSummaryDto? summary)
    {
        if (summary == null) return;

        var existingTrader = await _traderService.GetTraderByIdAsync(summary.TraderId);
        if (existingTrader == null) return;

        var dialog = new AddTraderDialog(existingTrader)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.TraderResult != null)
        {
            try
            {
                await _traderService.UpdateTraderAsync(dialog.TraderResult);
                StatusMessage = $"✅ تم تعديل بيانات التاجر ({dialog.TraderResult.Name}) بنجاح";
                await LoadTradersAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء التعديل: {ex.Message}";
            }
        }
    }

    private async Task OpenTraderDetailDialogAsync(TraderSummaryDto? summary)
    {
        if (summary == null) return;

        try
        {
            var dialog = new TraderDetailDialog(summary.TraderId, _traderService)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
            await LoadTradersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء فتح تفاصيل التاجر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteTraderAsync(TraderSummaryDto? summary)
    {
        if (summary == null) return;

        var confirmResult = MessageBox.Show(
            $"⚠️ هل أنت متأكد من حذف التاجر \"{summary.TraderName}\"؟\n\n" +
            "💡 ملاحظة هامة: بدلاً من الحذف يمكنك تصنيف التاجر كـ \"غير نشط\" لضمان عدم فقدان البيانات التاريخية.\n\n" +
            "عند التأكيد سيتم حذف التاجر وإنشاء نسخة احتياطية تلقائياً لبياناته بالنظام.\n\nهل تريد المتابعة وحذف التاجر؟",
            "تأكيد حذف التاجر",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes) return;

        try
        {
            // ── Create safety backup BEFORE deletion ──────────────
            if (_backupService != null)
            {
                StatusMessage = "⏳ جارٍ إنشاء نسخة احتياطية قبل الحذف...";
                await _backupService.CreateBackupAsync(BackupType.Automatic);
            }

            await _traderService.DeleteTraderAsync(summary.TraderId);
            StatusMessage = $"✅ تم حذف التاجر ({summary.TraderName}) وإنشاء نسخة احتياطية تلقائياً.";
            await LoadTradersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء حذف التاجر: {ex.Message}";
        }
    }
}
