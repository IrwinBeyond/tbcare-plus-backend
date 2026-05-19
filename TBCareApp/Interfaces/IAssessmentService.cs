using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Interfaces;

public interface IAssessmentService
{
    Task<List<AssessmentTypeDto>> GetAllTypesAsync();
    Task<AssessmentTypeDto?> GetTypeByIdAsync(int id);
    Task<AssessmentTypeSimpleDto> CreateTypeAsync(CreateAssessmentTypeDto dto);
    Task<AssessmentTypeSimpleDto?> UpdateTypeAsync(int id, UpdateAssessmentTypeDto dto);
    Task<bool> DeleteTypeAsync(int id);
    Task<List<AssessmentQuestionDto>> GetQuestionsByTypeAsync(int assessmentTypeId);
    Task<AssessmentQuestionDto> CreateQuestionAsync(CreateAssessmentQuestionDto dto);
    Task<AssessmentQuestionDto?> UpdateQuestionAsync(int id, UpdateAssessmentQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int id);
}
