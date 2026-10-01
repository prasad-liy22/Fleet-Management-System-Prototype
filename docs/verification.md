# Verification history

## Phase 4 — verified 2026-10-01

Implemented Vehicles, Drivers and Customer Companies management using the existing Phase 1–3 architecture. Full rules and API contracts are in master-data.md. Phase 5 was not started.

| Check | Executed result |
| --- | --- |
| Backend restore | Passed; all projects restored/up to date |
| Entire backend build | Passed; 0 compiler warnings, 0 errors |
| All backend tests with real PostgreSQL | **85 passed, 0 failed, 0 skipped**; includes all 51 Phase 1–3 tests, 33 master-data cases and an added customer xmin model case |
| PostgreSQL migration | Applied 20261001172250_CustomerConcurrency to the retained local database and fresh verification/test databases |
| Pending EF model changes | None |
| Original migration files | Unchanged; verified against Git |
| Migration script idempotency | Existing integration test retained, expanded for third migration, passes two executions |
| Frontend build / TypeScript | Passed |
| Complete normal Vitest suite | **42 passed, 0 failed**; original 21 tests plus 21 master-data UI cases |
| Live React → HTTP API → PostgreSQL | **3 passed, 0 failed**; separate opt-in jsdom suite, no fetch mocks |
| npm audit | 0 vulnerabilities across all severities |
| Live server authorization | Admin: 200 on all three collections; Operations/Mechanic/Driver/Owner: 403 on all three; anonymous: 401 on all three |
| Deterministic seed initialization | Ran twice against isolated database; 3 seeded vehicles, 2 drivers, 2 customers, 5 accounts and 5 roles confirmed with SQL |
| Private local credentials | Exact-value scan of all 131 repository candidate files passed; local .tools content remains ignored |
| Whitespace/diff checks | Passed after final cleanup |
| Visual browser review | **Not performed**: desktop browser creation timed out and opening the panel was queued. No screenshot/layout review is claimed. |

### What the tests verified

Real PostgreSQL HTTP integration tests cover create/read/update/soft-delete/reactivate for all three entities, normalized unique registration/licence protection (including deactivated records), validation and unsupported status changes, monotonic odometer, stale edits and stale activation requests, simultaneous updates with exactly one successful writer, actual audit actor and immutable creation timestamps, list filters/search/pagination, 400/401/403/404/409 behavior, and every non-administrator role against reads and mutations.

Additional relationship tests prove linked accounts remain attached after driver edits, generic driver requests cannot change the link, linked drivers cannot be deactivated, and no login is created implicitly. Actual Assigned trips block vehicle/driver edits even when resource status is inconsistent. Unfinished orders block customer deactivation. Cancelled order/trip history remains after successful soft deletion of associated master records. Existing relational/Identity/security tests remain intact.

The normal frontend suite covers lists, read-only details, create/edit requests, required and numeric/email validation, submitted versions, confirmation before deactivation, reactivation, loading/empty/error/retry states, server pagination/search/status filters, conflict reload using the new version and protected routes for all three features.

The opt-in live suite renders the real App, signs in through the real API, and creates, searches, views, edits, deactivates, finds deactivated records, reactivates and signs out for each module. It ran against an isolated local PostgreSQL database and a real Kestrel API, without mocked HTTP. SQL confirmed the created/reactivated rows. This verifies component/API integration but does not exercise browser CORS enforcement or visual responsive layout.

### Fixes during verification and retained advisories

The existing migration test now requires the third migration instead of two; its original history and idempotency assertions are preserved. A timestamp comparison exposed the .NET/PostgreSQL sub-microsecond precision difference: new service responses now re-read persisted DTO values. An initial frontend run hit the existing asynchronous test timeout under parallel load; one Vitest worker resolved the contention without removing assertions. Live HTTP tests use a separate longer wait for API startup/password hashing. Final suites above all pass.

The four inherited EF required-navigation/global-query-filter warnings remain documented in database-design.md. Vite still reports an approximately 506 kB uncompressed initial chunk (159 kB gzip); feature modules are lazy-loaded. These are advisories, not compiler/test failures.

### Assumptions and scope

