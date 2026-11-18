-- Database-owned digest of the exact UTF-8 JSON representation validated by DiagramListReader.
ALTER TABLE diagrams ADD COLUMN IF NOT EXISTS model_hash bytea;

CREATE OR REPLACE FUNCTION sddp_set_diagram_model_hash() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $$
BEGIN
    NEW.model_hash := sha256(convert_to(NEW.model_json::text, 'UTF8'));
    RETURN NEW;
END
$$;

CREATE OR REPLACE TRIGGER set_diagram_model_hash
BEFORE INSERT OR UPDATE OF model_json, model_hash ON diagrams
FOR EACH ROW EXECUTE FUNCTION sddp_set_diagram_model_hash();
ALTER TABLE diagrams ENABLE ALWAYS TRIGGER set_diagram_model_hash;

-- Fire the same trigger for pre-V024 rows, leaving all document and audit fields intact.
UPDATE diagrams SET model_hash = NULL WHERE model_hash IS NULL;
ALTER TABLE diagrams ALTER COLUMN model_hash SET NOT NULL;
