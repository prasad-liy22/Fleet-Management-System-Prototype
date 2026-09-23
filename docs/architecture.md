# Architecture

Status: Phases 1–3 implemented and verified. Operational business modules remain planned.

Three runtime tiers: React client, ASP.NET Core 8 REST API and PostgreSQL. The backend is a modular monolith. Domain contains framework-independent entities; Application contains DTOs, contracts and role/policy vocabulary; Infrastructure implements EF Core and Identity services; Api composes HTTP endpoints, error handling, authentication and trusted audit identity.

Dependencies remain Application -> Domain; Infrastructure -> Application; Api -> Application and Infrastructure. The existing ApplicationUser remains in Infrastructure; Driver remains a separate Domain entity with an optional unique user ID.

Phase 3 adds Identity password hashing/lockout/reset, 15-minute JWT access tokens, current-user validation, server-side role policies and administrator account operations. Tokens are checked against active status, current role and a revocation version on every request. A single-role unique membership index and TokenVersion are added by a new migration; InitialPersistence is unchanged.

Transactions and a shared advisory lock protect account/role/link changes and the last active administrator. Identity security stamps protect reset links. Reset delivery has a Development-only private file adapter; no production file fallback or SendGrid integration exists. See authentication.md for complete security decisions.

The frontend uses a lightweight AuthProvider with tab-scoped sessionStorage, identity verification on reload, protected routes and role navigation. Five distinct landing shells display planned modules without business metrics. Administrator user management is implemented; operational pages remain deferred.

Persistence retains automatic UTC auditing, master-data soft deletion, restrictive history relationships and xmin concurrency tokens. HTTP writes use the validated subject as audit actor; unauthenticated/system work uses a named system actor. Required historical navigations to filtered master data require deliberate IgnoreQueryFilters queries.

Future assignment must transact trip/resource/outbox changes together. xmin and active-trip uniqueness already prepare conflict protection; Phase 6 still implements the workflow. SendGrid and Azure Blob adapters, operational dashboards and reports remain later phases.

Assumptions: one trip per order; immediate resource reservation on assignment; whole-kilometre odometers; kilogram capacities; calendar dates interpreted consistently in UTC; owner access read-only. No GPS, billing, fuel, inventory, AI or other out-of-scope features.
