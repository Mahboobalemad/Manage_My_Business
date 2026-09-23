using System.Windows;
using System.Windows.Controls;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.UI.Views.Dialogs;

public partial class TraderDetailDialog : Window
{
    private readonly int _traderId;
    private readonly ITraderService _traderService;
    private TraderSummaryDto? _summary;
    private List<TraderTransaction> _allTransactions = [];
    private bool _isInitialized = false;

    public TraderDetailDialog(int traderId, ITraderService traderService)
    {
        InitializeComponent();
        _traderId = traderId;
        _traderService = traderService;

        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _summary = await _traderService.GetTraderSummaryAsync(_traderId);
            _allTransactions = await _traderService.GetTraderStatementAsync(_traderId);

            TxtTraderTitle.Text = $"🤝 كشف حساب التاجر: {_summary.TraderName}";
            var infoParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_summary.BusinessName)) infoParts.Add($"النشاط: {_summary.BusinessName}");
            if (!string.IsNullOrWhiteSpace(_summary.Location)) infoParts.Add($"الموقع: {_summary.Location}");
            if (!string.IsNullOrWhiteSpace(_summary.Phone)) infoParts.Add($"الهاتف: {_summary.Phone}");
            TxtTraderSubtitle.Text = string.Join("   |   ", infoParts);

            TxtTraderNetBalance.Text = _summary.NetBalanceDisplayText;
            TxtTraderNetBalance.Foreground = _summary.IsNegativeNet
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74));

            // Populate Year filter
            var years = _allTransactions.Select(t => t.Date.Year).Distinct().OrderByDescending(y => y).ToList();
            CmbYear.Items.Clear();
            CmbYear.Items.Add(new ComboBoxItem { Content = "جميع السنوات", Tag = 0 });
            foreach (var yr in years)
                CmbYear.Items.Add(new ComboBoxItem { Content = yr.ToString(), Tag = yr });
            CmbYear.SelectedIndex = 0;

            // Populate Month filter
            CmbMonth.Items.Clear();
            CmbMonth.Items.Add(new ComboBoxItem { Content = "جميع الأشهر", Tag = 0 });
            string[] monthNames = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                                    "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];
            for (int m = 1; m <= 12; m++)
                CmbMonth.Items.Add(new ComboBoxItem { Content = monthNames[m - 1], Tag = m });
            CmbMonth.SelectedIndex = 0;

            _isInitialized = true;
            RefreshGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"حدث خطأ أثناء تحميل كشف حساب التاجر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (!_isInitialized) return;

        int? year = CmbYear.SelectedItem is ComboBoxItem yItem && (int)yItem.Tag != 0 ? (int)yItem.Tag : null;
        int? month = CmbMonth.SelectedItem is ComboBoxItem mItem && (int)mItem.Tag != 0 ? (int)mItem.Tag : null;
        string typeTag = (CmbTypeFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

        TraderTransactionType? type = typeTag switch
        {
            "Invoice" => TraderTransactionType.Invoice,
            "Payment" => TraderTransactionType.Payment,
            _ => null
        };

        var query = _allTransactions.AsEnumerable();
        if (year.HasValue) query = query.Where(t => t.Date.Year == year.Value);
        if (month.HasValue) query = query.Where(t => t.Date.Month == month.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);

        DgTransactions.ItemsSource = query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
    }

    private async void BtnAddTransaction_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddTraderTransactionDialog
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.TransactionResult != null)
        {
            try
            {
                var tx = dialog.TransactionResult;
                await _traderService.AddTransactionAsync(_traderId, tx.Type, tx.Amount, tx.Date.ToDateTime(TimeOnly.MinValue), tx.Notes);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnEditTx_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not TraderTransaction tx) return;

        var dialog = new AddTraderTransactionDialog(tx)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.TransactionResult != null)
        {
            try
            {
                var updated = dialog.TransactionResult;
                updated.TraderId = _traderId;
                await _traderService.UpdateTransactionAsync(updated);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تعديل الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnDeleteTx_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not TraderTransaction tx) return;

        var result = MessageBox.Show(
            $"هل أنت متأكد من حذف حركة ({tx.Type}) بمبلغ {tx.Amount:N2} ر.ي؟\n\nتلميح: بدلاً من حذف البيانات يمكنك متابعة الحركات، وعند تأكيد الحذف سيتم حفظ نسخة احتياطية تلقائياً للنظام.",
            "تأكيد حذف الحركة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _traderService.DeleteTransactionAsync(tx.Id);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حذف الحركة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
