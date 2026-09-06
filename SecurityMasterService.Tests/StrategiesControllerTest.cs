using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Controllers;
using SecurityMasterService.Data;

namespace SecurityMasterService.Tests;

[TestFixture]
public class StrategiesControllerTests
{
    private SecurityMasterDbContext _db = null!;
    private StrategiesController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SecurityMasterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SecurityMasterDbContext(options);

        var allowedFields = new AllowedFields<Strategy>(FilterFieldResolver.GetAllowedFields(typeof(Strategy)));
        _controller = new StrategiesController(_db, allowedFields);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task Create_ValidRequest_AddsStrategy()
    {
        var request = new CreateStrategyRequest { Display = "Strategija 1" };

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        Assert.That(_db.Strategies.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetById_ExistingStrategy_ReturnsStrategy()
    {
        var strategy = new Strategy { Display = "Strategija 1" };
        _db.Strategies.Add(strategy);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(strategy.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NonExistingStrategy_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ExistingStrategy_RemovesStrategy()
    {
        var strategy = new Strategy { Display = "Strategija 1" };
        _db.Strategies.Add(strategy);
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(strategy.Id);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_db.Strategies.Count(), Is.EqualTo(0));
    }
}