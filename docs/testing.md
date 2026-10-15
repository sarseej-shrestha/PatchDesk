# Testing and evidence

The observed suite has **51 backend test cases: 30 unit, 20 integration, and one health endpoint check**. xUnit theory rows count as separate cases. Four Playwright cases exercise the real frontend and backend. See [verification](verification.md) for dated results and CI links.

## Backend

```bash
./scripts/dotnet.sh test backend/PatchDesk.sln --configuration Release --logger trx
./scripts/dotnet.sh test backend/PatchDesk.sln --filter FullyQualifiedName~RegressionTests
./scripts/dotnet.sh test backend/PatchDesk.sln --filter FullyQualifiedName~Integration
```

- **Unit:** valid/invalid state transitions, assignment before review, resolution notes/overrides, evidence freshness/result/commit matching, corrected calculator behavior, defective fixture detection, invalid calculator input, Git reference validation, and upstream GitHub errors.
- **Integration:** real migrated SQLite, repeatable seed, foreign keys, independent-context concurrency, CRUD, assignment, comments, search/filter/pagination, invalid entities/inputs, concurrent requests, API regression executions, duplicate association, code references, complete verified resolution/reopen, dirty-build rejection, manual resolution labeling, and rejection of forged execution fields.
- **Health:** the ASP.NET host responds through WebApplicationFactory.

Each API factory uses a unique temporary SQLite file and removes it at disposal. Persistence fixtures use an open SQLite in-memory connection **with migrations**, not EF's nonrelational in-memory provider. Test-only build identities simulate clean/dirty metadata in isolated lifecycle tests; these are fixtures, never seed data or published application evidence. Real build provenance is exercised separately by browser CI and the demo script.

## Before/after regression

`RegressionRegistry.Run("discount.boundary", true)` calls the isolated defective fixture, compares its actual result to the expected decimal, and returns a failure. The same assertion with `false` calls the corrected implementation and returns a pass.

The xUnit test verifies both outcomes; it does not leave an intentionally failing production test. The API stores the registry output, duration, timestamp, binary hash, fixture label, and source commit. The client cannot choose `Passed` or supply a commit. The API accepts only known identifiers; it never executes arbitrary scripts.

All current tests associated with a bug must pass for the current evidence revision and linked clean build. Historical passes, dirty builds, stale revisions, wrong commits, and defective-fixture executions do not qualify. Results are labeled **PatchDesk controlled registry**, not `dotnet test` or GitHub Actions. xUnit and Actions have their own independently observed results.

## Frontend and browser

```bash
npm --prefix frontend ci
npm --prefix frontend run lint -- --max-warnings 0
npm --prefix frontend run typecheck
npm --prefix frontend run build
npm --prefix frontend exec -- playwright install chromium
./scripts/browser-check.sh
```

Stop manually running servers on 5080/5173 first. The harness applies migrations, seeds a dedicated `.tools/patchdesk.e2e.db`, starts Kestrel and Vite, and uses full Chromium. It verifies:

1. Database-derived dashboard counts, searching, reporting, assignment, comments, and persistence after page reload.
2. Actual failing and passing regression executions and their historical outputs.
3. Linking the running commit, testing every association, reviewed resolution, reopening, and evidence invalidation on a clean build. On a dirty local build it instead asserts that verified resolution is rejected. CI **requires a clean build** and exercises the successful path.
4. Mobile navigation and absence of whole-page horizontal overflow at 390px width.

Playwright retains failure screenshots and traces, and writes an HTML report. Desktop/mobile screenshots are actual browser captures. These checks are not a comprehensive cross-browser or accessibility audit.

## CI

`.github/workflows/ci.yml` has independent backend, frontend, and browser jobs using .NET 10 and Node 24. Dependency lockfiles are committed. C# warnings and ESLint warnings fail their jobs. A failing test, type error, build error, or model drift fails CI; none is suppressed. Uploads run after failures where relevant, preserving diagnostic artifacts.

Backend TRX and Cobertura coverage files, frontend bundles, and Playwright reports/traces are artifacts. Coverage is collected, but no unmeasured coverage percentage is claimed. CI has no deployment or production secrets. GitHub API integration error handling is tested with an HTTP test double; no successful live PR review is fabricated when no actual PR exists.
