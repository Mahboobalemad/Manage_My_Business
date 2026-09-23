using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class DailyActivityService : IDailyActivityService
{
    private readonly AppDbContext _context;

    public DailyActivityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DailyActivity>> GetActivitiesByBranchAndMonthAsync(int? branchId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var query = _context.DailyActivities
            .Include(d => d.Branch)
            .Include(d => d.ExpenseItems)
            .AsNoTracking()
            .Where(d => d.Date >= startDate && d.Date <= endDate);

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(d => d.BranchId == branchId.Value);
        }

        return await query.OrderBy(d => d.Date).ThenBy(d => d.BranchId).ToListAsync();
    }

    public async Task<List<DailyActivity>> GetActivitiesByBranchAsync(int branchId, int? year, int? month)
    {
        var query = _context.DailyActivities
            .Include(d => d.Branch)
            .Include(d => d.ExpenseItems)
            .AsNoTracking()
            .Where(d => d.BranchId == branchId);

        if (year.HasValue)
            query = query.Where(d => d.Date.Year == year.Value);

        if (month.HasValue)
            query = query.Where(d => d.Date.Month == month.Value);

        return await query.OrderByDescending(d => d.Date).ToListAsync();
    }

    public async Task<DailyActivity?> GetActivityByBranchAndDateAsync(int branchId, DateOnly date)
    {
        return await _context.DailyActivities
            .Include(d => d.Branch)
            .Include(d => d.ExpenseItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.BranchId == branchId && d.Date == date);
    }

    public async Task<DailyActivity> SaveOrUpdateActivityAsync(DailyActivity activity, List<ExpenseItem>? expenseItems = null)
    {
        if (expenseItems != null && expenseItems.Count > 0)
        {
            activity.TotalExpenses = expenseItems.Sum(e => e.Amount);
        }

        DailyActivity? existing = null;
        if (activity.Id > 0)
        {
            existing = await _context.DailyActivities
                .Include(d => d.ExpenseItems)
                .FirstOrDefaultAsync(d => d.Id == activity.Id);
        }
        else
        {
            existing = await _context.DailyActivities
                .Include(d => d.ExpenseItems)
                .FirstOrDefaultAsync(d => d.BranchId == activity.BranchId && d.Date == activity.Date);
        }

        if (existing == null)
        {
            _context.DailyActivities.Add(activity);
            if (expenseItems != null && expenseItems.Count > 0)
            {
                foreach (var item in expenseItems)
                {
                    item.DailyActivityId = activity.Id;
                    _context.ExpenseItems.Add(item);
                }
            }
        }
        else
        {
            existing.BranchId = activity.BranchId;
            existing.Date = activity.Date;
            existing.TotalSales = activity.TotalSales;
            existing.TotalExpenses = activity.TotalExpenses;
            existing.NetProfit = activity.NetProfit;
            existing.Notes = activity.Notes;

            if (expenseItems != null)
            {
                _context.ExpenseItems.RemoveRange(existing.ExpenseItems);
                foreach (var item in expenseItems)
                {
                    item.DailyActivityId = existing.Id;
                    _context.ExpenseItems.Add(item);
                }
            }
            activity = existing;
        }

        await _context.SaveChangesAsync();
        return activity;
    }

    public async Task<bool> DeleteActivityAsync(int id)
    {
        var activity = await _context.DailyActivities.FindAsync(id);
        if (activity == null) return false;

        _context.DailyActivities.Remove(activity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MonthlySummaryDto> GetMonthlySummaryAsync(int? branchId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var query = _context.DailyActivities
            .AsNoTracking()
            .Where(d => d.Date >= startDate && d.Date <= endDate);

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(d => d.BranchId == branchId.Value);
        }

        var activities = await query.ToListAsync();

        return new MonthlySummaryDto
        {
            BranchId = branchId ?? 0,
            Year = year,
            Month = month,
            TotalSales = activities.Sum(a => a.TotalSales),
            TotalExpenses = activities.Sum(a => a.TotalExpenses),
            TotalProfits = activities.Sum(a => a.DisplayProfit),
            TotalDaysRecorded = activities.Count
        };
    }
}
