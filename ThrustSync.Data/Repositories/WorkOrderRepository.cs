using Microsoft.EntityFrameworkCore;
using ThrustSync.Core.Models;
using ThrustSync.Core.Repositories;
using ThrustSync.Data.Context;

namespace ThrustSync.Data.Repositories;

/// <summary>
/// Repository implementation for WorkOrder operations
/// </summary>
public class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    public WorkOrderRepository(TrustSyncDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<WorkOrder> Items, int Total)> GetFilteredWorkOrdersAsync(
        int skip, int take,
        string? jcn = null,
        string? fracpr = null,
        string? mid = null,
        string? tailNumber = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(jcn))
            query = query.Where(w => w.JCN.Contains(jcn));

        if (!string.IsNullOrWhiteSpace(fracpr))
            query = query.Where(w => w.FRACPR.Contains(fracpr));

        if (!string.IsNullOrWhiteSpace(mid))
            query = query.Where(w => w.MID.Contains(mid));

        if (!string.IsNullOrWhiteSpace(tailNumber))
            query = query.Where(w => w.TailNumber != null && w.TailNumber.Contains(tailNumber));

        if (fromDate.HasValue)
            query = query.Where(w => w.CreatedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(w => w.CreatedDate <= toDate.Value.AddDays(1));

        var total = await query.CountAsync();

        var items = await query
            .Include(w => w.APU)
            .Include(w => w.KpiEntries)
            .OrderByDescending(w => w.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, total);
    }

    public async Task<WorkOrder?> GetWorkOrderWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(w => w.APU)
            .Include(w => w.OracleSnapshot)
            .Include(w => w.KpiEntries.OrderByDescending(k => k.EnteredOn))
            .Include(w => w.AuditLogs.OrderByDescending(a => a.ChangedOn))
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersForKpiUpdateAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        return await _dbSet
            .Where(w => w.LastKpiUpdatedOn == null || w.LastKpiUpdatedOn < cutoffDate)
            .Include(w => w.APU)
            .Include(w => w.KpiEntries)
            .ToListAsync();
    }
}
