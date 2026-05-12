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
                    new Document
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
                    fileName = document.OriginalFileName,
                    folderId = document.FolderId
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

        // =========================================
        // GET USER ACCESS LEVEL
        // =========================================

        private async Task<AccessLevel?> GetUserAccess(Guid userId)
        {
            var user =
                await _context.Users
                    .Include(x => x.AccessLevel)
                    .FirstOrDefaultAsync(x => x.Id == userId);

            return user?.AccessLevel;
        }

        // =========================
        // GET DOCUMENTS
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var documents =
                    await _context.Documents

                        .Include(x => x.Category)

                        .Include(x => x.Folder)

                        .Where(x => !x.IsDeleted)

                        .OrderByDescending(x => x.CreatedAt)

                        .Select(x => new
                        {
                            x.Id,

                            x.OriginalFileName,

                            x.FileExtension,

                            x.FileSize,

                            x.StoragePath,

                            x.CreatedAt,

                            x.FolderId,

                            x.CategoryId,

                            UploadedBy =
                        x.User != null
                        ? x.User.FullName
                        : "Unknown User",

                            Category =
                                x.Category != null
                                ? x.Category.Name
                                : "",

                            Folder =
                                x.Folder != null
                                ? x.Folder.Name
                                : "Root"
                        })

                        .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // =========================
        // VIEW FILE
        // =========================

        [HttpGet("view/{id}")]
        public async Task<IActionResult> ViewFile(
    Guid id,
    [FromQuery] Guid userId)
        {
            // =========================
            // CHECK ACCESS
            // =========================

            var access = await GetUserAccess(userId);

            if (access == null || !access.CanView)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "You do not have permission to view files."
                    });
            }

            // =========================
            // FIND DOCUMENT
            // =========================

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            // =========================
            // CHECK FILE EXISTS
            // =========================

            if (!System.IO.File.Exists(document.StoragePath))
            {
                return NotFound("Physical file missing.");
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
        // DOWNLOAD DOCUMENT
        // =========================

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(
     Guid id,
     [FromQuery] Guid userId)
        {
            // =========================
            // CHECK ACCESS
            // =========================

            var access = await GetUserAccess(userId);

            if (access == null || !access.CanDownload)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "You do not have permission to download."
                    });
            }

            // =========================
            // FIND DOCUMENT
            // =========================

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            // =========================
            // CHECK FILE EXISTS
            // =========================

            if (!System.IO.File.Exists(document.StoragePath))
            {
                return NotFound("Physical file missing.");
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
        public async Task<IActionResult> DeleteDocument(
    Guid id,
    [FromQuery] Guid userId)
        {
            // =========================
            // CHECK ACCESS
            // =========================

            var access = await GetUserAccess(userId);

            if (access == null || !access.CanDelete)
            {
                return Unauthorized(
                    new
                    {
                        success = false,
                        message = "You do not have permission to delete."
                    });
            }

            // =========================
            // FIND DOCUMENT
            // =========================

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
            // DELETE FILE
            // =========================

            if (System.IO.File.Exists(document.StoragePath))
            {
                System.IO.File.Delete(document.StoragePath);
            }

            // =========================
            // DELETE VERSIONS
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

        // =========================================
        // GET DOCUMENTS BY USER
        // =========================================

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetDocumentsByUser(Guid userId)
        {
            try
            {
                var documents =
                    await _context.Documents

                        .Include(x => x.Category)

                        .Include(x => x.Folder)

                        .Where(x =>
                            !x.IsDeleted &&
                            x.UploadedBy == userId)

                        .OrderByDescending(x => x.CreatedAt)

                        .Select(x => new
                        {
                            x.Id,

                            x.OriginalFileName,

                            x.FileSize,

                            x.StoragePath,

                            x.CreatedAt,

                            Category =
                                x.Category != null
                                ? x.Category.Name
                                : "",

                            x.UploadedBy,

                            x.FolderId
                        })

                        .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}