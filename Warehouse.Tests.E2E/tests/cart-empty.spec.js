const { test, expect } = require('@playwright/test');

function uniqueUser() {
    const n = Date.now();
    return {
        email: `pw_${n}@test.local`,
        username: `pw_customer_${n}`,
        password: 'Test123$',
    };
}

test('Customer sees empty cart message and Start ordering link', async ({ page }) => {
    const u = uniqueUser();

    await page.goto('/Account/Register');
    await page.locator('input[name="Email"]').fill(u.email);
    await page.locator('input[name="UserName"]').fill(u.username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');
    await page.locator('select[name="Role"]').selectOption('Customer');
    await page.locator('input[name="Password"]').fill(u.password);
    await page.locator('input[name="ConfirmPassword"]').fill(u.password);

    await Promise.all([
        page.waitForLoadState('networkidle'),
        page.locator('button[type="submit"]').click(),
    ]);

    await page.goto('/Carts');

    await expect(page.getByRole('heading', { name: /shopping cart/i })).toBeVisible();
    await expect(page.locator('body')).toContainText('Your cart is empty');

    const startOrdering = page.getByRole('link', { name: /start ordering/i });
    await expect(startOrdering).toBeVisible();
    await expect(startOrdering).toHaveAttribute('href', '/Categories');

    await startOrdering.click();
    await expect(page).toHaveURL(/\/Categories/i);
});
