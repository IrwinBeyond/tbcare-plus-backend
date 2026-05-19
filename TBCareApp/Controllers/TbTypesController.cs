using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/tb-types")]
public class TbTypesController : ControllerBase
{
    private readonly ITbTypeService _service;
    public TbTypesController(ITbTypeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = null)
    {
        var types = await _service.GetAllAsync(activeOnly);
        return Ok(ApiResponse<List<TbTypeDto>>.Ok(types));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tbType = await _service.GetByIdAsync(id);
        if (tbType is null) return NotFound(ApiResponse<object>.Fail("TB type not found."));
        return Ok(ApiResponse<TbTypeDto>.Ok(tbType));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTbTypeDto dto)
    {
        var tbType = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = tbType.Id }, ApiResponse<TbTypeDto>.Ok(tbType, "TB type created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTbTypeDto dto)
    {
        var tbType = await _service.UpdateAsync(id, dto);
        if (tbType is null) return NotFound(ApiResponse<object>.Fail("TB type not found."));
        return Ok(ApiResponse<TbTypeDto>.Ok(tbType, "TB type updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("TB type not found."));
        return Ok(ApiResponse<object>.Ok(null!, "TB type deleted."));
    }
}
