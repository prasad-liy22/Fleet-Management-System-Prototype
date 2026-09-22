# Implementation plan

1. Foundation (implemented): project boundaries, API bootstrap, responsive web shell and smoke tests.
2. Persistence (implemented; results in verification.md): entities, PostgreSQL/EF Core, constraints, migrations and development seeds.
3. Access: Identity, JWT, reset, active users, profiles and server policies.
4. Master data: vehicles, drivers, customers, audited soft deletion.
5. Orders: draft editing, validated CRUD, search/filter/pagination.
6. Dispatch: transactional assignment, state machine and concurrent assignment tests.
7. Driver: ownership, start/end odometers, notes, completion and cancellation.
8. Maintenance: logs, receipts, schedule completion, due calculations and note review.
9. Notifications: durable outbox, SendGrid delivery and idempotent due-service alerts.
10. Dashboards: five role-specific API projections and responsive views.
11. Reports: filtered/paginated reports and XLSX exports with private blob storage.
12. Delivery: integration/security tests, Docker Compose, GitHub Actions and complete setup docs.

After each phase, build both applications and run relevant tests. Record environmental blockers explicitly. Do not represent planned features as implemented. PostgreSQL-backed concurrency tests are required before dispatch is considered complete.

Phase 2 defers account/role seeds, login, authorization and HTTP actor resolution to Phase 3. No business workflows or frontend features were added.