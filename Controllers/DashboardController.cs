using API_DASM.Data;
using API_DASM.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // GET DASHBOARD DATA
        // =========================================

        [HttpGet]
        public async Task<ActionResult<DashboardDataDto>> GetDashboard()
        {
            var totalDocuments =
                await _context.Documents.CountAsync();

            var recentlyUploaded =
                await _context.Documents
                    .Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();

            var uploadedToday =
                await _context.Documents
                    .Where(x => x.CreatedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync();

            // =========================================
            // ACTIVE USERS
            // =========================================

            var activeUsers =
                await _context.Users
                    .Where(x => x.IsActive)
                    .CountAsync();

            // =========================================
            // RECENT FILES
            // =========================================

            var recentFiles =
                await _context.Documents
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .Select(x => new DocumentItemDto
                    {
                        Id = x.Id,
                        OriginalFileName = x.OriginalFileName,
                        FileExtension = x.FileExtension,
                        FileSize = x.FileSize,
                        CreatedAt = x.CreatedAt,
                        UploadedBy = x.User.FullName
                    })
                    .ToListAsync();

            // =========================================
            // TODAY'S UPLOADS
            // =========================================

            var todaysUploads =
                await _context.Documents
                    .Where(x => x.CreatedAt.Date == DateTime.UtcNow.Date)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new DocumentItemDto
                    {
                        Id = x.Id,
                        OriginalFileName = x.OriginalFileName,
                        FileExtension = x.FileExtension,
                        FileSize = x.FileSize,
                        CreatedAt = x.CreatedAt,
                        UploadedBy = x.User.FullName
                    })
                    .ToListAsync();

            var result = new DashboardDataDto
            {
                TotalDocuments = totalDocuments,
                RecentlyUploaded = recentlyUploaded,
                UploadedToday = uploadedToday,

                // =====================================
                // SET ACTIVE USERS
                // =====================================

                ActiveUsers = activeUsers,

                RecentFiles = recentFiles,
                TodaysUploads = todaysUploads
            };

            return Ok(result);
        }
    }
}
