using Microsoft.AspNetCore.Mvc;
using ThrustSync.Core.Services;
using ThrustSync.Web.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ThrustSync.Web.Controllers;

/// <summary>
/// Controller for work order management (binder list view)
/// Handles: list, detail, filters, KPI entry, audit trail, export
/// </summary>
public class WorkOrderController : Controller
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IKpiService _kpiService;
    private readonly IAuditService _auditService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<WorkOrderController> _logger;

    public WorkOrderController(
        IWorkOrderService workOrderService,
        IKpiService kpiService,
        IAuditService auditService,
        IExcelExportService excelExportService,
        ILogger<WorkOrderController> logger)
    {
        _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets paginated list of work orders with optional filters
    /// </summary>
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 25,
        string? jcn = null,
        string? fracpr = null,
        string? mid = null,
        string? tailNumber = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            _logger.LogInformation("Getting work orders list: Page={Page}, PageSize={PageSize}", page, pageSize);

            var (workOrders, total) = await _workOrderService.GetWorkOrdersAsync(
                page, pageSize, jcn, fracpr, mid, tailNumber, fromDate, toDate);

            var viewModel = new WorkOrderListViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = total,
                JCN = jcn,
                FRACPR = fracpr,
                MID = mid,
                TailNumber = tailNumber,
                FromDate = fromDate,
                ToDate = toDate
            };

            foreach (var workOrder in workOrders)
            {
                var latestKpi = workOrder.KpiEntries?.OrderByDescending(k => k.EnteredOn).FirstOrDefault();

                viewModel.WorkOrders.Add(new WorkOrderItemViewModel
                {
                    Id = workOrder.Id,
                    JCN = workOrder.JCN,
                    FRACPR = workOrder.FRACPR,
                    MID = workOrder.MID,
                    TailNumber = workOrder.TailNumber,
                    CreatedDate = workOrder.CreatedDate,
                    OraclePulledOn = workOrder.OraclePulledOn,
                    LastKpiUpdatedOn = workOrder.LastKpiUpdatedOn,
                    HMC = workOrder.HMC,
                    FAC = workOrder.FAC,
                    FlightHours = latestKpi?.FlightHours,
                    OtherHours = latestKpi?.OtherHours,
                    OpTime = latestKpi?.OpTime,
                    KpiSource = latestKpi?.Source ?? "Oracle",
                    CssClass = latestKpi?.Source == "FMxC2 Manual" ? "table-warning" : "text-muted"
                });
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work orders");
            TempData["ErrorMessage"] = "Error retrieving work orders";
            return View(new WorkOrderListViewModel());
        }
    }

    /// <summary>
    /// Gets detailed view of a single work order including KPI history and audit trail
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            _logger.LogInformation("Getting work order details: Id={Id}", id);

            var workOrder = await _workOrderService.GetWorkOrderDetailsAsync(id);
            if (workOrder == null)
                return NotFound();

            var auditLogs = await _auditService.GetAuditHistoryAsync(id);

            var viewModel = new WorkOrderDetailViewModel
            {
                Id = workOrder.Id,
                JCN = workOrder.JCN,
                FRACPR = workOrder.FRACPR,
                MID = workOrder.MID,
                TailNumber = workOrder.TailNumber,
                CreatedDate = workOrder.CreatedDate,
                OraclePulledOn = workOrder.OraclePulledOn,
                LastKpiUpdatedOn = workOrder.LastKpiUpdatedOn,
                HMC = workOrder.HMC,
                FAC = workOrder.FAC
            };

            // Map APU
            if (workOrder.APU != null)
            {
                viewModel.APU = new ApuDetailViewModel
                {
                    Id = workOrder.APU.Id,
                    RefDes = workOrder.APU.RefDes,
                    PartNumber = workOrder.APU.PartNumber,
                    PartSerialNumber = workOrder.APU.PartSerialNumber,
                    FailureCode = workOrder.APU.FailureCode,
                    RemovalIndicator = workOrder.APU.RemovalIndicator,
                    ReviewStatus = workOrder.APU.ReviewStatus
                };
            }

            // Map KPI entries
            foreach (var kpiEntry in workOrder.KpiEntries?.OrderByDescending(k => k.EnteredOn) ?? Enumerable.Empty<Core.Models.KpiEntry>())
            {
                viewModel.KpiEntries.Add(new KpiEntryViewModel
                {
                    Id = kpiEntry.Id,
                    FlightHours = kpiEntry.FlightHours,
                    OtherHours = kpiEntry.OtherHours,
                    OpTime = kpiEntry.OpTime,
                    EnteredBy = kpiEntry.EnteredBy,
                    EnteredOn = kpiEntry.EnteredOn,
                    Source = kpiEntry.Source,
                    Notes = kpiEntry.Notes,
                    CssClass = kpiEntry.Source == "FMxC2 Manual" ? "table-warning" : "text-muted"
                });
            }

            // Map audit logs
            foreach (var auditLog in auditLogs.OrderByDescending(a => a.ChangedOn))
            {
                viewModel.AuditLogs.Add(new AuditLogViewModel
                {
                    Id = auditLog.Id,
                    ChangeType = auditLog.ChangeType,
                    FieldName = auditLog.FieldName,
                    OldValue = auditLog.OldValue,
                    NewValue = auditLog.NewValue,
                    ChangedBy = auditLog.ChangedBy,
                    ChangedOn = auditLog.ChangedOn
                });
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work order detail: Id={Id}", id);
            TempData["ErrorMessage"] = "Error retrieving work order";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Saves a new KPI entry for a work order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveKpi(int id, [FromForm] KpiEntryEditViewModel model)
    {
        try
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "KPI entry data is required";
                return RedirectToAction("Detail", new { id });
            }

            var currentUser = User.Identity?.Name ?? "System";

            _logger.LogInformation("Saving KPI entry: WorkOrderId={Id}, EnteredBy={User}", id, currentUser);

            var kpiEntry = await _kpiService.SaveKpiEntryAsync(
                id,
                model.FlightHours,
                model.OtherHours,
                model.OpTime,
                currentUser,
                model.Notes);

            TempData["SuccessMessage"] = "KPI entry saved successfully";
            return RedirectToAction("Detail", new { id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Detail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving KPI entry: WorkOrderId={Id}", id);
            TempData["ErrorMessage"] = "Error saving KPI entry";
            return RedirectToAction("Detail", new { id });
        }
    }

    /// <summary>
    /// Exports work orders to Excel
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Export([FromForm] ExportViewModel model)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "System";

            _logger.LogInformation("Exporting work orders to Excel: ExportedBy={User}", currentUser);

            var (workOrders, _) = await _workOrderService.GetWorkOrdersAsync(
                1, 10000, model.JCN, model.FRACPR, model.MID, model.TailNumber, model.FromDate, model.ToDate);

            var excelBytes = await _excelExportService.ExportWorkOrdersAsync(workOrders, currentUser);

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"WorkOrders_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting work orders");
            TempData["ErrorMessage"] = "Error exporting work orders";
            return RedirectToAction("Index");
        }
    }
}
