using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("strategies")]
public class StrategiesController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly AllowedFields<Strategy> allowedFields;

    public StrategiesController(SecurityMasterDbContext db, AllowedFields<Strategy> allowedFields)
    {
        this.db = db;
        this.allowedFields = allowedFields;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var baseQuery = this.db.Strategies.AsQueryable();
        return await QueryHelper.HandleQuery(Request.Query, baseQuery, this.allowedFields.Fields);
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
        return Ok();
    }
}