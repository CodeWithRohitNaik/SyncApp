using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using ThrustSync.Core.Models;
using ThrustSync.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ThrustSync.ETL.Services;

/// <summary>
/// Service implementation for Oracle data extraction and transformation
/// </summary>
public class OracleService : IOracleService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OracleService> _logger;
    private readonly string _connectionString;

    public OracleService(IConfiguration configuration, ILogger<OracleService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = _configuration.GetConnectionString("OracleConnection") 
            ?? throw new InvalidOperationException("OracleConnection string is not configured");
    }

    public async Task<List<Dictionary<string, object>>> FetchFlaggedRecordsAsync(DateTime? sinceDate = null)
    {
        var records = new List<Dictionary<string, object>>();

        try
        {
            _logger.LogInformation("Fetching flagged records from Oracle: SinceDate={SinceDate}", sinceDate);

            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT a.fracpr, a.jcn, a.ajcn, a.refdes, a.wuc, a.mid, c.cus_des_fmt AS tail_number,
                           b.discrepancy, b.corr_action AS corrective_action, a.typefail, a.removal, a.review_status,
                           a.part_nbr, a.part_serial_nbr, a.mx_dt, a.hmc, a.fac
                    FROM matweb01.c17_aeh_tbl a
                    JOIN matweb01.c17_comment_tbl b 
                        ON a.fracpr = b.fracpr
                    LEFT JOIN matweb01.c17_aircraft_def c 
                        ON a.mid = c.mid
                    WHERE a.refdes = '4910AA001'
                      AND (a.removal = 'R' OR a.typefail = '6')
                      AND a.review_status IN ('C','Y')";

                if (sinceDate.HasValue)
                {
                    query += " AND a.mx_dt >= :SinceDate";
                }

                query += " ORDER BY a.mx_dt DESC";

                using (var command = new OracleCommand(query, connection))
                {
                    if (sinceDate.HasValue)
                    {
                        command.Parameters.Add(":SinceDate", sinceDate.Value);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var record = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                // Normalize column names to lowercase for case-insensitive lookup
                                var columnName = reader.GetName(i).ToLower();
                                record[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            records.Add(record);
                            
                            // Log first few records' FRACPR values for debugging
                            if (records.Count <= 5)
                            {
                                var fracpr = record.TryGetValue("fracpr", out var val) ? val?.ToString() : "NULL";
                                _logger.LogInformation("Record {Count}: FRACPR={FRACPR}, JCN={JCN}, MID={MID}", 
                                    records.Count, fracpr, 
                                    record.TryGetValue("jcn", out var jcn) ? jcn?.ToString() : "NULL",
                                    record.TryGetValue("mid", out var mid) ? mid?.ToString() : "NULL");
                            }
                        }
                    }
                }

                _logger.LogInformation("Fetched {Count} flagged records from Oracle", records.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching records from Oracle");
            throw;
        }

        return records;
    }

    public List<WorkOrder> TransformOracleData(List<Dictionary<string, object>> oracleData)
    {
        var workOrders = new List<WorkOrder>();

        _logger.LogInformation("Transforming {Count} Oracle records", oracleData.Count);

        foreach (var record in oracleData)
        {
            try
            {
                var fracpr = GetStringValue(record, "fracpr");
                var jcn = GetStringValue(record, "jcn");
                var mid = GetStringValue(record, "mid");
                
                // Log first few transformed records for debugging
                if (workOrders.Count < 5)
                {
                    _logger.LogInformation("Transforming record {Count}: FRACPR={FRACPR}, JCN={JCN}, MID={MID}", 
                        workOrders.Count + 1, fracpr, jcn, mid);
                }
                
                var workOrder = new WorkOrder
                {
                    JCN = jcn,
                    FRACPR = fracpr,
                    MID = mid,
                    TailNumber = GetStringValue(record, "tail_number"),
                    CreatedDate = GetDateTimeValue(record, "mx_dt") ?? DateTime.UtcNow,
                    OraclePulledOn = DateTime.UtcNow,
                    HMC = GetStringValue(record, "hmc"),
                    FAC = GetStringValue(record, "fac"),
                    APU = new APU
                    {
                        RefDes = GetStringValue(record, "refdes"),
                        PartNumber = GetStringValue(record, "part_nbr"),
                        PartSerialNumber = GetStringValue(record, "part_serial_nbr"),
                        FailureCode = GetIntValue(record, "typefail"),
                        RemovalIndicator = GetStringValue(record, "removal"),
                        ReviewStatus = GetStringValue(record, "review_status")
                    },
                    OracleSnapshot = new OracleSnapshot
                    {
                        PulledOn = DateTime.UtcNow,
                        SourceData = JsonSerializer.Serialize(record)
                    }
                };

                workOrders.Add(workOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming Oracle record: {Record}", 
                    JsonSerializer.Serialize(record));
            }
        }

        _logger.LogInformation("Successfully transformed {Count} records into {WorkOrderCount} work orders",
            oracleData.Count, workOrders.Count);
        
        // Log sample of unique FRACPRs for verification
        var uniqueFracprs = workOrders.Select(w => w.FRACPR).Distinct().Count();
        _logger.LogInformation("Unique FRACPR values in transformed data: {UniqueCount}/{TotalCount}", uniqueFracprs, workOrders.Count);

        return workOrders;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing Oracle connection");

            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                _logger.LogInformation("Oracle connection successful");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle connection test failed");
            return false;
        }
    }

    private string GetStringValue(Dictionary<string, object> record, string key)
    {
        if (record.TryGetValue(key, out var value) && value != null)
            return value.ToString() ?? string.Empty;
        return string.Empty;
    }

    private int? GetIntValue(Dictionary<string, object> record, string key)
    {
        if (record.TryGetValue(key, out var value) && value != null)
        {
            if (int.TryParse(value.ToString(), out var result))
                return result;
        }
        return null;
    }

    private DateTime? GetDateTimeValue(Dictionary<string, object> record, string key)
    {
        if (record.TryGetValue(key, out var value) && value != null)
        {
            if (value is DateTime dt)
                return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed;
        }
        return null;
    }
}
