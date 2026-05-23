namespace TBCarePlus.API.DTOs;

// ── Request ──────────────────────────────────────────────────────────
public class SubmitAssessmentRequest
{
    public int AssessmentTypeId { get; set; }
    public List<AnswerItem> Answers { get; set; } = new();
}

public class AnswerItem
{
    public int QuestionId { get; set; }
    public decimal CfValue { get; set; }
}

// ── Response ─────────────────────────────────────────────────────────
public class AssessmentResultResponse
{
    public long SessionId { get; set; }
    public string AssessmentTypeName { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public List<TbTypeResult> Results { get; set; } = new();
}

public class TbTypeResult
{
    public string TbTypeName { get; set; } = string.Empty;
    public string? TbTypeCode { get; set; }
    public double CombinedCF { get; set; }
    public double TotalScore { get; set; }
    public RiskLevelResult? RiskLevel { get; set; }
    public List<SymptomDetail> SymptomDetails { get; set; } = new();
}

public class RiskLevelResult
{
    public string Title { get; set; } = string.Empty;
    public string? Code { get; set; }
    public double MinScore { get; set; }
    public double MaxScore { get; set; }
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
}

public class SymptomDetail
{
    public string SymptomName { get; set; } = string.Empty;
    public double CfValue { get; set; }
    public double Weight { get; set; }
}

// ── History ──────────────────────────────────────────────────────────
public class HistorySessionDto
{
    public long SessionId { get; set; }
    public string? AssessmentTypeName { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<HistoryResultDto> Results { get; set; } = new();
}

public class HistoryResultDto
{
    public string TbTypeName { get; set; } = string.Empty;
    public double TotalScore { get; set; }
    public string? RiskLevelTitle { get; set; }
    public string? RiskLevelCode { get; set; }
}

// ── Quick Check Config ────────────────────────────────────────────────
public class QuickCheckConfigDto
{
    public List<QuickCheckQuestionDto> Questions { get; set; } = new();
    public List<RiskLevelDto> RiskLevels { get; set; } = new();
    public string ScoringMethod { get; set; } = "soft_saturation_cf";
    public double SaturationK { get; set; } = 0.35;
}

public class QuickCheckQuestionDto
{
    public int QuestionId { get; set; }
    public int SymptomId { get; set; }
    public string SymptomCode { get; set; } = string.Empty;
    public string SymptomName { get; set; } = string.Empty;
    public string? SymptomDescription { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public decimal Weight { get; set; }
    public int TbTypeId { get; set; }
    public string? TbTypeName { get; set; }
}
