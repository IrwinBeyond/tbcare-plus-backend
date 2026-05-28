using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/assessment")]
[Authorize]
public class AssessmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAssessmentHistoryWriter _historyWriter;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(AppDbContext db, IAssessmentHistoryWriter historyWriter, ILogger<AssessmentController> logger)
    {
        _db = db;
        _historyWriter = historyWriter;
        _logger = logger;
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = default;
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");
        return subClaim is not null && Guid.TryParse(subClaim.Value, out userId);
    }

    private static string EncodeSessionKey(int assessmentTypeId, DateTime createdAtUtc)
    {
        var raw = $"{assessmentTypeId}|{createdAtUtc.ToString("O")}";
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodeSessionKey(string sessionKey, out int assessmentTypeId, out DateTime createdAtUtc)
    {
        assessmentTypeId = 0;
        createdAtUtc = default;

        try
        {
            var raw = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(sessionKey));
            var parts = raw.Split('|', 2);
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out assessmentTypeId)) return false;
            if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out createdAtUtc)) return false;
            createdAtUtc = createdAtUtc.ToUniversalTime();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int RiskRank(string? code)
    {
        var c = (code ?? "").ToUpperInvariant();
        if (c.Contains("HIGH")) return 3;
        if (c.Contains("MEDIUM") || c.Contains("MODERATE")) return 2;
        return 1;
    }

    // ── Symptom variant helpers ───────────────────────────────────────
    // Variant codes end with a letter (e.g. G01a, G03b). Stripping trailing
    // letters yields the base code (e.g. G01, G03). Base codes belong to
    // tb_type=1; variants map to other TB types with their own weights.

    private static string GetBaseCode(string code)
    {
        while (code.Length > 0 && char.IsLetter(code[^1]))
            code = code[..^1];
        return code;
    }

    private static bool IsVariantCode(string code)
        => code.Length > 0 && char.IsLetter(code[^1]);

    [AllowAnonymous]
    [HttpGet("quick-check-config")]
    public async Task<IActionResult> GetQuickCheckConfig()
    {
        const int quickCheckTypeId = 1;

        var assessmentType = await _db.AssessmentTypes.FindAsync(quickCheckTypeId);

        var questions = await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .ThenInclude(s => s.TbType)
            .Where(q => q.AssessmentTypeId == quickCheckTypeId)
            .OrderBy(q => q.SortOrder)
            .Select(q => new QuickCheckQuestionDto
            {
                QuestionId = q.Id,
                SymptomId = q.SymptomId,
                SymptomCode = q.Symptom.Code,
                SymptomName = q.Symptom.Name,
                SymptomDescription = q.Symptom.Description,
                QuestionText = q.QuestionText,
                SortOrder = q.SortOrder,
                IsRequired = q.IsRequired,
                Weight = _db.RiskRules
                    .Where(r => r.AssessmentTypeId == quickCheckTypeId
                             && r.SymptomId == q.SymptomId
                             && r.IsActive)
                    .Select(r => r.Weight)
                    .FirstOrDefault(),
                TbTypeId = q.Symptom.TbTypeId,
                TbTypeName = q.Symptom.TbType.Name,
                ApplicableTbTypes = new List<TbTypeWeightDto>
                {
                    new TbTypeWeightDto
                    {
                        TbTypeId = q.Symptom.TbTypeId,
                        TbTypeName = q.Symptom.TbType.Name,
                        Weight = _db.RiskRules
                            .Where(r => r.AssessmentTypeId == quickCheckTypeId
                                     && r.SymptomId == q.SymptomId
                                     && r.IsActive)
                            .Select(r => r.Weight)
                            .FirstOrDefault(),
                    },
                },
            })
            .ToListAsync();

        var distinctTbTypeIds = questions.Select(q => q.TbTypeId).Distinct().ToList();

        var riskLevels = await _db.RiskLevels
            .Where(rl => distinctTbTypeIds.Contains(rl.TbTypeId))
            .Select(rl => new RiskLevelDto
            {
                Id = rl.Id,
                TbTypeId = rl.TbTypeId,
                Code = rl.Code,
                Title = rl.Title,
                MinScore = rl.MinScore,
                MaxScore = rl.MaxScore,
                Description = rl.Description,
                Recommendation = rl.Recommendation,
            })
            .ToListAsync();

        return Ok(ApiResponse<QuickCheckConfigDto>.Ok(new QuickCheckConfigDto
        {
            Questions = questions,
            RiskLevels = riskLevels,
            ScoringMethod = assessmentType?.ScoringMethod ?? "soft_saturation_cf",
            SaturationK = assessmentType?.SaturationK ?? 0.35,
        }));
    }

    [AllowAnonymous]
    [HttpGet("full-assessment-config")]
    public async Task<IActionResult> GetFullAssessmentConfig()
    {
        const int fullAssessmentTypeId = 2;

        var assessmentType = await _db.AssessmentTypes.FindAsync(fullAssessmentTypeId);

        // Fetch all questions including variant symptoms
        var allQuestions = await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .ThenInclude(s => s.TbType)
            .Where(q => q.AssessmentTypeId == fullAssessmentTypeId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        // Build map: baseCode → List<(symptomId, tbTypeId, tbTypeName)>
        var variantMap = new Dictionary<string, List<(int SymptomId, int TbTypeId, string TbTypeName)>>();
        foreach (var q in allQuestions)
        {
            var code = q.Symptom.Code;
            var baseCode = GetBaseCode(code);
            if (!variantMap.ContainsKey(baseCode))
                variantMap[baseCode] = new();
            variantMap[baseCode].Add((q.SymptomId, q.Symptom.TbTypeId, q.Symptom.TbType.Name));
        }

        // Get all risk rule weights: (symptomId, tbTypeId) → weight
        var allWeights = await _db.RiskRules
            .Where(r => r.AssessmentTypeId == fullAssessmentTypeId && r.IsActive)
            .ToDictionaryAsync(r => (r.SymptomId, r.TbTypeId), r => r.Weight);

        // Filter: keep only non-variant questions (primary/base codes)
        var questions = allQuestions
            .Where(q => !IsVariantCode(q.Symptom.Code))
            .Select(q =>
            {
                var baseCode = GetBaseCode(q.Symptom.Code);
                var applicableTbTypes = (variantMap.TryGetValue(baseCode, out var variants)
                    ? variants
                    : new List<(int SymptomId, int TbTypeId, string TbTypeName)>())
                    .Select(v => new TbTypeWeightDto
                    {
                        TbTypeId = v.TbTypeId,
                        TbTypeName = v.TbTypeName,
                        Weight = allWeights.TryGetValue((v.SymptomId, v.TbTypeId), out var w) ? w : 0m,
                    })
                    .ToList();

                return new QuickCheckQuestionDto
                {
                    QuestionId = q.Id,
                    SymptomId = q.SymptomId,
                    SymptomCode = q.Symptom.Code,
                    SymptomName = q.Symptom.Name,
                    SymptomDescription = q.Symptom.Description,
                    QuestionText = q.QuestionText,
                    SortOrder = q.SortOrder,
                    IsRequired = q.IsRequired,
                    Weight = allWeights.TryGetValue((q.SymptomId, q.Symptom.TbTypeId), out var primaryW) ? primaryW : 0m,
                    TbTypeId = q.Symptom.TbTypeId,
                    TbTypeName = q.Symptom.TbType.Name,
                    ApplicableTbTypes = applicableTbTypes,
                };
            })
            .ToList();

        // Distinct TB type IDs from ALL questions (including variants), so risk levels cover all types
        var distinctTbTypeIds = allQuestions
            .Select(q => q.Symptom.TbTypeId)
            .Distinct()
            .ToList();

        var riskLevels = await _db.RiskLevels
            .Where(rl => distinctTbTypeIds.Contains(rl.TbTypeId))
            .Select(rl => new RiskLevelDto
            {
                Id = rl.Id,
                TbTypeId = rl.TbTypeId,
                Code = rl.Code,
                Title = rl.Title,
                MinScore = rl.MinScore,
                MaxScore = rl.MaxScore,
                Description = rl.Description,
                Recommendation = rl.Recommendation,
            })
            .ToListAsync();

        return Ok(ApiResponse<QuickCheckConfigDto>.Ok(new QuickCheckConfigDto
        {
            Questions = questions,
            RiskLevels = riskLevels,
            ScoringMethod = assessmentType?.ScoringMethod ?? "soft_saturation_cf",
            SaturationK = assessmentType?.SaturationK ?? 0.35,
        }));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitAssessment([FromBody] SubmitAssessmentRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        if (request.AssessmentTypeId <= 0 || request.Answers.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("AssessmentTypeId and answers are required."));

        _logger.LogInformation("SubmitAssessment: userId={UserId} assessmentTypeId={AssessmentTypeId} answers={AnswersCount}",
            userId, request.AssessmentTypeId, request.Answers.Count);

        // Use a single timestamp for all inserted rows from one submission (full assessment creates multiple rows),
        // so history can group them as one session.
        var submissionAtUtc = DateTime.UtcNow;

        var assessmentType = await _db.AssessmentTypes.FindAsync(request.AssessmentTypeId);
        if (assessmentType is null)
            return BadRequest(ApiResponse<object>.Fail("Invalid assessment type."));

        var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();

        var questions = await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .ThenInclude(s => s.TbType)
            .Where(q => q.AssessmentTypeId == request.AssessmentTypeId && questionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("No valid questions found for this assessment type."));

        var weights = await _db.RiskRules
            .Where(r => r.AssessmentTypeId == request.AssessmentTypeId && r.IsActive)
            .Select(r => new { r.SymptomId, r.TbTypeId, r.Weight })
            .ToListAsync();

        var weightBySymptomAndTb = weights
            .GroupBy(w => (w.SymptomId, w.TbTypeId))
            .ToDictionary(g => g.Key, g => g.First().Weight);

        var answerByQuestionId = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().CfValue);

        // Build variant map from ALL symptoms for this assessment type
        var allSymptomCodes = await _db.AssessmentQuestions
            .Include(q => q.Symptom)
            .Where(q => q.AssessmentTypeId == request.AssessmentTypeId)
            .Select(q => new { q.SymptomId, q.Symptom.Code, q.Symptom.TbTypeId })
            .ToListAsync();

        // baseCode → List<(symptomId, tbTypeId)>
        var variantMap = new Dictionary<string, List<(int SymptomId, int TbTypeId)>>();
        foreach (var s in allSymptomCodes)
        {
            var baseCode = GetBaseCode(s.Code);
            if (!variantMap.ContainsKey(baseCode))
                variantMap[baseCode] = new();
            variantMap[baseCode].Add((s.SymptomId, s.TbTypeId));
        }

        // Build tbTypeId → tbTypeName lookup
        var tbTypeNames = await _db.TbTypes
            .Where(t => allSymptomCodes.Select(s => s.TbTypeId).Distinct().Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var selectedSymptoms = new List<object>();
        var sumByTbType = new Dictionary<int, double>();

        foreach (var q in questions)
        {
            answerByQuestionId.TryGetValue(q.Id, out var cfValue);

            var key = (q.SymptomId, q.Symptom.TbTypeId);
            var weight = weightBySymptomAndTb.TryGetValue(key, out var w) ? w : 0m;

            if (cfValue > 0)
            {
                // Apply contribution to ALL variant TB types using their specific weights
                var baseCode = GetBaseCode(q.Symptom.Code);
                if (variantMap.TryGetValue(baseCode, out var variants))
                {
                    foreach (var (variantSymptomId, variantTbTypeId) in variants)
                    {
                        var variantWeight = weightBySymptomAndTb.TryGetValue((variantSymptomId, variantTbTypeId), out var vw) ? vw : weight;
                        var contribution = (double)(variantWeight * cfValue);
                        if (!sumByTbType.ContainsKey(variantTbTypeId))
                            sumByTbType[variantTbTypeId] = 0;
                        sumByTbType[variantTbTypeId] += contribution;
                    }
                }
            }

            selectedSymptoms.Add(new
            {
                questionId = q.Id,
                symptomId = q.SymptomId,
                symptomCode = q.Symptom.Code,
                symptomName = q.Symptom.Name,
                symptomDescription = q.Symptom.Description,
                tbTypeId = q.Symptom.TbTypeId,
                tbTypeName = q.Symptom.TbType.Name,
                cfValue,
                weight
            });
        }

        var isQuickAssessment = request.AssessmentTypeId == 1;
        var k = assessmentType.SaturationK <= 0 ? 0.35 : (double)assessmentType.SaturationK;

        // For quick assessment, score is based on total number of questions and their weights.
        // Denominator uses all questions for the assessment type (not only the submitted ones).
        Dictionary<int, double> totalWeightByTbType = new();
        if (isQuickAssessment)
        {
            var allQuestions = await _db.AssessmentQuestions
                .Include(q => q.Symptom)
                .Where(q => q.AssessmentTypeId == request.AssessmentTypeId)
                .ToListAsync();

            foreach (var q in allQuestions)
            {
                var key = (q.SymptomId, q.Symptom.TbTypeId);
                var w = weightBySymptomAndTb.TryGetValue(key, out var found) ? found : 0m;
                if (!totalWeightByTbType.ContainsKey(q.Symptom.TbTypeId))
                    totalWeightByTbType[q.Symptom.TbTypeId] = 0;
                totalWeightByTbType[q.Symptom.TbTypeId] += (double)w;
            }
        }

        // Include ALL TB types from the assessment (not just answered questions),
        // so that shared symptoms contribute to every variant TB type.
        var tbTypeIds = allSymptomCodes.Select(s => s.TbTypeId).Distinct().ToList();
        var riskLevels = await _db.RiskLevels
            .Where(rl => tbTypeIds.Contains(rl.TbTypeId))
            .ToListAsync();

        object BuildRiskLevelPayload(RiskLevel? rl) => rl is null
            ? new { id = 0, code = "LOW", title = "Low Risk", minScore = 0.0, maxScore = 100.0, description = "", recommendation = "" }
            : new
            {
                id = rl.Id,
                code = rl.Code,
                title = rl.Title,
                minScore = (double)rl.MinScore,
                maxScore = (double)rl.MaxScore,
                description = rl.Description ?? "",
                recommendation = rl.Recommendation ?? "",
            };

        RiskLevel? FindMatchedRiskLevel(int tbTypeId, double score)
        {
            var levels = riskLevels.Where(r => r.TbTypeId == tbTypeId).OrderBy(r => r.MinScore).ToList();
            if (levels.Count == 0) return null;
            var exact = levels.FirstOrDefault(l => score >= (double)l.MinScore && score <= (double)l.MaxScore);
            if (exact is not null) return exact;
            if (score < (double)levels.First().MinScore) return levels.First();
            return levels.Last();
        }

        // Build symptomId → set of tbTypeIds it contributes to (for shared symptom display)
        var symptomTbTypes = new Dictionary<int, HashSet<int>>();
        foreach (var (_, variants) in variantMap)
        {
            var allTbTypeIds = variants.Select(v => v.TbTypeId).ToHashSet();
            foreach (var (sId, _) in variants)
            {
                var code = allSymptomCodes.First(x => x.SymptomId == sId).Code;
                if (!IsVariantCode(code))
                {
                    symptomTbTypes[sId] = allTbTypeIds;
                }
            }
        }

        var breakdown = new List<object>();
        var historyRecords = new List<AssessmentHistory>();

        // Per-TB-type breakdown (used for score calculation and history records)
        var perTbTypeResults = new List<(int tbTypeId, string tbTypeName, double combinedCf, double score, RiskLevel? matched, object payload)>();

        foreach (var tbTypeId in tbTypeIds)
        {
            sumByTbType.TryGetValue(tbTypeId, out var sum);

            double combinedCf;
            double score;

            if (isQuickAssessment)
            {
                totalWeightByTbType.TryGetValue(tbTypeId, out var totalWeight);
                if (totalWeight <= 0)
                {
                    combinedCf = 0;
                    score = 0;
                }
                else
                {
                    combinedCf = sum / totalWeight;
                    score = Math.Round(combinedCf * 100, 0);
                }
            }
            else
            {
                combinedCf = 1.0 - Math.Exp(-k * sum);
                score = Math.Round(combinedCf * 100, 0);
            }
            var matched = FindMatchedRiskLevel(tbTypeId, score);

            var resultPayload = new
            {
                tbTypeId,
                tbTypeName = tbTypeNames.TryGetValue(tbTypeId, out var name) ? name : "Unknown",
                combinedCF = combinedCf,
                totalScore = score,
                riskLevel = BuildRiskLevelPayload(matched),
            };

            breakdown.Add(resultPayload);
            perTbTypeResults.Add((tbTypeId, resultPayload.tbTypeName, combinedCf, score, matched, resultPayload));
        }

        if (isQuickAssessment)
        {
            // Quick check: create ONE history record with the highest-risk TB type as primary.
            var best = perTbTypeResults
                .OrderByDescending(r => RiskRank(r.matched?.Code))
                .ThenByDescending(r => r.score)
                .First();

            // Overall score: weighted average across TB types or just the best TB type's score
            var overallScore = best.score;
            var overallCf = best.combinedCf;
            var overallTbTypeId = best.tbTypeId;
            var overallTbTypeName = best.tbTypeName;

            historyRecords.Add(new AssessmentHistory
            {
                UserId = userId,
                AssessmentTypeId = request.AssessmentTypeId,
                PrimaryTbTypeId = overallTbTypeId,
                RiskLevelId = best.matched?.Id ?? 0,
                TotalScore = (decimal)overallScore,
                SelectedSymptoms = JsonSerializer.Serialize(selectedSymptoms),
                ScoreBreakdown = JsonSerializer.Serialize(new { results = breakdown }),
                CreatedAt = submissionAtUtc,
            });
        }
        else
        {
            // Full assessment: create one record per TB type.
            foreach (var (tbTypeId, tbTypeName, combinedCf, score, matched, payload) in perTbTypeResults)
            {
                historyRecords.Add(new AssessmentHistory
                {
                    UserId = userId,
                    AssessmentTypeId = request.AssessmentTypeId,
                    PrimaryTbTypeId = tbTypeId,
                    RiskLevelId = matched?.Id ?? 0,
                    TotalScore = (decimal)score,
                    SelectedSymptoms = JsonSerializer.Serialize(selectedSymptoms.Cast<dynamic>().Where(s => (int)s.tbTypeId == tbTypeId || (symptomTbTypes.TryGetValue((int)s.symptomId, out var ids) && ids.Contains(tbTypeId))).ToList()),
                    ScoreBreakdown = JsonSerializer.Serialize(new { results = new[] { payload } }),
                    CreatedAt = submissionAtUtc,
                });
            }
        }

        // Finalize history records - ensure RiskLevelId is valid
        foreach (var history in historyRecords)
        {
            if (history.RiskLevelId == 0)
            {
                var fallback = riskLevels.Where(r => r.TbTypeId == history.PrimaryTbTypeId).OrderBy(r => r.MinScore).FirstOrDefault();
                if (fallback is not null) history.RiskLevelId = fallback.Id;
            }
        }

        if (historyRecords.Any(h => h.RiskLevelId == 0))
            return StatusCode(500, ApiResponse<object>.Fail("Risk level configuration missing for one or more TB types."));

        bool shouldSave = true;
        if (isQuickAssessment)
        {
            var hasFullAssessment = await _db.AssessmentHistories.AnyAsync(h => h.UserId == userId && h.AssessmentTypeId == 2);
            if (hasFullAssessment)
            {
                shouldSave = false;
            }
        }

        if (shouldSave)
        {
            try
            {
                _db.AssessmentHistories.AddRange(historyRecords);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save AssessmentHistories via EF Core. userId={UserId} assessmentTypeId={AssessmentTypeId}", userId, request.AssessmentTypeId);
                return StatusCode(500, ApiResponse<object>.Fail($"Database insert failed: {ex.GetBaseException().Message}"));
            }
        }

        return Ok(ApiResponse<object>.Ok(new { historyIds = shouldSave ? historyRecords.Select(h => h.Id) : Array.Empty<long>() }, shouldSave ? "Assessment saved to history." : "Assessment processed (not saved due to existing full assessment)."));
    }

    [HttpGet("history-sessions")]
    public async Task<IActionResult> GetHistorySessions()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var histories = await _db.AssessmentHistories
            .Include(h => h.AssessmentType)
            .Include(h => h.PrimaryTbType)
            .Include(h => h.RiskLevel)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        DateTime RoundToSecondUtc(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, DateTimeKind.Utc);
        }

        var sessions = histories
            .GroupBy(h => new { h.AssessmentTypeId, CreatedAt = RoundToSecondUtc(h.CreatedAt) })
            .Select(g =>
            {
                var best = g
                    .OrderByDescending(h => RiskRank(h.RiskLevel?.Code))
                    .ThenByDescending(h => h.TotalScore)
                    .First();

                var createdAtUtc = g.Key.CreatedAt;
                var sessionKey = EncodeSessionKey(g.Key.AssessmentTypeId, createdAtUtc);

                return new
                {
                    sessionKey,
                    createdAt = createdAtUtc,
                    assessmentTypeId = g.Key.AssessmentTypeId,
                    assessmentTypeName = best.AssessmentType?.Name ?? "Assessment",
                    riskLevelId = best.RiskLevelId,
                    riskLevelTitle = best.RiskLevel?.Title ?? "Unknown",
                    riskLevelCode = best.RiskLevel?.Code ?? "LOW",
                    primaryTbTypeId = best.PrimaryTbTypeId,
                    primaryTbTypeName = best.PrimaryTbType?.Name ?? "Unknown",
                    totalScore = best.TotalScore,
                    historyIds = g.Select(x => x.Id).OrderBy(x => x).ToList(),
                };
            })
            .OrderByDescending(s => s.createdAt)
            .ToList();

        return Ok(ApiResponse<object>.Ok(sessions));
    }

    [HttpGet("history-sessions/{sessionKey}")]
    public async Task<IActionResult> GetHistorySessionDetail(string sessionKey)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        if (!TryDecodeSessionKey(sessionKey, out var assessmentTypeId, out var createdAtUtc))
            return BadRequest(ApiResponse<object>.Fail("Invalid session key."));

        var createdAtRoundedUtc = new DateTime(createdAtUtc.Year, createdAtUtc.Month, createdAtUtc.Day, createdAtUtc.Hour, createdAtUtc.Minute, createdAtUtc.Second, DateTimeKind.Utc);
        var nextSecondUtc = createdAtRoundedUtc.AddSeconds(1);

        var items = await _db.AssessmentHistories
            .Include(h => h.AssessmentType)
            .Include(h => h.PrimaryTbType)
            .Include(h => h.RiskLevel)
            .Where(h => h.UserId == userId
                        && h.AssessmentTypeId == assessmentTypeId
                        && h.CreatedAt >= createdAtRoundedUtc
                        && h.CreatedAt < nextSecondUtc)
            .OrderBy(h => h.PrimaryTbTypeId)
            .Select(h => new
            {
                h.Id,
                h.AssessmentTypeId,
                assessmentTypeName = h.AssessmentType != null ? h.AssessmentType.Name : "Assessment",
                h.PrimaryTbTypeId,
                primaryTbTypeName = h.PrimaryTbType != null ? h.PrimaryTbType.Name : "Unknown",
                h.RiskLevelId,
                riskLevelTitle = h.RiskLevel != null ? h.RiskLevel.Title : "Unknown",
                riskLevelCode = h.RiskLevel != null ? h.RiskLevel.Code : "LOW",
                h.TotalScore,
                h.ResultNote,
                h.CreatedAt,
                selectedSymptoms = JsonSerializer.Deserialize<JsonElement>(h.SelectedSymptoms),
                scoreBreakdown = JsonSerializer.Deserialize<JsonElement>(h.ScoreBreakdown),
            })
            .ToListAsync();

        if (items.Count == 0)
            return NotFound(ApiResponse<object>.Fail("Session not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            sessionKey,
            createdAt = createdAtRoundedUtc,
            assessmentTypeId,
            assessmentTypeName = items.First().assessmentTypeName,
            items,
        }));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var history = await _db.AssessmentHistories
            .Include(h => h.AssessmentType)
            .Include(h => h.PrimaryTbType)
            .Include(h => h.RiskLevel)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.Id,
                h.AssessmentTypeId,
                AssessmentTypeName = h.AssessmentType != null ? h.AssessmentType.Name : "Assessment",
                h.PrimaryTbTypeId,
                PrimaryTbTypeName = h.PrimaryTbType != null ? h.PrimaryTbType.Name : "Unknown",
                h.RiskLevelId,
                RiskLevelTitle = h.RiskLevel != null ? h.RiskLevel.Title : "Unknown",
                RiskLevelCode = h.RiskLevel != null ? h.RiskLevel.Code : "LOW",
                h.TotalScore,
                h.CreatedAt,
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(history));
    }

    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetHistoryById(long id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var history = await _db.AssessmentHistories
            .Include(h => h.AssessmentType)
            .Include(h => h.PrimaryTbType)
            .Include(h => h.RiskLevel)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        if (history is null)
            return NotFound(ApiResponse<object>.Fail("History not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            history.Id,
            history.AssessmentTypeId,
            AssessmentTypeName = history.AssessmentType != null ? history.AssessmentType.Name : "Assessment",
            history.PrimaryTbTypeId,
            PrimaryTbTypeName = history.PrimaryTbType != null ? history.PrimaryTbType.Name : "Unknown",
            history.RiskLevelId,
            RiskLevelTitle = history.RiskLevel != null ? history.RiskLevel.Title : "Unknown",
            RiskLevelCode = history.RiskLevel != null ? history.RiskLevel.Code : "LOW",
            history.TotalScore,
            history.ResultNote,
            history.CreatedAt,
            SelectedSymptoms = JsonSerializer.Deserialize<JsonElement>(history.SelectedSymptoms),
            ScoreBreakdown = JsonSerializer.Deserialize<JsonElement>(history.ScoreBreakdown),
        }));
    }
}
