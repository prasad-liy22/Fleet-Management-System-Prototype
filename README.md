# Fleet Management System

University industry-based project for heavy-vehicle fleet operations: React, ASP.NET Core 8 and PostgreSQL.

## Status

Phases 1–2 implement the application shell, API liveness, entities, EF configuration, audit/soft deletion, concurrency preparation, initial migration and fictional development seeds. Authentication, business workflows, dashboards, reports, cloud integrations, Docker and CI remain later phases. No demo login exists.

## Prerequisites

.NET 8 SDK (8.0.400 or later 8.0 feature band), Node.js 22 LTS, npm and PostgreSQL 16+. Use npm.cmd if Windows PowerShell blocks npm.ps1. Docker is optional.

This workspace includes an ignored local .NET 8.0.425 SDK. Enable it in PowerShell from the repository root:

~~~powershell
$env:DOTNET_ROOT = Join-Path (Get-Location) '.tools\dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
~~~

## Database and backend setup

Start PostgreSQL and use a dedicated development database/user. Replace placeholders; never commit credentials:

~~~powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=fleet_management;Username=<development-user>;Password=<development-password>'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
~~~

EF can create the named database if the user has CREATE DATABASE rights; otherwise pre-create it and grant schema permissions. Production runtime users should not have database-creation rights.

~~~sh
dotnet restore backend/FleetManagement.sln
dotnet tool restore
dotnet build backend/FleetManagement.sln
dotnet ef database update --project backend/FleetManagement.Infrastructure
dotnet ef migrations has-pending-model-changes --project backend/FleetManagement.Infrastructure
dotnet run --project backend/FleetManagement.Api --no-launch-profile -- --seed-only
dotnet run --project backend/FleetManagement.Api --launch-profile http
~~~

The seeder requires Development and applied migrations. --seed-only seeds then exits; Seed__Enabled=true alternatively opts into startup seeding. It is additive and repeatable. Account/role seeding is deferred to Phase 3.

GET /health remains process liveness independent of the database. Persistence requires connection configuration when resolved. Startup never auto-migrates.

## Migration maintenance

~~~sh
dotnet ef migrations add MeaningfulChangeName --project backend/FleetManagement.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project backend/FleetManagement.Infrastructure --output migration.sql
dotnet ef database update --project backend/FleetManagement.Infrastructure
~~~

Review migrations before applying to shared databases. Do not replace migrations with EnsureCreated.

## Frontend

~~~sh
cd frontend
npm ci
npm run dev
~~~

Open http://localhost:5173. Vite proxies /health and /api to http://localhost:5080. Production requires HTTPS and an explicit API origin or reverse proxy.

## Tests

~~~sh
dotnet test backend/FleetManagement.sln
cd frontend
npm run typecheck
npm run build
npm test
~~~

For relational verification, set an administrative test connection first:

~~~powershell
$env:FMS_TEST_POSTGRES = 'Host=localhost;Port=5432;Database=postgres;Username=<test-user>;Password=<test-password>'
dotnet test backend/FleetManagement.sln
~~~

Tests create/drop only a random fms_test_* database and never reset the supplied database. CREATE DATABASE is required. Without FMS_TEST_POSTGRES, PostgreSQL cases are explicitly skipped; model/API tests still run. See docs/verification.md for observed results.

## Configuration, data and structure

.env.example lists environment names; ASP.NET Core does not load .env files automatically. Frontend .env.example contains only a public API origin. Never put secrets in VITE_ variables. JWT/SendGrid/Azure configuration is reserved for later phases.

Fictional seed data includes vehicles, drivers, customers, service types, orders/trips, maintenance, notes and schedules. No real contacts or credentials are used. See docs/database-design.md for relationships, constraints, history-query caveats, auditing, concurrency and assumptions.

- frontend/src: API access, layouts, pages and tests.
- backend/FleetManagement.Api: HTTP and composition root.
- backend/FleetManagement.Application: audit contract and role vocabulary.
- backend/FleetManagement.Domain: framework-independent entities/enums.
- backend/FleetManagement.Infrastructure: Identity user, EF mappings/context, migrations, seeder.
- backend/FleetManagement.Tests: xUnit API, model and PostgreSQL tests.
- docs: architecture, schema, API overview, rules, verification and phase plan.
- .config/dotnet-tools.json: pinned EF CLI.

Docker Compose and GitHub Actions remain Phase 12 deliverables. Source repository: [Fleet Management System Prototype](https://github.com/prasad-liy22/Fleet-Management-System-Prototype).
