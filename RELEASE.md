# v0.8.0 (Free Edition)

## Git File Viewer

- Select a folder to see its immediate files and subfolders, with their type, size, ownership, and last Git author.
- Open an owner's or author's profile from their avatar when SDDP can match the person to a profile.
- Read the folder's full repository path next to its name.

## Sound and read-aloud

- Enable sound in Preferences and mute or unmute it from the status bar.
- Play or stop read-aloud on supported content, including conversation messages, in browsers that provide speech synthesis.

## Monitoring high-volume reads

- The API records a warning when a user's successful Files, Artifact source content, Specs, Conversations, or project search reads cross a configured volume or request threshold.
- Operators can route `SDDP_SENSITIVE_READ_THRESHOLD` from API logs to their alerting service. Event records contain aggregate counts and verified IDs.

## Upgrade precautions

- For a new installation, follow the [Docker quick start](docs/public-guide.md#quick-start-docker-only).
- Upgrading from Free 0.7.0 requires no new database migration. Earlier installations must apply outstanding Free migrations through [V027](scripts/db/README.md#project-search) before starting the updated API.
- Stop writers, back up the database and API files together, and rehearse on a restored copy. Follow the [upgrade guide](docs/public-guide.md#preserve-files-when-upgrading) and [safe restore procedure](docs/deploy/api-data.md#restore-a-backup-safely).

See the [changelog](CHANGELOG.md) for details and the [Free edition guide](docs/public-guide.md#free-edition-support-scope) for edition scope.
