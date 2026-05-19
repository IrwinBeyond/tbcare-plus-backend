using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Interfaces;

public interface IRiskLevelService
{
    Task<List<RiskLevelDto>> GetAllAsync(int? tbTypeId = null);
    Task<RiskLevelDto?> GetByIdAsync(int id);
    Task<RiskLevelDto> CreateAsync(CreateRiskLevelDto dto);
    Task<RiskLevelDto?> UpdateAsync(int id, UpdateRiskLevelDto dto);
    Task<bool> DeleteAsync(int id);
}
