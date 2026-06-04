using Moq;
using ThrustSync.Core.Models;
using ThrustSync.Core.Services;
using ThrustSync.Core.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThrustSync.Tests;

/// <summary>
/// Unit tests for KpiService
/// </summary>
public class KpiServiceTests
{
    private readonly Mock<IKpiEntryRepository> _mockKpiRepository;
    private readonly Mock<IWorkOrderRepository> _mockWorkOrderRepository;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<KpiService>> _mockLogger;
    private readonly KpiService _kpiService;

    public KpiServiceTests()
    {
        _mockKpiRepository = new Mock<IKpiEntryRepository>();
        _mockWorkOrderRepository = new Mock<IWorkOrderRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<KpiService>>();

        _kpiService = new KpiService(
            _mockKpiRepository.Object,
            _mockWorkOrderRepository.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SaveKpiEntry_WithValidData_ShouldSucceed()
    {
        // Arrange
        int workOrderId = 1;
        decimal flightHours = 100.5m;
        decimal otherHours = 50.25m;
        decimal opTime = 150.75m;
        string enteredBy = "testuser";

        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            JCN = "JCN001",
            FRACPR = "FRACPR001",
            MID = "MID001",
            CreatedDate = DateTime.UtcNow,
            OraclePulledOn = DateTime.UtcNow
        };

        _mockWorkOrderRepository
            .Setup(r => r.GetByIdAsync(workOrderId))
            .ReturnsAsync(workOrder);

        _mockKpiRepository
            .Setup(r => r.AddAsync(It.IsAny<KpiEntry>()))
            .Returns(Task.CompletedTask);

        _mockKpiRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockWorkOrderRepository
            .Setup(r => r.Update(It.IsAny<WorkOrder>()))
            .Callback<WorkOrder>(w => { });

        _mockAuditService
            .Setup(s => s.LogChangeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _kpiService.SaveKpiEntryAsync(
            workOrderId, flightHours, otherHours, opTime, enteredBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(flightHours, result.FlightHours);
        Assert.Equal(otherHours, result.OtherHours);
        Assert.Equal(opTime, result.OpTime);
        Assert.Equal(enteredBy, result.EnteredBy);
        Assert.Equal("FMxC2 Manual", result.Source);

        _mockKpiRepository.Verify(r => r.AddAsync(It.IsAny<KpiEntry>()), Times.Once);
        _mockKpiRepository.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
        _mockAuditService.Verify(s => s.LogChangeAsync(
            It.IsAny<int>(), "Create", "KpiEntry",
            null, It.IsAny<string>(), enteredBy), Times.Once);
    }

    [Fact]
    public async Task SaveKpiEntry_WithInvalidWorkOrder_ShouldThrow()
    {
        // Arrange
        int invalidWorkOrderId = 999;

        _mockWorkOrderRepository
            .Setup(r => r.GetByIdAsync(invalidWorkOrderId))
            .ReturnsAsync((WorkOrder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _kpiService.SaveKpiEntryAsync(invalidWorkOrderId, 100, 50, 150, "testuser"));
    }

    [Fact]
    public async Task GetKpiHistory_ShouldReturnEntries()
    {
        // Arrange
        int workOrderId = 1;
        var kpiEntries = new List<KpiEntry>
        {
            new KpiEntry { Id = 1, WorkOrderId = workOrderId, FlightHours = 100, EnteredBy = "user1", EnteredOn = DateTime.UtcNow },
            new KpiEntry { Id = 2, WorkOrderId = workOrderId, FlightHours = 110, EnteredBy = "user2", EnteredOn = DateTime.UtcNow.AddDays(-1) }
        };

        _mockKpiRepository
            .Setup(r => r.GetKpiEntriesForWorkOrderAsync(workOrderId))
            .ReturnsAsync(kpiEntries);

        // Act
        var result = await _kpiService.GetKpiHistoryAsync(workOrderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockKpiRepository.Verify(r => r.GetKpiEntriesForWorkOrderAsync(workOrderId), Times.Once);
    }
}

/// <summary>
/// Unit tests for WorkOrderService
/// </summary>
public class WorkOrderServiceTests
{
    private readonly Mock<IWorkOrderRepository> _mockRepository;
    private readonly Mock<ILogger<WorkOrderService>> _mockLogger;
    private readonly WorkOrderService _workOrderService;

    public WorkOrderServiceTests()
    {
        _mockRepository = new Mock<IWorkOrderRepository>();
        _mockLogger = new Mock<ILogger<WorkOrderService>>();
        _workOrderService = new WorkOrderService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetWorkOrders_ShouldReturnFilteredResults()
    {
        // Arrange
        int pageNumber = 1;
        int pageSize = 25;
        var workOrders = new List<WorkOrder>
        {
            new WorkOrder { Id = 1, JCN = "JCN001", FRACPR = "FRACPR001", MID = "MID001", CreatedDate = DateTime.UtcNow, OraclePulledOn = DateTime.UtcNow },
            new WorkOrder { Id = 2, JCN = "JCN002", FRACPR = "FRACPR002", MID = "MID002", CreatedDate = DateTime.UtcNow, OraclePulledOn = DateTime.UtcNow }
        };

        _mockRepository
            .Setup(r => r.GetFilteredWorkOrdersAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((workOrders, 2));

        // Act
        var (items, total) = await _workOrderService.GetWorkOrdersAsync(pageNumber, pageSize);

        // Assert
        Assert.Equal(2, items.Count());
        Assert.Equal(2, total);
        _mockRepository.Verify(
            r => r.GetFilteredWorkOrdersAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                null, null, null, null, null, null),
            Times.Once);
    }

    [Fact]
    public async Task GetWorkOrderDetails_WithValidId_ShouldReturnWorkOrder()
    {
        // Arrange
        int workOrderId = 1;
        var workOrder = new WorkOrder
        {
            Id = workOrderId,
            JCN = "JCN001",
            FRACPR = "FRACPR001",
            MID = "MID001",
            CreatedDate = DateTime.UtcNow,
            OraclePulledOn = DateTime.UtcNow,
            KpiEntries = new List<KpiEntry>(),
            AuditLogs = new List<AuditLog>()
        };

        _mockRepository
            .Setup(r => r.GetWorkOrderWithDetailsAsync(workOrderId))
            .ReturnsAsync(workOrder);

        // Act
        var result = await _workOrderService.GetWorkOrderDetailsAsync(workOrderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workOrderId, result.Id);
        Assert.Equal("JCN001", result.JCN);
    }

    [Fact]
    public async Task UpsertWorkOrder_WithExistingFracpr_ShouldUpdate()
    {
        // Arrange
        var existingWorkOrder = new WorkOrder
        {
            Id = 1,
            JCN = "OLD_JCN",
            FRACPR = "FRACPR001",
            MID = "OLD_MID",
            CreatedDate = DateTime.UtcNow,
            OraclePulledOn = DateTime.UtcNow.AddDays(-1)
        };

        var newWorkOrder = new WorkOrder
        {
            JCN = "NEW_JCN",
            FRACPR = "FRACPR001",
            MID = "NEW_MID",
            CreatedDate = DateTime.UtcNow,
            OraclePulledOn = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<WorkOrder, bool>>>()))
            .ReturnsAsync(existingWorkOrder);

        _mockRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _workOrderService.UpsertWorkOrderAsync(newWorkOrder);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.Update(It.IsAny<WorkOrder>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}

/// <summary>
/// Unit tests for AuditService
/// </summary>
public class AuditServiceTests
{
    private readonly Mock<IRepository<AuditLog>> _mockRepository;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        _mockRepository = new Mock<IRepository<AuditLog>>();
        _mockLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LogChange_ShouldCreateAuditLog()
    {
        // Arrange
        int workOrderId = 1;
        string changeType = "Update";
        string fieldName = "FlightHours";
        string oldValue = "100";
        string newValue = "150";
        string changedBy = "testuser";

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _auditService.LogChangeAsync(
            workOrderId, changeType, fieldName, oldValue, newValue, changedBy);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAuditHistory_ShouldReturnLogs()
    {
        // Arrange
        int workOrderId = 1;
        var auditLogs = new List<AuditLog>
        {
            new AuditLog { Id = 1, WorkOrderId = workOrderId, ChangeType = "Create", FieldName = "KPI", ChangedBy = "user1", ChangedOn = DateTime.UtcNow },
            new AuditLog { Id = 2, WorkOrderId = workOrderId, ChangeType = "Update", FieldName = "KPI", ChangedBy = "user2", ChangedOn = DateTime.UtcNow.AddHours(-1) }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AuditLog, bool>>>()))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _auditService.GetAuditHistoryAsync(workOrderId);

        // Assert
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(
            r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AuditLog, bool>>>()),
            Times.Once);
    }
}
