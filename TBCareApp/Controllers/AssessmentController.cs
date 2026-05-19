using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/assessment")]
public class AssessmentController : ControllerBase
{
    private readonly IDiagnosisService _diagnosisService;

    public AssessmentController(IDiagnosisService diagnosisService) => _diagnosisService = diagnosisService;

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
