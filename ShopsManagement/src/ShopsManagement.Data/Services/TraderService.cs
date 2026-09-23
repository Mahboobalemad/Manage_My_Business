using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class TraderService : ITraderService
{
    private readonly AppDbContext _context;

    public TraderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Trader>> GetAllTradersAsync(bool? isActive = null)
    {
        var query = _context.Traders.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<List<TraderSummaryDto>> GetAllTradersSummaryAsync(bool? isActive = null)
    {
        var query = _context.Traders.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var traders = await query.OrderBy(t => t.Name).ToListAsync();
        var result = new List<TraderSummaryDto>();

        foreach (var trader in traders)
        {
            var txs = await _context.TraderTransactions
                .AsNoTracking()
                .Where(t => t.TraderId == trader.Id)
                .ToListAsync();

            var totalInvoices = txs
                .Where(t => t.Type == TraderTransactionType.Invoice)
                .Sum(t => t.Amount);

            var totalPayments = txs
                .Where(t => t.Type == TraderTransactionType.Payment)
                .Sum(t => t.Amount);

            var invoicesCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(i => i.TraderId == trader.Id);

            result.Add(new TraderSummaryDto
            {
                TraderId = trader.Id,
                TraderName = trader.Name,
                Location = trader.Location,
                BusinessName = trader.BusinessName,
                Phone = trader.Phone,
                IsActive = trader.IsActive,
                Notes = trader.Notes,
                OpeningBalance = trader.OpeningBalance,
                TotalInvoices = totalInvoices,
                TotalPayments = totalPayments,
                InvoicesCount = invoicesCount,
                TransactionsCount = txs.Count
            });
        }

        return result;
    }

    public async Task<TradersOverallSummaryDto> GetOverallSummaryAsync()
    {
        var allTx = await _context.TraderTransactions.AsNoTracking().ToListAsync();
        var totalInvoices = allTx.Where(t => t.Type == TraderTransactionType.Invoice).Sum(t => t.Amount);
        var totalPayments = allTx.Where(t => t.Type == TraderTransactionType.Payment).Sum(t => t.Amount);

        var totalTraders = await _context.Traders.AsNoTracking().CountAsync();
        var activeTraders = await _context.Traders.AsNoTracking().CountAsync(t => t.IsActive);

        return new TradersOverallSummaryDto
        {
            OverallTotalInvoices = totalInvoices,
            OverallTotalPayments = totalPayments,
            ActiveTradersCount = activeTraders,
            TotalTradersCount = totalTraders
        };
    }

    public async Task<Trader?> GetTraderByIdAsync(int id)
    {
        return await _context.Traders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Trader> AddTraderAsync(Trader trader)
    {
        if (string.IsNullOrWhiteSpace(trader.Name))
            throw new ArgumentException("اسم التاجر مطلوب", nameof(trader.Name));

        _context.Traders.Add(trader);
        await _context.SaveChangesAsync();
        return trader;
    }

    public async Task<Trader> UpdateTraderAsync(Trader trader)
    {
        var existing = await _context.Traders.FindAsync(trader.Id);
        if (existing == null)
            throw new KeyNotFoundException($"التاجر رقم {trader.Id} غير موجود");

        existing.Name = trader.Name;
        existing.Location = trader.Location;
        existing.BusinessName = trader.BusinessName;
        existing.Phone = trader.Phone;
        existing.OpeningBalance = trader.OpeningBalance;
        existing.IsActive = trader.IsActive;
        existing.Notes = trader.Notes;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> CanDeleteTraderAsync(int id)
    {
        bool hasInvoices = await _context.Invoices.AnyAsync(i => i.TraderId == id);
        bool hasTxs = await _context.TraderTransactions.AnyAsync(t => t.TraderId == id);
        return !hasInvoices && !hasTxs;
    }

    public async Task<bool> DeleteTraderAsync(int id)
    {
        var trader = await _context.Traders
            .Include(t => t.Transactions)
            .Include(t => t.Invoices)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trader == null) return false;

        // Auto backup before deletion
        await CreateTraderBackupAsync(trader);

        _context.Traders.Remove(trader);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TraderSummaryDto> GetTraderSummaryAsync(int traderId)
    {
        var list = await GetAllTradersSummaryAsync();
        var found = list.FirstOrDefault(t => t.TraderId == traderId);
        if (found == null)
            throw new KeyNotFoundException($"التاجر رقم {traderId} غير موجود");
        return found;
    }

    public async Task<List<TraderTransaction>> GetTraderStatementAsync(int traderId, int? year = null, int? month = null, TraderTransactionType? type = null)
    {
        var query = _context.TraderTransactions
            .Include(t => t.RelatedInvoice)
            .AsNoTracking()
            .Where(t => t.TraderId == traderId);

        if (year.HasValue)
            query = query.Where(t => t.Date.Year == year.Value);

        if (month.HasValue)
            query = query.Where(t => t.Date.Month == month.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task<TraderTransaction> AddPaymentAsync(int traderId, decimal amount, DateTime date, int? relatedInvoiceId = null, string? notes = null)
    {
        return await AddTransactionAsync(traderId, TraderTransactionType.Payment, amount, date, notes);
    }

    public async Task<TraderTransaction> AddTransactionAsync(int traderId, TraderTransactionType type, decimal amount, DateTime date, string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentException("المبلغ يجب أن يكون أكبر من صفر", nameof(amount));

        var trader = await _context.Traders.FindAsync(traderId);
        if (trader == null)
            throw new KeyNotFoundException($"التاجر رقم {traderId} غير موجود");

        var tx = new TraderTransaction
        {
            TraderId = traderId,
            Type = type,
            Amount = amount,
            Date = DateOnly.FromDateTime(date),
            Notes = notes
        };

        _context.TraderTransactions.Add(tx);
        await _context.SaveChangesAsync();
        return tx;
    }

    public async Task<TraderTransaction> UpdateTransactionAsync(TraderTransaction transaction)
    {
        var existing = await _context.TraderTransactions.FindAsync(transaction.Id);
        if (existing == null)
            throw new KeyNotFoundException($"الحركة رقم {transaction.Id} غير موجودة");

        existing.Type = transaction.Type;
        existing.Amount = transaction.Amount;
        existing.Date = transaction.Date;
        existing.Notes = transaction.Notes;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteTransactionAsync(int transactionId)
    {
        var tx = await _context.TraderTransactions.FindAsync(transactionId);
        if (tx == null) return false;

        // Auto backup before deleting transaction
        await CreateTransactionBackupAsync(tx);

        _context.TraderTransactions.Remove(tx);
        await _context.SaveChangesAsync();
        return true;
    }

    private static async Task CreateTraderBackupAsync(Trader trader)
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ShopsManagement_Backups",
                "DeletedTraders");

            Directory.CreateDirectory(backupDir);

            var backupData = new
            {
                DeletedAt = DateTime.Now,
                Trader = new
                {
                    trader.Id,
                    trader.Name,
                    trader.Location,
                    trader.BusinessName,
                    trader.Phone,
                    trader.OpeningBalance,
                    trader.IsActive,
                    trader.Notes,
                    InvoicesCount = trader.Invoices.Count,
                    TransactionsCount = trader.Transactions.Count
                }
            };

            string fileName = $"Trader_{trader.Id}_{trader.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(backupDir, fileName);
            string json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch
        {
            // Non-blocking
        }
    }

    private static async Task CreateTransactionBackupAsync(TraderTransaction tx)
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ShopsManagement_Backups",
                "DeletedTraderTransactions");

            Directory.CreateDirectory(backupDir);

            var backupData = new
            {
                DeletedAt = DateTime.Now,
                Transaction = new
                {
                    tx.Id,
                    tx.TraderId,
                    Type = tx.Type.ToString(),
                    tx.Amount,
                    tx.Date,
                    tx.Notes
                }
            };

            string fileName = $"TraderTx_{tx.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(backupDir, fileName);
            string json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch
        {
            // Non-blocking
        }
    }
}
