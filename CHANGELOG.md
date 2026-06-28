# Changelog

All notable changes to this project will be documented in this file.

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
