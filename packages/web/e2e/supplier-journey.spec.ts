import { test, expect } from '@playwright/test';

test.describe('Supplier Journey', () => {
  test.skip('supplier logs in, creates batch, submits event, sees compliance', async ({ page }) => {
    // TODO: Requires Auth0 test credentials
    // 1. Navigate to /login
    // 2. Click sign in → Auth0 login
    // 3. Create batch
    // 4. Submit custody event with documents
    // 5. Verify compliance status appears
    await page.goto('/');
    await expect(page).toHaveTitle(/Tungsten/i);
  });
});
