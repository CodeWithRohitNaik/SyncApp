namespace ThrustSync.Core.Models;

/// <summary>
/// Represents a work order for an engine/APU flagged for review
/// </summary>
public class WorkOrder
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>Job Control Number (JCN) - unique identifier</summary>
    public string JCN { get; set; } = string.Empty;
    
    /// <summary>FAA Form Release part number identifier</summary>
    public string FRACPR { get; set; } = string.Empty;
    
    /// <summary>Maintenance Item Description</summary>
    public string MID { get; set; } = string.Empty;
    
    /// <summary>Aircraft tail number</summary>
    public string? TailNumber { get; set; }
    
    /// <summary>Date the work order was created</summary>
    public DateTime CreatedDate { get; set; }
    
    /// <summary>Last time KPI was updated</summary>
    public DateTime? LastKpiUpdatedOn { get; set; }
    
    /// <summary>Timestamp when data was pulled from Oracle</summary>
    public DateTime OraclePulledOn { get; set; }
    
    /// <summary>Navigation property to APU</summary>
    public virtual APU? APU { get; set; }
    
    /// <summary>Navigation property to Oracle snapshot</summary>
    public virtual OracleSnapshot? OracleSnapshot { get; set; }
    
    /// <summary>Navigation property to KPI entries</summary>
    public virtual ICollection<KpiEntry> KpiEntries { get; set; } = new List<KpiEntry>();
    
    /// <summary>Navigation property to audit trail entries</summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Represents an engine or APU component flagged for review
/// </summary>
public class APU
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>Reference designation (e.g., 4910AA001)</summary>
    public string RefDes { get; set; } = string.Empty;
    
    /// <summary>Part number</summary>
    public string PartNumber { get; set; } = string.Empty;
    
    /// <summary>Part serial number</summary>
    public string PartSerialNumber { get; set; } = string.Empty;
    
    /// <summary>Failure code (numeric)</summary>
    public int? FailureCode { get; set; }
    
    /// <summary>Removal indicator (R, S, or null)</summary>
    public string? RemovalIndicator { get; set; }
    
    /// <summary>Review status (C=Complete, Y=Yes, etc.)</summary>
    public string ReviewStatus { get; set; } = string.Empty;
    
    /// <summary>Foreign key to WorkOrder</summary>
    public int WorkOrderId { get; set; }
    
    /// <summary>Navigation property to WorkOrder</summary>
    public virtual WorkOrder? WorkOrder { get; set; }
}

/// <summary>
/// Stores the raw Oracle snapshot data for traceability
/// </summary>
public class OracleSnapshot
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>Timestamp when data was pulled from Oracle</summary>
    public DateTime PulledOn { get; set; }
    
    /// <summary>Raw JSON snapshot of Oracle data</summary>
    public string SourceData { get; set; } = string.Empty;
    
    /// <summary>Foreign key to WorkOrder</summary>
    public int WorkOrderId { get; set; }
    
    /// <summary>Navigation property to WorkOrder</summary>
    public virtual WorkOrder? WorkOrder { get; set; }
}

/// <summary>
/// Represents manually entered KPI values (FlightHours, OtherHours, OpTime)
/// </summary>
public class KpiEntry
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>Flight hours value</summary>
    public decimal? FlightHours { get; set; }
    
    /// <summary>Other hours value</summary>
    public decimal? OtherHours { get; set; }
    
    /// <summary>Operating time value</summary>
    public decimal? OpTime { get; set; }
    
    /// <summary>User who entered the data</summary>
    public string EnteredBy { get; set; } = string.Empty;
    
    /// <summary>Timestamp when the data was entered</summary>
    public DateTime EnteredOn { get; set; }
    
    /// <summary>Source of the entry (FMxC2 Manual, Oracle, Import)</summary>
    public string Source { get; set; } = "FMxC2 Manual";
    
    /// <summary>Notes or comments</summary>
    public string? Notes { get; set; }
    
    /// <summary>Foreign key to WorkOrder</summary>
    public int WorkOrderId { get; set; }
    
    /// <summary>Navigation property to WorkOrder</summary>
    public virtual WorkOrder? WorkOrder { get; set; }
}

/// <summary>
/// Audit log for tracking changes to KPI entries
/// </summary>
public class AuditLog
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>Type of change (Create, Update, Delete)</summary>
    public string ChangeType { get; set; } = string.Empty;
    
    /// <summary>Name of the field that was changed</summary>
    public string? FieldName { get; set; }
    
    /// <summary>Previous value</summary>
    public string? OldValue { get; set; }
    
    /// <summary>New value</summary>
    public string? NewValue { get; set; }
    
    /// <summary>User who made the change</summary>
    public string ChangedBy { get; set; } = string.Empty;
    
    /// <summary>Timestamp of the change</summary>
    public DateTime ChangedOn { get; set; }
    
    /// <summary>Foreign key to WorkOrder</summary>
    public int WorkOrderId { get; set; }
    
    /// <summary>Navigation property to WorkOrder</summary>
    public virtual WorkOrder? WorkOrder { get; set; }
}

/// <summary>
/// Export history record for tracking Excel exports
/// </summary>
public class ExportLog
{
    /// <summary>Primary key</summary>
    public int Id { get; set; }
    
    /// <summary>User who requested the export</summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>Timestamp of the export</summary>
    public DateTime ExportedOn { get; set; }
    
    /// <summary>Number of records exported</summary>
    public int RecordCount { get; set; }
    
    /// <summary>Filter criteria used for the export</summary>
    public string? FilterCriteria { get; set; }
}
