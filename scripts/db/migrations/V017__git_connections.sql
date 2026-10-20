-- Project-bound Git credentials. Key material is held outside this database.
CREATE TABLE IF NOT EXISTS git_connections (
 id UUID PRIMARY KEY, tenant_id UUID NOT NULL, project_id UUID NOT NULL,
 name VARCHAR(100) NOT NULL, provider VARCHAR(20) NOT NULL, server_url VARCHAR(2048) NOT NULL,
 username VARCHAR(200) NOT NULL DEFAULT '', protected_token TEXT,
 version INT NOT NULL DEFAULT 0, checked_at TIMESTAMPTZ,
 is_active BOOLEAN NOT NULL DEFAULT TRUE, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
 CONSTRAINT fk_git_connection_project FOREIGN KEY (tenant_id,project_id) REFERENCES projects(tenant_id,id),
 CONSTRAINT uq_git_connection_scope UNIQUE (tenant_id,project_id,id)
);
CREATE INDEX IF NOT EXISTS ix_git_connections_project ON git_connections(tenant_id,project_id);
ALTER TABLE projects ADD COLUMN IF NOT EXISTS git_connection_id UUID;
ALTER TABLE projects ADD COLUMN IF NOT EXISTS repository_visibility VARCHAR(10);
DO $$ BEGIN
 IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_project_git_connection') THEN
 ALTER TABLE projects ADD CONSTRAINT fk_project_git_connection FOREIGN KEY (tenant_id,id,git_connection_id)
 REFERENCES git_connections(tenant_id,project_id,id) DEFERRABLE INITIALLY DEFERRED;
 END IF;
END $$;
