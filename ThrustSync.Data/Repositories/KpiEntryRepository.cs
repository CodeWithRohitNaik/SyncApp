using Microsoft.EntityFrameworkCore;
using ThrustSync.Core.Models;
using ThrustSync.Core.Repositories;
using ThrustSync.Data.Context;

namespace ThrustSync.Data.Repositories;

/// <summary>
/// Repository implementation for KpiEntry operations
/// </summary>
public class KpiEntryRepository : Repository<KpiEntry>, IKpiEntryRepository
{
    public KpiEntryRepository(TrustSyncDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<KpiEntry>> GetKpiEntriesForWorkOrderAsync(int workOrderId)
    {
        return await _dbSet
            .Where(k => k.WorkOrderId == workOrderId)
            .OrderByDescending(k => k.EnteredOn)
            .ToListAsync();
    }

    public async Task<KpiEntry?> GetLatestKpiEntryAsync(int workOrderId)
    {
        return await _dbSet
            .Where(k => k.WorkOrderId == workOrderId)
            .OrderByDescending(k => k.EnteredOn)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<KpiEntry>> GetKpiEntriesByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        return await _dbSet
            .Where(k => k.EnteredOn >= fromDate && k.EnteredOn <= toDate)
            .OrderByDescending(k => k.EnteredOn)
            .ToListAsync();
    }
}
