using API_DASM.Data;
using API_DASM.DTOs;
using API_DASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_DASM.Services;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly ActivityLoggerService _logger;

        public FoldersController(AppDbContext context, ActivityLoggerService logger )
        {
            _context = context;
            _logger = logger;
        }

        // =========================
        // GET ALL FOLDERS
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            var folders = await _context.Folders
                .Select(x => new
                {
                    x.Id,
                    x.Name,

                    FileCount =
                        _context.Documents.Count(d =>
                            d.FolderId == x.Id &&
                            !d.IsDeleted)
                })
                .ToListAsync();

            return Ok(folders);
        }

        // =========================
        // CREATE FOLDER
        // =========================

        [HttpPost]
        public async Task<IActionResult> CreateFolder(
            CreateFolderDto dto)
        {
            // VALIDATION
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Folder name is required.");
            }

            // CHECK DUPLICATE
            bool exists = await _context.Folders.AnyAsync(x =>
                x.Name == dto.Name &&
                x.ParentFolderId == dto.ParentFolderId);

            if (exists)
            {
                return BadRequest(
                    "Folder already exists.");
            }

            var folder = new Folder
            {
                Id = Guid.NewGuid(),

                Name = dto.Name,

                ParentFolderId = dto.ParentFolderId,

                CreatedBy = dto.CreatedBy,

                CreatedAt = DateTime.UtcNow
            };

            _context.Folders.Add(folder);

            await _context.SaveChangesAsync();
            
            await _logger.LogActivity(
                dto.CreatedBy,
                "Create Folder",
                folder.Name,
                folder.Id.ToString(),
                $"Created folder: {folder.Name}");

            return Ok(folder);
        }
    }
}