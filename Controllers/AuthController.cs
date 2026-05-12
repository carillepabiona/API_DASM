using API_DASM.Data;
using API_DASM.Models;
using API_DASM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DASM_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ActivityLoggerService _logger;

        public AuthController(
    AppDbContext context,
    ActivityLoggerService logger)
        {
            _context = context;

            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == request.Username);

            if (user == null)
            {
                return Unauthorized("Invalid username.");
            }

            bool validPassword = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

            if (!validPassword)
            {
                return Unauthorized("Invalid password.");
            }

            // APP ACCESS VALIDATION

            // Admin App
            if (request.AppType == "Admin")
            {
                if (user.RoleId != 1)
                {
                    return Unauthorized(
                        "This account is not allowed in Admin App.");
                }
            }

            // Personnel App
            if (request.AppType == "Personnel")
            {
                if (user.RoleId != 2 && user.RoleId != 3)
                {
                    return Unauthorized(
                        "This account is not allowed in Personnel App.");
                }
            }

            user.LastLoginAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // ACTIVITY LOG
            await _logger.LogActivity(
                user.Id,
                "Login",
                "Authentication",
                user.Id.ToString(),
                $"{user.FullName} logged into the system.",
                request.AppType);

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                Address = user.Address,

                RoleId = user.RoleId,
                AccessLevelId = user.AccessLevelId,

                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.UserId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // ACTIVITY LOG
            await _logger.LogActivity(
                user.Id,
                "Logout",
                user.FullName,
                user.Id.ToString(),
                $"{user.FullName} logged out from the system.",
                request.AppType);

            return Ok(new
            {
                message = "Logout successful."
            });
        }


    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        // Admin or Personnel
        public string AppType { get; set; } = "";
    }
    public class LogoutRequest
    {
        public Guid UserId { get; set; }

        // Admin or Personnel
        public string AppType { get; set; } = "";
    }
}