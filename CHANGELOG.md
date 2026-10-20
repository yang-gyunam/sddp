# Changelog

All notable changes to this project will be documented in this file.

## [v0.5.0] - 2026-10-20

### Git File Viewer

- Browse committed project files and whole-file Git history, with File Info and Git metadata shown separately.
- Follow links to current, authorized Artifacts, Tasks, Specs, Requirements and Conversations.
- Search every committed file name/path, including unopened folders, and open results directly.
- Restore a selected historical commit and its original path after browser reloads; remove revision parameters when returning to the latest version.
- Retain search results after file selection, scroll the selected tree row into view, and copy search state when splitting a tab without coupling subsequent changes.
- Preserve chosen side-panel widths during browser resizing, fit file contents into the remaining space, and keep the panel resize handle usable beside the editor scrollbar.
- Reuse shared compact tree/list patterns, show filenames above parent paths in search, and provide collapse/refresh actions beside the search field.
- Show file-type icons consistently and align both editor scrollbars with the app's rounded appearance and theme.
- Align page, panel and relationship components with the existing UI and preserve file, version, search and expanded folders on the page's Refresh repository action. Browser reloads restore file/version URLs but do not persist search or custom panel widths.
- File editing, branch switching and before/after comparisons are outside this release scope.

### Repository setup and access

- Add repository drafts, connection checks and confirmation in Settings → Project → General. Require confirmation before saving the first file relationship and lock the confirmed repository coordinates.
- Allow public repositories only in the free edition. Reject authentication token registration and repositories that require authentication on the server.
- Let administrators register project-scoped public GitHub/Gitea connections in System Config and select the connection in project settings.
- Check anonymous GitHub readability during Files access without consuming the provider's metadata API quota on each read.
- Enforce project access restrictions on Artifact source reads, refresh the repository form after Reload, and preserve the current repository configuration during project snapshot restore.
- Provide a transactional, repeatable V016/V017 SQL entrypoint for reviewed upgrades from 0.4.0/0.4.1. See [repository upgrade notes](docs/deploy/repository-050.md).

### Shared interaction

- Replace browser confirmation dialogs with the shared application Modal; cancelling Knowledge archive leaves the item unchanged.
- Add Single Expand and Collapse All to the sidebar's overflow menu when it contains at least two supported panels. Single Expand defaults to off and its preference survives browser reloads.
- Preserve the sidebar's selected expansion state while project panels reload.

### Known limitations

- The 2026-10-20 dependency audit retained seven Moderate npm findings, including two runtime findings, with no High/Critical findings; the NuGet audit reported no vulnerabilities.
- This free source distribution does not include AI/LLM modules or an automatic in-place upgrade bundle.

See [release notes](RELEASE.md) for installation, upgrade precautions and validation scope.

## [v0.4.1] - 2026-10-18

### Security and compatibility

- Update the ASP.NET Core runtime to 10.0.13 with a pinned image digest.
- Update ProseMirror and affected transitive dependencies within the existing package ranges.
- Keep shared conversation and snapshot integration compatible with the free edition, including disabled AI UI adapters.
- Verify public builds, tests, fresh-database/API behavior and edition boundaries locally.

### OnBrick corrections

- Initialize the sample artifact/glossary audit owners and empty glossary lists and provide a scoped, idempotent repair for existing OnBrick data that causes project detail HTTP500.
- Fix required Spec input, unique test data and Channel loading/accessibility selectors.
- Require both real OnBrick smoke tests and project statistics/legacy repair checks in the release gate.

### Known limitations

- The 2026-10-18 dependency audit retained seven Moderate findings, including two runtime findings, with no High/Critical findings.
- This free source distribution does not include AI/LLM modules or an automatic in-place upgrade bundle.

See [release notes](RELEASE.md) for installation, upgrade precautions and validation scope.

## [v0.4.0] - 2026-10-03

### Fixed

- Improved browser back/forward navigation and multi-panel item URLs across project menus.
- Corrected related-item links, dark-theme surfaces and theme indicators.
- Standardized list row heights and aligned frontend/API version reporting.

### Security and distribution

- Updated the transitive devalue dependency to 5.9.4.
- Kept the public edition free-only, excluding AI modules, model services and AI settings.
- Corrected provisioning guidance and the limited public unit-test commands.

See [release notes](RELEASE.md) for installation, upgrade precautions and known limitations.

## [v0.3.0] - 2026-09-13

### Security

- Strengthened tenant isolation and project-scoped permissions.
- Restricted private conversation references and notifications to eligible members.
- Made refresh-token rotation atomic and kept sessions bound to the selected tenant.
- Added single-use invitations and enforced conversation membership revocation in real time.
- Updated editor dependencies with security fixes.

### Reliability and UI

- Improved Knowledge editing, conflict handling, and unsaved draft preservation.
- Corrected profile fields and added loading feedback to Artifact pages.
- Improved notification retries and failure reporting.
- Added persistent API storage for artifact descriptors and Git working directories.
- Fixed public Docker builds and release version reporting.
- AI/LLM integration and artifact generation remain unavailable in this release.

### Upgrading from v0.2.0

- Back up the database and API files before upgrading.
- Preserve existing container files: [API data preservation](docs/deploy/api-data.md).
- Upgrade the API and frontend together; stop all older API instances first.
- Existing sessions may require signing in again; pending invitations must be issued again.
- Multi-tenant login may return `409 TENANT_SELECTION_REQUIRED`; the updated UI handles selection.
- Malformed resource IDs return `400 INVALID_ID`.
- Conflicting project headers return `400 PROJECT_SCOPE_MISMATCH`.
- Failed notifications stop retrying after five attempts; operator review is required and delivery may repeat.

## [v0.2.0] - 2026-06-28

### Added
- Speech-to-text message dictation in conversations (local Whisper via `sddp-whisper`, max 60s recording with countdown, Korean/English auto-detect)
- `sddp-whisper` service in `docker-compose.yml` for on-premise transcription

### Fixed
- Windows compatibility: shell scripts now enforce LF line endings to prevent container crash loops on CRLF checkout
- Microphone access allowed for same-origin requests via `Permissions-Policy` header (required for dictation)

## [v0.1.0] - 2026-03-12

### Added
- Conversation system (Channel, Forum, DirectMessage)
- Spec lifecycle management (Draft → InReview → Approved → Locked)
- Requirement tracking with priority levels
- Sign-off workflow with SLA monitoring
- Glossary term management
- Relationship & dependency tracking
- Entity metadata schema definition
- Audit logging
- Role-based access control (7 roles, 31 permissions)
- Multi-tenant, multi-project support

### Architecture
- Backend: .NET 10, Clean Architecture, CQRS with MediatR
- Frontend: Svelte 5 (Runes), monorepo (ui → shell → activities → web)
- Database: PostgreSQL with EF Core + Dapper
- Real-time: SignalR WebSocket hubs
