using API_DASM.Data;
using API_DASM.Models;
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

        public AuthController(AppDbContext context)
        {
            _context = context;
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

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                Address = user.Address,
                RoleId = user.RoleId,
                CreatedAt = user.CreatedAt
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
}