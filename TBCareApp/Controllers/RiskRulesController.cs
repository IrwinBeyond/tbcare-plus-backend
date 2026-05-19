using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/risk-rules")]
public class RiskRulesController : ControllerBase
{
    /// <summary>
    /// Risk rules are managed through the assessment configuration endpoints.
    /// This controller is reserved for future use.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(ApiResponse<object>.Ok(new { message = "Use assessment-types endpoints to manage risk rules." }));
    }
}
