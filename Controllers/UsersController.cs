using API_DASM.Data;
using API_DASM.DTOs;
using API_DASM.Hubs;
using API_DASM.Models;
using API_DASM.Services;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<UserHub> _hub;

        private readonly ActivityLoggerService _logger;

        public UsersController(AppDbContext context, IHubContext<UserHub> hub, ActivityLoggerService logger)
        {
            _context = context;

            _hub = hub;

            _logger = logger;
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

            await _logger.LogActivity(
                 dto.CreatedBy,
                 "Create User",
                 user.Username,
                 user.Id.ToString(),
                 $"Created user account: {user.Username}");

            return Ok(user);
        }



        [HttpPut("update-profile/{id}")]
        public async Task<IActionResult> UpdateProfile(
    Guid id,UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // ONLY UPDATE THESE
            user.ContactNumber = request.ContactNumber;
            user.Address = request.Address;

            await _context.SaveChangesAsync();

            await _logger.LogActivity(
                 id,
                 "Update Profile",
                 user.FullName,
                 user.Id.ToString(),
                 "Updated profile information");

            return Ok(new
            {
                message = "Profile updated successfully."
            });
        }

        [HttpPut("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(
    Guid id,
    ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // VERIFY CURRENT PASSWORD
            bool validPassword = BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash);

            if (!validPassword)
            {
                return BadRequest("Current password is incorrect.");
            }

            // HASH NEW PASSWORD
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.NewPassword);

            // OPTIONAL
            user.MustChangePassword = false;

            await _context.SaveChangesAsync();

            await _logger.LogActivity(
     id,
                 "Change Password",
                 user.Username,
                 user.Id.ToString(),
                 "Changed account password");

            return Ok(new
            {
                message = "Password updated successfully."
            });
        }

        [HttpGet("access/{userId}")]
        public async Task<IActionResult> GetAccess(Guid userId)
        {
            var user = await _context.Users

                .Include(x => x.AccessLevel)

                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            if (user.AccessLevel == null)
            {
                return BadRequest("Access level not found.");
            }

            return Ok(new
            {
                CanView = user.AccessLevel.CanView,
                CanEdit = user.AccessLevel.CanEdit,
                CanShare = user.AccessLevel.CanShare,
                CanDownload = user.AccessLevel.CanDownload,
                CanDelete = user.AccessLevel.CanDelete
            });
        }
    }
}
