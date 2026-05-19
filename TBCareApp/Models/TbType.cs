using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("tb_types", Schema = "tbcare_plus")]
public class TbType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? BodyArea { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Symptom> Symptoms { get; set; } = new List<Symptom>();
    public ICollection<RiskRule> RiskRules { get; set; } = new List<RiskRule>();
    public ICollection<RiskLevel> RiskLevels { get; set; } = new List<RiskLevel>();
}
