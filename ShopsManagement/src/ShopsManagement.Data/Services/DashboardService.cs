using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? branchId = null, DateTime? targetDate = null)
    {
        var date = targetDate?.Date ?? DateTime.Today;
        var dateOnly = DateOnly.FromDateTime(date);

        var firstDayOfMonth = new DateOnly(date.Year, date.Month, 1);
        var lastDayOfMonth = new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

        var dto = new DashboardSummaryDto
        {
            SelectedBranchId = branchId ?? 0,
            BranchName = "جميع الفروع"
        };

        if (branchId.HasValue && branchId.Value > 0)
        {
            var branch = await _context.Branches.FindAsync(branchId.Value);
            if (branch != null)
                dto.BranchName = branch.Name;
        }

        // 1. Today Activities
        var todayQuery = _context.DailyActivities.AsNoTracking().Where(d => d.Date == dateOnly);
        if (branchId.HasValue && branchId.Value > 0)
            todayQuery = todayQuery.Where(d => d.BranchId == branchId.Value);

        var todayList = await todayQuery.ToListAsync();
        dto.TodaySales = todayList.Sum(d => d.TotalSales);
        dto.TodayExpenses = todayList.Sum(d => d.TotalExpenses);

        // 2. Monthly Activities
        var monthQuery = _context.DailyActivities.AsNoTracking()
            .Where(d => d.Date >= firstDayOfMonth && d.Date <= lastDayOfMonth);
        if (branchId.HasValue && branchId.Value > 0)
            monthQuery = monthQuery.Where(d => d.BranchId == branchId.Value);

        var monthList = await monthQuery.ToListAsync();
        dto.MonthlySales = monthList.Sum(d => d.TotalSales);
        dto.MonthlyExpenses = monthList.Sum(d => d.TotalExpenses);
        dto.DaysRecordedThisMonthCount = monthList.Count;

        // 3. Total Traders Debt (Outstanding Balance across all traders)
        var traderTxs = await _context.TraderTransactions.AsNoTracking().ToListAsync();
        var totalInvoices = traderTxs.Where(t => t.Type == TraderTransactionType.Invoice).Sum(t => t.Amount);
        var totalPayments = traderTxs.Where(t => t.Type == TraderTransactionType.Payment).Sum(t => t.Amount);
        dto.TotalTradersDebt = totalInvoices - totalPayments;

        // 4. Total Employees Entitlement
        var empTxs = await _context.EmployeeTransactions.AsNoTracking().ToListAsync();
        var empEntitlements = empTxs.Where(t => t.Type == EmployeeTransactionType.Salary || t.Type == EmployeeTransactionType.Bonus).Sum(t => t.Amount);
        var empDeductions = empTxs.Where(t => t.Type == EmployeeTransactionType.Withdrawal || t.Type == EmployeeTransactionType.Deduction || t.Type == EmployeeTransactionType.Payment).Sum(t => t.Amount);
        dto.TotalEmployeesEntitlement = empEntitlements - empDeductions;

        // 5. Counts
        dto.ActiveBranchesCount = await _context.Branches.AsNoTracking().CountAsync();

        var invQuery = _context.Invoices.AsNoTracking()
            .Where(i => i.Date >= firstDayOfMonth && i.Date <= lastDayOfMonth);
        if (branchId.HasValue && branchId.Value > 0)
            invQuery = invQuery.Where(i => i.BranchId == branchId.Value);

        dto.InvoicesThisMonthCount = await invQuery.CountAsync();

        return dto;
    }
}
