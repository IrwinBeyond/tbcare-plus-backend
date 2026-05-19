using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Interfaces;

public interface ITbTypeService
{
    Task<List<TbTypeDto>> GetAllAsync(bool? activeOnly = null);
    Task<TbTypeDto?> GetByIdAsync(int id);
    Task<TbTypeDto> CreateAsync(CreateTbTypeDto dto);
    Task<TbTypeDto?> UpdateAsync(int id, UpdateTbTypeDto dto);
    Task<bool> DeleteAsync(int id);
}
