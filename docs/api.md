# REST API

Base URL: `http://127.0.0.1:5080/api`. The running application generates OpenAPI 3.1 at `/openapi/v1.json` with request/response schemas. JSON names use camelCase and enum names are strings. Unknown properties, numeric enums, invalid ranges, and invalid required fields are rejected with 400.

This prototype has **no authentication**. `actorId`/`reporterId` select a fictional developer; they are not authenticated claims. Use `/developers` to obtain valid IDs.

## Routes

| Method | Path | Behavior |
|---|---|---|
| GET | `/health` | Process liveness; not a database-readiness guarantee |
| GET | `/developers` | Fictional developer IDs, names, emails, roles |
| GET | `/issues` | Paginated issue summaries |
| GET | `/issues/{id}` | Full issue, comments, activities, fix references, tests, executions |
| POST | `/issues` | Create an Open issue; 201 with Location |
| PUT | `/issues/{id}` | Update title, description, reproduction, priority, severity |
| DELETE | `/issues/{id}?actorId=1&version=1` | Delete unresolved issue and its children; 204 |
| GET | `/issues/{id}/comments` | Persisted comments |
| POST | `/issues/{id}/comments` | Add comment; 201 |
| PATCH | `/issues/{id}/assignee` | Set or clear assignee |
| GET | `/issues/{id}/transitions` | Structurally allowed next states and current evidence boolean |
| PATCH | `/issues/{id}/status` | Enforce workflow; update status and history |
| GET | `/dashboard/summary` | Actual issue counts and eight recent activity events |
| GET | `/activity?page=1&pageSize=30` | Paginated global history |
| GET | `/regression-registry` | Allowed tests and running build provenance |
| POST | `/issues/{id}/regression-tests` | Associate known test; 201 |
| POST | `/issues/{id}/regression-tests/{testId}/executions` | Run the selected predefined fixture; persist actual result |
| POST | `/issues/{id}/fix-references` | Link code reference and invalidate prior evidence; 201 |
| GET | `/github/pulls/{number}` | Read actual PatchDesk PR state and up to 100 check runs |

List responses are `{ "items": [...], "total": 25, "page": 1, "pageSize": 20 }`. Issue summaries use the issue DTO with empty child collections; fetch the detail endpoint for child data. Times are UTC on the server and displayed in the browser's local timezone.

## Filtering

```bash
curl 'http://127.0.0.1:5080/api/issues?search=discount&status=InProgress&priority=Low&assigneeId=5&sort=priority&page=1&pageSize=20'
```

- `page`: 1–100000; default 1. `pageSize`: 1–100; default 20 (activity default 30).
- `status`: Open, InProgress, InReview, Resolved, Reopened.
- `priority`: Low, Medium, High, Critical.
- `assigneeId`: positive existing-ID filter; unknown IDs return no matches.
- `search`: at most 200 characters; matches title/description case-insensitively for the demonstrated ASCII data.
- `sort`: newest (default), oldest, priority. ID provides a stable tie-breaker.

Unknown sort values and invalid enum/pagination values return 400. Search uses parameterized EF queries; it is not interpolated into SQL.

## Create an issue

```bash
curl -i http://127.0.0.1:5080/api/issues \
  -H 'Content-Type: application/json' \
  -d '{"title":"Discount threshold defect","description":"At $100 the 10% discount is skipped; expected $90.","reproductionSteps":"Calculate(100,10) using the defective fixture.","issueType":"Bug","priority":"High","severity":"Major","reporterId":1,"assigneeId":2}'
```

`title` is required, nonblank, at most 200 characters. `description` is required, nonblank, at most 10000 characters. `reproductionSteps` is a string up to 10000 characters (may be empty). Types: Bug, Feature, Improvement, Maintenance. Severities: Minor, Moderate, Major, Critical. Reporter must exist; assignee may be null. Duplicate titles are allowed because separate reports may describe the same symptom; duplicate test associations on one issue are not.

The response contains the server-assigned `id`, `status: "Open"`, `version: 1`, `evidenceRevision: 1`, timestamps, developer names, and a creation event. Use the actual returned ID/version; examples below assume a freshly seeded PD-104 and must be adjusted after each write.

## Edit, assignment, and discussion

Update request (PUT) contains `title`, `description`, `reproductionSteps`, `priority`, `severity`, `actorId`, `version`. It cannot change status, reporter, issue type, or timestamps. Editing fields invalidates verification. Resolved issues must first be reopened.

