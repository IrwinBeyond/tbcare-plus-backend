using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("assessment_results", Schema = "tbcare_plus")]
public class AssessmentResult
{
    [Key]
    public long Id { get; set; }

    public long SessionId { get; set; }
    public int TbTypeId { get; set; }
    public int? RiskLevelId { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal TotalScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SessionId))]
    public AssessmentSession Session { get; set; } = null!;

    [ForeignKey(nameof(TbTypeId))]
    public TbType TbType { get; set; } = null!;

    [ForeignKey(nameof(RiskLevelId))]
    public RiskLevel? RiskLevel { get; set; }
}
