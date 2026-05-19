using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class RiskLevelService : IRiskLevelService
{
    private readonly AppDbContext _db;
    public RiskLevelService(AppDbContext db) => _db = db;

    public async Task<List<RiskLevelDto>> GetAllAsync(int? tbTypeId = null)
    {
        var query = _db.RiskLevels.Include(r => r.TbType).AsQueryable();
        if (tbTypeId.HasValue)
            query = query.Where(r => r.TbTypeId == tbTypeId.Value);
        return await query.OrderBy(r => r.TbType.SortOrder).ThenBy(r => r.MinScore).Select(r => Map(r)).ToListAsync();
    }

    public async Task<RiskLevelDto?> GetByIdAsync(int id)
    {
        var entity = await _db.RiskLevels.Include(r => r.TbType).FirstOrDefaultAsync(r => r.Id == id);
        return entity is null ? null : Map(entity);
    }

    public async Task<RiskLevelDto> CreateAsync(CreateRiskLevelDto dto)
    {
        var entity = new RiskLevel
        {
            TbTypeId = dto.TbTypeId, Code = dto.Code, Title = dto.Title,
            MinScore = dto.MinScore, MaxScore = dto.MaxScore,
            Description = dto.Description, Recommendation = dto.Recommendation,
        };
        _db.RiskLevels.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(r => r.TbType).LoadAsync();
        return Map(entity);
    }

    public async Task<RiskLevelDto?> UpdateAsync(int id, UpdateRiskLevelDto dto)
    {
        var entity = await _db.RiskLevels.Include(r => r.TbType).FirstOrDefaultAsync(r => r.Id == id);
        if (entity is null) return null;

        if (dto.TbTypeId.HasValue) entity.TbTypeId = dto.TbTypeId.Value;
        if (dto.Code is not null) entity.Code = dto.Code;
        if (dto.Title is not null) entity.Title = dto.Title;
        if (dto.MinScore.HasValue) entity.MinScore = dto.MinScore.Value;
        if (dto.MaxScore.HasValue) entity.MaxScore = dto.MaxScore.Value;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.Recommendation is not null) entity.Recommendation = dto.Recommendation;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.RiskLevels.FindAsync(id);
        if (entity is null) return false;
        _db.RiskLevels.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static RiskLevelDto Map(RiskLevel r) => new()
    {
        Id = r.Id, TbTypeId = r.TbTypeId, TbTypeName = r.TbType?.Name,
        Code = r.Code, Title = r.Title, MinScore = r.MinScore, MaxScore = r.MaxScore,
        Description = r.Description, Recommendation = r.Recommendation,
    };
}
