const { test, expect } = require('@playwright/test');

function uniqueUser() {
    const n = Date.now();
    return {
        email: `pw_${n}@test.local`,
        username: `pw_supplier_${n}`,
        password: 'Test123$',
    };
}

test('Supplier register shows Supplier navbar links', async ({ page }) => {
    const u = uniqueUser();

    await page.goto('/Account/Register');

    await page.locator('input[name="Email"]').fill(u.email);
    await page.locator('input[name="UserName"]').fill(u.username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');

    // Supplier role -> company box appears
    await page.locator('select[name="Role"]').selectOption('Supplier');
    await expect(page.locator('#companyNameBox')).toBeVisible();
    await page.locator('input[name="CompanyName"]').fill('PW Supplier Co');

    await page.locator('input[name="Password"]').fill(u.password);
    await page.locator('input[name="ConfirmPassword"]').fill(u.password);

    await Promise.all([
        page.waitForLoadState('networkidle'),
        page.locator('button[type="submit"]').click(),
    ]);

    // Check supplier-only navbar items (based on your _Layout)
    // Incoming Requests + History + Import from API button
    const nav = page.locator('header nav');
    await expect(nav).toContainText('Incoming Requests');
    await expect(nav).toContainText('History');
    await expect(nav).toContainText('Import from API');

    // Optional: logout at end to keep state clean
    const logoutBtn = page.locator('form[action*="Logout"] button[type="submit"]');
    if (await logoutBtn.count()) await logoutBtn.first().click();
});
