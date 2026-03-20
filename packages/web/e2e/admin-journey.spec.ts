import { test, expect } from '@playwright/test';

test.describe('Admin Journey', () => {
  test.skip('admin manages users, uploads RMAP list, reviews flags', async ({ page }) => {
    // TODO: Requires Auth0 test credentials
    await page.goto('/');
    await expect(page).toHaveTitle(/Tungsten/i);
  });
});
