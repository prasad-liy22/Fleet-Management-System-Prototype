# Phase 2 verification

Verified on 2026-09-22 using .NET SDK 8.0.425 and portable PostgreSQL 16.15 on Windows. Docker was not installed. The portable distribution came from [EDB's PostgreSQL binaries](https://www.enterprisedb.com/download-postgresql-binaries).

## Observed results

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

## Verified fictional seed counts

3 vehicles, 2 drivers, 2 customer companies, 3 service types, 3 orders, 3 trips, 1 driver note, 2 maintenance logs, 4 service schedules. Identity users and roles both remain zero.

## Files added or modified

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

## Local verification database

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

## Assumptions and deferred work

One trip per order; no reassignment/split loads. Capacity uses kilograms and odometer readings whole kilometres. Costs use one operating currency. Current driver is derived from active trips. Schedule thresholds hold next-service information. Audit actors are strings to retain attribution independently of user lifecycle.

Phase 3: Identity login/reset, JWTs, role authorization, active-user enforcement, trusted HTTP audit actor and fictional user/role seeds. Later phases: state-machine/assignment transactions, active-resource deletion guards, ownership checks, maintenance services, due-service calculation, email/blob adapters, dashboards and exports. Persistence constraints do not substitute for those future business services.