Vehicle operational status stays workflow-controlled; drivers may switch Available/Inactive only when unreserved. All vehicle/driver edits are blocked during active trips. Linked drivers must be unlinked/reassigned through Users before deactivation. Customer contact fields other than company name may be empty; provided numbers/emails are validated. Existing fictional phone placeholders were not rewritten and need valid numbers before saving affected records through these forms. Identifier uniqueness continues across deactivated rows. Future dispatch/order services must coordinate transactional reservations with master-record mutations.

All create/update/activation operations are administrator-only on the server. Soft deletion and UTC audit infrastructure are reused. Customers gain xmin through a new migration because the inspected Phase 2 schema lacked a customer concurrency token. No existing migration was rewritten, no deterministic seed was changed, and no order/trip/maintenance workflow was implemented.

Verification API/frontend/PostgreSQL processes were stopped after completion. The isolated live-test database was dropped; the retained development database keeps the applied customer migration. Private credentials and verification logs remain under ignored .tools. No commit or push was performed for Phase 4.

### Files created

- backend/FleetManagement.Api/Controllers/CustomersController.cs
- backend/FleetManagement.Api/Controllers/DriversController.cs
- backend/FleetManagement.Api/Controllers/VehiclesController.cs
- backend/FleetManagement.Application/MasterData/MasterContracts.cs
- backend/FleetManagement.Infrastructure/MasterData/CustomerService.cs
- backend/FleetManagement.Infrastructure/MasterData/DriverService.cs
- backend/FleetManagement.Infrastructure/MasterData/MasterValidation.cs
- backend/FleetManagement.Infrastructure/MasterData/VehicleService.cs
- backend/FleetManagement.Infrastructure/Persistence/Migrations/20261001172250_CustomerConcurrency.Designer.cs
- backend/FleetManagement.Infrastructure/Persistence/Migrations/20261001172250_CustomerConcurrency.cs
- backend/FleetManagement.Tests/MasterData/MasterDataTests.cs
- docs/master-data.md
- frontend/src/features/customers/CustomersPage.tsx
- frontend/src/features/drivers/DriversPage.tsx
- frontend/src/features/master-data/MasterData.live.tsx
- frontend/src/features/master-data/MasterDialog.tsx
- frontend/src/features/master-data/MasterPage.test.tsx
- frontend/src/features/master-data/MasterPage.tsx
- frontend/src/features/master-data/config.ts
- frontend/src/features/master-data/types.ts
- frontend/src/features/vehicles/VehiclesPage.tsx
- frontend/vitest.live.config.ts

### Files modified

- backend/FleetManagement.Domain/Entities/CustomerCompany.cs
- backend/FleetManagement.Infrastructure/DependencyInjection.cs
- backend/FleetManagement.Infrastructure/Persistence/Configurations/CustomerCompanyConfiguration.cs
- backend/FleetManagement.Infrastructure/Persistence/Migrations/FleetManagementDbContextModelSnapshot.cs
- backend/FleetManagement.Tests/Persistence/ModelTests.cs
- backend/FleetManagement.Tests/Persistence/PersistenceTests.cs
- docs/api-overview.md
- docs/architecture.md
- docs/business-rules.md
- docs/database-design.md
- docs/implementation-plan.md
- docs/verification.md
- frontend/src/App.tsx
- frontend/src/auth/roleConfig.ts
- frontend/src/pages/RoleHomePage.tsx
- frontend/tsconfig.json
- frontend/vite.config.ts
- README.md

## Phase 3 — verified 2026-09-23

| Check | Observed result |
| --- | --- |
| Backend dependency restore | Passed |
| Entire backend build | Passed; 0 compiler warnings, 0 errors |
| Full backend suite against PostgreSQL | 51 passed, 0 failed, 0 skipped (28 existing + 23 authentication cases) |
| Frontend TypeScript and production build | Passed |
| Full frontend suite | 21 passed (3 existing + 18 new) |
| npm audit | 0 vulnerabilities |
| New migration | 20260923150723_AuthenticationVersionAndSingleRole generated and applied |
| Initial migration preservation | Both original migration files unchanged |
| Pending model changes | None |
| Local Identity initialization | Seeded twice; exactly 5 roles and 5 demo accounts, one per role |
| Live API demo logins | All 5 returned 200; /me returned matching safe identities |
| Live anonymous access | /api/auth/me returned 401 |
| Live user-management boundary | Administrator 200; coordinator/mechanic/driver/owner each 403 |
| Every role's policy boundary | Integration tests verify own policy allowed, all four other roles denied |
| Test-only route isolation | Live API returned 404 for authenticated access to a test probe |
| Real development reset pickup | Link written privately; reset succeeded; prior JWT rejected; token reuse rejected |

