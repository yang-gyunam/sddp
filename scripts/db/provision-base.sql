-- =============================================================================
-- provision-base.sql
-- SDDP Database Provisioning — Schema + Base Data (excluding test data)
-- =============================================================================
-- Single source of truth
-- `make db-provision` executes this file directly.
-- When adding a new script, add the `\ir` line only here.
-- =============================================================================

\echo '============================================='
\echo 'SDDP Database Provisioning (Base) Started'
\echo '============================================='
\echo ''

-- Stop immediately when an error occurs
\set ON_ERROR_STOP on

-- -----------------------------------------------------------------------------
-- Extensions & Prerequisites (00-09)
-- -----------------------------------------------------------------------------
\echo '[00] Installing Extensions...'

-- >>> bundled from scripts/db/00-extensions.sql >>>
-- =============================================================================
-- 00-extensions.sql
-- PostgreSQL Extensions for SDDP
-- =============================================================================

-- UUID Create (uuid-ossp)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- (pg_trgm)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- (pgvector)
CREATE EXTENSION IF NOT EXISTS vector;

-- Config (Default simple )
-- : Config

--
DO $$
BEGIN
 RAISE NOTICE 'Extensions installed: uuid-ossp, pg_trgm, vector';
END $$;

-- <<< end bundled from scripts/db/00-extensions.sql <<<

\echo '[01] Configuring Storage Tablespace + Quota...'

-- >>> bundled from scripts/db/01-storage-tablespace.sql >>>
-- =============================================================================
-- 01-storage-tablespace.sql
-- Configure dedicated storage tablespace and quota metadata
-- =============================================================================

\if :{?storage_tablespace_name}
\else
\set storage_tablespace_name sddp_data_ts
\endif

\if :{?storage_tablespace_path}
\else
\set storage_tablespace_path /var/lib/postgresql/tablespaces/sddp_data
\endif

\if :{?storage_quota_gb}
\else
\set storage_quota_gb 20
\endif

\echo 'Storage Tablespace Bootstrap'
\echo ' - tablespace:' :storage_tablespace_name
\echo ' - location: ' :storage_tablespace_path
\echo ' - quota(gb): ' :storage_quota_gb

-- CREATE TABLESPACE cannot run inside a transaction block.
SELECT format(
 'CREATE TABLESPACE %I LOCATION %L',
 :'storage_tablespace_name',
 :'storage_tablespace_path'
)
WHERE NOT EXISTS (
 SELECT 1 FROM pg_tablespace WHERE spcname = :'storage_tablespace_name'
)
\gexec

SELECT format(
 'ALTER DATABASE %I SET default_tablespace = %L',
 current_database(),
 :'storage_tablespace_name'
)
\gexec

SELECT format(
 'ALTER DATABASE %I SET sddp.storage_tablespace = %L',
 current_database(),
 :'storage_tablespace_name'
)
\gexec

SELECT format(
 'ALTER DATABASE %I SET sddp.storage_quota_gb = %L',
 current_database(),
 :'storage_quota_gb'
)
\gexec

-- Apply immediately for this provisioning session.
SET default_tablespace = :'storage_tablespace_name';
SET sddp.storage_tablespace = :'storage_tablespace_name';
SET sddp.storage_quota_gb = :'storage_quota_gb';

DO $$
BEGIN
 IF to_regclass('public.system_configs') IS NOT NULL THEN
 UPDATE system_configs
 SET
 config_value = current_setting('sddp.storage_quota_gb', true),
 updated_at = NOW()
 WHERE tenant_id IS NULL
 AND project_id IS NULL
 AND config_group = 'storage'
 AND config_key = 'storageLimit';

 UPDATE system_configs
 SET
 config_value = CONCAT(
 'PostgreSQL (Tablespace: ',
 COALESCE(NULLIF(current_setting('sddp.storage_tablespace', true), ''), 'pg_default'),
 ')'
 ),
 updated_at = NOW()
 WHERE tenant_id IS NULL
 AND project_id IS NULL
 AND config_group = 'storage'
 AND config_key = 'database';
 END IF;
END $$;

DO $$
BEGIN
 RAISE NOTICE
 'Storage tablespace ready: %, quota=% GiB',
 current_setting('sddp.storage_tablespace', true),
 current_setting('sddp.storage_quota_gb', true);
END $$;

-- <<< end bundled from scripts/db/01-storage-tablespace.sql <<<

-- Start transaction
BEGIN;

-- -----------------------------------------------------------------------------
-- Schema (10-29)
-- -----------------------------------------------------------------------------
\echo '[10] Creating Core Schema...'

-- >>> bundled from scripts/db/10-schema-core.sql >>>
-- =============================================================================
-- 10-schema-core.sql
-- Core Tables: GroupCode, Code, Person, User, Role, Permission
-- =============================================================================

-- -----------------------------------------------------------------------------
-- GroupCode: (Lookup + Config )
-- : SPEC_STATUS, MESSAGE_TYPE, REQUIREMENT_LEVEL
-- -----------------------------------------------------------------------------
CREATE TABLE group_codes (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Default
 code VARCHAR(50) NOT NULL, -- (: SPEC_STATUS)
 name VARCHAR(100) NOT NULL, -- (: "Spec Status")
 description TEXT, --

 -- Category
 category VARCHAR(50) NOT NULL DEFAULT 'LOOKUP', -- LOOKUP, CONFIG, SYSTEM

 -- Config
 is_system BOOLEAN NOT NULL DEFAULT FALSE, -- System (Update )
 is_extensible BOOLEAN NOT NULL DEFAULT TRUE, -- User
 sort_order INT NOT NULL DEFAULT 0,

 -- Metadata
 metadata JSONB, -- Config (JSON)

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_group_codes_code UNIQUE (code)
);

COMMENT ON TABLE group_codes IS 'Code group definitions (lookup + config)';
COMMENT ON COLUMN group_codes.category IS 'LOOKUP: reference data, CONFIG: config values, SYSTEM: system-only';

-- -----------------------------------------------------------------------------
-- Code:
-- : SPEC_STATUS Detail DRAFT, IN_REVIEW, APPROVED
-- -----------------------------------------------------------------------------
CREATE TABLE codes (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Reference
 group_code_id UUID NOT NULL,

 -- Default
 code VARCHAR(50) NOT NULL, -- (: DRAFT)
 name VARCHAR(100) NOT NULL, -- (: "Draft")
 description TEXT, --

 --
 name_en VARCHAR(100), --
 name_ko VARCHAR(100), --

 -- Config
 is_default BOOLEAN NOT NULL DEFAULT FALSE, -- Default
 is_system BOOLEAN NOT NULL DEFAULT FALSE, -- System (Update )
 sort_order INT NOT NULL DEFAULT 0,

 -- ( Config)
 attributes JSONB, -- : {"color": "#FF0000", "icon": "draft"}

 -- (Config )
 valid_from TIMESTAMPTZ,
 valid_to TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_codes_group FOREIGN KEY (group_code_id)
 REFERENCES group_codes(id) ON DELETE RESTRICT,
 CONSTRAINT uq_codes_group_code UNIQUE (group_code_id, code)
);

COMMENT ON TABLE codes IS 'Individual code values';
COMMENT ON COLUMN codes.attributes IS 'Per-code extra attributes (color, icon, etc.)';

-- -----------------------------------------------------------------------------
-- Person: (System User + External Relationship)
-- -----------------------------------------------------------------------------
CREATE TABLE persons (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Default
 first_name VARCHAR(50),
 last_name VARCHAR(50),
 display_name VARCHAR(100) NOT NULL, --
 email VARCHAR(255), -- (nullable for external)
 phone VARCHAR(50),

 --
 avatar_url VARCHAR(500),
 bio TEXT,

 --
 organization VARCHAR(200), --
 department VARCHAR(100), --
 title VARCHAR(100), --

 -- Category
 person_type VARCHAR(20) NOT NULL DEFAULT 'INTERNAL', -- INTERNAL, EXTERNAL, SYSTEM

 -- Config
 timezone VARCHAR(50) DEFAULT 'Asia/Seoul',
 locale VARCHAR(10) DEFAULT 'ko-KR',

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE persons IS 'Person records (internal users and external stakeholders)';
COMMENT ON COLUMN persons.person_type IS 'INTERNAL: internal, EXTERNAL: external, SYSTEM: system';

-- -----------------------------------------------------------------------------
-- User: System login accounts (Person Reference)
-- -----------------------------------------------------------------------------
CREATE TABLE users (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Person Reference
 person_id UUID NOT NULL,

 -- Log
 username VARCHAR(50) NOT NULL,
 email VARCHAR(255) NOT NULL,
 password_hash VARCHAR(255) NOT NULL,
 display_name VARCHAR(100) NOT NULL DEFAULT '', --

 --
 avatar_url VARCHAR(500),

 -- AI
 is_ai BOOLEAN NOT NULL DEFAULT FALSE,

 -- Status
 is_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
 is_locked BOOLEAN NOT NULL DEFAULT FALSE,
 locked_until TIMESTAMPTZ,
 failed_login_count INT NOT NULL DEFAULT 0,

 -- Log
 last_login_at TIMESTAMPTZ,
 last_login_ip VARCHAR(45),

 -- Security
 password_changed_at TIMESTAMPTZ,
 require_password_change BOOLEAN NOT NULL DEFAULT FALSE,

 -- 2FA ()
 two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
 two_factor_secret VARCHAR(100),

 -- Config (theme, accentColor, autoSave )
 preferences JSONB DEFAULT NULL,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_users_person FOREIGN KEY (person_id)
 REFERENCES persons(id) ON DELETE RESTRICT,
 CONSTRAINT uq_users_username UNIQUE (username),
 CONSTRAINT uq_users_email UNIQUE (email),
 CONSTRAINT uq_users_person UNIQUE (person_id)
);

COMMENT ON TABLE users IS 'System login accounts';
COMMENT ON COLUMN users.is_ai IS 'Whether the account is an AI agent';

-- -----------------------------------------------------------------------------
-- Role: Role
-- -----------------------------------------------------------------------------
CREATE TABLE roles (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Default
 code VARCHAR(50) NOT NULL, -- Role (: ADMIN)
 name VARCHAR(100) NOT NULL, -- (: "System Manage")
 description TEXT,
 type INT NOT NULL DEFAULT 0, -- RoleType enum (0=PO, 1=DomainExpert, 2=Developer, 3=Reviewer, 4=QA, 5=SysOp, 6=AI)

 -- Category
 role_type VARCHAR(20) NOT NULL DEFAULT 'PROJECT', -- SYSTEM, PROJECT

 -- Config
 is_system BOOLEAN NOT NULL DEFAULT FALSE, -- System Role (Delete )
 is_default BOOLEAN NOT NULL DEFAULT FALSE, -- User Default Role
 sort_order INT NOT NULL DEFAULT 0,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_roles_code UNIQUE (code)
);

COMMENT ON TABLE roles IS 'Role definitions';
COMMENT ON COLUMN roles.role_type IS 'SYSTEM: global, PROJECT: per-project';

-- -----------------------------------------------------------------------------
-- Permission: Permission
-- -----------------------------------------------------------------------------
CREATE TABLE permissions (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Default
 code VARCHAR(100) NOT NULL, -- Permission (: spec:create)
 name VARCHAR(100) NOT NULL, --
 description TEXT,

 -- Category
 resource_type VARCHAR(50) NOT NULL, -- (: spec, conversation)
 action VARCHAR(50) NOT NULL, -- (: create, read, update, delete)

 -- Config
 is_system BOOLEAN NOT NULL DEFAULT FALSE,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_permissions_code UNIQUE (code),
 CONSTRAINT uq_permissions_resource_action UNIQUE (resource_type, action)
);

COMMENT ON TABLE permissions IS 'Permission definitions';

-- -----------------------------------------------------------------------------
-- UserRole: User-role mapping
-- -----------------------------------------------------------------------------
CREATE TABLE user_roles (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 user_id UUID NOT NULL,
 role_id UUID NOT NULL,

 -- Scope (Project Role )
 tenant_id UUID,
 project_id UUID,

 -- Audit
 assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 assigned_by UUID,

 --
 valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 valid_to TIMESTAMPTZ,

 is_active BOOLEAN NOT NULL DEFAULT TRUE,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 --
 CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE CASCADE,
 CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id)
 REFERENCES roles(id) ON DELETE CASCADE,
 CONSTRAINT uq_user_roles UNIQUE (user_id, role_id, tenant_id, project_id)
);

-- NULL-safe unique index: prevents duplicate (user, role) when tenant/project are NULL
-- PostgreSQL UNIQUE treats NULLs as distinct, so this COALESCE-based index fills the gap
CREATE UNIQUE INDEX uq_user_roles_null_safe
 ON user_roles (user_id, role_id, COALESCE(tenant_id, '00000000-0000-0000-0000-000000000000'), COALESCE(project_id, '00000000-0000-0000-0000-000000000000'));

COMMENT ON TABLE user_roles IS 'User-role mapping';

-- -----------------------------------------------------------------------------
-- RolePermission: Role-Permission
-- -----------------------------------------------------------------------------
CREATE TABLE role_permissions (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 role_id UUID NOT NULL,
 permission_id UUID NOT NULL,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id)
 REFERENCES roles(id) ON DELETE CASCADE,
 CONSTRAINT fk_role_permissions_permission FOREIGN KEY (permission_id)
 REFERENCES permissions(id) ON DELETE CASCADE,
 CONSTRAINT uq_role_permissions UNIQUE (role_id, permission_id)
);

COMMENT ON TABLE role_permissions IS 'Role-permission mappings';

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON COLUMN users.preferences IS 'JSONB: {theme, accentColor, autoSave, fontSize, locale, ...}';
COMMENT ON COLUMN codes.attributes IS 'JSONB: code-specific key-value pairs (e.g., color for status codes)';
COMMENT ON COLUMN roles.type IS 'RoleType enum: 0=ProductOwner, 1=DomainExpert, 2=Developer, 3=Reviewer, 4=QATester, 5=Admin';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_group_codes_category ON group_codes(category);
CREATE INDEX idx_group_codes_code ON group_codes(code);

CREATE INDEX idx_codes_group_code_id ON codes(group_code_id);
CREATE INDEX idx_codes_code ON codes(code);

CREATE INDEX idx_persons_email ON persons(email);
CREATE INDEX idx_persons_display_name ON persons(display_name);
CREATE INDEX idx_persons_person_type ON persons(person_type);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_person_id ON users(person_id);

CREATE INDEX idx_roles_code ON roles(code);
CREATE INDEX idx_roles_role_type ON roles(role_type);

CREATE INDEX idx_permissions_resource_type ON permissions(resource_type);

CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX idx_user_roles_project ON user_roles(tenant_id, project_id);

-- -----------------------------------------------------------------------------
-- Deferred FK: created_by / updated_by / assigned_by → users(id)
-- (group_codes, codes are created before users table)
-- -----------------------------------------------------------------------------
DO $$ BEGIN
 ALTER TABLE group_codes ADD CONSTRAINT fk_group_codes_created_by
 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
 ALTER TABLE group_codes ADD CONSTRAINT fk_group_codes_updated_by
 FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
 ALTER TABLE codes ADD CONSTRAINT fk_codes_created_by
 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
 ALTER TABLE codes ADD CONSTRAINT fk_codes_updated_by
 FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
 ALTER TABLE user_roles ADD CONSTRAINT fk_user_roles_assigned_by
 FOREIGN KEY (assigned_by) REFERENCES users(id) ON DELETE RESTRICT;
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Core schema created: group_codes, codes, persons, users, roles, permissions, user_roles, role_permissions';
END $$;

-- <<< end bundled from scripts/db/10-schema-core.sql <<<

\echo '[11] Creating Spec Schema...'

-- >>> bundled from scripts/db/11-schema-spec.sql >>>
-- =============================================================================
-- 11-schema-spec.sql
-- Spec Table: Spec, SignOff, Alternative
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Spec: Design Spec ( Manage)
-- -----------------------------------------------------------------------------
CREATE TABLE specs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 --
 code VARCHAR(50) NOT NULL, -- Spec (: SPEC-001)
 title VARCHAR(200) NOT NULL,

 --
 description TEXT, --
 decision TEXT, -- Decision
 context TEXT, -- /

 -- Scope
 scope TEXT, -- Scope
 out_of_scope TEXT, -- Scope

 -- (EF Core: text )
 definitions TEXT, --
 acceptance_criteria TEXT, --

 -- /
 owners TEXT NOT NULL DEFAULT '', -- (CSV)
 review_trigger TEXT, -- View Trigger

 -- Status (codes Table FK)
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000001000001', -- SPEC_STATUS/DRAFT

 --
 requirement_id UUID, -- Requirement
 born_from_conversation_id UUID, -- Create Conversation (Channel/Topic)
 supersedes_spec_id UUID, -- Spec ( )
 locked_at TIMESTAMPTZ, -- Lock

 -- Manage (SCD Type 6)
 version VARCHAR(50) NOT NULL DEFAULT '1.0.0', -- SemanticVersion (: 1.0.0)
 valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 valid_to TIMESTAMPTZ, -- NULL =

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_specs_tenant_project_code_version UNIQUE (tenant_id, project_id, code, version),
 CONSTRAINT fk_specs_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_specs_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_specs_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE specs IS 'Design specs (versioned)';
COMMENT ON COLUMN specs.valid_to IS 'NULL means current version (IsCurrent)';

-- -----------------------------------------------------------------------------
-- SignOff: Approve
-- -----------------------------------------------------------------------------
CREATE TABLE sign_offs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Spec Reference
 spec_id UUID NOT NULL,

 -- Relationship(Decision)
 stakeholder_id UUID NOT NULL,
 role INT NOT NULL, -- RoleType enum (0=PO, 1=DomainExpert, 2=Developer, 3=Reviewer, 4=QA, 5=SysOp, 6=AI)

 -- Decision (codes Table FK)
 decision_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000015000001', -- SIGNOFF_DECISION/PENDING
 conditions TEXT, -- Conditional Approval
 comments TEXT, --

 --
 requested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- SLA
 signed_at TIMESTAMPTZ, -- (NULL = Pending)

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_sign_offs_spec FOREIGN KEY (spec_id)
 REFERENCES specs(id) ON DELETE CASCADE,
 CONSTRAINT fk_sign_offs_stakeholder FOREIGN KEY (stakeholder_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_sign_offs_decision_code FOREIGN KEY (decision_code_id) REFERENCES codes(id)
);

COMMENT ON TABLE sign_offs IS 'Spec approval records';

