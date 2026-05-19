using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TBCarePlus.API.Models;

[Table("risk_levels", Schema = "tbcare_plus")]
public class RiskLevel
{
    [Key]
    public int Id { get; set; }

    public int TbTypeId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public double MinScore { get; set; }
    public double MaxScore { get; set; }

    public string? Description { get; set; }
    public string? Recommendation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TbTypeId))]
    public TbType TbType { get; set; } = null!;
}
