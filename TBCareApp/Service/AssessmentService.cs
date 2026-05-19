using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class AssessmentService : IAssessmentService
{
    private readonly AppDbContext _db;
    public AssessmentService(AppDbContext db) => _db = db;

    public async Task<List<AssessmentTypeDto>> GetAllTypesAsync()
    {
        return await _db.AssessmentTypes
            .Include(at => at.AssessmentQuestions.OrderBy(q => q.SortOrder))
                .ThenInclude(q => q.Symptom)
            .OrderBy(at => at.Id)
            .Select(at => new AssessmentTypeDto
            {
                Id = at.Id, Code = at.Code, Name = at.Name, Description = at.Description,
                CreatedAt = at.CreatedAt, UpdatedAt = at.UpdatedAt,
                Questions = at.AssessmentQuestions.Select(q => new AssessmentQuestionDto
                {
                    Id = q.Id, AssessmentTypeId = q.AssessmentTypeId, SymptomId = q.SymptomId,
                    SymptomName = q.Symptom.Name, QuestionText = q.QuestionText,
                    SortOrder = q.SortOrder, IsRequired = q.IsRequired,
                }).ToList(),
            })
            .ToListAsync();
    }

    public async Task<AssessmentTypeDto?> GetTypeByIdAsync(int id)
    {
        return await _db.AssessmentTypes
            .Include(at => at.AssessmentQuestions.OrderBy(q => q.SortOrder))
                .ThenInclude(q => q.Symptom)
            .Where(at => at.Id == id)
            .Select(at => new AssessmentTypeDto
            {
                Id = at.Id, Code = at.Code, Name = at.Name, Description = at.Description,
                CreatedAt = at.CreatedAt, UpdatedAt = at.UpdatedAt,
                Questions = at.AssessmentQuestions.Select(q => new AssessmentQuestionDto
                {
                    Id = q.Id, AssessmentTypeId = q.AssessmentTypeId, SymptomId = q.SymptomId,
                    SymptomName = q.Symptom.Name, QuestionText = q.QuestionText,
                    SortOrder = q.SortOrder, IsRequired = q.IsRequired,
                }).ToList(),
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AssessmentTypeSimpleDto> CreateTypeAsync(CreateAssessmentTypeDto dto)
    {
        var entity = new AssessmentType { Code = dto.Code, Name = dto.Name, Description = dto.Description };
        _db.AssessmentTypes.Add(entity);
        await _db.SaveChangesAsync();
        return new AssessmentTypeSimpleDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description };
    }

    public async Task<AssessmentTypeSimpleDto?> UpdateTypeAsync(int id, UpdateAssessmentTypeDto dto)
    {
        var entity = await _db.AssessmentTypes.FindAsync(id);
        if (entity is null) return null;
        if (dto.Code is not null) entity.Code = dto.Code;
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new AssessmentTypeSimpleDto { Id = entity.Id, Code = entity.Code, Name = entity.Name, Description = entity.Description };
    }

    public async Task<bool> DeleteTypeAsync(int id)
    {
        var entity = await _db.AssessmentTypes.FindAsync(id);
        if (entity is null) return false;
        _db.AssessmentTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<AssessmentQuestionDto>> GetQuestionsByTypeAsync(int assessmentTypeId)
    {
        return await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .Where(q => q.AssessmentTypeId == assessmentTypeId)
            .OrderBy(q => q.SortOrder)
            .Select(q => new AssessmentQuestionDto
            {
                Id = q.Id, AssessmentTypeId = q.AssessmentTypeId, SymptomId = q.SymptomId,
                SymptomName = q.Symptom.Name, QuestionText = q.QuestionText,
                SortOrder = q.SortOrder, IsRequired = q.IsRequired,
            })
            .ToListAsync();
    }

    public async Task<AssessmentQuestionDto> CreateQuestionAsync(CreateAssessmentQuestionDto dto)
    {
        var entity = new AssessmentQuestion
        {
            AssessmentTypeId = dto.AssessmentTypeId, SymptomId = dto.SymptomId,
            QuestionText = dto.QuestionText, SortOrder = dto.SortOrder, IsRequired = dto.IsRequired,
        };
        _db.AssessmentQuestions.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(q => q.Symptom).LoadAsync();
        return new AssessmentQuestionDto
        {
            Id = entity.Id, AssessmentTypeId = entity.AssessmentTypeId, SymptomId = entity.SymptomId,
            SymptomName = entity.Symptom.Name, QuestionText = entity.QuestionText,
            SortOrder = entity.SortOrder, IsRequired = entity.IsRequired,
        };
    }

    public async Task<AssessmentQuestionDto?> UpdateQuestionAsync(int id, UpdateAssessmentQuestionDto dto)
    {
        var entity = await _db.AssessmentQuestions.Include(q => q.Symptom).FirstOrDefaultAsync(q => q.Id == id);
        if (entity is null) return null;
        if (dto.SymptomId.HasValue) entity.SymptomId = dto.SymptomId.Value;
        if (dto.QuestionText is not null) entity.QuestionText = dto.QuestionText;
        if (dto.SortOrder.HasValue) entity.SortOrder = dto.SortOrder.Value;
        if (dto.IsRequired.HasValue) entity.IsRequired = dto.IsRequired.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new AssessmentQuestionDto
        {
            Id = entity.Id, AssessmentTypeId = entity.AssessmentTypeId, SymptomId = entity.SymptomId,
            SymptomName = entity.Symptom.Name, QuestionText = entity.QuestionText,
            SortOrder = entity.SortOrder, IsRequired = entity.IsRequired,
        };
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        var entity = await _db.AssessmentQuestions.FindAsync(id);
        if (entity is null) return false;
        _db.AssessmentQuestions.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
