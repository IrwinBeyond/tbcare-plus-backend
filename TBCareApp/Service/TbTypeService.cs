using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class TbTypeService : ITbTypeService
{
    private readonly AppDbContext _db;
    public TbTypeService(AppDbContext db) => _db = db;

    public async Task<List<TbTypeDto>> GetAllAsync(bool? activeOnly = null)
    {
        var query = _db.TbTypes.AsQueryable();
        if (activeOnly.HasValue)
            query = query.Where(t => t.IsActive == activeOnly.Value);
        return await query.OrderBy(t => t.SortOrder).Select(t => Map(t)).ToListAsync();
    }

    public async Task<TbTypeDto?> GetByIdAsync(int id)
    {
        var tbType = await _db.TbTypes.FindAsync(id);
        return tbType is null ? null : Map(tbType);
    }

    public async Task<TbTypeDto> CreateAsync(CreateTbTypeDto dto)
    {
        var entity = new TbType
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            BodyArea = dto.BodyArea,
            SortOrder = dto.SortOrder,
        };
        _db.TbTypes.Add(entity);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<TbTypeDto?> UpdateAsync(int id, UpdateTbTypeDto dto)
    {
        var entity = await _db.TbTypes.FindAsync(id);
        if (entity is null) return null;

        if (dto.Code is not null) entity.Code = dto.Code;
        if (dto.Name is not null) entity.Name = dto.Name;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.BodyArea is not null) entity.BodyArea = dto.BodyArea;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.SortOrder.HasValue) entity.SortOrder = dto.SortOrder.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.TbTypes.FindAsync(id);
        if (entity is null) return false;
        _db.TbTypes.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static TbTypeDto Map(TbType t) => new()
    {
        Id = t.Id, Code = t.Code, Name = t.Name, Description = t.Description,
        BodyArea = t.BodyArea, IsActive = t.IsActive, SortOrder = t.SortOrder,
        CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt,
    };
}
