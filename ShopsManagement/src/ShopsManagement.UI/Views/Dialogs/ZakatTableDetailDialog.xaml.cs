using System.Windows;
using System.Windows.Controls;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class ZakatTableDetailDialog : Window
{
    private readonly int _zakatTableId;
    private readonly IZakatService _zakatService;
    private ZakatTable? _table;

    public ZakatTableDetailDialog(int zakatTableId, IZakatService zakatService)
    {
        InitializeComponent();
        _zakatTableId = zakatTableId;
        _zakatService = zakatService;

        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _table = await _zakatService.GetZakatTableByIdAsync(_zakatTableId);
            if (_table == null) return;

            TxtTableTitle.Text = $"🌙 جدول زكاة: {_table.Name}";
            TxtTableSubtitle.Text = $"تاريخ الإنشاء: {_table.Date:yyyy/MM/dd}" + (!string.IsNullOrWhiteSpace(_table.Notes) ? $"   |   ملاحظات: {_table.Notes}" : "");

            var entries = await _zakatService.GetEntriesByTableIdAsync(_zakatTableId);
            DgEntries.ItemsSource = entries;

            decimal totalSpent = entries.Sum(e => e.Amount);
            TxtTotalSpent.Text = $"إجمالي المنفق: {totalSpent:N2} ر.ي";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء تحميل تفاصيل الزكاة المنفقة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnAddEntry_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddZakatEntryDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.EntryResult != null)
        {
            try
            {
                var entry = dialog.EntryResult;
                entry.ZakatTableId = _zakatTableId;
                await _zakatService.AddZakatEntryAsync(entry);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة بند الزكاة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnEditEntry_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ZakatEntry entry) return;

        var dialog = new AddZakatEntryDialog(entry)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.EntryResult != null)
        {
            try
            {
                var updated = dialog.EntryResult;
                updated.ZakatTableId = _zakatTableId;
                await _zakatService.UpdateZakatEntryAsync(updated);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تعديل بند الزكاة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnDeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ZakatEntry entry) return;

        var result = MessageBox.Show(
            $"هل أنت متأكد من حذف بند الزكاة ({entry.ZakatType}) بمبلغ {entry.Amount:N2} ر.ي؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _zakatService.DeleteZakatEntryAsync(entry.Id);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
