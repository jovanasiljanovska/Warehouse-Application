// playwright.config.js
const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
    testDir: './tests',
    timeout: 30_000,
    expect: { timeout: 5_000 },

    use: {
        baseURL: process.env.BASE_URL || 'http://localhost:5255',
        headless: false,              
        screenshot: 'only-on-failure',
        video: 'retain-on-failure',
        trace: 'retain-on-failure',
    },

    reporter: [['list'], ['html', { open: 'never' }]],
});
