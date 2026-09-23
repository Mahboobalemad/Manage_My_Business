namespace ShopsManagement.Domain.Models;

public class DashboardSummaryDto
{
    public int SelectedBranchId { get; set; }
    public string BranchName { get; set; } = "جميع الفروع";

    public decimal TodaySales { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal TodayNetSales => TodaySales - TodayExpenses;

    public decimal MonthlySales { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal MonthlyNetSales => MonthlySales - MonthlyExpenses;

    public decimal TotalTradersDebt { get; set; }
    public decimal TotalEmployeesEntitlement { get; set; }

    public int ActiveBranchesCount { get; set; }
    public int InvoicesThisMonthCount { get; set; }
    public int DaysRecordedThisMonthCount { get; set; }
}
