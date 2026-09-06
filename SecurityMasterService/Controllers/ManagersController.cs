using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

    [ApiController]
    [Route("managers")]
    public class ManagersController : ControllerBase
    {
    private readonly SecurityMasterDbContext db;
    private readonly AllowedFields<Manager> allowedFields;
    private readonly ILogger<ManagersController> logger;

    public ManagersController(SecurityMasterDbContext db, AllowedFields<Manager> allowedFields, ILogger<ManagersController> logger)
    {
        this.db = db;
        this.allowedFields = allowedFields;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var baseQuery = this.db.Managers.AsQueryable();
        return await QueryHelper.HandleQuery(Request.Query, baseQuery, this.allowedFields.Fields);
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
