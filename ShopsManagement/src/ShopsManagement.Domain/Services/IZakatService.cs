using ShopsManagement.Domain.Entities;

namespace ShopsManagement.Domain.Services;

public interface IZakatService
{
    Task<List<ZakatTable>> GetAllZakatTablesAsync();
    Task<ZakatTable?> GetZakatTableByIdAsync(int id);
    Task<ZakatTable> AddZakatTableAsync(ZakatTable table);
    Task<ZakatTable> UpdateZakatTableAsync(ZakatTable table);
    Task<bool> DeleteZakatTableAsync(int id);

    Task<List<ZakatEntry>> GetEntriesByTableIdAsync(int zakatTableId);
    Task<ZakatEntry> AddZakatEntryAsync(ZakatEntry entry);
    Task<ZakatEntry> UpdateZakatEntryAsync(ZakatEntry entry);
    Task<bool> DeleteZakatEntryAsync(int id);
}
