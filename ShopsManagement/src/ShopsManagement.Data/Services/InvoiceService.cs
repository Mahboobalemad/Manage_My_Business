using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;

    public InvoiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Invoice>> GetAllInvoicesAsync(int? branchId = null, int? traderId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Invoices
            .Include(i => i.Branch)
            .Include(i => i.Trader)
            .Include(i => i.BranchAllocations)
                .ThenInclude(ba => ba.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(i => i.BranchId == branchId.Value || i.BranchAllocations.Any(a => a.BranchId == branchId.Value));
        }

        if (traderId.HasValue && traderId.Value > 0)
            query = query.Where(i => i.TraderId == traderId.Value);

        if (startDate.HasValue)
        {
            var start = DateOnly.FromDateTime(startDate.Value);
            query = query.Where(i => i.Date >= start);
        }

        if (endDate.HasValue)
        {
            var end = DateOnly.FromDateTime(endDate.Value);
            query = query.Where(i => i.Date <= end);
        }

        return await query.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id).ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Branch)
            .Include(i => i.Trader)
            .Include(i => i.BranchAllocations)
                .ThenInclude(ba => ba.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> AddInvoiceAsync(Invoice invoice, List<InvoiceBranchAllocation>? allocations = null)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceName))
            throw new ArgumentException("اسم/رقم الفاتورة مطلوب", nameof(invoice.InvoiceName));

        // Calculate total amount from allocations if provided
        if (allocations != null && allocations.Count > 0)
        {
            invoice.Amount = allocations.Sum(a => a.Amount);
        }

        if (invoice.Amount <= 0)
            throw new ArgumentException("مبلغ الفاتورة الكلي يجب أن يكون أكبر من صفر", nameof(invoice.Amount));

        // Add Invoice
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Add Allocations
        if (allocations != null && allocations.Count > 0)
        {
            foreach (var alloc in allocations)
            {
                alloc.InvoiceId = invoice.Id;
                _context.InvoiceBranchAllocations.Add(alloc);
            }
            await _context.SaveChangesAsync();
        }
        else if (invoice.BranchId.HasValue && invoice.BranchId.Value > 0)
        {
            // Fallback single branch allocation
            _context.InvoiceBranchAllocations.Add(new InvoiceBranchAllocation
            {
                InvoiceId = invoice.Id,
                BranchId = invoice.BranchId.Value,
                Amount = invoice.Amount
            });
            await _context.SaveChangesAsync();
        }

        // Create corresponding Credit (+) TraderTransaction in Ledger
        var traderTransaction = new TraderTransaction
        {
            TraderId = invoice.TraderId,
            Date = invoice.Date,
            Type = TraderTransactionType.Invoice,
            Amount = invoice.Amount,
            RelatedInvoiceId = invoice.Id,
            Notes = $"فاتورة بضاعة رقم: {invoice.InvoiceName}"
        };

        _context.TraderTransactions.Add(traderTransaction);
        await _context.SaveChangesAsync();

        return invoice;
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice, List<InvoiceBranchAllocation>? allocations = null)
    {
        var existing = await _context.Invoices
            .Include(i => i.BranchAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (existing == null)
            throw new KeyNotFoundException($"الفاتورة رقم {invoice.Id} غير موجودة");

        if (allocations != null && allocations.Count > 0)
        {
            invoice.Amount = allocations.Sum(a => a.Amount);
        }

        existing.BranchId = invoice.BranchId;
        existing.TraderId = invoice.TraderId;
        existing.InvoiceName = invoice.InvoiceName;
        existing.Amount = invoice.Amount;
        existing.Date = invoice.Date;
        existing.Notes = invoice.Notes;

        // Update Allocations
        _context.InvoiceBranchAllocations.RemoveRange(existing.BranchAllocations);

        if (allocations != null && allocations.Count > 0)
        {
            foreach (var alloc in allocations)
            {
                alloc.Id = 0;
                alloc.InvoiceId = existing.Id;
                _context.InvoiceBranchAllocations.Add(alloc);
            }
        }
        else if (invoice.BranchId.HasValue && invoice.BranchId.Value > 0)
        {
            _context.InvoiceBranchAllocations.Add(new InvoiceBranchAllocation
            {
                InvoiceId = existing.Id,
                BranchId = invoice.BranchId.Value,
                Amount = invoice.Amount
            });
        }

        // Update linked ledger transaction for trader
        var linkedTx = await _context.TraderTransactions
            .FirstOrDefaultAsync(t => t.RelatedInvoiceId == invoice.Id && t.Type == TraderTransactionType.Invoice);

        if (linkedTx != null)
        {
            linkedTx.TraderId = invoice.TraderId;
            linkedTx.Amount = invoice.Amount;
            linkedTx.Date = invoice.Date;
            linkedTx.Notes = $"فاتورة بضاعة رقم: {invoice.InvoiceName}";
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.BranchAllocations)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return false;

        // Remove linked transactions
        var linkedTxs = await _context.TraderTransactions
            .Where(t => t.RelatedInvoiceId == id)
            .ToListAsync();

        _context.TraderTransactions.RemoveRange(linkedTxs);
        _context.InvoiceBranchAllocations.RemoveRange(invoice.BranchAllocations);
        _context.Invoices.Remove(invoice);

        await _context.SaveChangesAsync();
        return true;
    }
}
