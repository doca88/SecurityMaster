using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

    [ApiController]
    [Route("managers")]
    public class ManagersController : ControllerBase
    {
    private readonly SecurityMasterDbContext db;

    private readonly ILogger<ManagersController> logger;

    public ManagersController(SecurityMasterDbContext db, ILogger<ManagersController> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    [HttpPost("query")]
    public async Task<ActionResult<List<Manager>>> Query([FromBody] ServiceQuery<Manager> query)
    {
        var baseQuery = this.db.Managers.AsNoTracking().AsQueryable();
        return await baseQuery.ExecuteAsync(query);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var manager = await this.db.Managers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (manager == null)
            return NotFound();
        return Ok(manager);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateManagerRequest request)
    {
        var manager = new Manager
        {
            Display = request.Display
        };

        this.db.Managers.Add(manager);
        await this.db.SaveChangesAsync();
        return Created($"/managers/{manager.Id}", manager);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateManagerRequest request)
    {
        var manager = await this.db.Managers.FirstOrDefaultAsync(m => m.Id == id);
        if (manager == null)
            return NotFound();

        manager.Display = request.Display;

        await this.db.SaveChangesAsync();
        return Ok(manager);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var manager = await this.db.Managers.FirstOrDefaultAsync(m => m.Id == id);
        if (manager == null)
            return NotFound();

        var hasOrders = await this.db.Orders.AnyAsync(o => o.ManagerId == id);
        if (hasOrders)
            return Conflict("Manager is already used, unable to delete it.");

        this.db.Managers.Remove(manager);
        await this.db.SaveChangesAsync();
        return Ok();
    }
}
