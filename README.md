# ThrustSync - Oracle Data Viewer + KPI Management Application

A modern .NET 8 MVC application for managing flagged engines/APUs with readonly Oracle data integration and manual KPI entry capabilities.

## Project Overview

**ThrustSync** is a binder-style workflow application that:
- Pulls flagged engine/APU data from Oracle (readonly via ETL)
- Displays Oracle fields with color coding (gray = base, blue = updated)
- Allows manual KPI entry (Flight Hours, Other Hours, Operating Time) in yellow
- Maintains complete audit trails for compliance
- Exports annotated Excel reports
- Runs on Azure with CI/CD automation

## Tech Stack

- **.NET**: 8.0 LTS MVC
- **Database**: Azure SQL Server + EF Core 8.0
- **Background Jobs**: Hangfire (scheduled ETL tasks)
- **Oracle Access**: Oracle.ManagedDataAccess.Core (readonly)
- **Excel Export**: ClosedXML
- **Logging**: Application Insights
- **Security**: Azure Key Vault, Azure AD (configurable)
- **CI/CD**: Azure DevOps Pipelines
- **Testing**: xUnit + Moq

## Project Structure

```
ThrustSync/
├── ThrustSync.sln                 # Solution file
├── ThrustSync.Core/               # Domain models & interfaces
│   ├── Models/                   # Entity definitions
│   ├── Enums/                    # Domain enumerations
│   └── Services/                 # Service interfaces & implementations
├── ThrustSync.Data/               # EF Core data access layer
│   ├── Context/                  # DbContext
│   ├── Repositories/             # Repository implementations
│   └── Migrations/               # Database migrations
├── ThrustSync.ETL/                # Background job layer
│   ├── Services/                 # Oracle service, transformations
│   └── Jobs/                     # Hangfire job definitions
├── ThrustSync.Web/                # ASP.NET MVC web application
│   ├── Controllers/              # MVC controllers
│   ├── Views/                    # Razor views
│   ├── Models/                   # ViewModels
│   ├── wwwroot/                  # Static assets (CSS, JS)
│   ├── Program.cs                # Application startup
│   └── appsettings.json          # Configuration
├── ThrustSync.Tests/              # Unit & integration tests
├── azure-pipelines.yml           # Azure DevOps CI/CD
└── README.md                     # This file
```

## Core Data Models

### WorkOrder
- **JCN**: Job Control Number (unique identifier)
- **FRACPR**: FAA Form Release part number
- **MID**: Maintenance Item Description
- **TailNumber**: Aircraft tail number
- **OraclePulledOn**: Timestamp of last Oracle sync
- **LastKpiUpdatedOn**: Timestamp of last manual KPI update

### APU
- **RefDes**: Reference designation (e.g., 4910AA001)
- **PartNumber**: Part number
- **PartSerialNumber**: Serial number
- **FailureCode**: Numeric failure code
- **RemovalIndicator**: R/S indicator
- **ReviewStatus**: C/Y/N/Pending

### KpiEntry
- **FlightHours**: Manual entry (decimal)
- **OtherHours**: Manual entry (decimal)
- **OpTime**: Operating time (decimal)
- **EnteredBy**: Username of data enterer
- **EnteredOn**: Timestamp
- **Source**: "FMxC2 Manual" | "Oracle" | "Import"

### AuditLog
- Tracks all changes to KPI entries
- Records: ChangeType, FieldName, OldValue, NewValue, ChangedBy, ChangedOn

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (local or Azure)
- Oracle database access (or mock for development)
- Visual Studio 2022+ or VS Code with C# extension
- Azure CLI (for cloud deployment)

### Local Development Setup

1. **Clone and restore:**
```bash
git clone https://github.com/your-org/thrustsync.git
cd ThrustSync
dotnet restore
```

2. **Configure connection strings** in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ThrustSyncDb;Trusted_Connection=true;",
    "OracleConnection": "User Id=system;Password=oracle;Data Source=localhost:1521/XEPDB1;"
  }
}
```

3. **Apply migrations:**
```bash
cd ThrustSync.Data
dotnet ef database update
```

4. **Run the application:**
```bash
cd ThrustSync.Web
dotnet run
```

Access the app at `https://localhost:5001`

## ETL Job Configuration

### Oracle Filter Criteria
The Hangfire job pulls records matching:
```
WHERE REFDES = '4910AA001' 
  AND (RMVL = 'R' OR FAIL = 6)
  AND REVIEW_STATUS IN ('C', 'Y')
```

### Scheduled Job
- **Frequency**: Daily at 2:00 AM UTC
- **Operation**: Full ETL sync (idempotent)
- **Retry**: Automatic retry on failure
- **Dashboard**: Available at `/hangfire`

