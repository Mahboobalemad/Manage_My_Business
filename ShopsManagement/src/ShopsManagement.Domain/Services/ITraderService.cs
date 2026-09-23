using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;

namespace ShopsManagement.Domain.Services;

public interface ITraderService
{
    Task<List<Trader>> GetAllTradersAsync(bool? isActive = null);
    Task<List<TraderSummaryDto>> GetAllTradersSummaryAsync(bool? isActive = null);
    Task<TradersOverallSummaryDto> GetOverallSummaryAsync();
    Task<Trader?> GetTraderByIdAsync(int id);
    Task<Trader> AddTraderAsync(Trader trader);
    Task<Trader> UpdateTraderAsync(Trader trader);
    Task<bool> CanDeleteTraderAsync(int id);
    Task<bool> DeleteTraderAsync(int id);

    Task<TraderSummaryDto> GetTraderSummaryAsync(int traderId);
    Task<List<TraderTransaction>> GetTraderStatementAsync(int traderId, int? year = null, int? month = null, TraderTransactionType? type = null);
    Task<TraderTransaction> AddPaymentAsync(int traderId, decimal amount, DateTime date, int? relatedInvoiceId = null, string? notes = null);
    Task<TraderTransaction> AddTransactionAsync(int traderId, TraderTransactionType type, decimal amount, DateTime date, string? notes = null);
    Task<TraderTransaction> UpdateTransactionAsync(TraderTransaction transaction);
    Task<bool> DeleteTransactionAsync(int transactionId);
}
