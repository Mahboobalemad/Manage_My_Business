using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Models;

namespace ShopsManagement.Domain.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllEmployeesAsync(int? branchId = null, EmployeeStatus? status = null);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> AddEmployeeAsync(Employee employee);
    Task<Employee> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(int id);

    Task<EmployeeSummaryDto> GetEmployeeSummaryAsync(int employeeId);
    Task<List<EmployeeTransaction>> GetEmployeeTransactionsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<EmployeeTransaction> AddTransactionAsync(int employeeId, EmployeeTransactionType type, decimal amount, DateTime date, string? notes = null);
    Task<bool> DeleteTransactionAsync(int transactionId);
}
