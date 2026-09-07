using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SecurityMasterService.Controllers;
using SecurityMasterService.Data;

namespace SecurityMasterService.Tests;

[TestFixture]
public class SecuritiesControllerTests
{
    private SecurityMasterDbContext _db = null!;
    private SecuritiesController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<SecurityMasterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SecurityMasterDbContext(options);
        _controller = new SecuritiesController(_db, null!);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task Create_ValidRequest_AddsSecurity()
    {
        var request = new CreateSecurityRequest { Sid = 1, Description = "Security 1001" };

        var result = await _controller.Create(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        Assert.That(_db.Securities.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetById_ExistingSecurity_ReturnsSecurity()
    {
        var security = new Security { Sid = 1, Description = "Security 1001" };
        _db.Securities.Add(security);
        await _db.SaveChangesAsync();

        var result = await _controller.GetById(security.Sid);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NonExistingSecurity_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ExistingSecurity_RemovesSecurity()
    {
        var security = new Security { Sid = 1, Description = "Security 1001" };
        _db.Securities.Add(security);
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(security.Sid);

        Assert.That(result, Is.InstanceOf<OkResult>());
        Assert.That(_db.Securities.Count(), Is.EqualTo(0));
    }
}