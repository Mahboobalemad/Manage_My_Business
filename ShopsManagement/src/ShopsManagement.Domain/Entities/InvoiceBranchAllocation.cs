namespace ShopsManagement.Domain.Entities;

/// <summary>
/// تخصيص مبالغ الفاتورة على الفروع المستلمة.
/// يتيح توزيع الفاتورة الواحدة على فرع واحد أو عدة فروع بمبالغ مخصصة لكل فرع.
/// </summary>
public class InvoiceBranchAllocation
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int BranchId { get; set; }

    /// <summary>المبلغ المخصص لهذا الفرع المستلم</summary>
    public decimal Amount { get; set; }

    // ====== Navigation Properties ======
    public Invoice Invoice { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
