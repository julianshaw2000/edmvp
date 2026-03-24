import { test, expect } from '@playwright/test';

test.describe('Signup Page', () => {
  test('should display signup form with required fields', async ({ page }) => {
    await page.goto('/signup');
    await expect(page.locator('input').first()).toBeVisible();
    await expect(page.getByRole('button').filter({ hasText: /trial/i })).toBeVisible();
  });

  test('should show Pro plan by default', async ({ page }) => {
    await page.goto('/signup');
    await expect(page.getByText('Pro')).toBeVisible();
  });

  test('should show Starter plan when selected via query param', async ({ page }) => {
    await page.goto('/signup?plan=starter');
    await expect(page.getByText('Starter')).toBeVisible();
  });

  test('should have link back to login', async ({ page }) => {
    await page.goto('/signup');
    await expect(page.getByText(/already have an account/i)).toBeVisible();
  });

  test('should disable submit until form is valid', async ({ page }) => {
    await page.goto('/signup');
    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await expect(submitButton).toBeDisabled();
  });
});

test.describe('Signup Success Page', () => {
  test('should display success message and sign in link', async ({ page }) => {
    await page.goto('/signup/success');
    // Page should load without redirecting to login
    await expect(page).toHaveURL(/\/signup\/success/);
    await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible();
  });
});
