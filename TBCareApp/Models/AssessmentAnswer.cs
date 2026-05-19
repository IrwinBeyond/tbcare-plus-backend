using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("assessment_answers", Schema = "tbcare_plus")]
public class AssessmentAnswer
{
    [Key]
    public long Id { get; set; }

    public long SessionId { get; set; }
    public int QuestionId { get; set; }

    [Column(TypeName = "numeric(3,1)")]
    public decimal CfValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public AssessmentSession Session { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public AssessmentQuestion Question { get; set; } = null!;
}
