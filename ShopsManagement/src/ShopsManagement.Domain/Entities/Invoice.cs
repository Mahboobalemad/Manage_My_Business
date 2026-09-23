namespace ShopsManagement.Domain.Entities;

/// <summary>
/// فواتير البضاعة المستلمة من التاجر لحساب فرع واحد أو عدة فروع.
/// ملاحظة مهمة: الفاتورة هي استلام بضاعة وليست دفعة نقدية، وتُنشئ حركة دائنة في حساب التاجر.
/// </summary>
public class Invoice
{
    public int Id { get; set; }

    /// <summary>الفرع الرئيسي (اختياري / للتوافق)</summary>
    public int? BranchId { get; set; }

    public int TraderId { get; set; }

    /// <summary>تاريخ الفاتورة</summary>
    public DateOnly Date { get; set; }

    /// <summary>رقم / اسم / بيان الفاتورة (مثلاً: فاتورة الهندي 102)</summary>
    public string InvoiceName { get; set; } = string.Empty;

    /// <summary>إجمالي مبلغ الفاتورة الكلي (مجموع التوزيعات للفروع)</summary>
    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public Branch? Branch { get; set; }
    public Trader Trader { get; set; } = null!;
    public ICollection<InvoiceBranchAllocation> BranchAllocations { get; set; } = [];
    public ICollection<TraderTransaction> RelatedTraderTransactions { get; set; } = [];

    /// <summary>ملخص الفروع المستلمة ومبالغها للعرض بالجدول</summary>
    public string BranchesSummaryText
    {
        get
        {
            if (BranchAllocations != null && BranchAllocations.Count > 0)
            {
                var parts = BranchAllocations
                    .Where(a => a.Branch != null)
                    .Select(a => $"{a.Branch.Name} ({a.Amount:N0} ر.ي)");
                return string.Join(" | ", parts);
            }

            if (Branch != null)
                return $"{Branch.Name} ({Amount:N0} ر.ي)";

            return "لا يوجد فرع محدد";
        }
    }
}
