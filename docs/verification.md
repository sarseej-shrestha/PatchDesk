# Observed verification

Recorded 2026-10-15. These are executed checks, not planned checks. Links point to the exact historical runs; later commits have their own runs in [Actions](https://github.com/sarseej-shrestha/PatchDesk/actions).

| Check | Observed result |
|---|---|
| npm, NuGet, Microsoft SDK connectivity | HTTP 200; packages/SDK downloaded successfully |
| Local runtime setup | .NET SDK 10.0.402; Node 24.22.0 installed from official sources |
| GitHub identity | API returned `sarseej-shrestha`; `sarseej.shrestha@selu.edu` verified |
| Release backend build | Passed, zero warnings/errors |
| Backend suite | 51 passed, zero failed/skipped: 30 unit, 20 integration, one health |
| Database | Committed migration applied; seed repeatable; foreign keys and concurrency checked |
| Model drift | `has-pending-model-changes`: no pending changes |
| ESLint | Passed with zero warnings required |
| TypeScript | Strict type check passed |
| Frontend production bundle | Built successfully |
| Local browser | Four full-Chromium cases passed; real interactions and reload persistence |
| Mobile | 390px viewport navigation and no document-wide overflow; screenshot inspected |
| OpenAPI | Served successfully, version 3.1.1, 15 distinct paths |
| Actual regression demo | Defective boundary returned 100 (Failed); corrected returned 90.0 (Passed); full discount returned 0 (Passed) |
| Actual verified resolution | PD-104 resolved without override, associated with clean build commit `33e1ea58395fe4448c065b3bbabc3d490a95eda5` |

## GitHub Actions

First complete pipeline: [run 38705777294](https://github.com/sarseej-shrestha/PatchDesk/actions/runs/38705777294), commit `a4a71490f3af508543d436d4cf2b293b38e39bf4`.

Development-polish correction: [run 38706832614](https://github.com/sarseej-shrestha/PatchDesk/actions/runs/38706832614), commit `33e1ea58395fe4448c065b3bbabc3d490a95eda5`.

Both completed successfully. The first run's downloaded logs explicitly reported 51 backend tests passed, four browser tests passed, zero build warnings, and no migration drift. The three jobs separately verify backend, frontend, and real Chromium interactions. TRX/coverage and browser artifacts are attached to each run.

## Failures found and resolved

- Initial sandbox NuGet calls could not access certificate services. Approved access enabled installation; no checks were bypassed.
- The default transitive SQLite native package emitted a high-severity advisory. The bundle was upgraded to 3.0.5, which resolved the warning with the restored native SQLite dependency.
- A new regression execution initially returned 500 after persistence because its actor navigation was unloaded. `fix/activity-actor-loading` loads validated actors into the unit of work; the failing regression API test then passed. The earlier push had already integrated that failing milestone due to command sequencing; the separate correction preserves that actual history rather than rewriting it.
- The downloaded macOS headless-shell browser crashed with missing ICU data. Full Chromium ran the same tests successfully. CI also uses full Chromium.
- The default backend port and a removed template favicon reference were aligned/fixed on `fix/development-polish`.

No GitHub Actions build failure was observed in these recorded runs. There was therefore no CI failure to suppress or fabricate a correction for.

## Limits of verification

- Local registry execution is not CI attestation. [demo-evidence.json](demo-evidence.json) records its true execution source and historical build.
- Browser checks cover the documented smoke paths in Chromium, not every interaction or every browser. No full accessibility or production load test was performed.
- Successful live PR review/check retrieval was not claimed without an actual PR. GitHub reference persistence and error handling were tested; external API availability and permissions can change.
- Authentication, multi-user authorization, production deployment, and multi-instance SQLite operation are not implemented or verified.
- Seed histories are explicitly fictional. No production users, adoption, or performance gains are claimed.
