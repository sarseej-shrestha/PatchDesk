import { defineConfig } from '@playwright/test'
import { resolve } from 'node:path'
export default defineConfig({
  testDir: './e2e', fullyParallel: false, workers: 1, timeout: 45000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { channel: 'chromium', baseURL: 'http://127.0.0.1:5173', viewport: { width: 1440, height: 1000 }, trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  webServer: [
    { command: 'mkdir -p ../.tools && ../scripts/dotnet.sh run --project ../backend/PatchDesk.Api --no-launch-profile -- --reset --seed && ../scripts/dotnet.sh run --project ../backend/PatchDesk.Api --no-launch-profile --urls http://127.0.0.1:5080', url: 'http://127.0.0.1:5080/api/health', timeout: 120000, reuseExistingServer: false,
      env: { ConnectionStrings__PatchDesk: `Data Source=${resolve('../.tools/patchdesk.e2e.db')};Pooling=False`, Logging__LogLevel__Default: 'Warning' } },
    { command: 'npm run dev', url: 'http://127.0.0.1:5173', timeout: 60000, reuseExistingServer: false },
  ],
})
