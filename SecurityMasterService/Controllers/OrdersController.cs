using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly AllowedFields<Order> allowedFields;
    private readonly ILogger<ManagersController> logger;
    public OrdersController(SecurityMasterDbContext db, AllowedFields<Order> allowedFields, ILogger<ManagersController> logger)
    {
        this.db = db;
        this.allowedFields = allowedFields;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var baseQuery = this.db.Orders
       .Include(o => o.Manager)
       .Include(o => o.Strategy)
       .Include(o => o.Security)
       .Include(o => o.Allocations)
       .AsQueryable();
        logger.LogInformation("Get orders is called!!!");
        return await QueryHelper.HandleQuery(Request.Query, baseQuery, this.allowedFields.Fields);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await this.db.Orders
        .Include(o => o.Manager)
        .Include(o => o.Strategy)
        .Include(o => o.Security)
        .Include(o => o.Allocations)
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var manager = await this.db.Managers.FindAsync(request.ManagerId);
        if (manager == null)
            return BadRequest($"Manager with id {request.ManagerId} does not exist.");

        var strategy = await this.db.Strategies.FindAsync(request.StrategyId);
        if (strategy == null)
            return BadRequest($"Strategy with id {request.StrategyId} does not exist.");

        var security = await this.db.Securities.FindAsync(request.Sid);
        if (security == null)
            return BadRequest($"Security with sid {request.Sid} does not exist.");

        var order = new Order
        {
            Quantity = request.Quantity,
            TradeDate = request.TradeDate,
            ManagerId = request.ManagerId,
            StrategyId = request.StrategyId,
            Sid = request.Sid,
            Manager = manager,
            Strategy = strategy,
            Security = security
        };

        this.db.Orders.Add(order);
        await this.db.SaveChangesAsync();
        return Created($"/orders/{order.OrderId}", order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateOrderRequest request)
    {
        var order = await this.db.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return NotFound();

        order.Quantity = request.Quantity;
        order.TradeDate = request.TradeDate;
        order.ManagerId = request.ManagerId;
        order.StrategyId = request.StrategyId;
        order.Sid = request.Sid;

        await this.db.SaveChangesAsync();
        return Ok(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await this.db.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return NotFound();

        var hasAllocations = await this.db.Allocations.AnyAsync(a => a.OrderId == id);
        if (hasAllocations)
            return Conflict("Order is already used, unable to delete it.");

        this.db.Orders.Remove(order);
        await this.db.SaveChangesAsync();
        return Ok();
    }
}