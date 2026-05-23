using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/assessment")]
[Authorize]
public class AssessmentController : ControllerBase
{
    private readonly IDiagnosisService _diagnosisService;
    private readonly AppDbContext _db;

    public AssessmentController(IDiagnosisService diagnosisService, AppDbContext db)
    {
        _diagnosisService = diagnosisService;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("quick-check-config")]
    public async Task<IActionResult> GetQuickCheckConfig()
    {
        const int quickCheckTypeId = 1;

        var assessmentType = await _db.AssessmentTypes.FindAsync(quickCheckTypeId);

        var questions = await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .Where(q => q.AssessmentTypeId == quickCheckTypeId)
            .OrderBy(q => q.SortOrder)
            .Select(q => new QuickCheckQuestionDto
            {
                QuestionId = q.Id,
                SymptomId = q.SymptomId,
                SymptomCode = q.Symptom.Code,
                SymptomName = q.Symptom.Name,
                SymptomDescription = q.Symptom.Description,
                QuestionText = q.QuestionText,
                SortOrder = q.SortOrder,
                IsRequired = q.IsRequired,
                Weight = _db.RiskRules
                    .Where(r => r.AssessmentTypeId == quickCheckTypeId
                             && r.SymptomId == q.SymptomId
                             && r.IsActive)
                    .Select(r => r.Weight)
                    .FirstOrDefault(),
                TbTypeId = q.Symptom.TbTypeId,
                TbTypeName = q.Symptom.TbType.Name,
            })
            .ToListAsync();

        var distinctTbTypeIds = questions.Select(q => q.TbTypeId).Distinct().ToList();

        var riskLevels = await _db.RiskLevels
            .Where(rl => distinctTbTypeIds.Contains(rl.TbTypeId))
            .Select(rl => new RiskLevelDto
            {
                Id = rl.Id,
                TbTypeId = rl.TbTypeId,
                Code = rl.Code,
                Title = rl.Title,
                MinScore = rl.MinScore,
                MaxScore = rl.MaxScore,
                Description = rl.Description,
                Recommendation = rl.Recommendation,
            })
            .ToListAsync();

        return Ok(ApiResponse<QuickCheckConfigDto>.Ok(new QuickCheckConfigDto
        {
            Questions = questions,
            RiskLevels = riskLevels,
            ScoringMethod = assessmentType?.ScoringMethod ?? "soft_saturation_cf",
            SaturationK = assessmentType?.SaturationK ?? 0.35,
        }));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitAssessmentRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        try
        {
            var result = await _diagnosisService.SubmitAssessmentAsync(userId.Value, request);
            return Ok(ApiResponse<AssessmentResultResponse>.Ok(result, "Assessment completed."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var history = await _diagnosisService.GetUserHistoryAsync(userId.Value, limit);
        return Ok(ApiResponse<List<HistorySessionDto>>.Ok(history));
    }

    [HttpGet("session/{sessionId:long}")]
    public async Task<IActionResult> GetSession(long sessionId)
    {
        var session = await _diagnosisService.GetSessionAsync(sessionId);
        if (session is null)
            return NotFound(ApiResponse<object>.Fail("Assessment session not found."));

        return Ok(ApiResponse<AssessmentResultResponse>.Ok(session));
    }

    private Guid? GetUserId()
    {
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
            return null;
        return userId;
    }
}
