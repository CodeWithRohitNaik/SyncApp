using ThrustSync.Core.Services;
using ThrustSync.Data.Repositories;
using ThrustSync.ETL.Services;
using Microsoft.Extensions.Logging;
using ThrustSync.Core.Repositories;
using Hangfire;

namespace ThrustSync.ETL.Jobs;

/// <summary>
/// Hangfire background job for Oracle ETL operations
/// Scheduled to run daily to pull and sync flagged engine/APU data
/// </summary>
public class OracleEtlJob
{
    private readonly IOracleService _oracleService;
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<OracleEtlJob> _logger;

    public OracleEtlJob(
        IOracleService oracleService,
        IWorkOrderRepository workOrderRepository,
        ILogger<OracleEtlJob> logger)
    {
        _oracleService = oracleService ?? throw new ArgumentNullException(nameof(oracleService));
        _workOrderRepository = workOrderRepository ?? throw new ArgumentNullException(nameof(workOrderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main ETL job that executes daily to sync Oracle data to Azure SQL
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting Oracle ETL job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Step 1: Test connection
            var connected = await _oracleService.TestConnectionAsync();
            if (!connected)
            {
                _logger.LogError("Failed to connect to Oracle. Job aborted.");
                throw new InvalidOperationException("Oracle connection failed");
            }

            // Step 2: Fetch flagged records from Oracle
            _logger.LogInformation("Fetching flagged records from Oracle");
            var oracleRecords = await _oracleService.FetchFlaggedRecordsAsync();

            if (oracleRecords.Count == 0)
            {
                _logger.LogInformation("No flagged records found in Oracle");
                return;
            }

            _logger.LogInformation("Fetched {Count} records from Oracle", oracleRecords.Count);

            // Step 3: Transform Oracle data
            var workOrders = _oracleService.TransformOracleData(oracleRecords);

            // Step 4: Upsert into Azure SQL with immediate commits (idempotent operation using FRACPR as key)
            _logger.LogInformation("Upserting {Count} work orders into database with immediate commits", workOrders.Count);

            int upsertCount = 0;
            foreach (var workOrder in workOrders)
            {
                try
                {
                    // Check if workorder exists by FRACPR (composite key)
                    var existing = await _workOrderRepository.FirstOrDefaultAsync(
                        w => w.FRACPR == workOrder.FRACPR);

                    if (existing != null)
                    {
                        // Update existing
                        existing.JCN = workOrder.JCN;
                        existing.MID = workOrder.MID;
                        existing.TailNumber = workOrder.TailNumber;
                        existing.OraclePulledOn = DateTime.UtcNow;

                        // Update APU if exists
                        if (workOrder.APU != null && existing.APU != null)
                        {
                            existing.APU.FailureCode = workOrder.APU.FailureCode;
                            existing.APU.RemovalIndicator = workOrder.APU.RemovalIndicator;
                            existing.APU.ReviewStatus = workOrder.APU.ReviewStatus;
                        }

                        _workOrderRepository.Update(existing);
                        _logger.LogDebug("Updated workorder: FRACPR={FRACPR}, JCN={JCN}", workOrder.FRACPR, workOrder.JCN);
                    }
                    else
                    {
                        // Insert new
                        await _workOrderRepository.AddAsync(workOrder);
                        _logger.LogDebug("Created new workorder: FRACPR={FRACPR}, JCN={JCN}", workOrder.FRACPR, workOrder.JCN);
                    }

                    // Commit immediately after each record to free memory and prevent large transaction locks
                    await _workOrderRepository.SaveChangesAsync();
                    upsertCount++;

                    _logger.LogDebug("Committed record {Count}/{Total}", upsertCount, workOrders.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error upserting workorder: FRACPR={FRACPR}", workOrder.FRACPR);
                    // Continue processing other records
                }
            }

            _logger.LogInformation(
                "Oracle ETL job completed successfully: Upserted={Count}, Timestamp={Timestamp}",
                upsertCount, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle ETL job failed with exception");
            throw;
        }
    }

    /// <summary>
    /// Incremental ETL job that only fetches records updated since last run
    /// </summary>
    public async Task ExecuteIncrementalAsync(DateTime? lastRunTime = null)
    {
        lastRunTime = lastRunTime ?? DateTime.UtcNow.AddHours(-24);

        _logger.LogInformation("Starting incremental Oracle ETL job. Last run: {LastRun}", lastRunTime);

        try
        {
            var oracleRecords = await _oracleService.FetchFlaggedRecordsAsync(lastRunTime);

            if (oracleRecords.Count == 0)
            {
                _logger.LogInformation("No new records since last run");
                return;
            }

            var workOrders = _oracleService.TransformOracleData(oracleRecords);

            int updateCount = 0;
            foreach (var workOrder in workOrders)
            {
                try
                {
                    var existing = await _workOrderRepository.FirstOrDefaultAsync(w => w.FRACPR == workOrder.FRACPR);
                    if (existing != null)
                    {
                        existing.OraclePulledOn = DateTime.UtcNow;
                        _workOrderRepository.Update(existing);
                        
                        // Commit immediately after each record
                        await _workOrderRepository.SaveChangesAsync();
                        updateCount++;
                        
                        _logger.LogDebug("Updated record {Count}", updateCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating workorder: FRACPR={FRACPR}", workOrder.FRACPR);
                    // Continue processing other records
                }
            }

            _logger.LogInformation(
                "Incremental Oracle ETL completed: Updated={Count}, Timestamp={Timestamp}",
                updateCount, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental Oracle ETL job failed");
            throw;
        }
    }
}
