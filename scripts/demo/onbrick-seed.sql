-- =============================================================================
-- onbrick-seed.sql
-- OnBrick Brownfield Migration Scenario seed data (docs/test/onbrick-startup.md)
-- =============================================================================
-- OnBrick is a brownfield scenario that reverse-imports an existing system
-- (Confluence documents and legacy code) into SDDP.
-- It validates the reverse flow from Spec -> Requirement -> Gap discovery.
--
-- Usage:
--   Run after `make db-provision` (inserts the OnBrick demo dataset)
--   docker compose exec -T sddp-db psql -U sddp -d sddp < scripts/demo/onbrick-seed.sql
--
-- Prerequisites:
--   90-data-test-tenants.sql  (OnBrick Tenant/Project)
--   91-data-test-persons.sql  (4 OnBrick users: minjun.park, soojin.yoon, hyunwoo.kim, jieun.lee)
-- =============================================================================

-- Well-known IDs
-- Tenant:  00000000-0000-0000-0010-000000000001
-- Project: 00000000-0000-0000-0020-000000000021
-- Users:
--   Minjun Park (PM):        00000000-0000-0000-0005-000000000301
--   Soojin Yoon (Tech Lead): 00000000-0000-0000-0005-000000000302
--   Hyunwoo Kim (Backend):   00000000-0000-0000-0005-000000000303
--   Jieun Lee (Frontend):    00000000-0000-0000-0005-000000000304

BEGIN;

-- =============================================================================
-- Step 0: Clear existing OnBrick data
-- =============================================================================
-- Prevent conflicts from previous runs
DELETE FROM artifact_trackings WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM glossary_terms WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM relationships WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM worklogs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM tasks WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM sign_offs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM specs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM requirements WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM messages WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
DELETE FROM conversation_members
    WHERE conversation_id IN (
        SELECT id FROM conversations WHERE tenant_id = '00000000-0000-0000-0010-000000000001'
    );
DELETE FROM channels
    WHERE id IN (
        SELECT id FROM conversations WHERE tenant_id = '00000000-0000-0000-0010-000000000001'
    );
DELETE FROM conversations WHERE tenant_id = '00000000-0000-0000-0010-000000000001';

