using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/assessment-types")]
public class AssessmentTypesController : ControllerBase
{
    private readonly IAssessmentService _service;
    public AssessmentTypesController(IAssessmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetAllTypesAsync();
        return Ok(ApiResponse<List<AssessmentTypeDto>>.Ok(types));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var type = await _service.GetTypeByIdAsync(id);
        if (type is null) return NotFound(ApiResponse<object>.Fail("Assessment type not found."));
        return Ok(ApiResponse<AssessmentTypeDto>.Ok(type));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssessmentTypeDto dto)
    {
        var type = await _service.CreateTypeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, ApiResponse<AssessmentTypeSimpleDto>.Ok(type, "Assessment type created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssessmentTypeDto dto)
    {
        var type = await _service.UpdateTypeAsync(id, dto);
        if (type is null) return NotFound(ApiResponse<object>.Fail("Assessment type not found."));
        return Ok(ApiResponse<AssessmentTypeSimpleDto>.Ok(type, "Assessment type updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteTypeAsync(id);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Assessment type not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Assessment type deleted."));
    }
}
