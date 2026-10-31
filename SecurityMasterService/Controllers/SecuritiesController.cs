using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecurityMasterService.Data;
using SecurityMasterService.Hubs;

namespace SecurityMasterService.Controllers;

[ApiController]
[Route("securities")]
public class SecuritiesController : ControllerBase
{
    private readonly SecurityMasterDbContext db;
    private readonly ILogger<SecuritiesController> logger;
    private readonly IHubContext<NotificationHub> hub;
    
    public SecuritiesController(SecurityMasterDbContext db, ILogger<SecuritiesController> logger, IHubContext<NotificationHub> hub)
    {
        this.db = db;
        this.logger = logger;
        this.hub = hub;
    }

    [HttpPost("query")]
    public async Task<ActionResult<List<Security>>> Query([FromBody] ServiceQuery<Security> query)
    {

        var baseQuery = this.db.Securities.AsNoTracking().AsQueryable();
            return await baseQuery.ExecuteAsync(query);
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
        await this.hub.Clients.All.SendAsync(NotificationHub.SecuritiesChanged);
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
        await this.hub.Clients.All.SendAsync(NotificationHub.SecuritiesChanged);
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
         await this.hub.Clients.All.SendAsync(NotificationHub.SecuritiesChanged);
        await this.db.SaveChangesAsync();
        return Ok();
    }
}