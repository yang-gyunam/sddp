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
- `scripts/demo/onbrick-seed.sql`: optional OnBrick showcase seed

These files are copied into the generated public output as the release DB entrypoints.
The base bundle includes the editable Knowledge Hub schema and its project-scoped
`knowledge:*` role permissions.

## Notes

- Do not run shared test data in production environments.
- Scenario-specific story content is not part of the default provisioning path.
- The only public showcase scenario is the OnBrick brownfield story.
- `make demo-onbrick` removes all project volumes without confirmation before rebuilding and seeding. It is not a seed-only command. For seed-only instructions on an existing evaluation DB, see [Database Commands](../../docs/public-guide.md#database-commands).
- `make db-reset` destroys the database; `make db-reset-full` destroys all Compose project volumes. Neither is an upgrade procedure.

## Related Files

- [../../docs/public-guide.md](../../docs/public-guide.md)
- [../../README.md](../../README.md)

AI session tables and AI configuration values are not created in this free distribution.
The shared `ai_reports` and `embeddings` storage tables remain because project snapshot,
restore and deletion paths reference them. This does not install AI functionality.

`provision-base.sql` rejects a non-empty application database. `provision.sql` also loads the
public evaluation accounts, so it is an evaluation-only wrapper. None of these entrypoints
is an in-place migration script. An automatic in-place upgrade bundle is not included.

For non-default DB/storage values, export the shared settings before Make commands. See
[Make and Compose settings](../../docs/public-guide.md#shared-make-and-compose-settings). Make does not load `.env`.
