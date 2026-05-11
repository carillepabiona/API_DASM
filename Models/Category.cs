// ============================
// API - Category.cs
// ============================

namespace API_DASM.Models
{
    public class Category
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }
    }
}