-- Accept typed schema2 models without rewriting schema1 documents or immutable history.
ALTER TABLE diagrams DROP CONSTRAINT IF EXISTS ck_diagrams_model;
ALTER TABLE diagrams ADD CONSTRAINT ck_diagrams_model CHECK ((
    model_json->>'diagramId' = id::text AND model_json->>'revision' = revision::text
    AND model_json->>'schemaVersion' IN ('1', '2')) IS TRUE);
ALTER TABLE diagram_versions DROP CONSTRAINT IF EXISTS ck_diagram_versions_model;
ALTER TABLE diagram_versions ADD CONSTRAINT ck_diagram_versions_model CHECK ((
    model_json->>'diagramId' = diagram_id::text AND model_json->>'revision' = revision::text
    AND model_json->>'schemaVersion' IN ('1', '2')) IS TRUE);
