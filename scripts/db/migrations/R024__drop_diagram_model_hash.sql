-- Restore the pre-V024 reader before rolling back this derived column.
DROP TRIGGER IF EXISTS set_diagram_model_hash ON diagrams;

-- V023 readers captured SELECT * during rolling deployment. Only their derived
-- snapshot fields need removing for V023's strict restore-column validation.
DO $$
BEGIN
    IF to_regclass('project_snapshots') IS NOT NULL THEN
        WITH cleaned AS (
            SELECT s.id, jsonb_set(s.snapshot_data, '{Tables,diagrams}', (
                SELECT jsonb_agg(CASE WHEN jsonb_typeof(item) = 'object' THEN item - 'model_hash' ELSE item END ORDER BY position)
                FROM jsonb_array_elements(s.snapshot_data #> '{Tables,diagrams}')
                     WITH ORDINALITY AS rows(item, position)
            )) AS data
            FROM project_snapshots s
            WHERE jsonb_typeof(s.snapshot_data #> '{Tables,diagrams}') = 'array'
              AND s.snapshot_data @? '$.Tables.diagrams[*].model_hash'
        )
        UPDATE project_snapshots s
        SET snapshot_data = c.data, data_size_bytes = octet_length(convert_to(c.data::text, 'UTF8'))
        FROM cleaned c WHERE s.id = c.id;
    END IF;
END
$$;
ALTER TABLE diagrams DROP COLUMN IF EXISTS model_hash;
DROP FUNCTION IF EXISTS sddp_set_diagram_model_hash();
