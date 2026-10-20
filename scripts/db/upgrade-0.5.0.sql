-- Manual source-installation upgrade from public 0.4.0 / 0.4.1.
-- Run as the schema owner, with -X -v ON_ERROR_STOP=1 -v app_role=<API role>.
-- See docs/deploy/repository-050.md for backup, verification and recovery.
\set ON_ERROR_STOP on
\if :{?app_role}
\else
  \set app_role ''
\endif

BEGIN;
SET LOCAL search_path = public, pg_catalog;
SET LOCAL lock_timeout = '30s';
SET LOCAL statement_timeout = '5min';
SELECT pg_advisory_xact_lock(hashtext('sddp_schema_migrate'));

SELECT set_config('sddp.upgrade_app_role', :'app_role', true);
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles
                 WHERE rolname = current_setting('sddp.upgrade_app_role')) THEN
    RAISE EXCEPTION 'Set app_role to the existing API runtime database role';
  END IF;
END $$;

\ir migrations/V016__project_repository_confirmation.sql
\ir migrations/V017__git_connections.sql

-- Older V017 checks the constraint name globally. Another schema may use that
-- name; ensure this table has its own FK without rewriting migration history.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint
                 WHERE conname = 'fk_project_git_connection'
                   AND conrelid = 'public.projects'::regclass) THEN
    ALTER TABLE public.projects ADD CONSTRAINT fk_project_git_connection
      FOREIGN KEY (tenant_id, id, git_connection_id)
      REFERENCES public.git_connections(tenant_id, project_id, id)
      DEFERRABLE INITIALLY DEFERRED;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint
                 WHERE conname = 'fk_project_git_connection'
                   AND conrelid = 'public.projects'::regclass
                   AND contype = 'f'
                   AND confrelid = 'public.git_connections'::regclass
                   AND conkey = ARRAY(
                     SELECT attnum FROM unnest(ARRAY['tenant_id','id','git_connection_id'])
                       WITH ORDINALITY AS expected(name, position)
                     JOIN pg_attribute ON attrelid = 'public.projects'::regclass
                       AND attname = expected.name ORDER BY expected.position)
                   AND confkey = ARRAY(
                     SELECT attnum FROM unnest(ARRAY['tenant_id','project_id','id'])
                       WITH ORDINALITY AS expected(name, position)
                     JOIN pg_attribute ON attrelid = 'public.git_connections'::regclass
                       AND attname = expected.name ORDER BY expected.position)
                   AND condeferrable AND condeferred) THEN
    RAISE EXCEPTION 'Unexpected projects Git connection constraint; review the existing schema';
  END IF;
END $$;

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.git_connections TO :"app_role";
COMMIT;
\echo 'Repository database upgrade complete. Verify using the API runtime role before starting writers.'
