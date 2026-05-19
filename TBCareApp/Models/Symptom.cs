using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("symptoms", Schema = "tbcare_plus")]
public class Symptom
{
    [Key]
    public int Id { get; set; }

    public int TbTypeId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TbTypeId))]
    public TbType TbType { get; set; } = null!;

    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();
    public ICollection<RiskRule> RiskRules { get; set; } = new List<RiskRule>();
}
