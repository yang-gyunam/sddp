# Changelog

All notable changes to this project will be documented in this file.

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
