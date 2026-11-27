
import { test, expect } from '@playwright/test';
import { testContent } from './fixtures/content';

test.beforeEach(async ({ page }) => {
    await page.route(
        'https://lugarlang-api.onrender.com/api/content',
        async route => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify(testContent),
            });
        }
    );

    await page.goto('/');
});

test('Discover page loads LugarLang content', async ({ page }) => {
    await expect(
        page.getByRole('heading', {
            name: 'Discover places worth finding.',
        })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', {
            name: 'Northern Mindanao',
        })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', {
            name: 'Featured Places',
        })
    ).toBeVisible();
});

test('categories are displayed', async ({ page }) => {
    await expect(
        page.getByRole('button', {
            name: /Food 2 places available/,
        })
    ).toBeVisible();

    await expect(
        page.getByRole('button', {
            name: /Offices 1 place available/,
        })
    ).toBeVisible();
});

test('selecting a category filters the displayed places', async ({ page }) => {
    const foodCategory = page.getByRole('button', {
        name: /Food 2 places available/,
    });

    await foodCategory.click();

    await expect(foodCategory).toHaveAttribute('aria-pressed', 'true');

    await expect(
        page.getByRole('heading', { name: 'Food Places' })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', { name: 'Cucina Higala' })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', { name: 'Test Food Place' })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', { name: 'Test Office' })
    ).not.toBeVisible();
});

test('selecting a place opens its details', async ({ page }) => {
    await page.getByRole('button', {
        name: /Cucina Higala/,
    }).click();

    await expect(
        page.getByRole('heading', {
            name: 'Cucina Higala',
            level: 1,
        })
    ).toBeVisible();

    await expect(
        page.getByText('About this place')
    ).toBeVisible();
});

test('returning from place details restores Discover', async ({ page }) => {
    await page.getByRole('button', {
        name: /Cucina Higala/,
    }).click();

    await page.getByRole('button', {
        name: /Back to Discover/,
    }).click();

    await expect(
        page.getByRole('heading', {
            name: 'Discover places worth finding.',
        })
    ).toBeVisible();

    await expect(
        page.getByRole('heading', {
            name: 'Featured Places',
        })
    ).toBeVisible();
});
