using BackendExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendExam.Controllers;

[Route("users")]
[ApiController]
[Authorize(Roles = "MANAGER")]
public class UsersController : ControllerBase
{
    private readonly TicketManagementContext _context;

    public UsersController(TicketManagementContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.Users.Include(x => x.Role).Select(x => new
        {
            x.Id,
            x.Name,
            x.Email,
            Role = x.Role.Name,
            x.CreatedAt
        }).ToListAsync();
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDTO dto)
    {
        if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
        {
            return BadRequest("Email used.");
        }
        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
        {
            return BadRequest("Role not found.");
        }
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = dto.RoleId,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = role.Name,
            user.CreatedAt
        });
    }
}
