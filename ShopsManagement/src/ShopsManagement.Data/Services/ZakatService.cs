using Microsoft.EntityFrameworkCore;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class ZakatService : IZakatService
{
    private readonly AppDbContext _context;

    public ZakatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ZakatTable>> GetAllZakatTablesAsync()
    {
        return await _context.ZakatTables
            .Include(t => t.Entries)
            .AsNoTracking()
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<ZakatTable?> GetZakatTableByIdAsync(int id)
    {
        return await _context.ZakatTables
            .Include(t => t.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<ZakatTable> AddZakatTableAsync(ZakatTable table)
    {
        if (string.IsNullOrWhiteSpace(table.Name))
            throw new ArgumentException("اسم جدول الزكاة مطلوب", nameof(table.Name));

        _context.ZakatTables.Add(table);
        await _context.SaveChangesAsync();
        return table;
    }

    public async Task<ZakatTable> UpdateZakatTableAsync(ZakatTable table)
    {
        var existing = await _context.ZakatTables.FindAsync(table.Id);
        if (existing == null)
            throw new KeyNotFoundException($"جدول الزكاة رقم {table.Id} غير موجود");

        existing.Name = table.Name;
        existing.Date = table.Date;
        existing.Notes = table.Notes;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteZakatTableAsync(int id)
    {
        var table = await _context.ZakatTables
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null) return false;

        _context.ZakatEntries.RemoveRange(table.Entries);
        _context.ZakatTables.Remove(table);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ZakatEntry>> GetEntriesByTableIdAsync(int zakatTableId)
    {
        return await _context.ZakatEntries
            .AsNoTracking()
            .Where(z => z.ZakatTableId == zakatTableId)
            .OrderByDescending(z => z.Date)
            .ThenByDescending(z => z.Id)
            .ToListAsync();
    }

    public async Task<ZakatEntry> AddZakatEntryAsync(ZakatEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ZakatType))
            throw new ArgumentException("نوع الزكاة مطلوب", nameof(entry.ZakatType));

        if (entry.Amount < 0)
            throw new ArgumentException("مبلغ الزكاة لا يمكن أن يكون سالباً", nameof(entry.Amount));

        _context.ZakatEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<ZakatEntry> UpdateZakatEntryAsync(ZakatEntry entry)
    {
        var existing = await _context.ZakatEntries.FindAsync(entry.Id);
        if (existing == null)
            throw new KeyNotFoundException($"قيد الزكاة رقم {entry.Id} غير موجود");

        existing.Date = entry.Date;
        existing.Amount = entry.Amount;
        existing.ZakatType = entry.ZakatType;
        existing.Notes = entry.Notes;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteZakatEntryAsync(int id)
    {
        var entry = await _context.ZakatEntries.FindAsync(id);
        if (entry == null) return false;

        _context.ZakatEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
    }
}
