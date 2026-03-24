import { test, expect } from '@playwright/test';

test.describe('Full Signup Flow', () => {
  const testEmail = 'julian@carib212.com';

  test('should navigate from landing page to signup', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /Start.*free trial/i }).first().click();
    await expect(page).toHaveURL(/\/signup/);
  });

  test('should fill signup form and submit for Pro plan', async ({ page }) => {
    await page.goto('/signup?plan=pro');

    // Fill form
    await page.locator('input').nth(0).fill('Test Company Ltd');
    await page.locator('input').nth(1).fill('Test User');
    await page.locator('input[type="email"]').first().fill(testEmail);
    await page.locator('input[type="email"]').last().fill(testEmail);

    // Submit button should be enabled
    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await expect(submitButton).toBeEnabled();

    // Intercept the API call to avoid actually hitting Stripe
    await page.route('**/api/signup/checkout', async (route) => {
      const request = route.request();
      const body = JSON.parse(request.postData() ?? '{}');

      // Verify the request payload
      expect(body.companyName).toBe('Test Company Ltd');
      expect(body.name).toBe('Test User');
      expect(body.email).toBe(testEmail);
      expect(body.plan).toBe('PRO');

      // Return a mock Stripe checkout URL
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ checkoutUrl: 'https://checkout.stripe.com/test-session' }),
      });
    });

    // Click submit
    await submitButton.click();

    // Should attempt to redirect to Stripe (we intercepted it)
    // Wait a moment for the redirect attempt
    await page.waitForTimeout(1000);
  });

  test('should fill signup form and submit for Starter plan', async ({ page }) => {
    const starterEmail = `starter-${Date.now()}@example.com`;
    await page.goto('/signup?plan=starter');

    // Verify Starter plan is shown
    await expect(page.getByText('Starter')).toBeVisible();

    // Fill form
    await page.locator('input').nth(0).fill('Small Mining Co');
    await page.locator('input').nth(1).fill('Jane Smith');
    await page.locator('input[type="email"]').first().fill(starterEmail);
    await page.locator('input[type="email"]').last().fill(starterEmail);

    // Intercept API call
    await page.route('**/api/signup/checkout', async (route) => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      expect(body.plan).toBe('STARTER');

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ checkoutUrl: 'https://checkout.stripe.com/test-starter' }),
      });
    });

    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await submitButton.click();
    await page.waitForTimeout(1000);
  });

  test('should not submit with empty company name', async ({ page }) => {
    await page.goto('/signup');

    // Only fill email fields, leave company name empty
    await page.locator('input').nth(1).fill('Test User');
    await page.locator('input[type="email"]').first().fill('test@test.com');
    await page.locator('input[type="email"]').last().fill('test@test.com');

    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await expect(submitButton).toBeDisabled();
  });

  test('should prevent submission with mismatched emails', async ({ page }) => {
    await page.goto('/signup');

    await page.locator('input').nth(0).fill('Mismatch Co');
    await page.locator('input').nth(1).fill('User');
    await page.locator('input[type="email"]').first().fill('a@test.com');
    await page.locator('input[type="email"]').last().fill('b@test.com');

    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await expect(submitButton).toBeDisabled();
  });

  test('full flow: landing → signup → form fill → submit', async ({ page }) => {
    const flowEmail = `flow-${Date.now()}@example.com`;

    // Start on landing page
    await page.goto('/');

    // Click Starter plan CTA
    await page.getByRole('link', { name: /Start Free Trial/i }).first().click();
    await expect(page).toHaveURL(/\/signup/);

    // Fill form
    await page.locator('input').nth(0).fill('Flow Test Corp');
    await page.locator('input').nth(1).fill('Flow Tester');
    await page.locator('input[type="email"]').first().fill(flowEmail);
    await page.locator('input[type="email"]').last().fill(flowEmail);

    // Intercept and verify
    await page.route('**/api/signup/checkout', async (route) => {
      const body = JSON.parse(route.request().postData() ?? '{}');
      expect(body.companyName).toBe('Flow Test Corp');
      expect(body.email).toBe(flowEmail);

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ checkoutUrl: 'https://checkout.stripe.com/test-flow' }),
      });
    });

    const submitButton = page.getByRole('button').filter({ hasText: /trial/i });
    await expect(submitButton).toBeEnabled();
    await submitButton.click();
    await page.waitForTimeout(1000);
  });
});
