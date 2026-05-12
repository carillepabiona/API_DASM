using API_DASM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivityLogsController(
            AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs =
                await _context.ActivityLogs

                    .Include(x => x.User)

                    .OrderByDescending(x => x.CreatedAt)

                    .Select(x => new
                    {
                        x.Id,

                        User =
                            x.User != null
                            ? x.User.FullName
                            : "Unknown User",

                        x.Action,

                        x.EntityName,

                        x.EntityId,

                        x.Description,

                        x.AppType,

                        x.CreatedAt
                    })

                    .ToListAsync();

            return Ok(logs);
        }
    }
}