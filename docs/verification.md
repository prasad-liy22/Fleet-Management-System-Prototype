# Verification history

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
