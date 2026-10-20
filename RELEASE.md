# SDDP v0.5.0 — Free Edition

Release files prepared on 2026-10-20. This document describes the free public distribution.
It does not announce a hosted service deployment or include separately distributed AI modules.

## Changes

### Git File Viewer

- Browse committed files and search all file names/paths, including unopened folders.
- Select a Git history entry to read that complete version, including the path before a rename, or return to the latest locally synchronized revision.
- See File Info and Git metadata separately, followed by links to current Artifacts, Tasks, Specs, Requirements and Conversations.
- Keep search results after selecting a file, reveal the selected tree row, and preserve independent file/revision/search state across tab switches and splits.
- Restore the selected file and historical revision from its URL after a browser reload. The page's Refresh repository action also retains the search and expanded folders; browser reloads do not persist search or custom panel widths.
- Reuse shared panels, collapsible sections, file icons and scrollbars; retain side-panel widths while resizing the window and show file status in the app status bar.

Start with [repository connection and Files usage](docs/public-guide.md#connect-a-project-repository).
Use Settings → Project → General → Git Repository to save a draft, check the connection and confirm it.
File relationships require confirmation. Upgrade existing databases with V016 and V017; see the
[repository upgrade notes](docs/deploy/repository-050.md).
Refresh follows the configured synchronization interval and does not force a remote fetch.
File editing, branch switching and before/after comparisons are excluded.

### Repository setup and access

- Free supports public repositories only. The server rejects authentication token registration and
  repositories that require authentication.
- Administrators can register public GitHub/Gitea connections for a specific project in
  Settings → System Config → Git Connections. Project owners select a connection in General.
- Confirmation locks the repository URL, branch and Artifact root. Synchronization timing remains
  editable; changing the selected connection rechecks access and existing file paths before saving.
- Anonymous GitHub Files access checks current Git readability without a metadata API request for every read.
- Artifact source reads enforce project access. Reload refreshes the repository form, and snapshot restore
  preserves the current repository configuration rather than replacing it with historical settings.

### Shared interaction

- Confirmation actions use the application's common Modal. Cancelling Knowledge archive makes no change.
- Sidebars with at least two supported panels provide Single Expand and Collapse All in their top
  overflow menu. Single Expand is off by default, persists across browser reloads and retains the
  chosen expansion while the project list refreshes.

The runtime, dependency and OnBrick corrections from 0.4.1 remain included. Existing OnBrick demo
databases have a targeted [repair procedure](docs/public-guide.md#validation).
Release validation requires matching frontend/API versions, builds, tests, database/API checks,
edition-boundary checks and dependency/secret scans. These checks run locally before publication.

See [CHANGELOG.md](CHANGELOG.md) for version-by-version changes.

## Included and excluded

The free edition includes project conversations, requirements, specs, Knowledge, glossary,
artifacts, tasks, effort, permissions, the Git File Viewer and the OnBrick evaluation scenario.
Optional local Whisper dictation remains available; it is separate from LLM assistance.

AI/LLM assistance, semantic search, AI binaries, private harness source, Ollama services and
code-generation tools are not included. The global Search and SLA policy management screens
remain unavailable. SLA APIs remain available.

Shared `ai_reports` and `embeddings` tables, common capability types and disabled frontend adapters
remain for compatibility with project snapshot, restore and deletion operations. Their names may
appear in the source; they do not include an executable AI module. AI session tables, provider/model
settings and configuration seed values are not provisioned.

## Install and evaluate

Follow [the public guide](docs/public-guide.md) and [database entrypoints](scripts/db/README.md).
Use Node.js from `.nvmrc` and the exact .NET SDK in `global.json` for local builds.
The default Docker Compose profile is for local evaluation: host ports bind to loopback,
and the example DB/JWT/admin/demo credentials are public values, not production secrets.

## Upgrade precautions

**v0.5.0 does not support automatic in-place upgrades from existing installations.**
For existing 0.4.0/0.4.1 databases, follow the reviewed manual
[V016/V017 procedure](docs/deploy/repository-050.md) and its
[SQL entrypoint](scripts/db/upgrade-0.5.0.sql). Test it on a restored copy before upgrading your installation;
a clean installation is not a migration. Older or customized schemas need a separate migration review.

Back up both the database and API files; follow [API data preservation](docs/deploy/api-data.md).
Upgrade the API and frontend together. `provision-base.sql` is for a clean database, not an
in-place upgrade. Do not run reset commands or demo/test seed scripts against production data.
This source export does not include an automatic in-place migration bundle or registry image digest lock.
After both services are updated, reload open browser tabs and check the version in the application
status bar against the API version. A tab left open during deployment can retain references to old
JavaScript files; reload it if a lazily loaded screen fails to open.

For production use a separately reviewed configuration with unique credentials and private
service access. The API rejects published DB/JWT defaults and the known default administrator
password outside the explicitly allowed development profile. Demo-account passwords must also
be replaced or their accounts removed; the administrator guard does not audit every test account.

## Public validation commands

After `npm ci` in `src/frontend`, run `make build`, `make test` and `make lint`.
`make test` runs the deliberately limited UI-control and public error-handler unit suites
once, with missing tests treated as an error. It is not the complete
internal product suite; backend/internal AI tests are not shipped.

The two OnBrick smoke tests require evaluation services, scenario seed data and Chromium.
The headless smoke configuration avoids shared setup and Redis flushes; the release gate runs
it on disposable services in addition to the unit suite. It verifies Spec creation/readback and
Channel content, not the entire multi-day scenario. Follow [public validation](docs/public-guide.md#validation).
Those tests do not cover repository connection or Files. Use the separate
[Files verification checklist](docs/public-guide.md#verify-the-files-view) on the candidate installation.

## Known limitations

Repository replacement/unlock and SSH setup are not included. Use a public HTTPS repository;
repositories that require authentication are not supported in the free edition.
Git metadata is commit-based: Modified is the last change's commit time and Owner is the linked
SDDP Artifact owner. Related SDDP records are current, even when viewing an older commit.

The 2026-10-20 npm audit reported no High/Critical findings and retained seven Moderate
findings across all dependencies, including two runtime findings in the Monaco/DOMPurify
dependency chain. The NuGet audit reported no vulnerabilities across the five public backend
projects, including transitive dependencies. These dated audits do not prove that the application
is free of vulnerabilities. Published docs and tests describe the limited scope above.
