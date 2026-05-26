-- ============================================================================
-- Quick Check Assessment Seed Data (assessment_type_id = 1)
-- Run this against the Supabase PostgreSQL database
-- ============================================================================

-- 1. Ensure TB Type exists
INSERT INTO tbcare_plus.tb_types (id, code, name, description, body_area, is_active, sort_order, created_at, updated_at)
VALUES (1, 'TB_PARU', 'Tuberkulosis Paru', 'Pulmonary Tuberculosis', 'Lungs / Respiratory', true, 1, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 2. Ensure Assessment Type exists
INSERT INTO tbcare_plus.assessment_types (id, code, name, description, created_at, updated_at)
VALUES (1, 'QUICK', 'Quick TBC Check', 'Rapid assessment based on common symptoms', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 3. Symptoms for TB_PARU (8 symptoms)
INSERT INTO tbcare_plus.symptoms (id, tb_type_id, code, name, description, is_active, created_at, updated_at) VALUES
(1,  1, 'COUGH_2W',     'Persistent Cough',        'Cough persisting for more than 2 weeks',            true, NOW(), NOW()),
(2,  1, 'COUGH_BLOOD',  'Coughing Up Blood',       'Presence of blood when coughing (hemoptysis)',     true, NOW(), NOW()),
(3,  1, 'CHEST_PAIN',   'Chest Pain',              'Pain or tightness in the chest area',               true, NOW(), NOW()),
(4,  1, 'SHORT_BREATH', 'Shortness of Breath',     'Difficulty breathing or feeling breathless',        true, NOW(), NOW()),
(5,  1, 'WEIGHT_LOSS',  'Unexplained Weight Loss', 'Noticeable weight loss without dietary changes',     true, NOW(), NOW()),
(6,  1, 'FEVER',        'Prolonged Fever',         'Persistent fever or chills for extended period',    true, NOW(), NOW()),
(7,  1, 'NIGHT_SWEATS', 'Night Sweats',            'Excessive sweating during sleep',                    true, NOW(), NOW()),
(8,  1, 'FATIGUE',      'Fatigue & Weakness',      'Persistent tiredness and lack of energy',           true, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 4. Assessment Questions for Quick Check (assessment_type_id = 1)
INSERT INTO tbcare_plus.assessment_questions (id, assessment_type_id, symptom_id, question_text, sort_order, is_required, created_at, updated_at) VALUES
(1,  1, 1, 'Do you have a persistent cough lasting more than 2 weeks?',    1, true, NOW(), NOW()),
(2,  1, 2, 'Have you been coughing up blood?',                             2, true, NOW(), NOW()),
(3,  1, 3, 'Do you experience chest pain or tightness?',                   3, true, NOW(), NOW()),
(4,  1, 4, 'Do you feel short of breath regularly?',                       4, true, NOW(), NOW()),
(5,  1, 5, 'Have you lost weight without trying recently?',                5, true, NOW(), NOW()),
(6,  1, 6, 'Do you have a prolonged fever or chills?',                     6, true, NOW(), NOW()),
(7,  1, 7, 'Do you experience night sweats?',                              7, true, NOW(), NOW()),
(8,  1, 8, 'Do you feel unusually fatigued or weak?',                      8, true, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 5. Risk Rules: CF weight per symptom for Quick Check on TB_PARU
INSERT INTO tbcare_plus.risk_rules (id, assessment_type_id, symptom_id, tb_type_id, weight, is_active, created_at, updated_at) VALUES
(1,  1, 1, 1, 0.15, true, NOW(), NOW()),   -- Persistent Cough: CF 0.15
(2,  1, 2, 1, 0.20, true, NOW(), NOW()),   -- Coughing Blood: CF 0.20
(3,  1, 3, 1, 0.10, true, NOW(), NOW()),   -- Chest Pain: CF 0.10
(4,  1, 4, 1, 0.10, true, NOW(), NOW()),   -- Shortness of Breath: CF 0.10
(5,  1, 5, 1, 0.15, true, NOW(), NOW()),   -- Weight Loss: CF 0.15
(6,  1, 6, 1, 0.10, true, NOW(), NOW()),   -- Fever: CF 0.10
(7,  1, 7, 1, 0.10, true, NOW(), NOW()),   -- Night Sweats: CF 0.10
(8,  1, 8, 1, 0.10, true, NOW(), NOW())    -- Fatigue: CF 0.10
ON CONFLICT (id) DO NOTHING;

-- 6. Risk Levels for TB_PARU
INSERT INTO tbcare_plus.risk_levels (id, tb_type_id, code, title, min_score, max_score, description, recommendation, created_at, updated_at) VALUES
(1, 1, 'LOW',    'Risiko Rendah',    0,  30, 'Gejala Anda menunjukkan indikasi rendah TBC. Jaga kesehatan dan pantau kondisi Anda.',                       'Tidak perlu tindakan segera. Pertahankan gaya hidup sehat.',                          NOW(), NOW()),
(2, 1, 'MEDIUM', 'Risiko Sedang',    31, 60, 'Beberapa gejala memerlukan perhatian. Disarankan untuk melanjutkan dengan pemeriksaan yang lebih detail.', 'Lanjutkan dengan pemeriksaan lengkap untuk evaluasi yang lebih akurat.',               NOW(), NOW()),
(3, 1, 'HIGH',   'Risiko Tinggi',    61, 100, 'Gejala Anda sangat mengindikasikan potensi TBC. Silakan lanjutkan dengan pemeriksaan lengkap dan cari bantuan medis.', 'Segera cari bantuan medis dan selesaikan pemeriksaan lengkap.',          NOW(), NOW())
ON CONFLICT (id) DO UPDATE SET
  title = EXCLUDED.title,
  description = EXCLUDED.description,
  recommendation = EXCLUDED.recommendation,
  updated_at = NOW();

-- 7. Reset sequences to prevent ID conflicts
SELECT setval('tbcare_plus.tb_types_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.tb_types), 1));
SELECT setval('tbcare_plus.assessment_types_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.assessment_types), 1));
SELECT setval('tbcare_plus.symptoms_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.symptoms), 1));
SELECT setval('tbcare_plus.assessment_questions_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.assessment_questions), 1));
SELECT setval('tbcare_plus.risk_rules_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.risk_rules), 1));
SELECT setval('tbcare_plus.risk_levels_id_seq', COALESCE((SELECT MAX(id) FROM tbcare_plus.risk_levels), 1));
