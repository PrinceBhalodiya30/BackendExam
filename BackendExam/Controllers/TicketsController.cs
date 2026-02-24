using BackendExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackendExam.Controllers;

[Route("tickets")]
[ApiController]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly TicketManagementContext _context;

    public TicketsController(TicketManagementContext context) => _context = context;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var role = CurrentUserRole;
        var uid = CurrentUserId;

        var query = _context.Tickets
            .Include(x => x.CreatedByNavigation).ThenInclude(u => u.Role)
            .Include(x => x.AssignedToNavigation).ThenInclude(u => u.Role)
            .AsQueryable();

        if (role == "SUPPORT") query = query.Where(x => x.AssignedTo == uid);
        else if (role == "USER") query = query.Where(x => x.CreatedBy == uid);

        var tickets = await query.ToListAsync();
        return Ok(tickets.Select(MapToDTO));
    }

    [HttpPost]
    [Authorize(Roles = "USER,MANAGER")]
    public async Task<IActionResult> NewTicket([FromBody] CreateTicketDTO dto)
    {
        var ticket = new Ticket
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority.ToString(),
            Status = "OPEN",
            CreatedBy = CurrentUserId,
            CreatedAt = DateTime.Now
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        var res = await _context.Tickets
            .Include(x => x.CreatedByNavigation).ThenInclude(u => u.Role)
            .Include(x => x.AssignedToNavigation).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == ticket.Id);

        return Ok(MapToDTO(res!));
    }

    [HttpPatch("{id}/assign")]
    [Authorize(Roles = "MANAGER,SUPPORT")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignDTO dto)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }
        var user = await _context.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == dto.UserId);
        if (user == null || user.Role.Name == "USER") 
        {
            return BadRequest("Invalid assignment.");
        }
        ticket.AssignedTo = dto.UserId;
        await _context.SaveChangesAsync();

        var res = await _context.Tickets
            .Include(x => x.CreatedByNavigation).ThenInclude(u => u.Role)
            .Include(x => x.AssignedToNavigation).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        return Ok(MapToDTO(res!));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "MANAGER,SUPPORT")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateStatusDTO dto)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }
        var old = Enum.Parse<TicketStatus>(ticket.Status!);
        var next = dto.Status;

        bool ok = (old == TicketStatus.OPEN && next == TicketStatus.IN_PROGRESS) ||
                  (old == TicketStatus.IN_PROGRESS && next == TicketStatus.RESOLVED) ||
                  (old == TicketStatus.RESOLVED && next == TicketStatus.CLOSED);

        if (!ok)
        {
            return BadRequest("Bad transition.");
        }
        _context.TicketStatusLogs.Add(new TicketStatusLog
        {
            TicketId = id,
            OldStatus = old.ToString(),
            NewStatus = next.ToString(),
            ChangedBy = CurrentUserId,
            ChangedAt = DateTime.Now
        });

        ticket.Status = next.ToString();
        await _context.SaveChangesAsync();

        var res = await _context.Tickets
            .Include(x => x.CreatedByNavigation).ThenInclude(u => u.Role)
            .Include(x => x.AssignedToNavigation).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        return Ok(MapToDTO(res!));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }
        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private TicketResponseDTO MapToDTO(Ticket x) => new TicketResponseDTO
    {
        Id = x.Id,
        Title = x.Title!,
        Description = x.Description!,
        Status = x.Status!,
        Priority = x.Priority!,
        CreatedBy = x.CreatedBy,
        AssignedTo = x.AssignedTo,
        CreatedAt = x.CreatedAt,
        CreatedByNavigation = new UserResponseDTO 
        { 
            Id = x.CreatedByNavigation.Id, 
            Name = x.CreatedByNavigation.Name, 
            Email = x.CreatedByNavigation.Email, 
            Role = x.CreatedByNavigation.Role.Name 
        },
        AssignedToNavigation = x.AssignedToNavigation == null ? null : new UserResponseDTO
        {
            Id = x.AssignedToNavigation.Id,
            Name = x.AssignedToNavigation.Name,
            Email = x.AssignedToNavigation.Email,
            Role = x.AssignedToNavigation.Role.Name
        }
    };
}
