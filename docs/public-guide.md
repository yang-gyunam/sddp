# Public Guide

This guide covers the supported public-repository workflow for running SDDP, provisioning the database, and executing the public validation paths.

## Scope

The public repository includes:

- the backend and frontend application code
- the public test suite
- database provisioning scripts
- the OnBrick showcase scenario

## Requirements

- Docker
- Node.js 22 LTS recommended
- .NET 10 SDK

## Quick Start (Docker Only)

```bash
cp src/frontend/packages/web/.env.example src/frontend/packages/web/.env

docker compose up -d sddp-db sddp-redis sddp-aspire
make db-provision
make db-seed-test
docker compose up -d sddp-api sddp-front
```

Host-side `npm install` is not required when you use the frontend container only.

Optional — speech-to-text for conversation messages (local Whisper, no cloud calls):

```bash
docker compose up -d sddp-whisper
make whisper-warmup    # one-time model download (~460MB)
```

For local frontend development:

```bash
make infra
docker compose up -d sddp-api
cd src/frontend
npm install
cd packages/web
npm run dev
```

## URLs

- Frontend: `http://localhost:9010`
- Frontend dev server: `http://localhost:3500`
- API: `http://localhost:5001`
- API docs: `http://localhost:5001/scalar/v1`
- Aspire Dashboard: `http://localhost:18888`
- Whisper STT (optional): `http://localhost:9020`

## Database Commands

```bash
make db-provision      # schema + base data (clean DB only)
make db-seed-test      # shared test data
make db-reset          # drop + recreate + provision
make db-reset-full     # docker volumes removed + fresh start
make demo-onbrick      # OnBrick showcase seed
```

- `make db-provision` runs on a **clean database only**. If the schema already exists, use `make db-reset` first.
- `make db-seed-test` loads the tracked public shared test dataset.
- `make demo-onbrick` loads the public OnBrick showcase seed.

## Validation

```bash
make build
make test
make lint

cd src/frontend
npm run test:e2e
```

`make build` runs the frontend from the workspace root so the `ui`, `shell`, and `activities`
packages are generated first and the web production bundle consumes their `dist/` exports.

In the public repository, `npm run test:e2e` runs the OnBrick showcase path only.

The public showcase runner is:

```bash
cd src/frontend/packages/web
./scripts/run-e2e-showcase.sh
```

## Contribution Model

Issues and pull requests are reviewed on a best-effort basis.

## Preserve files when upgrading

Before replacing an existing API container, follow [API data preservation](deploy/api-data.md).
The new `api_data` volume preserves descriptors and Git working directories; existing container files
must be copied into it before recreation. Database-only backups do not include these files.

## Artifact generation scope

The Generation tab displays artifacts recorded on the server; selecting output types does not run a
code generator. Generation tools and the internal CLI are not included in this public distribution.
Contact your deployment operator for separately managed generation. The legacy regeneration endpoint
returns HTTP 410; its `GENERATION_VIA_CLI` error code is retained for compatibility, not as an instruction
to install a public CLI package.

## Version 0.3.0 support scope

The global Search UI and SLA policy management UI are not supported in 0.3.0. SLA policy APIs remain available; no SLA management screen is included. Semantic search (`POST /api/search`) and its AI/Ollama dependencies are excluded from this public distribution. Existing project and entity-specific navigation is unchanged.