-- =============================================================================
-- Step 1: Conversations (2 Channels)
-- =============================================================================
INSERT INTO conversations (
    id, tenant_id, project_id, name, description,
    conversation_type, visibility, scope,
    is_default, sort_order, created_by, created_at
)
VALUES
    -- #design-review: payment system design review channel (Day 3)
    ('00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'design-review', 'Payment system design review',
     'Channel', 'Public', 'ProjectScoped', FALSE, 1,
     '00000000-0000-0000-0005-000000000301',
     '2026-02-04 10:00:00+09'),

    -- #onbrick-general: general discussion channel
    ('00000000-0000-0000-0071-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'onbrick-general', 'Company-wide announcements and general discussion',
     'Channel', 'Public', 'ProjectScoped', TRUE, 2,
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 09:00:00+09');

-- =============================================================================
-- Step 2: Channel records (TPT pattern)
-- =============================================================================
INSERT INTO channels (id) VALUES
    ('00000000-0000-0000-0071-000000000001'),
    ('00000000-0000-0000-0071-000000000002');

-- =============================================================================
-- Step 3: Conversation Members (4 users x 2 channels = 8)
-- =============================================================================
INSERT INTO conversation_members (conversation_id, user_id, role, member_type, notifications_enabled)
VALUES
    -- #design-review (001) -- Minjun OWNER
    ('00000000-0000-0000-0071-000000000001', '00000000-0000-0000-0005-000000000301', 'OWNER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000001', '00000000-0000-0000-0005-000000000302', 'MEMBER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000001', '00000000-0000-0000-0005-000000000303', 'MEMBER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000001', '00000000-0000-0000-0005-000000000304', 'MEMBER', 'HUMAN', TRUE),
    -- #onbrick-general (002) -- Minjun OWNER
    ('00000000-0000-0000-0071-000000000002', '00000000-0000-0000-0005-000000000301', 'OWNER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000002', '00000000-0000-0000-0005-000000000302', 'MEMBER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000002', '00000000-0000-0000-0005-000000000303', 'MEMBER', 'HUMAN', TRUE),
    ('00000000-0000-0000-0071-000000000002', '00000000-0000-0000-0005-000000000304', 'MEMBER', 'HUMAN', TRUE);

-- =============================================================================
-- Step 4: Messages in #design-review (3 messages, Day 3)
-- =============================================================================
INSERT INTO messages (id, tenant_id, project_id, conversation_id, sender_id, content, type_code_id, created_at)
VALUES
    -- Minjun: PROPOSAL
    ('00000000-0000-0000-0072-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000301',  -- Minjun
     E'**[PROPOSAL] Partial refund handling**\n\nLet''s process partial refunds as a percentage of the original payment amount.\n\n- Example: a 50% refund = original payment amount x 0.5\n- The PG API already supports percentage-based refunds, so implementation stays simple\n- The user input is intuitive as well (% entry)\n\n@soojin.yoon please review.',
     '00000000-0000-0000-0006-000007000001',  -- PROPOSAL
     '2026-02-04 10:15:00+09'),

    -- Soojin: OBJECTION
    ('00000000-0000-0000-0072-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000302',  -- Soojin
     E'**[OBJECTION] Fixed-amount refunds are also required**\n\nPercentage-only refunds are not enough.\n\n- Cases like refunding a 2,500 KRW shipping fee need a fixed amount\n- The CS operations team frequently requests refunds for specific amounts\n- The PG API also supports fixed-amount refunds\n\n**Proposal**: support both percentage-based and fixed-amount refunds.',
     '00000000-0000-0000-0006-000007000003',  -- OBJECTION
     '2026-02-04 10:28:00+09'),

    -- Minjun: DECISION
    ('00000000-0000-0000-0072-000000000003',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000301',  -- Minjun
     E'**[DECISION] Partial refunds will support both percentage and fixed amounts**\n\nReflecting Soojin''s feedback, we are locking the policy as follows:\n\n**Refund modes**:\n- Percentage refund: N% of the original payment amount (1 to 100)\n- Fixed-amount refund: direct entry of a specific amount (not exceeding the original payment amount)\n\n**Validation rules**:\n- Refund amount <= original payment amount\n- Sum of previous refunds must be validated\n- Confirm the detailed PG API contract before final design\n\nI will reflect this in SPEC-PAY-001.',
     '00000000-0000-0000-0006-000007000005',  -- DECISION
     '2026-02-04 10:40:00+09');

-- =============================================================================
-- Step 5: Requirements (7: 2 A-level + 5 B-level)
-- =============================================================================
INSERT INTO requirements (
    id, tenant_id, project_id, code, title, description,
    level_code_id, parent_id, status_code_id, priority, category,
    source, rationale, conversation_id, created_by, updated_by, created_at
)
VALUES
    -- REQ-PAY-001: payment processing system (Level A) -- Day 1
    ('00000000-0000-0000-0073-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001', 'Payment processing system',
     'Payment processing system built around KG Inicis PG integration. Supports card payments, bank transfers, and easy-pay methods, including refund handling and payment status management.',
     '00000000-0000-0000-0006-000004000001', NULL,                                    -- Level A, no parent
     '00000000-0000-0000-0006-000003000003', 'HIGH', 'Payments',                       -- APPROVED
     'Reverse-imported from Confluence documents', 'Structured requirement definition for the legacy payment system',
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000301',
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 10:00:00+09'),

    -- REQ-PAY-001-1: PG integration module (Level B) -- Day 1
    ('00000000-0000-0000-0073-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001-1', 'PG integration module',
     'Module that integrates with the KG Inicis PG API to process payment requests, approvals, and cancellations.',
     '00000000-0000-0000-0006-000004000002',
     '00000000-0000-0000-0073-000000000001',                                           -- parent: REQ-PAY-001
     '00000000-0000-0000-0006-000003000003', 'HIGH', 'Feature',                        -- APPROVED
     'Reverse-extracted from SPEC-PAY-001', NULL,
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000301',
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 10:30:00+09'),

    -- REQ-PAY-001-2: refund processing logic (Level B) -- Day 1
    ('00000000-0000-0000-0073-000000000003',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001-2', 'Refund processing logic',
     'Logic for handling full and partial refunds, including refund requests through the PG API and refund status tracking.',
     '00000000-0000-0000-0006-000004000002',
     '00000000-0000-0000-0073-000000000001',                                           -- parent: REQ-PAY-001
     '00000000-0000-0000-0006-000003000003', 'HIGH', 'Feature',                        -- APPROVED
     'Reverse-extracted from SPEC-PAY-001', NULL,
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000301',
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 11:00:00+09'),

    -- REQ-PAY-001-3: partial refund handling (Level B) -- Day 2, Gap discovered
    ('00000000-0000-0000-0073-000000000004',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001-3', 'Partial refund handling',
     'Supports both percentage-based and fixed-amount partial refunds. The refund amount cannot exceed the original payment amount, and cumulative refunded amounts must be tracked.',
     '00000000-0000-0000-0006-000004000002',
     '00000000-0000-0000-0073-000000000001',                                           -- parent: REQ-PAY-001
     '00000000-0000-0000-0006-000003000003', 'HIGH', 'Feature',                        -- APPROVED
     'Gap analysis (Day 2)', 'Partial refund logic was not defined in the existing Confluence documents',
     '00000000-0000-0000-0071-000000000001',
     '00000000-0000-0000-0005-000000000302',
     '00000000-0000-0000-0005-000000000302',
     '2026-02-04 09:00:00+09'),

    -- REQ-PAY-001-4: payment retry policy (Level B) -- Day 2, Gap discovered
    ('00000000-0000-0000-0073-000000000005',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001-4', 'Payment retry policy',
     'Defines the retry policy for failed PG payments: up to 3 retries, exponential backoff (1s, 2s, 4s), and user notification on final failure.',
     '00000000-0000-0000-0006-000004000002',
     '00000000-0000-0000-0073-000000000001',                                           -- parent: REQ-PAY-001
     '00000000-0000-0000-0006-000003000001', 'MEDIUM', 'Feature',                      -- DRAFT
     'Gap analysis (Day 2)', 'The existing documents did not define a payment failure retry policy',
     NULL,
     '00000000-0000-0000-0005-000000000302',
     '00000000-0000-0000-0005-000000000302',
     '2026-02-04 09:30:00+09'),

    -- REQ-PAY-001-5: duplicate payment prevention (Level B) -- Day 2, Gap discovered
    ('00000000-0000-0000-0073-000000000006',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-001-5', 'Duplicate payment prevention',
     'Ensures idempotency for concurrent payment requests using idempotency keys and database-level unique constraints to prevent duplicate charges.',
     '00000000-0000-0000-0006-000004000002',
     '00000000-0000-0000-0073-000000000001',                                           -- parent: REQ-PAY-001
     '00000000-0000-0000-0006-000003000001', 'HIGH', 'Security',                       -- DRAFT
     'Gap analysis (Day 2)', 'The existing documents did not mention concurrency handling for duplicate payment prevention',
     NULL,
     '00000000-0000-0000-0005-000000000302',
     '00000000-0000-0000-0005-000000000302',
     '2026-02-04 10:00:00+09'),

    -- REQ-PAY-002: payment UI system (Level A) -- Day 7
    ('00000000-0000-0000-0073-000000000007',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'REQ-PAY-002', 'Payment UI system',
     'Implements a step-by-step payment wizard UI covering product confirmation, payment method selection, payment execution, and completion flow, including error display rules and accessibility requirements.',
     '00000000-0000-0000-0006-000004000001', NULL,                                    -- Level A, no parent
     '00000000-0000-0000-0006-000003000001', 'HIGH', 'UI',                            -- DRAFT
     'Linked from SPEC-PAY-UI-001', 'Improves payment UX and reflects accessibility requirements',
     NULL,
     '00000000-0000-0000-0005-000000000304',
     '00000000-0000-0000-0005-000000000304',
     '2026-02-10 09:00:00+09');

-- =============================================================================
-- Step 6: Specs (2: LOCKED v1.0.0 + DRAFT v1.0.0)
-- =============================================================================
INSERT INTO specs (
    id, tenant_id, project_id, code, title, description, decision, context,
    scope, out_of_scope, status_code_id, version,
    requirement_id, born_from_conversation_id, supersedes_spec_id, locked_at,
    valid_from, valid_to,
    created_by, updated_by, created_at
)
VALUES
    -- SPEC-PAY-001: payment system architecture (Locked, v1.0.0)
    ('00000000-0000-0000-0074-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'SPEC-PAY-001',
     'Payment system architecture (migrated from Confluence)',
     'Architecture spec for the payment system built around KG Inicis PG integration. Reverse-imported and structured from legacy Confluence documents.',
     'PG provider: KG Inicis. Payment methods: card, bank transfer, easy-pay. Partial refunds support both percentage-based and fixed-amount modes. Security uses HttpOnly cookies with CSRF tokens.',
     'The existing Confluence documents scattered payment flow and refund policy decisions across many pages, so the architecture had to be restructured. The Day 2 review surfaced three gaps: partial refunds, retry policy, and duplicate payment prevention.',
     'PG integration, payment approval/cancellation, refunds (full and partial), payment status management, payment retry handling, duplicate payment prevention',
     'Settlement system and tax invoice issuance (Phase 2)',
     '00000000-0000-0000-0006-000001000004',  -- LOCKED
     '1.0.0',
     '00000000-0000-0000-0073-000000000001',  -- REQ-PAY-001
     '00000000-0000-0000-0071-000000000001',  -- #design-review
     NULL,
     '2026-02-06 09:30:00+09',                -- locked_at (Day 4)
     '2026-02-03 10:00:00+09',                -- valid_from (Day 1)
     NULL,                                     -- valid_to (current version)
     '00000000-0000-0000-0005-000000000301',  -- Minjun
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 10:00:00+09'),

    -- SPEC-PAY-UI-001: payment screen UI/UX design (Draft, v1.0.0)
    ('00000000-0000-0000-0074-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'SPEC-PAY-UI-001',
     'Payment screen UI/UX design',
     'Defines the step-by-step payment wizard flow, error display rules, and accessibility requirements. This is a new spec derived from a UI gap discovered during migration.',
     'Step-by-step wizard: product confirmation -> payment method selection -> payment execution -> completion. Preserve progress when leaving a step. Show inline messages and toast notifications on errors.',
     'The existing payment UI is a single page with poor UX. It should be improved into a step-by-step wizard.',
     'Payment wizard UI, error display, accessibility (WCAG 2.1 AA), mobile responsiveness',
     'Payment admin screens (Phase 2)',
     '00000000-0000-0000-0006-000001000001',  -- DRAFT
     '1.0.0',
     '00000000-0000-0000-0073-000000000007',  -- REQ-PAY-002
     NULL,
     NULL,
     NULL,                                     -- not locked
     '2026-02-10 09:00:00+09',                -- valid_from (Day 7)
     NULL,                                     -- valid_to (current version)
     '00000000-0000-0000-0005-000000000304',  -- Jieun
     '00000000-0000-0000-0005-000000000304',
     '2026-02-10 09:00:00+09');

-- =============================================================================
-- Step 7: Sign-offs for SPEC-PAY-001 (2 approvals, Day 4)
-- =============================================================================
INSERT INTO sign_offs (
    id, tenant_id, project_id, spec_id, stakeholder_id,
    role, decision_code_id, comments, signed_at
)
VALUES
    -- Soojin (REVIEWER, role=3) APPROVED
    ('00000000-0000-0000-0075-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0074-000000000001',  -- SPEC-PAY-001
     '00000000-0000-0000-0005-000000000302',  -- Soojin
     3, '00000000-0000-0000-0006-000015000002',  -- APPROVED
     'Previously missing areas (partial refunds, retry policy, duplicate payment prevention) are now covered. Architectural consistency confirmed.',
     '2026-02-06 09:15:00+09'),

    -- Minjun (PRODUCT_OWNER, role=0) APPROVED
    ('00000000-0000-0000-0075-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0074-000000000001',  -- SPEC-PAY-001
     '00000000-0000-0000-0005-000000000301',  -- Minjun
     0, '00000000-0000-0000-0006-000015000002',  -- APPROVED
     'Confirmed that the business requirements are reflected. Achieved 95% coverage against the original Confluence documents.',
     '2026-02-06 09:30:00+09');

-- =============================================================================
-- Step 8: Tasks (1, Day 8)
-- =============================================================================
INSERT INTO tasks (
    id, tenant_id, project_id, title, description,
    status_code_id, priority_code_id, assignee_id, creator_id,
    estimated_hours, actual_hours, completed_at,
    created_by, updated_by, created_at
)
VALUES
    ('00000000-0000-0000-0076-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'Implement partial refund API',
     'Implement the partial refund endpoint based on REQ-PAY-001-3, including percentage and fixed-amount refund APIs, validation rules, and PG API integration.',
     '00000000-0000-0000-0006-000011000002',  -- IN_PROGRESS
     '00000000-0000-0000-0006-000012000003',  -- HIGH
     '00000000-0000-0000-0005-000000000303',  -- Hyunwoo (assignee)
     '00000000-0000-0000-0005-000000000303',  -- Hyunwoo (creator)
     16, 4, NULL,
     '00000000-0000-0000-0005-000000000303',
     '00000000-0000-0000-0005-000000000303',
     '2026-02-11 09:00:00+09');

-- =============================================================================
-- Step 9: Worklogs (1, Day 8)
-- =============================================================================
INSERT INTO worklogs (
    id, tenant_id, project_id, user_id, work_date, spent_hours, note, task_id
)
VALUES
    ('00000000-0000-0000-0077-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0005-000000000303',  -- Hyunwoo
     '2026-02-11', 4,
     'Started implementing the partial refund API endpoint. Designed DTOs for percentage/fixed-amount refunds and scaffolded the PG API integration module.',
     '00000000-0000-0000-0076-000000000001');

-- =============================================================================
-- Step 10: Relationships (2: Spec -> Requirement, implements)
-- =============================================================================
INSERT INTO relationships (
    id, tenant_id, project_id,
    from_entity_type, from_entity_id,
    to_entity_type, to_entity_id,
    type_code_id, reason, source_spec_id,
    created_by, created_at
)
VALUES
    -- SPEC-PAY-001 -> REQ-PAY-001 (implements)
    ('00000000-0000-0000-0078-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     0, '00000000-0000-0000-0074-000000000001',   -- from: Spec (type=0)
     1, '00000000-0000-0000-0073-000000000001',   -- to: Requirement (type=1)
     '00000000-0000-0000-0006-000014000006',       -- IMPLEMENTS
     'SPEC-PAY-001 implements the full REQ-PAY-001 payment processing system',
     '00000000-0000-0000-0074-000000000001',
     '00000000-0000-0000-0005-000000000301',
     '2026-02-03 11:00:00+09'),

    -- SPEC-PAY-UI-001 -> REQ-PAY-002 (implements)
    ('00000000-0000-0000-0078-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     0, '00000000-0000-0000-0074-000000000002',   -- from: Spec (type=0)
     1, '00000000-0000-0000-0073-000000000007',   -- to: Requirement (type=1)
     '00000000-0000-0000-0006-000014000006',       -- IMPLEMENTS
     'SPEC-PAY-UI-001 implements the REQ-PAY-002 payment UI system',
     '00000000-0000-0000-0074-000000000002',
     '00000000-0000-0000-0005-000000000304',
     '2026-02-10 10:00:00+09');

-- =============================================================================
-- Step 11: Glossary Terms (2, Day 9)
-- =============================================================================
INSERT INTO glossary_terms (
    id, tenant_id, project_id, term, definition,
    category_code_id, abbreviation, synonyms,
    defined_by, status_code_id, version,
    created_by, created_at
)
VALUES
    -- PG: Payment Gateway
    ('00000000-0000-0000-0079-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'PG',
     'Payment Gateway. A payment relay service that securely brokers payment data between online stores and financial institutions such as card issuers and banks. OnBrick uses KG Inicis as its PG provider.',
     '00000000-0000-0000-0006-000010000003',  -- ABBREVIATION
     'PG',
     'Payment Gateway, electronic payment processing, payment relay',
     '00000000-0000-0000-0005-000000000302',  -- Soojin
     '00000000-0000-0000-0006-000009000002',  -- ACTIVE
     '1.0.0',
     '00000000-0000-0000-0005-000000000302',
     '2026-02-12 10:00:00+09'),

    -- Refund
    ('00000000-0000-0000-0079-000000000002',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     'Refund',
     'Refund. The process of returning all or part of a completed payment to the customer. Supports both percentage-based and fixed-amount refund methods through the PG API.',
     '00000000-0000-0000-0006-000010000002',  -- BUSINESS
     NULL,
     'Refund, payment reversal, reimbursement',
     '00000000-0000-0000-0005-000000000302',  -- Soojin
     '00000000-0000-0000-0006-000009000002',  -- ACTIVE
     '1.0.0',
     '00000000-0000-0000-0005-000000000302',
     '2026-02-12 10:30:00+09');

-- =============================================================================
-- Step 12: Artifact Trackings (1, Day 6)
-- =============================================================================
INSERT INTO artifact_trackings (
    id, tenant_id, project_id, spec_id,
    artifact_path, artifact_type, artifact_category,
    content_hash, generator_version, template_version,
    entity_name,
    created_by, created_at
)
VALUES
    ('00000000-0000-0000-007a-000000000001',
     '00000000-0000-0000-0010-000000000001',
     '00000000-0000-0000-0020-000000000021',
     '00000000-0000-0000-0074-000000000001',  -- SPEC-PAY-001
     'src/backend/Services/PaymentService.cs',
     'SERVICE',
     'Code',
     'a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2',
     '1.0.0',
     '1.0.0',
     'PaymentService',
     '00000000-0000-0000-0005-000000000303',  -- Hyunwoo
     '2026-02-10 14:00:00+09');

COMMIT;

-- =============================================================================
-- Confirmation
-- =============================================================================
DO $$
DECLARE
    conv_count INT;
    msg_count INT;
    req_count INT;
    spec_count INT;
    signoff_count INT;
    task_count INT;
    worklog_count INT;
    rel_count INT;
    glossary_count INT;
    artifact_count INT;
BEGIN
    SELECT COUNT(*) INTO conv_count FROM conversations WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO msg_count FROM messages WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO req_count FROM requirements WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO spec_count FROM specs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO signoff_count FROM sign_offs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO task_count FROM tasks WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO worklog_count FROM worklogs WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO rel_count FROM relationships WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO glossary_count FROM glossary_terms WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    SELECT COUNT(*) INTO artifact_count FROM artifact_trackings WHERE tenant_id = '00000000-0000-0000-0010-000000000001';
    RAISE NOTICE 'OnBrick seed data: % conversations, % messages, % requirements, % specs, % sign-offs, % tasks, % worklogs, % relationships, % glossary terms, % artifacts',
        conv_count, msg_count, req_count, spec_count, signoff_count, task_count, worklog_count, rel_count, glossary_count, artifact_count;
END $$;
