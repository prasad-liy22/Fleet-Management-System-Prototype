# API overview

Implemented: GET /health returns plain-text Healthy when the API process responds. This is liveness only, not database or integration readiness.

Planned resource groups: /api/auth, /api/users, /api/vehicles, /api/drivers, /api/customers, /api/orders, /api/trips, /api/maintenance, /api/service-types, /api/service-schedules, /api/notifications, /api/reports and /api/dashboard.

Controllers accept request DTOs and return response DTOs; EF entities never cross the HTTP boundary. Paginated lists return items, page, pageSize and totalCount. Page size is capped by the server. Use 201 for creation, 204 for successful actions without bodies, 400 for validation, 401 for missing/invalid authentication, 403 for forbidden roles, 404 for unavailable resources and 409 for lifecycle/concurrency conflicts. Errors use Problem Details with validation errors where relevant.

Role mapping: administrator manages users/master records/service types; coordinator manages orders and trip assignment/cancellation/completion; driver reads and operates their own assigned trips; mechanic manages maintenance/schedules and reviews notes; owner reads fleet summaries and reports. Apply object ownership and active-account checks in addition to role policies.
