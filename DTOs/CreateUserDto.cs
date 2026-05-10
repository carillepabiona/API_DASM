namespace API_DASM.DTOs
{
    public class CreateUserDto
    {
        public string UserCode { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string ContactNumber { get; set; }

        public string Address { get; set; }

        public int RoleId { get; set; }

        public int AccessLevelId { get; set; }

        public bool IsActive { get; set; }
    }
}
