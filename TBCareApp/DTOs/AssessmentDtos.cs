namespace TBCarePlus.API.DTOs;

// ── Assessment Type ──────────────────────────────────────────────────
public class AssessmentTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<AssessmentQuestionDto> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AssessmentTypeSimpleDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateAssessmentTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateAssessmentTypeDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

// ── Assessment Question ──────────────────────────────────────────────
public class AssessmentQuestionDto
{
    public int Id { get; set; }
    public int AssessmentTypeId { get; set; }
    public int SymptomId { get; set; }
    public string? SymptomName { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class CreateAssessmentQuestionDto
{
    public int AssessmentTypeId { get; set; }
    public int SymptomId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class UpdateAssessmentQuestionDto
{
    public int? SymptomId { get; set; }
    public string? QuestionText { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsRequired { get; set; }
}

// ── Risk Rule ────────────────────────────────────────────────────────
public class RiskRuleDto
{
    public int Id { get; set; }
    public int AssessmentTypeId { get; set; }
    public string? AssessmentTypeName { get; set; }
    public int SymptomId { get; set; }
    public string? SymptomName { get; set; }
    public int TbTypeId { get; set; }
    public string? TbTypeName { get; set; }
    public decimal Weight { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRiskRuleDto
{
    public int AssessmentTypeId { get; set; }
    public int SymptomId { get; set; }
    public int TbTypeId { get; set; }
    public decimal Weight { get; set; }
}

public class UpdateRiskRuleDto
{
    public int? AssessmentTypeId { get; set; }
    public int? SymptomId { get; set; }
    public int? TbTypeId { get; set; }
    public decimal? Weight { get; set; }
    public bool? IsActive { get; set; }
}

// ── Risk Level ───────────────────────────────────────────────────────
public class RiskLevelDto
{
    public int Id { get; set; }
    public int TbTypeId { get; set; }
    public string? TbTypeName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double MinScore { get; set; }
    public double MaxScore { get; set; }
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
}

public class CreateRiskLevelDto
{
    public int TbTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double MinScore { get; set; }
    public double MaxScore { get; set; }
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
}

public class UpdateRiskLevelDto
{
    public int? TbTypeId { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public double? MinScore { get; set; }
    public double? MaxScore { get; set; }
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
}
