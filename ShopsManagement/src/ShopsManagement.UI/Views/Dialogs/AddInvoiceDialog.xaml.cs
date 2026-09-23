using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ShopsManagement.Domain.Entities;

namespace ShopsManagement.UI.Views.Dialogs;

public class BranchAllocationItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _amountText = "0";

    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnSelectionOrAmountChanged?.Invoke();
            }
        }
    }

    public string AmountText
    {
        get => _amountText;
        set
        {
            if (_amountText != value)
            {
                _amountText = value;
                OnPropertyChanged();
                OnSelectionOrAmountChanged?.Invoke();
            }
        }
    }

    public decimal Amount
    {
        get
        {
            decimal.TryParse(AmountText, out decimal val);
            return val;
        }
    }

    public Action? OnSelectionOrAmountChanged { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class AddInvoiceDialog : Window
{
    private readonly Invoice? _existingInvoice;
    public Invoice? InvoiceResult { get; private set; }
    public List<InvoiceBranchAllocation> AllocationsResult { get; private set; } = [];

    public ObservableCollection<BranchAllocationItem> AllocationItems { get; } = [];

    public AddInvoiceDialog(List<Branch> branches, List<Trader> traders, Invoice? existingInvoice = null)
    {
        InitializeComponent();
        _existingInvoice = existingInvoice;

        CmbTrader.ItemsSource = traders;
        if (traders.Count > 0)
            CmbTrader.SelectedIndex = 0;

        DpDate.SelectedDate = DateTime.Today;

        // Populate Branch Allocation items
        foreach (var b in branches)
        {
            var item = new BranchAllocationItem
            {
                BranchId = b.Id,
                BranchName = b.Name,
                IsSelected = false,
                AmountText = "0",
                OnSelectionOrAmountChanged = CalculateTotal
            };
            AllocationItems.Add(item);
        }

        DgBranchAllocations.ItemsSource = AllocationItems;

        if (_existingInvoice != null)
        {
            Title = "تعديل بيانات الفاتورة";
            TxtTitle.Text = "✏️ تعديل بيانات الفاتورة";
            BtnSave.Content = "حفظ التعديلات";

            DpDate.SelectedDate = _existingInvoice.Date.ToDateTime(TimeOnly.MinValue);
            TxtInvoiceName.Text = _existingInvoice.InvoiceName;
            TxtNotes.Text = _existingInvoice.Notes ?? string.Empty;

            var selectedTrader = traders.FirstOrDefault(t => t.Id == _existingInvoice.TraderId);
            if (selectedTrader != null)
                CmbTrader.SelectedItem = selectedTrader;

            // Load existing allocations
            if (_existingInvoice.BranchAllocations != null && _existingInvoice.BranchAllocations.Count > 0)
            {
                foreach (var alloc in _existingInvoice.BranchAllocations)
                {
                    var item = AllocationItems.FirstOrDefault(i => i.BranchId == alloc.BranchId);
                    if (item != null)
                    {
                        item.IsSelected = true;
                        item.AmountText = alloc.Amount.ToString("F2");
                    }
                }
            }
            else if (_existingInvoice.BranchId.HasValue)
            {
                var item = AllocationItems.FirstOrDefault(i => i.BranchId == _existingInvoice.BranchId.Value);
                if (item != null)
                {
                    item.IsSelected = true;
                    item.AmountText = _existingInvoice.Amount.ToString("F2");
                }
            }
        }

        CalculateTotal();
    }

    private void CalculateTotal()
    {
        decimal total = AllocationItems
            .Where(i => i.IsSelected)
            .Sum(i => i.Amount);

        TxtTotalAmount.Text = $"{total:N2} ر.ي";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInvoiceName.Text))
        {
            MessageBox.Show("يرجى إدخال رقم أو اسم الفاتورة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbTrader.SelectedItem is not Trader selectedTrader)
        {
            MessageBox.Show("يرجى اختيار التاجر المورّد.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedBranchItems = AllocationItems.Where(i => i.IsSelected && i.Amount > 0).ToList();
        if (selectedBranchItems.Count == 0)
        {
            MessageBox.Show("يرجى اختيار فرع واحد على الأقل وتخصيص مبلغ أكبر من صفر له.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal totalAmount = selectedBranchItems.Sum(i => i.Amount);

        AllocationsResult = selectedBranchItems.Select(i => new InvoiceBranchAllocation
        {
            BranchId = i.BranchId,
            Amount = i.Amount
        }).ToList();

        InvoiceResult = new Invoice
        {
            Id = _existingInvoice?.Id ?? 0,
            Date = DateOnly.FromDateTime(DpDate.SelectedDate ?? DateTime.Today),
            InvoiceName = TxtInvoiceName.Text.Trim(),
            TraderId = selectedTrader.Id,
            BranchId = selectedBranchItems.Count == 1 ? selectedBranchItems[0].BranchId : (int?)null,
            Amount = totalAmount,
            Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim()
        };

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
