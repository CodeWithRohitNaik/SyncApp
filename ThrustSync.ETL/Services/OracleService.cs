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
                    SELECT 
                        JCN, FRACPR, MID, TAIL_NUMBER, REFDES, PART_NUMBER, 
                        PART_SERIAL_NUMBER, FAIL, RMVL, REVIEW_STATUS, 
                        CREATED_DATE, UPDATED_DATE
                    FROM FLAGGED_ITEMS
                    WHERE REFDES = '4910AA001' 
                    AND (RMVL = 'R' OR FAIL = 6)
                    AND REVIEW_STATUS IN ('C', 'Y')";

                if (sinceDate.HasValue)
                {
                    query += " AND UPDATED_DATE >= :SinceDate";
                }

                query += " ORDER BY CREATED_DATE DESC";

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
                            var record = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                record[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            records.Add(record);
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
                var workOrder = new WorkOrder
                {
                    JCN = GetStringValue(record, "JCN"),
                    FRACPR = GetStringValue(record, "FRACPR"),
                    MID = GetStringValue(record, "MID"),
                    TailNumber = GetStringValue(record, "TAIL_NUMBER"),
                    CreatedDate = GetDateTimeValue(record, "CREATED_DATE") ?? DateTime.UtcNow,
                    OraclePulledOn = DateTime.UtcNow,
                    APU = new APU
                    {
                        RefDes = GetStringValue(record, "REFDES"),
                        PartNumber = GetStringValue(record, "PART_NUMBER"),
                        PartSerialNumber = GetStringValue(record, "PART_SERIAL_NUMBER"),
                        FailureCode = GetIntValue(record, "FAIL"),
                        RemovalIndicator = GetStringValue(record, "RMVL"),
                        ReviewStatus = GetStringValue(record, "REVIEW_STATUS")
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
