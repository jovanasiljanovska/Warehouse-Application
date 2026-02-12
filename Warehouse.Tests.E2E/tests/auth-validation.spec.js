const { test, expect } = require('@playwright/test');

function uniqueUser() {
    const n = Date.now();
    return {
        email: `pw_${n}@test.local`,
        username: `pw_user_${n}`,
        weakPassword: '123',
        strongPassword: 'Test123$',
    };
}

test('Register with weak password -> stays on Register and shows validation error', async ({ page }) => {
    const u = uniqueUser();

    await page.goto('/Account/Register');

    await page.locator('input[name="Email"]').fill(u.email);
    await page.locator('input[name="UserName"]').fill(u.username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');
    await page.locator('select[name="Role"]').selectOption('Customer');

    await page.locator('input[name="Password"]').fill(u.weakPassword);
    await page.locator('input[name="ConfirmPassword"]').fill(u.weakPassword);

    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/\/Account\/Register/i);

    const errors = page.locator('.text-danger');
    await expect(errors.first()).toBeVisible();

    
    await expect(page.locator('body')).toContainText(/password/i);
});

test('Login with wrong password -> shows "Invalid login attempt."', async ({ page }) => {
    const u = uniqueUser();

    
    await page.goto('/Account/Register');

    await page.locator('input[name="Email"]').fill(u.email);
    await page.locator('input[name="UserName"]').fill(u.username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');
    await page.locator('select[name="Role"]').selectOption('Customer');

    await page.locator('input[name="Password"]').fill(u.strongPassword);
    await page.locator('input[name="ConfirmPassword"]').fill(u.strongPassword);

    await Promise.all([
        page.waitForURL(/\/(Home\/Index)?$/i, { timeout: 15000 }).catch(() => { }),
        page.locator('button[type="submit"]').click(),
    ]);

    
    const logoutBtn = page.locator('form[action*="Account/Logout"] button[type="submit"]');
    if (await logoutBtn.count()) {
        await logoutBtn.click();
    }

   
    await page.goto('/Account/Login');

    await page.locator('input[name="EmailOrUserName"]').fill(u.email); 
    await page.locator('input[name="Password"]').fill('WrongPassword123!');
    await page.locator('button[type="submit"]').click();

   
    await expect(page).toHaveURL(/\/Account\/Login/i);
    await expect(page.locator('body')).toContainText('Invalid login attempt.');
});
