namespace ShopsManagement.Domain.Enums;

/// <summary>
/// نوع حركة حساب التاجر:
/// Invoice = فاتورة (حركة + تزيد المتبقي للتاجر)
/// Payment = دفعة (حركة - تنقص المتبقي للتاجر)
/// </summary>
public enum TraderTransactionType
{
    Invoice = 1, // فاتورة
    Payment = 2  // دفعة
}
