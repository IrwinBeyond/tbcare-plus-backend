using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Interfaces;

public interface ISymptomService
{
    Task<List<SymptomDto>> GetAllAsync(int? tbTypeId = null);
    Task<SymptomDto?> GetByIdAsync(int id);
    Task<SymptomDto> CreateAsync(CreateSymptomDto dto);
    Task<SymptomDto?> UpdateAsync(int id, UpdateSymptomDto dto);
    Task<bool> DeleteAsync(int id);
}
