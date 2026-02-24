using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackendExam.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketPriority { LOW, MEDIUM, HIGH }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatus { OPEN, IN_PROGRESS, RESOLVED, CLOSED }

public class LoginDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class CreateUserDTO
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    public int RoleId { get; set; }
}

public class CreateTicketDTO
{
    [Required]
    [MinLength(5)]
    public string Title { get; set; } = null!;

    [Required]
    [MinLength(10)]
    public string Description { get; set; } = null!;

    public TicketPriority Priority { get; set; } = TicketPriority.MEDIUM;
}

public class AssignDTO
{
    [Required]
    public int UserId { get; set; }
}

public class UpdateStatusDTO
{
    [Required]
    public TicketStatus Status { get; set; }
}

public class CommentDTO
{
    [Required]
    public string Comment { get; set; } = null!;
}

public class AuthResponse
{
    public string Token { get; set; } = null!;
}

public class UserResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
}

public class TicketResponseDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public int CreatedBy { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public UserResponseDTO CreatedByNavigation { get; set; } = null!;
    public UserResponseDTO? AssignedToNavigation { get; set; }
}
