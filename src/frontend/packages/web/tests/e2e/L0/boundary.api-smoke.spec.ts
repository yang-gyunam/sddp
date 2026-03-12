import { test, expect } from '@playwright/test';
import { E2E_API_BASE, TEST_PROJECT_ID } from '../helpers/constants';

const API = E2E_API_BASE;
const APP = API.replace(/\/api\/?$/, '');
const ZERO_ID = '00000000-0000-0000-0000-000000000001';

test.describe('L0: API Authorization Boundaries Across Domains', () => {
  test('Specs read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/specs`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Requirements read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/requirements`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Conversations read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/conversations`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Projects read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/projects`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Effort read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/projects/${TEST_PROJECT_ID}/effort/summary`, {
      failOnStatusCode: false,
    });
    expect(response.status()).toBe(401);
  });

  test('Glossary read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/glossary`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Task read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/tasks`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Settings read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/system/config`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Dashboard read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/dashboard/my`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Search read boundary denies anonymous', async ({ request }) => {
    const response = await request.post(`${API}/search`, {
      data: { text: 'boundary', topK: 5 },
      failOnStatusCode: false,
    });
    expect(response.status()).toBe(401);
  });

  test('Relationship read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/relationships/graph/spec/${ZERO_ID}`, {
      failOnStatusCode: false,
    });
    expect(response.status()).toBe(401);
  });

  test('Artifact read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/artifacts`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('AI read boundary denies anonymous', async ({ request }) => {
    const response = await request.get(`${API}/ai/status`, { failOnStatusCode: false });
    expect(response.status()).toBe(401);
  });

  test('Specs create boundary denies anonymous', async ({ request }) => {
    const response = await request.post(`${API}/specs`, {
      data: {
        code: 'SPEC_L0_BOUNDARY',
        title: 'L0 boundary',
        description: 'Unauthorized create should be denied',
        decision: 'N/A',
      },
      failOnStatusCode: false,
    });
    expect(response.status()).toBe(401);
  });

  test('Artifact delete boundary denies anonymous', async ({ request }) => {
    const response = await request.delete(`${API}/artifacts/${ZERO_ID}`, {
      failOnStatusCode: false,
    });
    expect(response.status()).toBe(401);
  });

  test('Conversations realtime boundary denies anonymous', async ({ request }) => {
    const response = await request.post(
      `${APP}/hubs/conversation/negotiate?negotiateVersion=1`,
      { failOnStatusCode: false }
    );
    expect(response.status()).toBe(401);
  });
});
