using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("risk_rules", Schema = "tbcare_plus")]
public class RiskRule
{
    [Key]
    public int Id { get; set; }

    public int AssessmentTypeId { get; set; }
    public int SymptomId { get; set; }
    public int TbTypeId { get; set; }

    [Column(TypeName = "numeric(3,1)")]
    public decimal Weight { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AssessmentTypeId))]
    public AssessmentType AssessmentType { get; set; } = null!;

    [ForeignKey(nameof(SymptomId))]
    public Symptom Symptom { get; set; } = null!;

    [ForeignKey(nameof(TbTypeId))]
    public TbType TbType { get; set; } = null!;
}
