namespace ThrustSync.Core.Enums;

/// <summary>
/// Represents the source of KPI data entry
/// </summary>
public enum KpiSource
{
    /// <summary>Manual entry from FMxC2 application</summary>
    FMxC2Manual = 1,
    
    /// <summary>Data pulled from Oracle system</summary>
    Oracle = 2,
    
    /// <summary>Imported from external source</summary>
    Import = 3
}

/// <summary>
/// Represents Oracle review status
/// </summary>
public enum ReviewStatus
{
    /// <summary>Complete</summary>
    Complete = 1,
    
    /// <summary>Yes - approved</summary>
    Yes = 2,
    
    /// <summary>No - rejected</summary>
    No = 3,
    
    /// <summary>Pending</summary>
    Pending = 4
}

/// <summary>
/// Represents color coding for UI display
/// </summary>
public enum DataSourceColor
{
    /// <summary>Gray - Oracle base data (readonly)</summary>
    OracleBase = 1,
    
    /// <summary>Blue - Oracle data that was updated</summary>
    OracleUpdated = 2,
    
    /// <summary>Yellow - KPI manual entry</summary>
    KpiManual = 3
}
