const { test, expect } = require('@playwright/test');

function uniqueUser() {
    const n = Date.now();
    return {
        email: `pw_${n}@test.local`,
        username: `pw_user_${n}`,
        password: 'Test123$',
    };
}

async function registerCustomer(page, u) {
    await page.goto('/Account/Register');

    await page.locator('input[name="Email"]').fill(u.email);
    await page.locator('input[name="UserName"]').fill(u.username);
    await page.locator('input[name="FirstName"]').fill('Play');
    await page.locator('input[name="LastName"]').fill('Wright');
    await page.locator('select[name="Role"]').selectOption('Customer');
    await page.locator('input[name="Password"]').fill(u.password);
    await page.locator('input[name="ConfirmPassword"]').fill(u.password);

    await page.locator('form[action$="/Account/Register"] button[type="submit"]').click();

    await expect(page.locator('ul.navbar-nav').last()).toContainText(`Hello ${u.username}!`);
}

test('Customer CreateOrder flow: Shop Catalog -> Explore -> Add to cart -> Place Order', async ({ page }) => {
    const u = uniqueUser();
    await registerCustomer(page, u);

    await page.locator('header nav').getByRole('link', { name: 'Shop Catalog' }).click();
    await expect(page).toHaveURL(/\/Categories/i);

    const exploreBtn =
        page.getByRole('link', { name: /explore/i }).first()
            .or(page.getByRole('button', { name: /explore/i }).first())
            .or(page.locator('a:has-text("Explore Products")').first())
            .or(page.locator('a:has-text("Explore")').first())
            .or(page.locator('a:has-text("Details")').first());

    await expect(exploreBtn, 'No Explore/Details button found. Probably no seeded products in Catalog.').toBeVisible();
    await exploreBtn.click();


    const addToCartBtn =
        page.getByRole('button', { name: /add to cart/i }).first()
            .or(page.locator('button:has-text("Add to cart")').first())
            .or(page.locator('button:has-text("Add")').first())
            .or(page.locator('form[action*="Add"] button[type="submit"]').first());

    await expect(addToCartBtn, 'No "Add to cart" button found on product page.').toBeVisible();
    await addToCartBtn.click();


    await page.locator('header nav').getByRole('link', { name: /cart/i }).click();
    await expect(page).toHaveURL(/\/Carts/i);

    await expect(page.getByRole('heading', { name: /shopping cart/i })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('Your cart is empty');


    const placeOrderBtn = page.locator('form[action$="/Carts/Checkout"] button:has-text("Place Order")');
    await expect(placeOrderBtn).toBeVisible();

    await Promise.all([
        page.waitForURL(/\/CustomerOrders\/Details/i, { timeout: 15000 }),
        placeOrderBtn.click(),
    ]);


    await expect(page).toHaveURL(/\/CustomerOrders\/Details/i);
});
