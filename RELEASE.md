# SDDP v0.6.0 — Free Edition

Release files prepared on 2026-11-18. This document describes the free public distribution.
It does not announce a hosted service deployment or include separately distributed AI modules.

## Changes

Open **Project → Diagrams** to create a flowchart, sequence, class, state, ERD,
or C4 context/container diagram. Drag shapes from the palette, resize or connect
them, then save explicitly. Open a related item from either detail panel to view
it in a separate work tab.

- Compare saved revisions in Visual, YAML or Source view. The visual comparison
  distinguishes changes to the model from movement and styling.
- Import and export SDDP YAML. Preview supported Mermaid syntax before saving.
  If you edit the diagram visually, the editor marks the preserved source as stale.
- Primary Flow and Traceability Map now apply the same project and private
  conversation read checks as related-item views. A readable child remains
  visible when its parent is private.

See the [changelog](CHANGELOG.md) for the full list of changes.

## Included and excluded

The free edition includes project conversations, requirements, specs, Knowledge, glossary,
artifacts, tasks, effort, permissions, Diagrams, the Git File Viewer and the OnBrick evaluation scenario.
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

**v0.6.0 does not support automatic in-place upgrades from existing installations.**
For a standard Free 0.5.0 or 0.5.1 installation, stop writers and take a matching
backup of the database and API files. Rehearse on a restored copy, then apply the
[four diagram migrations, V021 through V024](scripts/db/README.md#diagram-storage-and-permissions)
before starting the updated API. Update the API and frontend together.

For Free 0.4.0 or 0.4.1, complete the earlier [V016/V017 repository upgrade](docs/deploy/repository-050.md)
first. Older or customized schemas require a separate migration review.
To return to 0.5.x after enabling diagrams, restore the matching pre-upgrade database
and [API files](docs/deploy/api-data.md) with the previous application version.

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

The 2026-11-18 audit found zero npm runtime vulnerabilities and zero High or Critical
findings across the five public NuGet projects, including transitive dependencies.
Including npm development tools yielded ten High and ten Moderate findings. The High
findings include a [picomatch advisory](https://github.com/advisories/GHSA-fc5q-39xc-vjpf)
and affected tools that depend on it. The advisory listed no patched version at the time
of the audit. The frontend production image serves the built assets through Nginx.
Published docs and tests describe the limited scope above.

The workspace root also declares the existing Monaco dependency so npm consistently applies
the DOMPurify 3.4.17 override across workspace links. Keep this declaration and the lockfile
together; changing dependency resolutions requires another audit and build.
