using TBCarePlus.API.DTOs;

namespace TBCarePlus.API.Interfaces;

public interface IDiagnosisService
{
    Task<AssessmentResultResponse> SubmitAssessmentAsync(Guid userId, SubmitAssessmentRequest request);
    Task<List<HistorySessionDto>> GetUserHistoryAsync(Guid userId, int limit = 20);
    Task<AssessmentResultResponse?> GetSessionAsync(long sessionId);
}
