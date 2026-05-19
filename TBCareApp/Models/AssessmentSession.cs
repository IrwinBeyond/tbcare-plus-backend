using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("assessment_sessions", Schema = "tbcare_plus")]
public class AssessmentSession
{
    [Key]
    public long Id { get; set; }

    public Guid UserId { get; set; }
    public int AssessmentTypeId { get; set; }

    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(AssessmentTypeId))]
    public AssessmentType AssessmentType { get; set; } = null!;

    public ICollection<AssessmentAnswer> Answers { get; set; } = new List<AssessmentAnswer>();
    public ICollection<AssessmentResult> Results { get; set; } = new List<AssessmentResult>();
}
