# Interview preparation

## Three architectural decisions to discuss

1. **Separate write concurrency from evidence freshness.** `Version` rejects stale clients; `EvidenceRevision` invalidates old tests when the issue or fix changes. A comment advances the write token without unnecessarily invalidating verification. Reopening and linking new code invalidate evidence.
2. **Execute a bounded registry, not user scripts.** The API and xUnit share known assertions. The server computes actual outcomes and records source commit and binary fingerprint. This demonstrates evidence-driven resolution without introducing arbitrary code execution or pretending a user-selected status is a test result.
3. **Use relational integration tests with the actual workflow.** WebApplicationFactory hosts the real API over isolated migrated SQLite databases. Tests cover concurrent updates, transaction rollback behavior, evidence provenance, and the full resolution/reopen lifecycle. Separate Playwright tests prove the client actually communicates with it.

## Three likely questions

**How do you prevent two engineers from overwriting one another?**

Every mutation submits the version the client read. The service returns 409 if it is stale. EF also includes the original version in its update predicate, so a race after the service check still fails atomically. Tests verify both independent EF contexts and concurrent HTTP requests, with exactly one winning update.

**What makes “verified” different from “resolved” here?**

Verified bug resolution requires all associated tests' newest executions to pass, use the corrected implementation, match the current evidence revision, and match the newest linked fix commit. Dirty builds cannot claim commit-bound evidence. The manual path requires a reason and simulated reviewer, and is labeled as an override. The prototype does not authenticate actors or offer cryptographic CI attestations.

**Why use SQLite and a modular monolith? What would change in production?**

They make a small project reproducible and easy to test with real foreign keys, transactions, and migrations. A production design would begin with identity/access control, deployment topology, backups, monitoring, retention, and measured workload. A server database may be appropriate for multi-instance write concurrency; microservices are not automatically required.

## 90-second demonstration

Use the timed [demo script](demo.md). Show the actual failed boundary assertion, corrected output, linked build commit, real CI run, reviewed resolution, and persisted history. Explain that the defective implementation is an isolated fixture introduced alongside the correction, not an invented historical production incident.

## Two truthful resume bullets

- Built an ASP.NET Core 10 and React/TypeScript issue-management prototype with EF Core migrations, SQLite persistence, optimistic concurrency, and evidence-gated resolution linking fixes to executed regression tests.
- Implemented and passed 51 backend test cases and four Chromium end-to-end cases; configured GitHub Actions to build the API/frontend, check migration drift, and publish test and browser artifacts.

These describe the implemented portfolio project. They do not imply production adoption, employment history, performance gains, or independent authorship claims.
