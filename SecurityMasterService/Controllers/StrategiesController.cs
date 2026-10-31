using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecurityMasterService.Data;
using SecurityMasterService.Hubs;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("strategies")]
public class StrategiesController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly ILogger<StrategiesController> logger;
    private readonly IHubContext<NotificationHub> hub;
    public StrategiesController(SecurityMasterDbContext db, ILogger<StrategiesController> logger, IHubContext<NotificationHub> hub)
    {
        this.db = db;
        this.logger = logger;
        this.hub = hub;
    }

    [HttpPost("query")]
    public async Task<ActionResult<List<Strategy>>> Query([FromBody] ServiceQuery<Strategy> query)
    {
        var baseQuery = this.db.Strategies.AsNoTracking().AsQueryable();
        return await baseQuery.ExecuteAsync(query);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var strategy = await this.db.Strategies.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (strategy == null)
            return NotFound();
        return Ok(strategy);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStrategyRequest request)
    {
        var strategy = new Strategy
        {
            Display = request.Display
        };

        this.db.Strategies.Add(strategy);
        await this.db.SaveChangesAsync();
        await this.hub.Clients.All.SendAsync(NotificationHub.StrategiesChanged);
        return Created($"/strategies/{strategy.Id}", strategy);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateStrategyRequest request)
    {
        var strategy = await this.db.Strategies.FirstOrDefaultAsync(s => s.Id == id);
        if (strategy == null)
            return NotFound();

        strategy.Display = request.Display;

        await this.db.SaveChangesAsync();
        await this.hub.Clients.All.SendAsync(NotificationHub.StrategiesChanged);
        return Ok(strategy);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var strategy = await this.db.Strategies.FirstOrDefaultAsync(s => s.Id == id);
        if (strategy == null)
            return NotFound();

        var hasOrders = await this.db.Orders.AnyAsync(o => o.StrategyId == id);
        if (hasOrders)
            return Conflict("Strategy is already used, unable to delete it.");

        this.db.Strategies.Remove(strategy);
        await this.db.SaveChangesAsync();
        await this.hub.Clients.All.SendAsync(NotificationHub.StrategiesChanged);
        return Ok();
    }
}