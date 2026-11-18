# Database Provisioning Scripts

This directory contains the tracked public database provisioning files for SDDP.

## Entry Points

```bash
make db-provision
make db-seed-test
make demo-onbrick      # DESTRUCTIVE: deletes all Compose project volumes first
```

## Files

- `provision-base.sql`: schema, indexes, storage setup, and base data
- `provision-test.sql`: shared public test accounts, tenants, and project membership
- `provision.sql`: runs `provision-base.sql` and `provision-test.sql` in order
- `upgrade-0.5.0.sql`: manual additive upgrade from public 0.4.0/0.4.1, including V016/V017 and runtime grants
- `migrations/V016__project_repository_confirmation.sql`, `migrations/V017__git_connections.sql`: included by that upgrade entrypoint
- `scripts/demo/onbrick-seed.sql`: optional OnBrick showcase seed

These files are copied into the generated public output as the release DB entrypoints.
The base bundle includes the editable Knowledge Hub schema and its project-scoped
`knowledge:*` role permissions.

## Notes

- Do not run shared test data in production environments.
- Scenario-specific story content is not part of the default provisioning path.
- The only public showcase scenario is the OnBrick brownfield story.
- `make demo-onbrick` removes all project volumes without confirmation before rebuilding and seeding. Running `onbrick-seed.sql` alone also deletes existing business data across the demo tenant, including other projects. Use it only for a newly provisioned, disposable evaluation DB; see [Database Commands](../../docs/public-guide.md#database-commands).
- `make db-reset` destroys the database; `make db-reset-full` destroys all Compose project volumes. Neither is an upgrade procedure.

## Related Files

- [../../docs/public-guide.md](../../docs/public-guide.md)
- [../../README.md](../../README.md)

AI session tables and AI configuration values are not created in this free distribution.
The shared `ai_reports` and `embeddings` storage tables remain because project snapshot,
restore and deletion paths reference them. This does not install AI functionality.

`provision-base.sql` is for a new, empty database only. It has no preflight check that
rejects an existing database before writes: it can change database defaults and existing
storage settings before failing on an already existing table. Do not rely on that error
to protect existing data. `provision.sql` also loads the
public evaluation accounts, so it is an evaluation-only wrapper. None of these entrypoints
is an in-place migration script. An automatic in-place upgrade bundle is not included.
For existing 0.4.0/0.4.1 databases, follow the separate
[manual 0.5.0 upgrade procedure](../../docs/deploy/repository-050.md).

For non-default DB/storage values, export the shared settings before Make commands. See
[Make and Compose settings](../../docs/public-guide.md#shared-make-and-compose-settings). Make does not load `.env`.

Only the reviewed Free database assets are included in this distribution.


## Diagram storage and permissions

Fresh provisioning already includes the diagram schema. For a standard Free
0.5.0 or 0.5.1 database, stop writers and take a matching database and API file
backup. Rehearse on a restored copy. From the release root, set `DATABASE_URL`
to the target database using its migration owner. Apply the four files together
so a failure rolls back the transaction:

```sh
psql "$DATABASE_URL" -X -v ON_ERROR_STOP=1 --single-transaction \
  -f scripts/db/migrations/V021__diagram_revisions.sql \
  -f scripts/db/migrations/V022__sync_role_permission_matrix.sql \
  -f scripts/db/migrations/V023__diagram_schema2.sql \
  -f scripts/db/migrations/V024__diagram_model_hash.sql
```

V021 creates the diagram and revision tables. V022 synchronizes the permissions
of the six built-in Free roles. Review custom roles separately in role administration.
V023 allows schema2 and preserves schema1 documents and revisions. V024 takes a
table lock during the transaction and backfills the SHA-256 fingerprint of each
stored model. The updated API requires that column.

If the API uses a separate database role, check its access to the new tables
before starting it. Where default privileges do not cover newly created tables,
the migration owner can grant the runtime role the usual project-data permissions.
Set `APP_DB_ROLE` to the existing API database role:

```sh
psql "$DATABASE_URL" -X -v ON_ERROR_STOP=1 -v app_role="$APP_DB_ROLE" <<'SQL'
GRANT SELECT, INSERT, UPDATE, DELETE ON public.diagrams, public.diagram_versions TO :"app_role";
SQL
```

Start the updated API and frontend together. In a project where you have access,
save a diagram, make and save a second edit, then reopen it and compare the two revisions.

To return to 0.5.x, restore the matching pre-upgrade database and API files with
the previous application. For a rollback of V024 alone to a V023 application,
switch to that application first and apply `R024__drop_diagram_model_hash.sql`
in a transaction. R024 removes the derived fingerprint, including copies in
project snapshots, and preserves diagram bodies and revisions.
