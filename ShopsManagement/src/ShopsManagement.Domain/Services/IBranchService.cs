using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Models;

namespace ShopsManagement.Domain.Services;

public interface IBranchService
{
    Task<List<Branch>> GetAllBranchesAsync();
    Task<List<BranchSummaryDto>> GetAllBranchesSummaryAsync();
    Task<Branch?> GetBranchByIdAsync(int id);
    Task<Branch> AddBranchAsync(Branch branch);
    Task<Branch> UpdateBranchAsync(Branch branch);
    Task<bool> CanDeleteBranchAsync(int id);
    Task<bool> DeleteBranchAsync(int id);
}
