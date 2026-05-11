namespace API_DASM.Models
{
    public class DocumentVersion
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public int VersionNumber { get; set; }

        public string FilePath { get; set; }

        public Guid UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; }

        // NAVIGATION

        public Document? Document { get; set; }
    }
}