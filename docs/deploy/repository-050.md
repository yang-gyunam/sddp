# Repository upgrade to 0.5.0

This is a manual, additive database upgrade from the public 0.4.0 or 0.4.1
schema. It adds repository confirmation (V016) and project-scoped Git connections
(V017). It preserves existing application rows and relationships and does not
automatically confirm repositories. An automatic in-place upgrade bundle is not
provided.

## Back up and stop writers

1. Record the installed version and keep its source, configuration and images for
   recovery. Keep credentials outside Git.
2. Stop the API and all other writers, including CLI or integration jobs. In the
   standard Compose deployment, run `docker compose stop sddp-api sddp-front`; leave PostgreSQL
   running. Keep writers stopped until the upgrade and checks finish.
3. Back up the database with `pg_dump --format=custom`, using the existing database
   connection and a backup account with read access to every application table.
   Back up the API instance data at the same point; see [API data](api-data.md).
   Store backups outside the checkout with restrictive filesystem permissions.
4. Restore those backups into a separate database and API data directory first.
   Rehearse this procedure there and compare application row counts and important
   relationships with the original before upgrading the installation.

Example for a host with PostgreSQL client tools, using connection details in a
local PostgreSQL service file and credentials in `.pgpass` (mode `0600`):

```bash
umask 077
pg_dump --dbname=service=sddp-backup --format=custom --file=/safe/backup/sddp-before-050.dump
```

Use service names and backup paths appropriate to the installation. Never put a
password or token in command-line arguments, repository URLs or shell history.
Do not use `make db-provision`, `db-reset`, `db-reset-full` or `demo-onbrick`
against the existing database. Those commands are for fresh or disposable data.

### Existing OnBrick samples from 0.4.0

If this installation loaded the OnBrick sample from 0.4.0 or an earlier release,
apply the existing scoped sample repair after the backup and before starting the
new API. Those older sample files omitted the update owner on one Artifact and two
glossary rows, and omitted the glossary list values. Reading those rows can return
HTTP 500 even when the repository schema upgrade succeeded.

```bash
psql --dbname=service=sddp-schema-owner -X --set=ON_ERROR_STOP=1 \
  --file=scripts/demo/onbrick-repair-audit.sql
```

For the standard Compose installation:

```bash
docker compose exec -T sddp-db psql -X -U sddp -d sddp --set=ON_ERROR_STOP=1 \
  --file=/scripts/demo/onbrick-repair-audit.sql
```

This idempotent repair only fills missing audit/list fields on the three known
OnBrick sample IDs. It preserves valid values, timestamps and all other rows.
The 0.4.1 sample already includes these values. Installations that did not load
the older sample do not need this step. Do not replay the destructive seed or
generalize the repair to other rows. See [sample validation](../public-guide.md#validation)
for the exact repair scope and the OnBrick smoke test commands. After restarting,
check the OnBrick project, Artifact list and glossary list as well as repository
settings.

## Apply the supplied SQL

Use `psql` version 14 or newer. Connect as the schema owner (or an administrator
permitted to alter the existing `public.projects` table and grant privileges).
Set `app_role` to the **existing API runtime database role**, not a new arbitrary
role. The script checks that it exists; it does not create roles or change their
passwords or memberships. Existing installations that use one role for both
ownership and runtime may keep that role for this upgrade. Separating those roles
is recommended but is a separate deployment change.

From the 0.5.0 checkout:

```bash
psql --dbname=service=sddp-schema-owner -X --set=ON_ERROR_STOP=1 \
  --set=app_role=sddp \
  --file=scripts/db/upgrade-0.5.0.sql
```

The standard Compose database mounts `scripts/` read-only at `/scripts`. Use its
installed `psql`. Replace `sddp` below if the
database, owner or API runtime role has a different name. Container authentication
must already be configured; do not add passwords to these commands.

```bash
docker compose exec -T sddp-db psql -X -U sddp -d sddp \
  --set=ON_ERROR_STOP=1 --set=app_role=sddp \
  --file=/scripts/db/upgrade-0.5.0.sql
```

The entrypoint includes the reviewed
`migrations/V016__project_repository_confirmation.sql` and
`migrations/V017__git_connections.sql`. Keep all three files together. They run
in one transaction with a migration advisory lock, a 30-second lock timeout and
a 5-minute statement timeout. Any SQL error rolls the transaction back.
After resolving the error, rerun the same command.

The script is safe to reapply to an upgraded or freshly provisioned schema.
It grants `SELECT`, `INSERT`, `UPDATE` and `DELETE` on the new `git_connections`
table to `app_role`, including installations without default table grants.
It does not grant schema ownership or DDL privileges. It does not write a
`schema_migrations` ledger: this public source installation uses explicit manual
SQL, not the separate migration runner.

## Verify before opening access

Connect as the **API runtime role** and run:

```sql
SELECT repository_version, repository_checked_at, repository_confirmed_at,
       git_connection_id, repository_visibility
FROM public.projects LIMIT 1;
SELECT COUNT(*) FROM public.git_connections;
SELECT has_table_privilege(current_user, 'public.git_connections', 'SELECT')
   AND has_table_privilege(current_user, 'public.git_connections', 'INSERT')
   AND has_table_privilege(current_user, 'public.git_connections', 'UPDATE')
   AND has_table_privilege(current_user, 'public.git_connections', 'DELETE')
   AS git_connection_access;
```

All queries must succeed and `git_connection_access` must be `true`. On the first
upgrade, existing projects have version `0`, null check/confirmation/connection
values, and the connection table is empty. Reapplying must preserve any values
already created by 0.5.0. Compare pre-upgrade and post-upgrade row counts and
relationship checks from the rehearsal.

Build/start the 0.5.0 API and web using the normal deployment procedure while
preserving the database and API data volumes. Check health, login, existing
project details and relationship navigation. Reload any browser tabs that were
open during the upgrade so they use the new web assets.

## Repository confirmation after upgrade

A project owner with project update permission opens Settings → Project → General
→ Git Repository, checks the connection and confirms its coordinates. New Artifact
writes and file–Spec mappings require confirmation. Existing records and Files
browsing remain readable.

For legacy projects with file relationships, the existing repository URL, branch
and root are protected from ordinary changes. An initially empty URL can be
entered; connection checks compare all existing relationship paths with Git before
allowing confirmation. Missing or renamed paths need review before confirming.
Repository migration and automatic unlock are not provided in 0.5.0.

Free accepts public repositories only. It refuses token registration and private
access. Existing server-wide Git token environment variables are no longer used.
Public github.com direct connections remain supported. For public Gitea,
administrators register a project-scoped connection under Settings → System Config
→ Git Connections, then select it in the project's repository settings.

Browsing caches now use a URL/branch fingerprint below the existing tenant/project
directory. The first read may clone again. Older cached directories are preserved
and need not be removed for the upgrade. Reserve space for the additional clone.

## Recovery

Do not drop the new columns or table to downgrade: that would discard confirmation
and connection data. If the application must be rolled back after a successful
upgrade, stop writers and restore the **matching database and API data backup**
into a separate clean recovery target, then start the saved older application
version against that target. Verify it before switching traffic. A pre-upgrade
backup cannot retain writes made after it was taken.

An older API does not enforce the new confirmation rule. Keep repository and
relationship writes disabled until a compatible version and reviewed data are in
place. Do not restore a backup over a live database or reset the existing volumes.
