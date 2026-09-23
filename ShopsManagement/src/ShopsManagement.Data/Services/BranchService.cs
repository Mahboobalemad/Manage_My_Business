using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;
using System.Text.Json;

namespace ShopsManagement.Data.Services;

public class BranchService : IBranchService
{
    private readonly AppDbContext _context;

    public BranchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Branch>> GetAllBranchesAsync()
    {
        return await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<List<BranchSummaryDto>> GetAllBranchesSummaryAsync()
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .ToListAsync();

        var result = new List<BranchSummaryDto>();

        foreach (var branch in branches)
        {
            // Net Sales: sum all DailyActivity NetSales (TotalSales - TotalExpenses)
            var dailyActivities = await _context.DailyActivities
                .AsNoTracking()
                .Where(d => d.BranchId == branch.Id)
                .ToListAsync();

            decimal netSalesTotal = dailyActivities.Sum(d => d.TotalSales - d.TotalExpenses);

            // Total Invoices: sum from branch allocations + legacy invoices
            var allocationTotal = await _context.InvoiceBranchAllocations
                .AsNoTracking()
                .Where(a => a.BranchId == branch.Id)
                .SumAsync(a => a.Amount);

            var legacyTotal = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.BranchId == branch.Id && !i.BranchAllocations.Any())
                .SumAsync(i => i.Amount);

            decimal totalInvoices = allocationTotal + legacyTotal;

            // Employees balance
            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.BranchId == branch.Id)
                .ToListAsync();

            decimal employeesPositiveBalance = 0m;
            decimal employeesNegativeBalance = 0m;

            foreach (var emp in employees)
            {
                var txs = await _context.EmployeeTransactions
                    .AsNoTracking()
                    .Where(t => t.EmployeeId == emp.Id)
                    .ToListAsync();

                var totalSalaries = txs.Where(t => t.Type == EmployeeTransactionType.Salary).Sum(t => t.Amount);
                var totalBonuses = txs.Where(t => t.Type == EmployeeTransactionType.Bonus).Sum(t => t.Amount);
                var totalWithdrawals = txs.Where(t => t.Type == EmployeeTransactionType.Withdrawal).Sum(t => t.Amount);
                var totalDeductions = txs.Where(t => t.Type == EmployeeTransactionType.Deduction).Sum(t => t.Amount);
                var totalPayments = txs.Where(t => t.Type == EmployeeTransactionType.Payment).Sum(t => t.Amount);

                decimal net = emp.OpeningBalance + totalSalaries + totalBonuses
                              - (totalWithdrawals + totalDeductions + totalPayments);

                if (net > 0)
                    employeesPositiveBalance += net;
                else
                    employeesNegativeBalance += Math.Abs(net);
            }

            result.Add(new BranchSummaryDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                Address = branch.Address,
                RecipientName = branch.RecipientName,
                Phone = branch.Phone,
                IsActive = branch.IsActive,
                OpeningBalance = branch.OpeningBalance,
                NetSalesTotal = netSalesTotal,
                TotalInvoices = totalInvoices,
                EmployeesPositiveBalance = employeesPositiveBalance,
                EmployeesNegativeBalance = employeesNegativeBalance
            });
        }

        return result;
    }

    public async Task<Branch?> GetBranchByIdAsync(int id)
    {
        return await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Branch> AddBranchAsync(Branch branch)
    {
        if (string.IsNullOrWhiteSpace(branch.Name))
            throw new ArgumentException("اسم الفرع مطلوب", nameof(branch.Name));

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
        return branch;
    }

    public async Task<Branch> UpdateBranchAsync(Branch branch)
    {
        var existing = await _context.Branches.FindAsync(branch.Id);
        if (existing == null)
            throw new KeyNotFoundException($"الفرع رقم {branch.Id} غير موجود");

        existing.Name = branch.Name;
        existing.Address = branch.Address;
        existing.RecipientName = branch.RecipientName;
        existing.Phone = branch.Phone;
        existing.OpeningBalance = branch.OpeningBalance;
        existing.IsActive = branch.IsActive;
        existing.Notes = branch.Notes;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> CanDeleteBranchAsync(int id)
    {
        bool hasEmployees = await _context.Employees.AnyAsync(e => e.BranchId == id);
        bool hasInvoices = await _context.Invoices.AnyAsync(i => i.BranchId == id) || await _context.InvoiceBranchAllocations.AnyAsync(a => a.BranchId == id);
        return !hasEmployees && !hasInvoices;
    }

    public async Task<bool> DeleteBranchAsync(int id)
    {
        // Check constraints first
        bool hasEmployees = await _context.Employees.AnyAsync(e => e.BranchId == id);
        if (hasEmployees)
            throw new InvalidOperationException("لا يمكن حذف الفرع لأنه مرتبط بموظفين. يرجى نقل الموظفين أو حذفهم أولاً.");

        bool hasInvoices = await _context.Invoices.AnyAsync(i => i.BranchId == id) || await _context.InvoiceBranchAllocations.AnyAsync(a => a.BranchId == id);
        if (hasInvoices)
            throw new InvalidOperationException("لا يمكن حذف الفرع لأنه مرتبط بفواتير مسجلة. يرجى حذف الفواتير أولاً.");

        var branch = await _context.Branches
            .Include(b => b.DailyActivities)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null) return false;

        // Create a local backup of the branch data before deletion
        await CreateBranchBackupAsync(branch);

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();
        return true;
    }

    private static async Task CreateBranchBackupAsync(Branch branch)
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ShopsManagement_Backups",
                "DeletedBranches");

            Directory.CreateDirectory(backupDir);

            var backupData = new
            {
                DeletedAt = DateTime.Now,
                Branch = new
                {
                    branch.Id,
                    branch.Name,
                    branch.Address,
                    branch.RecipientName,
                    branch.Phone,
                    branch.OpeningBalance,
                    branch.IsActive,
                    branch.Notes,
                    DailyActivitiesCount = branch.DailyActivities.Count
                }
            };

            string fileName = $"Branch_{branch.Id}_{branch.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(backupDir, fileName);
            string json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch
        {
            // Backup failure should not stop deletion; silently continue
        }
    }
}
