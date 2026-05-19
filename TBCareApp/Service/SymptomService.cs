using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class SymptomService : ISymptomService
{
    private readonly AppDbContext _db;
    public SymptomService(AppDbContext db) => _db = db;

    public async Task<List<SymptomDto>> GetAllAsync(int? tbTypeId = null)
    {
        var query = _db.Symptoms.Include(s => s.TbType).AsQueryable();
        if (tbTypeId.HasValue)
            query = query.Where(s => s.TbTypeId == tbTypeId.Value);
        return await query.OrderBy(s => s.TbType.SortOrder).ThenBy(s => s.Name).Select(s => Map(s)).ToListAsync();
    }

    public async Task<SymptomDto?> GetByIdAsync(int id)
    {
        var symptom = await _db.Symptoms.Include(s => s.TbType).FirstOrDefaultAsync(s => s.Id == id);
        return symptom is null ? null : Map(symptom);
    }

    public async Task<SymptomDto> CreateAsync(CreateSymptomDto dto)
    {
        var entity = new Symptom
        {
            TbTypeId = dto.TbTypeId, Code = dto.Code, Name = dto.Name, Description = dto.Description,
        };
        _db.Symptoms.Add(entity);
        await _db.SaveChangesAsync();

        await _db.Entry(entity).Reference(s => s.TbType).LoadAsync();
        return Map(entity);
    }

    public async Task<SymptomDto?> UpdateAsync(int id, UpdateSymptomDto dto)
    {
        var entity = await _db.Symptoms.Include(s => s.TbType).FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null) return null;

        if (dto.TbTypeId.HasValue) entity.TbTypeId = dto.TbTypeId.Value;
        if (dto.Code is not null) entity.Code = dto.Code;
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Symptoms.FindAsync(id);
        if (entity is null) return false;
        _db.Symptoms.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static SymptomDto Map(Symptom s) => new()
    {
        Id = s.Id, TbTypeId = s.TbTypeId, TbTypeName = s.TbType?.Name,
        Code = s.Code, Name = s.Name, Description = s.Description,
        IsActive = s.IsActive, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt,
    };
}
