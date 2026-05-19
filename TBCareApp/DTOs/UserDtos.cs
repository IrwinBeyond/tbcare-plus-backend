namespace TBCarePlus.API.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateUserDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
}
