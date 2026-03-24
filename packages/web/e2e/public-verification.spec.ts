import { test, expect } from '@playwright/test';

test.describe('Public Verification Page', () => {
  test('should load verification page for valid batch ID format', async ({ page }) => {
    // Use a random GUID — page should load even if batch doesn't exist
    await page.goto('/verify/00000000-0000-0000-0000-000000000000');
    // Should show the verification page structure (not a 404)
    await expect(page.locator('body')).not.toContainText('Page not found');
  });

  test('should not require authentication', async ({ page }) => {
    await page.goto('/verify/test-batch-id');
    // Should NOT redirect to login
    await expect(page).not.toHaveURL(/\/login/);
  });
});

test.describe('Navigation', () => {
  test('should show 404 page for unknown routes', async ({ page }) => {
    await page.goto('/nonexistent-page-xyz');
    await expect(page.locator('body')).toContainText(/not found|404/i);
  });
});
