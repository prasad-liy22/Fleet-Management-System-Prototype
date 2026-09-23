# API overview

Implemented through Phase 3. All responses containing account data use DTOs. EF entities, password hashes and Identity security stamps are never returned.

| Method | Route | Access |
| --- | --- | --- |
| GET | /health | Anonymous process liveness |
| POST | /api/auth/login | Anonymous, rate limited |
| POST | /api/auth/forgot-password | Anonymous, rate limited |
| POST | /api/auth/reset-password | Anonymous, rate limited |
| GET | /api/auth/me | Authenticated |
| POST | /api/auth/logout | Authenticated |
| GET | /api/users | FleetAdministrator; page/pageSize/search/role/isActive filters |
| GET | /api/users/{id} | FleetAdministrator |
| POST | /api/users | FleetAdministrator |
| PUT | /api/users/{id} | FleetAdministrator; optimistic public version required |
| GET | /api/users/driver-options | FleetAdministrator; optional userId/search, maximum 100 |

There is no public registration or physical account-delete endpoint. User updates include active status, role and driver link. Creation returns 201 with Location; logout returns 204; validation/authentication/authorization/missing/conflict errors use 400/401/403/404/409. Throttling returns 429. Unconfigured production reset delivery returns a uniform 503. Errors use Problem Details; paginated users return items/page/pageSize/totalCount.

See authentication.md for request/response contracts, role policies, driver-link rules, session revocation and reset delivery. Health remains independent of database connectivity but the host requires valid security configuration.

Future resource groups remain /api/vehicles, /api/drivers, /api/customers, /api/orders, /api/trips, /api/maintenance, /api/service-types, /api/service-schedules, /api/notifications, /api/reports and /api/dashboard. None are implemented as fake business endpoints. Authorization probes exist only in the test assembly.
