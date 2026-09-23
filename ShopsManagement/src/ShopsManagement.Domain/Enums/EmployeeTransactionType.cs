namespace ShopsManagement.Domain.Enums;

/// <summary>
/// نوع حركة الموظف:
/// Salary = راتب (+)
/// Bonus = مكافأة (+)
/// Withdrawal = سحبية (-)
/// Deduction = خصم (-)
/// Payment = دفعة / تسوية (-)
/// </summary>
public enum EmployeeTransactionType
{
    Salary = 1,     // راتب (+ يزيد المستحق)
    Bonus = 2,      // مكافأة (+ يزيد المستحق)
    Withdrawal = 3, // سحبية (- ينقص المستحق)
    Deduction = 4,  // خصم (- ينقص المستحق)
    Payment = 5     // دفعة (- تسوية تنقص المستحق)
}
