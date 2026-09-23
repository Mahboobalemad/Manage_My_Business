using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class InvoicesViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IBranchService _branchService;
    private readonly ITraderService _traderService;

    private Branch? _selectedFilterBranch;
    private Trader? _selectedFilterTrader;
    private string _statusMessage = string.Empty;

    public InvoicesViewModel(IInvoiceService invoiceService, IBranchService branchService, ITraderService traderService)
    {
        _invoiceService = invoiceService;
        _branchService = branchService;
        _traderService = traderService;

        Invoices = new ObservableCollection<Invoice>();
        FilterBranches = new ObservableCollection<Branch>();
        FilterTraders = new ObservableCollection<Trader>();

        OpenAddInvoiceDialogCommand = new RelayCommand(async () => await OpenAddInvoiceDialogAsync());
        OpenEditInvoiceDialogCommand = new RelayCommand<Invoice>(async (i) => await OpenEditInvoiceDialogAsync(i));
        DeleteInvoiceCommand = new RelayCommand<Invoice>(async (i) => await DeleteInvoiceAsync(i));
    }

    public ObservableCollection<Invoice> Invoices { get; }
    public ObservableCollection<Branch> FilterBranches { get; }
    public ObservableCollection<Trader> FilterTraders { get; }

    public Branch? SelectedFilterBranch
    {
        get => _selectedFilterBranch;
        set
        {
            if (SetProperty(ref _selectedFilterBranch, value))
                _ = LoadInvoicesAsync();
        }
    }

    public Trader? SelectedFilterTrader
    {
        get => _selectedFilterTrader;
        set
        {
            if (SetProperty(ref _selectedFilterTrader, value))
                _ = LoadInvoicesAsync();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddInvoiceDialogCommand { get; }
    public ICommand OpenEditInvoiceDialogCommand { get; }
    public ICommand DeleteInvoiceCommand { get; }

    public async Task InitializeAsync()
    {
        var branchList = await _branchService.GetAllBranchesAsync();
        FilterBranches.Clear();
        FilterBranches.Add(new Branch { Id = 0, Name = "جميع الفروع" });
        foreach (var b in branchList)
            FilterBranches.Add(b);

        var traderList = await _traderService.GetAllTradersAsync();
        FilterTraders.Clear();
        FilterTraders.Add(new Trader { Id = 0, Name = "جميع التجار" });
        foreach (var t in traderList)
            FilterTraders.Add(t);

        SelectedFilterBranch = FilterBranches[0];
        SelectedFilterTrader = FilterTraders[0];

        await LoadInvoicesAsync();
    }

    public async Task LoadInvoicesAsync()
    {
        try
        {
            StatusMessage = "جارٍ تحميل بيانات الفواتير...";
            int? branchId = SelectedFilterBranch?.Id > 0 ? SelectedFilterBranch.Id : null;
            int? traderId = SelectedFilterTrader?.Id > 0 ? SelectedFilterTrader.Id : null;

            var list = await _invoiceService.GetAllInvoicesAsync(branchId, traderId);
            Invoices.Clear();
            foreach (var inv in list)
                Invoices.Add(inv);

            StatusMessage = $"تم تحميل {Invoices.Count} فاتورة بضاعة";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء التحميل: {ex.Message}";
        }
    }

    private async Task OpenAddInvoiceDialogAsync()
    {
        var branchList = await _branchService.GetAllBranchesAsync();
        var traderList = await _traderService.GetAllTradersAsync();

        if (branchList.Count == 0 || traderList.Count == 0)
        {
            MessageBox.Show("يرجى التأكد من إضافة فروع وتجار في النظام قبل تسجيل الفواتير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new AddInvoiceDialog(branchList, traderList)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.InvoiceResult != null)
        {
            try
            {
                await _invoiceService.AddInvoiceAsync(dialog.InvoiceResult, dialog.AllocationsResult);
                StatusMessage = $"✅ تم حفظ الفواتير وتحديث أرصدة الفروع وحساب التاجر أوتوماتيكياً بنجاح";
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء حفظ الفاتورة: {ex.Message}";
            }
        }
    }

    private async Task OpenEditInvoiceDialogAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        var fullInvoice = await _invoiceService.GetInvoiceByIdAsync(invoice.Id);
        if (fullInvoice == null) return;

        var branchList = await _branchService.GetAllBranchesAsync();
        var traderList = await _traderService.GetAllTradersAsync();

        var dialog = new AddInvoiceDialog(branchList, traderList, fullInvoice)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.InvoiceResult != null)
        {
            try
            {
                await _invoiceService.UpdateInvoiceAsync(dialog.InvoiceResult, dialog.AllocationsResult);
                StatusMessage = $"✅ تم تعديل الفاتورة وتحديث أرصدة الفروع والتاجر تلقائياً بنجاح";
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء تعديل الفاتورة: {ex.Message}";
            }
        }
    }

    private async Task DeleteInvoiceAsync(Invoice? invoice)
    {
        if (invoice == null) return;

        var confirm = MessageBox.Show(
            $"⚠️ هل أنت متأكد من حذف الفاتورة رقم ({invoice.InvoiceName}) بمبلغ كلي {invoice.Amount:N2} ر.ي؟\n\n" +
            "تنبيه: سيتم إلغاء مبالغ الفواتير وحذف الحركة الماليّة من حساب التاجر المورّد وأرصدة الفروع المستلمة تلقائياً.\n\n" +
            "هل تريد المتابعة وتأكيد الحذف؟",
            "تأكيد حذف الفاتورة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var ok = await _invoiceService.DeleteInvoiceAsync(invoice.Id);
            if (ok)
            {
                StatusMessage = $"✅ تم حذف الفاتورة ({invoice.InvoiceName}) وتحديث جميع الأرصدة المرتبطة بنجاح";
                await LoadInvoicesAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء حذف الفاتورة: {ex.Message}";
        }
    }
}
