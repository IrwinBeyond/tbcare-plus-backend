namespace TBCarePlus.API.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? ProfilePicture { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateProfileRequest
{
    public string? Nickname { get; set; }
    public string? ProfilePicture { get; set; }
}
