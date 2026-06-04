using EcoHub.API.Data;
using EcoHub.API.Services;
using EcoHub.Shared.Enums;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public UsersController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAll()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpGet("new")]
        public async Task<ActionResult<List<UserDto>>> GetNewUsers([FromQuery] DateTime since)
        {
            var users = await _context.Users
                .Where(u => u.CreatedAt > since)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [AllowAnonymous]
        [HttpPost("seed-admin")]
        public async Task<IActionResult> SeedAdmin()
        {
            if (await _context.Users.AnyAsync(u => u.Role == UserRole.Admin))
                return BadRequest(new { message = "Admin already exists" });

            var admin = new Data.Models.User
            {
                Email = "admin@ecohub.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "System",
                LastName = "Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Admin created", email = admin.Email });
        }
    }
}
