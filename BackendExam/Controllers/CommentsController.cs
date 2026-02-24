using BackendExam.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackendExam.Controllers;

[Route("comments")]
[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly TicketManagementContext _context;

    public CommentsController(TicketManagementContext context)
    {
        _context = context;
    }

    private int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role)!;

    private async Task<bool> HasAccess(int tid)
    {
        if (Role == "MANAGER") { 
            return true; 
        }
        var t = await _context.Tickets.FindAsync(tid);
        return t != null && (t.CreatedBy == Uid || t.AssignedTo == Uid);
    }

    [HttpPost("/tickets/{id}/comments")]
    public async Task<IActionResult> Add(int id, [FromBody] CommentDTO dto)
    {
        if (!await HasAccess(id))  {
            return Forbid();
        }
        var c = new TicketComment { TicketId = id, UserId = Uid, Comment = dto.Comment, CreatedAt = DateTime.Now };
        _context.TicketComments.Add(c);
        await _context.SaveChangesAsync();
        
        var res = await _context.TicketComments
            .Include(x => x.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == c.Id);

        return Ok(res);
    }

    [HttpGet("/tickets/{id}/comments")]
    public async Task<IActionResult> GetByTicket(int id)
    {
        if (!await HasAccess(id)) return Forbid();
        var list = await _context.TicketComments
            .Include(x => x.User).ThenInclude(u => u.Role)
            .Where(x => x.TicketId == id)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CommentDTO dto)
    {
        var c = await _context.TicketComments.FindAsync(id);
        if (c == null) 
        {
            return NotFound();
        }
        if (Role != "MANAGER" && c.UserId != Uid) 
        {
            return Forbid();
        }

        c.Comment = dto.Comment;
        await _context.SaveChangesAsync();

        var res = await _context.TicketComments
            .Include(x => x.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        return Ok(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        var c = await _context.TicketComments.FindAsync(id);
        if (c == null)
        {
            return NotFound();
        }
        if (Role != "MANAGER" && c.UserId != Uid)
        {
            return Forbid();
        }
        _context.TicketComments.Remove(c);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
