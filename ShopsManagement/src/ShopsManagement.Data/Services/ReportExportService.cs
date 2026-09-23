using System.Text;
using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class ReportExportService : IReportExportService
{
    private readonly AppDbContext _context;

    public ReportExportService(AppDbContext context)
    {
        _context = context;
    }

    #region Daily Activities Export

    public async Task ExportDailyActivitiesToExcelAsync(int? branchId, int? year, int? month, DateOnly? date, string filePath)
    {
        var branchName = "جميع الفروع";
        if (branchId.HasValue && branchId.Value > 0)
        {
            var branch = await _context.Branches.FindAsync(branchId.Value);
            branchName = branch?.Name ?? "الفرع المحدد";
        }

        var query = _context.DailyActivities
            .Include(d => d.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(d => d.BranchId == branchId.Value);

        if (date.HasValue)
            query = query.Where(d => d.Date == date.Value);
        else
        {
            if (year.HasValue && year.Value > 0)
                query = query.Where(d => d.Date.Year == year.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(d => d.Date.Month == month.Value);
        }

        var list = await query.OrderByDescending(d => d.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:Tahoma,Arial;direction:rtl;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #cbd5e1;padding:8px;text-align:right;} th{background-color:#1e293b;color:white;}</style></head><body>");
        sb.AppendLine($"<h2>تقرير النشاط اليومي والمبيعات — {branchName}</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>الفرع</th><th>التاريخ</th><th>المبيعات (ر.ي)</th><th>المصروفات (ر.ي)</th><th>صافي المبيعات (ر.ي)</th><th>الأرباح (ر.ي)</th><th>الملاحظات</th></tr>");

        foreach (var item in list)
        {
            sb.AppendLine($"<tr><td>{item.Branch?.Name ?? ""}</td><td>{item.Date:yyyy/MM/dd}</td><td>{item.TotalSales:N2}</td><td>{item.TotalExpenses:N2}</td><td>{item.NetSales:N2}</td><td>{item.DisplayProfit:N2}</td><td>{item.Notes ?? ""}</td></tr>");
        }

        var totalSales = list.Sum(l => l.TotalSales);
        var totalExpenses = list.Sum(l => l.TotalExpenses);
        var netSales = totalSales - totalExpenses;
        var totalProfits = list.Sum(l => l.DisplayProfit);

        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td colspan='2'>الإجمالي الكلي</td><td>{totalSales:N2}</td><td>{totalExpenses:N2}</td><td>{netSales:N2}</td><td>{totalProfits:N2}</td><td></td></tr>");
        sb.AppendLine("</table></body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportDailyActivitiesToPdfAsync(int? branchId, int? year, int? month, DateOnly? date, string filePath)
    {
        await ExportDailyActivitiesToExcelAsync(branchId, year, month, date, filePath);
    }

    #endregion

    #region Invoices Export

    public async Task ExportInvoicesToExcelAsync(int? branchId, int? traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        var query = _context.Invoices
            .Include(i => i.Branch)
            .Include(i => i.Trader)
            .Include(i => i.BranchAllocations)
                .ThenInclude(ba => ba.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(i => i.BranchId == branchId.Value || i.BranchAllocations.Any(a => a.BranchId == branchId.Value));

        if (traderId.HasValue && traderId.Value > 0)
            query = query.Where(i => i.TraderId == traderId.Value);

        if (date.HasValue)
            query = query.Where(i => i.Date == date.Value);
        else
        {
            if (year.HasValue && year.Value > 0)
                query = query.Where(i => i.Date.Year == year.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(i => i.Date.Month == month.Value);

            if (startDate.HasValue)
                query = query.Where(i => i.Date >= DateOnly.FromDateTime(startDate.Value));

            if (endDate.HasValue)
                query = query.Where(i => i.Date <= DateOnly.FromDateTime(endDate.Value));
        }

        var list = await query.OrderByDescending(i => i.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:Tahoma,Arial;direction:rtl;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #cbd5e1;padding:8px;text-align:right;} th{background-color:#1e293b;color:white;}</style></head><body>");
        sb.AppendLine("<h2>تقرير فواتير البضاعة المستلمة</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>#</th><th>التاريخ</th><th>اسم/رقم الفاتورة</th><th>التاجر المورّد</th><th>الفروع المستلمة</th><th>المبلغ الكلي (ر.ي)</th><th>الملاحظات</th></tr>");

        foreach (var item in list)
        {
            sb.AppendLine($"<tr><td>{item.Id}</td><td>{item.Date:yyyy/MM/dd}</td><td>{item.InvoiceName}</td><td>{item.Trader?.Name ?? ""}</td><td>{item.BranchesSummaryText}</td><td>{item.Amount:N2}</td><td>{item.Notes ?? ""}</td></tr>");
        }

        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td colspan='5'>الإجمالي الكلي للفواتير</td><td>{list.Sum(i => i.Amount):N2}</td><td></td></tr>");
        sb.AppendLine("</table></body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportInvoicesToPdfAsync(int? branchId, int? traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        await ExportInvoicesToExcelAsync(branchId, traderId, year, month, date, startDate, endDate, filePath);
    }

    #endregion

    #region Trader Statement Export

    public async Task ExportTraderStatementToExcelAsync(int traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        var trader = await _context.Traders.FindAsync(traderId);
        var traderName = trader?.Name ?? "التاجر";

        var query = _context.TraderTransactions
            .Include(t => t.RelatedInvoice)
            .AsNoTracking()
            .Where(t => t.TraderId == traderId);

        if (date.HasValue)
            query = query.Where(t => t.Date == date.Value);
        else
        {
            if (year.HasValue && year.Value > 0)
                query = query.Where(t => t.Date.Year == year.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(t => t.Date.Month == month.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= DateOnly.FromDateTime(startDate.Value));

            if (endDate.HasValue)
                query = query.Where(t => t.Date <= DateOnly.FromDateTime(endDate.Value));
        }

        var list = await query.OrderBy(t => t.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:Tahoma,Arial;direction:rtl;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #cbd5e1;padding:8px;text-align:right;} th{background-color:#1e293b;color:white;}</style></head><body>");
        sb.AppendLine($"<h2>كشف حساب التاجر: {traderName}</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>التاريخ</th><th>نوع الحركة</th><th>المبلغ (ر.ي)</th><th>الفاتورة المرتبطة</th><th>البيان والملاحظات</th></tr>");

        foreach (var item in list)
        {
            var typeStr = item.Type == TraderTransactionType.Invoice ? "بضاعة مستلمة (فاتورة +)" : "دفعة من الحساب (-)";
            sb.AppendLine($"<tr><td>{item.Date:yyyy/MM/dd}</td><td>{typeStr}</td><td>{item.Amount:N2}</td><td>{item.RelatedInvoice?.InvoiceName ?? ""}</td><td>{item.Notes ?? ""}</td></tr>");
        }

        var totalInvoices = list.Where(l => l.Type == TraderTransactionType.Invoice).Sum(l => l.Amount);
        var totalPayments = list.Where(l => l.Type == TraderTransactionType.Payment).Sum(l => l.Amount);
        var balance = (trader?.OpeningBalance ?? 0m) + totalInvoices - totalPayments;

        sb.AppendLine($"<tr style='font-weight:bold;'><td colspan='2'>إجمالي الفواتير (+)</td><td>{totalInvoices:N2}</td><td colspan='2'></td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;'><td colspan='2'>إجمالي الدفعات (-)</td><td>{totalPayments:N2}</td><td colspan='2'></td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td colspan='2'>صافي المتبقي للتاجر</td><td>{balance:N2}</td><td colspan='2'></td></tr>");
        sb.AppendLine("</table></body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportTraderStatementToPdfAsync(int traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        await ExportTraderStatementToExcelAsync(traderId, year, month, date, startDate, endDate, filePath);
    }

    #endregion

    #region Employee Payroll Export

    public async Task ExportEmployeePayrollToExcelAsync(int employeeId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        var employee = await _context.Employees.Include(e => e.Branch).AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        var empName = employee?.Name ?? "الموظف";

        var query = _context.EmployeeTransactions.AsNoTracking().Where(t => t.EmployeeId == employeeId);

        if (date.HasValue)
            query = query.Where(t => t.Date == date.Value);
        else
        {
            if (year.HasValue && year.Value > 0)
                query = query.Where(t => t.Date.Year == year.Value);

            if (month.HasValue && month.Value > 0)
                query = query.Where(t => t.Date.Month == month.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= DateOnly.FromDateTime(startDate.Value));

            if (endDate.HasValue)
                query = query.Where(t => t.Date <= DateOnly.FromDateTime(endDate.Value));
        }

        var list = await query.OrderByDescending(t => t.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:Tahoma,Arial;direction:rtl;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #cbd5e1;padding:8px;text-align:right;} th{background-color:#1e293b;color:white;}</style></head><body>");
        sb.AppendLine($"<h2>كشف حساب الموظف: {empName} ({employee?.Position})</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>التاريخ</th><th>نوع الحركة</th><th>المبلغ (ر.ي)</th><th>الملاحظات</th></tr>");

        foreach (var item in list)
        {
            var typeStr = item.Type switch
            {
                EmployeeTransactionType.Salary => "راتب (+)",
                EmployeeTransactionType.Bonus => "مكافأة (+)",
                EmployeeTransactionType.Withdrawal => "سحبية (-)",
                EmployeeTransactionType.Deduction => "خصم / غياب (-)",
                EmployeeTransactionType.Payment => "دفعة ماليّة (-)",
                _ => item.Type.ToString()
            };

            sb.AppendLine($"<tr><td>{item.Date:yyyy/MM/dd}</td><td>{typeStr}</td><td>{item.Amount:N2}</td><td>{item.Notes ?? ""}</td></tr>");
        }

        var entitlements = list.Where(l => l.Type == EmployeeTransactionType.Salary || l.Type == EmployeeTransactionType.Bonus).Sum(l => l.Amount);
        var deductions = list.Where(l => l.Type == EmployeeTransactionType.Withdrawal || l.Type == EmployeeTransactionType.Deduction || l.Type == EmployeeTransactionType.Payment).Sum(l => l.Amount);
        var net = (employee?.OpeningBalance ?? 0) + entitlements - deductions;

        sb.AppendLine($"<tr style='font-weight:bold;'><td colspan='2'>إجمالي المستحقات (رواتب + مكافآت)</td><td>{entitlements:N2}</td><td></td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;'><td colspan='2'>إجمالي السحبيات والخصومات</td><td>{deductions:N2}</td><td></td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td colspan='2'>المتبقي له / عليه</td><td>{net:N2}</td><td></td></tr>");
        sb.AppendLine("</table></body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportEmployeePayrollToPdfAsync(int employeeId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        await ExportEmployeePayrollToExcelAsync(employeeId, year, month, date, startDate, endDate, filePath);
    }

    #endregion

    #region Branch Statement Export

    public async Task ExportBranchStatementToExcelAsync(int branchId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        var branch = await _context.Branches.FindAsync(branchId);
        var branchName = branch?.Name ?? "الفرع";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>body{font-family:Tahoma,Arial;direction:rtl;} table{border-collapse:collapse;width:100%;} th,td{border:1px solid #cbd5e1;padding:8px;text-align:right;} th{background-color:#1e293b;color:white;}</style></head><body>");
        sb.AppendLine($"<h2>كشف حساب الفرع التفصيلي: {branchName}</h2>");

        // Section 1: Sales
        var salesQuery = _context.DailyActivities.AsNoTracking().Where(d => d.BranchId == branchId);
        if (date.HasValue) salesQuery = salesQuery.Where(d => d.Date == date.Value);
        else if (year.HasValue && year.Value > 0) salesQuery = salesQuery.Where(d => d.Date.Year == year.Value);

        var salesList = await salesQuery.OrderByDescending(d => d.Date).ToListAsync();

        sb.AppendLine("<h3>1. النشاط اليومي والمبيعات</h3>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>التاريخ</th><th>المبيعات (ر.ي)</th><th>المصروفات (ر.ي)</th><th>صافي المبيعات (ر.ي)</th></tr>");
        foreach (var s in salesList)
            sb.AppendLine($"<tr><td>{s.Date:yyyy/MM/dd}</td><td>{s.TotalSales:N2}</td><td>{s.TotalExpenses:N2}</td><td>{s.NetSales:N2}</td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td>الإجمالي</td><td>{salesList.Sum(s => s.TotalSales):N2}</td><td>{salesList.Sum(s => s.TotalExpenses):N2}</td><td>{salesList.Sum(s => s.NetSales):N2}</td></tr>");
        sb.AppendLine("</table>");

        // Section 2: Invoices
        var invoiceAllocations = await _context.InvoiceBranchAllocations
            .Include(a => a.Invoice)
                .ThenInclude(i => i.Trader)
            .AsNoTracking()
            .Where(a => a.BranchId == branchId)
            .ToListAsync();

        sb.AppendLine("<h3 style='margin-top:20px;'>2. الفواتير المستلمة للفرع</h3>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>التاريخ</th><th>الفاتورة</th><th>التاجر</th><th>المبلغ المخصص للفرع (ر.ي)</th></tr>");
        foreach (var inv in invoiceAllocations)
            sb.AppendLine($"<tr><td>{inv.Invoice?.Date:yyyy/MM/dd}</td><td>{inv.Invoice?.InvoiceName}</td><td>{inv.Invoice?.Trader?.Name}</td><td>{inv.Amount:N2}</td></tr>");
        sb.AppendLine($"<tr style='font-weight:bold;background-color:#f1f5f9;'><td colspan='3'>إجمالي فواتير الفرع</td><td>{invoiceAllocations.Sum(i => i.Amount):N2}</td></tr>");
        sb.AppendLine("</table></body></html>");

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportBranchStatementToPdfAsync(int branchId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath)
    {
        await ExportBranchStatementToExcelAsync(branchId, year, month, date, startDate, endDate, filePath);
    }

    #endregion
}
