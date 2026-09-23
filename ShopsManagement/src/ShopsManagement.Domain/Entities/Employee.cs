using ShopsManagement.Domain.Enums;

namespace ShopsManagement.Domain.Entities;

/// <summary>
/// الموظفون.
/// </summary>
public class Employee
{
    public int Id { get; set; }

    /// <summary>اسم الموظف</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>رقم الهاتف</summary>
    public string? Phone { get; set; }

    /// <summary>المسمى الوظيفي</summary>
    public string? Position { get; set; }

    /// <summary>الفرع الذي يعمل به الموظف</summary>
    public int BranchId { get; set; }

    /// <summary>الراتب المتفق عليه</summary>
    public decimal Salary { get; set; }

    /// <summary>الرصيد الافتتاحي (موجب = له / سالب = عليه)</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>تاريخ التعيين / الدخول</summary>
    public DateOnly HireDate { get; set; }

    /// <summary>حالة الموظف (نشط / غير نشط)</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>ملاحظات</summary>
    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public Branch Branch { get; set; } = null!;
    public ICollection<EmployeeTransaction> Transactions { get; set; } = [];
}
