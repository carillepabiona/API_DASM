// ============================
// API - Folder.cs
// ============================

namespace API_DASM.Models
{
    public class Folder
    {
        public Guid Id { get; set; }

        public Guid? ParentFolderId { get; set; }

        public string Name { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        // NAVIGATION
        public ICollection<Document> Documents { get; set; }
    }
}