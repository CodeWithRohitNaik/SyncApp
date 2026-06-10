using ThrustSync.Core.Models;

namespace ThrustSync.Core.Repositories;

/// <summary>
/// Repository interface for APU operations
/// </summary>
public interface IApuRepository : IRepository<APU>
{
    /// <summary>Gets an APU by its unique composite key (RefDes, PartNumber, PartSerialNumber)</summary>
    Task<APU?> GetByCompositeKeyAsync(string refDes, string partNumber, string partSerialNumber);

    /// <summary>Gets or creates an APU with the given composite key</summary>
    Task<APU> GetOrCreateAsync(string refDes, string partNumber, string partSerialNumber, 
        int? failureCode = null, string? removalIndicator = null, string? reviewStatus = null);
}
