namespace ShopsManagement.Domain.Models;

public class MonthlySummaryDto
{
    public int BranchId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSales => TotalSales - TotalExpenses;
    public decimal TotalProfits { get; set; }
    public int TotalDaysRecorded { get; set; }
}
