using ShopsManagement.Domain.Models;

namespace ShopsManagement.Domain.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int? branchId = null, DateTime? targetDate = null);
}
