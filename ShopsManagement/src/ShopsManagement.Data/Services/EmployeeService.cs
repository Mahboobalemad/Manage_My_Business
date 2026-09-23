using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;

    public EmployeeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Employee>> GetAllEmployeesAsync(int? branchId = null, EmployeeStatus? status = null)
    {
        // First ensure monthly auto-salaries are processed
        await EnsureMonthlySalariesProcessedAsync();

        var query = _context.Employees
            .Include(e => e.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(e => e.BranchId == branchId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return await query.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _context.Employees
            .Include(e => e.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.Name))
            throw new ArgumentException("اسم الموظف مطلوب", nameof(employee.Name));

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Process auto salary for completed months immediately if salary > 0
        await EnsureMonthlySalariesProcessedAsync();

        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee)
    {
        var existing = await _context.Employees.FindAsync(employee.Id);
        if (existing == null)
            throw new KeyNotFoundException($"الموظف رقم {employee.Id} غير موجود");

        existing.Name = employee.Name;
        existing.Phone = employee.Phone;
        existing.Position = employee.Position;
        existing.BranchId = employee.BranchId;
        existing.Salary = employee.Salary;
        existing.OpeningBalance = employee.OpeningBalance;
        existing.HireDate = employee.HireDate;
        existing.Status = employee.Status;
        existing.Notes = employee.Notes;

        await _context.SaveChangesAsync();

        await EnsureMonthlySalariesProcessedAsync();

        return existing;
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        // Delete all employee transactions
        var txs = await _context.EmployeeTransactions
            .Where(t => t.EmployeeId == id)
            .ToListAsync();
        _context.EmployeeTransactions.RemoveRange(txs);

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EmployeeSummaryDto> GetEmployeeSummaryAsync(int employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            throw new KeyNotFoundException($"الموظف رقم {employeeId} غير موجود");

        var txs = await _context.EmployeeTransactions
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .ToListAsync();

        var totalSalaries = txs.Where(t => t.Type == EmployeeTransactionType.Salary).Sum(t => t.Amount);
        var totalBonuses = txs.Where(t => t.Type == EmployeeTransactionType.Bonus).Sum(t => t.Amount);
        var totalWithdrawals = txs.Where(t => t.Type == EmployeeTransactionType.Withdrawal).Sum(t => t.Amount);
        var totalDeductions = txs.Where(t => t.Type == EmployeeTransactionType.Deduction).Sum(t => t.Amount);
        var totalPayments = txs.Where(t => t.Type == EmployeeTransactionType.Payment).Sum(t => t.Amount);

        return new EmployeeSummaryDto
        {
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            Position = employee.Position,
            BranchName = employee.Branch?.Name,
            BaseSalary = employee.Salary,
            OpeningBalance = employee.OpeningBalance,
            TotalSalaries = totalSalaries,
            TotalBonuses = totalBonuses,
            TotalWithdrawals = totalWithdrawals,
            TotalDeductions = totalDeductions,
            TotalPayments = totalPayments
        };
    }

    public async Task<List<EmployeeTransaction>> GetEmployeeTransactionsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.EmployeeTransactions
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId);

        if (startDate.HasValue)
        {
            var start = DateOnly.FromDateTime(startDate.Value);
            query = query.Where(t => t.Date >= start);
        }

        if (endDate.HasValue)
        {
            var end = DateOnly.FromDateTime(endDate.Value);
            query = query.Where(t => t.Date <= end);
        }

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task<EmployeeTransaction> AddTransactionAsync(int employeeId, EmployeeTransactionType type, decimal amount, DateTime date, string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentException("مبلغ الحركة يجب أن يكون أكبر من صفر", nameof(amount));

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new KeyNotFoundException($"الموظف رقم {employeeId} غير موجود");

        var transaction = new EmployeeTransaction
        {
            EmployeeId = employeeId,
            Date = DateOnly.FromDateTime(date),
            Type = type,
            Amount = amount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        _context.EmployeeTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> DeleteTransactionAsync(int transactionId)
    {
        var tx = await _context.EmployeeTransactions.FindAsync(transactionId);
        if (tx == null) return false;

        _context.EmployeeTransactions.Remove(tx);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// التوليد التلقائي للرواتب الشهرية في نهاية كل شهر انقضى للموظفين النشطين اللذين لديهم راتب أساسي أكبر من صفر
    /// </summary>
    private async Task EnsureMonthlySalariesProcessedAsync()
    {
        var activeEmployeesWithSalary = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active && e.Salary > 0)
            .ToListAsync();

        if (activeEmployeesWithSalary.Count == 0) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        bool hasChanges = false;

        foreach (var emp in activeEmployeesWithSalary)
        {
            // Start checking from hire date month or current year start
            var currentMonthDate = new DateOnly(emp.HireDate.Year, emp.HireDate.Month, 1);

            while (currentMonthDate <= today)
            {
                var daysInMonth = DateTime.DaysInMonth(currentMonthDate.Year, currentMonthDate.Month);
                var endOfMonth = new DateOnly(currentMonthDate.Year, currentMonthDate.Month, daysInMonth);

                // Only auto-post if month has ended or is today at end of month
                if (endOfMonth <= today)
                {
                    // Check if salary transaction already exists for this employee in this month
                    var startOfMonth = new DateOnly(currentMonthDate.Year, currentMonthDate.Month, 1);
                    var exists = await _context.EmployeeTransactions
                        .AnyAsync(t => t.EmployeeId == emp.Id 
                                    && t.Type == EmployeeTransactionType.Salary 
                                    && t.Date >= startOfMonth 
                                    && t.Date <= endOfMonth);

                    if (!exists)
                    {
                        var autoSalaryTx = new EmployeeTransaction
                        {
                            EmployeeId = emp.Id,
                            Date = endOfMonth,
                            Type = EmployeeTransactionType.Salary,
                            Amount = emp.Salary,
                            Notes = $"استحقاق الراتب الشهري التلقائي لـ ({currentMonthDate:MM/yyyy})"
                        };
                        _context.EmployeeTransactions.Add(autoSalaryTx);
                        hasChanges = true;
                    }
                }

                currentMonthDate = currentMonthDate.AddMonths(1);
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }
    }
}
