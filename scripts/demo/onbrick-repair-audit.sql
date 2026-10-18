-- Repair the legacy OnBrick sample artifact and glossary terms without replaying the destructive demo seed.
-- Missing glossary lists become empty arrays; existing values are preserved.
-- Only a missing update owner on these known tenant/project/seed IDs is repaired.
-- Creation ownership is the truthful initial update owner (AuditableEntityBase.SetCreatedBy).
-- Existing update owners, timestamps, content and all other records remain unchanged.
BEGIN;
UPDATE artifact_trackings
SET updated_by = created_by
WHERE id = '00000000-0000-0000-007a-000000000001'
  AND tenant_id = '00000000-0000-0000-0010-000000000001'
  AND project_id = '00000000-0000-0000-0020-000000000021'
  AND updated_by IS NULL
  AND created_by IS NOT NULL;
UPDATE glossary_terms
SET updated_by = COALESCE(updated_by, created_by),
    usage_examples = COALESCE(usage_examples, '[]'::jsonb),
    related_term_ids = COALESCE(related_term_ids, '[]'::jsonb)
WHERE id IN ('00000000-0000-0000-0079-000000000001', '00000000-0000-0000-0079-000000000002')
  AND tenant_id = '00000000-0000-0000-0010-000000000001'
  AND project_id = '00000000-0000-0000-0020-000000000021'
  AND (updated_by IS NULL OR usage_examples IS NULL OR related_term_ids IS NULL)
  AND created_by IS NOT NULL;
COMMIT;
