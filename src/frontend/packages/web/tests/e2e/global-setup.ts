/**
 * Playwright Global Setup
 *
 * - Reset Redis rate limits
 * - Create the project used by E2E tests (single test tenant)
 * - Create the storageState file referenced by Playwright config
 *
 * Authentication is handled per test by the autoAuth fixture (fixtures.ts).
 * Because the backend uses refresh token rotation,
 * any token saved during global setup becomes invalid after the first test.
 * This setup therefore creates only the storageState file,
 * while actual authentication is handled by autoAuth through page.request.
 */

import { execSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import type { FullConfig } from '@playwright/test';

const STORAGE_STATE_PATH = 'tests/e2e/.auth/storage-state.json';
const CURRENT_FILE_PATH = fileURLToPath(import.meta.url);
const CURRENT_DIR_PATH = path.dirname(CURRENT_FILE_PATH);
const COMPOSE_FILE_PATH = path.resolve(CURRENT_DIR_PATH, '../../../../../../docker-compose.yml');
const POSTGRES_USER = process.env.POSTGRES_USER || 'sddp';
const POSTGRES_DB = process.env.POSTGRES_DB || 'sddp';

function composeExec(service: string, command: string): string {
  return `docker compose -f "${COMPOSE_FILE_PATH}" exec -T ${service} ${command}`;
}

function ensureE2EProject() {
  const sql = `
    INSERT INTO projects (id, tenant_id, code, name, description, status, owner_id, repo_url, repo_branch, artifact_root_path)
    VALUES (
      '00000000-0000-0000-0020-000000000099',
      '00000000-0000-0000-0010-000000000001',
      'PROJ_E2E_TEST',
      'E2E Test Project',
      'Auto-created for E2E testing',
      'active',
      '00000000-0000-0000-0005-000000000001',
      'https://git.local/e2e/test.git',
      'main',
      'artifacts'
    )
    ON CONFLICT (id) DO UPDATE SET
      tenant_id = EXCLUDED.tenant_id,
      code = EXCLUDED.code,
      name = EXCLUDED.name,
      description = EXCLUDED.description,
      status = EXCLUDED.status,
      owner_id = EXCLUDED.owner_id,
      repo_url = EXCLUDED.repo_url,
      repo_branch = EXCLUDED.repo_branch,
      artifact_root_path = EXCLUDED.artifact_root_path;
  `.trim();

  try {
    execSync(
      composeExec(
        'sddp-db',
        `psql -U ${POSTGRES_USER} -d ${POSTGRES_DB} -c "${sql.replace(/"/g, '\\"').replace(/\n/g, ' ')}"`,
      ),
      { stdio: 'pipe', timeout: 10000 },
    );
    console.log(`[global-setup] E2E project ensured in ${POSTGRES_DB}`);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'unknown error';
    console.log(`[global-setup] E2E project creation skipped: ${message}`);
  }
}

async function globalSetup(_config: FullConfig) {
  try {
    execSync(composeExec('sddp-redis', 'redis-cli FLUSHALL'), {
      stdio: 'pipe',
      timeout: 5000,
    });
    console.log('[global-setup] Redis flushed - rate limits cleared');
  } catch {
    console.log('[global-setup] Redis flush skipped (container not running?)');
  }

  ensureE2EProject();

  const dir = path.dirname(STORAGE_STATE_PATH);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(
    STORAGE_STATE_PATH,
    JSON.stringify({ cookies: [], origins: [] }),
  );
  console.log('[global-setup] Empty storage state created ->', STORAGE_STATE_PATH);
}

export default globalSetup;
