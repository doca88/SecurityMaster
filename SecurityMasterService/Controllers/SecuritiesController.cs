using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("securities")]
public class SecuritiesController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly AllowedFields<Security> allowedFields;

    public SecuritiesController(SecurityMasterDbContext db, AllowedFields<Security> allowedFields)
    {
        this.db = db;
        this.allowedFields = allowedFields;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var baseQuery = this.db.Securities.AsQueryable();
        return await QueryHelper.HandleQuery(Request.Query, baseQuery, this.allowedFields.Fields);
    }

    [HttpGet("{sid}")]
    public async Task<IActionResult> GetById(long sid)
    {
        var security = await this.db.Securities.AsNoTracking().FirstOrDefaultAsync(s => s.Sid == sid);
        if (security == null)
            return NotFound();
        return Ok(security);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecurityRequest request)
    {
        var security = new Security
        {
            Sid = request.Sid,
            Description = request.Description
        };

        this.db.Securities.Add(security);
        await this.db.SaveChangesAsync();
        return Created($"/securities/{security.Sid}", security);
    }

    [HttpPut("{sid}")]
    public async Task<IActionResult> Update(long sid, [FromBody] CreateSecurityRequest request)
    {
        var security = await this.db.Securities.FirstOrDefaultAsync(s => s.Sid == sid);
        if (security == null)
            return NotFound();

        security.Description = request.Description;

        await this.db.SaveChangesAsync();
        return Ok(security);
    }

    [HttpDelete("{sid}")]
    public async Task<IActionResult> Delete(long sid)
    {
        var security = await this.db.Securities.FirstOrDefaultAsync(s => s.Sid == sid);
        if (security == null)
            return NotFound();

        var isUsed = await this.db.Orders.AnyAsync(o => o.Sid == sid);
        if (isUsed)
            return Conflict("Security is already used, unable to delete it.");

        this.db.Securities.Remove(security);
        await this.db.SaveChangesAsync();
        return Ok();
    }
}