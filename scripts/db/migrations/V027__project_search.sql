-- Rebuildable index of readable Git objects at the configured project tip.
CREATE TABLE IF NOT EXISTS project_search_file_heads (
    tenant_id uuid NOT NULL, project_id uuid NOT NULL, revision text NOT NULL, max_bytes integer NOT NULL,
    PRIMARY KEY (tenant_id, project_id),
    FOREIGN KEY (tenant_id, project_id) REFERENCES projects(tenant_id,id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS project_search_files (
    tenant_id uuid NOT NULL, project_id uuid NOT NULL, path text NOT NULL,
    blob text NOT NULL, content text NOT NULL, updated_at timestamptz NOT NULL,
    PRIMARY KEY (tenant_id,project_id,path),
    FOREIGN KEY (tenant_id,project_id) REFERENCES projects(tenant_id,id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_project_search_files_text ON project_search_files USING gin(content gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_project_search_spec_text ON specs USING gin(to_tsvector('simple',left( coalesce(description,'') || ' ' || coalesce(decision,'') || ' ' || coalesce(context,'') || ' ' || coalesce(scope,'') || ' ' || coalesce(out_of_scope,'') || ' ' || coalesce(definitions,'') || ' ' || coalesce(acceptance_criteria,''),60000))) WHERE is_active AND valid_to IS NULL;
CREATE INDEX IF NOT EXISTS ix_project_search_requirement_text ON requirements USING gin(to_tsvector('simple',left(coalesce(description,'') || ' ' || coalesce(rationale,'') || ' ' || coalesce(source,''),60000))) WHERE is_active AND valid_to IS NULL;
CREATE INDEX IF NOT EXISTS ix_project_search_message_text ON messages USING gin(to_tsvector('simple',left(content,60000))) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_project_search_task_text ON tasks USING gin(to_tsvector('simple',left(description,60000))) WHERE is_active;
