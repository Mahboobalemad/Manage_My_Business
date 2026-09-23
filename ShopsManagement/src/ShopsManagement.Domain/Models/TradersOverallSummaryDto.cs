namespace ShopsManagement.Domain.Models;

public class TradersOverallSummaryDto
{
    public decimal OverallTotalInvoices { get; set; }
    public decimal OverallTotalPayments { get; set; }
    public decimal OverallOutstandingBalance => OverallTotalInvoices - OverallTotalPayments;
    public int ActiveTradersCount { get; set; }
    public int TotalTradersCount { get; set; }
}
