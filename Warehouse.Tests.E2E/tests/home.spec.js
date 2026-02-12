const { test, expect } = require('@playwright/test');

test('Home loads', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Warehouse/i);
});
