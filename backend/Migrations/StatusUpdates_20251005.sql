-- =============================================================================
-- STATUS UPDATES SCRIPT
-- Generated: October 5, 2025
-- Purpose: Restore candidate status changes that were made after initial load
-- Total Updates: 56 candidates (1 Accepted, 4 OnHold, 25 Rejected, 26 Screening)
-- =============================================================================

-- Begin transaction to ensure atomic updates
BEGIN;

-- =============================================================================
-- ACCEPTED STATUS (1 candidate)
-- =============================================================================

UPDATE candidates 
SET 
    current_status = 'Accepted',
    status_updated_at = '2025-10-03 22:47:27.844121+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928edb5a8';
-- Bbober Eng


-- =============================================================================
-- ONHOLD STATUS (4 candidates)
-- =============================================================================

UPDATE candidates 
SET 
    current_status = 'OnHold',
    status_updated_at = '2025-09-29 13:08:55.51518+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928df672f';
-- Michael Chung

UPDATE candidates 
SET 
    current_status = 'OnHold',
    status_updated_at = '2025-09-29 13:11:05.375301+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509282afd51';
-- Mickey Cy pham

UPDATE candidates 
SET 
    current_status = 'OnHold',
    status_updated_at = '2025-09-29 13:11:29.125096+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928fb4a32';
-- Robert Sperospera

UPDATE candidates 
SET 
    current_status = 'OnHold',
    status_updated_at = '2025-09-29 13:12:18.416489+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928d12d7f';
-- Michael Vang


-- =============================================================================
-- REJECTED STATUS (25 candidates)
-- =============================================================================

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-28 23:03:51.034376+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928024a87';
-- Uantharam Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-28 23:27:48.916227+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928c0917e';
-- Danuj Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:04:49.002647+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509280a131e';
-- Bijyata Bhat

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:26:13.749089+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092810d5ca';
-- Jkassaw Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:43:51.562949+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509281b7b9c';
-- Kevindelaney Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:43:59.104745+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928a09511';
-- Kevindelaney Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:44:05.09908+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092865fd6d';
-- Kevindelaney Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:45:47.635877+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092823e536';
-- Netcarltjohnson Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:45:53.514993+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092816597c';
-- Netcarltjohnson Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:49:39.567112+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092856704e';
-- Dbanks Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:51:14.476042+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928645c0e';
-- Aaqeelahmadqadri Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:51:51.287583+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509285b67c5';
-- Paulcamgatech Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:51:58.035488+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509280aa58a';
-- Paulcamgatech Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:52:14.600009+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928558112';
-- Khareprabhat Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:52:44.969523+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928137d93';
-- Subbu Ss

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:52:51.294859+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092831c842';
-- Mansoorsyed Al

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:58:56.806275+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928d94065';
-- Iamxander Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:59:02.966926+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928ce816b';
-- Iamxander Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:59:09.25542+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509281700c3';
-- Iamxander Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 00:59:15.849384+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928d5896a';
-- Iamxander Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 01:03:19.261654+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928eb7e07';
-- Yifanyin Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 01:03:46.382741+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928efeea0';
-- Koppakaamar Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 01:03:52.645655+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928c6544a';
-- Koppakaamar Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-09-29 01:11:27.568851+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928d4871d';
-- Ishmaq Unknown

UPDATE candidates 
SET 
    current_status = 'Rejected',
    status_updated_at = '2025-10-03 22:52:13.321934+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092851cb2e';
-- Chaseson Unknown


-- =============================================================================
-- SCREENING STATUS (26 candidates)
-- =============================================================================

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:05:37.487057+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928ceb2d4';
-- Mominmokhtardev Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:06:42.718312+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509280faee9';
-- Lesuhailofficielle Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:07:33.551391+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928f0a335';
-- Rothluebbert Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:08:20.685629+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928ceedd7';
-- Rai Prashantk

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:09:24.85641+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928579a92';
-- Rodolfovaldez Tech

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:10:22.283366+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509289ba575';
-- Jeffreylin Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:11:11.640798+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928ab2bec';
-- Hwang James

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:12:17.780589+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928659155';
-- Methiumang Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:13:17.509726+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928a16f25';
-- Alexanderlemkin Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:14:39.331548+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928ce99cd';
-- Kaiwenshen Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:15:19.360918+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928311c36';
-- Zherenyang Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:15:51.781261+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928e1d025';
-- Jordanrinehart Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:18:01.482222+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509281bc3a5';
-- Philip S luther

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:18:43.902693+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928bdd4a1';
-- Joshua Smith

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:19:49.401602+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928490819';
-- Mannyiskewl Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:20:37.441068+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928cf77c5';
-- Dylanespinosa Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:21:10.978956+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928e4ccd9';
-- Walcottj Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:21:36.515859+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928096e71';
-- Griffin Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:22:42.885032+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509282aae97';
-- Logancollins Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-28 23:23:36.206684+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509282ec9bf';
-- Saisindhum Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 00:08:18.316618+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092888d994';
-- Chandlerronharris Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 00:09:33.869094+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092868809a';
-- Oluinc Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 00:25:41.09916+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C202509287ced34';
-- Matthjohn Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 00:45:36.712698+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092883b374';
-- Netcarltjohnson Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 00:58:19.125782+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C2025092852d34e';
-- Dinuit Unknown

UPDATE candidates 
SET 
    current_status = 'Screening',
    status_updated_at = '2025-09-29 01:02:26.754759+00',
    status_updated_by = 'User',
    updated_at = CURRENT_TIMESTAMP
WHERE candidate_code = 'C20250928fc10d2';
-- Stephensamuels Unknown


-- =============================================================================
-- VERIFICATION QUERY
-- =============================================================================

-- After running this script, verify the updates:
SELECT 
    current_status, 
    COUNT(*) as count 
FROM candidates 
GROUP BY current_status 
ORDER BY count DESC;

-- Expected results:
-- New:       601
-- Screening: 26
-- Rejected:  25
-- OnHold:    4
-- Accepted:  1

COMMIT;

-- =============================================================================
-- END OF SCRIPT
-- =============================================================================