-- -----------------------------------------------------------------------------
-- Alternative: (Rejected Design )
-- -----------------------------------------------------------------------------
CREATE TABLE alternatives (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Spec Reference
 spec_id UUID NOT NULL,

 -- Suggestion
 title VARCHAR(200) NOT NULL,
 description TEXT NOT NULL,
 proposed_by UUID NOT NULL,
 proposed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 "order" INT NOT NULL DEFAULT 0,

 -- Rejected
 rejected_reason TEXT,
 rejected_by UUID,
 rejected_at TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_alternatives_spec FOREIGN KEY (spec_id)
 REFERENCES specs(id) ON DELETE CASCADE,
 CONSTRAINT fk_alternatives_proposed_by FOREIGN KEY (proposed_by)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_alternatives_rejected_by FOREIGN KEY (rejected_by)
 REFERENCES users(id) ON DELETE SET NULL,
 CONSTRAINT fk_alternatives_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_alternatives_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE alternatives IS 'Rejected design alternatives';

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON COLUMN sign_offs.role IS 'RoleType enum: 0=ProductOwner, 1=DomainExpert, 2=Developer, 3=Reviewer, 4=QATester, 5=Admin';
COMMENT ON COLUMN specs.definitions IS 'Newline-delimited list of term definitions';
COMMENT ON COLUMN specs.acceptance_criteria IS 'Newline-delimited list of acceptance criteria';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_specs_tenant_project ON specs(tenant_id, project_id);
CREATE INDEX idx_specs_code ON specs(code);
CREATE INDEX idx_specs_status_code_id ON specs(status_code_id);
CREATE INDEX idx_specs_valid_to ON specs(valid_to) WHERE valid_to IS NULL;

-- GIN Index ( )
CREATE INDEX idx_specs_title_gin ON specs USING GIN (title gin_trgm_ops);
CREATE INDEX idx_specs_description_gin ON specs USING GIN (description gin_trgm_ops);

CREATE INDEX idx_sign_offs_spec_id ON sign_offs(spec_id);
CREATE INDEX idx_sign_offs_stakeholder_id ON sign_offs(stakeholder_id);

CREATE INDEX idx_alternatives_spec_id ON alternatives(spec_id);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Spec schema created: specs, sign_offs, alternatives';
END $$;

-- <<< end bundled from scripts/db/11-schema-spec.sql <<<

\echo '[12] Creating Requirement Schema...'

-- >>> bundled from scripts/db/12-schema-requirement.sql >>>
-- =============================================================================
-- 12-schema-requirement.sql
-- Requirement Table ( Requirement)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Requirement: Requirement ( Manage, )
-- -----------------------------------------------------------------------------
CREATE TABLE requirements (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 --
 code VARCHAR(50) NOT NULL, -- Requirement (: REQ-A-001)
 title VARCHAR(200) NOT NULL,
 description TEXT,

 -- (codes Table FK)
 level_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000004000001', -- REQUIREMENT_LEVEL/A
 parent_id UUID, -- Top Requirement Reference

 -- Status (codes Table FK)
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000003000001', -- REQUIREMENT_STATUS/DRAFT

 -- Priority Category
 priority VARCHAR(10) DEFAULT 'MEDIUM', -- LOW, MEDIUM, HIGH, URGENT
 category VARCHAR(50), -- Category (, , )

 --
 source TEXT, -- (, )
 rationale TEXT, --

 --
 owner_user_id UUID, -- Requirement (nullable)

 -- Conversation
 conversation_id UUID, -- Conversation (Channel/Topic)

 -- Manage (SCD Type 6)
 version VARCHAR(50) NOT NULL DEFAULT '1.0.0', -- SemanticVersion (: 1.0.0)
 valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 valid_to TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_requirements_parent FOREIGN KEY (parent_id)
 REFERENCES requirements(id) ON DELETE SET NULL,
 CONSTRAINT fk_requirements_level_code FOREIGN KEY (level_code_id) REFERENCES codes(id),
 CONSTRAINT fk_requirements_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_requirements_owner FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL,
 CONSTRAINT fk_requirements_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_requirements_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_requirements_tenant_project_code_version UNIQUE (tenant_id, project_id, code, version)
);

COMMENT ON TABLE requirements IS 'Requirements (hierarchical and versioned)';
COMMENT ON COLUMN requirements.level_code_id IS 'A=Top level (business), B=Middle level (feature), C=Detail level — codes table FK';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_requirements_tenant_project ON requirements(tenant_id, project_id);
CREATE INDEX idx_requirements_code ON requirements(code);
CREATE INDEX idx_requirements_parent_id ON requirements(parent_id);
CREATE INDEX idx_requirements_level_code_id ON requirements(level_code_id);
CREATE INDEX idx_requirements_status_code_id ON requirements(status_code_id);
CREATE INDEX idx_requirements_owner ON requirements(owner_user_id);
CREATE INDEX idx_requirements_valid_to ON requirements(valid_to) WHERE valid_to IS NULL;

-- GIN Index ( )
CREATE INDEX idx_requirements_title_gin ON requirements USING GIN (title gin_trgm_ops);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Requirement schema created: requirements';
END $$;

-- <<< end bundled from scripts/db/12-schema-requirement.sql <<<

\echo '[13] Creating Conversation Schema...'

-- >>> bundled from scripts/db/13-schema-conversation.sql >>>
-- =============================================================================
-- 13-schema-conversation.sql
-- Conversation System: TPT (Table-Per-Type)
-- conversations (base) + channels / forums / direct_messages
-- conversation_members, topics, messages
-- =============================================================================

-- -----------------------------------------------------------------------------
-- conversations: TPT Base Table
-- -----------------------------------------------------------------------------
CREATE TABLE conversations (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID, -- NULL = tenant-wide (Global) Channel

 -- Default
 name VARCHAR(100) NOT NULL,
 description TEXT,
 conversation_type VARCHAR(20) NOT NULL DEFAULT 'Channel', -- Channel | Forum | DirectMessage
 visibility VARCHAR(20) NOT NULL DEFAULT 'Public', -- Public | Private
 scope VARCHAR(20) NOT NULL, -- TenantWide | ProjectScoped

 -- Config
 is_archived BOOLEAN NOT NULL DEFAULT FALSE,
 is_default BOOLEAN NOT NULL DEFAULT FALSE,
 sort_order INT NOT NULL DEFAULT 0,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_conversations_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_conversations_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE conversations IS 'Conversation base table (TPT). Shared columns for Channel/Forum/DirectMessage';

-- -----------------------------------------------------------------------------
-- TPT Table: channels, forums, direct_messages
-- PK = FK → conversations(id)
-- -----------------------------------------------------------------------------
CREATE TABLE channels (
 id UUID PRIMARY KEY REFERENCES conversations(id) ON DELETE CASCADE,
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000021000001',
 decision_spec_id UUID,
 CONSTRAINT fk_channels_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_channels_decision_spec FOREIGN KEY (decision_spec_id) REFERENCES specs(id)
);
CREATE INDEX idx_channels_status ON channels(status_code_id);
COMMENT ON TABLE channels IS 'Channel derived table (TPT). Realtime chat and decision discussions';

CREATE TABLE forums (
 id UUID PRIMARY KEY REFERENCES conversations(id) ON DELETE CASCADE
);
COMMENT ON TABLE forums IS 'Forum derived table (TPT). Discussion board';

CREATE TABLE direct_messages (
 id UUID PRIMARY KEY REFERENCES conversations(id) ON DELETE CASCADE,
 status VARCHAR(20) NOT NULL DEFAULT 'Active',
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000022000001',
 decision_spec_id UUID,
 CONSTRAINT fk_direct_messages_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_direct_messages_decision_spec FOREIGN KEY (decision_spec_id) REFERENCES specs(id)
);
COMMENT ON TABLE direct_messages IS 'DirectMessage derived table (TPT). One-to-one conversation';

-- -----------------------------------------------------------------------------
-- conversation_members: Conversation
-- -----------------------------------------------------------------------------
CREATE TABLE conversation_members (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Reference
 conversation_id UUID NOT NULL,
 user_id UUID NOT NULL,

 -- Role
 role VARCHAR(20) NOT NULL DEFAULT 'MEMBER', -- MEMBER, MODERATOR, OWNER
 member_type VARCHAR(10) NOT NULL DEFAULT 'HUMAN', -- HUMAN, AI

 -- Config
 muted_until TIMESTAMPTZ,
 notifications_enabled BOOLEAN NOT NULL DEFAULT TRUE,

 -- Audit
 joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_conversation_members_conversation FOREIGN KEY (conversation_id)
 REFERENCES conversations(id) ON DELETE CASCADE,
 CONSTRAINT fk_conversation_members_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_conversation_members UNIQUE (conversation_id, user_id)
);

COMMENT ON TABLE conversation_members IS 'Conversation membership';

-- -----------------------------------------------------------------------------
-- topics: Forum
-- -----------------------------------------------------------------------------
CREATE TABLE topics (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- Forum Reference
 forum_id UUID NOT NULL,

 -- Default
 title VARCHAR(200) NOT NULL,
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000020000001', -- TOPIC_STATUS/OPEN

 --
 author_id UUID NOT NULL,

 -- Config
 is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
 is_locked BOOLEAN NOT NULL DEFAULT FALSE,

 -- Spec (Decision )
 decision_spec_id UUID,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_topics_forum FOREIGN KEY (forum_id)
 REFERENCES forums(id) ON DELETE CASCADE,
 CONSTRAINT fk_topics_author FOREIGN KEY (author_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_topics_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id)
);

COMMENT ON TABLE topics IS 'Forum topics (Conversation > Forum > Topic)';

-- -----------------------------------------------------------------------------
-- messages: Message (Conversation Topic )
-- -----------------------------------------------------------------------------
CREATE TABLE messages (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID, -- NULL = tenant-wide conversation Message

 -- ( NOT NULL)
 conversation_id UUID, -- Channel/DirectMessage Message
 topic_id UUID, -- Forum Message

 --
 sender_id UUID NOT NULL,

 --
 content TEXT NOT NULL,
 type_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000007000009', -- MESSAGE_TYPE/NORMAL

 -- Reference
 "references" JSONB DEFAULT '[]'::jsonb, -- Entity
 reply_to_id UUID, --

 -- Config
 is_edited BOOLEAN NOT NULL DEFAULT FALSE,
 is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
 edited_at TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_messages_conversation FOREIGN KEY (conversation_id)
 REFERENCES conversations(id) ON DELETE CASCADE,
 CONSTRAINT fk_messages_topic FOREIGN KEY (topic_id)
 REFERENCES topics(id) ON DELETE CASCADE,
 CONSTRAINT fk_messages_sender FOREIGN KEY (sender_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_messages_reply_to FOREIGN KEY (reply_to_id)
 REFERENCES messages(id) ON DELETE SET NULL,
 CONSTRAINT fk_messages_type_code FOREIGN KEY (type_code_id) REFERENCES codes(id),
 CONSTRAINT chk_messages_context CHECK (
 (conversation_id IS NOT NULL)::INT +
 (topic_id IS NOT NULL)::INT = 1
 )
);

COMMENT ON TABLE messages IS 'Messages shared by conversations and topics';

-- -----------------------------------------------------------------------------
-- user_read_statuses: User Status
-- -----------------------------------------------------------------------------
CREATE TABLE user_read_statuses (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,

 -- Reference
 user_id UUID NOT NULL,
 conversation_id UUID NOT NULL, -- Conversation Topic ID

 -- Status
 last_read_at TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_user_read_statuses_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_user_read_statuses UNIQUE (user_id, conversation_id)
);

COMMENT ON TABLE user_read_statuses IS 'Per-user conversation/topic read state';
COMMENT ON COLUMN user_read_statuses.conversation_id IS 'Polymorphic FK: references conversations(id) — no DB-level FK to allow Topic ID references';

-- -----------------------------------------------------------------------------
-- user_conversation_settings: User Conversation Config
-- -----------------------------------------------------------------------------
CREATE TABLE user_conversation_settings (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,

 -- Reference
 user_id UUID NOT NULL,
 conversation_id UUID NOT NULL,

 -- Config
 is_starred BOOLEAN NOT NULL DEFAULT FALSE,
 is_muted BOOLEAN NOT NULL DEFAULT FALSE,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_user_conversation_settings_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_user_conversation_settings UNIQUE (user_id, conversation_id)
);

COMMENT ON TABLE user_conversation_settings IS 'Per-user conversation settings (starred, muted)';
COMMENT ON COLUMN user_conversation_settings.conversation_id IS 'Polymorphic FK: references conversations(id) — no DB-level FK to allow flexible scoping';

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON COLUMN messages."references" IS 'JSONB array: [{type, id, label}] — referenced entities (spec, requirement, glossary)';
COMMENT ON COLUMN channels.decision_spec_id IS 'Spec created from channel decision (Channel → Spec outcome)';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_conversations_tenant_project ON conversations(tenant_id, project_id);
CREATE INDEX idx_conversations_tenant_global ON conversations(tenant_id) WHERE project_id IS NULL;
CREATE INDEX idx_conversations_taxonomy_scope_type ON conversations(tenant_id, scope, conversation_type);

CREATE INDEX idx_topics_forum_id ON topics(forum_id);
CREATE INDEX idx_topics_author_id ON topics(author_id);
CREATE INDEX idx_direct_messages_status_code_id ON direct_messages(status_code_id);

CREATE INDEX idx_messages_conversation_id ON messages(conversation_id);
CREATE INDEX idx_messages_topic_id ON messages(topic_id);
CREATE INDEX idx_messages_sender_id ON messages(sender_id);
CREATE INDEX idx_messages_created_at ON messages(created_at);

CREATE INDEX idx_conversation_members_conversation_id ON conversation_members(conversation_id);
CREATE INDEX idx_conversation_members_user_id ON conversation_members(user_id);

CREATE INDEX idx_user_read_statuses_user_id ON user_read_statuses(user_id);
CREATE INDEX idx_user_read_statuses_conversation_id ON user_read_statuses(conversation_id);

CREATE INDEX idx_user_conversation_settings_user_id ON user_conversation_settings(user_id);
CREATE INDEX idx_user_conversation_settings_conversation_id ON user_conversation_settings(conversation_id);
CREATE INDEX idx_user_conversation_settings_is_starred ON user_conversation_settings(is_starred);

-- GIN Index ( )
CREATE INDEX idx_messages_content_gin ON messages USING GIN (content gin_trgm_ops);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Conversation schema created: conversations (TPT base), channels, forums, direct_messages, topics, messages, conversation_members, user_read_statuses, user_conversation_settings';
END $$;

-- <<< end bundled from scripts/db/13-schema-conversation.sql <<<

\echo '[14] Creating Glossary Schema...'

-- >>> bundled from scripts/db/14-schema-glossary.sql >>>
-- =============================================================================
-- 14-schema-glossary.sql
-- GlossaryTerm Table (Glossary )
-- =============================================================================

-- -----------------------------------------------------------------------------
-- GlossaryTerm: Glossary ( Manage)
-- -----------------------------------------------------------------------------
CREATE TABLE glossary_terms (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Default
 term VARCHAR(200) NOT NULL, -- Glossary
 definition TEXT NOT NULL, -- (Markdown)

 -- Category (codes Table FK)
 category_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000010000001', -- GLOSSARY_CATEGORY/TECHNICAL

 --
 usage_examples JSONB, --
 related_term_ids JSONB, -- Glossary ID
 source VARCHAR(500), -- ( Document, URL)
 synonyms VARCHAR(500), -- ( )
 abbreviation VARCHAR(50), -- Abbreviation

 --
 defined_by UUID NOT NULL, -- Glossary User

 -- Status (codes Table FK)
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000009000001', -- GLOSSARY_STATUS/DRAFT

 --
 owner_user_id UUID, -- Glossary Manage

 -- Approve
 approved_by UUID,
 approved_at TIMESTAMPTZ,

 -- Reference (Source References)
 source_spec_id UUID, -- Glossary Spec
 source_conversation_id UUID, -- Glossary Conversation
 source_requirement_id UUID, -- Glossary Requirement

 -- Glossary (Deprecated )
 replaced_by_term_id UUID, -- Glossary

 -- Manage (SCD Type 6)
 version VARCHAR(50) NOT NULL DEFAULT '1.0.0', -- SemanticVersion (: 1.0.0)
 valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 valid_to TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_glossary_terms_category_code FOREIGN KEY (category_code_id) REFERENCES codes(id),
 CONSTRAINT fk_glossary_terms_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_glossary_terms_defined_by FOREIGN KEY (defined_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_glossary_terms_approved_by FOREIGN KEY (approved_by) REFERENCES users(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_replaced_by_term FOREIGN KEY (replaced_by_term_id) REFERENCES glossary_terms(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_source_spec FOREIGN KEY (source_spec_id) REFERENCES specs(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_source_conversation FOREIGN KEY (source_conversation_id) REFERENCES conversations(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_source_requirement FOREIGN KEY (source_requirement_id) REFERENCES requirements(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_owner_user FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL,
 CONSTRAINT fk_glossary_terms_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_glossary_terms_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_glossary_terms_tenant_project_term_version UNIQUE (tenant_id, project_id, term, version),
 CONSTRAINT chk_glossary_terms_no_self_replace CHECK (replaced_by_term_id IS NULL OR replaced_by_term_id != id)
);

COMMENT ON TABLE glossary_terms IS 'Glossary terms (versioned)';
COMMENT ON COLUMN glossary_terms.usage_examples IS 'JSONB array: ["example sentence", ...] — glossary usage examples';
COMMENT ON COLUMN glossary_terms.related_term_ids IS 'JSONB array: ["uuid", ...] — related glossary term IDs (glossary_terms.id)';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_glossary_terms_tenant_project ON glossary_terms(tenant_id, project_id);
CREATE INDEX idx_glossary_terms_term ON glossary_terms(term);
CREATE INDEX idx_glossary_terms_category_code_id ON glossary_terms(category_code_id);
CREATE INDEX idx_glossary_terms_status_code_id ON glossary_terms(status_code_id);
CREATE INDEX idx_glossary_terms_valid_to ON glossary_terms(valid_to) WHERE valid_to IS NULL;
CREATE INDEX idx_glossary_terms_source_spec_id ON glossary_terms(source_spec_id);
CREATE INDEX idx_glossary_terms_source_conversation_id ON glossary_terms(source_conversation_id);
CREATE INDEX idx_glossary_terms_source_requirement_id ON glossary_terms(source_requirement_id);
CREATE INDEX idx_glossary_terms_owner_user_id ON glossary_terms(owner_user_id);

-- GIN Index ( )
CREATE INDEX idx_glossary_terms_term_gin ON glossary_terms USING GIN (term gin_trgm_ops);
CREATE INDEX idx_glossary_terms_definition_gin ON glossary_terms USING GIN (definition gin_trgm_ops);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Glossary schema created: glossary_terms';
END $$;

-- <<< end bundled from scripts/db/14-schema-glossary.sql <<<

\echo '[15] Creating Relationship Schema...'

-- >>> bundled from scripts/db/15-schema-relationship.sql >>>
-- =============================================================================
-- 15-schema-relationship.sql
-- Relationship Table (Entity Relationship)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Relationship: Entity relationships (temporal versioning)
-- -----------------------------------------------------------------------------
CREATE TABLE relationships (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Source Entity
 from_entity_type INT NOT NULL, -- EntityType enum: 0=Spec, 1=Requirement, 2=Topic, 3=GlossaryTerm, 4=Conversation, 5=Artifact, 6=Task
 from_entity_id UUID NOT NULL,

 -- Target Entity
 to_entity_type INT NOT NULL, -- EntityType enum: 0=Spec, 1=Requirement, 2=Topic, 3=GlossaryTerm, 4=Conversation, 5=Artifact, 6=Task
 to_entity_id UUID NOT NULL,

 -- Relationship Type (codes Table FK)
 type_code_id UUID NOT NULL, -- RELATIONSHIP_TYPE codes FK

 -- /
 reason TEXT,

 -- ( Relationship )
 source_spec_id UUID, -- Relationship Spec
 source_decision_id UUID, -- Relationship Conversation/Decision

 -- Manage (SCD Type 6)
 valid_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 valid_to TIMESTAMPTZ, -- NULL = (is_current )

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 CONSTRAINT fk_relationships_type_code FOREIGN KEY (type_code_id) REFERENCES codes(id),
 CONSTRAINT fk_relationships_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE relationships IS 'Entity relationships (temporal versioning)';
COMMENT ON COLUMN relationships.from_entity_type IS 'EntityType enum: 0=Spec, 1=Requirement, 2=Topic, 3=GlossaryTerm, 4=Conversation, 5=Artifact, 6=Task';
COMMENT ON COLUMN relationships.to_entity_type IS 'EntityType enum: 0=Spec, 1=Requirement, 2=Topic, 3=GlossaryTerm, 4=Conversation, 5=Artifact, 6=Task';
COMMENT ON COLUMN relationships.type_code_id IS 'RELATIONSHIP_TYPE codes table FK — supersedes, evolves_from, extends, conflicts_with, depends_on, implements, replaces, affects';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_relationships_tenant_project ON relationships(tenant_id, project_id);
CREATE INDEX idx_relationships_from_entity ON relationships(from_entity_type, from_entity_id);
CREATE INDEX idx_relationships_to_entity ON relationships(to_entity_type, to_entity_id);
CREATE INDEX idx_relationships_type_code_id ON relationships(type_code_id);
CREATE INDEX idx_relationships_current ON relationships(from_entity_id, to_entity_id) WHERE valid_to IS NULL;

-- Active relationships: prevent duplicate (from, to, type) while valid
CREATE UNIQUE INDEX uq_relationships_active
 ON relationships (from_entity_id, to_entity_id, type_code_id) WHERE valid_to IS NULL;

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Relationship schema created: relationships';
END $$;

-- <<< end bundled from scripts/db/15-schema-relationship.sql <<<

\echo '[20] Creating Project Schema...'

-- >>> bundled from scripts/db/20-schema-project.sql >>>
-- =============================================================================
-- 20-schema-project.sql
-- Project Tables: projects, project_members
-- =============================================================================

-- NOTE: tenant_id has no FK — tenants table not yet implemented.
-- Tenant isolation is enforced at application layer (TenantContextMiddleware).
-- See docs/deploy/database.md "Tenant " for details.

-- -----------------------------------------------------------------------------
-- Projects
-- -----------------------------------------------------------------------------
CREATE TABLE projects (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 tenant_id UUID NOT NULL,
 code VARCHAR(50) NOT NULL,
 name VARCHAR(255) NOT NULL,
 description VARCHAR(2000),
 status VARCHAR(50) NOT NULL DEFAULT 'planning',
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000019000001', -- PROJECT_STATUS/PLANNING
 owner_id UUID,

 -- Git/Artifact metadata
 repo_url VARCHAR(2048) NOT NULL,
 repo_branch VARCHAR(255) NOT NULL DEFAULT 'main',
 artifact_root_path VARCHAR(1024) NOT NULL,
 sync_interval_minutes INT NOT NULL DEFAULT 60,
 last_synced_at TIMESTAMPTZ,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_projects_tenant_code UNIQUE (tenant_id, code),
 CONSTRAINT fk_projects_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id)
);

COMMENT ON TABLE projects IS 'Project Metadata';

-- -----------------------------------------------------------------------------
-- Project Members
-- -----------------------------------------------------------------------------
CREATE TABLE project_members (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 project_id UUID NOT NULL,
 user_id UUID NOT NULL,
 user_role_id UUID NOT NULL,
 joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_project_members_project FOREIGN KEY (project_id)
 REFERENCES projects(id) ON DELETE CASCADE,
 CONSTRAINT fk_project_members_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE CASCADE,
 CONSTRAINT fk_project_members_user_role FOREIGN KEY (user_role_id)
 REFERENCES user_roles(id) ON DELETE CASCADE,
 CONSTRAINT uq_project_members_user_role UNIQUE (user_role_id)
);

COMMENT ON TABLE project_members IS 'Project membership';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_projects_tenant_name ON projects(tenant_id, name);
CREATE INDEX idx_projects_is_active ON projects(is_active);
CREATE INDEX idx_projects_status_code_id ON projects(status_code_id);

CREATE INDEX idx_project_members_project_user ON project_members(project_id, user_id);
CREATE UNIQUE INDEX uq_project_members_project_user_active
 ON project_members(project_id, user_id)
 WHERE is_active = TRUE;
CREATE INDEX idx_project_members_user ON project_members(user_id);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Project schema created: projects, project_members';
END $$;

-- <<< end bundled from scripts/db/20-schema-project.sql <<<

\echo '[16] Creating Metadata Schema...'

-- >>> bundled from scripts/db/16-schema-metadata.sql >>>
-- =============================================================================
-- 16-schema-metadata.sql
-- Create Metadata: EntityMetadata, FieldMetadata, EntityRelationshipMetadata
-- =============================================================================

-- -----------------------------------------------------------------------------
-- EntityMetadata: Entity metadata (for code generation)
-- -----------------------------------------------------------------------------
CREATE TABLE entity_metadata (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Spec ( Create )
 spec_id UUID,

 --
 code VARCHAR(100) NOT NULL, -- Entity (: EM_USER)

 -- Entity
 entity_name VARCHAR(100) NOT NULL, -- (: User)
 table_name VARCHAR(100) NOT NULL, -- Table (: users)
 base_class VARCHAR(100), -- Default (: EntityBase)
 namespace VARCHAR(200), --

 -- Config
 is_generated BOOLEAN NOT NULL DEFAULT TRUE,
 is_auditable BOOLEAN NOT NULL DEFAULT FALSE,
 is_versioned BOOLEAN NOT NULL DEFAULT FALSE,

 --
 description TEXT,
 display_name VARCHAR(100),

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_entity_metadata_tenant_project_code UNIQUE (tenant_id, project_id, code),
 CONSTRAINT fk_entity_metadata_spec FOREIGN KEY (spec_id)
 REFERENCES specs(id) ON DELETE SET NULL,
 CONSTRAINT fk_entity_metadata_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_entity_metadata_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE entity_metadata IS 'Entity metadata (for code generation)';
COMMENT ON COLUMN entity_metadata.spec_id IS 'Linked spec ID (code generation source)';

-- -----------------------------------------------------------------------------
-- FieldMetadata: Field Metadata
-- -----------------------------------------------------------------------------
CREATE TABLE field_metadata (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- EntityMetadata Reference
 entity_metadata_id UUID NOT NULL,

 --
 code VARCHAR(100) NOT NULL, -- Field (: FM_USER_NAME)

 -- Field
 field_name VARCHAR(100) NOT NULL, -- (: Name)
 column_name VARCHAR(100) NOT NULL, -- (: name)
 field_type VARCHAR(50) NOT NULL, -- (String, Int, DateTime, Guid )

 --
 is_required BOOLEAN NOT NULL DEFAULT FALSE,
 is_unique BOOLEAN NOT NULL DEFAULT FALSE,
 max_length INT,
 min_length INT,
 validation_type VARCHAR(50), -- email, phone, url, regex
 pattern VARCHAR(500), -- (validation_type=regex )

 -- Default
 default_value TEXT,

 --
 display_order INT NOT NULL DEFAULT 0,
 display_name VARCHAR(100),
 description TEXT,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_field_metadata_entity FOREIGN KEY (entity_metadata_id)
 REFERENCES entity_metadata(id) ON DELETE CASCADE,
 CONSTRAINT uq_field_metadata_entity_code UNIQUE (entity_metadata_id, code)
);

COMMENT ON TABLE field_metadata IS 'Field Metadata';

-- -----------------------------------------------------------------------------
-- EntityRelationshipMetadata: Entity relationship metadata
-- -----------------------------------------------------------------------------
CREATE TABLE entity_relationship_metadata (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- EntityMetadata Reference
 entity_metadata_id UUID NOT NULL,

 -- Relationship
 related_entity_code VARCHAR(100) NOT NULL, -- Entity
 relationship_type VARCHAR(20) NOT NULL, -- ManyToOne, OneToMany, ManyToMany

 --
 foreign_key_name VARCHAR(100), -- FK
 foreign_key_column VARCHAR(100), -- FK

 --
 navigation_name VARCHAR(100), --
 inverse_navigation_name VARCHAR(100), --

 -- Config
 is_required BOOLEAN NOT NULL DEFAULT FALSE,
 on_delete VARCHAR(20) DEFAULT 'Restrict', -- Cascade, Restrict, SetNull, NoAction

 --
 display_order INT NOT NULL DEFAULT 0,
 description TEXT,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_entity_relationship_metadata_entity FOREIGN KEY (entity_metadata_id)
 REFERENCES entity_metadata(id) ON DELETE CASCADE
);

COMMENT ON TABLE entity_relationship_metadata IS 'Entity relationship metadata';

-- -----------------------------------------------------------------------------
-- ArtifactTracking: (Create )
-- -----------------------------------------------------------------------------
CREATE TABLE artifact_trackings (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Spec Reference
 spec_id UUID,

 --
 artifact_path VARCHAR(500) NOT NULL, --
 artifact_type VARCHAR(50) NOT NULL, -- Code Type (Entity, DTO, Table, View )
 artifact_category VARCHAR(20) NOT NULL DEFAULT 'Code', -- Code, Database, Other

 -- /
 content_hash VARCHAR(64) NOT NULL, -- SHA-256
 generator_version VARCHAR(50) NOT NULL,
 template_version VARCHAR(50),

 -- DB (artifact_category = 'Database' )
 db_object_name VARCHAR(100), -- Table, View
 db_schema VARCHAR(50) DEFAULT 'public',
 depends_on JSONB, -- DB

 -- Entity
 entity_name VARCHAR(100),

 -- Glossary
 glossary_term_id UUID, -- Glossary

 -- Reference (Conversation, Requirement)
 source_conversation_id UUID, -- Conversation
 source_requirement_id UUID, -- Requirement

 --
 owner_user_id UUID, -- Artifact

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_artifact_trackings_spec FOREIGN KEY (spec_id)
 REFERENCES specs(id) ON DELETE SET NULL,
 CONSTRAINT fk_artifact_trackings_glossary_term FOREIGN KEY (glossary_term_id)
 REFERENCES glossary_terms(id) ON DELETE SET NULL,
 CONSTRAINT fk_artifact_trackings_conversation FOREIGN KEY (source_conversation_id)
 REFERENCES conversations(id) ON DELETE SET NULL,
 CONSTRAINT fk_artifact_trackings_requirement FOREIGN KEY (source_requirement_id)
 REFERENCES requirements(id) ON DELETE SET NULL,
 CONSTRAINT fk_artifact_trackings_owner FOREIGN KEY (owner_user_id) REFERENCES users(id) ON DELETE SET NULL,
 CONSTRAINT fk_artifact_trackings_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_artifact_trackings_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE artifact_trackings IS 'Artifact tracking (generated code and DB objects)';
COMMENT ON COLUMN artifact_trackings.depends_on IS 'JSONB array: ["table_name", ...] — dependent DB objects';

-- -----------------------------------------------------------------------------
-- ArtifactToSpecMapping: Existing code <-> spec mapping (brownfield)
-- artifact_trackings Spec Create (greenfield, 1:1)
-- artifact_to_spec_mappings Spec (brownfield, N:M)
-- -----------------------------------------------------------------------------
CREATE TABLE artifact_to_spec_mappings (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 --
 spec_id UUID NOT NULL,
 artifact_path VARCHAR(500) NOT NULL, -- (: src/PaymentService.cs)

 --
 mapping_reason VARCHAR(20) NOT NULL DEFAULT 'Manual', -- Manual, AutoDetected, AISuggested
 source_content TEXT, -- ( )
 notes TEXT, --

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 -- NOTE: tenant_id has no FK — tenants table not yet implemented (tenant isolation via application layer)
 CONSTRAINT fk_asm_project FOREIGN KEY (project_id)
 REFERENCES projects(id) ON DELETE CASCADE,
 CONSTRAINT fk_asm_spec FOREIGN KEY (spec_id)
 REFERENCES specs(id) ON DELETE CASCADE,
 CONSTRAINT fk_asm_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_asm_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT uq_asm_tenant_project_spec_path
 UNIQUE (tenant_id, project_id, spec_id, artifact_path)
);

COMMENT ON TABLE artifact_to_spec_mappings IS 'Existing code <-> spec mapping (brownfield)';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_entity_metadata_tenant_project ON entity_metadata(tenant_id, project_id);
CREATE INDEX idx_entity_metadata_code ON entity_metadata(code);
CREATE INDEX idx_entity_metadata_spec_id ON entity_metadata(spec_id);

CREATE INDEX idx_field_metadata_entity_id ON field_metadata(entity_metadata_id);

CREATE INDEX idx_entity_relationship_metadata_entity_id ON entity_relationship_metadata(entity_metadata_id);

CREATE INDEX idx_artifact_trackings_tenant_project ON artifact_trackings(tenant_id, project_id);
CREATE INDEX idx_artifact_trackings_spec_id ON artifact_trackings(spec_id);
CREATE INDEX idx_artifact_trackings_artifact_type ON artifact_trackings(artifact_type);
CREATE INDEX idx_artifact_trackings_artifact_category ON artifact_trackings(artifact_category);
CREATE INDEX idx_artifact_trackings_glossary_term_id ON artifact_trackings(glossary_term_id);
CREATE INDEX idx_artifact_trackings_conversation_id ON artifact_trackings(source_conversation_id);
CREATE INDEX idx_artifact_trackings_requirement_id ON artifact_trackings(source_requirement_id);
CREATE INDEX idx_artifact_tracking_owner_user_id ON artifact_trackings(owner_user_id);

CREATE INDEX idx_asm_tenant_project ON artifact_to_spec_mappings(tenant_id, project_id);
CREATE INDEX idx_asm_spec_id ON artifact_to_spec_mappings(spec_id);
CREATE INDEX idx_asm_artifact_path ON artifact_to_spec_mappings(tenant_id, project_id, artifact_path);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Metadata schema created: entity_metadata, field_metadata, entity_relationship_metadata, artifact_trackings, artifact_to_spec_mappings';
END $$;

-- <<< end bundled from scripts/db/16-schema-metadata.sql <<<

\echo '[17] Creating Audit Schema...'

-- >>> bundled from scripts/db/17-schema-audit.sql >>>
-- =============================================================================
-- 17-schema-audit.sql
-- Audit/Tracking Table: AuditLog, OutboxMessage, LegacyIdMapping, AiReport
-- =============================================================================

-- -----------------------------------------------------------------------------
-- AuditLog: Audit Log
-- -----------------------------------------------------------------------------
CREATE TABLE audit_logs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 actor_id UUID, -- NULL = System
 actor_type VARCHAR(20) DEFAULT 'USER', -- USER, SYSTEM, AI
 actor_type_code_id UUID NOT NULL,

 --
 action VARCHAR(50) NOT NULL, -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT
 resource_type VARCHAR(50) NOT NULL, -- spec, requirement, conversation
 resource_id UUID,

 --
 payload JSONB, -- ({changes: [{field, oldValue, newValue}]})
 description TEXT,

 --
 tenant_id UUID,
 project_id UUID,

 --
 ip_address VARCHAR(45),
 user_agent TEXT,
 correlation_id VARCHAR(64),

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_audit_logs_actor_type_code FOREIGN KEY (actor_type_code_id) REFERENCES codes(id),
 CONSTRAINT chk_audit_logs_actor_system CHECK (
 (actor_type = 'SYSTEM' AND actor_id IS NULL) OR (actor_type != 'SYSTEM')
 )
);

COMMENT ON TABLE audit_logs IS 'Audit Log';

-- -----------------------------------------------------------------------------
-- OutboxMessage: Transactional Outbox Pattern
-- -----------------------------------------------------------------------------
CREATE TABLE outbox_messages (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 event_type VARCHAR(100) NOT NULL,
 aggregate_type VARCHAR(100) NOT NULL,
 aggregate_id UUID NOT NULL,

 --
 payload JSONB NOT NULL,

 -- Status
 processed_at TIMESTAMPTZ,
 retry_count INT NOT NULL DEFAULT 0,
 last_error TEXT,
 max_retries INT NOT NULL DEFAULT 5,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE outbox_messages IS 'Transactional Outbox Pattern message queue';

-- -----------------------------------------------------------------------------
-- LegacyIdMapping: System ID
-- -----------------------------------------------------------------------------
CREATE TABLE legacy_id_mappings (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 entity_type VARCHAR(50) NOT NULL, -- spec, requirement
 legacy_system VARCHAR(100) NOT NULL, -- System
 legacy_id VARCHAR(200) NOT NULL, -- System ID
 new_id UUID NOT NULL, -- SDDP ID

 -- Metadata
 metadata JSONB,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT uq_legacy_id_mappings UNIQUE (entity_type, legacy_system, legacy_id)
);

COMMENT ON TABLE legacy_id_mappings IS 'Legacy system ID mapping (brownfield support)';

-- -----------------------------------------------------------------------------
-- AiReport: AI analysis result
-- -----------------------------------------------------------------------------
CREATE TABLE ai_reports (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 --
 analysis_type VARCHAR(50) NOT NULL, -- QUALITY_CHECK, CONSISTENCY, SUGGESTION, SUMMARY
 target_type VARCHAR(50) NOT NULL, -- spec, requirement, conversation
 target_id UUID NOT NULL,

 -- Status
 status VARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING, PROCESSING, COMPLETED, FAILED

 --
 result_json JSONB,
 error_message TEXT,

 --
 job_id VARCHAR(100),
 started_at TIMESTAMPTZ,
 completed_at TIMESTAMPTZ,

 -- AI
 model_used VARCHAR(100),
 tokens_used BIGINT,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 -- FK
 CONSTRAINT fk_ai_reports_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_ai_reports_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE ai_reports IS 'AI analysis result';

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON COLUMN audit_logs.payload IS 'JSONB: {changes: [{field, oldValue, newValue}]} — change details';
COMMENT ON COLUMN audit_logs.actor_type IS 'USER | SYSTEM | AI';
COMMENT ON COLUMN ai_reports.result_json IS 'JSONB: analysis result (structure varies by analysis_type)';
COMMENT ON COLUMN ai_reports.analysis_type IS 'QUALITY_CHECK | CONSISTENCY | SUGGESTION | SUMMARY | MISSING_FIELD | CONFLICT | IMPACT | REMINDER';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
-- AuditLog
CREATE INDEX idx_audit_logs_actor_id ON audit_logs(actor_id);
CREATE INDEX idx_audit_logs_actor_type_code_id ON audit_logs(actor_type_code_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_resource ON audit_logs(resource_type, resource_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at);
CREATE INDEX idx_audit_logs_tenant_project ON audit_logs(tenant_id, project_id);
CREATE INDEX idx_audit_logs_correlation_id ON audit_logs(correlation_id);

-- OutboxMessage
CREATE INDEX idx_outbox_messages_processed_at ON outbox_messages(processed_at) WHERE processed_at IS NULL;
CREATE INDEX idx_outbox_messages_aggregate ON outbox_messages(aggregate_type, aggregate_id);
CREATE INDEX idx_outbox_messages_event_type ON outbox_messages(event_type);

-- LegacyIdMapping
CREATE INDEX idx_legacy_id_mappings_entity_type ON legacy_id_mappings(entity_type);
CREATE INDEX idx_legacy_id_mappings_new_id ON legacy_id_mappings(new_id);

-- AiReport
CREATE INDEX idx_ai_reports_tenant_project ON ai_reports(tenant_id, project_id);
CREATE INDEX idx_ai_reports_target ON ai_reports(target_type, target_id);
CREATE INDEX idx_ai_reports_status ON ai_reports(status);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Audit schema created: audit_logs, outbox_messages, legacy_id_mappings, ai_reports';
END $$;

-- <<< end bundled from scripts/db/17-schema-audit.sql <<<

\echo '[18] Creating Task Schema...'

-- >>> bundled from scripts/db/18-schema-task.sql >>>
-- =============================================================================
-- 18-schema-task.sql
-- Task Management Tables
-- =============================================================================

-- -----------------------------------------------------------------------------
-- tasks - Task Table
-- -----------------------------------------------------------------------------
CREATE TABLE tasks (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 tenant_id UUID NOT NULL,
 project_id UUID,
 title VARCHAR(500) NOT NULL,
 description TEXT NOT NULL DEFAULT '',
 status_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000011000001', -- TASK_STATUS/TODO
 priority_code_id UUID NOT NULL DEFAULT '00000000-0000-0000-0006-000012000002', -- TASK_PRIORITY/MEDIUM
 assignee_id UUID,
 creator_id UUID NOT NULL,
 estimated_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
 actual_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
 completed_at TIMESTAMPTZ,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,
 created_by UUID NOT NULL,
 updated_by UUID NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 sort_order INT NOT NULL DEFAULT 0,
 due_date DATE,

 CONSTRAINT fk_tasks_assignee FOREIGN KEY (assignee_id) REFERENCES users(id),
 CONSTRAINT fk_tasks_creator FOREIGN KEY (creator_id) REFERENCES users(id),
 CONSTRAINT fk_tasks_status_code FOREIGN KEY (status_code_id) REFERENCES codes(id),
 CONSTRAINT fk_tasks_priority_code FOREIGN KEY (priority_code_id) REFERENCES codes(id),
 CONSTRAINT fk_tasks_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_tasks_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

-- Indexes
CREATE INDEX idx_tasks_tenant_project ON tasks (tenant_id, project_id);
CREATE INDEX idx_tasks_status_code_id ON tasks (status_code_id);
CREATE INDEX idx_tasks_priority_code_id ON tasks (priority_code_id);
CREATE INDEX idx_tasks_assignee ON tasks (assignee_id);
CREATE INDEX idx_tasks_is_active ON tasks (is_active);
CREATE INDEX idx_tasks_created_at ON tasks (created_at DESC);
CREATE INDEX idx_tasks_sort_order ON tasks (status_code_id, sort_order);
CREATE INDEX idx_tasks_due_date ON tasks (due_date) WHERE due_date IS NOT NULL AND is_active = TRUE;
CREATE INDEX idx_tasks_assignee_due_date ON tasks (assignee_id, due_date) WHERE due_date IS NOT NULL AND is_active = TRUE;

-- -----------------------------------------------------------------------------
-- task_acceptance_criteria - Task
-- -----------------------------------------------------------------------------
CREATE TABLE task_acceptance_criteria (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 task_id UUID NOT NULL,
 description TEXT NOT NULL,
 completed BOOLEAN NOT NULL DEFAULT FALSE,
 sort_order INT NOT NULL DEFAULT 0,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 CONSTRAINT fk_task_criteria_task FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE
);

CREATE INDEX idx_task_criteria_task ON task_acceptance_criteria (task_id);

-- -----------------------------------------------------------------------------
-- task_linked_items - Task
-- -----------------------------------------------------------------------------
CREATE TABLE task_linked_items (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 task_id UUID NOT NULL,
 linked_type VARCHAR(50) NOT NULL, -- conversation, requirement, spec, artifact
 linked_entity_id UUID NOT NULL,
 linked_by UUID NOT NULL,
 linked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 CONSTRAINT fk_task_links_task FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
 CONSTRAINT fk_task_links_linked_by FOREIGN KEY (linked_by) REFERENCES users(id),
 CONSTRAINT uq_task_links_unique UNIQUE (task_id, linked_type, linked_entity_id),
 CONSTRAINT chk_task_links_linked_type CHECK (linked_type IN ('conversation', 'requirement', 'spec', 'artifact', 'glossary'))
);

CREATE INDEX idx_task_links_task ON task_linked_items (task_id);
CREATE INDEX idx_task_links_type_entity ON task_linked_items (linked_type, linked_entity_id);

-- -----------------------------------------------------------------------------
-- task_time_logs - Task
-- -----------------------------------------------------------------------------
CREATE TABLE task_time_logs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 task_id UUID NOT NULL,
 user_id UUID NOT NULL,
 log_date DATE NOT NULL,
 hours DECIMAL(5,2) NOT NULL,
 description TEXT NOT NULL DEFAULT '',
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 CONSTRAINT fk_task_time_logs_task FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
 CONSTRAINT fk_task_time_logs_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_task_time_logs_task ON task_time_logs (task_id);
CREATE INDEX idx_task_time_logs_user ON task_time_logs (user_id);
CREATE INDEX idx_task_time_logs_date ON task_time_logs (log_date DESC);

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------

COMMENT ON TABLE tasks IS 'Project or personal tasks';
COMMENT ON COLUMN tasks.due_date IS 'Due date (nullable, optional)';
COMMENT ON TABLE task_acceptance_criteria IS 'Task acceptance criteria';
COMMENT ON TABLE task_time_logs IS 'Task worklog entries';
COMMENT ON COLUMN task_linked_items.linked_type IS 'conversation | requirement | spec | artifact | glossary';

-- -----------------------------------------------------------------------------
-- task_categories - User-defined task categories
-- -----------------------------------------------------------------------------
CREATE TABLE task_categories (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 tenant_id UUID NOT NULL,
 user_id UUID NOT NULL REFERENCES users(id),
 name VARCHAR(200) NOT NULL,
 icon VARCHAR(50),
 sort_order INT NOT NULL DEFAULT 0,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_task_categories_tenant_user ON task_categories(tenant_id, user_id);
CREATE UNIQUE INDEX uq_task_categories_name ON task_categories(tenant_id, user_id, name) WHERE is_active = TRUE;

COMMENT ON TABLE task_categories IS 'User-defined task categories';

-- Add category_id FK to task_categories (nullable, ON DELETE SET NULL)
ALTER TABLE tasks ADD COLUMN category_id UUID REFERENCES task_categories(id) ON DELETE SET NULL;
CREATE INDEX idx_tasks_category_id ON tasks(category_id);

-- <<< end bundled from scripts/db/18-schema-task.sql <<<

\echo '[19] Creating Effort Schema...'

-- >>> bundled from scripts/db/19-schema-effort.sql >>>
-- =============================================================================
-- 19-schema-effort.sql
-- Effort Management: EffortAllocation, Worklog, WorkingDay
-- =============================================================================

-- -----------------------------------------------------------------------------
-- EffortAllocation: Per-project user effort allocation plan
-- -----------------------------------------------------------------------------
CREATE TABLE effort_allocations (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- User Reference
 user_id UUID NOT NULL,

 -- Assign
 allocation_date DATE NOT NULL,
 allocated_hours DECIMAL(4,2) NOT NULL DEFAULT 0,

 -- Audit
 created_by UUID NOT NULL,
 updated_by UUID NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_effort_allocations_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_effort_allocations_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_effort_allocations_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT chk_effort_allocations_hours CHECK (allocated_hours >= 0 AND allocated_hours <= 24)
);

COMMENT ON TABLE effort_allocations IS 'Per-project user effort allocation plan';

-- -----------------------------------------------------------------------------
-- Worklog:
-- -----------------------------------------------------------------------------
CREATE TABLE worklogs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- User Reference
 user_id UUID NOT NULL,

 --
 work_date DATE NOT NULL,
 spent_hours DECIMAL(4,2) NOT NULL DEFAULT 0,
 note TEXT,

 -- Task ()
 task_id UUID,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_worklogs_user FOREIGN KEY (user_id)
 REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_worklogs_task FOREIGN KEY (task_id)
 REFERENCES tasks(id) ON DELETE SET NULL,
 CONSTRAINT chk_worklogs_hours CHECK (spent_hours >= 0 AND spent_hours <= 24)
);

COMMENT ON TABLE worklogs IS 'Actual work time records';

-- -----------------------------------------------------------------------------
-- WorkingDay: Per-project working day settings
-- -----------------------------------------------------------------------------
CREATE TABLE working_days (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Workday
 work_date DATE NOT NULL,
 day_type VARCHAR(20) NOT NULL DEFAULT 'workday', -- workday, offday, holiday, exception
 day_type_code_id UUID NOT NULL,
 note TEXT,

 -- Audit
 created_by UUID NOT NULL,
 updated_by UUID NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_working_days_day_type_code FOREIGN KEY (day_type_code_id) REFERENCES codes(id),
 CONSTRAINT fk_working_days_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_working_days_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT
);

COMMENT ON TABLE working_days IS 'Per-project working day settings';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------

-- EffortAllocation indexes
CREATE INDEX idx_effort_allocations_tenant_project ON effort_allocations(tenant_id, project_id);
CREATE INDEX idx_effort_allocations_user ON effort_allocations(user_id);
CREATE INDEX idx_effort_allocations_date ON effort_allocations(allocation_date);
CREATE INDEX idx_effort_allocations_project_date ON effort_allocations(project_id, allocation_date);
CREATE UNIQUE INDEX uq_effort_allocations_unique ON effort_allocations(project_id, user_id, allocation_date);

-- Worklog indexes
CREATE INDEX idx_worklogs_tenant_project ON worklogs(tenant_id, project_id);
CREATE INDEX idx_worklogs_user ON worklogs(user_id);
CREATE INDEX idx_worklogs_date ON worklogs(work_date);
CREATE INDEX idx_worklogs_project_date ON worklogs(project_id, work_date);

-- WorkingDay indexes
CREATE INDEX idx_working_days_tenant_project ON working_days(tenant_id, project_id);
CREATE INDEX idx_working_days_project_date ON working_days(project_id, work_date);
CREATE INDEX idx_working_days_type ON working_days(day_type);
CREATE INDEX idx_working_days_day_type_code_id ON working_days(day_type_code_id);
CREATE UNIQUE INDEX uq_working_days_unique ON working_days(project_id, work_date);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Effort schema created: effort_allocations, worklogs, working_days';
END $$;

-- <<< end bundled from scripts/db/19-schema-effort.sql <<<

\echo '[21] Creating System Config Schema...'

-- >>> bundled from scripts/db/21-schema-system-config.sql >>>
-- =============================================================================
-- 21-schema-system-config.sql
-- System Configuration Tables
-- =============================================================================

-- -----------------------------------------------------------------------------
-- SystemConfig: System/tenant/project scoped configuration values
-- tenant_id NULL = global system config
-- tenant_id NOT NULL, project_id NULL = tenant-level config
-- tenant_id NOT NULL, project_id NOT NULL = Project Config
-- -----------------------------------------------------------------------------
CREATE TABLE system_configs (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 -- (NULL = Top )
 tenant_id UUID, -- NULL = Global Config
 project_id UUID, -- NULL = tenant-level config

 -- Config
 config_group VARCHAR(50) NOT NULL, -- : general, auth, storage, performance, aiAgent
 config_key VARCHAR(100) NOT NULL, -- : siteName, sessionTimeout, cacheEnabled
 config_value TEXT, -- Config ( )

 -- ()
 value_type VARCHAR(20) NOT NULL DEFAULT 'string', -- string, number, boolean, json

 --
 display_name VARCHAR(200), -- UI
 description TEXT, --

 --
 validation_regex VARCHAR(500), --
 min_value DECIMAL, --
 max_value DECIMAL, --
 allowed_values TEXT, -- (JSON array)

 -- Metadata
 is_sensitive BOOLEAN NOT NULL DEFAULT FALSE, -- ( )
 is_readonly BOOLEAN NOT NULL DEFAULT FALSE, -- (System Config)
 is_system BOOLEAN NOT NULL DEFAULT FALSE, -- System Config (Delete )
 sort_order INT NOT NULL DEFAULT 0,

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 created_by UUID,
 updated_by UUID,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 -- (tenant_id FK — tenants Table , codes Table Manage)
 CONSTRAINT fk_system_configs_project FOREIGN KEY (project_id)
 REFERENCES projects(id) ON DELETE CASCADE,
 -- + (UNIQUE INDEX , Reference)
 CONSTRAINT fk_system_configs_created_by FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT fk_system_configs_updated_by FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE RESTRICT,
 CONSTRAINT chk_system_configs_project_scope CHECK (
 project_id IS NULL OR tenant_id IS NOT NULL -- Project Config Tenant
 )
);

COMMENT ON TABLE system_configs IS 'System/tenant/project scoped configuration values';
COMMENT ON COLUMN system_configs.tenant_id IS 'NULL = global system config';
COMMENT ON COLUMN system_configs.project_id IS 'NULL = tenant-level config';
COMMENT ON COLUMN system_configs.value_type IS 'string, number, boolean, json';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
-- + (COALESCE UNIQUE constraint , INDEX )
CREATE UNIQUE INDEX uq_system_configs_scope_group_key
 ON system_configs (
 COALESCE(tenant_id, '00000000-0000-0000-0000-000000000000'),
 COALESCE(project_id, '00000000-0000-0000-0000-000000000000'),
 config_group,
 config_key
 );

CREATE INDEX idx_system_configs_tenant ON system_configs(tenant_id) WHERE tenant_id IS NOT NULL;
CREATE INDEX idx_system_configs_project ON system_configs(tenant_id, project_id) WHERE project_id IS NOT NULL;
CREATE INDEX idx_system_configs_group ON system_configs(config_group);
CREATE INDEX idx_system_configs_key ON system_configs(config_key);
CREATE INDEX idx_system_configs_scope ON system_configs(tenant_id, project_id, config_group);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'System config schema created: system_configs';
END $$;

-- <<< end bundled from scripts/db/21-schema-system-config.sql <<<

\echo '[22] Creating Embeddings Schema...'

-- >>> bundled from scripts/db/22-schema-embeddings.sql >>>
-- =============================================================================
-- 22-schema-embeddings.sql
-- AI Embeddings Table (pgvector)
-- =============================================================================
-- Purpose: Store text embeddings for vector similarity search (RAG pipeline)
-- Source types: message, spec, decision, glossary
-- Model: nomic-embed-text (768-dimensional vectors)
-- =============================================================================

-- Table: embeddings
CREATE TABLE embeddings (
 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Source tracking
 source_type VARCHAR(50) NOT NULL, -- message, spec, decision, glossary
 source_id UUID NOT NULL, -- references original entity ID
 chunk_index INTEGER NOT NULL DEFAULT 0,

 -- Content + vector (nomic-embed-text 768-dim)
 chunk_text TEXT NOT NULL,
 embedding vector(768) NOT NULL,

 -- Metadata (nullable, varies by source_type)
 conversation_id UUID, -- for message/decision types
 spec_version VARCHAR(50), -- for spec type
 message_type VARCHAR(50), -- for decision type

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 -- Foreign keys
 -- NOTE: tenant_id has no FK — tenants table not yet implemented (tenant isolation via application layer)
 CONSTRAINT fk_embeddings_project FOREIGN KEY (project_id)
 REFERENCES projects(id) ON DELETE CASCADE,
 CONSTRAINT fk_embeddings_conversation FOREIGN KEY (conversation_id)
 REFERENCES conversations(id) ON DELETE SET NULL
);

-- Query indexes
CREATE INDEX idx_embeddings_tenant_project
 ON embeddings(tenant_id, project_id, is_active);
CREATE INDEX idx_embeddings_source
 ON embeddings(tenant_id, project_id, source_type, source_id, is_active);
CREATE INDEX idx_embeddings_conversation
 ON embeddings(conversation_id)
 WHERE conversation_id IS NOT NULL AND is_active = true;

-- Vector similarity search index (HNSW: works from 0 rows, better recall than IVFFlat)
CREATE INDEX idx_embeddings_vector_hnsw
 ON embeddings USING hnsw (embedding vector_cosine_ops)
 WHERE is_active = true;

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON COLUMN embeddings.source_type IS 'message | spec | decision | glossary | requirement | artifact';
COMMENT ON COLUMN embeddings.embedding IS 'vector(768): nomic-embed-text model output';

-- <<< end bundled from scripts/db/22-schema-embeddings.sql <<<

\echo '[23] Creating SLA Schema...'

-- >>> bundled from scripts/db/23-schema-sla.sql >>>
-- =============================================================================
-- 23-schema-sla.sql
-- SLA + Notification Table
-- =============================================================================

-- -----------------------------------------------------------------------------
-- SLA Policies: Per-project approval SLA policy
-- -----------------------------------------------------------------------------
CREATE TABLE sla_policies (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 --
 sla_type VARCHAR(20) NOT NULL DEFAULT 'signoff', -- signoff, review, decision
 sla_hours INTEGER NOT NULL DEFAULT 24, -- SLA ()
 urgent_sla_hours INTEGER NOT NULL DEFAULT 4, -- Critical SLA ()
 reminder_at_percent VARCHAR(50) NOT NULL DEFAULT '50,100,200', -- (%)
 escalation_role VARCHAR(50), -- Role

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 -- NOTE: tenant_id has no FK — tenants table not yet implemented (tenant isolation via application layer)
 CONSTRAINT fk_slap_project FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
 CONSTRAINT uq_slap_project_type UNIQUE (tenant_id, project_id, sla_type)
);

COMMENT ON TABLE sla_policies IS 'Per-project approval SLA policy';
COMMENT ON COLUMN sla_policies.reminder_at_percent IS 'Comma-separated integers (for example: 50,100,200)';

-- -----------------------------------------------------------------------------
-- SLA Notifications: SLA Notification ( )
-- -----------------------------------------------------------------------------
CREATE TABLE sla_notifications (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

 --
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,

 -- Notification
 sign_off_id UUID NOT NULL,
 threshold_percent INTEGER NOT NULL, -- 50, 100, 200
 notification_type VARCHAR(20) NOT NULL, -- reminder, escalation, warning

 --
 notified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 -- Audit
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 --
 CONSTRAINT fk_slan_signoff FOREIGN KEY (sign_off_id) REFERENCES sign_offs(id) ON DELETE CASCADE,
 CONSTRAINT uq_slan_signoff_threshold UNIQUE (sign_off_id, threshold_percent)
);

COMMENT ON TABLE sla_notifications IS 'SLA notification delivery log (deduplicated)';

-- -----------------------------------------------------------------------------
-- Indexes
-- -----------------------------------------------------------------------------
CREATE INDEX idx_slap_tenant_project ON sla_policies(tenant_id, project_id);
CREATE INDEX idx_slan_signoff_id ON sla_notifications(sign_off_id);
CREATE INDEX idx_slan_tenant_project ON sla_notifications(tenant_id, project_id);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'SLA schema created: sla_policies, sla_notifications';
END $$;

-- <<< end bundled from scripts/db/23-schema-sla.sql <<<

\echo '[24] Creating Project Snapshots Schema...'

-- >>> bundled from scripts/db/24-schema-project-snapshots.sql >>>
-- =============================================================================
-- 24-schema-project-snapshots.sql
-- Project Snapshots (full JSONB backup of all project data)
-- =============================================================================

CREATE TABLE project_snapshots (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 tenant_id UUID NOT NULL,
 project_id UUID NOT NULL,
 name VARCHAR(200) NOT NULL,
 description TEXT,
 snapshot_type VARCHAR(20) NOT NULL DEFAULT 'manual',
 status VARCHAR(20) NOT NULL DEFAULT 'completed',
 snapshot_data JSONB NOT NULL,
 table_counts JSONB NOT NULL DEFAULT '{}'::jsonb,
 data_size_bytes BIGINT NOT NULL DEFAULT 0,
 created_by UUID NOT NULL,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 is_active BOOLEAN NOT NULL DEFAULT TRUE,

 CONSTRAINT fk_snapshots_project
 FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
 CONSTRAINT fk_snapshots_created_by
 FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE RESTRICT
);

CREATE INDEX idx_project_snapshots_project
 ON project_snapshots(tenant_id, project_id);

-- -----------------------------------------------------------------------------
-- Column/Table Comments
-- -----------------------------------------------------------------------------
COMMENT ON TABLE project_snapshots IS 'Project data snapshot (backup and restore)';
COMMENT ON COLUMN project_snapshots.snapshot_data IS 'JSONB: full project data backup {specs: [...], requirements: [...], ...}';
COMMENT ON COLUMN project_snapshots.table_counts IS 'JSONB: {table_name: row_count, ...} — per-table row counts';
COMMENT ON COLUMN project_snapshots.snapshot_type IS 'manual | pre_restore (auto-created before restore)';

\echo ' -> project_snapshots table created'

-- <<< end bundled from scripts/db/24-schema-project-snapshots.sql <<<

\echo '[25] Creating Notification Schema...'

-- >>> bundled from scripts/db/25-schema-notification.sql >>>
-- =============================================================================
-- 25-schema-notification.sql
-- Notification Management Tables
-- =============================================================================

-- -----------------------------------------------------------------------------
-- notifications - User Notification
-- -----------------------------------------------------------------------------
CREATE TABLE notifications (
 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
 tenant_id UUID NOT NULL,
 recipient_id UUID NOT NULL, -- Notification (users.id FK)
 actor_id UUID, -- Notification User
 type VARCHAR(50) NOT NULL, -- 'task_assigned', 'new_message'
 title VARCHAR(500) NOT NULL,
 message TEXT NOT NULL DEFAULT '',
 entity_type VARCHAR(50), -- 'task', 'conversation'
 entity_id UUID, -- Entity ID
 is_read BOOLEAN NOT NULL DEFAULT FALSE,
 read_at TIMESTAMPTZ,
 is_active BOOLEAN NOT NULL DEFAULT TRUE,
 created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

 CONSTRAINT fk_notifications_recipient FOREIGN KEY (recipient_id) REFERENCES users(id) ON DELETE CASCADE,
 CONSTRAINT fk_notifications_actor FOREIGN KEY (actor_id) REFERENCES users(id) ON DELETE SET NULL
);

-- Indexes
CREATE INDEX idx_notifications_recipient_unread
 ON notifications (recipient_id, is_read, created_at DESC)
 WHERE is_active = TRUE;

CREATE INDEX idx_notifications_tenant_recipient
 ON notifications (tenant_id, recipient_id);

CREATE INDEX idx_notifications_created_at
 ON notifications (created_at DESC);

COMMENT ON TABLE notifications IS 'User Notification';
COMMENT ON COLUMN notifications.type IS 'task_assigned | new_message';
COMMENT ON COLUMN notifications.entity_type IS 'task | conversation';

-- <<< end bundled from scripts/db/25-schema-notification.sql <<<

-- -----------------------------------------------------------------------------
-- Indexes (30-49)
-- -----------------------------------------------------------------------------
\echo '[30] Creating Indexes...'

-- >>> bundled from scripts/db/30-indexes.sql >>>
-- =============================================================================
-- 30-indexes.sql
-- Index: Query Index
-- /Default Index
-- =============================================================================

-- -----------------------------------------------------------------------------
-- specs: Read (GetSpecsQuery)
-- WHERE tenant_id=X AND project_id=Y AND is_active AND valid_to IS NULL
-- -----------------------------------------------------------------------------
CREATE INDEX idx_specs_current_by_project
 ON specs(tenant_id, project_id, is_active, status_code_id)
 WHERE valid_to IS NULL;

-- -----------------------------------------------------------------------------
-- requirements: Read (GetRequirementsQuery)
-- -----------------------------------------------------------------------------
CREATE INDEX idx_requirements_current_by_project
 ON requirements(tenant_id, project_id, is_active, status_code_id)
 WHERE valid_to IS NULL;

-- requirements: Read (GetRequirementTreeQuery)
CREATE INDEX idx_requirements_children
 ON requirements(parent_id, is_active)
 WHERE valid_to IS NULL;

-- -----------------------------------------------------------------------------
-- sign_offs: tenant_project + SLA Read
-- -----------------------------------------------------------------------------
CREATE INDEX idx_sign_offs_tenant_project
 ON sign_offs(tenant_id, project_id);

CREATE INDEX idx_sign_offs_spec_project
 ON sign_offs(spec_id, tenant_id, project_id, is_active);

CREATE INDEX idx_sign_offs_pending
 ON sign_offs(tenant_id, project_id, is_active, requested_at)
 WHERE signed_at IS NULL;

-- -----------------------------------------------------------------------------
-- alternatives: tenant_project
-- -----------------------------------------------------------------------------
CREATE INDEX idx_alternatives_tenant_project
 ON alternatives(tenant_id, project_id);

-- -----------------------------------------------------------------------------
-- conversations: Read (GetConversationsQuery)
-- WHERE tenant_id=X AND project_id=Y AND conversation_type=Z AND is_active
-- -----------------------------------------------------------------------------
CREATE INDEX idx_conversations_project_type
 ON conversations(tenant_id, project_id, conversation_type, is_active)
 WHERE project_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- messages: Conversation Read (Message )
-- WHERE conversation_id=X AND is_active ORDER BY created_at DESC
-- -----------------------------------------------------------------------------
CREATE INDEX idx_messages_conversation_chrono
 ON messages(conversation_id, is_active, created_at DESC);

-- messages: Read
CREATE INDEX idx_messages_topic_chrono
 ON messages(topic_id, is_active, created_at DESC)
 WHERE topic_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- conversation_members: ( Permission )
-- WHERE conversation_id=X AND user_id=Y AND is_active
-- -----------------------------------------------------------------------------
CREATE INDEX idx_conversation_members_lookup
 ON conversation_members(conversation_id, user_id, is_active);

-- -----------------------------------------------------------------------------
-- relationships: ( Relationship)
-- WHERE tenant_id=X AND project_id=Y AND from_entity_type=Z AND is_active
-- -----------------------------------------------------------------------------
CREATE INDEX idx_relationships_from_current
 ON relationships(tenant_id, project_id, from_entity_type, from_entity_id, is_active)
 WHERE valid_to IS NULL;

CREATE INDEX idx_relationships_to_current
 ON relationships(tenant_id, project_id, to_entity_type, to_entity_id, is_active)
 WHERE valid_to IS NULL;

-- -----------------------------------------------------------------------------
-- tasks: Project Task (GetTasksQuery)
-- WHERE tenant_id=X AND project_id=Y AND is_active AND status=Z
-- -----------------------------------------------------------------------------
CREATE INDEX idx_tasks_project_status
 ON tasks(tenant_id, project_id, is_active, status_code_id, priority_code_id);

-- -----------------------------------------------------------------------------
-- audit_logs: Audit Log Read (GetAuditLogsQuery)
-- WHERE tenant_id=X AND project_id=Y AND is_active ORDER BY created_at DESC
-- -----------------------------------------------------------------------------
CREATE INDEX idx_audit_logs_project_chrono
 ON audit_logs(tenant_id, project_id, is_active, created_at DESC);

-- -----------------------------------------------------------------------------
-- artifact_trackings: Spec Read (GetArtifactsBySpecQuery)
-- -----------------------------------------------------------------------------
CREATE INDEX idx_artifact_trackings_by_spec
 ON artifact_trackings(tenant_id, project_id, spec_id, is_active);

-- -----------------------------------------------------------------------------
-- glossary_terms: Read
-- -----------------------------------------------------------------------------
CREATE INDEX idx_glossary_terms_current_by_project
 ON glossary_terms(tenant_id, project_id, is_active, category_code_id)
 WHERE valid_to IS NULL;

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Composite indexes created (30-indexes.sql): 16 indexes';
END $$;

-- <<< end bundled from scripts/db/30-indexes.sql <<<

-- -----------------------------------------------------------------------------
-- Base Data (50-69)
-- -----------------------------------------------------------------------------
\echo '[50] Inserting Group Codes...'

-- >>> bundled from scripts/db/50-data-group-codes.sql >>>
-- =============================================================================
-- 50-data-group-codes.sql
-- GroupCode
-- =============================================================================

-- -----------------------------------------------------------------------------
-- System GroupCode
-- -----------------------------------------------------------------------------
INSERT INTO group_codes (id, code, name, description, category, is_system, is_extensible, sort_order)
VALUES
 -- Spec
 ('00000000-0000-0000-0001-000000000001', 'SPEC_STATUS', 'Spec Status', 'Spec workflow status', 'SYSTEM', TRUE, FALSE, 1),
 ('00000000-0000-0000-0001-000000000002', 'SPEC_TYPE', 'Spec Type', 'Spec category type', 'LOOKUP', TRUE, TRUE, 2),

 -- Requirement
 ('00000000-0000-0000-0001-000000000003', 'REQUIREMENT_STATUS', 'Requirement Status', 'Requirement workflow status', 'SYSTEM', TRUE, FALSE, 3),
 ('00000000-0000-0000-0001-000000000004', 'REQUIREMENT_LEVEL', 'Requirement Level', 'Requirement hierarchy level', 'SYSTEM', TRUE, FALSE, 4),
 ('00000000-0000-0000-0001-000000000005', 'REQUIREMENT_TYPE', 'Requirement Type', 'Requirement category type', 'LOOKUP', TRUE, TRUE, 5),

 -- Conversation
 ('00000000-0000-0000-0001-000000000007', 'MESSAGE_TYPE', 'Message Type', 'Message type (general, decision, suggestion, etc.)', 'SYSTEM', TRUE, FALSE, 7),
 ('00000000-0000-0000-0001-000000000008', 'CONVERSATION_TYPE', 'Conversation Type', 'Conversation types (Channel, Forum, DirectMessage)', 'SYSTEM', TRUE, FALSE, 8),

 -- Glossary
 ('00000000-0000-0000-0001-000000000009', 'GLOSSARY_STATUS', 'Glossary Status', 'Glossary workflow status', 'SYSTEM', TRUE, FALSE, 9),
 ('00000000-0000-0000-0001-000000000010', 'GLOSSARY_CATEGORY', 'Glossary Category', 'Glossary category', 'LOOKUP', TRUE, TRUE, 10),

 -- Task
 ('00000000-0000-0000-0001-000000000011', 'TASK_STATUS', 'Task Status', 'Task status', 'SYSTEM', TRUE, FALSE, 11),
 ('00000000-0000-0000-0001-000000000012', 'TASK_PRIORITY', 'Task Priority', 'Task priority', 'SYSTEM', TRUE, FALSE, 12),
 ('00000000-0000-0000-0001-000000000013', 'TASK_TYPE', 'Task Type', 'Task type', 'LOOKUP', TRUE, TRUE, 13),

 -- Relationship
 ('00000000-0000-0000-0001-000000000014', 'RELATIONSHIP_TYPE', 'Relationship Type', 'Entity relationship type', 'SYSTEM', TRUE, FALSE, 14),

 -- SignOff
 ('00000000-0000-0000-0001-000000000015', 'SIGNOFF_DECISION', 'SignOff Decision', 'SignOff Decision Type', 'SYSTEM', TRUE, FALSE, 15),

 -- Person
 ('00000000-0000-0000-0001-000000000016', 'PERSON_TYPE', 'Person Type', 'Person category', 'SYSTEM', TRUE, FALSE, 16),

 -- Role
 ('00000000-0000-0000-0001-000000000017', 'ROLE_TYPE', 'Role Type', 'Role scope type', 'SYSTEM', TRUE, FALSE, 17),

 -- Artifact
 ('00000000-0000-0000-0001-000000000018', 'ARTIFACT_TYPE', 'Artifact Type', 'Artifact type', 'LOOKUP', TRUE, TRUE, 18),

 -- Topic
 ('00000000-0000-0000-0001-000000000020', 'TOPIC_STATUS', 'Topic Status', 'Forum topic status', 'SYSTEM', TRUE, FALSE, 20),

 -- Channel
 ('00000000-0000-0000-0001-000000000021', 'CHANNEL_STATUS', 'Channel Status', 'Channel decision status', 'SYSTEM', TRUE, FALSE, 21),

 -- Project
 ('00000000-0000-0000-0001-000000000019', 'PROJECT_STATUS', 'Project Status', 'Project status', 'SYSTEM', TRUE, FALSE, 19),

 -- Direct Message
 ('00000000-0000-0000-0001-000000000022', 'DIRECT_MESSAGE_STATUS', 'DirectMessage Status', 'Direct message status', 'SYSTEM', TRUE, FALSE, 22),

 -- Effort
 ('00000000-0000-0000-0001-000000000023', 'WORKING_DAY_TYPE', 'Workday Type', 'Workday/Offday/Holiday/Exception Day Type', 'SYSTEM', TRUE, FALSE, 23),

 -- Audit
 ('00000000-0000-0000-0001-000000000024', 'AUDIT_ACTOR_TYPE', 'Audit Actor Type', 'Audit log actor type', 'SYSTEM', TRUE, FALSE, 24),

 -- Config
 ('00000000-0000-0000-0001-000000000100', 'SYS_CONFIG', 'System Config', 'System Config Value', 'CONFIG', TRUE, FALSE, 100),
 ('00000000-0000-0000-0001-000000000101', 'AI_CONFIG', 'AI Config', 'AI agent config values', 'CONFIG', TRUE, FALSE, 101)

ON CONFLICT (code) DO UPDATE SET
 name = EXCLUDED.name,
 description = EXCLUDED.description,
 category = EXCLUDED.category,
 is_system = EXCLUDED.is_system,
 is_extensible = EXCLUDED.is_extensible,
 sort_order = EXCLUDED.sort_order,
 updated_at = NOW();

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 cnt INT;
BEGIN
 SELECT COUNT(*) INTO cnt FROM group_codes;
 RAISE NOTICE 'GroupCodes inserted/updated: %', cnt;
END $$;

-- <<< end bundled from scripts/db/50-data-group-codes.sql <<<

\echo '[51] Inserting Codes...'

-- >>> bundled from scripts/db/51-data-codes.sql >>>
-- =============================================================================
-- 51-data-codes.sql
-- Code (Status, Type )
-- Well-known UUID : 00000000-0000-0000-0006-GGGGGGSSSSSS
-- GGGGGG = group_code sort_order (6)
-- SSSSSS = code sort_order (6)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- SPEC_STATUS (Spec Status) — Group 01
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000001000001', '00000000-0000-0000-0001-000000000001', 'DRAFT', 'Draft', 'Draft', 'Draft', TRUE, TRUE, 1,
 '{"color": "#6B7280", "icon": "edit", "allowEdit": true}'),
 ('00000000-0000-0000-0006-000001000002', '00000000-0000-0000-0001-000000000001', 'IN_REVIEW', 'In Review', 'In Review', 'In Review', FALSE, TRUE, 2,
 '{"color": "#F59E0B", "icon": "eye", "allowEdit": false}'),
 ('00000000-0000-0000-0006-000001000003', '00000000-0000-0000-0001-000000000001', 'APPROVED', 'Approved', 'Approved', 'Approved', FALSE, TRUE, 3,
 '{"color": "#10B981", "icon": "check", "allowEdit": false}'),
 ('00000000-0000-0000-0006-000001000004', '00000000-0000-0000-0001-000000000001', 'LOCKED', 'Locked', 'Locked', 'Locked', FALSE, TRUE, 4,
 '{"color": "#EF4444", "icon": "lock", "allowEdit": false}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 is_default = EXCLUDED.is_default, sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes,
 updated_at = NOW();

-- -----------------------------------------------------------------------------
-- REQUIREMENT_STATUS (Requirement Status) — Group 03
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000003000001', '00000000-0000-0000-0001-000000000003', 'DRAFT', 'Draft', 'Draft', 'Draft', TRUE, TRUE, 1,
 '{"color": "#6B7280", "icon": "edit"}'),
 ('00000000-0000-0000-0006-000003000002', '00000000-0000-0000-0001-000000000003', 'IN_REVIEW', 'In Review', 'In Review', 'In Review', FALSE, TRUE, 2,
 '{"color": "#F59E0B", "icon": "eye"}'),
 ('00000000-0000-0000-0006-000003000003', '00000000-0000-0000-0001-000000000003', 'APPROVED', 'Approved', 'Approved', 'Approved', FALSE, TRUE, 3,
 '{"color": "#10B981", "icon": "check"}'),
 ('00000000-0000-0000-0006-000003000004', '00000000-0000-0000-0001-000000000003', 'DEPRECATED', 'Deprecated', 'Deprecated', 'Deprecated', FALSE, TRUE, 4,
 '{"color": "#EF4444", "icon": "archive"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- REQUIREMENT_LEVEL (Requirement ) — Group 04
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000004000001', '00000000-0000-0000-0001-000000000004', 'A', 'Level A', 'Level A', 'Top', TRUE, TRUE, 1,
 '{"color": "#3B82F6", "description": "Top-level business requirement"}'),
 ('00000000-0000-0000-0006-000004000002', '00000000-0000-0000-0001-000000000004', 'B', 'Level B', 'Level B', 'Middle', FALSE, TRUE, 2,
 '{"color": "#8B5CF6", "description": "Sub-requirement"}'),
 ('00000000-0000-0000-0006-000004000003', '00000000-0000-0000-0001-000000000004', 'C', 'Level C', 'Level C', 'Detail', FALSE, TRUE, 3,
 '{"color": "#EC4899", "description": "Detail requirement"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- MESSAGE_TYPE (Message Type) — Group 07
-- C# MessageType enum: Proposal=0, Question=1, Objection=2, Reference=3, Decision=4, AiReminder=5, AiSummary=6, AiSuggestion=7, Normal=8
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000007000001', '00000000-0000-0000-0001-000000000007', 'PROPOSAL', 'Proposal', 'Proposal', 'Suggestion', TRUE, TRUE, 1,
 '{"icon": "lightbulb", "highlight": true, "highlightColor": "#8B5CF6"}'),
 ('00000000-0000-0000-0006-000007000002', '00000000-0000-0000-0001-000000000007', 'QUESTION', 'Question', 'Question', 'Question', FALSE, TRUE, 2,
 '{"icon": "help-circle", "highlight": true, "highlightColor": "#F59E0B"}'),
 ('00000000-0000-0000-0006-000007000003', '00000000-0000-0000-0001-000000000007', 'OBJECTION', 'Objection', 'Objection', 'Objection', FALSE, TRUE, 3,
 '{"icon": "x-circle", "highlight": true, "highlightColor": "#EF4444"}'),
 ('00000000-0000-0000-0006-000007000004', '00000000-0000-0000-0001-000000000007', 'REFERENCE', 'Reference', 'Reference', 'Reference', FALSE, TRUE, 4,
 '{"icon": "link", "highlight": false}'),
 ('00000000-0000-0000-0006-000007000005', '00000000-0000-0000-0001-000000000007', 'DECISION', 'Decision', 'Decision', 'Decision', FALSE, TRUE, 5,
 '{"icon": "gavel", "highlight": true, "highlightColor": "#10B981"}'),
 ('00000000-0000-0000-0006-000007000006', '00000000-0000-0000-0001-000000000007', 'AI_REMINDER', 'AI Reminder', 'AI Reminder', 'AI notification', FALSE, TRUE, 6,
 '{"icon": "bell", "highlight": false, "isSystemOnly": true}'),
 ('00000000-0000-0000-0006-000007000007', '00000000-0000-0000-0001-000000000007', 'AI_SUMMARY', 'AI Summary', 'AI Summary', 'AI summary', FALSE, TRUE, 7,
 '{"icon": "file-text", "highlight": false, "isSystemOnly": true}'),
 ('00000000-0000-0000-0006-000007000008', '00000000-0000-0000-0001-000000000007', 'AI_SUGGESTION', 'AI Suggestion', 'AI Suggestion', 'AI suggestion', FALSE, TRUE, 8,
 '{"icon": "sparkles", "highlight": false, "isSystemOnly": true}'),
 ('00000000-0000-0000-0006-000007000009', '00000000-0000-0000-0001-000000000007', 'NORMAL', 'Normal', 'Normal', 'General', FALSE, TRUE, 9,
 '{"icon": "message-circle", "highlight": false}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- CONVERSATION_TYPE (Conversation Type) — Group 08
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000008000001', '00000000-0000-0000-0001-000000000008', 'CHANNEL', 'Channel', 'Channel', 'Channel', TRUE, TRUE, 1,
 '{"icon": "message-square", "allowTopics": false}'),
 ('00000000-0000-0000-0006-000008000002', '00000000-0000-0000-0001-000000000008', 'FORUM', 'Forum', 'Forum', 'Forum', FALSE, TRUE, 2,
 '{"icon": "layout", "allowTopics": true}'),
 ('00000000-0000-0000-0006-000008000003', '00000000-0000-0000-0001-000000000008', 'DIRECT_MESSAGE', 'Direct Message', 'Direct Message', 'Direct Message', FALSE, TRUE, 3,
 '{"icon": "mail", "allowTopics": false}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- GLOSSARY_STATUS (Glossary Status) — Group 09
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000009000001', '00000000-0000-0000-0001-000000000009', 'DRAFT', 'Draft', 'Draft', 'Draft', TRUE, TRUE, 1,
 '{"color": "#6B7280"}'),
 ('00000000-0000-0000-0006-000009000002', '00000000-0000-0000-0001-000000000009', 'ACTIVE', 'Active', 'Active', 'Active', FALSE, TRUE, 2,
 '{"color": "#10B981"}'),
 ('00000000-0000-0000-0006-000009000003', '00000000-0000-0000-0001-000000000009', 'DEPRECATED', 'Deprecated', 'Deprecated', 'Deprecated', FALSE, TRUE, 3,
 '{"color": "#EF4444"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- GLOSSARY_CATEGORY (Glossary ) — Group 10
-- C# TermCategory enum: Technical=0, Business=1, Abbreviation=2, Domain=3, Architecture=4, Infrastructure=5, Security=6, Compliance=7, DesignPattern=8
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000010000001', '00000000-0000-0000-0001-000000000010', 'TECHNICAL', 'Technical', 'Technical', 'Technical', FALSE, TRUE, 1,
 '{"color": "#3B82F6"}'),
 ('00000000-0000-0000-0006-000010000002', '00000000-0000-0000-0001-000000000010', 'BUSINESS', 'Business', 'Business', 'Business', FALSE, TRUE, 2,
 '{"color": "#10B981"}'),
 ('00000000-0000-0000-0006-000010000003', '00000000-0000-0000-0001-000000000010', 'ABBREVIATION', 'Abbreviation', 'Abbreviation', 'Abbreviation', FALSE, TRUE, 3,
 '{"color": "#8B5CF6"}'),
 ('00000000-0000-0000-0006-000010000004', '00000000-0000-0000-0001-000000000010', 'DOMAIN', 'Domain', 'Domain', 'Domain', FALSE, TRUE, 4,
 '{"color": "#F59E0B"}'),
 ('00000000-0000-0000-0006-000010000005', '00000000-0000-0000-0001-000000000010', 'ARCHITECTURE', 'Architecture', 'Architecture', 'Architecture', FALSE, TRUE, 5,
 '{"color": "#6366F1"}'),
 ('00000000-0000-0000-0006-000010000006', '00000000-0000-0000-0001-000000000010', 'INFRASTRUCTURE', 'Infrastructure', 'Infrastructure', 'Infrastructure', FALSE, TRUE, 6,
 '{"color": "#14B8A6"}'),
 ('00000000-0000-0000-0006-000010000007', '00000000-0000-0000-0001-000000000010', 'SECURITY', 'Security', 'Security', 'Security', FALSE, TRUE, 7,
 '{"color": "#DC2626"}'),
 ('00000000-0000-0000-0006-000010000008', '00000000-0000-0000-0001-000000000010', 'COMPLIANCE', 'Compliance', 'Compliance', 'Compliance', FALSE, TRUE, 8,
 '{"color": "#EA580C"}'),
 ('00000000-0000-0000-0006-000010000009', '00000000-0000-0000-0001-000000000010', 'DESIGN_PATTERN', 'Design Pattern', 'Design Pattern', 'Design Pattern', FALSE, TRUE, 9,
 '{"color": "#7C3AED"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- TASK_STATUS (Task Status) — Group 11
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000011000005', '00000000-0000-0000-0001-000000000011', 'BACKLOG', 'Backlog', 'Backlog', 'Backlog', FALSE, TRUE, 0,
 '{"color": "#94A3B8", "kanbanColumn": 0}'),
 ('00000000-0000-0000-0006-000011000001', '00000000-0000-0000-0001-000000000011', 'TODO', 'To Do', 'To Do', 'To Do', TRUE, TRUE, 1,
 '{"color": "#6B7280", "kanbanColumn": 1}'),
 ('00000000-0000-0000-0006-000011000002', '00000000-0000-0000-0001-000000000011', 'IN_PROGRESS', 'In Progress', 'In Progress', 'In Progress', FALSE, TRUE, 2,
 '{"color": "#F59E0B", "kanbanColumn": 2}'),
 ('00000000-0000-0000-0006-000011000003', '00000000-0000-0000-0001-000000000011', 'DONE', 'Done', 'Done', 'Done', FALSE, TRUE, 3,
 '{"color": "#10B981", "kanbanColumn": 3}'),
 ('00000000-0000-0000-0006-000011000004', '00000000-0000-0000-0001-000000000011', 'BLOCKED', 'Blocked', 'Blocked', 'Blocked', FALSE, TRUE, 4,
 '{"color": "#EF4444", "kanbanColumn": 2}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- TASK_PRIORITY (Task Priority) — Group 12
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000012000001', '00000000-0000-0000-0001-000000000012', 'LOW', 'Low', 'Low', 'Low', FALSE, TRUE, 1,
 '{"color": "#6B7280", "icon": "arrow-down"}'),
 ('00000000-0000-0000-0006-000012000002', '00000000-0000-0000-0001-000000000012', 'MEDIUM', 'Medium', 'Medium', 'Medium', TRUE, TRUE, 2,
 '{"color": "#F59E0B", "icon": "minus"}'),
 ('00000000-0000-0000-0006-000012000003', '00000000-0000-0000-0001-000000000012', 'HIGH', 'High', 'High', 'High', FALSE, TRUE, 3,
 '{"color": "#EF4444", "icon": "arrow-up"}'),
 ('00000000-0000-0000-0006-000012000004', '00000000-0000-0000-0001-000000000012', 'URGENT', 'Urgent', 'Urgent', 'Critical', FALSE, TRUE, 4,
 '{"color": "#DC2626", "icon": "alert-triangle"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- RELATIONSHIP_TYPE (Relationship Type) — Group 14
-- C# RelationType enum: Supersedes=0, EvolvesFrom=1, Extends=2, ConflictsWith=3, DependsOn=4, Implements=5, Replaces=6, Affects=7
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000014000001', '00000000-0000-0000-0001-000000000014', 'SUPERSEDES', 'Supersedes', 'Supersedes', 'Supersedes', FALSE, TRUE, 1,
 '{"reverseCode": "SUPERSEDED_BY"}'),
 ('00000000-0000-0000-0006-000014000002', '00000000-0000-0000-0001-000000000014', 'EVOLVES_FROM', 'Evolves From', 'Evolves From', 'Evolves From', FALSE, TRUE, 2,
 '{"reverseCode": "EVOLVED_TO"}'),
 ('00000000-0000-0000-0006-000014000003', '00000000-0000-0000-0001-000000000014', 'EXTENDS', 'Extends', 'Extends', 'Extends', FALSE, TRUE, 3,
 '{"reverseCode": "EXTENDED_BY"}'),
 ('00000000-0000-0000-0006-000014000004', '00000000-0000-0000-0001-000000000014', 'CONFLICTS_WITH', 'Conflicts With', 'Conflicts With', 'Conflicts With', FALSE, TRUE, 4,
 '{"bidirectional": true}'),
 ('00000000-0000-0000-0006-000014000005', '00000000-0000-0000-0001-000000000014', 'DEPENDS_ON', 'Depends On', 'Depends On', 'Depends On', FALSE, TRUE, 5,
 '{"reverseCode": "DEPENDED_BY"}'),
 ('00000000-0000-0000-0006-000014000006', '00000000-0000-0000-0001-000000000014', 'IMPLEMENTS', 'Implements', 'Implements', 'Implements', FALSE, TRUE, 6,
 '{"reverseCode": "IMPLEMENTED_BY"}'),
 ('00000000-0000-0000-0006-000014000007', '00000000-0000-0000-0001-000000000014', 'REPLACES', 'Replaces', 'Replaces', 'Replaces', FALSE, TRUE, 7,
 '{"reverseCode": "REPLACED_BY"}'),
 ('00000000-0000-0000-0006-000014000008', '00000000-0000-0000-0001-000000000014', 'AFFECTS', 'Affects', 'Affects', 'Affects', FALSE, TRUE, 8,
 '{"reverseCode": "AFFECTED_BY"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- SIGNOFF_DECISION (SignOff Decision) — Group 15
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000015000001', '00000000-0000-0000-0001-000000000015', 'PENDING', 'Pending', 'Pending', 'Pending', TRUE, TRUE, 1,
 '{"color": "#6B7280"}'),
 ('00000000-0000-0000-0006-000015000002', '00000000-0000-0000-0001-000000000015', 'APPROVED', 'Approved', 'Approved', 'Approve', FALSE, TRUE, 2,
 '{"color": "#10B981"}'),
 ('00000000-0000-0000-0006-000015000003', '00000000-0000-0000-0001-000000000015', 'REJECTED', 'Rejected', 'Rejected', 'Rejected', FALSE, TRUE, 3,
 '{"color": "#EF4444"}'),
 ('00000000-0000-0000-0006-000015000004', '00000000-0000-0000-0001-000000000015', 'CONDITIONAL', 'Conditional', 'Conditional', 'Conditional Approval', FALSE, TRUE, 4,
 '{"color": "#F59E0B"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- PERSON_TYPE (Person Type) — Group 16
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000016000001', '00000000-0000-0000-0001-000000000016', 'INTERNAL', 'Internal', 'Internal', 'Internal', TRUE, TRUE, 1,
 '{"canLogin": true}'),
 ('00000000-0000-0000-0006-000016000002', '00000000-0000-0000-0001-000000000016', 'EXTERNAL', 'External', 'External', 'External', FALSE, TRUE, 2,
 '{"canLogin": false}'),
 ('00000000-0000-0000-0006-000016000003', '00000000-0000-0000-0001-000000000016', 'SYSTEM', 'System', 'System', 'System', FALSE, TRUE, 3,
 '{"canLogin": true, "isAI": true}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- ROLE_TYPE (Role Type) — Group 17
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000017000001', '00000000-0000-0000-0001-000000000017', 'SYSTEM', 'System', 'System', 'System', FALSE, TRUE, 1,
 '{"scope": "global"}'),
 ('00000000-0000-0000-0006-000017000002', '00000000-0000-0000-0001-000000000017', 'PROJECT', 'Project', 'Project', 'Project', TRUE, TRUE, 2,
 '{"scope": "project"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- PROJECT_STATUS (Project Status) — Group 19
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000019000001', '00000000-0000-0000-0001-000000000019', 'PLANNING', 'Planning', 'Planning', 'Planning', TRUE, TRUE, 1,
 '{"color": "#6B7280"}'),
 ('00000000-0000-0000-0006-000019000002', '00000000-0000-0000-0001-000000000019', 'ACTIVE', 'Active', 'Active', 'Active', FALSE, TRUE, 2,
 '{"color": "#10B981"}'),
 ('00000000-0000-0000-0006-000019000003', '00000000-0000-0000-0001-000000000019', 'CONCLUDED', 'Concluded', 'Concluded', 'Concluded', FALSE, TRUE, 3,
 '{"color": "#F59E0B"}'),
 ('00000000-0000-0000-0006-000019000004', '00000000-0000-0000-0001-000000000019', 'ARCHIVED', 'Archived', 'Archived', 'Archived', FALSE, TRUE, 4,
 '{"color": "#6B7280"}'),
 ('00000000-0000-0000-0006-000019000005', '00000000-0000-0000-0001-000000000019', 'SUSPENDED', 'Suspended', 'Suspended', 'Suspended', FALSE, TRUE, 5,
 '{"color": "#EF4444"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 is_default = EXCLUDED.is_default,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- DIRECT_MESSAGE_STATUS (DirectMessage Status) — Group 22
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000022000001', '00000000-0000-0000-0001-000000000022', 'ACTIVE', 'Active', 'Active', 'Active', TRUE, TRUE, 1,
 '{"color": "#10B981"}'),
 ('00000000-0000-0000-0006-000022000002', '00000000-0000-0000-0001-000000000022', 'CONCLUDED', 'Concluded', 'Concluded', 'Concluded', FALSE, TRUE, 2,
 '{"color": "#F59E0B"}'),
 ('00000000-0000-0000-0006-000022000003', '00000000-0000-0000-0001-000000000022', 'ARCHIVED', 'Archived', 'Archived', 'Archived', FALSE, TRUE, 3,
 '{"color": "#6B7280"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 is_default = EXCLUDED.is_default,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- WORKING_DAY_TYPE (Workday Type) — Group 23
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000023000001', '00000000-0000-0000-0001-000000000023', 'WORKDAY', 'Workday', 'Workday', 'Workday', TRUE, TRUE, 1,
 '{"isWorkingDay": true}'),
 ('00000000-0000-0000-0006-000023000002', '00000000-0000-0000-0001-000000000023', 'OFFDAY', 'Offday', 'Offday', 'Offday', FALSE, TRUE, 2,
 '{"isWorkingDay": false}'),
 ('00000000-0000-0000-0006-000023000003', '00000000-0000-0000-0001-000000000023', 'HOLIDAY', 'Holiday', 'Holiday', 'Holiday', FALSE, TRUE, 3,
 '{"isWorkingDay": false}'),
 ('00000000-0000-0000-0006-000023000004', '00000000-0000-0000-0001-000000000023', 'EXCEPTION', 'Exception', 'Exception', 'Exception Day', FALSE, TRUE, 4,
 '{"isWorkingDay": true}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 is_default = EXCLUDED.is_default,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- AUDIT_ACTOR_TYPE (Audit Type) — Group 24
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000024000001', '00000000-0000-0000-0001-000000000024', 'USER', 'User', 'User', 'User', TRUE, TRUE, 1,
 '{"isSystem": false}'),
 ('00000000-0000-0000-0006-000024000002', '00000000-0000-0000-0001-000000000024', 'SYSTEM', 'System', 'System', 'System', FALSE, TRUE, 2,
 '{"isSystem": true}'),
 ('00000000-0000-0000-0006-000024000003', '00000000-0000-0000-0001-000000000024', 'AI', 'AI', 'AI', 'AI', FALSE, TRUE, 3,
 '{"isSystem": true, "isAi": true}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 is_default = EXCLUDED.is_default,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- TOPIC_STATUS (Topic Status) — Group 20
-- C# TopicStatus enum: Open=0, Closed=1, Archived=2
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000020000001', '00000000-0000-0000-0001-000000000020', 'OPEN', 'Open', 'Open', 'Open', TRUE, TRUE, 1,
 '{"color": "#10B981", "icon": "message-circle"}'),
 ('00000000-0000-0000-0006-000020000002', '00000000-0000-0000-0001-000000000020', 'CLOSED', 'Closed', 'Closed', 'Closed', FALSE, TRUE, 2,
 '{"color": "#6B7280", "icon": "check-circle"}'),
 ('00000000-0000-0000-0006-000020000003', '00000000-0000-0000-0001-000000000020', 'ARCHIVED', 'Archived', 'Archived', 'Archived', FALSE, TRUE, 3,
 '{"color": "#6B7280", "icon": "archive"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- CHANNEL_STATUS (Channel Status) — Group 21
-- C# ChannelStatus enum: Active=0, Concluded=1, Archived=2
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 ('00000000-0000-0000-0006-000021000001', '00000000-0000-0000-0001-000000000021', 'ACTIVE', 'Active', 'Active', 'Active', TRUE, TRUE, 1,
 '{"color": "#10B981", "icon": "message-circle"}'),
 ('00000000-0000-0000-0006-000021000002', '00000000-0000-0000-0001-000000000021', 'CONCLUDED', 'Concluded', 'Concluded', 'Concluded', FALSE, TRUE, 2,
 '{"color": "#F59E0B", "icon": "check-circle"}'),
 ('00000000-0000-0000-0006-000021000003', '00000000-0000-0000-0001-000000000021', 'ARCHIVED', 'Archived', 'Archived', 'Archived', FALSE, TRUE, 3,
 '{"color": "#6B7280", "icon": "archive"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- ARTIFACT_TYPE (Artifact Type) — Group 18
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, name_en, name_ko, is_default, is_system, sort_order, attributes)
VALUES
 -- Code
 ('00000000-0000-0000-0006-000018000001', '00000000-0000-0000-0001-000000000018', 'ENTITY', 'Entity', 'Entity', 'Entity', FALSE, TRUE, 1,
 '{"category": "Code", "icon": "cube", "extension": ".cs"}'),
 ('00000000-0000-0000-0006-000018000002', '00000000-0000-0000-0001-000000000018', 'DTO', 'DTO', 'DTO', 'DTO', FALSE, TRUE, 2,
 '{"category": "Code", "icon": "package", "extension": ".cs"}'),
 ('00000000-0000-0000-0006-000018000003', '00000000-0000-0000-0001-000000000018', 'REPOSITORY', 'Repository', 'Repository', 'Repository', FALSE, TRUE, 3,
 '{"category": "Code", "icon": "database", "extension": ".cs"}'),
 ('00000000-0000-0000-0006-000018000004', '00000000-0000-0000-0001-000000000018', 'SERVICE', 'Service', 'Service', 'Service', FALSE, TRUE, 4,
 '{"category": "Code", "icon": "server", "extension": ".cs"}'),
 ('00000000-0000-0000-0006-000018000005', '00000000-0000-0000-0001-000000000018', 'CONTROLLER', 'Controller', 'Controller', 'Controller', FALSE, TRUE, 5,
 '{"category": "Code", "icon": "globe", "extension": ".cs"}'),
 ('00000000-0000-0000-0006-000018000006', '00000000-0000-0000-0001-000000000018', 'TEST', 'Test', 'Test', 'Test', FALSE, TRUE, 6,
 '{"category": "Code", "icon": "beaker", "extension": ".cs"}'),

 -- Database
 ('00000000-0000-0000-0006-000018000010', '00000000-0000-0000-0001-000000000018', 'TABLE', 'Table', 'Table', 'Table', FALSE, TRUE, 10,
 '{"category": "Database", "icon": "table", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000011', '00000000-0000-0000-0001-000000000018', 'VIEW', 'View', 'View', 'View', FALSE, TRUE, 11,
 '{"category": "Database", "icon": "eye", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000012', '00000000-0000-0000-0001-000000000018', 'STORED_PROCEDURE', 'Stored Procedure', 'Stored Procedure', 'Stored Procedure', FALSE, TRUE, 12,
 '{"category": "Database", "icon": "terminal", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000013', '00000000-0000-0000-0001-000000000018', 'FUNCTION', 'Function', 'Function', 'Function', FALSE, TRUE, 13,
 '{"category": "Database", "icon": "function", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000014', '00000000-0000-0000-0001-000000000018', 'TRIGGER', 'Trigger', 'Trigger', 'Trigger', FALSE, TRUE, 14,
 '{"category": "Database", "icon": "zap", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000015', '00000000-0000-0000-0001-000000000018', 'INDEX', 'Index', 'Index', 'Index', FALSE, TRUE, 15,
 '{"category": "Database", "icon": "list", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000016', '00000000-0000-0000-0001-000000000018', 'QUERY', 'Query', 'Query', 'Query', FALSE, TRUE, 16,
 '{"category": "Database", "icon": "search", "extension": ".sql", "description": "Named query for Dapper"}'),

 -- Other
 ('00000000-0000-0000-0006-000018000020', '00000000-0000-0000-0001-000000000018', 'MIGRATION', 'Migration', 'Migration', 'Migration', FALSE, TRUE, 20,
 '{"category": "Other", "icon": "git-branch", "extension": ".sql"}'),
 ('00000000-0000-0000-0006-000018000021', '00000000-0000-0000-0001-000000000018', 'DOCUMENT', 'Document', 'Document', 'Document', FALSE, TRUE, 21,
 '{"category": "Other", "icon": "file-text", "extension": ".md"}'),
 ('00000000-0000-0000-0006-000018000022', '00000000-0000-0000-0001-000000000018', 'CONFIG', 'Config', 'Config', 'Config', FALSE, TRUE, 22,
 '{"category": "Other", "icon": "settings", "extension": ".json"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, name_en = EXCLUDED.name_en, name_ko = EXCLUDED.name_ko,
 sort_order = EXCLUDED.sort_order, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- SYS_CONFIG (System Config) — Group 100
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, description, is_system, attributes)
VALUES
 ('00000000-0000-0000-0006-000100000001', '00000000-0000-0000-0001-000000000100', 'DEFAULT_LOCALE', 'Default Locale', 'Default Locale', TRUE,
 '{"value": "en-US", "type": "string"}'),
 ('00000000-0000-0000-0006-000100000002', '00000000-0000-0000-0001-000000000100', 'DEFAULT_TIMEZONE', 'Default Timezone', 'Default Timezone', TRUE,
 '{"value": "Asia/Seoul", "type": "string"}'),
 ('00000000-0000-0000-0006-000100000003', '00000000-0000-0000-0001-000000000100', 'SESSION_TIMEOUT_MINUTES', 'Session Timeout', 'Session Timeout (Minutes)', TRUE,
 '{"value": 30, "type": "integer", "min": 5, "max": 480}'),
 ('00000000-0000-0000-0006-000100000004', '00000000-0000-0000-0001-000000000100', 'PASSWORD_MIN_LENGTH', 'Password Min Length', 'Password Minimum Length', TRUE,
 '{"value": 8, "type": "integer", "min": 6, "max": 32}'),
 ('00000000-0000-0000-0006-000100000005', '00000000-0000-0000-0001-000000000100', 'MAX_UPLOAD_SIZE_MB', 'Max Upload Size', 'Maximum Upload Size (MB)', TRUE,
 '{"value": 10, "type": "integer", "min": 1, "max": 100}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, description = EXCLUDED.description, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- AI_CONFIG (AI Config) — Group 101
-- -----------------------------------------------------------------------------
INSERT INTO codes (id, group_code_id, code, name, description, is_system, attributes)
VALUES
 ('00000000-0000-0000-0006-000101000001', '00000000-0000-0000-0001-000000000101', 'AI_PROVIDER', 'AI Provider', 'AI Service Provider', TRUE,
 '{"value": "ollama", "type": "string", "options": ["ollama", "openai", "anthropic"]}'),
 ('00000000-0000-0000-0006-000101000002', '00000000-0000-0000-0001-000000000101', 'AI_MODEL', 'AI Model', 'AI Model', TRUE,
 '{"value": "llama3.2", "type": "string"}'),
 ('00000000-0000-0000-0006-000101000003', '00000000-0000-0000-0001-000000000101', 'AI_ENDPOINT', 'AI Endpoint', 'AI API Endpoint', TRUE,
 '{"value": "http://localhost:11434", "type": "string"}'),
 ('00000000-0000-0000-0006-000101000004', '00000000-0000-0000-0001-000000000101', 'AI_ENABLED', 'AI Enabled', 'AI Feature Enabled', TRUE,
 '{"value": true, "type": "boolean"}')
ON CONFLICT (group_code_id, code) DO UPDATE SET
 name = EXCLUDED.name, description = EXCLUDED.description, attributes = EXCLUDED.attributes, updated_at = NOW();

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 cnt INT;
BEGIN
 SELECT COUNT(*) INTO cnt FROM codes;
 RAISE NOTICE 'Codes inserted/updated: %', cnt;
END $$;

-- <<< end bundled from scripts/db/51-data-codes.sql <<<

\echo '[52] Inserting Roles...'

-- >>> bundled from scripts/db/52-data-roles.sql >>>
-- =============================================================================
-- 52-data-roles.sql
-- System Role
-- =============================================================================

-- Well-known Role IDs
-- Admin: 00000000-0000-0000-0002-000000000001
-- ProductOwner: 00000000-0000-0000-0002-000000000002
-- DomainExpert: 00000000-0000-0000-0002-000000000003
-- Developer: 00000000-0000-0000-0002-000000000004
-- Reviewer: 00000000-0000-0000-0002-000000000005
-- QATester: 00000000-0000-0000-0002-000000000006
INSERT INTO roles (id, code, name, description, type, role_type, is_system, is_default, sort_order)
VALUES
 -- System Role (Global)
 ('00000000-0000-0000-0002-000000000001', 'ADMIN', 'Admin',
 'Administrator with full system-wide management permissions', 5, 'SYSTEM', TRUE, FALSE, 1), -- 5=Admin

 -- Project Role
 ('00000000-0000-0000-0002-000000000002', 'PRODUCT_OWNER', 'Product Owner',
 'Project owner with requirement definition, spec approval, and team management permissions', 0, 'PROJECT', TRUE, FALSE, 2), -- 0=ProductOwner

 ('00000000-0000-0000-0002-000000000003', 'DOMAIN_EXPERT', 'Domain Expert',
 'Domain expert with requirement authoring, glossary definition, and spec review permissions', 1, 'PROJECT', TRUE, FALSE, 3), -- 1=DomainExpert

 ('00000000-0000-0000-0002-000000000004', 'DEVELOPER', 'Developer',
 'Developer with spec authoring, discussion participation, and code generation permissions', 2, 'PROJECT', TRUE, TRUE, 4), -- 2=Developer (Default Role)

 ('00000000-0000-0000-0002-000000000005', 'REVIEWER', 'Reviewer',
 'Reviewer with spec and requirement review approval permissions', 3, 'PROJECT', TRUE, FALSE, 5), -- 3=Reviewer

 ('00000000-0000-0000-0002-000000000006', 'QA_TESTER', 'QA Tester',
 'QA tester with read-only access and test-case authoring permissions', 4, 'PROJECT', TRUE, FALSE, 6) -- 4=QATester

ON CONFLICT (id) DO UPDATE SET
 code = EXCLUDED.code,
 name = EXCLUDED.name,
 description = EXCLUDED.description,
 type = EXCLUDED.type,
 role_type = EXCLUDED.role_type,
 is_system = EXCLUDED.is_system,
 is_default = EXCLUDED.is_default,
 sort_order = EXCLUDED.sort_order,
 updated_at = NOW();

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 cnt INT;
BEGIN
 SELECT COUNT(*) INTO cnt FROM roles WHERE is_system = TRUE;
 RAISE NOTICE 'System Roles inserted/updated: %', cnt;
END $$;

-- <<< end bundled from scripts/db/52-data-roles.sql <<<

\echo '[53] Inserting Permissions...'

-- >>> bundled from scripts/db/53-data-permissions.sql >>>
-- =============================================================================
-- 53-data-permissions.sql
-- System Permission
-- =============================================================================

-- Permission : {resource}:{action}
-- Resources: spec, conversation, requirement, generation, glossary, user, role, audit, project, task
-- Actions: create, read, update, delete, approve, lock, execute, rollback, manage, assign, post, close, deprecate

INSERT INTO permissions (id, code, name, description, resource_type, action, is_system)
VALUES
 -- Spec Permission (6)
 ('00000000-0000-0000-0003-000000000001', 'spec:create', 'Create Spec', 'Spec Create', 'spec', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000002', 'spec:read', 'Read Spec', 'Spec Read', 'spec', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000003', 'spec:update', 'Update Spec', 'Spec Update', 'spec', 'update', TRUE),
 ('00000000-0000-0000-0003-000000000004', 'spec:delete', 'Delete Spec', 'Spec Delete', 'spec', 'delete', TRUE),
 ('00000000-0000-0000-0003-000000000005', 'spec:approve', 'Approve Spec', 'Spec Approve', 'spec', 'approve', TRUE),
 ('00000000-0000-0000-0003-000000000006', 'spec:lock', 'Lock Spec', 'Spec Lock', 'spec', 'lock', TRUE),

 -- Conversation Permission (4)
 ('00000000-0000-0000-0003-000000000011', 'conversation:create', 'Create Conversation', 'Conversation Create', 'conversation', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000012', 'conversation:read', 'Read Conversation', 'Conversation Read', 'conversation', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000013', 'conversation:post', 'Post to Conversation', 'Post conversation messages', 'conversation', 'post', TRUE),
 ('00000000-0000-0000-0003-000000000014', 'conversation:close', 'Close Conversation', 'Conversation Concluded', 'conversation', 'close', TRUE),

 -- Requirement Permission (4)
 ('00000000-0000-0000-0003-000000000021', 'requirement:create', 'Create Requirement', 'Requirement Create', 'requirement', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000022', 'requirement:read', 'Read Requirement', 'Requirement Read', 'requirement', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000023', 'requirement:update', 'Update Requirement', 'Requirement Update', 'requirement', 'update', TRUE),
 ('00000000-0000-0000-0003-000000000024', 'requirement:delete', 'Delete Requirement', 'Requirement Delete', 'requirement', 'delete', TRUE),

 -- Generation Permission (2)
 ('00000000-0000-0000-0003-000000000031', 'generation:execute', 'Execute Generation', 'Execute code generation', 'generation', 'execute', TRUE),
 ('00000000-0000-0000-0003-000000000032', 'generation:rollback', 'Rollback Generation', 'Rollback code generation', 'generation', 'rollback', TRUE),

 -- Glossary Permission (4)
 ('00000000-0000-0000-0003-000000000041', 'glossary:create', 'Create Glossary', 'Glossary Create', 'glossary', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000042', 'glossary:read', 'Read Glossary', 'Glossary Read', 'glossary', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000043', 'glossary:update', 'Update Glossary', 'Glossary Update', 'glossary', 'update', TRUE),
 ('00000000-0000-0000-0003-000000000044', 'glossary:deprecate', 'Deprecate Glossary', 'Glossary Deprecate', 'glossary', 'deprecate', TRUE),

 -- User Permission (1)
 ('00000000-0000-0000-0003-000000000051', 'user:manage', 'Manage Users', 'User Manage', 'user', 'manage', TRUE),

 -- Role Permission (1)
 ('00000000-0000-0000-0003-000000000061', 'role:assign', 'Assign Roles', 'Role Assign', 'role', 'assign', TRUE),

 -- Audit Permission (1)
 ('00000000-0000-0000-0003-000000000062', 'audit:read', 'Read Audit Logs', 'Audit Log Read', 'audit', 'read', TRUE),

 -- Project Permission (4)
 ('00000000-0000-0000-0003-000000000071', 'project:create', 'Create Project', 'Project Create', 'project', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000072', 'project:read', 'Read Project', 'Project Read', 'project', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000073', 'project:update', 'Update Project', 'Project Update', 'project', 'update', TRUE),
 ('00000000-0000-0000-0003-000000000074', 'project:delete', 'Delete Project', 'Project Delete', 'project', 'delete', TRUE),

 -- Task Permission (4)
 ('00000000-0000-0000-0003-000000000081', 'task:create', 'Create Task', 'Task Create', 'task', 'create', TRUE),
 ('00000000-0000-0000-0003-000000000082', 'task:read', 'Read Task', 'Task Read', 'task', 'read', TRUE),
 ('00000000-0000-0000-0003-000000000083', 'task:update', 'Update Task', 'Task Update', 'task', 'update', TRUE),
 ('00000000-0000-0000-0003-000000000084', 'task:delete', 'Delete Task', 'Task Delete', 'task', 'delete', TRUE)

ON CONFLICT (code) DO UPDATE SET
 name = EXCLUDED.name,
 description = EXCLUDED.description,
 resource_type = EXCLUDED.resource_type,
 action = EXCLUDED.action,
 is_system = EXCLUDED.is_system;

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 cnt INT;
BEGIN
 SELECT COUNT(*) INTO cnt FROM permissions WHERE is_system = TRUE;
 RAISE NOTICE 'System Permissions inserted/updated: %', cnt;
END $$;

-- <<< end bundled from scripts/db/53-data-permissions.sql <<<

\echo '[54] Mapping Role-Permissions...'

-- >>> bundled from scripts/db/54-data-role-permissions.sql >>>
-- =============================================================================
-- 54-data-role-permissions.sql
-- Role-Permission
-- Source of Truth: docs/business/user-roles.md Role-Permission
-- =============================================================================

-- Role IDs (from 52-data-roles.sql)
-- ADMIN: 00000000-0000-0000-0002-000000000001
-- PRODUCT_OWNER: 00000000-0000-0000-0002-000000000002
-- DOMAIN_EXPERT: 00000000-0000-0000-0002-000000000003
-- DEVELOPER: 00000000-0000-0000-0002-000000000004
-- REVIEWER: 00000000-0000-0000-0002-000000000005
-- QA_TESTER: 00000000-0000-0000-0002-000000000006
-- -----------------------------------------------------------------------------
-- ADMIN: Permission (31)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000001'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- PRODUCT_OWNER: project:delete (30)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000002'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
 AND code NOT IN ('project:delete')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- DOMAIN_EXPERT: Spec CRU, Conversation , Requirement CRU, Glossary ,
-- Project , Task CRU (18)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000003'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
 AND code IN (
 'spec:create', 'spec:read', 'spec:update',
 'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
 'requirement:create', 'requirement:read', 'requirement:update',
 'glossary:create', 'glossary:read', 'glossary:update', 'glossary:deprecate',
 'project:read',
 'task:create', 'task:read', 'task:update'
 )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- DEVELOPER: Spec CRU, Conversation , Requirement CRU, Generation execute,
-- Glossary CRU, Project , Task CRU (18)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000004'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
 AND code IN (
 'spec:create', 'spec:read', 'spec:update',
 'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
 'requirement:create', 'requirement:read', 'requirement:update',
 'generation:execute',
 'glossary:create', 'glossary:read', 'glossary:update',
 'project:read',
 'task:create', 'task:read', 'task:update'
 )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- REVIEWER: + spec:approve + Conversation (10)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000005'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
 AND code IN (
 'spec:read', 'spec:approve',
 'conversation:create', 'conversation:read', 'conversation:post', 'conversation:close',
 'requirement:read',
 'glossary:read',
 'project:read',
 'task:read'
 )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- QA_TESTER: + Conversation create/post + Task CRU (10)
-- -----------------------------------------------------------------------------
INSERT INTO role_permissions (role_id, permission_id)
SELECT
 '00000000-0000-0000-0002-000000000006'::UUID,
 id
FROM permissions
WHERE is_system = TRUE
 AND code IN (
 'spec:read',
 'conversation:create', 'conversation:read', 'conversation:post',
 'requirement:read',
 'glossary:read',
 'project:read',
 'task:create', 'task:read', 'task:update'
 )
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Permission Read
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 role_record RECORD;
 perm_count INT;
BEGIN
 RAISE NOTICE '=== Role-Permission Mapping Summary ===';
 FOR role_record IN SELECT id, code, name FROM roles WHERE is_system = TRUE ORDER BY sort_order LOOP
 SELECT COUNT(*) INTO perm_count FROM role_permissions WHERE role_id = role_record.id;
 RAISE NOTICE '% (%): % permissions', role_record.name, role_record.code, perm_count;
 END LOOP;
END $$;

-- <<< end bundled from scripts/db/54-data-role-permissions.sql <<<

\echo '[55] Inserting System Users...'

-- >>> bundled from scripts/db/55-data-system-users.sql >>>
-- =============================================================================
-- 55-data-system-users.sql
-- System User (admin)
-- =============================================================================

-- Well-known IDs
-- Admin Person: 00000000-0000-0000-0004-000000000001
-- Admin User: 00000000-0000-0000-0005-000000000001
-- -----------------------------------------------------------------------------
-- System Persons
-- -----------------------------------------------------------------------------
INSERT INTO persons (id, display_name, email, organization, person_type)
VALUES
 -- System Manage
 ('00000000-0000-0000-0004-000000000001', 'System Administrator', 'admin@sddp.local', 'SDDP System', 'INTERNAL')

ON CONFLICT (id) DO UPDATE SET
 display_name = EXCLUDED.display_name,
 email = EXCLUDED.email,
 organization = EXCLUDED.organization,
 person_type = EXCLUDED.person_type,
 updated_at = NOW();

-- -----------------------------------------------------------------------------
-- System Users
-- : BCrypt (Admin@123!)
-- BCrypt WorkFactor=12
-- -----------------------------------------------------------------------------
INSERT INTO users (id, person_id, username, email, password_hash, display_name, is_ai, is_email_verified)
VALUES
 -- admin
 -- : Admin@123! (BCrypt WorkFactor=12)
 ('00000000-0000-0000-0005-000000000001',
 '00000000-0000-0000-0004-000000000001',
 'admin',
 'admin@sddp.local',
 '$2a$12$XwvXx1/2jN3GoZ16iFGX5eoDKUIlhzPcdBbq8JyhlgxZdIHENFjhS', -- Admin@123!
 'System Administrator',
 FALSE,
 TRUE)

ON CONFLICT (id) DO UPDATE SET
 username = EXCLUDED.username,
 email = EXCLUDED.email,
 display_name = EXCLUDED.display_name,
 password_hash = EXCLUDED.password_hash,
 is_ai = EXCLUDED.is_ai,
 is_email_verified = EXCLUDED.is_email_verified,
 updated_at = NOW();

-- -----------------------------------------------------------------------------
-- User Role Assign
-- -----------------------------------------------------------------------------

-- Role ( )
-- : Tenant(ACME well-known tenant)
DELETE FROM user_roles
WHERE user_id IN (
 '00000000-0000-0000-0005-000000000001'
)
 AND (tenant_id IS NULL OR tenant_id = '00000000-0000-0000-0000-000000000001')
 AND project_id IS NULL;

-- Admin ADMIN Role Assign ( Tenant: ACME)
INSERT INTO user_roles (user_id, role_id, tenant_id, project_id, assigned_by)
SELECT
 '00000000-0000-0000-0005-000000000001',
 '00000000-0000-0000-0002-000000000001', -- ADMIN
 '00000000-0000-0000-0010-000000000001', -- ACME tenant (single-tenant deployment)
 NULL,
 '00000000-0000-0000-0005-000000000001' -- self-assigned
WHERE NOT EXISTS (
 SELECT 1
 FROM user_roles ur
 WHERE ur.user_id = '00000000-0000-0000-0005-000000000001'
 AND ur.role_id = '00000000-0000-0000-0002-000000000001'
 AND ur.tenant_id = '00000000-0000-0000-0010-000000000001'
 AND ur.project_id IS NULL
);

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
DECLARE
 person_cnt INT;
 user_cnt INT;
 role_assign_cnt INT;
BEGIN
 SELECT COUNT(*) INTO person_cnt FROM persons WHERE person_type IN ('INTERNAL', 'SYSTEM');
 SELECT COUNT(*) INTO user_cnt FROM users;
 SELECT COUNT(*) INTO role_assign_cnt FROM user_roles;

 RAISE NOTICE 'System Persons: %, Users: %, Role Assignments: %', person_cnt, user_cnt, role_assign_cnt;
END $$;

-- <<< end bundled from scripts/db/55-data-system-users.sql <<<

\echo '[56] Inserting System Configs...'

-- >>> bundled from scripts/db/56-data-system-configs.sql >>>
-- =============================================================================
-- 56-data-system-configs.sql
-- Default System Configuration Values
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Global System Defaults (tenant_id = NULL, project_id = NULL)
-- -----------------------------------------------------------------------------

-- General Settings
INSERT INTO system_configs (tenant_id, project_id, config_group, config_key, config_value, value_type, display_name, description, is_system, sort_order)
VALUES
 (NULL, NULL, 'general', 'siteName', 'SDDP', 'string', 'Site Name', 'Application display name', TRUE, 1),
 (NULL, NULL, 'general', 'siteUrl', 'https://sddp.example.com', 'string', 'Site URL', 'Public URL of the application', TRUE, 2),
 (NULL, NULL, 'general', 'adminEmail', 'admin@example.com', 'string', 'Admin Email', 'Administrator contact email', FALSE, 3),
 (NULL, NULL, 'general', 'defaultLocale', 'ko-KR', 'string', 'Default Locale', 'Default language and region', FALSE, 4),
 (NULL, NULL, 'general', 'defaultTimezone', 'Asia/Seoul', 'string', 'Default Timezone', 'Default timezone for new users', FALSE, 5)
ON CONFLICT DO NOTHING; -- relies on uq_system_configs_scope_group_key (COALESCE-based unique index)

-- Auth Settings
INSERT INTO system_configs (tenant_id, project_id, config_group, config_key, config_value, value_type, display_name, description, min_value, max_value, is_system, sort_order)
VALUES
 (NULL, NULL, 'auth', 'sessionTimeout', '30', 'number', 'Session Timeout (minutes)', 'Idle session timeout in minutes', 5, 1440, FALSE, 1),
 (NULL, NULL, 'auth', 'strongPassword', 'true', 'boolean', 'Require Strong Passwords', 'Minimum 8 characters with mixed case, numbers, and symbols', NULL, NULL, FALSE, 2),
 (NULL, NULL, 'auth', 'passwordMinLength', '8', 'number', 'Minimum Password Length', 'Minimum required password length', 6, 128, FALSE, 3),
 (NULL, NULL, 'auth', 'twoFactorAuth', 'false', 'boolean', 'Two-Factor Authentication', 'Require 2FA for all users', NULL, NULL, FALSE, 4),
 (NULL, NULL, 'auth', 'ssoEnabled', 'false', 'boolean', 'SSO (Single Sign-On)', 'Enable SAML/OAuth authentication', NULL, NULL, FALSE, 5),
 (NULL, NULL, 'auth', 'maxFailedLogins', '5', 'number', 'Max Failed Logins', 'Lock account after N failed attempts', 3, 10, FALSE, 6),
 (NULL, NULL, 'auth', 'lockoutDuration', '15', 'number', 'Lockout Duration (minutes)', 'Account lockout duration', 5, 1440, FALSE, 7)
ON CONFLICT DO NOTHING; -- relies on uq_system_configs_scope_group_key (COALESCE-based unique index)

-- Storage Settings (read-only system values)
INSERT INTO system_configs (tenant_id, project_id, config_group, config_key, config_value, value_type, display_name, description, is_readonly, is_system, sort_order)
VALUES
 (NULL, NULL, 'storage', 'database',
 CONCAT('PostgreSQL (Tablespace: ', COALESCE(NULLIF(current_setting('sddp.storage_tablespace', true), ''), 'pg_default'), ')'),
 'string', 'Database', 'Database system and bound tablespace', TRUE, TRUE, 1),
 (NULL, NULL, 'storage', 'storageUsed', '0', 'number', 'Storage Used (GB)', 'Current storage usage', TRUE, TRUE, 2),
 (NULL, NULL, 'storage', 'storageLimit',
 COALESCE(NULLIF(current_setting('sddp.storage_quota_gb', true), ''), '20'),
 'number', 'Storage Limit (GB)', 'Quota bound to storage tablespace', TRUE, TRUE, 3),
 (NULL, NULL, 'storage', 'maxUploadSize', '50', 'number', 'Max Upload Size (MB)', 'Maximum file upload size', FALSE, FALSE, 4)
ON CONFLICT DO NOTHING; -- relies on uq_system_configs_scope_group_key (COALESCE-based unique index)

-- Performance Settings
INSERT INTO system_configs (tenant_id, project_id, config_group, config_key, config_value, value_type, display_name, description, is_system, sort_order)
VALUES
 (NULL, NULL, 'performance', 'cacheEnabled', 'true', 'boolean', 'Enable Caching', 'Cache frequently accessed data', FALSE, 1),
 (NULL, NULL, 'performance', 'cacheTtlSeconds', '300', 'number', 'Cache TTL (seconds)', 'Default cache time-to-live', FALSE, 2),
 (NULL, NULL, 'performance', 'cdnEnabled', 'false', 'boolean', 'CDN', 'Serve static assets from CDN', FALSE, 3),
 (NULL, NULL, 'performance', 'compressionEnabled', 'true', 'boolean', 'Response Compression', 'Compress API responses', FALSE, 4),
 (NULL, NULL, 'performance', 'rateLimitEnabled', 'false', 'boolean', 'Rate Limiting', 'Enable API rate limiting (not yet enforced)', FALSE, 5),
 (NULL, NULL, 'performance', 'rateLimitPerMinute', '100', 'number', 'Rate Limit (per minute)', 'Max API requests per minute (not yet enforced)', FALSE, 6)
ON CONFLICT DO NOTHING; -- relies on uq_system_configs_scope_group_key (COALESCE-based unique index)

-- AI Agent Settings
INSERT INTO system_configs (tenant_id, project_id, config_group, config_key, config_value, value_type, display_name, description, is_sensitive, sort_order)
VALUES
 (NULL, NULL, 'aiAgent', 'enabled', 'false', 'boolean', 'AI Agent Enabled', 'Enable AI-assisted features', FALSE, 1),
 (NULL, NULL, 'aiAgent', 'provider', 'Ollama', 'string', 'AI Provider', 'AI service provider (Ollama, OpenAI, Azure)', FALSE, 2),
 (NULL, NULL, 'aiAgent', 'model', 'llama3.2', 'string', 'AI Model', 'Model name/identifier', FALSE, 3),
 (NULL, NULL, 'aiAgent', 'endpoint', 'http://localhost:11434', 'string', 'AI Endpoint', 'API endpoint URL', FALSE, 4),
 (NULL, NULL, 'aiAgent', 'apiKey', '', 'string', 'API Key', 'AI service API key', TRUE, 5),
 (NULL, NULL, 'aiAgent', 'maxTokens', '4096', 'number', 'Max Tokens', 'Maximum tokens per request', FALSE, 6),
 (NULL, NULL, 'aiAgent', 'temperature', '0.7', 'number', 'Temperature', 'Response randomness (0-2)', FALSE, 7)
ON CONFLICT DO NOTHING; -- relies on uq_system_configs_scope_group_key (COALESCE-based unique index)

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Default system configurations inserted';
END $$;

-- <<< end bundled from scripts/db/56-data-system-configs.sql <<<

\echo '[57] Syncing Legacy-Code Bridge...'

-- >>> bundled from scripts/db/57-sync-legacy-code-bridge.sql >>>
-- =============================================================================
-- 57-sync-legacy-code-bridge.sql
-- Legacy (status/actor_type/day_type) codes FK
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Helper Functions
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION sddp_normalize_legacy_code_value(p_value TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
BEGIN
 IF p_value IS NULL THEN
 RETURN NULL;
 END IF;

 RETURN UPPER(REPLACE(REPLACE(TRIM(p_value), '-', '_'), ' ', '_'));
END;
$$;

CREATE OR REPLACE FUNCTION sddp_alias_legacy_code_value(p_group_code VARCHAR, p_normalized_value TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
BEGIN
 IF p_normalized_value IS NULL THEN
 RETURN NULL;
 END IF;

 IF p_group_code IN ('PROJECT_STATUS', 'DIRECT_MESSAGE_STATUS') THEN
 IF p_normalized_value IN ('DONE', 'CLOSED') THEN
 RETURN 'CONCLUDED';
 END IF;
 ELSIF p_group_code = 'WORKING_DAY_TYPE' THEN
 IF p_normalized_value IN ('WORK', 'WEEKDAY') THEN
 RETURN 'WORKDAY';
 END IF;
 IF p_normalized_value IN ('OFF', 'WEEKEND') THEN
 RETURN 'OFFDAY';
 END IF;
 ELSIF p_group_code = 'AUDIT_ACTOR_TYPE' THEN
 IF p_normalized_value = 'AGENT' THEN
 RETURN 'AI';
 END IF;
 END IF;

 RETURN p_normalized_value;
END;
$$;

CREATE OR REPLACE FUNCTION sddp_resolve_code_id(p_group_code VARCHAR, p_legacy_value VARCHAR)
RETURNS UUID
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
 v_normalized TEXT;
 v_code_id UUID;
BEGIN
 v_normalized := sddp_alias_legacy_code_value(
 p_group_code,
 sddp_normalize_legacy_code_value(p_legacy_value)
 );

 IF v_normalized IS NULL THEN
 RETURN NULL;
 END IF;

 SELECT c.id
 INTO v_code_id
 FROM codes c
 JOIN group_codes gc ON gc.id = c.group_code_id
 WHERE gc.code = p_group_code
 AND c.code = v_normalized;

 IF v_code_id IS NULL THEN
 RAISE EXCEPTION 'Cannot resolve legacy value "%" for group "%"', p_legacy_value, p_group_code;
 END IF;

 RETURN v_code_id;
END;
$$;

CREATE OR REPLACE FUNCTION sddp_resolve_code_value(p_group_code VARCHAR, p_code_id UUID)
RETURNS VARCHAR
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
 v_code VARCHAR(50);
BEGIN
 IF p_code_id IS NULL THEN
 RETURN NULL;
 END IF;

 SELECT c.code
 INTO v_code
 FROM codes c
 JOIN group_codes gc ON gc.id = c.group_code_id
 WHERE gc.code = p_group_code
 AND c.id = p_code_id;

 IF v_code IS NULL THEN
 RAISE EXCEPTION 'Code id "%" does not belong to group "%"', p_code_id, p_group_code;
 END IF;

 RETURN v_code;
END;
$$;

-- -----------------------------------------------------------------------------
-- Backfill Existing Rows
-- -----------------------------------------------------------------------------
UPDATE projects
 SET status_code_id = sddp_resolve_code_id('PROJECT_STATUS', status),
 status = LOWER(sddp_resolve_code_value('PROJECT_STATUS', sddp_resolve_code_id('PROJECT_STATUS', status)));

UPDATE direct_messages
 SET status_code_id = sddp_resolve_code_id('DIRECT_MESSAGE_STATUS', status),
 status = INITCAP(LOWER(sddp_resolve_code_value('DIRECT_MESSAGE_STATUS', sddp_resolve_code_id('DIRECT_MESSAGE_STATUS', status))));

UPDATE audit_logs
 SET actor_type_code_id = sddp_resolve_code_id('AUDIT_ACTOR_TYPE', COALESCE(actor_type, 'USER')),
 actor_type = UPPER(sddp_resolve_code_value('AUDIT_ACTOR_TYPE', sddp_resolve_code_id('AUDIT_ACTOR_TYPE', COALESCE(actor_type, 'USER'))));

UPDATE working_days
 SET day_type_code_id = sddp_resolve_code_id('WORKING_DAY_TYPE', day_type),
 day_type = LOWER(sddp_resolve_code_value('WORKING_DAY_TYPE', sddp_resolve_code_id('WORKING_DAY_TYPE', day_type)));

-- -----------------------------------------------------------------------------
-- Trigger Functions
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION sddp_sync_projects_status_fields()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
 v_default_code_id UUID := '00000000-0000-0000-0006-000019000001';
BEGIN
 IF TG_OP = 'UPDATE'
 AND NEW.status_code_id IS DISTINCT FROM OLD.status_code_id
 AND NEW.status IS NOT DISTINCT FROM OLD.status THEN
 NEW.status := LOWER(sddp_resolve_code_value('PROJECT_STATUS', NEW.status_code_id));
 RETURN NEW;
 END IF;

 IF NEW.status IS NULL AND NEW.status_code_id IS NULL THEN
 NEW.status_code_id := v_default_code_id;
 NEW.status := LOWER(sddp_resolve_code_value('PROJECT_STATUS', v_default_code_id));
 RETURN NEW;
 END IF;

 IF NEW.status IS NOT NULL THEN
 NEW.status_code_id := sddp_resolve_code_id('PROJECT_STATUS', NEW.status);
 NEW.status := LOWER(sddp_resolve_code_value('PROJECT_STATUS', NEW.status_code_id));
 RETURN NEW;
 END IF;

 NEW.status := LOWER(sddp_resolve_code_value('PROJECT_STATUS', NEW.status_code_id));
 RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION sddp_sync_direct_messages_status_fields()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
 v_default_code_id UUID := '00000000-0000-0000-0006-000022000001';
BEGIN
 IF TG_OP = 'UPDATE'
 AND NEW.status_code_id IS DISTINCT FROM OLD.status_code_id
 AND NEW.status IS NOT DISTINCT FROM OLD.status THEN
 NEW.status := INITCAP(LOWER(sddp_resolve_code_value('DIRECT_MESSAGE_STATUS', NEW.status_code_id)));
 RETURN NEW;
 END IF;

 IF NEW.status IS NULL AND NEW.status_code_id IS NULL THEN
 NEW.status_code_id := v_default_code_id;
 NEW.status := INITCAP(LOWER(sddp_resolve_code_value('DIRECT_MESSAGE_STATUS', v_default_code_id)));
 RETURN NEW;
 END IF;

 IF NEW.status IS NOT NULL THEN
 NEW.status_code_id := sddp_resolve_code_id('DIRECT_MESSAGE_STATUS', NEW.status);
 NEW.status := INITCAP(LOWER(sddp_resolve_code_value('DIRECT_MESSAGE_STATUS', NEW.status_code_id)));
 RETURN NEW;
 END IF;

 NEW.status := INITCAP(LOWER(sddp_resolve_code_value('DIRECT_MESSAGE_STATUS', NEW.status_code_id)));
 RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION sddp_sync_audit_logs_actor_type_fields()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
 v_default_code_id UUID := '00000000-0000-0000-0006-000024000001';
BEGIN
 IF TG_OP = 'UPDATE'
 AND NEW.actor_type_code_id IS DISTINCT FROM OLD.actor_type_code_id
 AND NEW.actor_type IS NOT DISTINCT FROM OLD.actor_type THEN
 NEW.actor_type := UPPER(sddp_resolve_code_value('AUDIT_ACTOR_TYPE', NEW.actor_type_code_id));
 RETURN NEW;
 END IF;

 IF NEW.actor_type IS NULL AND NEW.actor_type_code_id IS NULL THEN
 NEW.actor_type_code_id := v_default_code_id;
 NEW.actor_type := UPPER(sddp_resolve_code_value('AUDIT_ACTOR_TYPE', v_default_code_id));
 RETURN NEW;
 END IF;

 IF NEW.actor_type IS NOT NULL THEN
 NEW.actor_type_code_id := sddp_resolve_code_id('AUDIT_ACTOR_TYPE', NEW.actor_type);
 NEW.actor_type := UPPER(sddp_resolve_code_value('AUDIT_ACTOR_TYPE', NEW.actor_type_code_id));
 RETURN NEW;
 END IF;

 NEW.actor_type := UPPER(sddp_resolve_code_value('AUDIT_ACTOR_TYPE', NEW.actor_type_code_id));
 RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION sddp_sync_working_days_day_type_fields()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
 v_default_code_id UUID := '00000000-0000-0000-0006-000023000001';
BEGIN
 IF TG_OP = 'UPDATE'
 AND NEW.day_type_code_id IS DISTINCT FROM OLD.day_type_code_id
 AND NEW.day_type IS NOT DISTINCT FROM OLD.day_type THEN
 NEW.day_type := LOWER(sddp_resolve_code_value('WORKING_DAY_TYPE', NEW.day_type_code_id));
 RETURN NEW;
 END IF;

 IF NEW.day_type IS NULL AND NEW.day_type_code_id IS NULL THEN
 NEW.day_type_code_id := v_default_code_id;
 NEW.day_type := LOWER(sddp_resolve_code_value('WORKING_DAY_TYPE', v_default_code_id));
 RETURN NEW;
 END IF;

 IF NEW.day_type IS NOT NULL THEN
 NEW.day_type_code_id := sddp_resolve_code_id('WORKING_DAY_TYPE', NEW.day_type);
 NEW.day_type := LOWER(sddp_resolve_code_value('WORKING_DAY_TYPE', NEW.day_type_code_id));
 RETURN NEW;
 END IF;

 NEW.day_type := LOWER(sddp_resolve_code_value('WORKING_DAY_TYPE', NEW.day_type_code_id));
 RETURN NEW;
END;
$$;

-- -----------------------------------------------------------------------------
-- Triggers
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_projects_status_code_sync
BEFORE INSERT OR UPDATE OF status, status_code_id ON projects
FOR EACH ROW
EXECUTE FUNCTION sddp_sync_projects_status_fields();

CREATE TRIGGER trg_direct_messages_status_code_sync
BEFORE INSERT OR UPDATE OF status, status_code_id ON direct_messages
FOR EACH ROW
EXECUTE FUNCTION sddp_sync_direct_messages_status_fields();

CREATE TRIGGER trg_audit_logs_actor_type_code_sync
BEFORE INSERT OR UPDATE OF actor_type, actor_type_code_id ON audit_logs
FOR EACH ROW
EXECUTE FUNCTION sddp_sync_audit_logs_actor_type_fields();

CREATE TRIGGER trg_working_days_day_type_code_sync
BEFORE INSERT OR UPDATE OF day_type, day_type_code_id ON working_days
FOR EACH ROW
EXECUTE FUNCTION sddp_sync_working_days_day_type_fields();

-- -----------------------------------------------------------------------------
-- Confirmation
-- -----------------------------------------------------------------------------
DO $$
BEGIN
 RAISE NOTICE 'Legacy/code bridge created: projects.status, direct_messages.status, audit_logs.actor_type, working_days.day_type';
END $$;

-- <<< end bundled from scripts/db/57-sync-legacy-code-bridge.sql <<<

-- Commit transaction
COMMIT;

\echo ''
\echo '============================================='
\echo 'SDDP Database Provisioning (Base) Completed!'
\echo '============================================='

-- Final verification
SELECT 'group_codes' as table_name, COUNT(*) as count FROM group_codes
UNION ALL SELECT 'codes', COUNT(*) FROM codes
UNION ALL SELECT 'persons', COUNT(*) FROM persons
UNION ALL SELECT 'users', COUNT(*) FROM users
UNION ALL SELECT 'roles', COUNT(*) FROM roles
UNION ALL SELECT 'permissions', COUNT(*) FROM permissions
UNION ALL SELECT 'role_permissions', COUNT(*) FROM role_permissions
UNION ALL SELECT 'system_configs', COUNT(*) FROM system_configs
ORDER BY table_name;
