using ThrustSync.Core.Models;
using ThrustSync.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace ThrustSync.Core.Services;

/// <summary>
/// Service implementation for WorkOrder business logic
/// </summary>
public class WorkOrderService : IWorkOrderService
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(IWorkOrderRepository workOrderRepository, ILogger<WorkOrderService> logger)
    {
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(IEnumerable<WorkOrder> Items, int Total)> GetWorkOrdersAsync(
        int pageNumber = 1, int pageSize = 25,
        string? jcn = null, string? fracpr = null, string? mid = null,
        string? tailNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var skip = (pageNumber - 1) * pageSize;
        
        _logger.LogInformation(
            "Fetching workorders: Page={Page}, PageSize={PageSize}, JCN={JCN}, FRACPR={FRACPR}",
            pageNumber, pageSize, jcn, fracpr);

        return await _workOrderRepository.GetFilteredWorkOrdersAsync(
            skip, pageSize, jcn, fracpr, mid, tailNumber, fromDate, toDate);
    }

    public async Task<WorkOrder?> GetWorkOrderDetailsAsync(int id)
    {
        _logger.LogInformation("Fetching workorder details: WorkOrderId={Id}", id);
        return await _workOrderRepository.GetWorkOrderWithDetailsAsync(id);
    }

    public async Task<WorkOrder> UpsertWorkOrderAsync(WorkOrder workOrder)
    {
        if (workOrder == null)
            throw new ArgumentNullException(nameof(workOrder));

        var existing = await _workOrderRepository.FirstOrDefaultAsync(w => w.FRACPR == workOrder.FRACPR);

        if (existing != null)
        {
            _logger.LogInformation("Updating existing workorder: FRACPR={FRACPR}", workOrder.FRACPR);
            existing.JCN = workOrder.JCN;
            existing.MID = workOrder.MID;
            existing.TailNumber = workOrder.TailNumber;
            existing.OraclePulledOn = DateTime.UtcNow;
            _workOrderRepository.Update(existing);
        }
        else
        {
            _logger.LogInformation("Creating new workorder: FRACPR={FRACPR}, JCN={JCN}", workOrder.FRACPR, workOrder.JCN);
            await _workOrderRepository.AddAsync(workOrder);
        }

        await _workOrderRepository.SaveChangesAsync();
        return existing ?? workOrder;
    }

    public async Task<IEnumerable<WorkOrder>> GetStaleWorkOrdersAsync(int days = 30)
    {
        _logger.LogInformation("Fetching stale workorders: Days={Days}", days);
        return await _workOrderRepository.GetWorkOrdersForKpiUpdateAsync(days);
    }
}

/// <summary>
/// Service implementation for KPI entry management
/// </summary>
public class KpiService : IKpiService
{
    private readonly IKpiEntryRepository _kpiRepository;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<KpiService> _logger;

    public KpiService(
        IKpiEntryRepository kpiRepository,
        IWorkOrderRepository workOrderRepository,
        IAuditService auditService,
        ILogger<KpiService> logger)
    {
        _kpiRepository = kpiRepository ?? throw new ArgumentNullException(nameof(kpiRepository));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KpiEntry> SaveKpiEntryAsync(
        int workOrderId, decimal? flightHours, decimal? otherHours,
        decimal? opTime, string enteredBy, string? notes = null)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
        if (workOrder == null)
            throw new InvalidOperationException($"WorkOrder {workOrderId} not found");

        var kpiEntry = new KpiEntry
        {
            WorkOrderId = workOrderId,
            FlightHours = flightHours,
            OtherHours = otherHours,
            OpTime = opTime,
            EnteredBy = enteredBy,
            EnteredOn = DateTime.UtcNow,
            Source = "FMxC2 Manual",
            Notes = notes
        };

        _logger.LogInformation(
            "Saving KPI entry: WorkOrderId={WorkOrderId}, EnteredBy={EnteredBy}",
            workOrderId, enteredBy);

        await _kpiRepository.AddAsync(kpiEntry);
        await _kpiRepository.SaveChangesAsync();

        // Update the workorder's last KPI update timestamp
        workOrder.LastKpiUpdatedOn = DateTime.UtcNow;
        _workOrderRepository.Update(workOrder);
        await _workOrderRepository.SaveChangesAsync();

        // Log to audit trail
        await _auditService.LogChangeAsync(
            workOrderId, "Create", "KpiEntry",
            null, $"FH={flightHours}, OH={otherHours}, OT={opTime}", enteredBy);

        return kpiEntry;
    }

    public async Task<IEnumerable<KpiEntry>> GetKpiHistoryAsync(int workOrderId)
    {
        _logger.LogInformation("Fetching KPI history: WorkOrderId={WorkOrderId}", workOrderId);
        return await _kpiRepository.GetKpiEntriesForWorkOrderAsync(workOrderId);
    }

    public async Task<KpiEntry?> GetLatestKpiAsync(int workOrderId)
    {
        return await _kpiRepository.GetLatestKpiEntryAsync(workOrderId);
    }

    public async Task DeleteKpiEntryAsync(int kpiEntryId, string deletedBy)
    {
        var kpiEntry = await _kpiRepository.GetByIdAsync(kpiEntryId);
        if (kpiEntry == null)
            throw new InvalidOperationException($"KPI Entry {kpiEntryId} not found");

        _logger.LogInformation("Deleting KPI entry: KpiEntryId={KpiEntryId}, DeletedBy={DeletedBy}",
            kpiEntryId, deletedBy);

        _kpiRepository.Remove(kpiEntry);
        await _kpiRepository.SaveChangesAsync();

        // Log to audit trail
        await _auditService.LogChangeAsync(
            kpiEntry.WorkOrderId, "Delete", "KpiEntry",
            $"FH={kpiEntry.FlightHours}, OH={kpiEntry.OtherHours}, OT={kpiEntry.OpTime}",
            null, deletedBy);
    }
}

/// <summary>
/// Service implementation for audit trail management
/// </summary>
public class AuditService : IAuditService
{
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IRepository<AuditLog> auditRepository, ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogChangeAsync(
        int workOrderId, string changeType, string fieldName,
        string? oldValue, string? newValue, string changedBy)
    {
        var auditLog = new AuditLog
        {
            WorkOrderId = workOrderId,
            ChangeType = changeType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedOn = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Logging audit change: WorkOrderId={WorkOrderId}, ChangeType={ChangeType}, Field={Field}, ChangedBy={ChangedBy}",
            workOrderId, changeType, fieldName, changedBy);

        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditHistoryAsync(int workOrderId)
    {
        return await _auditRepository.FindAsync(a => a.WorkOrderId == workOrderId);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(string userName, DateTime fromDate, DateTime toDate)
    {
        return await _auditRepository.FindAsync(
            a => a.ChangedBy == userName && a.ChangedOn >= fromDate && a.ChangedOn <= toDate);
    }
}
