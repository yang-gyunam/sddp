-- Project-shared diagrams. IDs/revisions inside the common model agree with the row.
CREATE TABLE IF NOT EXISTS diagrams (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    project_id UUID NOT NULL,
    title VARCHAR(200) NOT NULL CHECK (length(btrim(title)) > 0),
    kind VARCHAR(20) NOT NULL CHECK (kind IN ('authored', 'derived')),
    revision INT NOT NULL CHECK (revision > 0),
    model_json JSONB NOT NULL,
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    updated_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_diagrams_scope UNIQUE (tenant_id, project_id, id),
    CONSTRAINT fk_diagrams_project FOREIGN KEY (tenant_id, project_id) REFERENCES projects(tenant_id, id) ON DELETE RESTRICT,
    CONSTRAINT ck_diagrams_model CHECK ((
        model_json->>'diagramId' = id::text AND model_json->>'revision' = revision::text
        AND model_json->>'schemaVersion' = '1') IS TRUE)
);
CREATE TABLE IF NOT EXISTS diagram_versions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    project_id UUID NOT NULL,
    diagram_id UUID NOT NULL,
    revision INT NOT NULL CHECK (revision > 0),
    title VARCHAR(200) NOT NULL CHECK (length(btrim(title)) > 0),
    model_json JSONB NOT NULL,
    change_summary VARCHAR(500),
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_diagram_versions_revision UNIQUE (diagram_id, revision),
    CONSTRAINT fk_diagram_versions_diagram FOREIGN KEY (tenant_id, project_id, diagram_id)
        REFERENCES diagrams(tenant_id, project_id, id) ON DELETE RESTRICT,
    CONSTRAINT ck_diagram_versions_model CHECK ((
        model_json->>'diagramId' = diagram_id::text AND model_json->>'revision' = revision::text
        AND model_json->>'schemaVersion' = '1') IS TRUE)
);
CREATE INDEX IF NOT EXISTS ix_diagrams_project ON diagrams(tenant_id, project_id, updated_at);
CREATE INDEX IF NOT EXISTS ix_diagram_versions_scope ON diagram_versions(tenant_id, project_id, diagram_id);
INSERT INTO permissions (id, code, name, description, resource_type, action, is_system)
VALUES
('00000000-0000-0000-0003-000000000101', 'diagram:read', 'Read Diagrams', 'Read diagrams and revisions', 'diagram', 'read', TRUE),
('00000000-0000-0000-0003-000000000102', 'diagram:create', 'Create Diagrams', 'Create project diagrams', 'diagram', 'create', TRUE),
('00000000-0000-0000-0003-000000000103', 'diagram:update', 'Update Diagrams', 'Save diagram revisions', 'diagram', 'update', TRUE)
ON CONFLICT (code) DO NOTHING;
INSERT INTO role_permissions (id, role_id, permission_id)
SELECT uuid_generate_v4(), r.id, p.id
FROM roles r CROSS JOIN permissions p
WHERE r.is_system AND r.is_active AND
    ((r.type IN (0,1,2,5) AND p.code IN ('diagram:read','diagram:create','diagram:update'))
     OR (r.type IN (3,4,6) AND p.code = 'diagram:read'))
ON CONFLICT (role_id,permission_id) DO NOTHING;
