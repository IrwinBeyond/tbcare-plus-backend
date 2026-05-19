using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/assessment-questions")]
public class AssessmentQuestionsController : ControllerBase
{
    private readonly IAssessmentService _service;
    public AssessmentQuestionsController(IAssessmentService service) => _service = service;

    [HttpGet("by-type/{assessmentTypeId:int}")]
    public async Task<IActionResult> GetByType(int assessmentTypeId)
    {
        var questions = await _service.GetQuestionsByTypeAsync(assessmentTypeId);
        return Ok(ApiResponse<List<AssessmentQuestionDto>>.Ok(questions));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssessmentQuestionDto dto)
    {
        var question = await _service.CreateQuestionAsync(dto);
        return CreatedAtAction(nameof(GetByType), new { assessmentTypeId = question.AssessmentTypeId }, ApiResponse<AssessmentQuestionDto>.Ok(question, "Question created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssessmentQuestionDto dto)
    {
        var question = await _service.UpdateQuestionAsync(id, dto);
        if (question is null) return NotFound(ApiResponse<object>.Fail("Question not found."));
        return Ok(ApiResponse<AssessmentQuestionDto>.Ok(question, "Question updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteQuestionAsync(id);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Question not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Question deleted."));
    }
}
