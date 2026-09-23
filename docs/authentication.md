# Authentication and access — Phase 3

## Identity and tokens

The existing ApplicationUser and Identity tables are reused. Identity manages password hashing, password validation, lockout and reset tokens. There is no duplicate user entity, public registration, social login or refresh-token flow.

Password policy: minimum 12 characters with uppercase, lowercase, digit and symbol. Five failed password attempts lock login for 15 minutes. Public authentication/reset operations are rate limited to 20 requests per minute per direct client IP, configurable through Authentication__RequestsPerMinute. Reverse-proxy forwarding must be restricted to trusted proxies when deployment is configured.

JWT access tokens last 15 minutes by default (configurable 5–60), use HS256, and validate issuer, audience, signature, expiry and algorithm with zero clock skew. Jwt__SigningKey must be at least 64 UTF-8 bytes; generate it cryptographically. Claims are subject, email, role, token version, token ID and standard lifetime/issuer/audience fields. No hashes or security stamps are exposed.

Each authenticated request also verifies the stored account is active, its token version matches, and its single current role matches the token. Driver sessions additionally require a live linked Driver. Logout revokes ALL bearer sessions for that account. Profile/access changes and successful password resets also revoke sessions; account administration rotates the Identity security stamp to invalidate outstanding reset links. An already executing authorized request is not retroactively cancelled.

## Roles and API authorization

Exactly these roles are supported:

| Role | Policy preparation |
| --- | --- |
| FleetAdministrator | ManageUsers, ManageFleet |
| OperationsCoordinator | ManageOperations |
| Mechanic | ManageMaintenance |
| Driver | DriverWorkflow |
| FleetOwner | ReadReports |

Role vocabulary/policies live in Application. Authorization is enforced by API attributes and a fallback policy requiring authentication. Only login/reset endpoints and health are anonymous. All user-management operations require ManageUsers. Future operational modules must apply their corresponding policy AND object-level ownership checks. The Driver identity is resolved from the database, not a caller-supplied driver ID.

Tests add policy probes from the test assembly only. These are not deployed business/demo endpoints.

## Endpoints

| Method and path | Access and purpose |
| --- | --- |
| POST /api/auth/login | Anonymous; email/password, safe identity and expiring access token |
| GET /api/auth/me | Authenticated; current ID/name/email/role/linked driver |
| POST /api/auth/logout | Authenticated; revoke account sessions |
| POST /api/auth/forgot-password | Anonymous; generic acknowledgement |
| POST /api/auth/reset-password | Anonymous; Identity token and new password |
| GET /api/users | Administrator; paginated search, role and active-status filters |
| GET /api/users/{id} | Administrator; safe account details |
| POST /api/users | Administrator; create account with initial password and one role |
| PUT /api/users/{id} | Administrator; profile, role, driver link and active status |
| GET /api/users/driver-options | Administrator; bounded searchable options for linking, not Driver CRUD |

Errors use Problem Details with 400/401/403/404/409 as appropriate; throttling returns 429. Requests and responses do not contain Identity internals. Authenticated subject IDs automatically populate audit actors. Public login/reset persistence uses the named system actor.

## Account administration and driver links

A single transaction covers each create/update, including roles and driver links. An advisory transaction lock serializes account mutations so concurrent requests cannot deactivate/demote every administrator. The last active administrator must remain active with the administrator role. The admin UI confirms role/status/link changes.

Updates require the last returned public version number. A stale form gets 409. This version is a revocation counter, not an Identity security stamp. Create/update responses re-read persisted data so PostgreSQL timestamp precision is consistent with later GETs.

Driver accounts require an existing non-deleted Driver. Other roles may not carry a driver link. A driver already linked to ANY other user, including an inactive user, cannot be taken over. Unlink that user explicitly first. Link changes involving a driver on a trip are refused. Deactivation retains the link. The pre-existing unique Driver.ApplicationUserId index remains unchanged.

The new migration adds ApplicationUser.TokenVersion with a nonnegative check and a unique UserId index on AspNetUserRoles (at most one role per user). Services ensure each account receives exactly one supported role. InitialPersistence is unchanged.

## Password reset

Identity reset tokens expire after 30 minutes and become unusable after a successful reset. Tokens are URL-safe encoded and delivered only through IPasswordResetDelivery. Responses never include them. Unknown and inactive addresses receive the same acknowledgement; delivery failures do not reveal whether the account exists.

Development delivery requires an explicitly configured PRIVATE pickup directory outside anything served by the frontend or API. Each request writes a JSON file containing recipient and resetLink. Open that link in the frontend. Treat these files like temporary passwords; they are not logged and belong under the ignored .tools directory. The browser removes the reset token from the visible route after loading and uses no-referrer policy.

Production has no development-file fallback. Until a real delivery adapter is configured, forgot-password returns the same 503 for every address BEFORE looking up accounts. SendGrid integration remains a later phase. Existing valid reset tokens still use Identity validation. Persist/protect Data Protection keys across deployments if reset links must survive restarts; DataProtection__KeyDirectory configures storage. Restrict key-directory access and configure encryption at rest for production hosting.

## Frontend sessions

AuthProvider stores only the bearer token and expiry in sessionStorage for the current tab and verifies identity through /api/auth/me on reload. API requests attach Authorization: Bearer and omit cookies. Session expiry, invalid tokens and logout clear state; protected content is withheld during verification. Logout clears local state even if its network request fails; in that case a copied token can remain valid until expiry or another server-side revocation.

sessionStorage survives tab reloads but is readable by same-origin JavaScript. It reduces persistence compared with localStorage, but is NOT protection against XSS. Tokens are never logged, rendered or placed in URLs. The application does not render untrusted HTML. Deployment must serve over HTTPS and apply appropriate CSP/security headers. No cookie/refresh-token complexity is introduced.

Five distinct role landing shells provide meaningful navigation. Unimplemented modules are disabled and labelled Planned. The Users page supports list/detail, creation, profile/role/link updates and activation/deactivation. Editing your own account requires signing in again. Full analytics and all Phase 4 fleet CRUD remain deferred.

## Development roles and accounts

Role initialization is idempotent and may run separately with --seed-roles-only. Demo users are permitted only in Development with a configured Seed__Password and explicit --seed-only (or Seed__Enabled=true). The fleet fixtures are seeded first; the demo Driver links to Sample Driver Alpha. Repeated initialization never resets passwords, roles, profiles or active status of existing demo accounts.

| Role | Fictional login email |
| --- | --- |
| FleetAdministrator | admin@fleet.example |
| OperationsCoordinator | operations@fleet.example |
| Mechanic | mechanic@fleet.example |
| Driver | driver@fleet.example |
| FleetOwner | owner@fleet.example |

All newly created demo accounts use the locally configured Seed__Password. There is no committed default password. Production bootstrap account provisioning is a controlled deployment task; no public bootstrap endpoint or production demo account exists.
