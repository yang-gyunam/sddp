# SDDP — Spec-Driven Design Platform

> People decide, specs remember, code proves.

SDDP turns discussions and agreements into structured specs and keeps implementation changes traceable against those specs.

Code explains what changed. SDDP keeps the why.

This repository contains the free edition: conversations, requirements, specs, Knowledge, glossary, artifacts, tasks, effort tracking and the Git File Viewer. AI assistance, semantic search and code-generation tools are not included. Optional local Whisper dictation is available separately.

## One Decision, End to End

This recorded example follows a partial-refund decision from a discussion to a requirement, a reviewed spec and a linked implementation artifact. Artifact links preserve traceability; they do not prove that code satisfies every requirement.

![SDDP following a partial-refund decision from conversation through requirement and spec to a linked implementation artifact](docs/assets/readme/01-decision-trail.gif)

## Why SDDP?

If you open code written six months ago and can no longer explain why it looks that way, SDDP is the problem space we care about.

- Preserve decision context through **Conversation -> Requirement -> Spec -> Code**
- Review and approve specs, then explicitly lock them; preserve decisions through versions
- Track recorded artifact status and content-hash verification results
- Explore committed files, whole-file Git history and the current SDDP records linked to each file
- Dictate conversation messages with local speech-to-text (Whisper, no cloud calls)

Public setup and evaluation notes are in [docs/public-guide.md](docs/public-guide.md).

## Git File Viewer — 0.5.0

Open **Projects → your project → Files** to search every committed file path, browse folders,
read a historical version and follow links to Artifacts, Tasks, Specs, Requirements and Conversations.
The side panels use the shared project layout; file status appears in the app status bar.
Historical file URLs survive browser reloads, and split tabs retain independent file and search state.

[Connect the project's repository](docs/public-guide.md#connect-a-project-repository) in
**Settings → Project → General → Git Repository**. Save a draft, check the connection and confirm
it before saving file relationships. Free supports public repositories without authentication.
Administrators can register a project-scoped public GitHub/Gitea connection in System Config.
Confirmed URL/branch/root settings are locked; sync timing remains editable.
Files is read-only: editing, branch switching and before/after comparisons are outside this release.
Existing 0.4.0/0.4.1 installations must follow the [manual database upgrade](docs/deploy/repository-050.md)
before starting the updated API.

## Explore the Whole Project Context

Move through the vocabulary, implementation artifacts, work, and effort behind a project, then inspect the Traceability graph that connects a spec back to its requirement and code.

![SDDP project views moving from glossary and artifacts through tasks and effort to the traceability graph](docs/assets/readme/02-project-knowledge.gif)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Svelte 5, TypeScript, TailwindCSS, Vite |
| Backend | .NET 10, C#, ASP.NET Core |
| Database | PostgreSQL (EF Core + Dapper) |
| Cache | Redis |
| Observability | Aspire Dashboard, Serilog |

## Quick Start

### Prerequisites

- Git, Docker with Docker Compose v2, Make and a POSIX-compatible shell
- For local frontend development/tests: Node.js from `.nvmrc` (22.23.2) and npm
- For local backend builds: .NET SDK from `global.json` (10.0.300)
- For the optional Whisper warmup command: curl and Python 3

### Docker-Only Setup

```bash
git clone https://github.com/yang-gyunam/sddp.git sddp
cd sddp
cp src/frontend/packages/web/.env.example src/frontend/packages/web/.env

docker compose up -d --wait sddp-db sddp-redis
docker compose up -d sddp-aspire
make db-provision      # new, empty application database only
make db-seed-test
docker compose up -d --build sddp-api sddp-front
```

Open `http://localhost:9010` after startup. Evaluation accounts are defined in
[scripts/db/provision-test.sql](scripts/db/provision-test.sql); OnBrick has separate scenario accounts.
The Compose profile uses loopback ports and public evaluation credentials. Do not use these
credentials or seed data in production. See [upgrade precautions](RELEASE.md#upgrade-precautions)
before replacing an existing installation; reset commands delete data.

Docker builds install the frontend dependencies and use the .NET SDK inside containers.
Host Node.js and .NET are not required for this path.

Optional — speech-to-text for conversation messages (local Whisper):

```bash
docker compose up -d sddp-whisper
make whisper-warmup    # one-time model download (~460MB)
```

### Local Frontend Development (Optional)

After the Docker setup above, use this path for Vite HMR. Run commands from the repository root:

```bash
cd src/frontend
npm ci
cd packages/web
npm run dev
```

### Service URLs

| Service | URL |
|---------|-----|
| Frontend (Docker) | http://localhost:9010 |
| Frontend (Vite dev) | http://localhost:3500 |
| API | http://localhost:5001 |
| API Docs (Scalar) | http://localhost:5001/scalar/v1 |
| Aspire Dashboard | http://localhost:18888 |
| Whisper STT (optional) | http://localhost:9020 |

## Project Structure

```text
src/
├── backend/
│   ├── Sddp.Api/
│   ├── Sddp.Application/
│   ├── Sddp.Domain/
│   ├── Sddp.Infrastructure/
│   └── Sddp.Abstractions/
└── frontend/packages/
    ├── ui/
    ├── shell/
    ├── activities/
    └── web/
```

## Documentation

- [Release notes](RELEASE.md) — free-edition scope, changes and limitations
- [Changelog](CHANGELOG.md) — version-by-version changes
- [GitHub Releases](https://github.com/yang-gyunam/sddp/releases) — published versions and source downloads
- [Public Guide](docs/public-guide.md) — supported setup, database, testing, and repository scope
- [scripts/db/README.md](scripts/db/README.md) — SQL provisioning entry points
- [src/frontend/packages/web/README.md](src/frontend/packages/web/README.md) — web package commands

## Testing

From the repository root, install frontend dependencies before running local checks:

```bash
cd src/frontend
npm ci
cd ../..
make test
make lint
```

`make test` runs the limited UI-control and public error-handler suites.
These are not the full product tests. Backend and AI tests are not shipped.
For the separate OnBrick browser scenario, including seed and browser prerequisites, follow
[public validation](docs/public-guide.md#validation). The headless smoke avoids shared setup and Redis
flushes. The older headed showcase setup clears the evaluation Redis
instance and changes test data; use a disposable evaluation environment.

## Contributing

Issues and pull requests are reviewed on a best-effort basis.

## License

[Apache-2.0](LICENSE)
