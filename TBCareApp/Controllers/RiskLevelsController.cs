using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/risk-levels")]
public class RiskLevelsController : ControllerBase
{
    private readonly IRiskLevelService _service;
    public RiskLevelsController(IRiskLevelService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? tbTypeId = null)
    {
        var levels = await _service.GetAllAsync(tbTypeId);
        return Ok(ApiResponse<List<RiskLevelDto>>.Ok(levels));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var level = await _service.GetByIdAsync(id);
        if (level is null) return NotFound(ApiResponse<object>.Fail("Risk level not found."));
        return Ok(ApiResponse<RiskLevelDto>.Ok(level));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRiskLevelDto dto)
    {
        var level = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = level.Id }, ApiResponse<RiskLevelDto>.Ok(level, "Risk level created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRiskLevelDto dto)
    {
        var level = await _service.UpdateAsync(id, dto);
        if (level is null) return NotFound(ApiResponse<object>.Fail("Risk level not found."));
        return Ok(ApiResponse<RiskLevelDto>.Ok(level, "Risk level updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Risk level not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Risk level deleted."));
    }
}
