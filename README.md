# Fleet Management System

University industry-based project for heavy-vehicle operations, using React/TypeScript/Vite/MUI/Tailwind, ASP.NET Core 8, Identity and PostgreSQL.

## Current scope

Phases 1–2 provide the application foundation, EF entities/migrations, auditing, soft deletion, concurrency preparation and fictional fleet fixtures. Phase 3 adds Identity/JWT login, password reset, five-role authorization, account administration, driver linking and authenticated role-specific frontend shells.

Vehicle/driver/customer CRUD, operational workflows, analytics, reports, SendGrid and Azure Blob integration remain future phases. See docs/verification.md for executed checks and docs/authentication.md for security decisions.

## Prerequisites

.NET 8 SDK (8.0.400 or later 8.0 feature band), Node.js 22 LTS, npm and PostgreSQL 16+. Use npm.cmd if Windows PowerShell blocks npm.ps1.

The existing workspace has an ignored local SDK. Enable it in PowerShell from the repository root:

~~~powershell
$env:DOTNET_ROOT = Join-Path (Get-Location) '.tools\dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
~~~

## Configuration

Use environment variables; ASP.NET Core does not automatically load .env files. .env.example lists names without secrets.

Required backend settings:
- ConnectionStrings__DefaultConnection: PostgreSQL connection.
- Jwt__SigningKey: cryptographically random secret, at least 64 UTF-8 bytes.
- Jwt__Issuer / Jwt__Audience: defaults FleetManagement / FleetManagement.Web.
- PasswordReset__PublicBaseUrl: frontend origin; HTTPS in production.
- Seed__Password: strong private development-only seed password when creating demo users.
- PasswordReset__DevelopmentPickupDirectory: private local folder for development reset links.

Set ASPNETCORE_ENVIRONMENT=Development locally. In deployment configure AllowedHosts, Cors__AllowedOrigins__0, HTTPS, protected persistent Data Protection keys and a real password-reset delivery provider. The production forgot-password endpoint returns a uniform 503 until that provider exists. Never put secrets in frontend VITE_ variables.

## Existing local workspace setup

Local PostgreSQL binaries/data are under .tools/postgresql. Start the retained cluster:

~~~powershell
& '.\.tools\postgresql\pgsql\bin\pg_ctl.exe' -D '.tools/postgresql/data' -l '.tools/postgresql/server.log' -o '-h 127.0.0.1 -p 55432' -w start
$database = Get-Content '.tools/postgresql/local-settings.json' -Raw | ConvertFrom-Json
$auth = Get-Content '.tools/phase3-settings.json' -Raw | ConvertFrom-Json
$env:ConnectionStrings__DefaultConnection = $database.ConnectionString
$env:FMS_TEST_POSTGRES = $database.TestConnectionString
$env:Jwt__SigningKey = $auth.SigningKey
$env:Seed__Password = $auth.SeedPassword
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:PasswordReset__DevelopmentPickupDirectory = Join-Path (Get-Location) '.tools/password-resets'
~~~

The two local-settings files are ignored by Git and contain private generated credentials. Open phase3-settings.json locally to obtain SeedPassword for demo login; do not copy it into source control or shared logs. Re-running seeding preserves existing passwords; use password reset if an account's password was changed.

## Fresh checkout setup

Install/start PostgreSQL and create a dedicated development database/user (or give the migration user CREATE DATABASE). Set ConnectionStrings__DefaultConnection with your private credentials.

Generate the signing secret in your terminal:

~~~powershell
$jwtBytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtBytes)
$rng.Dispose()
$env:Jwt__SigningKey = [Convert]::ToBase64String($jwtBytes)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
~~~

Set Seed__Password privately to a password of at least 12 characters with uppercase, lowercase, a digit and symbol. Configure PasswordReset__DevelopmentPickupDirectory to an absolute PRIVATE folder, preferably beneath .tools. Save secrets only in a private local configuration mechanism if they must survive terminal restarts.

## Build, migrate and initialize

~~~sh
dotnet restore backend/FleetManagement.sln
dotnet tool restore
dotnet build backend/FleetManagement.sln
dotnet ef database update --project backend/FleetManagement.Infrastructure
dotnet ef migrations has-pending-model-changes --project backend/FleetManagement.Infrastructure
dotnet run --project backend/FleetManagement.Api --no-launch-profile -- --seed-only
dotnet run --project backend/FleetManagement.Api --launch-profile http
~~~

Both InitialPersistence and AuthenticationVersionAndSingleRole must be applied. Startup never auto-migrates. --seed-only seeds fictional fleet data, the five roles and development users, then exits. --seed-roles-only initializes roles without demo accounts. Do not enable development fixtures in production.

## Frontend and demo login

~~~sh
cd frontend
npm ci
npm run dev
~~~

Open http://localhost:5173. The API runs at http://localhost:5080 through the existing development proxy.

| Role | Email |
| --- | --- |
| Fleet Administrator | admin@fleet.example |
| Operations Coordinator | operations@fleet.example |
| Mechanic | mechanic@fleet.example |
| Driver | driver@fleet.example |
| Fleet Owner | owner@fleet.example |

Password: the private Seed__Password used when these accounts were first created. No password is committed. Fleet Administrator can manage accounts from Users.

For a local password reset, request it through Forgot password, open the new JSON file in the configured pickup folder, then open its resetLink. Tokens expire in 30 minutes and are single-use. The pickup folder is never served by the application.

## Verification

~~~sh
dotnet test backend/FleetManagement.sln
cd frontend
npm run build
npm test
npm audit
~~~

Set FMS_TEST_POSTGRES to an administrative PostgreSQL connection with CREATE DATABASE for all persistence/authentication integration tests. Tests create/drop their own random databases; without this setting relational tests are explicitly skipped. Existing Phase 2 tests remain, and Phase 3 tests exercise real HTTP authentication/authorization against PostgreSQL.

## Migration maintenance

~~~sh
dotnet ef migrations add MeaningfulChangeName --project backend/FleetManagement.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project backend/FleetManagement.Infrastructure --output migration.sql
dotnet ef database update --project backend/FleetManagement.Infrastructure
~~~

Review generated migrations; never edit already-applied migrations or replace migrations with EnsureCreated.

## Structure

- frontend/src/auth, api, pages, layouts, features/users: session handling, authentication UI and user administration.
- backend/FleetManagement.Api: HTTP controllers, policies, errors and trusted HTTP audit identity.
- backend/FleetManagement.Application: contracts, DTOs and role/policy vocabulary.
- backend/FleetManagement.Domain: framework-independent entities/enums.
- backend/FleetManagement.Infrastructure: Identity services, EF persistence, migrations and seeders.
- backend/FleetManagement.Tests: API, model, PostgreSQL and authentication tests.
- docs: architecture, database design, authentication, API overview, rules, phase plan and verification.

Docker Compose and GitHub Actions remain Phase 12 deliverables. Repository: [Fleet Management System Prototype](https://github.com/prasad-liy22/Fleet-Management-System-Prototype).
