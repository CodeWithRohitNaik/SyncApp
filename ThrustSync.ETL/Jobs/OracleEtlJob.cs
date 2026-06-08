using ThrustSync.Core.Services;
using ThrustSync.Data.Repositories;
using ThrustSync.ETL.Services;
using Microsoft.Extensions.Logging;
using ThrustSync.Core.Repositories;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using System.Text.Json;

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
    public async Task ExecuteAsync(PerformContext context)
    {
        var timestamp = DateTime.UtcNow;
        context.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] Starting Oracle ETL job", ConsoleTextColor.Cyan);
        _logger.LogInformation("Starting Oracle ETL job at {Timestamp}", timestamp);

        try
        {
            // Step 1: Test connection
            context.WriteLine("Step 1: Testing Oracle connection...", ConsoleTextColor.Yellow);
            var connected = await _oracleService.TestConnectionAsync();
            
            if (!connected)
            {
                context.WriteLine("ERROR: Failed to connect to Oracle. Job aborted.", ConsoleTextColor.Red);
                _logger.LogError("Failed to connect to Oracle. Job aborted.");
                throw new InvalidOperationException("Oracle connection failed");
            }
            
            context.WriteLine("✓ Oracle connection successful", ConsoleTextColor.Green);

            // Step 2: Fetch flagged records from Oracle
            context.WriteLine("Step 2: Fetching flagged records from Oracle...", ConsoleTextColor.Yellow);
            var oracleRecords = await _oracleService.FetchFlaggedRecordsAsync();

            if (oracleRecords.Count == 0)
            {
                context.WriteLine("✓ No flagged records found in Oracle", ConsoleTextColor.Green);
                _logger.LogInformation("No flagged records found in Oracle");
                return;
            }

            context.WriteLine($"✓ Fetched {oracleRecords.Count} records from Oracle", ConsoleTextColor.Green);
            _logger.LogInformation("Fetched {Count} records from Oracle", oracleRecords.Count);

            // Step 3: Transform Oracle data
            context.WriteLine("Step 3: Transforming Oracle data...", ConsoleTextColor.Yellow);
            var workOrders = _oracleService.TransformOracleData(oracleRecords);
            context.WriteLine($"✓ Transformed {workOrders.Count} records into work orders", ConsoleTextColor.Green);
            _logger.LogInformation("Transformed {Count} Oracle records into {WorkOrderCount} work orders", oracleRecords.Count, workOrders.Count);

            // Step 4: Upsert into Azure SQL with immediate commits
            context.WriteLine($"Step 4: Upserting {workOrders.Count} work orders into database with immediate commits", ConsoleTextColor.Yellow);
            _logger.LogInformation("Upserting {Count} work orders into database with immediate commits", workOrders.Count);

            int upsertCount = 0;
            int errorCount = 0;
            int updateCount = 0;
            int insertCount = 0;
            
            foreach (var workOrder in workOrders)
            {
                try
                {
                    // Log each record being processed
                    _logger.LogDebug("Processing record {Current}/{Total}: FRACPR={FRACPR}", upsertCount + 1, workOrders.Count, workOrder.FRACPR);
                    
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
                        updateCount++;
                        _logger.LogDebug("UPDATED: FRACPR={FRACPR}, JCN={JCN}", workOrder.FRACPR, workOrder.JCN);
                    }
                    else
                    {
                        // Insert new
                        await _workOrderRepository.AddAsync(workOrder);
                        insertCount++;
                        _logger.LogDebug("INSERTED: FRACPR={FRACPR}, JCN={JCN}", workOrder.FRACPR, workOrder.JCN);
                    }

                    // Commit immediately after each record to free memory and prevent large transaction locks
                    await _workOrderRepository.SaveChangesAsync();
                    upsertCount++;

                    if (upsertCount % 100 == 0)
                    {
                        context.WriteLine($"  Progress: {upsertCount}/{workOrders.Count} records processed ({insertCount} inserted, {updateCount} updated)", ConsoleTextColor.Cyan);
                        _logger.LogInformation("Progress: {Current}/{Total} processed - Inserted: {Inserted}, Updated: {Updated}", upsertCount, workOrders.Count, insertCount, updateCount);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMsg = $"ERROR: Record {upsertCount + 1}/{workOrders.Count} - FRACPR={workOrder.FRACPR} - {ex.Message}";
                    context.WriteLine(errorMsg, ConsoleTextColor.Red);
                    _logger.LogError(ex, "Error upserting workorder: FRACPR={FRACPR}, Message={Message}", workOrder.FRACPR, ex.Message);
                    
                    // Continue processing other records instead of breaking
                }
            }

            context.WriteLine($"", ConsoleTextColor.White);
            var completionMsg = $"✓ Oracle ETL job completed successfully: Total Upserted={upsertCount} (Inserted={insertCount}, Updated={updateCount}), Errors={errorCount}, Timestamp={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            context.WriteLine(completionMsg, ConsoleTextColor.Green);
            _logger.LogInformation(
                "Oracle ETL job completed successfully: Upserted={Count}, Inserted={InsertCount}, Updated={UpdateCount}, Errors={ErrorCount}, Timestamp={Timestamp}",
                upsertCount, insertCount, updateCount, errorCount, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            var errorMsg = $"CRITICAL ERROR: Oracle ETL job failed with exception: {ex.Message}";
            context.WriteLine(errorMsg, ConsoleTextColor.Red);
            _logger.LogError(ex, "Oracle ETL job failed with exception");
            throw;
        }
    }

    /// <summary>
    /// Incremental ETL job that only fetches records updated since last run
    /// </summary>
    public async Task ExecuteIncrementalAsync(DateTime? lastRunTime, PerformContext context)
    {
        lastRunTime = lastRunTime ?? DateTime.UtcNow.AddHours(-24);

        context.WriteLine($"Starting incremental Oracle ETL job. Last run: {lastRunTime:yyyy-MM-dd HH:mm:ss}", ConsoleTextColor.Cyan);
        _logger.LogInformation("Starting incremental Oracle ETL job. Last run: {LastRun}", lastRunTime);

        try
        {
            context.WriteLine("Fetching records modified since last run...", ConsoleTextColor.Yellow);
            var oracleRecords = await _oracleService.FetchFlaggedRecordsAsync(lastRunTime);

            if (oracleRecords.Count == 0)
            {
                context.WriteLine("✓ No new records since last run", ConsoleTextColor.Green);
                _logger.LogInformation("No new records since last run");
                return;
            }

            context.WriteLine($"✓ Fetched {oracleRecords.Count} records", ConsoleTextColor.Green);
            var workOrders = _oracleService.TransformOracleData(oracleRecords);

            context.WriteLine($"Updating {workOrders.Count} work orders in database...", ConsoleTextColor.Yellow);
            int updateCount = 0;
            int errorCount = 0;
            
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
                        
                        if (updateCount % 50 == 0)
                        {
                            context.WriteLine($"  Progress: {updateCount}/{workOrders.Count} records updated", ConsoleTextColor.Cyan);
                        }
                        
                        _logger.LogDebug("Updated record {Count}", updateCount);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    var errorMsg = $"ERROR: Record {updateCount + 1}/{workOrders.Count} - FRACPR={workOrder.FRACPR} - {ex.Message}";
                    context.WriteLine(errorMsg, ConsoleTextColor.Red);
                    _logger.LogError(ex, "Error updating workorder: FRACPR={FRACPR}", workOrder.FRACPR);
                    // Continue processing other records
                }
            }

            context.WriteLine($"", ConsoleTextColor.White);
            var completionMsg = $"✓ Incremental Oracle ETL completed: Updated={updateCount}, Errors={errorCount}, Timestamp={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            context.WriteLine(completionMsg, ConsoleTextColor.Green);
            _logger.LogInformation(
                "Incremental Oracle ETL completed: Updated={Count}, Errors={ErrorCount}, Timestamp={Timestamp}",
                updateCount, errorCount, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            var errorMsg = $"CRITICAL ERROR: Incremental Oracle ETL job failed: {ex.Message}";
            context.WriteLine(errorMsg, ConsoleTextColor.Red);
            _logger.LogError(ex, "Incremental Oracle ETL job failed");
            throw;
        }
    }
}
