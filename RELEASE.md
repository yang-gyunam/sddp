# SDDP v0.4.1 — Free Edition

Release files prepared on 2026-10-18. This document describes the free public distribution.
It does not announce a hosted service deployment or include separately distributed AI modules.

## Changes

- The ASP.NET Core runtime is updated to 10.0.13 with a pinned image digest.
- ProseMirror and affected transitive dependencies are updated within the existing package ranges.
- Shared conversation and snapshot integration retains free-edition compatibility. Disabled AI UI adapters preserve normal non-AI operation.
- The frontend status bar and API report the same release version. Public builds, tests, fresh-database/API behavior and edition boundaries are checked locally.
- Public provisioning continues to omit AI settings, providers, models and endpoints. AI settings remain hidden even if an AI flag is set without a module.

- OnBrick seed data now initializes artifact/glossary update owners and empty glossary lists, preventing project detail HTTP500. Existing demo databases have a targeted [repair procedure](docs/public-guide.md#validation).
- The release gate now requires both real OnBrick smoke tests, exact project statistics and legacy audit repair checks on disposable services.

See [CHANGELOG.md](CHANGELOG.md) for version-by-version changes.

## Included and excluded

The free edition includes project conversations, requirements, specs, Knowledge, glossary,
artifacts, tasks, effort, permissions and the OnBrick evaluation scenario.
Optional local Whisper dictation remains available; it is separate from LLM assistance.

AI/LLM assistance, semantic search, AI binaries, private harness source, Ollama services and
code-generation tools are not included. The global Search and SLA policy management screens
remain unavailable. SLA APIs remain available.

Shared `ai_reports` and `embeddings` tables remain for compatibility with project snapshot,
restore and deletion operations. These empty storage structures do not install an AI module;
AI session tables and AI configuration seed values are not provisioned.

## Install and evaluate

Follow [the public guide](docs/public-guide.md) and [database entrypoints](scripts/db/README.md).
Use Node.js from `.nvmrc` and the exact .NET SDK in `global.json` for local builds.
The default Docker Compose profile is for local evaluation: host ports bind to loopback,
and the example DB/JWT/admin/demo credentials are public values, not production secrets.

## Upgrade precautions

**v0.4.1 does not support automatic in-place upgrades from existing installations.**
Use a separate, reviewed migration plan to preserve existing data; a clean installation is not a migration.

Back up both the database and API files; follow [API data preservation](docs/deploy/api-data.md).
Upgrade the API and frontend together. `provision-base.sql` is for a clean database, not an
in-place upgrade. Do not run reset commands or demo/test seed scripts against production data.
This source export does not include an automatic in-place migration bundle or registry image digest lock.

For production use a separately reviewed configuration with unique credentials and private
service access. The API rejects published DB/JWT defaults and the known default administrator
password outside the explicitly allowed development profile. Demo-account passwords must also
be replaced or their accounts removed; the administrator guard does not audit every test account.

## Public validation commands

After `npm ci` in `src/frontend`, run `make build`, `make test` and `make lint`.
`make test` runs the deliberately limited UI-control and public error-handler unit suites
(6 files, 51 cases), once, with missing tests treated as an error. It is not the complete
internal product suite; backend/internal AI tests are not shipped.

The two OnBrick smoke tests require evaluation services, scenario seed data and Chromium.
The headless smoke configuration avoids shared setup and Redis flushes; the release gate runs
it on disposable services in addition to the unit suite. It verifies Spec creation/readback and
Channel content, not the entire multi-day scenario. Follow [public validation](docs/public-guide.md#validation).

## Known limitations

The 2026-10-18 npm audit reported no High/Critical findings and retained seven Moderate
findings across all dependencies, including two runtime findings in the Monaco/DOMPurify
dependency chain. This is a dated dependency audit, not proof that the application is free
of vulnerabilities. No model-quality claim applies to the free edition. Published docs and
tests describe the limited scope above.
