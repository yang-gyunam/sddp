/**
 * Execute an API request with automatic rate-limit (429) retry and exponential backoff.
 */
export async function withRetry(
  fn: () => Promise<import('@playwright/test').APIResponse>,
  options?: { maxAttempts?: number; baseDelayMs?: number }
): Promise<import('@playwright/test').APIResponse> {
  const { maxAttempts = 3, baseDelayMs = 500 } = options ?? {};
  let response = await fn();
  for (let attempt = 1; response.status() === 429 && attempt <= maxAttempts; attempt++) {
    const delay = baseDelayMs * Math.pow(2, attempt - 1);
    await new Promise((r) => setTimeout(r, delay));
    response = await fn();
  }
  return response;
}
