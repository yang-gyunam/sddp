-- =============================================================================
-- provision-test.sql
-- SDDP Database Provisioning — Public Shared Test Data
-- =============================================================================
-- Single source of truth
-- `make db-seed-test` executes this file directly.
-- Do not run this file in production environments.
-- =============================================================================

\echo '============================================='
\echo 'SDDP Public Test Data Seeding Started'
\echo '============================================='
\echo ''

\set ON_ERROR_STOP on

\echo '[90] Inserting Public Test Tenants and Projects...'

INSERT INTO group_codes (id, code, name, description, is_system)
VALUES
    ('00000000-0000-0000-0001-000000000099', 'TEST_TENANT', 'Test Tenant', 'Public tenant discriminator for sample data', TRUE)
ON CONFLICT (id) DO UPDATE SET
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    is_system = EXCLUDED.is_system,
    updated_at = NOW();

DELETE FROM codes
WHERE group_code_id = '00000000-0000-0000-0001-000000000099'
  AND code IN ('TENANT_ACME', 'TENANT_ONBRICK', 'PROJ_ONBRICK_PAY');

INSERT INTO codes (group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
    ('00000000-0000-0000-0001-000000000099', 'TENANT_ACME', 'ACME Platform', 'ACME Platform', 'ACME Platform', TRUE, FALSE, 1,
     '{"id": "00000000-0000-0000-0010-000000000001", "domain": "acme.sddp.local"}'),
    ('00000000-0000-0000-0001-000000000099', 'PROJ_ONBRICK_PAY', 'OnBrick Payment System', 'OnBrick Payment System', 'OnBrick Payment System', FALSE, FALSE, 40,
     '{"id": "00000000-0000-0000-0020-000000000021", "tenantId": "00000000-0000-0000-0010-000000000001", "status": "ACTIVE"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
    name = EXCLUDED.name,
    name_en = EXCLUDED.name_en,
    name_ko = EXCLUDED.name_ko,
    is_default = EXCLUDED.is_default,
    is_system = EXCLUDED.is_system,
    sort_order = EXCLUDED.sort_order,
    attributes = EXCLUDED.attributes,
    updated_at = NOW();

INSERT INTO projects (
    id,
    tenant_id,
    code,
    name,
    description,
    status,
    owner_id,
    repo_url,
    repo_branch,
    artifact_root_path,
    sync_interval_minutes
)
VALUES
    ('00000000-0000-0000-0020-000000000021', '00000000-0000-0000-0010-000000000001', 'PROJ_ONBRICK_PAY',
     'OnBrick Payment System', 'Brownfield payment migration showcase for the public repository', 'active',
     '00000000-0000-0000-0005-000000000301', 'https://github.com/onbrick/payment-system.git', 'main', 'artifacts', 60)
ON CONFLICT (id) DO UPDATE SET
    tenant_id = EXCLUDED.tenant_id,
    code = EXCLUDED.code,
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    status = EXCLUDED.status,
    owner_id = EXCLUDED.owner_id,
    repo_url = EXCLUDED.repo_url,
    repo_branch = EXCLUDED.repo_branch,
    artifact_root_path = EXCLUDED.artifact_root_path,
    sync_interval_minutes = EXCLUDED.sync_interval_minutes,
    updated_at = NOW();

\echo '[91] Inserting Public Test Persons and Users...'

INSERT INTO persons (id, display_name, email, organization, department, title, person_type, timezone, locale, is_active)
VALUES
    ('00000000-0000-0000-0004-000000000301', 'Minjun Park', 'minjun.park@onbrick.local', 'OnBrick Studio', 'Product', 'Product Owner', 'INTERNAL', 'Asia/Seoul', 'en-US', TRUE),
    ('00000000-0000-0000-0004-000000000302', 'Soojin Yoon', 'soojin.yoon@onbrick.local', 'OnBrick Studio', 'Engineering', 'Reviewer', 'INTERNAL', 'Asia/Seoul', 'en-US', TRUE),
    ('00000000-0000-0000-0004-000000000303', 'Hyunwoo Kim', 'hyunwoo.kim@onbrick.local', 'OnBrick Studio', 'Engineering', 'Backend Developer', 'INTERNAL', 'Asia/Seoul', 'en-US', TRUE),
    ('00000000-0000-0000-0004-000000000304', 'Jieun Lee', 'jieun.lee@onbrick.local', 'OnBrick Studio', 'Engineering', 'Frontend Developer', 'INTERNAL', 'Asia/Seoul', 'en-US', TRUE)
ON CONFLICT (id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    email = EXCLUDED.email,
    organization = EXCLUDED.organization,
    department = EXCLUDED.department,
    title = EXCLUDED.title,
    person_type = EXCLUDED.person_type,
    timezone = EXCLUDED.timezone,
    locale = EXCLUDED.locale,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

INSERT INTO users (id, person_id, username, email, password_hash, display_name, is_email_verified, is_locked, is_ai, is_active)
VALUES
    ('00000000-0000-0000-0005-000000000301', '00000000-0000-0000-0004-000000000301', 'minjun.park', 'minjun.park@onbrick.local', '$2a$12$.SR6n9PKSWV0GuB8Zncvjeid2JYRYI6.Omxls629LvO.O.D.XbQIu', 'Minjun Park', TRUE, FALSE, FALSE, TRUE),
    ('00000000-0000-0000-0005-000000000302', '00000000-0000-0000-0004-000000000302', 'soojin.yoon', 'soojin.yoon@onbrick.local', '$2a$12$.SR6n9PKSWV0GuB8Zncvjeid2JYRYI6.Omxls629LvO.O.D.XbQIu', 'Soojin Yoon', TRUE, FALSE, FALSE, TRUE),
    ('00000000-0000-0000-0005-000000000303', '00000000-0000-0000-0004-000000000303', 'hyunwoo.kim', 'hyunwoo.kim@onbrick.local', '$2a$12$.SR6n9PKSWV0GuB8Zncvjeid2JYRYI6.Omxls629LvO.O.D.XbQIu', 'Hyunwoo Kim', TRUE, FALSE, FALSE, TRUE),
    ('00000000-0000-0000-0005-000000000304', '00000000-0000-0000-0004-000000000304', 'jieun.lee', 'jieun.lee@onbrick.local', '$2a$12$.SR6n9PKSWV0GuB8Zncvjeid2JYRYI6.Omxls629LvO.O.D.XbQIu', 'Jieun Lee', TRUE, FALSE, FALSE, TRUE)
ON CONFLICT (id) DO UPDATE SET
    person_id = EXCLUDED.person_id,
    username = EXCLUDED.username,
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    display_name = EXCLUDED.display_name,
    is_email_verified = EXCLUDED.is_email_verified,
    is_locked = EXCLUDED.is_locked,
    is_ai = EXCLUDED.is_ai,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

\echo '[92] Assigning Public Project Roles...'

DELETE FROM messages
WHERE sender_id IN (
    SELECT id
    FROM users
    WHERE is_ai = TRUE
      AND email LIKE '%@onbrick.local'
);

DELETE FROM conversation_members
WHERE user_id IN (
    SELECT id
    FROM users
    WHERE is_ai = TRUE
      AND email LIKE '%@onbrick.local'
);

DELETE FROM project_members
WHERE user_id IN (
    '00000000-0000-0000-0005-000000000301',
    '00000000-0000-0000-0005-000000000302',
    '00000000-0000-0000-0005-000000000303',
    '00000000-0000-0000-0005-000000000304'
)
OR user_id IN (
    SELECT id
    FROM users
    WHERE is_ai = TRUE
      AND email LIKE '%@onbrick.local'
);

DELETE FROM user_roles
WHERE user_id IN (
    '00000000-0000-0000-0005-000000000301',
    '00000000-0000-0000-0005-000000000302',
    '00000000-0000-0000-0005-000000000303',
    '00000000-0000-0000-0005-000000000304'
)
OR user_id IN (
    SELECT id
    FROM users
    WHERE is_ai = TRUE
      AND email LIKE '%@onbrick.local'
);

DELETE FROM users
WHERE is_ai = TRUE
  AND email LIKE '%@onbrick.local';

DELETE FROM persons
WHERE person_type = 'SYSTEM'
  AND email LIKE '%@onbrick.local';

INSERT INTO user_roles (user_id, role_id, tenant_id, project_id, assigned_by)
VALUES
    ('00000000-0000-0000-0005-000000000301', '00000000-0000-0000-0002-000000000002', '00000000-0000-0000-0010-000000000001', '00000000-0000-0000-0020-000000000021', '00000000-0000-0000-0005-000000000001'),
    ('00000000-0000-0000-0005-000000000302', '00000000-0000-0000-0002-000000000005', '00000000-0000-0000-0010-000000000001', '00000000-0000-0000-0020-000000000021', '00000000-0000-0000-0005-000000000001'),
    ('00000000-0000-0000-0005-000000000303', '00000000-0000-0000-0002-000000000004', '00000000-0000-0000-0010-000000000001', '00000000-0000-0000-0020-000000000021', '00000000-0000-0000-0005-000000000001'),
    ('00000000-0000-0000-0005-000000000304', '00000000-0000-0000-0002-000000000004', '00000000-0000-0000-0010-000000000001', '00000000-0000-0000-0020-000000000021', '00000000-0000-0000-0005-000000000001')
ON CONFLICT DO NOTHING;

INSERT INTO project_members (project_id, user_id, user_role_id, joined_at, created_at, updated_at, is_active)
SELECT
    ur.project_id,
    ur.user_id,
    ur.id,
    NOW(),
    NOW(),
    NOW(),
    TRUE
FROM user_roles ur
WHERE ur.project_id = '00000000-0000-0000-0020-000000000021'
  AND ur.user_id IN (
      '00000000-0000-0000-0005-000000000301',
      '00000000-0000-0000-0005-000000000302',
      '00000000-0000-0000-0005-000000000303',
      '00000000-0000-0000-0005-000000000304'
  )
ON CONFLICT (project_id, user_id) WHERE is_active DO UPDATE
SET
    user_role_id = EXCLUDED.user_role_id,
    updated_at = NOW(),
    is_active = TRUE;

\echo '[93] Confirmation...'

DO $$
DECLARE
    project_count INT;
    person_count INT;
    user_count INT;
    member_count INT;
BEGIN
    SELECT COUNT(*) INTO project_count
    FROM projects
    WHERE id = '00000000-0000-0000-0020-000000000021'
      AND tenant_id = '00000000-0000-0000-0010-000000000001';

    SELECT COUNT(*) INTO person_count
    FROM persons
    WHERE id::text LIKE '00000000-0000-0000-0004-0000000003%';

    SELECT COUNT(*) INTO user_count
    FROM users
    WHERE id::text LIKE '00000000-0000-0000-0005-0000000003%';

    SELECT COUNT(*) INTO member_count
    FROM project_members
    WHERE project_id = '00000000-0000-0000-0020-000000000021'
      AND is_active = TRUE;

    RAISE NOTICE 'Public test data inserted: % project, % persons, % users, % active project members', project_count, person_count, user_count, member_count;
END $$;
