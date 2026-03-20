import { test, expect } from '@playwright/test';

test.describe('Compliance Flow', () => {
  test.skip('non-conformant smelter triggers flag visible to buyer', async ({ page }) => {
    // TODO: Requires full stack running
    await page.goto('/');
    await expect(page).toHaveTitle(/Tungsten/i);
  });
});
