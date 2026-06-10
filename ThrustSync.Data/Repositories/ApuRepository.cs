using Microsoft.EntityFrameworkCore;
using ThrustSync.Core.Models;
using ThrustSync.Core.Repositories;
using ThrustSync.Data.Context;

namespace ThrustSync.Data.Repositories;

/// <summary>
/// Repository implementation for APU operations
/// </summary>
public class ApuRepository : Repository<APU>, IApuRepository
{
    public ApuRepository(TrustSyncDbContext context) : base(context)
    {
    }

    public async Task<APU?> GetByCompositeKeyAsync(string refDes, string partNumber, string partSerialNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(a =>
            a.RefDes == refDes &&
            a.PartNumber == partNumber &&
            a.PartSerialNumber == partSerialNumber);
    }

    public async Task<APU> GetOrCreateAsync(string refDes, string partNumber, string partSerialNumber,
        int? failureCode = null, string? removalIndicator = null, string? reviewStatus = null)
    {
        // Try to find existing APU
        var existingApu = await GetByCompositeKeyAsync(refDes, partNumber, partSerialNumber);
        
        if (existingApu != null)
        {
            // Update properties if provided
            if (failureCode.HasValue)
                existingApu.FailureCode = failureCode;
            if (!string.IsNullOrEmpty(removalIndicator))
                existingApu.RemovalIndicator = removalIndicator;
            if (!string.IsNullOrEmpty(reviewStatus))
                existingApu.ReviewStatus = reviewStatus;
            
            Update(existingApu);
            return existingApu;
        }

        // Create new APU
        var newApu = new APU
        {
            RefDes = refDes,
            PartNumber = partNumber,
            PartSerialNumber = partSerialNumber,
            FailureCode = failureCode,
            RemovalIndicator = removalIndicator,
            ReviewStatus = reviewStatus ?? string.Empty
        };

        await AddAsync(newApu);
        return newApu;
    }
}
