namespace API_DASM.DTOs
{
    public class DocumentItemDto
    {
        public Guid Id { get; set; }

        public string OriginalFileName { get; set; } = "";

        public string FileExtension { get; set; } = "";

        public long FileSize { get; set; }

        public DateTime CreatedAt { get; set; }

        public string UploadedBy { get; set; } = "";
    }
}
