using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThrustSync.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExportedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    FilterCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JCN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FRACPR = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MID = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TailNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastKpiUpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OraclePulledOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "APUs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefDes = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PartSerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FailureCode = table.Column<int>(type: "int", nullable: true),
                    RemovalIndicator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APUs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APUs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OtherHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OpTime = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EnteredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EnteredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OracleSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PulledOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OracleSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OracleSnapshots_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APUs_PartNumber",
                table: "APUs",
                column: "PartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_APUs_PartSerialNumber",
                table: "APUs",
                column: "PartSerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_APUs_RefDes",
                table: "APUs",
                column: "RefDes");

            migrationBuilder.CreateIndex(
                name: "IX_APUs_RefDes_PartNumber_PartSerialNumber",
                table: "APUs",
                columns: new[] { "RefDes", "PartNumber", "PartSerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ChangedBy",
                table: "AuditLogs",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ChangedOn",
                table: "AuditLogs",
                column: "ChangedOn");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_WorkOrderId",
                table: "AuditLogs",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportLogs_ExportedOn",
                table: "ExportLogs",
                column: "ExportedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ExportLogs_ExportedBy",
                table: "ExportLogs",
                column: "ExportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_KpiEntries_EnteredOn",
                table: "KpiEntries",
                column: "EnteredOn");

            migrationBuilder.CreateIndex(
                name: "IX_KpiEntries_WorkOrderId",
                table: "KpiEntries",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OracleSnapshots_PulledOn",
                table: "OracleSnapshots",
                column: "PulledOn");

            migrationBuilder.CreateIndex(
                name: "IX_OracleSnapshots_WorkOrderId",
                table: "OracleSnapshots",
                column: "WorkOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_CreatedDate",
                table: "WorkOrders",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_FRACPR",
                table: "WorkOrders",
                column: "FRACPR");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_JCN",
                table: "WorkOrders",
                column: "JCN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_MID",
                table: "WorkOrders",
                column: "MID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_OraclePulledOn",
                table: "WorkOrders",
                column: "OraclePulledOn");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TailNumber",
                table: "WorkOrders",
                column: "TailNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APUs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ExportLogs");

            migrationBuilder.DropTable(
                name: "KpiEntries");

            migrationBuilder.DropTable(
                name: "OracleSnapshots");

            migrationBuilder.DropTable(
                name: "WorkOrders");
        }
    }
}
