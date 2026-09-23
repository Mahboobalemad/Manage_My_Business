using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Models;

namespace ShopsManagement.Domain.Services;

public interface IDailyActivityService
{
    Task<List<DailyActivity>> GetActivitiesByBranchAndMonthAsync(int? branchId, int year, int month);
    Task<List<DailyActivity>> GetActivitiesByBranchAsync(int branchId, int? year, int? month);
    Task<DailyActivity?> GetActivityByBranchAndDateAsync(int branchId, DateOnly date);
    Task<DailyActivity> SaveOrUpdateActivityAsync(DailyActivity activity, List<ExpenseItem>? expenseItems = null);
    Task<bool> DeleteActivityAsync(int id);
    Task<MonthlySummaryDto> GetMonthlySummaryAsync(int? branchId, int year, int month);
}
