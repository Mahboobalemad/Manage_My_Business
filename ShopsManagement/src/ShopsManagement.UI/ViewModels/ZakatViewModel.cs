using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Views.Dialogs;

namespace ShopsManagement.UI.ViewModels;

public class ZakatViewModel : ViewModelBase
{
    private readonly IZakatService _zakatService;
    private string _statusMessage = string.Empty;

    public ZakatViewModel(IZakatService zakatService)
    {
        _zakatService = zakatService;

        ZakatTables = new ObservableCollection<ZakatTable>();

        OpenAddTableDialogCommand = new RelayCommand(async () => await OpenAddTableDialogAsync());
        OpenEditTableDialogCommand = new RelayCommand<ZakatTable>(async (t) => await OpenEditTableDialogAsync(t));
        OpenTableDetailCommand = new RelayCommand<ZakatTable>(async (t) => await OpenTableDetailDialogAsync(t));
        DeleteTableCommand = new RelayCommand<ZakatTable>(async (t) => await DeleteTableAsync(t));
    }

    public ObservableCollection<ZakatTable> ZakatTables { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OpenAddTableDialogCommand { get; }
    public ICommand OpenEditTableDialogCommand { get; }
    public ICommand OpenTableDetailCommand { get; }
    public ICommand DeleteTableCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadZakatTablesAsync();
    }

    public async Task LoadZakatTablesAsync()
    {
        try
        {
            StatusMessage = "جارٍ تحميل جداول الزكاة...";
            var list = await _zakatService.GetAllZakatTablesAsync();
            ZakatTables.Clear();
            foreach (var t in list)
                ZakatTables.Add(t);

            StatusMessage = $"تم تحميل {ZakatTables.Count} جدول زكاة";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء تحميل جداول الزكاة: {ex.Message}";
        }
    }

    private async Task OpenAddTableDialogAsync()
    {
        var dialog = new AddZakatTableDialog
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.TableResult != null)
        {
            try
            {
                await _zakatService.AddZakatTableAsync(dialog.TableResult);
                StatusMessage = $"✅ تم إضافة جدول الزكاة ({dialog.TableResult.Name}) بنجاح";
                await LoadZakatTablesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء إضافة جدول الزكاة: {ex.Message}";
            }
        }
    }

    private async Task OpenEditTableDialogAsync(ZakatTable? table)
    {
        if (table == null) return;

        var dialog = new AddZakatTableDialog(table)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.TableResult != null)
        {
            try
            {
                await _zakatService.UpdateZakatTableAsync(dialog.TableResult);
                StatusMessage = $"✅ تم تعديل جدول الزكاة ({dialog.TableResult.Name}) بنجاح";
                await LoadZakatTablesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطأ أثناء التعديل: {ex.Message}";
            }
        }
    }

    private async Task OpenTableDetailDialogAsync(ZakatTable? table)
    {
        if (table == null) return;

        try
        {
            var dialog = new ZakatTableDetailDialog(table.Id, _zakatService)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
            await LoadZakatTablesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء فتح تفاصيل جدول الزكاة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteTableAsync(ZakatTable? table)
    {
        if (table == null) return;

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من حذف جدول الزكاة \"{table.Name}\" بجميع بنود الزكاة المنفقة التابعة له؟\n\nلا يمكن التراجع عن الحذف بعد تنفيذه.",
            "تأكيد حذف جدول الزكاة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var ok = await _zakatService.DeleteZakatTableAsync(table.Id);
            if (ok)
            {
                StatusMessage = $"✅ تم حذف جدول الزكاة ({table.Name}) بنجاح";
                await LoadZakatTablesAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ أثناء الحذف: {ex.Message}";
        }
    }
}
