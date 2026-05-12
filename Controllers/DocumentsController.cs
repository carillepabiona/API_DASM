// ============================
// API - DocumentsController.cs
// ============================

using API_DASM.Data;
using API_DASM.Models;
using API_DASM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly DocumentPermissionService _permission;

        private readonly ActivityLoggerService _logger;

        public DocumentsController(AppDbContext context, DocumentPermissionService permission, ActivityLoggerService logger)
        {
            _context = context;

            _permission = permission;

            _logger = logger;
        }

        // =========================
        // GET CONTENT TYPE
        // =========================

        private string GetContentType(string extension)
        {
            extension = extension.ToLower();

            return extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".png" =>
                    "image/png",

                ".jpg" or ".jpeg" =>
                    "image/jpeg",

                ".doc" =>
                    "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" =>
                    "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".ppt" =>
                    "application/vnd.ms-powerpoint",

                ".pptx" =>
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                ".txt" =>
                    "text/plain",

                ".mp4" =>
                    "video/mp4",

                _ =>
                    "application/octet-stream"
            };
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
                // SAVE FILE
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

                        // FULL FILE PATH
                        StoragePath = fullPath,

                        UploadedBy = uploadedBy,

                        CurrentVersion = 1,

                        IsDeleted = false,

                        CreatedAt = DateTime.UtcNow,

                        UpdatedAt = DateTime.UtcNow
                    };

                _context.Documents.Add(document);

                await _context.SaveChangesAsync();

                await _logger.LogActivity(
                        uploadedBy,
                        "Upload Document",
                        document.OriginalFileName,
                        document.Id.ToString(),
                        $"Uploaded document: {document.OriginalFileName}");

                // =========================
                // CREATE VERSION
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

        // =========================
        // GET USER ACCESS
        // =========================

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

                        .Include(x => x.User)

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
        public async Task<IActionResult> ViewDocument(
            Guid id,
            Guid userId)
        {
            // CHECK ACCESS
            if (!await _permission.CanView(userId))
            {
                return Unauthorized(
                    "You do not have permission to view documents.");
            }

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // REAL FILE PATH
            var path = document.StoragePath;

            if (!System.IO.File.Exists(path))
            {
                return NotFound("File not found.");
            }

            // CONTENT TYPE
            var contentType =
                GetContentType(document.FileExtension);

            await _logger.LogActivity(
                 userId,
                 "View Document",
                 document.OriginalFileName,
                 document.Id.ToString(),
                 $"Viewed document: {document.OriginalFileName}");

            return PhysicalFile(
                path,
                contentType,
                enableRangeProcessing: true);
        }

        // =========================
        // DOWNLOAD DOCUMENT
        // =========================

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(
            Guid id,
            Guid userId)
        {
            if (!await _permission.CanDownload(userId))
            {
                return Unauthorized(
                    "No download permission.");
            }

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // REAL FILE PATH
            var path = document.StoragePath;

            if (!System.IO.File.Exists(path))
            {
                return NotFound("File not found.");
            }

            var bytes =
                await System.IO.File.ReadAllBytesAsync(path);

            await _logger.LogActivity(
                userId,
                "Download Document",
                document.OriginalFileName,
                document.Id.ToString(),
                $"Downloaded document: {document.OriginalFileName}");

            return File(
                bytes,
                GetContentType(document.FileExtension),
                document.OriginalFileName);
        }

        // =========================
        // DELETE DOCUMENT
        // =========================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(
            Guid id,
            Guid userId)
        {
            if (!await _permission.CanDelete(userId))
            {
                return Unauthorized(
                    "No delete permission.");
            }

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            document.IsDeleted = true;

            document.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _logger.LogActivity(
                 userId,
                 "Delete Document",
                 document.OriginalFileName,
                 document.Id.ToString(),
                 $"Deleted document: {document.OriginalFileName}");

            return Ok("Document deleted.");
        }

        // =========================
        // RENAME DOCUMENT
        // =========================

        [HttpPut("rename/{id}")]
        public async Task<IActionResult> RenameDocument(
            Guid id,
            Guid userId,
            RenameDocumentRequest request)
        {
            if (!await _permission.CanEdit(userId))
            {
                return Unauthorized(
                    "No edit permission.");
            }

            var document =
                await _context.Documents
                    .FirstOrDefaultAsync(x => x.Id == id);

            var oldName = document.OriginalFileName;

            if (document == null)
            {
                return NotFound();
            }

            document.OriginalFileName =
                request.NewFileName;

            document.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _logger.LogActivity(
                 userId,
                 "Rename Document",
                 oldName,
                 document.Id.ToString(),
                 $"Renamed document from {oldName} to {request.NewFileName}");

            return Ok("Document renamed.");
        }

        // =========================
        // GET DOCUMENTS BY USER
        // =========================

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetDocumentsByUser(
            Guid userId)
        {
            try
            {
                var documents =
                    await _context.Documents

                        .Include(x => x.Category)

                        .Include(x => x.Folder)

                        .Include(x => x.User)

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

                            UploadedBy =
                                x.User != null
                                ? x.User.FullName
                                : "Unknown User",

                            Category =
                                x.Category != null
                                ? x.Category.Name
                                : "",

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