using System;
using System.Collections.Generic;

namespace ThrustSync.Web.Models;

/// <summary>
/// ViewModel for work order list display with filtering
/// </summary>
public class WorkOrderListViewModel
{
    /// <summary>List of work orders for current page</summary>
    public List<WorkOrderItemViewModel> WorkOrders { get; set; } = new();

    /// <summary>Total count of matching records</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-based)</summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>Page size</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>Total pages</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filter properties
    public string? JCN { get; set; }
    public string? FRACPR { get; set; }
    public string? MID { get; set; }
    public string? TailNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// ViewModel for individual work order in list
/// </summary>
public class WorkOrderItemViewModel
{
    public int Id { get; set; }
    public string JCN { get; set; } = string.Empty;
    public string FRACPR { get; set; } = string.Empty;
    public string MID { get; set; } = string.Empty;
    public string? TailNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime OraclePulledOn { get; set; }
    public DateTime? LastKpiUpdatedOn { get; set; }
    public string? HMC { get; set; }
    public string? FAC { get; set; }

    // Latest KPI values
    public decimal? FlightHours { get; set; }
    public decimal? OtherHours { get; set; }
    public decimal? OpTime { get; set; }

    // Data source flags
    public string KpiSource { get; set; } = "Oracle";
    public string CssClass { get; set; } = "text-muted"; // gray for Oracle base
}

/// <summary>
/// ViewModel for work order detail view
/// </summary>
public class WorkOrderDetailViewModel
{
    public int Id { get; set; }
    public string JCN { get; set; } = string.Empty;
    public string FRACPR { get; set; } = string.Empty;
    public string MID { get; set; } = string.Empty;
    public string? TailNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime OraclePulledOn { get; set; }
    public DateTime? LastKpiUpdatedOn { get; set; }
    public string? HMC { get; set; }
    public string? FAC { get; set; }

    // APU details (readonly)
    public ApuDetailViewModel? APU { get; set; }

    // KPI entries (most recent first)
    public List<KpiEntryViewModel> KpiEntries { get; set; } = new();

    // Audit history
    public List<AuditLogViewModel> AuditLogs { get; set; } = new();
}

/// <summary>
/// ViewModel for APU detail
/// </summary>
public class ApuDetailViewModel
{
    public int Id { get; set; }
    public string RefDes { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string PartSerialNumber { get; set; } = string.Empty;
    public int? FailureCode { get; set; }
    public string? RemovalIndicator { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for KPI entry
/// </summary>
public class KpiEntryViewModel
{
    public int Id { get; set; }
    public decimal? FlightHours { get; set; }
    public decimal? OtherHours { get; set; }
    public decimal? OpTime { get; set; }
    public string EnteredBy { get; set; } = string.Empty;
    public DateTime EnteredOn { get; set; }
    public string Source { get; set; } = "FMxC2 Manual";
    public string? Notes { get; set; }
    public string CssClass { get; set; } = "table-warning"; // yellow for manual entry
}

/// <summary>
/// ViewModel for creating/editing KPI entry
/// </summary>
public class KpiEntryEditViewModel
{
    public int WorkOrderId { get; set; }
    public decimal? FlightHours { get; set; }
    public decimal? OtherHours { get; set; }
    public decimal? OpTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// ViewModel for audit log entry
/// </summary>
public class AuditLogViewModel
{
    public int Id { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedOn { get; set; }
}

/// <summary>
/// ViewModel for export
/// </summary>
public class ExportViewModel
{
    public int[]? WorkOrderIds { get; set; }
    public string JCN { get; set; } = string.Empty;
    public string FRACPR { get; set; } = string.Empty;
    public string MID { get; set; } = string.Empty;
    public string? TailNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
