import { test, expect } from '@playwright/test'

test('dashboard, filtering, report, assignment and comment persist through reload', async ({
  page,
}) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Every fix needs evidence.' })).toBeVisible()
  await expect(page.locator('.metric').first()).toContainText('25')
  await page.screenshot({ path: '../.tools/overview.png', fullPage: true })
  await page.getByRole('link', { name: 'Issue explorer' }).click()
  await page.getByRole('textbox', { name: 'Search issues' }).fill('discount')
  await expect(page.getByRole('link', { name: /Discount calculation fails/ })).toBeVisible()
  await page.getByRole('link', { name: '+ Report issue' }).click()
  await page.getByLabel('Title', { exact: true }).fill('Browser-created regression issue')
  await page
    .getByLabel('Description', { exact: true })
    .fill('Persisted from a real Chromium interaction.')
  await page
    .getByLabel('Reproduction steps', { exact: true })
    .fill('Open workspace and create issue.')
  await page.getByRole('button', { name: 'Create issue' }).click()
  await expect(
    page.getByRole('heading', { name: 'Browser-created regression issue' }),
  ).toBeVisible()
  await page.getByLabel('Issue assignee', { exact: true }).selectOption('3')
  await expect(page.getByRole('status')).toContainText('Changes saved')
  await page.getByLabel('Add a comment').fill('Confirmed persistence from the browser.')
  await page.getByRole('button', { name: 'Post comment' }).click()
  await expect(page.locator('.comment')).toContainText('Confirmed persistence from the browser.')
  await page.reload()
  await expect(page.getByLabel('Issue assignee', { exact: true })).toHaveValue('3')
  await expect(page.locator('.comment')).toContainText('Confirmed persistence from the browser.')
})

test('verification executes the defective and corrected assertion with real results', async ({
  page,
}) => {
  await page.goto('/verification/104')
  const boundary = page.locator('.test-card').filter({ hasText: 'Discount at the $100 boundary' })
  await boundary.getByRole('button', { name: 'Run defective fixture' }).click()
  await expect(boundary.locator('.badge').first()).toHaveText('Failed')
  await boundary.getByRole('button', { name: 'Run corrected test' }).click()
  await expect(boundary.locator('.badge').first()).toHaveText('Passed')
  await boundary.getByText('Execution history (2)').click()
  await expect(boundary.locator('.execution').first()).toContainText('actual=90')
  await expect(boundary.locator('.execution').last()).toContainText('actual=100')
})

test('linked build evidence gates resolution and reopening invalidates it', async ({
  page,
  request,
}) => {
  const registry = (await (
    await request.get('http://127.0.0.1:5080/api/regression-registry')
  ).json()) as { buildCommit: string; isClean: boolean }
  if (process.env.CI) expect(registry.isClean).toBe(true)
  await page.goto('/verification/104')
  await page.getByText('Link a code change', { exact: true }).click()
  await page.getByLabel('Full commit hash').fill(registry.buildCommit)
  await page
    .getByLabel('Change description')
    .fill('Link the running build to regression verification.')
  await page.getByRole('button', { name: 'Save code reference' }).click()
  await expect(page.locator('.reference')).toContainText(registry.buildCommit)
  for (const card of await page.locator('.test-card').all()) {
    await card.getByRole('button', { name: 'Run corrected test' }).click()
    await expect(card.locator('.badge').first()).toHaveText('Passed')
    await expect(card.getByRole('button', { name: 'Run corrected test' })).toBeEnabled()
  }
  await page.getByLabel('Next status').selectOption('InReview')
  await page.getByRole('button', { name: 'Update status' }).click()
  await expect(page.locator('.page-heading .badge')).toHaveText('In Review')
  await page.getByLabel('Simulated actor').selectOption('5')
  await page.getByLabel('Next status').selectOption('Resolved')
  await page
    .getByLabel('Resolution note')
    .fill('Boundary and full-discount assertions pass for the linked build.')
  await page.getByRole('button', { name: 'Update status' }).click()
  if (!registry.isClean) {
    await expect(page.getByRole('status')).toContainText('Verified bug resolution requires')
    await expect(page.locator('.page-heading .badge')).toHaveText('In Review')
  } else {
    await expect(page.locator('.page-heading .badge')).toHaveText('Resolved')
    await page.getByLabel('Next status').selectOption('Reopened')
    await page.getByRole('button', { name: 'Update status' }).click()
    await expect(page.locator('.page-heading .badge')).toHaveText('Reopened')
    await expect(page.locator('.detail-aside')).toContainText(
      'Verified bug resolution needs current passing evidence',
    )
  }
})

test('mobile navigation and layout remain usable without page overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Every fix needs evidence.' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  )
  await page.getByRole('link', { name: 'Issue explorer' }).click()
  await expect(page.getByRole('textbox', { name: 'Search issues' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  )
  await page.screenshot({ path: '../.tools/mobile.png', fullPage: true })
})
