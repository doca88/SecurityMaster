using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Controllers;
using SecurityMasterService.Data;

namespace SecurityMasterService.Tests;

[TestFixture]
public class OrdersControllerTests
{
    private SecurityMasterDbContext _db = null!;
    private OrdersController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SecurityMasterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SecurityMasterDbContext(options);

        var allowedFields = new AllowedFields<Order>(FilterFieldResolver.GetAllowedFields(typeof(Order)));
        _controller = new OrdersController(_db, allowedFields, null!);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    private async Task<(Manager manager, Strategy strategy, Security security)> SeedDependenciesAsync()
    {
        var manager = new Manager { Display = "Aca" };
        var strategy = new Strategy { Display = "Strategija 1" };
        var security = new Security { Sid = 1, Description = "Security 1001" };

        _db.Managers.Add(manager);
        _db.Strategies.Add(strategy);
        _db.Securities.Add(security);
        await _db.SaveChangesAsync();

        return (manager, strategy, security);
    }

    [Test]
    public async Task Create_ValidRequest_AddsOrder()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        var request = new CreateOrderRequest
        {
            Quantity = 100,
            TradeDate = DateTime.UtcNow,
            ManagerId = manager.Id,
            StrategyId = strategy.Id,
            Sid = security.Sid
        };

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        Assert.That(_db.Orders.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Create_NonExistingManager_ReturnsBadRequest()
    {
        var (_, strategy, security) = await SeedDependenciesAsync();

        var request = new CreateOrderRequest
        {
            Quantity = 100,
            TradeDate = DateTime.UtcNow,
            ManagerId = 999,
            StrategyId = strategy.Id,
            Sid = security.Sid
        };

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetById_ExistingOrder_ReturnsOrder()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        var order = new Order
        {
            Quantity = 100,
            TradeDate = DateTime.UtcNow,
            ManagerId = manager.Id,
            StrategyId = strategy.Id,
            Sid = security.Sid,
            Manager = manager,
            Strategy = strategy,
            Security = security
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(order.OrderId);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NonExistingOrder_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_OrderWithoutAllocations_RemovesOrder()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        var order = new Order
        {
            Quantity = 100,
            TradeDate = DateTime.UtcNow,
            ManagerId = manager.Id,
            StrategyId = strategy.Id,
            Sid = security.Sid,
            Manager = manager,
            Strategy = strategy,
            Security = security
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(order.OrderId);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_db.Orders.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task Delete_OrderWithAllocations_ReturnsConflict()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        var order = new Order
        {
            Quantity = 100,
            TradeDate = DateTime.UtcNow,
            ManagerId = manager.Id,
            StrategyId = strategy.Id,
            Sid = security.Sid,
            Manager = manager,
            Strategy = strategy,
            Security = security
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var allocation = new Allocation
        {
            Quantity = 50,
            ManagerId = manager.Id,
            StrategyId = strategy.Id,
            OrderId = order.OrderId,
            Manager = manager,
            Strategy = strategy
        };
        _db.Allocations.Add(allocation);
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(order.OrderId);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        Assert.That(_db.Orders.Count(), Is.EqualTo(1)); // order nije obrisan
    }
}