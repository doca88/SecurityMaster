using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("managers")]
    public class ManagersController : ControllerBase
    {
    private readonly SecurityMasterDbContext _db;
    private readonly AllowedFields<Manager> _allowedFields;

    public ManagersController(SecurityMasterDbContext db, AllowedFields<Manager> allowedFields)
    {
        _db = db;
        _allowedFields = allowedFields;
    }

   [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? filter)
    {
        IQueryable<Manager> query = _db.Managers.AsNoTracking();

        var baseQuery = _db.Managers.AsQueryable();
        var result = await QueryHelper.HandleQuery(Request.Query, baseQuery, _allowedFields.Fields);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateManagerRequest request)
    {
        var manager = new Manager
        {
            Display = request.Display
        };

        _db.Managers.Add(manager);
        await _db.SaveChangesAsync();
        return Created($"/managers/{manager.Id}", manager);
    }
}
