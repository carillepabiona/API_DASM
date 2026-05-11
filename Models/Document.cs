// ============================
// API - Document.cs
// ============================

namespace API_DASM.Models
{
    public class Document
    {
        public Guid Id { get; set; }

        public Guid? FolderId { get; set; }

        public Folder? Folder { get; set; }

        public Guid? CategoryId { get; set; }

        public Category? Category { get; set; }

        public string FileName { get; set; }

        public string OriginalFileName { get; set; }

        public string FileExtension { get; set; }

        public long FileSize { get; set; }

        public string StoragePath { get; set; }

        // =========================================
        // USER RELATIONSHIP
        // =========================================

        public Guid UploadedBy { get; set; }

        public User? User { get; set; }

        // =========================================

        public int CurrentVersion { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}