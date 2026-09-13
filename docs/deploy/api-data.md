# Preserve API files during upgrades

The API stores artifact descriptors and Git working directories under `/app/data` by default.
The `api_data` named volume persists this directory across container replacements. Automatic bulk
reconstruction of descriptors from the database is not implemented.

For an existing installation, **stop API writes and copy the old container's `/app/data` before
recreating it**. Mounting a new empty volume does not recover the old container's writable layer.
Keep the old container until you have verified the migrated files and the matching database backup.

1. Record your `Git__RepoRoot` and `ArtifactDescriptor__Root` storage settings. Paths outside `/app/data` need separate mounts
   and backups. Record the current API image ID and the database backup from the same maintenance window.
2. Stop the old API container. Copy `/app/data/.` to a new private host backup directory with `docker cp`.
3. Import that snapshot into the new `api_data` volume, using a temporary container based on the old
   API image. Restore ownership to `appuser`. The default volume name is `sddp-public_api_data`;
   verify it with `docker compose config` if you changed the project name.
4. Start the new API with the new Compose file. Verify descriptor hashes, Git HEAD, and DB artifact
   references before removing the old container. Retain the host snapshot for rollback.

The development repository provides `scripts/upgrade/preserve-api-data.sh` for this migration.
It requires a stopped source container and new destination volume/backup directory. This operator
script is maintained by the project maintainers; it is not a command shipped in the public application.

Back up PostgreSQL, API files, and Gitea data together with application writes stopped. A database-only
backup does not preserve these files. Do not use `docker compose down -v` during an upgrade: it deletes
named volumes. Changing the Compose project name also selects different volumes.
