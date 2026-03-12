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
make test
make lint

cd src/frontend
npm run test:e2e
```

In the public repository, `npm run test:e2e` runs the OnBrick showcase path only.

The public showcase runner is:

```bash
cd src/frontend/packages/web
./scripts/run-e2e-showcase.sh
```

## Contribution Model

Issues and pull requests are reviewed on a best-effort basis.
