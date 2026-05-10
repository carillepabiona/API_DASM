namespace API_DASM.Models
{
    public class AccessLevel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool CanView { get; set; }

        public bool CanEdit { get; set; }

        public bool CanShare { get; set; }

        public bool CanDownload { get; set; }

        public bool CanDelete { get; set; }
    }
}
