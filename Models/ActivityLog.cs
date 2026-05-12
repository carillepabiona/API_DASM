namespace API_DASM.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }

        // USER
        public Guid UserId { get; set; }

        public User? User { get; set; }

        // ACTION
        public string Action { get; set; } = "";

        // ENTITY
        public string? EntityName { get; set; }

        public string? EntityId { get; set; }

        // DETAILS
        public string? Description { get; set; }

        // EXTRA
        public string? AppType { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        // DATE
        public DateTime CreatedAt { get; set; }
    }
}
