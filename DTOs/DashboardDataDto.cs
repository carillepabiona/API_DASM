namespace API_DASM.DTOs
{
    public class DashboardDataDto
    {
        public int TotalDocuments { get; set; }

        public int RecentlyUploaded { get; set; }

        public int UploadedToday { get; set; }

        public int ActiveUsers { get; set; }

        public List<DocumentItemDto> RecentFiles { get; set; } = new();

        public List<DocumentItemDto> TodaysUploads { get; set; } = new();
    }
}
