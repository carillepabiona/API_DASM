// ============================
// API - DocumentsController.cs
// ============================

using API_DASM.Data;
using API_DASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DocumentsController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // UPLOAD DOCUMENT
        // =========================

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            [FromForm] string? category,
            [FromForm] string? folderId,
            [FromForm] Guid uploadedBy)
        {
            try
            {
                // =========================
                // VALIDATE FILE
                // =========================

                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                // =========================
                // VALIDATE USER
                // =========================

                var userExists =
                    await _context.Users
                        .AnyAsync(x => x.Id == uploadedBy);

                if (!userExists)
                {
                    return BadRequest(
                        $"User does not exist: {uploadedBy}");
                }

                // =========================
                // VALIDATE CATEGORY
                // =========================

                Guid? categoryId = null;

                if (!string.IsNullOrWhiteSpace(category))
                {
                    var dbCategory =
                        await _context.Categories
                            .FirstOrDefaultAsync(x =>
                                x.Name == category);

                    if (dbCategory != null)
                    {
                        categoryId = dbCategory.Id;
                    }
                    else
                    {
                        return BadRequest(
                            $"Category not found: {category}");
                    }
                }

                // =========================
                // VALIDATE FOLDER
                // =========================

                Guid? parsedFolderId = null;

                if (!string.IsNullOrWhiteSpace(folderId))
                {
                    if (!Guid.TryParse(folderId, out Guid folderGuid))
                    {
                        return BadRequest("Invalid folder ID.");
                    }

                    var folderExists =
                        await _context.Folders
                            .AnyAsync(x => x.Id == folderGuid);

                    if (!folderExists)
                    {
                        return BadRequest(
                            $"Folder does not exist: {folderId}");
                    }

                    parsedFolderId = folderGuid;
                }

                // =========================
                // CREATE STORAGE FOLDER
                // =========================

                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "UploadedFiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // =========================
                // GENERATE UNIQUE FILE NAME
                // =========================

                var uniqueFileName =
                    $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                var fullPath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                // =========================
                // SAVE FILE TO SERVER
                // =========================

                using (var stream =
                       new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // =========================
                // CREATE DOCUMENT
                // =========================

                var document =
                    new API_DASM.Models.Document
                    {
                        Id = Guid.NewGuid(),

                        FolderId = parsedFolderId,

                        CategoryId = categoryId,

                        FileName = uniqueFileName,

                        OriginalFileName = file.FileName,

                        FileExtension =
                            Path.GetExtension(file.FileName),

                        FileSize = file.Length,

                        StoragePath = fullPath,

                        UploadedBy = uploadedBy,

                        CurrentVersion = 1,

                        IsDeleted = false,

                        CreatedAt = DateTime.UtcNow,

                        UpdatedAt = DateTime.UtcNow
                    };

                // =========================
                // SAVE TO DATABASE
                // =========================

                _context.Documents.Add(document);

                await _context.SaveChangesAsync();

                // =========================
                // CREATE DOCUMENT VERSION
                // =========================

                var version =
                    new DocumentVersion
                    {
                        Id = Guid.NewGuid(),

                        DocumentId = document.Id,

                        VersionNumber = 1,

                        FilePath = fullPath,

                        UploadedBy = uploadedBy,

                        UploadedAt = DateTime.UtcNow
                    };

                _context.DocumentVersions.Add(version);

                await _context.SaveChangesAsync();

                // =========================
                // RETURN SUCCESS
                // =========================

                return Ok(new
                {
                    success = true,
                    message = "File uploaded successfully.",
                    documentId = document.Id,
                    fileName = document.OriginalFileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    error = ex.ToString()
                });
            }
        }

        // =========================
        // GET DOCUMENTS
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            var documents =
                await _context.Documents
                    .Include(x => x.Category)
                    .Include(x => x.Folder)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        x.Id,

                        x.OriginalFileName,

                        x.FileExtension,

                        x.FileSize,

                        x.StoragePath,

                        x.CreatedAt,

                        Category =
                            x.Category != null
                            ? x.Category.Name
                            : null,

                        Folder =
                            x.Folder != null
                            ? x.Folder.Name
                            : "Root"
                    })
                    .ToListAsync();

            return Ok(documents);
        }

        // =========================
        // VIEW FILE
        // =========================

        [HttpGet("view/{id}")]
        public async Task<IActionResult> ViewFile(Guid id)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            if (!System.IO.File.Exists(document.StoragePath))
            {
                return NotFound("Physical file missing.");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(document.StoragePath);

            return File(
                bytes,
                "application/octet-stream",
                document.OriginalFileName);
        }

        // =========================
        // DOWNLOAD DOCUMENT
        // =========================

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            if (!System.IO.File.Exists(document.StoragePath))
            {
                return NotFound("Physical file not found.");
            }

            var bytes =
                await System.IO.File.ReadAllBytesAsync(
                    document.StoragePath);

            return File(
                bytes,
                "application/octet-stream",
                document.OriginalFileName);
        }

        // =========================
        // DELETE DOCUMENT
        // =========================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message = "Document not found."
                    });
            }

            // =========================
            // DELETE PHYSICAL FILE
            // =========================

            if (System.IO.File.Exists(document.StoragePath))
            {
                System.IO.File.Delete(document.StoragePath);
            }

            // =========================
            // DELETE DOCUMENT VERSIONS
            // =========================

            var versions =
                await _context.DocumentVersions
                    .Where(x => x.DocumentId == id)
                    .ToListAsync();

            _context.DocumentVersions.RemoveRange(versions);

            // =========================
            // DELETE DOCUMENT
            // =========================

            _context.Documents.Remove(document);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Document deleted successfully."
            });
        }
    }
}