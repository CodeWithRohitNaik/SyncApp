using ThrustSync.Core.Models;

namespace ThrustSync.Core.Repositories;

/// <summary>
/// Repository interface for KpiEntry operations
/// </summary>
public interface IKpiEntryRepository : IRepository<KpiEntry>
{
    /// <summary>Gets KPI entries for a specific workorder, ordered by date descending</summary>
    Task<IEnumerable<KpiEntry>> GetKpiEntriesForWorkOrderAsync(int workOrderId);

    /// <summary>Gets the latest KPI entry for a workorder</summary>
    Task<KpiEntry?> GetLatestKpiEntryAsync(int workOrderId);

    /// <summary>Gets KPI entries within a date range</summary>
    Task<IEnumerable<KpiEntry>> GetKpiEntriesByDateRangeAsync(DateTime fromDate, DateTime toDate);
}
