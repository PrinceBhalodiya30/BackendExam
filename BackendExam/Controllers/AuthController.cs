using BackendExam.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackendExam.Controllers;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly TicketManagementContext _context;
    private readonly IConfiguration _config;

    public AuthController(TicketManagementContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet("managerdata")]
    public IActionResult managerdata()
    {
        if (!_context.Roles.Any())
        {
            _context.Roles.AddRange(
                new Role { Name = "MANAGER" },
                new Role { Name = "SUPPORT" },
                new Role { Name = "USER" }
            );
            _context.SaveChanges();
        }

        var adminRole = _context.Roles.First(r => r.Name == "MANAGER");
        var user = _context.Users.FirstOrDefault(x => x.Email == "manager@example.com");

        if (user != null)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword("password123");
            _context.SaveChanges();
            return Ok(new { message = "Manager account is ready. Password: password123" });
        }

        user = new User
        {
            Name = "Initial Manager",
            Email = "manager@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            RoleId = adminRole.Id,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok(new { message = "Manager created successfully.", user = user.Email });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDTO dto)
    {
        var user = _context.Users.Include(x => x.Role)
            .FirstOrDefault(x => x.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return Unauthorized();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        return Ok(new AuthResponse { Token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
