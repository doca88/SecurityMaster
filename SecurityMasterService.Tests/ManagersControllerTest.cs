using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityMasterService.Controllers;
using SecurityMasterService.Data;

namespace SecurityMasterService.Tests;

[TestFixture]
public class ManagersControllerTests
{
    private SecurityMasterDbContext _db = null!;
    private ManagersController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SecurityMasterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SecurityMasterDbContext(options);

        _controller = new ManagersController(_db, null!);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task Create_ValidRequest_AddsManager()
    {
        var request = new CreateManagerRequest { Display = "Aca" };

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        Assert.That(_db.Managers.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetById_ExistingManager_ReturnsManager()
    {
        var manager = new Manager { Display = "Aca" };
        _db.Managers.Add(manager);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(manager.Id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NonExistingManager_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ExistingManager_RemovesManager()
    {
        var manager = new Manager { Display = "Aca" };
        _db.Managers.Add(manager);
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(manager.Id);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_db.Managers.Count(), Is.EqualTo(0));
    }
}