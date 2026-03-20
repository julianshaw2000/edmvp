import { test, expect } from '@playwright/test';

test.describe('Buyer Journey', () => {
  test.skip('buyer views batches, generates passport, downloads PDF', async ({ page }) => {
    // TODO: Requires Auth0 test credentials and seeded data
    await page.goto('/');
    await expect(page).toHaveTitle(/Tungsten/i);
  });
});
