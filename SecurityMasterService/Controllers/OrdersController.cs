using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly ILogger<OrdersController> logger;
    public OrdersController(SecurityMasterDbContext db, ILogger<OrdersController> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    [HttpPost("query")]
    public async Task<ActionResult<List<Order>>> Query([FromBody] ServiceQuery<Order> query)
    {
        var baseQuery = this.db.Orders
        .Include(o => o.Manager)
        .Include(o => o.Strategy)
        .Include(o => o.Security)
        .Include(o => o.Allocations)
            .ThenInclude(a => a.Manager)
        .Include(o => o.Allocations)
            .ThenInclude(a => a.Strategy)
        .AsNoTracking()
        .AsQueryable();

       return await baseQuery.ExecuteAsync(query);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await this.LoadOrder(id);
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

        var allocationError = await this.ValidateAllocations(request.Allocations);
        if (allocationError != null)
            return BadRequest(allocationError);

        // Novi nalog: sve alokacije su nove; EF im sam upisuje OrderId pri snimanju
        var allocations = new List<Allocation>();
        foreach (var a in request.Allocations)
            allocations.Add(await this.NewAllocation(a));

        var order = new Order
        {
            Quantity = request.Quantity,
            TradeDate = request.TradeDate,
            ManagerId = request.ManagerId,
            StrategyId = request.StrategyId,
            Sid = request.Sid,
            Manager = manager,
            Strategy = strategy,
            Security = security,
            Allocations = allocations
        };

        this.db.Orders.Add(order);
        await this.db.SaveChangesAsync();

        // Vraćamo nalog ponovo učitan iz baze, da alokacije imaju Manager i Strategy
        return Created($"/orders/{order.OrderId}", await this.LoadOrder(order.OrderId));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateOrderRequest request)
    {
        var order = await this.db.Orders
            .Include(o => o.Allocations)
            .FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return NotFound();

        var allocationError = await this.ValidateAllocations(request.Allocations);
        if (allocationError != null)
            return BadRequest(allocationError);

        order.Quantity = request.Quantity;
        order.TradeDate = request.TradeDate;
        order.ManagerId = request.ManagerId;
        order.StrategyId = request.StrategyId;
        order.Sid = request.Sid;

        // Lista iz zahteva je kompletno stanje alokacija naloga:
        // AllocationId == 0 -> insert, postojeći id -> update, ono čega nema u listi -> delete
        var keptIds = request.Allocations
            .Where(a => a.AllocationId > 0)
            .Select(a => a.AllocationId)
            .ToList();

        var removed = order.Allocations
            .Where(a => !keptIds.Contains(a.AllocationId))
            .ToList();
        this.db.Allocations.RemoveRange(removed);

        foreach (var a in request.Allocations)
        {
            if (a.AllocationId == 0)
            {
                var added = await this.NewAllocation(a);
                added.OrderId = order.OrderId;
                this.db.Allocations.Add(added);
                continue;
            }

            var existing = order.Allocations.FirstOrDefault(x => x.AllocationId == a.AllocationId);
            if (existing == null)
                return BadRequest($"Allocation {a.AllocationId} does not belong to order {id}.");

            existing.ManagerId = a.ManagerId;
            existing.StrategyId = a.StrategyId;
            existing.Quantity = a.Quantity;
        }

        await this.db.SaveChangesAsync();
        return Ok(await this.LoadOrder(id));
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

    /// <summary>Nalog sa svim podacima koje forma prikazuje (i alokacije sa Manager/Strategy)</summary>
    private Task<Order?> LoadOrder(int id)
    {
        return this.db.Orders
            .Include(o => o.Manager)
            .Include(o => o.Strategy)
            .Include(o => o.Security)
            .Include(o => o.Allocations)
                .ThenInclude(a => a.Manager)
            .Include(o => o.Allocations)
                .ThenInclude(a => a.Strategy)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == id);
    }

    /// <summary>
    /// Nova alokacija iz zahteva. Manager i Strategy su required, pa ih postavljamo.
    /// Već su provereni u ValidateAllocations: FindAsync ih vraća iz memorije (bez novog upita) i nisu null.
    /// </summary>
    private async Task<Allocation> NewAllocation(AllocationRequest a)
    {
        var manager = await this.db.Managers.FindAsync(a.ManagerId);
        var strategy = await this.db.Strategies.FindAsync(a.StrategyId);

        return new Allocation
        {
            ManagerId = a.ManagerId,
            StrategyId = a.StrategyId,
            Quantity = a.Quantity,
            Manager = manager!,     // ! = "sigurno nije null" (provereno u ValidateAllocations)
            Strategy = strategy!
        };
    }

    /// <summary>Vraća poruku o grešci, ili null ako su sve alokacije ispravne</summary>
    private async Task<string?> ValidateAllocations(List<AllocationRequest> allocations)
    {
        foreach (var a in allocations)
        {
            if (a.Quantity <= 0)
                return "Allocation quantity must be greater than 0.";

            if (await this.db.Managers.FindAsync(a.ManagerId) == null)
                return $"Manager with id {a.ManagerId} does not exist.";

            if (await this.db.Strategies.FindAsync(a.StrategyId) == null)
                return $"Strategy with id {a.StrategyId} does not exist.";
        }
        return null;
    }
}