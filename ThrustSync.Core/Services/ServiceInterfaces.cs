using ThrustSync.Core.Models;

namespace ThrustSync.Core.Services;

/// <summary>
/// Service interface for WorkOrder business logic
/// </summary>
public interface IWorkOrderService
{
    /// <summary>Gets filtered workorders for the binder list view</summary>
    Task<(IEnumerable<WorkOrder> Items, int Total)> GetWorkOrdersAsync(
        int pageNumber = 1, int pageSize = 25,
        string? jcn = null, string? fracpr = null, string? mid = null,
        string? tailNumber = null, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>Gets a single workorder with all details</summary>
    Task<WorkOrder?> GetWorkOrderDetailsAsync(int id);

    /// <summary>Creates or updates a workorder from Oracle data</summary>
    Task<WorkOrder> UpsertWorkOrderAsync(WorkOrder workOrder);

    /// <summary>Gets workorders that haven't been updated in N days</summary>
    Task<IEnumerable<WorkOrder>> GetStaleWorkOrdersAsync(int days = 30);
}

/// <summary>
/// Service interface for KPI entry management
/// </summary>
public interface IKpiService
{
    /// <summary>Saves or updates a KPI entry</summary>
    Task<KpiEntry> SaveKpiEntryAsync(
        int workOrderId, decimal? flightHours, decimal? otherHours,
        decimal? opTime, string enteredBy, string? notes = null);

    /// <summary>Gets KPI history for a workorder</summary>
    Task<IEnumerable<KpiEntry>> GetKpiHistoryAsync(int workOrderId);

    /// <summary>Gets the latest KPI entry for a workorder</summary>
    Task<KpiEntry?> GetLatestKpiAsync(int workOrderId);

    /// <summary>Deletes a KPI entry (soft delete via audit log)</summary>
    Task DeleteKpiEntryAsync(int kpiEntryId, string deletedBy);
}

/// <summary>
/// Service interface for audit trail management
/// </summary>
public interface IAuditService
{
    /// <summary>Logs a change to the audit trail</summary>
    Task LogChangeAsync(
        int workOrderId, string changeType, string fieldName,
        string? oldValue, string? newValue, string changedBy);

    /// <summary>Gets audit history for a workorder</summary>
    Task<IEnumerable<AuditLog>> GetAuditHistoryAsync(int workOrderId);

    /// <summary>Gets audit logs by user</summary>
    Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(string userName, DateTime fromDate, DateTime toDate);
}

/// <summary>
/// Service interface for Excel export functionality
/// </summary>
public interface IExcelExportService
{
    /// <summary>Exports workorders to Excel with annotations</summary>
    Task<byte[]> ExportWorkOrdersAsync(
        IEnumerable<WorkOrder> workOrders, string exportedBy);

    /// <summary>Exports workorder details to Excel</summary>
    Task<byte[]> ExportWorkOrderDetailsAsync(int workOrderId, string exportedBy);
}

/// <summary>
/// Service interface for Oracle connectivity and ETL
/// </summary>
public interface IOracleService
{
    /// <summary>Fetches flagged records from Oracle based on filter criteria</summary>
    Task<List<Dictionary<string, object>>> FetchFlaggedRecordsAsync(
        DateTime? sinceDate = null);

    /// <summary>Transforms Oracle raw data into WorkOrder entities</summary>
    List<WorkOrder> TransformOracleData(List<Dictionary<string, object>> oracleData);

    /// <summary>Tests the connection to Oracle</summary>
    Task<bool> TestConnectionAsync();
}
