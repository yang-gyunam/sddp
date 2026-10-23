# Public Guide

This guide covers the supported public-repository workflow for running SDDP, provisioning the database, and executing the public validation paths.

## Scope

This is the free edition. See [release notes](../RELEASE.md) for version 0.5.1 support and limitations.

The public repository includes:

- the backend and frontend application code
- the public test suite
- database provisioning scripts
- the OnBrick showcase scenario
- the read-only Git File Viewer with current SDDP relationship links

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
make db-provision      # empty application database only
make db-seed-test
docker compose up -d --build sddp-api sddp-front
```

Host-side `npm ci` is not required when you use the frontend container only.
The commands above start PostgreSQL, Redis, the API and the web frontend.

Optional — speech-to-text for conversation messages (local Whisper, no cloud calls):

```bash
docker compose up -d sddp-whisper
make whisper-warmup    # one-time model download (~460MB)
```

### Optional telemetry dashboard

Aspire Dashboard is an optional viewer for OpenTelemetry logs and traces. SDDP runs without it;
the API's required Compose dependencies are PostgreSQL and Redis. The targeted Quick Start
commands above and `make infra` do not start Aspire.

To use the dashboard:

```bash
docker compose up -d sddp-aspire
```

Open `http://localhost:18888` after the container starts. Without the dashboard, use
`docker compose logs -f sddp-api` to read API console logs.

Aspire is defined in the default Compose configuration, so running `docker compose up -d`
without service names also starts it.

### Local frontend development (optional)

After completing the Docker setup, run the local frontend from the repository root:

```bash
cd src/frontend
npm ci
cd packages/web
VITE_API_URL=http://localhost:5001 npm run dev
```

Vite has no API proxy. This command sets the API origin for this local process only;
substitute your API host and port if different. Keep `VITE_API_URL` empty for the
Docker frontend, where nginx proxies `/api` and `/hubs` requests.

## URLs

- Frontend: `http://localhost:9010`
- Frontend dev server (optional): `http://localhost:3500`
- API: `http://localhost:5001`
- API docs: `http://localhost:5001/scalar/v1`
- Aspire Dashboard (optional, when started): `http://localhost:18888`
- Whisper STT (optional): `http://localhost:9020`

## Database Commands

```bash
make db-provision      # schema + base data (clean DB only)
make db-seed-test      # shared test data
make db-reset          # drop + recreate + provision
make db-reset-full     # deletes ALL project volumes, then provisions the DB
make demo-onbrick      # deletes ALL project volumes, rebuilds services and seeds OnBrick
```

- `make db-provision` runs on a **clean database only**. Existing 0.4.0/0.4.1 installations must follow the [manual repository upgrade procedure](deploy/repository-050.md); older or customized schemas need a separate migration review. `make db-reset` destroys the database and is only for deliberate evaluation resets.
- `make db-seed-test` loads the tracked public shared test dataset.
- `make demo-onbrick` runs `docker compose down -v` before rebuilding and provisioning. It removes DB, Redis, API-file and Gitea volumes for this Compose project without an interactive confirmation. Use it only in a disposable evaluation environment.
- The OnBrick seed below is **destructive**, even though it does not reset volumes.
  It deletes existing Artifacts, Tasks, Specs, Requirements, Conversations and related
  records across the demo tenant, including other projects in that tenant, then inserts
  the scenario. Use it only to initialize a newly provisioned, disposable evaluation DB
  after `make db-seed-test`. Do not rerun it to repair an installation with work to preserve.
  Run from the repository root:

```bash
docker compose exec -T sddp-db psql -v ON_ERROR_STOP=1 -U sddp -d sddp -f /scripts/demo/onbrick-seed.sql
```

