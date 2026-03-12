import { test, expect } from '@playwright/test';
import { E2E_HEALTH_URL } from '../helpers/constants';

test.describe('Health Check', () => {
  test('should return 200 OK', async ({ request }) => {
    const response = await request.get(E2E_HEALTH_URL, {
    });

    expect(response.status()).toBe(200);
  });
});
