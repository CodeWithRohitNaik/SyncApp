using ThrustSync.Core.Models;

namespace ThrustSync.Core.Repositories;

/// <summary>
/// Repository interface for WorkOrder operations
/// </summary>
public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    /// <summary>Gets workorders with filters for the binder list view</summary>
    Task<(IEnumerable<WorkOrder> Items, int Total)> GetFilteredWorkOrdersAsync(
        int skip, int take,
        string? jcn = null,
        string? fracpr = null,
        string? mid = null,
        string? tailNumber = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>Gets a workorder with all related data</summary>
    Task<WorkOrder?> GetWorkOrderWithDetailsAsync(int id);

    /// <summary>Gets workorders that need KPI updates</summary>
    Task<IEnumerable<WorkOrder>> GetWorkOrdersForKpiUpdateAsync(int days = 30);
}
