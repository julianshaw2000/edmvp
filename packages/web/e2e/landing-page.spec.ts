import { test, expect } from '@playwright/test';

test.describe('Landing Page', () => {
  test('should display hero section with correct headline', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('3TG supply chain compliance');
  });

  test('should display three feature cards', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Tamper-Evident Tracking' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Automated Compliance' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Material Passports' })).toBeVisible();
  });

  test('should display pricing section with both plans', async ({ page }) => {
    await page.goto('/');
    await page.getByText('Simple, transparent pricing').scrollIntoViewIfNeeded();
    await expect(page.getByText('Simple, transparent pricing')).toBeVisible();
    await expect(page.getByText('Starter').first()).toBeVisible();
    await expect(page.getByText('Pro').first()).toBeVisible();
  });

  test('should navigate to login page', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: 'Login' }).first().click();
    await expect(page).toHaveURL(/\/login/);
  });

  test('should navigate to signup page from CTA', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /Start.*free trial/i }).first().click();
    await expect(page).toHaveURL(/\/signup/);
  });

  test('should have navigation bar with logo', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('auditraks').first()).toBeVisible();
  });

  test('should display footer with copyright', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('© 2026 auditraks')).toBeVisible();
  });
});
