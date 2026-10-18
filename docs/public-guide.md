# Public Guide

This guide covers the supported public-repository workflow for running SDDP, provisioning the database, and executing the public validation paths.

## Scope

This is the free edition. See [release notes](../RELEASE.md) for version 0.4.0 support and limitations.

The public repository includes:

- the backend and frontend application code
- the public test suite
- database provisioning scripts
- the OnBrick showcase scenario

## Requirements

- Git, Docker with Docker Compose v2, Make and a POSIX-compatible shell
- Local frontend builds/tests: Node.js from `.nvmrc` (22.23.2) and npm
- Local backend builds: .NET SDK from `global.json` (10.0.300)
- Optional Whisper warmup: curl and Python 3

Run commands from the repository root unless a `cd` command says otherwise. Docker-only
application builds do not require host Node.js or .NET.

## Shared Make and Compose settings

Make does not parse the root `.env`. For DB provisioning and infrastructure commands, pass
`POSTGRES_USER`, `POSTGRES_DB`, `SDDP_STORAGE_TABLESPACE_NAME`,
`SDDP_STORAGE_TABLESPACE_PATH` and `SDDP_STORAGE_QUOTA_GB` using shell `export` or `make KEY=value`.
Make command-line values take priority over shell values, then defaults. The same selected values
are passed to child Compose and Make commands. If a shared setting exists only in `.env`, these
Make commands stop before running Docker and report the key to export. Do not source dotenv as shell code.

For example, in a new disposable installation:

```bash
export POSTGRES_USER=custom_user POSTGRES_DB=custom_db SDDP_STORAGE_QUOTA_GB=57
# Also export tablespace name/path if set in .env.
make infra
make db-provision
```

Changing these values does not rename an existing database or migrate its volumes. Direct
`docker compose` still reads `.env`; keep the exports when alternating between the two tools.
Passwords remain on the existing Compose configuration path. Public container/service names are fixed.

## Quick Start (Docker Only)

```bash
cp src/frontend/packages/web/.env.example src/frontend/packages/web/.env

docker compose up -d --wait sddp-db sddp-redis
docker compose up -d sddp-aspire
make db-provision      # empty application database only
make db-seed-test
docker compose up -d --build sddp-api sddp-front
```

Host-side `npm ci` is not required when you use the frontend container only.

Optional — speech-to-text for conversation messages (local Whisper, no cloud calls):

```bash
docker compose up -d sddp-whisper
make whisper-warmup    # one-time model download (~460MB)
```

After completing the Docker setup, run the local frontend from the repository root:

```bash
cd src/frontend
npm ci
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
make db-reset-full     # deletes ALL project volumes, then provisions the DB
make demo-onbrick      # deletes ALL project volumes, rebuilds services and seeds OnBrick
```

- `make db-provision` runs on a **clean database only**. Existing installations need an upgrade plan; `make db-reset` destroys the database and is only for deliberate evaluation resets.
- `make db-seed-test` loads the tracked public shared test dataset.
- `make demo-onbrick` runs `docker compose down -v` before rebuilding and provisioning. It removes DB, Redis, API-file and Gitea volumes for this Compose project without an interactive confirmation. Use it only in a disposable evaluation environment.
- To add only OnBrick seed data to an already provisioned evaluation DB, run from the repository root:

```bash
docker compose exec -T sddp-db psql -v ON_ERROR_STOP=1 -U sddp -d sddp -f /scripts/demo/onbrick-seed.sql
```

The command uses the default DB user/database; substitute your configured values if changed.
It adds or updates scenario data rather than resetting volumes. The shared evaluation accounts
are in [provision-test.sql](../scripts/db/provision-test.sql); scenario accounts are in
[onbrick-seed.sql](../scripts/demo/onbrick-seed.sql).

## Validation

Install frontend dependencies first. From the repository root:

```bash
cd src/frontend
npm ci
cd ../..
make build
make test
make lint
```

`make build` requires the local .NET SDK and builds the backend in Release mode. It builds
`ui`, `shell` and `activities` before the web bundle consumes their `dist/` exports.
`make test` runs 6 files / 51 cases covering UI controls and the public error handler, once.
Backend and AI tests are not shipped; this limited suite is not full product acceptance.

For the OnBrick browser scenario, use a disposable evaluation stack prepared above, load the
OnBrick seed, and leave the API running. A graphical desktop is required because this command
uses a headed browser. From the repository root:

```bash
cd src/frontend
npx playwright install chromium
E2E_USE_EXTERNAL_WEB=1 E2E_WEB_BASE_URL=http://localhost:9010 npm run test:e2e
```

This uses the Docker frontend and API. `npm run test:e2e` selects only the OnBrick scenario;
it does not seed OnBrick or run the L0 suite. Its global setup clears **all keys in the Compose
Redis instance** and creates/updates an E2E project; the tests also write application data.
The command is available but a complete OnBrick run is not claimed by the limited unit results.

Without `E2E_USE_EXTERNAL_WEB=1`, Playwright starts or reuses the API at port 5001 and Vite
at port 3500; starting a local API requires the .NET SDK and its local configuration.
The optional `src/frontend/packages/web/scripts/run-e2e-showcase.sh` wrapper additionally requires an
already running API and Vite at port 3500. It is not the Docker-frontend command above.

## Contribution Model

Issues and pull requests are reviewed on a best-effort basis.

## Preserve files when upgrading

Before replacing an existing API container, follow [API data preservation](deploy/api-data.md).
The `api_data` volume preserves descriptors and Git working directories. For older installations
without that volume, copy existing container files into it before recreation. Database-only backups do not include these files.

## Artifact generation scope

The Generation tab displays artifacts recorded on the server; selecting output types does not run a
code generator. Generation tools and the internal CLI are not included in this public distribution.
Contact your deployment operator for separately managed generation. The legacy regeneration endpoint
returns HTTP 410; its `GENERATION_VIA_CLI` error code is retained for compatibility, not as an instruction
to install a public CLI package.

## Version 0.4.0 support scope

The global Search UI and SLA policy management UI are not supported in 0.4.0. SLA policy APIs remain available; no SLA management screen is included. Semantic search (`POST /api/search`) and its AI/Ollama dependencies are excluded from this public distribution. Project list search and item navigation remain available; item URLs and browser back/forward navigation support multiple panels.

## Evaluation credentials and tests

The supplied Compose profile binds host ports to loopback and uses known public evaluation credentials. Do not use it as production configuration. Replace DB/JWT/admin credentials and omit demo/test data for production.

L0 browser tests and the OnBrick scenario are separate from the unit suite. See [Validation](#validation) for their execution prerequisites and side effects.

The base database retains shared report/embedding storage for project snapshot compatibility, without AI session tables or AI configuration seeds. See [release notes](../RELEASE.md).
