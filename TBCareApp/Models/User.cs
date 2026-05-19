using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("users", Schema = "tbcare_plus")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(255)]
    public string? FullName { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    public int? Age { get; set; }

    [MaxLength(50)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; } = "user";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AssessmentSession> Sessions { get; set; } = new List<AssessmentSession>();
}
