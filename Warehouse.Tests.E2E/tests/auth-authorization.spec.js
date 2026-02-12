const { test, expect } = require('@playwright/test');

test('Guest is redirected to Identity Login when opening Cart', async ({ page }) => {
    await page.goto('/Carts');

    await expect(page).toHaveURL(/\/Identity\/Account\/Login/i);
    await expect(page).toHaveURL(/ReturnUrl=%2FCarts/i);

    await expect(page.getByRole('heading', { name: 'Log in', exact: true })).toBeVisible();

    await expect(page.locator('#Input_Email')).toBeVisible();
    await expect(page.locator('#Input_Password')).toBeVisible();

    await expect(page.getByRole('button', { name: /log in/i })).toBeVisible();
});





test('Customer cannot access InventoryDashboard (Employee-only)', async ({ page }) => {
    const n = Date.now();
    const email = `pw_${n}@test.local`;
    const username = `pw_user_${n}`;
    const password = 'Test123$';

    await page.goto('/Account/Register');
    await page.locator('input[name="Email"]').fill(email);
    await page.locator('input[name="UserName"]').fill(username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');
    await page.locator('select[name="Role"]').selectOption('Customer');
    await page.locator('input[name="Password"]').fill(password);
    await page.locator('input[name="ConfirmPassword"]').fill(password);
    await page.locator('button[type="submit"]').click();

    await page.goto('/Products/InventoryDashboard');

    await expect(page.locator('body')).toContainText(/AccessDenied|Access Denied|Login/i);
});