PostgreSQL 16.15 was restarted from the existing local cluster. Tests use isolated databases and the real migrations. Both migrations are recorded in the retained local development database. No Phase 4 functionality was implemented.

### Added coverage

Login success for every role; generic invalid credentials; inactive-account rejection; JWT subject/email/role, expiry, issuer, audience and signature checks; anonymous rejection and cross-role 403s; user create/detail/search/update; stale edits; HTTP audit identity; safe DTOs; driver linking; role and demo-seed idempotency; role changes/deactivation/logout revocation; password reset single-use and session revocation; uniform unavailable production delivery and delivery-failure responses; login lockout/rate limiting; concurrent last-administrator protection.

Frontend tests cover login success/failure, restoring and loading sessions, expired/invalid sessions, protected routes, all role navigation, 403, logout, reset requests, user forms, server errors, loading/retry and confirmation before deactivation. Existing persistence and frontend connection tests remain. The migration-count assertion now expects both migrations and explicitly retains the initial migration check.

Verification API/frontend/PostgreSQL processes were stopped after completion. The isolated live-test database was dropped; the retained development database keeps the applied customer migration. Private credentials and verification logs remain under ignored .tools. No commit or push was performed for Phase 4.

### Files created or modified

- Application: authentication/user DTOs and service contracts, reset-delivery abstraction, policy constants and request errors.
- Infrastructure/Identity: Identity registration, JWT options/issuer/events, authentication/user services, mutation lock, seeders and reset delivery; ApplicationUser adds TokenVersion.
- Persistence: one-role index, user-version check, new migration/designer and current snapshot. Initial migration files unchanged.
- API: auth/users controllers, trusted HTTP audit actor, centralized exception handler, authentication/authorization, throttling and explicit seed commands.
- Frontend: API client, AuthProvider/guards/types/navigation, login/reset/403 pages, five role shells, Users UI, responsive shell and route splitting.
- Tests: PostgreSQL-backed authentication fixture/policy probes and integration cases; frontend auth/user tests; minimal health/migration-test configuration updates.
- Configuration/documentation: .env.example, README, authentication/API/architecture/database/rules/phase-plan documents and this report.

### Security decisions and limits

One role per account; Driver users require an existing unclaimed non-deleted driver. Links cannot change while the driver is on a trip. The last active administrator cannot be deactivated or demoted, including concurrent attempts. No physical account deletion exists.

JWTs last 15 minutes with no refresh flow and zero clock skew. Tab-scoped sessionStorage is intentionally simple but remains exposed to same-origin XSS; see authentication.md. Logout revokes all account sessions when the server is reachable. Profile/access changes require re-login. Development secrets reside only in ignored local files and environment variables.

Reset tokens use Identity, expire in 30 minutes and are not returned by APIs. Development pickup files are private; production returns uniform 503 until a real delivery adapter is installed. Production email integration and controlled first-admin provisioning remain deployment concerns, not public bootstrap features.

Inherited EF required-navigation/query-filter warnings remain documented and tested. Vite's production build reports a roughly 505 kB uncompressed initial chunk (about 159 kB gzip); the administrator page is split into its own chunk. This is a size advisory, not a build/test failure.

### Local demo setup

The five emails and setup commands are documented in README and authentication.md. Passwords and JWT signing keys are generated privately in .tools/phase3-settings.json and never committed. Re-seeding does not overwrite existing passwords. The reset smoke test retained the configured demo password.

Verification API and PostgreSQL processes are stopped after completion; binaries, migrated database and private configuration remain for the next phase. Restart using README instructions.

The historical Phase 2 report below describes the state before authentication and demo accounts were added.
## Phase 2 verification (historical)

