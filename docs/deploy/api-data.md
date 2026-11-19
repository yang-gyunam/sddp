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

## Restore a backup safely

1. Stop the API and any other writers or scheduled jobs before restoring. Keep the recovery target
   isolated from normal traffic. Restore the matching database, API files, and Gitea data together.
   If this is an upgrade rehearsal, apply the migrations for the target version before starting the API.
2. Use the latest privacy journal kept independently of the application backup, with its matching
   managed-file vault. Use absolute, owner-only paths for the two separate directories. If this
   installation has never used the privacy tool, it can initialize new directories to clear restored
   statistics consent. If a previously used journal or its matching vault is missing, recover it before
   reopening the installation so that past deletions can be replayed.
3. On the recovery host, use Python 3.11 or later and `psql`. Configure a libpq service such as
   `sddp-restored` that points only to the restored database. Use a maintenance role allowed to
   read, lock, and delete affected records and create temporary tables. From the release directory,
   substitute your paths and service name and run

   ```bash
   python3 scripts/privacy/lifecycle.py \
     --journal /path/to/latest/privacy-journal \
     --vault /path/to/matching/privacy-vault \
     --product-service sddp-restored restore-replay
   ```

   The command reapplies recorded deletions and clears all restored weekly statistics consent,
   including pending reports. Check that it succeeds and reports `"sharingRequiresFreshConsent": true`.
   Resolve any hold, fingerprint mismatch, or other replay error before starting the API.
4. Start the API and verify `/health`, descriptor hashes, Git HEAD, and DB artifact references before
   reopening access. For versions with weekly statistics, confirm that external sharing is off and
   no saved permission remains in **Settings → System Config → Usage statistics**. A tenant
   administrator must opt in again to resume sharing.
