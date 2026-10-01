# Fleet master data (Phase 4)

Vehicles, Drivers and Customer Companies are implemented in the existing modular architecture. Application contains request/response contracts; Infrastructure contains the three services; API controllers use the existing ManageFleet policy. All reads and writes, including deleted-record reads, require FleetAdministrator. Anonymous callers receive 401 and all four other roles receive 403. No broad operational read access has been granted.

## API

Each of `/api/vehicles`, `/api/drivers`, `/api/customers` supports:

| Method | Route suffix | Behavior |
| --- | --- | --- |
| GET | (collection) | Paged list, excluding deactivated records by default |
| GET | /{id} | Detail DTO; 404 for missing/deactivated records by default |
| POST | (collection) | Create; 201 and Location header |
| PUT | /{id} | Update using the version read by the client; 200 |
| POST | /{id}/deactivate | Body `{ "version": 123 }`; soft-delete and return updated DTO |
| POST | /{id}/reactivate | Same version contract; restore normal visibility |

List parameters: page (1–1,000,000, default 1), pageSize (1–100, default 20), search (maximum 200 characters), includeDeleted (default false). Vehicle and driver lists also accept exact enum names in status. Customers reject a supplied status filter. Detail reads accept includeDeleted=true for administrator inspection. Responses use the existing items/page/pageSize/totalCount envelope. Counts, case-insensitive substring searches, status filters, stable ordering and paging execute in PostgreSQL before materialization. Read queries use AsNoTracking. Driver account summaries are fetched in one batch per page.

DTOs expose business fields, id, isDeleted, version and nested audit {createdBy, createdAt, updatedBy, updatedAt}. Driver DTOs additionally expose a nullable linkedAccount {id, displayName, email, isActive}. Passwords, security stamps and EF navigation graphs are never returned.

Validation errors use 400; missing records 404; uniqueness, stale versions and unsafe business changes 409. Existing centralized Problem Details handling hides database exception details. There are no physical DELETE endpoints.

## Validation and workflow boundaries

- Registration: required, trimmed/uppercased using the Phase 2 normalization, maximum 32 characters. Model: required, maximum 120. Year: existing 1900–2100 database range. Capacity: positive kilograms, at most two decimal places, maximum 9,999,999,999.99 to fit numeric(12,2). Odometer: nonnegative whole kilometres, cannot decrease through normal editing. The API caps odometers at JavaScript's largest safe integer so the browser cannot silently round a submitted reading.
- New vehicles start Available. Update requests must preserve current status. OnTrip and UnderMaintenance cannot be set through generic editing; trip and maintenance workflows are deferred. There is no CurrentDriverId on this entity; the existing model derives assignments from trips.
- Driver name: required, maximum 160. Licence: required, trimmed/uppercased, maximum 64. Contact: required, maximum 40; accepts international number punctuation and extensions without country-specific length/prefix rules. Drivers can be Available or Inactive through this module; OnTrip is reserved for the later trip workflow.
- Vehicle/driver edits and activation changes reject both an OnTrip status and any actual Assigned/InProgress trip. Checking the trip table prevents inconsistent resource status from bypassing the safeguard. This phase conservatively blocks all master edits while a resource is reserved, including descriptive edits.
- Creating a driver never creates a login. Linking stays in the Phase 3 Users service. Linked drivers cannot be soft-deactivated, even if their account is inactive: first reassign the account to another driver or change its role/remove its link through Users. Driver mutation and user linking share the existing transactional Identity advisory lock. Generic driver requests cannot alter ApplicationUserId. Driver Inactive is operational availability; it does not deactivate the separate login account.
- Company name: required, maximum 200. Contact person (160), telephone (40), email (254) and address (500) may be empty, matching the existing non-null string schema. Email is validated when present. Telephone accepts digits, international prefixes, common punctuation and extension markers; no country-specific format is imposed. Text fields are trimmed and reject control characters; addresses are a single line.
- Existing fictional seed contacts explicitly contain DEMO-NOT-A-PHONE placeholders. They remain unchanged and visible; replace a placeholder with a valid contact number when saving that record through the new forms. Seeding remains deterministic, additive and idempotent.
- Customers with unfinished orders (Draft, Assigned, InProgress) cannot be deactivated. Completed/cancelled orders and all historical relationships remain intact. This guard does not implement Order Management.

## Soft deletion, concurrency and audit

Phase 2 IsDeleted/query filters are reused, with no second deletion flag. Editing a deactivated record returns 409 until it is reactivated. Registration/licence uniqueness includes deactivated rows, so identifiers remain reserved. Database unique indexes remain the final arbiter, including concurrent creates/updates/reactivation.

Every update/deactivate/reactivate must provide the version returned by a read. Missing/zero or stale versions return 409. Vehicles and drivers retain their existing PostgreSQL xmin mapping. Customers lacked a concurrency token in the actual Phase 2 model; the new CustomerConcurrency migration maps their existing PostgreSQL xmin system column, using the same uint row-version approach. The Npgsql provider handles the system column; no separate counter/version mechanism is introduced. Previously applied migration source files are untouched.

Preflight version comparison gives a useful response for stale forms; EF's original xmin condition also detects races between loading and saving. API DTOs are re-read after persistence so timestamp precision reflects PostgreSQL. UTC auditing remains centralized in FleetManagementDbContext and uses the authenticated HTTP subject. Creation audit metadata remains immutable.

The React forms retain the version originally loaded. On 409 they display the server explanation and offer Reload latest record (discard edits). They never silently retry with a newer version. Activation conflicts offer cancel/reload of the list before another confirmation.

Future trip assignment/order services must transact their checks and resource updates and coordinate concurrent reservations/deactivation. The existing active-resource indexes and xmin safeguards remain; Phase 4 does not claim to implement dispatch transactions.

## Frontend

Administrator navigation includes Dashboard, Users, Vehicles, Drivers and Customers. Each feature has a lazy-loaded page using shared typed list/dialog components, the existing API client and protected routes. Lists provide search, relevant status chips/filters, backend paging, include-deactivated toggle, view/edit actions and confirmed deactivate/reactivate actions. Forms provide validation, loading/saving states, server errors, read-only details/audit metadata and success feedback. Tables scroll horizontally on narrow screens and forms use responsive MUI dialogs. No production frontend data arrays or alternate UI frameworks were introduced.

Order creation, trips, maintenance histories/workflows, analytics, exports, billing and notifications are intentionally absent. Phase 5 has not begun.

## Live React/API verification

An opt-in suite renders the actual React application in jsdom and uses real HTTP requests to a running PostgreSQL-backed API. It does not mock fetch. Use only an isolated development database, apply all migrations, and run the existing seed initialization. The suite creates clearly fictional test records and exercises login, lists, create, detail, update, soft deactivation and reactivation for all three modules.

From frontend, set VITE_API_BASE_URL to the isolated API URL and FMS_LIVE_TEST_PASSWORD privately to that database's administrator seed password, then run:

~~~sh
npx vitest run --config vitest.live.config.ts
~~~

The password is injected through Vitest's test-only provided context, not a VITE_ variable or application bundle. Each run uses distinct fictional identifiers. The normal npm test suite remains independent of a running API. These tests verify rendered component behavior and HTTP persistence; they do not substitute for a visual browser/layout review.
