using ThrustSync.Core.Models;
using ThrustSync.Core.Services;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ThrustSync.Web.Services;

/// <summary>
/// Service implementation for Excel export functionality
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> ExportWorkOrdersAsync(
        IEnumerable<WorkOrder> workOrders, string exportedBy)
    {
        _logger.LogInformation("Exporting workorders to Excel: Count={Count}, ExportedBy={ExportedBy}",
            workOrders.Count(), exportedBy);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Work Orders");

            // Add headers
            var headers = new[] 
            { 
                "JCN", "FRACPR", "MID", "Tail Number", "Created Date", 
                "Oracle Pulled", "Last KPI Updated", "Flight Hours", "Other Hours", "Op Time" 
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
                worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Add data rows
            int row = 2;
            foreach (var workOrder in workOrders)
            {
                var latestKpi = workOrder.KpiEntries?.OrderByDescending(k => k.EnteredOn).FirstOrDefault();

                worksheet.Cell(row, 1).Value = workOrder.JCN;
                worksheet.Cell(row, 2).Value = workOrder.FRACPR;
                worksheet.Cell(row, 3).Value = workOrder.MID;
                worksheet.Cell(row, 4).Value = workOrder.TailNumber ?? "";
                worksheet.Cell(row, 5).Value = workOrder.CreatedDate;
                worksheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                worksheet.Cell(row, 6).Value = workOrder.OraclePulledOn;
                worksheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                worksheet.Cell(row, 7).Value = workOrder.LastKpiUpdatedOn.HasValue ? workOrder.LastKpiUpdatedOn.Value.ToString("yyyy-MM-dd HH:mm") : "";
                worksheet.Cell(row, 8).Value = latestKpi?.FlightHours?.ToString() ?? "";
                worksheet.Cell(row, 9).Value = latestKpi?.OtherHours?.ToString() ?? "";
                worksheet.Cell(row, 10).Value = latestKpi?.OpTime?.ToString() ?? "";

                // Color code: Yellow for manual entries
                if (latestKpi != null && latestKpi.Source == "FMxC2 Manual")
                {
                    worksheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.Yellow;
                    worksheet.Cell(row, 9).Style.Fill.BackgroundColor = XLColor.Yellow;
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.Yellow;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            // Add metadata sheet
            var metaSheet = workbook.Worksheets.Add("Metadata");
            metaSheet.Cell(1, 1).Value = "Export Date";
            metaSheet.Cell(1, 2).Value = DateTime.UtcNow;
            metaSheet.Cell(2, 1).Value = "Exported By";
            metaSheet.Cell(2, 2).Value = exportedBy;
            metaSheet.Cell(3, 1).Value = "Record Count";
            metaSheet.Cell(3, 2).Value = workOrders.Count();
            metaSheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }

    public async Task<byte[]> ExportWorkOrderDetailsAsync(int workOrderId, string exportedBy)
    {
        _logger.LogInformation("Exporting workorder details to Excel: WorkOrderId={WorkOrderId}, ExportedBy={ExportedBy}",
            workOrderId, exportedBy);

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Details");

            worksheet.Cell(1, 1).Value = "Field";
            worksheet.Cell(1, 2).Value = "Value";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 2).Style.Font.Bold = true;

            worksheet.Cell(2, 1).Value = "WorkOrder ID";
            worksheet.Cell(2, 2).Value = workOrderId;

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }
}
