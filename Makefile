# =============================================================================
# SDDP Makefile - Public Development Commands
# =============================================================================

.PHONY: help infra api web start stop status build test lint db-provision db-seed-test db-reset db-reset-full demo-onbrick security-scan security-scan-staged sbom

POSTGRES_USER ?= sddp
POSTGRES_DB   ?= sddp
SDDP_STORAGE_TABLESPACE_NAME ?= sddp_data_ts
SDDP_STORAGE_TABLESPACE_PATH ?= /var/lib/postgresql/tablespaces/sddp_data
SDDP_STORAGE_QUOTA_GB ?= 20

help:
	@echo "SDDP Public Development Commands"
	@echo ""
	@echo "Development:"
	@echo "  make infra           Start infrastructure (db, redis)"
	@echo "  make api             Start API with hot reload"
	@echo "  make web             Start frontend dev server"
	@echo "  make start           Start full dev environment"
	@echo "  make stop            Stop all services"
	@echo "  make status          Show service status"
	@echo ""
	@echo "Build & Test:"
	@echo "  make build           Build backend and frontend"
	@echo "  make test            Run public frontend tests"
	@echo "  make lint            Run linters"
	@echo ""
	@echo "Database:"
	@echo "  make db-provision    Run DB provisioning scripts (schema + base data)"
	@echo "  make db-seed-test    Insert shared test data"
	@echo "  make db-reset        Reset DB only (drop/recreate + provision)"
	@echo "  make db-reset-full   Full reset (docker compose down -v)"
	@echo ""
	@echo "Scenario:"
	@echo "  make demo-onbrick    Init OnBrick brownfield story"
	@echo ""
	@echo "Security:"
	@echo "  make security-scan   Run gitleaks scan"
	@echo "  make sbom            Generate backend/frontend SBOM"

infra:
	docker compose up -d sddp-db sddp-redis
	@echo "Infrastructure started (db: 5432, redis: 6379)"
	@echo "  Aspire Dashboard is not started by 'make infra'"
	@echo "  Start it with: docker compose up -d sddp-aspire"

api:
	cd src/backend && dotnet watch run --project Sddp.Api --urls "http://localhost:5001"

web:
	cd src/frontend/packages/web && npm run dev

start: infra
	@echo "Starting API and Frontend..."
	@echo "Run 'make api' and 'make web' in separate terminals"

stop:
	docker compose down
	-pkill -f "dotnet.*Sddp.Api" 2>/dev/null

status:
	@echo "=== Docker Containers ==="
	@docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || docker compose ps
	@echo ""
	@echo "=== URLs ==="
	@echo "  Frontend: http://localhost:9010"
	@echo "  API:      http://localhost:5001"
	@echo "  Scalar:   http://localhost:5001/scalar/v1"
	@echo "  Aspire:   http://localhost:18888"

build:
	dotnet build src/backend/Sddp.Api/Sddp.Api.csproj --configuration Release
	cd src/frontend/packages/web && npm run build

test:
	cd src/frontend/packages/web && npm test

lint:
	cd src/frontend/packages/web && npm run lint && npm run type-check

db-provision:
	@echo "Running DB provisioning (schema + base data)..."
	@echo "Storage quota config: tablespace=$(SDDP_STORAGE_TABLESPACE_NAME), path=$(SDDP_STORAGE_TABLESPACE_PATH), quota=$(SDDP_STORAGE_QUOTA_GB)GB"
	@docker compose exec -T --user root sddp-db bash -lc "mkdir -p '$(SDDP_STORAGE_TABLESPACE_PATH)' && chown -R postgres:postgres '$(SDDP_STORAGE_TABLESPACE_PATH)'"
	@docker compose exec -T sddp-db psql \
		-v storage_tablespace_name=$(SDDP_STORAGE_TABLESPACE_NAME) \
		-v storage_tablespace_path=$(SDDP_STORAGE_TABLESPACE_PATH) \
		-v storage_quota_gb=$(SDDP_STORAGE_QUOTA_GB) \
		-U $(POSTGRES_USER) -d $(POSTGRES_DB) -f /scripts/db/provision-base.sql
	@echo ""
	@echo "DB provisioning completed! (run 'make db-seed-test' for shared fixtures)"

db-seed-test:
	@echo "Inserting shared test data..."
	@docker compose exec -T sddp-db psql -U $(POSTGRES_USER) -d $(POSTGRES_DB) -f /scripts/db/provision-test.sql
	@echo ""
	@echo "Shared test data inserted!"

db-reset:
	@echo "This will DROP and RECREATE only the database '$(POSTGRES_DB)'. Press Ctrl+C to cancel, Enter to continue..."
	@read _
	docker compose up -d sddp-db
	@echo "Waiting for DB to be ready (pg_isready)..."
	@until docker compose exec -T sddp-db pg_isready -U $(POSTGRES_USER) -d postgres >/dev/null 2>&1; do sleep 1; done
	@echo "Terminating active connections to $(POSTGRES_DB)..."
	@docker compose exec -T sddp-db psql -U $(POSTGRES_USER) -d postgres -v ON_ERROR_STOP=1 -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$(POSTGRES_DB)' AND pid <> pg_backend_pid();"
	@echo "Dropping and recreating $(POSTGRES_DB)..."
	@docker compose exec -T sddp-db psql -U $(POSTGRES_USER) -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS \"$(POSTGRES_DB)\";"
	@docker compose exec -T sddp-db psql -U $(POSTGRES_USER) -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"$(POSTGRES_DB)\" OWNER \"$(POSTGRES_USER)\";"
	$(MAKE) db-provision
	@echo "DB reset complete! (run 'make db-seed-test' for shared fixtures)"

db-reset-full:
	@echo "This will remove all Docker volumes for this project (DB/Redis/Ollama/Gitea). Press Ctrl+C to cancel, Enter to continue..."
	@read _
	docker compose down -v
	docker compose up -d sddp-db sddp-redis
	@echo "Waiting for DB to be ready (pg_isready)..."
	@until docker compose exec -T sddp-db pg_isready -U $(POSTGRES_USER) -d postgres >/dev/null 2>&1; do sleep 1; done
	$(MAKE) db-provision
	@echo "Full reset complete! (run 'make db-seed-test' if needed)"

demo-onbrick:
	@echo "=== OnBrick Scenario Setup ==="
	docker compose down -v
	docker compose up -d --build
	@echo "Waiting for DB to be ready..."
	@sleep 5
	$(MAKE) db-provision
	$(MAKE) db-seed-test
	@echo "[demo] OnBrick seed (2-week migration)..."
	@docker compose exec -T sddp-db psql -U $(POSTGRES_USER) -d $(POSTGRES_DB) -f /scripts/demo/onbrick-seed.sql
	@echo ""
	$(MAKE) status
	@echo ""
	@echo "✓ Ready! Login: minjun.park / Test@123!"

security-scan:
	@echo "Running gitleaks scan..."
	gitleaks detect --source . --config .gitleaks.toml --verbose

security-scan-staged:
	@echo "Running gitleaks on staged files..."
	gitleaks protect --staged --config .gitleaks.toml --verbose

sbom:
	@echo "Generating SBOM (CycloneDX)..."
	@mkdir -p dist/sbom
	cd src/backend && dotnet CycloneDX Sddp.Api/Sddp.Api.csproj -o ../../dist/sbom -fn sddp-backend-sbom.json -j
	cd src/frontend && npx @cyclonedx/cyclonedx-npm --output-file ../../dist/sbom/sddp-frontend-sbom.json
	@echo "SBOM files generated in dist/sbom/"
