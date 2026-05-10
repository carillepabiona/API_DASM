using API_DASM.Data;
using API_DASM.DTOs;
using API_DASM.Hubs;
using API_DASM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<UserHub> _hub;

        public UsersController(AppDbContext context,
            IHubContext<UserHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(x => x.Role)
                .Include(x => x.AccessLevel)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            var usernameExists = await _context.Users
                .AnyAsync(x => x.Username == dto.Username);

            if (usernameExists)
            {
                return BadRequest("Username already exists.");
            }

            var emailExists = await _context.Users
                .AnyAsync(x => x.Email == dto.Email);

            if (emailExists)
            {
                return BadRequest("Email already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),

                UserCode = dto.UserCode,

                FullName = dto.FullName,

                Username = dto.Username,

                Email = dto.Email,

                ContactNumber = dto.ContactNumber,

                Address = dto.Address,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password),

                RoleId = dto.RoleId,

                AccessLevelId = dto.AccessLevelId,

                IsActive = dto.IsActive,

                MustChangePassword = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(user);
        }
    }
}
