# Changelog

All notable changes to this project will be documented in this file.

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
