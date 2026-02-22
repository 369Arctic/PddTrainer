using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PddTrainer.Api.Data;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using Serilog;

namespace PddTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
