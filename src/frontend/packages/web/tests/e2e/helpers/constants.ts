/**
 * E2E Test Constants
 * test user, tenant, API URL
 */

// Test user credentials (matches backend seed data)
export const TEST_USER = {
  username: 'admin',
  password: 'Admin@123!',
};

export const TEST_USERS = {
  admin: { username: 'admin', password: 'Admin@123!', userId: '00000000-0000-0000-0005-000000000001' },
  john: { username: 'john.kim', password: 'Test@123!', userId: '00000000-0000-0000-0005-000000000101' },
  sarah: { username: 'sarah.lee', password: 'Test@123!', userId: '00000000-0000-0000-0005-000000000102' },
  mike: { username: 'mike.park', password: 'Test@123!', userId: '00000000-0000-0000-0005-000000000103' },
  jane: { username: 'jane.choi', password: 'Test@123!', userId: '00000000-0000-0000-0005-000000000104' },
  david: { username: 'david.seo', password: 'Test@123!', userId: '00000000-0000-0000-0005-000000000105' },
} as const;

// DevFlow Story users (matches 91-data-test-persons.sql)
export const DEVFLOW_USERS = {
  alex: { username: 'alex.kim', password: 'Test@123!', role: 'PRODUCT_OWNER', userId: '00000000-0000-0000-0005-000000000201' },
  jordan: { username: 'jordan.lee', password: 'Test@123!', role: 'DOMAIN_EXPERT', userId: '00000000-0000-0000-0005-000000000202' },
  taylor: { username: 'taylor.park', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000203' },
  morgan: { username: 'morgan.choi', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000204' },
  casey: { username: 'casey.seo', password: 'Test@123!', role: 'REVIEWER', userId: '00000000-0000-0000-0005-000000000205' },
  riley: { username: 'riley.han', password: 'Test@123!', role: 'QA_TESTER', userId: '00000000-0000-0000-0005-000000000206' },
} as const;

// API base URL
export const E2E_API_BASE = process.env.E2E_API_BASE ?? 'http://localhost:5001/api';
export const E2E_HEALTH_URL = process.env.E2E_HEALTH_URL ?? 'http://localhost:5001/health';
export const API_BASE = E2E_API_BASE;

// Single test tenant and project IDs
export const TEST_TENANT_ID = '00000000-0000-0000-0010-000000000001';
export const TEST_PROJECT_ID = '00000000-0000-0000-0020-000000000099'; // E2E test project (global-setup.ts)

// ACME tenant/project (test users john, sarah, mike, jane, david belong here)
export const ACME_TENANT_ID = '00000000-0000-0000-0010-000000000001';
export const ACME_PROJECT_ID = '00000000-0000-0000-0020-000000000001';

// DevFlow tenant/project
export const DEVFLOW_TENANT_ID = '00000000-0000-0000-0010-000000000001';
export const DEVFLOW_PROJECT_ID = '00000000-0000-0000-0020-000000000011';

// OnBrick tenant/project
export const ONBRICK_TENANT_ID = '00000000-0000-0000-0010-000000000001';
export const ONBRICK_PROJECT_ID = '00000000-0000-0000-0020-000000000021';

// OnBrick Story users (matches 91-data-test-persons.sql)
export const ONBRICK_USERS = {
  minjun: { username: 'minjun.park', password: 'Test@123!', role: 'PRODUCT_OWNER', userId: '00000000-0000-0000-0005-000000000301' },
  soojin: { username: 'soojin.yoon', password: 'Test@123!', role: 'REVIEWER', userId: '00000000-0000-0000-0005-000000000302' },
  hyunwoo: { username: 'hyunwoo.kim', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000303' },
  jieun: { username: 'jieun.lee', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000304' },
} as const;
