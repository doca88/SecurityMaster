using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("allocations")]
public class AllocationsController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly AllowedFields<Allocation> allowedFields;

    public AllocationsController(SecurityMasterDbContext db, AllowedFields<Allocation> allowedFields)
    {
        this.db = db;
        this.allowedFields = allowedFields;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var baseQuery = this.db.Allocations
        .Include(a => a.Manager)
        .Include(a => a.Strategy)
        .AsQueryable();
        return await QueryHelper.HandleQuery(Request.Query, baseQuery, this.allowedFields.Fields);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var allocation = await this.db.Allocations
        .Include(a => a.Manager)
        .Include(a => a.Strategy)
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.AllocationId == id);
        if (allocation == null)
            return NotFound();
        return Ok(allocation);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAllocationRequest request)
    {
        var manager = await this.db.Managers.FindAsync(request.ManagerId);
        if (manager == null)
            return BadRequest($"Manager with id {request.ManagerId} does not exist.");

        var strategy = await this.db.Strategies.FindAsync(request.StrategyId);
        if (strategy == null)
            return BadRequest($"Strategy with id {request.StrategyId} does not exist.");

        var orderExists = await this.db.Orders.AnyAsync(o => o.OrderId == request.OrderId);
        if (!orderExists)
            return BadRequest($"Order with id {request.OrderId} does not exist.");

        var allocation = new Allocation
        {
            Quantity = request.Quantity,
            ManagerId = request.ManagerId,
            StrategyId = request.StrategyId,
            OrderId = request.OrderId,
            Manager = manager,
            Strategy = strategy
        };

        this.db.Allocations.Add(allocation);
        await this.db.SaveChangesAsync();
        return Created($"/allocations/{allocation.AllocationId}", allocation);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAllocationRequest request)
    {
        var allocation = await this.db.Allocations.FirstOrDefaultAsync(a => a.AllocationId == id);
        if (allocation == null)
            return NotFound();

        allocation.Quantity = request.Quantity;
        allocation.ManagerId = request.ManagerId;
        allocation.StrategyId = request.StrategyId;
        allocation.OrderId = request.OrderId;

        await this.db.SaveChangesAsync();
        return Ok(allocation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var allocation = await this.db.Allocations.FirstOrDefaultAsync(a => a.AllocationId == id);
        if (allocation == null)
            return NotFound();

        this.db.Allocations.Remove(allocation);
        await this.db.SaveChangesAsync();
        return Ok();
    }
}