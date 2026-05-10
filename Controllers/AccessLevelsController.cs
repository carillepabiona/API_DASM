using API_DASM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessLevelsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccessLevelsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccessLevels()
        {
            var accessLevels =
                await _context.AccessLevels.ToListAsync();

            return Ok(accessLevels);
        }
    }
}
