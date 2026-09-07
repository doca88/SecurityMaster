using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
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

        _controller = new OrdersController(_db, null!);
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
    public async Task Query_FilterByOrderIdEquals_ReturnsSingleMatchingOrder()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        for (int i = 1; i <= 10; i++)
        {
            _db.Orders.Add(new Order
            {
                Quantity = 100,
                TradeDate = DateTime.UtcNow,
                ManagerId = manager.Id,
                StrategyId = strategy.Id,
                Sid = security.Sid,
                Manager = manager,
                Strategy = strategy,
                Security = security
            });
        }
        await _db.SaveChangesAsync();

        var targetOrderId = _db.Orders.Skip(4).First().OrderId;

        var query = new ServiceQuery<Order>
        {
            SortBy = "OrderId",
            SortDescending = false,
            Filters = new List<QueryFilter>
            {
                new QueryFilter { Field = "OrderId", Operator = FilterOperator.Equals, Value = targetOrderId.ToString() }
            }
        };

        var actionResult = await _controller.Query(query);
        var result = actionResult.Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0].OrderId, Is.EqualTo(targetOrderId));
    }

    [Test]
    public async Task Query_NoFilters_ReturnsAllOrdersUpToPageSize()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        for (int i = 1; i <= 5; i++)
        {
            _db.Orders.Add(new Order
            {
                Quantity = 100,
                TradeDate = DateTime.UtcNow,
                ManagerId = manager.Id,
                StrategyId = strategy.Id,
                Sid = security.Sid,
                Manager = manager,
                Strategy = strategy,
                Security = security
            });
        }
        await _db.SaveChangesAsync();

        var query = new ServiceQuery<Order>();

        var actionResult = await _controller.Query(query);
        var result = actionResult.Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task Query_SortDescendingByOrderId_ReturnsOrdersInDescendingOrder()
    {
        var (manager, strategy, security) = await SeedDependenciesAsync();

        for (int i = 1; i <= 3; i++)
        {
            _db.Orders.Add(new Order
            {
                Quantity = 100,
                TradeDate = DateTime.UtcNow,
                ManagerId = manager.Id,
                StrategyId = strategy.Id,
                Sid = security.Sid,
                Manager = manager,
                Strategy = strategy,
                Security = security
            });
        }
        await _db.SaveChangesAsync();

        var query = new ServiceQuery<Order>
        {
            SortBy = "OrderId",
            SortDescending = true
        };

        var actionResult = await _controller.Query(query);
        var result = actionResult.Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Ordered.Descending.By(nameof(Order.OrderId)));
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