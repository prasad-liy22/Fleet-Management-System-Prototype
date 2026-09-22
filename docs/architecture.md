# Architecture

Status: Phase 2 persistence implemented. This document distinguishes planned behavior from implemented behavior.

Three runtime tiers: React web client, ASP.NET Core 8 REST API, PostgreSQL. The backend is a modular monolith. Domain contains entities and lifecycle rules; Application contains use cases, DTOs and integration contracts; Infrastructure implements EF Core, Identity, Azure Blob Storage and SendGrid; Api provides composition, authentication, authorization and HTTP endpoints. Dependency direction: Application -> Domain; Infrastructure -> Application; Api -> Application and Infrastructure.

Frontend modules: auth, fleet, customers, orders, trips, driver workflow, maintenance, schedules, notifications, dashboards and reports. Shared API client, layouts and form/table components support these modules. Business decisions remain in API use cases. Protected browser routes improve navigation; they never grant authority.

Security plan: Identity password hashing; short-lived JWTs; server-side role policies and driver ownership checks; active-user checks on authenticated requests; strict CORS origins; secrets from environment/configuration providers. Define token revocation and password-reset behavior in Phase 3. No business endpoints are exposed in Phase 1.

Transactions will persist trip/resource changes and notification outbox records together. EF concurrency tokens and database uniqueness constraints protect assignment; conflicts return HTTP 409. Email is delivered asynchronously from the durable outbox with bounded retry. Cloud file access remains private and authorized by API.

Assumptions: one trip per order in the initial scope; no split loads or reassignments. Assignment reserves resources immediately, regardless of required date. Date-only schedules use UTC calendar dates; event timestamps use UTC instants. Owner access is read-only. No GPS, billing, fuel or inventory features.

Phase 2 adds EF Core/Npgsql, Identity tables (without login), audit contracts, explicit migrations and Development-only seeding. PostgreSQL xmin and active-trip unique indexes prepare concurrency; assignment transactions remain Phase 6. Required historical navigations to filtered master data need deliberate IgnoreQueryFilters queries; see database-design.md.