```json
{"assigneeId":2,"actorId":1,"version":1}
```

Send that body to PATCH `/issues/104/assignee`. Set `assigneeId` to null to unassign, except while the issue is in review.

```json
{"content":"Reproduced at the exact boundary.","actorId":1,"version":2}
```

POST to `/issues/104/comments`. Content is required, nonblank, max 5000. Comments can be added to resolved issues and do not invalidate evidence. Every successful mutation increments `version`; refetch or use the updated issue returned by mutations. Comment creation returns the comment DTO, so refetch the issue to obtain its new version.

## Workflow

Allowed edges are Open→InProgress; InProgress→InReview/Open; InReview→Resolved/InProgress/Open; Resolved→Reopened; Reopened→InProgress/Open. `/transitions` reports structural options, not a promise that all prerequisites are met.

```json
{"status":"InReview","actorId":1,"version":3}
```

```json
{"status":"Resolved","actorId":5,"version":4,"resolutionNote":"Current registered assertions pass for the linked build."}
```

Review requires assignment. Resolution requires a note (max 5000) and a simulated Reviewer. Bug resolution also requires current, passing, corrected-fixture evidence for **every** associated test, matching the full commit hash on the newest fix reference. Reopening clears resolution fields and invalidates evidence.

Manual override request adds `manualOverride: true` and a nonblank `overrideReason` (max 5000). The note and reviewer remain required. It is available only when transitioning into Resolved and records an explicit override activity. It is not verified resolution.

## Regression execution

GET `/regression-registry` returns definitions (`discount.boundary`, `discount.full`, `discount.rounding`), `buildCommit`, `isClean`, `assemblySha256`, and `executionSource`.

Associate:

```json
{"testIdentifier":"discount.boundary","actorId":1,"version":1}
```

Execute using the associated test's returned numeric ID:

```json
{"actorId":1,"version":2,"defectiveFixture":true}
```

Then execute with `defectiveFixture: false` and the updated version. The API computes the actual result; it accepts no result/commit/timestamp fields. Only reviewed repository identifiers may execute. Historical runs are available in GET issue detail. A dirty build records its binary fingerprint but an empty execution commit; it cannot satisfy verified resolution.

Executions report the actual registry source, not an invented CI run. New association, fix linking, relevant field edits, and reopening make older evidence stale. Multiple related tests must all pass again when the evidence revision changes.

## GitHub references

```json
{"branchName":"feat/regression-testing","commitHash":null,"pullRequestUrl":null,"description":"Controlled calculator regression implementation","actorId":1,"version":1}
```

At least one branch/commit/PR is required. Branch: max 200, conservative Git-safe syntax. Commit: exactly 40 hexadecimal characters. PR URL: exactly `https://github.com/sarseej-shrestha/PatchDesk/pull/<positive integer>`, without query/fragment/other repositories. Description: nonblank, max 2000. For verification use the real `buildCommit` returned by the running clean registry, not a placeholder.

The read-only lookup endpoint returns actual PR title/state/merge flag/head commit, named check statuses/conclusions, and `fetchedAt`. An empty check list is **not** success. Network/API errors remain errors; stored references still work. Optional server environment variable `GitHub__Token` raises authenticated access limits without exposing it to the frontend.

## Errors and concurrency

| Status | Meaning |
|---|---|
| 200 | Successful read/update/execution |
| 201 | Created issue/comment/association/reference |
| 204 | Deleted unresolved issue |
| 400 | Validation failure, unknown actor/test, invalid reference or override request |
| 404 | Missing issue/association/remote PR |
| 409 | Stale version, invalid transition, missing verification, duplicate association, protected resolved issue |
| 500 | Unexpected server error; details are logged, not exposed |
| 502/504 | GitHub unavailable/rate limited/upstream error or timeout |

Errors use a ProblemDetails-style structure with `status`, `title`, and `detail` or field `errors`. Server-handled exceptions include a `traceId`. Example conflict shape:

```json
{"status":409,"title":"Conflict","detail":"This issue changed. Refresh it and retry with the current version.","instance":"/api/issues/104/assignee","traceId":"<request identifier>"}
```

A conflicting request is not silently retried with a fresh version: the caller must inspect current state. EF's database concurrency token ensures two clients cannot both overwrite the same observed version.
