using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class DiagnosisService : IDiagnosisService
{
    private readonly AppDbContext _db;
    public DiagnosisService(AppDbContext db) => _db = db;

    public async Task<AssessmentResultResponse> SubmitAssessmentAsync(Guid userId, SubmitAssessmentRequest request)
    {
        var assessmentType = await _db.AssessmentTypes.FindAsync(request.AssessmentTypeId)
            ?? throw new KeyNotFoundException("Assessment type not found.");

        var rules = await _db.RiskRules
            .Include(r => r.Symptom)
            .Include(r => r.TbType)
            .Where(r => r.AssessmentTypeId == request.AssessmentTypeId && r.IsActive)
            .ToListAsync();

        if (rules.Count == 0)
            throw new KeyNotFoundException("No risk rules configured for this assessment type.");

        var rulesByTbType = rules.GroupBy(r => r.TbType);
        var answerLookup = request.Answers.ToDictionary(a => a.QuestionId, a => (double)a.CfValue);

        var questionIds = request.Answers.Select(a => a.QuestionId).ToList();
        var questionSymptomMap = await _db.AssessmentQuestions
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.SymptomId);

        var results = new List<TbTypeResult>();
        var session = new AssessmentSession
        {
            UserId = userId,
            AssessmentTypeId = request.AssessmentTypeId,
            CompletedAt = DateTime.UtcNow,
        };
        _db.AssessmentSessions.Add(session);
        await _db.SaveChangesAsync();

        foreach (var group in rulesByTbType)
        {
            var tbType = group.Key;
            var symptomDetails = new List<SymptomDetail>();
            double totalScore = 0;

            foreach (var rule in group)
            {
                var matchingQuestion = request.Answers
                    .FirstOrDefault(a =>
                    {
                        questionSymptomMap.TryGetValue(a.QuestionId, out var sid);
                        return sid == rule.SymptomId;
                    });

                if (matchingQuestion is null) continue;

                double cfValue = (double)matchingQuestion.CfValue;
                double weight = (double)rule.Weight;
                double score = weight * cfValue;

                symptomDetails.Add(new SymptomDetail
                {
                    SymptomName = rule.Symptom.Name,
                    CfValue = cfValue,
                    Weight = weight,
                });

                totalScore += score;
            }

            var riskLevel = await _db.RiskLevels
                .Where(rl => rl.TbTypeId == tbType.Id && totalScore >= rl.MinScore && totalScore <= rl.MaxScore)
                .OrderBy(rl => rl.MinScore)
                .FirstOrDefaultAsync();

            results.Add(new TbTypeResult
            {
                TbTypeName = tbType.Name,
                TbTypeCode = tbType.Code,
                TotalScore = totalScore,
                RiskLevel = riskLevel is null ? null : new RiskLevelResult
                {
                    Title = riskLevel.Title, Code = riskLevel.Code,
                    MinScore = riskLevel.MinScore, MaxScore = riskLevel.MaxScore,
                    Description = riskLevel.Description, Recommendation = riskLevel.Recommendation,
                },
                SymptomDetails = symptomDetails,
            });

            _db.AssessmentResults.Add(new AssessmentResult
            {
                SessionId = session.Id, TbTypeId = tbType.Id,
                RiskLevelId = riskLevel?.Id, TotalScore = (decimal)totalScore,
            });
        }

        foreach (var answer in request.Answers)
        {
            _db.AssessmentAnswers.Add(new AssessmentAnswer
            {
                SessionId = session.Id, QuestionId = answer.QuestionId, CfValue = answer.CfValue,
            });
        }

        await _db.SaveChangesAsync();

        return new AssessmentResultResponse
        {
            SessionId = session.Id, AssessmentTypeName = assessmentType.Name,
            CompletedAt = session.CompletedAt, Results = results,
        };
    }

    public async Task<List<HistorySessionDto>> GetUserHistoryAsync(Guid userId, int limit = 20)
    {
        return await _db.AssessmentSessions
            .Include(s => s.AssessmentType)
            .Include(s => s.Results).ThenInclude(r => r.TbType)
            .Include(s => s.Results).ThenInclude(r => r.RiskLevel)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .Select(s => new HistorySessionDto
            {
                SessionId = s.Id, AssessmentTypeName = s.AssessmentType.Name,
                CompletedAt = s.CompletedAt,
                Results = s.Results.Select(r => new HistoryResultDto
                {
                    TbTypeName = r.TbType.Name,
                    TotalScore = (double)r.TotalScore,
                    RiskLevelTitle = r.RiskLevel != null ? r.RiskLevel.Title : null,
                    RiskLevelCode = r.RiskLevel != null ? r.RiskLevel.Code : null,
                }).ToList(),
            })
            .ToListAsync();
    }

    public async Task<AssessmentResultResponse?> GetSessionAsync(long sessionId)
    {
        var session = await _db.AssessmentSessions
            .Include(s => s.AssessmentType)
            .Include(s => s.Results).ThenInclude(r => r.TbType)
            .Include(s => s.Results).ThenInclude(r => r.RiskLevel)
            .Include(s => s.Answers).ThenInclude(a => a.Question).ThenInclude(q => q.Symptom)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null) return null;

        var resultsByType = session.Results.GroupBy(r => r.TbType);

        return new AssessmentResultResponse
        {
            SessionId = session.Id,
            AssessmentTypeName = session.AssessmentType.Name,
            CompletedAt = session.CompletedAt,
            Results = resultsByType.Select(g =>
            {
                var result = g.First();
                return new TbTypeResult
                {
                    TbTypeName = result.TbType.Name,
                    TbTypeCode = result.TbType.Code,
                    TotalScore = (double)result.TotalScore,
                    RiskLevel = result.RiskLevel is null ? null : new RiskLevelResult
                    {
                        Title = result.RiskLevel.Title, Code = result.RiskLevel.Code,
                        MinScore = result.RiskLevel.MinScore, MaxScore = result.RiskLevel.MaxScore,
                        Description = result.RiskLevel.Description, Recommendation = result.RiskLevel.Recommendation,
                    },
                    SymptomDetails = session.Answers
                        .Where(a => a.Question.Symptom.TbTypeId == result.TbTypeId)
                        .Select(a => new SymptomDetail
                        {
                            SymptomName = a.Question.Symptom.Name,
                            CfValue = (double)a.CfValue,
                            Weight = 0,
                        }).ToList(),
                };
            }).ToList(),
        };
    }
}
