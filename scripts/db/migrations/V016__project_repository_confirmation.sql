-- Repository confirmation is explicit; existing projects remain unconfirmed.
ALTER TABLE projects ADD COLUMN IF NOT EXISTS repository_version INT NOT NULL DEFAULT 0;
ALTER TABLE projects ADD COLUMN IF NOT EXISTS repository_checked_at TIMESTAMPTZ;
ALTER TABLE projects ADD COLUMN IF NOT EXISTS repository_confirmed_at TIMESTAMPTZ;