Verified on 2026-09-22 using .NET SDK 8.0.425 and portable PostgreSQL 16.15 on Windows. Docker was not installed. The portable distribution came from [EDB's PostgreSQL binaries](https://www.enterprisedb.com/download-postgresql-binaries).

### Observed results

| Check | Result |
| --- | --- |
| dotnet restore | Passed |
| Backend compilation | Passed; zero compiler warnings/errors |
| Backend tests with PostgreSQL enabled | 28 passed, 0 failed, 0 skipped |
| Initial migration generation | Generated InitialPersistence and model snapshot |
| Apply migration to empty PostgreSQL database | Passed |
| Pending model changes | None |
| Idempotent migration SQL | Generated; integration test executes it twice |
| Development seed CLI | Passed twice with unchanged counts |
| Frontend TypeScript/production build | Passed |
| Frontend Vitest suite | 3 passed |

The first database-independent test run explicitly skipped PostgreSQL tests. The final run above enabled them all. Integration tests create/drop a random database and apply the real migration; they never use EnsureCreated or EF InMemory.

Coverage includes normalized registration/licence uniqueness, required foreign keys, decimal cost and nonnegative checks, all three soft-delete filters with retained history, immutable creation audit and UTC updates, restricted history deletion, schedule thresholds, user-driver uniqueness, xmin stale writes from independent contexts, active-trip resource uniqueness, completed/cancelled history compatibility, seed idempotency, production seed rejection, model configuration, and API liveness.

EF emits four expected model-validation warnings about required navigations to filtered master records. These are not compiler warnings. Database-design.md documents the historical-query approach; an integration test proves that deleted master records remain accessible with history. No warning suppression was added.

### Verified fictional seed counts

3 vehicles, 2 drivers, 2 customer companies, 3 service types, 3 orders, 3 trips, 1 driver note, 2 maintenance logs, 4 service schedules. Identity users and roles both remain zero.

### Files added or modified

- Added Domain/Common/AuditableEntity.cs, Domain/Enums/Statuses.cs and ten Domain/Entities classes.
- Added Application/Abstractions/IAuditActor.cs and Application/Access/FleetRoles.cs.
- Added Infrastructure/Identity/ApplicationUser.cs; Persistence context, design-time factory, system audit actor and development seeder; eleven separate entity configurations.
- Added initial migration, generated designer and model snapshot under Infrastructure/Persistence/Migrations.
- Added Infrastructure/DependencyInjection.cs and EF/Npgsql/Identity dependencies in its project file.
- Modified Api/Program.cs to register persistence and support explicit seeding.
- Added Tests/Persistence model tests, database fixture and relational tests.
- Added .config/dotnet-tools.json; updated .env.example, README and architecture/database/phase documentation.
- Frontend source/configuration remains unchanged.

The initial migration is 20260922174736_InitialPersistence. Database constraints and relationships are enumerated in database-design.md.

### Local verification database

The ignored .tools/postgresql directory contains binaries, the local database cluster, logs, generated review SQL and local-settings.json. The latter holds generated local-only credentials: do not commit or share it. The cluster listens only on 127.0.0.1:55432 and uses password authentication. No Windows service was installed.

The verification server is stopped after completion; data is retained. To restart from the repository root:

~~~powershell
& '.\.tools\postgresql\pgsql\bin\pg_ctl.exe' -D '.tools/postgresql/data' -l '.tools/postgresql/server.log' -o '-h 127.0.0.1 -p 55432' -w start
$settings = Get-Content '.tools/postgresql/local-settings.json' -Raw | ConvertFrom-Json
$env:ConnectionStrings__DefaultConnection = $settings.ConnectionString
$env:FMS_TEST_POSTGRES = $settings.TestConnectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
~~~

Use the local SDK and application/test commands in README. Stop when finished:

~~~powershell
& '.\.tools\postgresql\pgsql\bin\pg_ctl.exe' -D '.tools/postgresql/data' -m fast -w stop
~~~

The local account is for development/testing only; application runtime and migration permissions should be separated in deployment.

### Assumptions and deferred work

One trip per order; no reassignment/split loads. Capacity uses kilograms and odometer readings whole kilometres. Costs use one operating currency. Current driver is derived from active trips. Schedule thresholds hold next-service information. Audit actors are strings to retain attribution independently of user lifecycle.

Phase 3: Identity login/reset, JWTs, role authorization, active-user enforcement, trusted HTTP audit actor and fictional user/role seeds. Later phases: state-machine/assignment transactions, active-resource deletion guards, ownership checks, maintenance services, due-service calculation, email/blob adapters, dashboards and exports. Persistence constraints do not substitute for those future business services.
