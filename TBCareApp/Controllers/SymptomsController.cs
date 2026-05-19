using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/symptoms")]
public class SymptomsController : ControllerBase
{
    private readonly ISymptomService _service;
    public SymptomsController(ISymptomService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? tbTypeId = null)
    {
        var symptoms = await _service.GetAllAsync(tbTypeId);
        return Ok(ApiResponse<List<SymptomDto>>.Ok(symptoms));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var symptom = await _service.GetByIdAsync(id);
        if (symptom is null) return NotFound(ApiResponse<object>.Fail("Symptom not found."));
        return Ok(ApiResponse<SymptomDto>.Ok(symptom));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSymptomDto dto)
    {
        var symptom = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = symptom.Id }, ApiResponse<SymptomDto>.Ok(symptom, "Symptom created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSymptomDto dto)
    {
        var symptom = await _service.UpdateAsync(id, dto);
        if (symptom is null) return NotFound(ApiResponse<object>.Fail("Symptom not found."));
        return Ok(ApiResponse<SymptomDto>.Ok(symptom, "Symptom updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Symptom not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Symptom deleted."));
    }
}
