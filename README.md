# SDDP — Spec-Driven Design Platform

> Understand the decision behind the code.

Six months later, the code is still there. Is the reason behind it?

SDDP is a self-hosted workspace that connects conversations, requirements, specs, tasks and repository files. Follow a decision into implementation—or open a file and find the agreement, work and discussion behind it.

**People decide. Specs preserve the agreement. The context stays connected.**

[Get started](#quick-start) · [Explore Files](#start-with-a-file-find-the-context) · [Public guide](docs/public-guide.md) · [Release notes](RELEASE.md)

## Follow a Decision into Implementation

“Should a partial refund use a percentage or a fixed amount?”

The answer starts in a conversation. In SDDP, the team can carry that agreement into a requirement, review the spec, and link the implementation artifact. The next person can follow the same trail.

![The OnBrick demo follows a partial-refund discussion into its requirement, spec and linked artifact in SDDP 0.5.0](docs/assets/readme/01-decision-trail.gif)

- **Keep the reasoning.** Record proposals, objections and decisions alongside the work they inform.
- **Make agreement explicit.** Review and approve specs, then lock them when the team is ready.
- **Keep implementation in context.** Link artifacts and tasks to specs, and inspect artifact status and content-hash verification.

These are explicit links maintained by the team. They preserve traceability; they do not prove that code satisfies every requirement.

## Start with a File. Find the Context.

New in **0.5.0**, the Git File Viewer brings the repository into the project workspace. Search file names and paths across the whole repository, including unopened folders, then read the file with its context beside it.

| Beside each file | What you can find |
| --- | --- |
| **File Info** | File size, last-change commit time and linked Artifact owners |
| **Git** | Commit author, commit details and file change history |
| **Project context** | Links to Artifacts, Tasks, Specs, Requirements and Conversations |

![Searching the public repository in SDDP 0.5.0 and opening the project records linked to a file](docs/assets/readme/02-project-knowledge.gif)

Choose a commit to read an earlier version of the file. SDDP relationships remain the **current records you can access**, even when viewing historical content.

Open **Projects → your project → Files** after [connecting a public repository](docs/public-guide.md#connect-a-project-repository). Save the repository settings, check the connection and confirm them before creating file relationships.

Files is read-only in this release. File editing, branch switching and before/after comparisons are outside its scope.

## A Shared Memory for the Project

| When you need to… | Use… |
| --- | --- |
| Understand what the team agreed | Conversations, requirements and versioned specs |
| See how the work connects | Artifact links and the Traceability graph |
| Plan and account for implementation | Tasks, assignments and effort tracking |
| Keep language and knowledge consistent | The glossary and Knowledge entries |
| Capture a thought by speaking | Optional local Whisper dictation in conversations |

This repository is the **Free edition**, licensed under Apache-2.0. It supports public repositories without authentication. AI assistance, semantic search and code-generation tools are not included; optional Whisper transcription runs locally.

## Quick Start

**Upgrading from 0.4.0 or 0.4.1?** Complete the [manual database upgrade](docs/deploy/repository-050.md) before starting the updated API.

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
Host Node.js and .NET are not required for this path. These commands start PostgreSQL, Redis,
the API and the web frontend.

Optional — speech-to-text for conversation messages (local Whisper):

```bash
docker compose up -d sddp-whisper
make whisper-warmup    # one-time model download (~460MB)
```

For logs and traces in a dashboard, you can also start the
[optional Aspire Dashboard](docs/public-guide.md#optional-telemetry-dashboard).
It is not required to run SDDP and is not started by the Quick Start commands above.

### Local Frontend Development (Optional)

After the Docker setup above, use this path for Vite HMR. Run commands from the repository root:

```bash
cd src/frontend
npm ci
cd packages/web
VITE_API_URL=http://localhost:5001 npm run dev
```

Vite has no API proxy. This command sets the API origin for this local process only;
substitute your API host and port if different. Keep `VITE_API_URL` empty for the
Docker frontend, where nginx proxies `/api` and `/hubs` requests.

### Service URLs

| Service | URL |
|---------|-----|
| Frontend (Docker) | http://localhost:9010 |
| Frontend (Vite dev, optional) | http://localhost:3500 |
| API | http://localhost:5001 |
| API Docs (Scalar) | http://localhost:5001/scalar/v1 |
| Aspire Dashboard (optional, when started) | http://localhost:18888 |
| Whisper STT (optional) | http://localhost:9020 |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Svelte 5, TypeScript, TailwindCSS, Vite |
| Backend | .NET 10, C#, ASP.NET Core |
| Database | PostgreSQL (EF Core + Dapper) |
| Cache | Redis |
| Logging | Serilog; optional Aspire Dashboard for telemetry |

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

The public repository includes a **limited test subset**, not full product acceptance tests.
The paths below are relative to `src/frontend/packages/`. Run `make` from the repository
root and the listed `npm` commands from `src/frontend`.

| Included tests | Source location | Execution | Scope |
| --- | --- | --- | --- |
| UI controls | [ui/src/controls/__tests__/](src/frontend/packages/ui/src/controls/__tests__/) | `make test` | Button, Input, Icon, IconButton and Badge rendering and interactions. |
| Error handler | [web/tests/unit/shell/utils/error-handler.test.ts](src/frontend/packages/web/tests/unit/shell/utils/error-handler.test.ts) | `make test` | Global error listeners and reporting to the Problems panel. |
| OnBrick smoke | [web/tests/e2e/S/S2-onbrick.full-story.spec.ts](src/frontend/packages/web/tests/e2e/S/S2-onbrick.full-story.spec.ts) | `npm run test:e2e:smoke -w @sddp/web` — separate from `make test` | Two scenario tests: Spec creation/readback/cleanup and access to seeded project conversations. |
| Additional L0 scenarios | [web/tests/e2e/L0/](src/frontend/packages/web/tests/e2e/L0/) | `npm run test:e2e:L0 -w @sddp/web` — separate from `make test` and OnBrick smoke | Selected shell, login, API, visual and accessibility scenarios; not a complete application test suite. |

`make test` runs only the two unit-test groups above, plus workspace dependency and lockfile
checks. It does not exercise repository setup, Files browsing/history, or complete business
workflows. .NET backend test projects and AI tests are not shipped. `make lint` runs the web
package's ESLint and Svelte/TypeScript checks; it does not execute application tests.

Browser tests require a prepared evaluation stack, seed data and a Playwright browser;
follow [public validation](docs/public-guide.md#validation) before running OnBrick smoke.
That headless smoke avoids shared global setup and Redis flushes. The additional L0 and
older headed showcase configurations use global setup that clears the evaluation Redis
instance and creates or updates an E2E project. Use disposable services for those commands.
Passing these checks does not establish that all SDDP features work.

## Contributing

Issues and pull requests are reviewed on a best-effort basis.

## License

[Apache-2.0](LICENSE)