### Running Jobs Manually
```csharp
var job = serviceProvider.GetRequiredService<OracleEtlJob>();
await job.ExecuteAsync();
```

## Color Coding in UI

| Color | Meaning | Editable |
|-------|---------|----------|
| Gray (#e8e8e8) | Oracle base data | No |
| Blue (#d6ebff) | Oracle updated data | No |
| Yellow (#ffffcc) | KPI manual entry | Yes |

## API Endpoints

### WorkOrder Controller

#### Get Filtered List
```
GET /api/workorder/list?page=1&pageSize=25&jcn=JCN001&fracpr=FRACPR001
```

#### Get Details
```
GET /api/workorder/{id}
```

#### Save KPI Entry
```
POST /api/workorder/{id}/kpi
Content-Type: application/json

{
  "flightHours": 100.5,
  "otherHours": 50.25,
  "opTime": 150.75,
  "notes": "Manual entry notes"
}
```

#### Export to Excel
```
POST /api/workorder/export
Content-Type: application/json

{
  "jcn": "JCN001",
  "fracpr": "FRACPR001",
  "fromDate": "2024-01-01",
  "toDate": "2024-12-31"
}
```

## Database Migrations

### Creating a new migration
```bash
cd ThrustSync.Data
dotnet ef migrations add YourMigrationName --project ../ThrustSync.Data --startup-project ../ThrustSync.Web
```

### Applying migrations
```bash
dotnet ef database update --project ThrustSync.Data --startup-project ThrustSync.Web
```

## Azure Deployment

### Prerequisites
- Azure Subscription
- Azure SQL Database
- Azure App Service
- Azure Key Vault
- Azure DevOps project

### Key Vault Setup
Store sensitive data:
- `DbConnection`: Azure SQL connection string
- `OracleConnection`: Oracle connection string
- `AppInsightsKey`: Application Insights instrumentation key

### CI/CD Pipeline
The `azure-pipelines.yml` automates:
1. Build & unit tests
2. Code coverage analysis
3. Database migrations
4. Deployment to Dev/Prod
5. Health checks

## Testing

### Unit Tests
```bash
dotnet test ThrustSync.Tests
```

### Test Coverage
- Service layer: WorkOrderService, KpiService, AuditService
- Repository pattern: CRUD operations
- ETL transformation: Oracle data mapping

### Sample Test
```csharp
[Fact]
public async Task SaveKpiEntry_WithValidData_ShouldSucceed()
{
    // Arrange
    var service = new KpiService(...);
    
    // Act
    var result = await service.SaveKpiEntryAsync(1, 100, 50, 150, "user");
    
    // Assert
    Assert.NotNull(result);
}
```

## Logging

### Application Insights
- Configured in `Program.cs`
- Automatic SQL query logging (with sensitive data filtering)
- Custom event tracking for KPI saves, exports, ETL runs

### Log Levels
- **Debug**: Development only
- **Information**: ETL runs, user actions
- **Warning**: Validation failures, retries
- **Error**: Database errors, API failures

## Security Best Practices

1. **Connection Strings**: Use Azure Key Vault, never commit to repo
2. **Oracle Access**: Readonly credentials, no direct user access
3. **Authentication**: Integrate Azure AD for user identity
4. **Authorization**: Role-based access (Admin, Technician, Viewer)
5. **Audit Trail**: Immutable logs of all KPI changes
6. **HTTPS**: Required in production

## Troubleshooting

### Database Connection Issues
```bash
# Test connection string
dotnet ef dbcontext validate
```

### Migration Errors
```bash
# Rollback last migration
dotnet ef migrations remove --force
```

### Hangfire Job Failures
- Check Application Insights logs
- Review job history in `/hangfire` dashboard
- Check Oracle connectivity with `OracleService.TestConnectionAsync()`

## Contributing

1. Create feature branch: `git checkout -b feature/description`
2. Commit changes: `git commit -m "Add feature"`
3. Push to branch: `git push origin feature/description`
4. Create Pull Request with test coverage

## Deployment Checklist

- [ ] All tests passing
- [ ] Code review approved
- [ ] Database backups created
- [ ] Key Vault secrets configured
- [ ] Application Insights enabled
- [ ] Hangfire dashboard secured
- [ ] Load testing completed
- [ ] Documentation updated

## License

Internal use only - Confidential

## Support

For issues or questions:
1. Check Application Insights logs
2. Review Hangfire dashboard at `/hangfire`
3. Contact: [DevOps Team]

## Version History

- **v1.0.0** (June 2024): Initial release
  - Oracle ETL integration
  - KPI manual entry
  - Excel export
  - Audit trail
  - Azure DevOps CI/CD
