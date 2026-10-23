# SDDP v0.5.1 — Free Edition

Release files prepared on 2026-10-23. This document describes the free public distribution.
It does not announce a hosted service deployment or include separately distributed AI modules.

## Changes

This patch improves Knowledge document readability and editing reliability, updates
security-sensitive dependencies, and corrects the public setup and validation guidance
and the bundled Gitea API origin.
The Git File Viewer and repository confirmation features introduced in 0.5.0 remain included.

- Patch the DOMPurify dependency used by Monaco and update compatible Vite, Storybook
  and Vitest dependencies for newly reported security advisories.
- Improve the shared rich-text editor's headings, lists and document spacing, with
  16px body text, 24px line height and 8px paragraph spacing. Knowledge reading,
  editing, history and conflict views use the shared typography.
- Restore the saved body immediately when discarding Knowledge edits, without
  leaving temporary text in the reading view or marking unchanged content as modified.
- Preserve ordered-list numbering types and table-cell alignment when saving and
  reopening Knowledge pages. Retain start numbers, nested lists, column widths and
  merged cells, while continuing to reject unsupported attribute values.
- Use container port 3000 for bundled Gitea API requests and support `SDDP_GIT__SERVERURL`
  overrides for installations with a custom internal address.
- Set the API origin explicitly for local Vite development; Docker continues to use nginx proxying.
- Explain the destructive scope of OnBrick seeding, fresh-database provisioning limits,
  test setup side effects and the separate default-credential guards.
- Refresh the README and animated walkthroughs; identify the shipped tests, their locations,
  commands and limited coverage. Correct the smoke description to Spec creation/readback
  and deactivation. Aspire is optional for the documented local setup.

See [CHANGELOG.md](CHANGELOG.md) for version-by-version changes and the
[public guide](docs/public-guide.md) for repository setup and Files usage.

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

**v0.5.1 does not support automatic in-place upgrades from existing installations.**
For a 0.5.0 installation, this patch adds no database schema changes or new migrations.
Keep the existing database and API files; rebuild and redeploy the API and frontend together.
Do not reprovision the database or rerun demo seeds. If your Gitea container listens on a
custom port (including 3100), set `SDDP_GIT__SERVERURL` to that internal address before
redeploying. Existing saved and confirmed repository URLs are not automatically rewritten.

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
service access. The DB/JWT startup guard rejects published defaults outside Development
and Testing; those two environments are exempt from that guard. The known default
administrator password has a separate guard: it is allowed only in Development with
`Authentication__DevelopmentCredentials__AllowKnownCredentials=true` and
`Authentication__DevelopmentCredentials__ExposureMode=Loopback`. These configuration values
do not themselves restrict network access. Demo-account passwords must also be replaced
or their accounts removed; the administrator guard does not audit every test account.

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

The 2026-10-23 npm audit retained two Moderate development-dependency findings in the
Tailwind Typography/PostCSS selector-parser chain. No runtime, High or Critical npm
findings remained after the dependency updates. The NuGet audit covers all five public
backend projects, including transitive dependencies; consult the candidate validation results.
These dated audits do not prove that the application is free of vulnerabilities.
Published docs and tests describe the limited scope above.

The workspace root also declares the existing Monaco dependency so npm consistently applies
the DOMPurify 3.4.17 override across workspace links. Keep this declaration and the lockfile
together; changing dependency resolutions requires another audit and build.
