# SDDP v0.6.1 — Free Edition

Prepared on 2026-11-19.

## Changes

- Update the API container to the [ASP.NET Core and .NET 10.0.15 security release](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- Preserve readable Diagram drafts when a save or historical comparison loses access. Fix YAML import and automatic Diagram refresh.
- Keep weekly statistics sharing opt-in and isolate delivery failures by tenant. `/health` reports missing consent storage or database permissions.

See the [changelog](CHANGELOG.md) for the full list.

## Install and evaluate

Start with the [Docker quick start](docs/public-guide.md#quick-start-docker-only). Replace the example credentials and omit demo data for production.

The Free edition retains report and embedding tables for project snapshot compatibility. AI execution modules are distributed separately. See the [Free edition scope](docs/public-guide.md#free-edition-support-scope).

## Upgrade precautions

Upgrades are manual. Stop writers, back up the database and API files together, and rehearse on a restored copy. Update the API and frontend together. [Backup and recovery guide](docs/deploy/api-data.md)

- From Free 0.5.0 or 0.5.1, apply [V021–V025](scripts/db/README.md#diagram-storage-and-permissions) before starting the updated API.
- From Free 0.6.0, apply [V025](scripts/db/README.md#weekly-statistics-consent) if your earlier upgrade stopped at V024.
- From Free 0.4.0 or 0.4.1, complete the [V016/V017 repository upgrade](docs/deploy/repository-050.md) first. Older or customized schemas need a migration review.

Before reopening a restored installation, replay recorded deletions and clear restored statistics consent. [Restore a backup safely](docs/deploy/api-data.md#restore-a-backup-safely)

After the upgrade, reload open browser tabs, check `/health`, and confirm that the API container reports `Microsoft.AspNetCore.App 10.0.15` with `dotnet --list-runtimes`.
