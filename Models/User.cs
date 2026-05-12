using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace API_DASM.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        public string UserCode { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string ContactNumber { get; set; }

        public string Address { get; set; }

        public string PasswordHash { get; set; }

        public int RoleId { get; set; }

        public int AccessLevelId { get; set; }

        public bool IsActive { get; set; }

        public bool MustChangePassword { get; set; }

        public DateTime CreatedAt { get; set; }

        // REAL-TIME STATUS

        public DateTime? LastLoginAt { get; set; }

        // NAVIGATION
        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        [ForeignKey(nameof(AccessLevelId))]
        public AccessLevel? AccessLevel { get; set; }


    }
}
