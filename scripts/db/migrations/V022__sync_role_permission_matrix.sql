-- =============================================================================
-- 54-data-role-permissions.sql
-- Exact system role-permission seed contract
-- Executable SSOT: Sddp.Abstractions.Constants.RolePermissionMatrix
-- =============================================================================

DROP TABLE IF EXISTS _desired_role_permissions;
DROP TABLE IF EXISTS _role_permission_contract;

CREATE TEMP TABLE _role_permission_contract (
    role_type INT PRIMARY KEY,
    permission_codes TEXT[] NOT NULL
) ON COMMIT DROP;

-- Keep this contract table explicit so docs, C#, base SQL, and migrations can be
-- compared mechanically. RoleType: PO=0, Domain=1, Dev=2, Reviewer=3, QA=4,
-- Admin=5, AI=6.
INSERT INTO _role_permission_contract (role_type, permission_codes)
VALUES
    (5, ARRAY[
        'diagram:read', 'diagram:create', 'diagram:update',
        'spec:create', 'spec:read', 'spec:update', 'spec:delete', 'spec:approve', 'spec:lock',
        'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
        'requirement:create', 'requirement:read', 'requirement:update', 'requirement:delete',
        'generation:execute', 'generation:rollback',
        'glossary:create', 'glossary:read', 'glossary:update', 'glossary:deprecate',
        'user:manage', 'role:assign', 'audit:read',
        'project:create', 'project:read', 'project:update', 'project:delete',
        'task:create', 'task:read', 'task:update', 'task:delete',
        'knowledge:read', 'knowledge:create', 'knowledge:update',
        'knowledge:archive', 'knowledge:publish', 'knowledge:admin'
    ]::TEXT[]),
    (0, ARRAY[
        'diagram:read', 'diagram:create', 'diagram:update',
        'spec:create', 'spec:read', 'spec:update', 'spec:delete', 'spec:approve', 'spec:lock',
        'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
        'requirement:create', 'requirement:read', 'requirement:update', 'requirement:delete',
        'generation:execute', 'generation:rollback',
        'glossary:create', 'glossary:read', 'glossary:update', 'glossary:deprecate',
        'role:assign', 'audit:read',
        'project:create', 'project:read', 'project:update',
        'task:create', 'task:read', 'task:update', 'task:delete',
        'knowledge:read', 'knowledge:create', 'knowledge:update',
        'knowledge:archive', 'knowledge:publish', 'knowledge:admin'
    ]::TEXT[]),
    (1, ARRAY[
        'diagram:read', 'diagram:create', 'diagram:update',
        'spec:create', 'spec:read', 'spec:update',
        'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
        'requirement:create', 'requirement:read', 'requirement:update',
        'glossary:create', 'glossary:read', 'glossary:update', 'glossary:deprecate',
        'project:read',
        'task:create', 'task:read', 'task:update',
        'knowledge:read', 'knowledge:create', 'knowledge:update', 'knowledge:publish'
    ]::TEXT[]),
    (2, ARRAY[
        'diagram:read', 'diagram:create', 'diagram:update',
        'spec:create', 'spec:read', 'spec:update',
        'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
        'requirement:create', 'requirement:read', 'requirement:update',
        'generation:execute',
        'glossary:create', 'glossary:read', 'glossary:update',
        'project:read',
        'task:create', 'task:read', 'task:update',
        'knowledge:read', 'knowledge:create', 'knowledge:update'
    ]::TEXT[]),
    (3, ARRAY[
        'diagram:read',
        'spec:read', 'spec:approve',
        'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
        'requirement:read', 'glossary:read', 'project:read', 'task:read',
        'knowledge:read', 'knowledge:publish'
    ]::TEXT[]),
    (4, ARRAY[
        'diagram:read',
        'spec:read',
        'conversation:create', 'conversation:read', 'conversation:post',
        'requirement:read', 'glossary:read', 'project:read',
        'task:create', 'task:read', 'task:update',
        'knowledge:read'
    ]::TEXT[]);

CREATE TEMP TABLE _desired_role_permissions ON COMMIT DROP AS
SELECT role_type, UNNEST(permission_codes) AS permission_code
FROM _role_permission_contract;

CREATE UNIQUE INDEX ON _desired_role_permissions(role_type, permission_code);

DO $$
DECLARE
    resolved_role_count INT;
    resolved_permission_count INT;
    desired_mapping_count INT;
BEGIN
    SELECT COUNT(*) INTO resolved_role_count
    FROM roles role
    JOIN _role_permission_contract contract ON contract.role_type = role.type
    WHERE role.is_system = TRUE;

    SELECT COUNT(DISTINCT permission.id) INTO resolved_permission_count
    FROM _desired_role_permissions desired
    JOIN permissions permission ON permission.code = desired.permission_code;

    SELECT COUNT(*) INTO desired_mapping_count FROM _desired_role_permissions;

    IF resolved_role_count <> 6 THEN
        RAISE EXCEPTION 'Role-permission seed requires exactly 6 system roles; found %.', resolved_role_count;
    END IF;
    IF resolved_permission_count <> 40 THEN
        RAISE EXCEPTION 'Role-permission seed requires exactly 40 permissions; found %.', resolved_permission_count;
    END IF;
    IF desired_mapping_count <> 152 THEN
        RAISE EXCEPTION 'Role-permission contract requires exactly 152 mappings; found %.', desired_mapping_count;
    END IF;
END $$;

-- Revoke obsolete active mappings for the seven built-in roles. Custom roles are
-- intentionally outside this correction boundary.
UPDATE role_permissions mapping
SET is_active = FALSE,
    updated_at = NOW()
FROM roles role
WHERE mapping.role_id = role.id
  AND role.is_system = TRUE
  AND EXISTS (
      SELECT 1 FROM _role_permission_contract contract WHERE contract.role_type = role.type
  )
  AND mapping.is_active = TRUE
  AND NOT EXISTS (
      SELECT 1
      FROM _desired_role_permissions desired
      JOIN permissions permission ON permission.code = desired.permission_code
      WHERE desired.role_type = role.type
        AND permission.id = mapping.permission_id
  );

-- Add missing mappings and reactivate desired mappings that were soft-deleted.
INSERT INTO role_permissions (role_id, permission_id, is_active)
SELECT role.id, permission.id, TRUE
FROM _desired_role_permissions desired
JOIN roles role
  ON role.type = desired.role_type
 AND role.is_system = TRUE
JOIN permissions permission ON permission.code = desired.permission_code
ON CONFLICT (role_id, permission_id) DO UPDATE
SET is_active = TRUE,
    updated_at = NOW()
WHERE role_permissions.is_active = FALSE;

DO $$
DECLARE
    role_record RECORD;
    permission_count INT;
BEGIN
    RAISE NOTICE '=== Role-Permission Mapping Summary ===';
    FOR role_record IN
        SELECT id, code, name FROM roles WHERE is_system = TRUE ORDER BY sort_order
    LOOP
        SELECT COUNT(*) INTO permission_count
        FROM role_permissions
        WHERE role_id = role_record.id AND is_active = TRUE;
        RAISE NOTICE '% (%): % active permissions', role_record.name, role_record.code, permission_count;
    END LOOP;
END $$;