The command uses the default DB user/database; substitute your configured values if changed.
Evaluation accounts, including the OnBrick users, are in
[provision-test.sql](../scripts/db/provision-test.sql); the seed supplies scenario content.
For the known audit-field issue in an existing OnBrick dataset, use the targeted
[audit repair procedure](#validation) instead of reseeding.

## Connect a project repository

Open **Settings → Project → General → Git Repository** with a role that has project update
permission. The older Integrations entry points to General; no feature flag is needed for Git settings.

1. Enter a public HTTPS clone URL without credentials, branch and optional Artifact root directory. Set the sync
   interval and choose **Save repository**. This saves a draft; Files browsing does not freeze it.
2. Choose **Check connection**. The server checks the remote branch, the root directory and any existing
   file relationship paths. Correct failures before continuing. Checks expire after 15 minutes and are
   invalidated when the URL, branch or root changes.
3. Use **Browse files** to inspect the repository if needed. Confirm the saved configuration using
   **Confirm repository → Confirm and lock settings** before saving the first Artifact or file–Spec mapping.
4. After confirmation, the URL, branch and root are locked. The sync interval remains editable and new
   commits continue to sync. Changing the selected connection rechecks access and existing file paths
   before saving. Deleting relationships or resetting the project lifecycle does not unlock it.

An empty Artifact root means tracked Artifact paths are relative to the repository root. Files always
browses the whole repository. Projects with existing file relationships preserve their existing connection
coordinates and must verify those paths before confirmation. A missing URL can be entered for the first
connection. Moving an existing repository, changing its branch or reinterpreting its Artifact root requires
a separate migration review; this release does not include an unlock/migration screen.

Upgrading an existing installation requires **V016 and V017** before starting the updated API. Existing projects
are not silently marked confirmed: their owners must check and confirm them before new API or CLI file
relationships can be saved. Existing data remains readable. See the
[0.5.0 repository upgrade notes](deploy/repository-050.md).

### Public Git connections, storage and synchronization

| Setting | Current behavior |
| --- | --- |
| `projects.repo_url` / `repo_branch` | Repository and branch for this project; use a URL without embedded credentials. |
| `projects.sync_interval_minutes` | Default 60. A file request fetches when this interval has elapsed; there is no background polling in this path. Values of 0 or less disable periodic synchronization after the initial clone. |
| `projects.last_synced_at` | Written after successful synchronization. The page's Refresh repository action follows the interval and does not force a remote fetch. |
| `SDDP_GIT__REPOROOT` | Git working directories; default `data/git-repos`, inside the `api_data` volume. |
| `SDDP_GIT__SERVERURL` | Compose input for the API's `Git__ServerUrl`; defaults to `http://sddp-gitea:3000`. Explicitly allowed internal Gitea origin. HTTP is accepted only for this configured Gitea origin; other Git server connections require HTTPS. |

Free supports public repositories only. Private repositories and authentication token registration
are rejected by the server. Public github.com repositories can be connected directly. Administrators
can register a public GitHub/Gitea connection in Settings → System Config → Git Connections,
restricted to one project. Select it in Project → General → Git Repository, then save, check and confirm.
For a configured internal Gitea server, use its matching clone URL; the repository must still be public.
For the bundled service:

1. Start it with `docker compose --profile tools up -d sddp-gitea`.
2. Open `http://localhost:3100` to complete Gitea setup and create a **public** repository
   with at least one commit. Host port 3100 maps to container port 3000.
3. In SDDP's Git Connections settings, use provider **Gitea** and server URL
   `http://sddp-gitea:3000`. Use `http://sddp-gitea:3000/owner/repository.git` as the
   project's repository URL and select a branch that exists.
   These addresses are resolved by the API container, not your browser.
4. Save the project repository, check the connection, browse files, then confirm it.

If you use a different Gitea origin, set `SDDP_GIT__SERVERURL` in the root `.env`
and recreate the API with `docker compose up -d sddp-api`. The origin must be reachable
from that container. For an API started outside Compose, set `Git__ServerUrl` directly.
Earlier exports hardcoded `http://sddp-gitea:3100`; the bundled service's correct internal
port is 3000. An installation customized to listen on 3100 can retain it with
`SDDP_GIT__SERVERURL=http://sddp-gitea:3100`. Existing project URLs are not rewritten:
review any already confirmed repository before changing its server address.

The connection check verifies repository visibility against provider metadata. Later GitHub Files reads
verify anonymous Git access without repeating the metadata request; access failures are never treated
as public access. Gitea reads continue to use the provider visibility check.

No repository token is needed or accepted in this edition. Preserve the API data volume and database
together during upgrades. Project snapshot restore retains the project's current Git connection and
confirmation state, rather than restoring old repository settings.

The Files view pins its tree/search/history to the locally synchronized commit. Use the page refresh to
reload that snapshot when synchronization is due; an older commit remains selected until **Back to latest**.
Files does not force-push, switch branches or resolve diverged branches. Draft repository settings are managed in General.

### Use Files

- Search matches file names and repository-relative paths across all committed folders, including unopened ones.
  It does not search file contents or uncommitted working files. Selecting a result keeps the query and results;
  clear the search to return to the tree with the selected file revealed.
- Select a history entry to read that whole file version. History follows renames. Before/after comparison and
  two-commit comparison are not included.
- File Info shows size, the last change's commit time and the linked Artifact's SDDP owner. This is not a
  filesystem modification time or Git author; Git author is shown separately in the Git section.
- Artifacts, Tasks, Specs, Requirements and Conversations show current, authorized, explicitly linked records,
  even for a historical file. An empty relationship section is valid; matching names alone do not create links.
- The page refresh preserves file/revision/search/expanded folders. Browser reload restores file/revision from
  the URL, but search, expanded-folder choices and custom panel widths are not browser-persistent. Required
  ancestors reopen for the selected file. Split tabs copy search/file/revision state and then operate independently.
- File content is read-only. Binary/non-UTF-8 files and files above the preview limit (5 MiB by default) show
  metadata and an explanation. Symbolic links show their target text; submodules are not traversed.

### Troubleshooting

| Symptom | Check |
| --- | --- |
| Access Denied | Signed-in account, tenant and active project membership/role. Git credentials do not grant SDDP access. |
| Repository unavailable | Public repository visibility, project URL/branch, selected project connection and API-server network access. An authenticated browser opening the repository does not prove anonymous access. |
| New remote commit is not visible | Synchronization interval and last successful sync; page refresh is not a force-fetch operation. |
| No linked records / Unassigned | Exact Artifact path and root mapping, explicit links, and the current user's access to related records. |
| Preview unavailable | Binary encoding or configured file-size limit; metadata/history may still be available. |

## Verify the Files view

On the candidate installation, use a repository connected and confirmed through General and an ordinary project member:

1. Search for a file in an unopened folder, open it, clear search and verify the selected row is visible.
2. Open a historical version and return to latest. Reload a historical file URL and check the commit and body.
3. Follow existing links to all five SDDP record types, checking the destination and access restrictions.
4. Switch tabs, split Files, change one side and verify the other remains unchanged. Resize the window and
   verify chosen side-panel widths; verify the active file's status bar and both page/browser refresh behavior.
5. Check missing-repository, denied-access and preview-limited states, and inspect browser/API error reports.

This is a candidate verification checklist, not a claim that every installation has passed it.
The public unit suite and OnBrick smoke below do not exercise the full repository/Files workflow.

## Sidebar and confirmation controls

Sidebars with at least two supported collapsible panels show **⋮ → Single Expand / Collapse All**
beside their header controls. Single Expand is off by default. When enabled, opening a panel closes
the other panels; the preference survives browser reloads. Collapse All closes the panels without
changing that preference. The options do not appear in sidebars with only one panel or in the custom
Conversations sidebar.

Confirmation actions use the application's shared Modal. Cancelling an action, including Knowledge
archive, leaves the selected data unchanged. The sidebar's Refresh action retains its current
expansion state while the project panels reload.

## Knowledge document editing

Knowledge uses the shared rich-text editor in reading and editing views, with
consistent typography in history and conflict comparisons. Body text uses 16px type,
24px line height and 8px paragraph spacing.

Discarding edits restores the saved body immediately. Saving and reopening preserves
ordered-list start numbers and numbering types, nested lists, table-cell alignment,
column widths and merged cells. Supported numbering types are decimal, lower/upper
alphabetic and lower/upper Roman; table alignment supports left, center and right.
This describes preservation of supported document formatting, not additional toolbar controls.
Unsupported attributes and values are rejected rather than silently accepted.

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
`make test` runs the selected UI-control and public error-handler suites once.
Backend and AI tests are not shipped; this limited suite is not full product acceptance.

The release gate also runs both OnBrick smoke tests on a disposable PostgreSQL/Redis/API
stack and the built frontend. It checks Spec creation/readback/cleanup, seeded Channel access
and messages, project detail statistics, and repair of the legacy missing artifact/glossary audit owners.
Chromium is required; missing browsers or skipped/flaky tests fail the gate.

To repeat only these two tests against your prepared evaluation stack (load the OnBrick seed
first), run from the repository root, adjusting both URLs to your stack:

```bash
cd src/frontend
npx playwright install chromium
E2E_WEB_BASE_URL=http://localhost:9010 E2E_API_BASE=http://localhost:9010/api \
  npm run test:e2e:smoke -w @sddp/web
```

This headless configuration has no global setup, Redis flush, or shared saved login. It creates
one uniquely named Spec and deactivates that same Spec after readback; seeded records remain.
It does not seed or repair the database automatically. Results use `test-results/onbrick-smoke`
and `test-results/onbrick-smoke.json`; set `ONBRICK_OUTPUT_DIR` and `ONBRICK_REPORT` to preserve
separate runs.

Existing OnBrick installations seeded before this fix may return HTTP500 on project detail
because the sample artifact and two glossary rows have NULL `updated_by`, and the glossary
list fields are NULL. Back up the database, then run the scoped,
idempotent repair from the repository root:

```bash
docker compose exec -T sddp-db psql -v ON_ERROR_STOP=1 -U sddp -d sddp \
  < scripts/demo/onbrick-repair-audit.sql
```

It fills only the missing update owners of three known OnBrick sample rows from their creators,
and initializes missing `usage_examples` / `related_term_ids` in the two glossary rows to empty arrays.
It preserves valid update owners, timestamps and all other rows. Do **not** replay
`onbrick-seed.sql` to repair existing data: that seed deletes the tenant's demo content.
The fix does not repair arbitrary corrupt audit data from other sources.

The older headed `npm run test:e2e` showcase remains available. Its global setup clears all
keys in the Compose Redis instance and creates/updates an E2E project. Use that command only
with disposable services; it is not used by the release gate.

## Contribution Model

Issues and pull requests are reviewed on a best-effort basis.

## Preserve files when upgrading

Before replacing an existing API container, follow [API data preservation](deploy/api-data.md).
The `api_data` volume preserves descriptors and Git working directories. For older installations
without that volume, copy existing container files into it before recreation. Database-only backups do not include these files.

For 0.4.0/0.4.1, apply the [manual V016/V017 upgrade](deploy/repository-050.md) to a restored copy first,
then follow the same reviewed procedure for the installation. Update the API and frontend together
and reload open browser tabs. Check that the app status bar and API report the same version. If a
tab kept open during deployment cannot load a screen, reload it to replace references to old assets.

## Artifact generation scope

The Generation tab displays artifacts recorded on the server; selecting output types does not run a
code generator. Generation tools and the internal CLI are not included in this public distribution.
Contact your deployment operator for separately managed generation. The legacy regeneration endpoint
returns HTTP 410; its `GENERATION_VIA_CLI` error code is retained for compatibility, not as an instruction
to install a public CLI package.

## Version 0.5.1 support scope

The global Search UI and SLA policy management UI are not supported in 0.5.1. SLA policy APIs remain available; no SLA management screen is included. Semantic search (`POST /api/search`) and its execution dependencies are excluded from this public distribution. Project list search, Files repository-path search and item navigation remain available; item URLs and browser back/forward navigation support multiple panels. Configure repositories in Settings → Project → General; the older Integrations entry redirects there.

## Evaluation credentials and tests

The supplied Compose profile binds host ports to loopback and uses known public evaluation credentials. Do not use it as production configuration. Replace DB/JWT/admin credentials and omit demo/test data for production.

L0 browser tests and the OnBrick scenario are separate from the unit suite. See [Validation](#validation) for their execution prerequisites and side effects.

The base database retains shared report/embedding storage for project snapshot compatibility, without
AI session tables or AI configuration seeds. Common capability types and disabled frontend adapters
remain in the source for compatibility; AI execution modules and their dedicated dependencies are
not included. See [release notes](../RELEASE.md).
