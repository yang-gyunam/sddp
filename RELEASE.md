# SDDP v0.4.0 — Free Edition

Release files prepared on 2026-10-03. This document describes the free public distribution.
It does not announce a hosted service deployment or include separately distributed AI modules.

## Changes

- Browser back/forward navigation and item URLs now preserve project context and multiple panels.
- Related-item links use project-scoped routes. Dark-theme surfaces, theme labels and list row heights are more consistent.
- The frontend status bar and API report the same release version.
- The transitive devalue dependency is updated to 5.9.4; Svelte remains at 5.56.6.
- Public provisioning omits AI settings, providers, models and endpoints. AI settings remain hidden even if an AI flag is set without a module.

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

**v0.4.0 does not support automatic in-place upgrades from existing installations.**
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

OnBrick browser tests require evaluation services, scenario seed data, a Playwright browser
and a graphical desktop. Their setup clears evaluation Redis and the tests write data. Follow
[public validation](docs/public-guide.md#validation); the unit results do not claim an OnBrick run.

## Known limitations

The 2026-10-03 npm runtime audit reported no High/Critical findings and retained one Moderate
DOMPurify finding plus one dependent Low Monaco finding. This is a dated dependency audit,
not proof that the application is free of vulnerabilities. No model-quality claim applies to
the free edition. Published docs and tests describe the limited scope above.
