namespace ShopsManagement.Domain.Services;

public interface IReportExportService
{
    Task ExportDailyActivitiesToExcelAsync(int? branchId, int? year, int? month, DateOnly? date, string filePath);
    Task ExportDailyActivitiesToPdfAsync(int? branchId, int? year, int? month, DateOnly? date, string filePath);

    Task ExportInvoicesToExcelAsync(int? branchId, int? traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);
    Task ExportInvoicesToPdfAsync(int? branchId, int? traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);

    Task ExportTraderStatementToExcelAsync(int traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);
    Task ExportTraderStatementToPdfAsync(int traderId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);

    Task ExportEmployeePayrollToExcelAsync(int employeeId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);
    Task ExportEmployeePayrollToPdfAsync(int employeeId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);

    Task ExportBranchStatementToExcelAsync(int branchId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);
    Task ExportBranchStatementToPdfAsync(int branchId, int? year, int? month, DateOnly? date, DateTime? startDate, DateTime? endDate, string filePath);
}
