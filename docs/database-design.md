# Database design — Phases 2–3

EF Core 8 and Npgsql persist to PostgreSQL. Entities live in Domain; Identity-specific ApplicationUser lives in Infrastructure. Authentication is implemented in Phase 3; operational workflows remain deferred.

## Entities and relationships

| Entity | Relationships and storage |
| --- | --- |
| ApplicationUser | Identity GUID key, display name, active flag, audit fields; optional one-to-one Driver |
| Vehicle | Unique registration, model/year, numeric(12,2) capacity, bigint odometer, status; many trips/logs/schedules |
| Driver | Unique licence, name/contact/status, optional unique ApplicationUserId; many trips/notes |
| CustomerCompany | Company/contact/address fields; many orders |
| Order | Required customer; locations/date/status/notes; zero or one trip |
| Trip | Required unique order; nullable resources for drafts; readings/UTC event times; many notes |
| DriverNote | Required trip/driver; text, attention/review flags, reviewer/time and audit |
| ServiceType | Unique name, description, active flag; many logs/schedules |
| MaintenanceLog | Required vehicle/type; numeric(18,2) cost, parts, date/odometer, optional private receipt blob name |
| ServiceSchedule | Required vehicle/type; independent nullable due date/odometer; completion time and optional log |
| Notification | Email snapshot, optional user, subject/body, related record, sent time/flag, retry metadata |

Identity tables use GUID keys, unique normalized usernames and unique non-null normalized emails. The five role names are declared in Application and seeded idempotently. A unique AspNetUserRoles.UserId index enforces at most one membership; Identity services assign exactly one supported role. Business foreign keys use RESTRICT. Identity's internal claim/role/token relationships retain standard defaults. Audit actor strings and notification polymorphic references do not have cascading foreign keys.

## Constraints

Registration/licence values are trimmed and uppercased on SaveChanges, bounded and unique including deleted records. Database checks reject noncanonical values. Year is 1900–2100, capacity positive, odometers/cost nonnegative. Enums use readable strings plus check constraints.

One trip per order. Drafts have no resources; assigned/in-progress/completed trips require both. Partial unique indexes on vehicle/driver for Assigned or InProgress prevent duplicate reservations. Checks enforce paired reading/time fields, nondecreasing odometers, valid return chronology and a cancellation reason. They validate row shape, NOT transitions between states.

Schedules require a date or odometer threshold and consistent completion metadata. Review/sent flags require corresponding metadata. Explicit string lengths and status/date/history/queue indexes support later APIs.

Assumptions: capacity is kilograms, odometers whole kilometres, costs use one company operating currency. Current driver is derived from active trips. Next-service thresholds belong to schedules.

## Soft deletion and history

Vehicle, Driver and CustomerCompany have IsDeleted global filters. Remove becomes an audited update, never DELETE. Other audited history rejects physical deletion through SaveChanges. RESTRICT foreign keys prevent deleting referenced records directly.

Bulk ExecuteDelete/ExecuteUpdate and raw SQL bypass auditing/soft deletion; normal workflows must use tracked writes. Business eligibility checks for deleting active resources belong to Phase 4.

Required navigations to filtered master data can omit historical rows in Include/joins. Authorized history/report queries must deliberately use IgnoreQueryFilters() and project historical DTOs. Integration tests cover preserved history. This explains EF's required-navigation/query-filter warnings; weakening foreign keys or filtering history is not a fix.

## Auditing

All eleven entities implement IAuditable. Sync/async SaveChanges set trusted CreatedBy/CreatedAt on insert and UpdatedBy/UpdatedAt on every write. Creation metadata is immutable in storage. Injectable TimeProvider supplies UTC timestamps. IAuditActor uses the validated HTTP subject for authenticated operations, otherwise system. Never accept audit identity from DTOs.

Events use UTC DateTimeOffset/timestamp with time zone. Calendar dates use DateOnly/date. Seed IDs/business dates are fixed; audit times record actual execution.

## Concurrency

Vehicle, Driver, Order and Trip use uint Version mapped by IsRowVersion to PostgreSQL xmin. Stale writes throw DbUpdateConcurrencyException. No manual version increment or SQL Server rowversion is used. Independent-context tests exercise stale resource updates. Active-trip unique indexes provide additional protection; transactional assignment and HTTP 409 mapping remain Phase 6.

## Seeds

Development plus explicit --seed-only or Seed:Enabled=true is required, with migrations already applied. Seeding uses a transaction and advisory lock, inserts missing fixed IDs, preserves edits/deletion state and never creates production data.

Fixtures: 3 vehicles, 2 drivers, 2 companies, 3 service types, 3 orders, 3 trips (draft/assigned/completed), 1 note, 2 maintenance logs and 4 schedules (completed/date-only/odometer-only/both). All names, identifiers and contacts are fictional; emails use reserved example domains. The fleet fixture seeder creates no accounts or notifications. The separate Phase 3 IdentitySeeder adds five roles and five Development-only accounts using a private configured password; no real cloud files are seeded.

## Verification strategy

Real PostgreSQL tests apply migrations, check constraints, soft deletion, auditing, xmin, seeding and an idempotent migration script. No InMemory/SQLite substitute is used. FMS_TEST_POSTGRES needs CREATE DATABASE: tests create/drop only a random fms_test_* database. Without it relational tests are explicitly skipped. See README commands and verification.md results.


## Phase 3 migration

20260923150723_AuthenticationVersionAndSingleRole adds a nonnegative TokenVersion integer to AspNetUsers and the unique single-role membership index. Existing Identity tables and the initial migration are preserved. Account updates/logout/password reset increment TokenVersion to revoke existing JWTs. Driver linking uses the existing unique nullable ApplicationUserId foreign key. Account mutation transactions and an advisory lock protect the last active administrator and concurrent linking.

Identity update concurrency stamps remain internal. Administrator DTOs expose a public version counter for stale-form detection. New account/update responses re-read persisted records to match PostgreSQL timestamp precision.