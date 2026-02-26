using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PddTrainer.Api.Data;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using PddTrainer.Api.Services;
using PddTrainer.Api.Services.Interfaces;
using Serilog;
using System.Security.Claims;

namespace PddTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == email);

            if (existingUser)
                return BadRequest(new { message = "User already exists" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash);

            if (!isValidPassword)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _jwtService.GenerateToken(existingUser);

            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(2)
            });

            return Ok(new { message = "Logged in successfully" });
        }

        // Тестовый метод для проверки авторизации в Swagger.
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new { userId, email });
        }
        
    }
}
