const { test, expect } = require('@playwright/test');

function uniqueUser() {
    const n = Date.now();
    return {
        email: `pw_${n}@test.local`,
        username: `pw_user_${n}`,
        password: 'Test123$',
    };
}

test('Register as Customer then Logout', async ({ page }) => {
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
        page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
        page.locator('form[method="post"] button[type="submit"]').click(),
    ]);


    await expect(page.getByText(`Hello ${u.username}!`)).toBeVisible();

    const logoutSubmit = page.locator('form[action*="Account/Logout"] button[type="submit"]');

    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
        logoutSubmit.click(),
    ]);

    const loginNav = page.locator('ul.navbar-nav a[href="/Account/Login"]').first();
    const registerNav = page.locator('ul.navbar-nav a[href="/Account/Register"]').first();

    await expect(loginNav).toBeVisible();
    await expect(registerNav).toBeVisible();

    await expect(page).toHaveURL(/\/($|Home\/Index$)/);
});
