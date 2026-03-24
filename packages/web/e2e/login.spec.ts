import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test('should display login page', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText(/sign in/i).first()).toBeVisible();
  });

  test('should have Google sign in button', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('should have link to signup', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText(/free trial/i)).toBeVisible();
  });

  test('should redirect unauthenticated users to login from protected routes', async ({ page }) => {
    await page.goto('/admin');
    // Should redirect to login since not authenticated
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated users from supplier routes', async ({ page }) => {
    await page.goto('/supplier');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated users from buyer routes', async ({ page }) => {
    await page.goto('/buyer');
    await expect(page).toHaveURL(/\/login/);
  });
});
