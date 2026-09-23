using ShopsManagement.Domain.Entities;

namespace ShopsManagement.Domain.Services;

public interface IInvoiceService
{
    Task<List<Invoice>> GetAllInvoicesAsync(int? branchId = null, int? traderId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<Invoice?> GetInvoiceByIdAsync(int id);
    Task<Invoice> AddInvoiceAsync(Invoice invoice, List<InvoiceBranchAllocation>? allocations = null);
    Task<Invoice> UpdateInvoiceAsync(Invoice invoice, List<InvoiceBranchAllocation>? allocations = null);
    Task<bool> DeleteInvoiceAsync(int id);
}
