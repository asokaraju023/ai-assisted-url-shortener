# AI-Assisted Software Engineering — URL Shortener

## 1. Purpose

This prototype demonstrates engineer-led AI-assisted development for the mandatory requirement:

> Build a scalable URL shortener service with APIs, persistence, and analytics.

The implementation demonstrates requirement decomposition, API design, persistence, redirect handling, analytics, validation, automated tests, security considerations, scalability trade-offs, and documentation.

The assignment principle is: **AI assists the engineer within tasks; the engineer owns execution and quality.**

## 2. Technology choice

- .NET 8 / ASP.NET Core Minimal API
- Entity Framework Core 8
- SQLite for the runnable prototype
- xUnit + ASP.NET Core integration testing
- Swagger/OpenAPI
- Docker support

SQLite is intentionally used to keep the prototype self-contained. A production deployment should use a managed relational database such as PostgreSQL or SQL Server, with a cache such as Redis where traffic warrants it.

## 3. Requirements and assumptions

### Functional
1. Accept a valid absolute HTTP/HTTPS URL.
2. Generate a short, URL-safe code.
3. Persist the mapping.
4. Return the generated short URL.
5. Redirect a short code to the original URL.
6. Record click count and last access time.
7. Expose basic analytics.
8. Return appropriate 4xx responses for invalid or unknown inputs.

### Assumptions
- No authentication is required for this assignment prototype.
- No custom aliases are required.
- No expiry is required.
- Analytics are aggregate only; no IP/device tracking is stored.
- HTTP 302 is used for redirects because destination permanence was not specified.
- Short codes are generated server-side.

### Ambiguities to discuss with the client
- Expected peak requests/sec and redirect/read/write ratio.
- Required availability and recovery objectives.
- Code lifetime and collision policy.
- Authentication/authorization requirements.
- URL expiry/custom aliases.
- Analytics granularity and retention/privacy requirements.
- Abuse controls, quotas and rate limits.
- Multi-region requirements.

## 4. API contract

### POST /api/urls
Request:
```json
{"url":"https://example.com/products/123"}
```
Response `201 Created`:
```json
{
  "shortCode":"aB7xP2q",
  "originalUrl":"https://example.com/products/123",
  "shortUrl":"http://localhost:5000/r/aB7xP2q",
  "createdAtUtc":"2026-10-02T00:00:00Z"
}
```

### GET /r/{shortCode}
Returns HTTP 302 to the original URL and records a click.

### GET /api/urls/{shortCode}/analytics
Response `200 OK`:
```json
{
  "shortCode":"aB7xP2q",
  "originalUrl":"https://example.com/products/123",
  "clickCount":3,
  "createdAtUtc":"2026-10-02T00:00:00Z",
  "lastAccessedAtUtc":"2026-10-02T01:00:00Z"
}
```

### GET /health
Returns service health.

## 5. Requirement-to-engineering prototype endpoint

The prototype also accepts a requirement and returns structured engineering outputs, which demonstrates the engineer-led decomposition expected by the assignment.

### POST /api/requirements/analyze
Request:
```json
{"requirement":"Build a scalable URL shortener service with APIs, persistence, and analytics."}
```

The response contains:
- engineering problem
- assumptions
- ambiguities
- ordered tasks
- AI assistance opportunities
- validation checks
- risks

This endpoint intentionally does **not** pretend that a deterministic rules engine is an LLM. In a production implementation, an AI provider can sit behind the same analysis contract; the engineer would still validate the generated plan before execution.

## 5. Architecture

```text
Client
  |
  v
ASP.NET Core API
  |
  +--> URL validation
  |
  +--> URL Shortener Service
          |
          +--> EF Core
                 |
                 +--> SQLite (prototype)

Redirect path:
Client -> GET /r/{code} -> DB lookup -> increment analytics -> 302 redirect

Analytics path:
Client -> GET /api/urls/{code}/analytics -> DB lookup -> aggregate metrics
```

### Production evolution
```text
Load Balancer
      |
  API instances (stateless)
      |
      +---- Redis cache ---- PostgreSQL/SQL Server
      |
      +---- OpenTelemetry -> metrics/logs/traces platform
```

The redirect path is read-heavy and latency-sensitive, so a production design can cache code-to-destination mappings. Writes remain durable in the relational store. Analytics can be moved to an asynchronous event stream if high-volume click recording becomes a bottleneck.

## 6. AI-assisted engineering record

The AI was used as a development accelerator, not as an authority.

### Task: API design
Example prompt:
> Design a minimal REST API for a URL shortener supporting URL creation, redirect, analytics and health. Include status codes and validation rules. Do not add authentication or features not required by the assignment.

Engineer validation:
- Compared proposed endpoints against the assignment.
- Removed unnecessary endpoints.
- Confirmed 201/302/404/400 semantics.

### Task: implementation
Example prompt:
> Generate an ASP.NET Core 8 service that validates HTTP/HTTPS URLs, generates a cryptographically strong 7-character code, persists it using EF Core and exposes creation, redirect and analytics operations. Explain failure cases.

Engineer validation:
- Reviewed URL scheme validation.
- Reviewed code-generation entropy and collision handling.
- Added a unique DB constraint rather than relying only on application logic.
- Reviewed exception-to-HTTP mapping.

### Task: testing
Example prompt:
> Generate unit and integration tests for URL creation, invalid URL rejection, redirect of an existing code, 404 for an unknown code, analytics click counting and health endpoint. Include boundary cases.

Engineer validation:
- Checked that tests assert behavior rather than implementation details.
- Added persistence-backed integration coverage.
- Considered concurrency and duplicate-code scenarios.

### Task: review/refactoring
Example prompt:
> Review this URL shortener implementation for security, reliability, scalability, concurrency and maintainability. Identify concrete issues and propose changes without introducing unnecessary complexity.

Engineer action:
- Accepted scheme validation and unique index.
- Identified SQLite as a prototype limitation.
- Identified need for rate limiting/authentication in a production public deployment.
- Identified analytics as a potential write bottleneck at scale.

## 7. Validation strategy

### Automated
- Unit tests for service behavior.
- Integration tests through the HTTP API.
- Persistence verification.
- Redirect verification.
- Analytics verification.
- Health endpoint verification.

### Manual
- Swagger request/response checks.
- Invalid URL tests.
- Unknown short-code tests.
- Repeated redirect tests.
- Review generated code for security and maintainability.

### Quality gates
A change is accepted only when:
1. Build succeeds.
2. Automated tests pass.
3. API contract remains consistent.
4. No known security regression is introduced.
5. Failure behavior is understood.
6. AI-generated changes have been reviewed by the engineer.

## 8. Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Short-code collision | Failed/duplicate mapping | Unique DB constraint + collision retry |
| Database becomes bottleneck | Latency/availability | Production DB, indexes, cache, async analytics |
| Abuse/spam URL creation | Resource exhaustion | Authentication, quotas, rate limiting, abuse controls |
| Malicious destination URLs | User safety/reputation | Abuse detection, allow/deny policies where required |
| Analytics write contention | Redirect latency | Async event processing at scale |
| SQLite single-node limitation | HA limitation | Replace with managed multi-instance DB |
| AI-generated incorrect code | Functional/security defects | Human review, tests, static analysis, threat review |
| Requirements ambiguity | Wrong product behavior | Explicit assumptions and client clarification |

## 9. Greenfield scenario

Requirement: “Build a scalable URL shortener service with APIs, persistence, and analytics.”

Breakdown:
1. Clarify functional/non-functional requirements.
2. Define API contract.
3. Define data model and indexes.
4. Implement URL validation and code generation.
5. Implement persistence.
6. Implement redirect path.
7. Implement analytics.
8. Add tests.
9. Validate security/performance.
10. Document deployment and trade-offs.

AI assists with design alternatives, code scaffolding, tests and review; the engineer decides what is accepted.

## 10. Brownfield scenario

Existing behavior: the redirect endpoint exists, but analytics are missing and the service has duplicated validation logic.

Tasks:
1. Inspect existing code and tests.
2. Establish current behavior as a regression baseline.
3. Extract URL validation into one service boundary.
4. Add click-count persistence.
5. Add analytics endpoint.
6. Add regression and integration tests.
7. Review performance impact of analytics writes.

AI can suggest a refactoring and tests, but the engineer verifies that existing redirect behavior is preserved.

## 11. Ambiguous scenario

Requirement: “Make the URL shortener scalable and add analytics.”

Clarifications required before implementation:
- What is “scalable”: requests/sec, data volume, users, or regions?
- What availability target is required?
- What analytics are needed?
- What retention period is required?
- Is analytics synchronous or eventually consistent acceptable?
- Is authentication required?

If answers are unavailable, document assumptions and implement a prototype with explicit limitations rather than silently guessing.

## 12. Example requirement-analysis call

```powershell
curl -X POST http://localhost:5000/api/requirements/analyze `
  -H "Content-Type: application/json" `
  -d '{"requirement":"Build a scalable URL shortener service with APIs, persistence, and analytics."}'
```

The output is the structured engineering plan used to drive implementation and validation.

## 13. Setup

Prerequisites:
- .NET 8 SDK

Commands:
```powershell
dotnet restore
dotnet build
dotnet test
cd UrlShortener.Api
dotnet run
```

Swagger: `http://localhost:5000/swagger` (actual port may be shown by `dotnet run`).

Create a URL:
```powershell
curl -X POST http://localhost:5000/api/urls `
  -H "Content-Type: application/json" `
  -d '{"url":"https://example.com"}'
```

Redirect:
```text
http://localhost:5000/r/{shortCode}
```

Analytics:
```text
http://localhost:5000/api/urls/{shortCode}/analytics
```

## 14. Client discussion points

Be prepared to explain:
- Why Minimal API and SQLite were selected for a prototype.
- Why a unique DB constraint is necessary even with collision checking.
- Why the redirect path is read-heavy and cacheable.
- What changes are needed for production scale.
- How analytics can become asynchronous.
- What AI generated versus what the engineer reviewed and changed.
- How tests protect against incorrect AI output.
- What assumptions require client confirmation.

## 15. Client discussion Q&A

### Why SQLite?
For a self-contained assignment prototype it removes external infrastructure. It is not the production scaling choice.

### How would you scale it?
Run stateless API instances behind a load balancer, move persistence to a managed relational database, add Redis caching for hot redirects, and move high-volume analytics to asynchronous processing/event streaming.

### Why seven characters?
It provides a large code space for a prototype while keeping URLs short. The exact length should be selected from expected cardinality, collision tolerance and encoding requirements.

### What happens if two codes collide?
The database has a unique constraint and the service retries code generation. The database remains the final source of truth.

### Is the collision check alone sufficient?
No. Two concurrent requests can both observe “not exists.” The unique database constraint closes that race; the service handles the resulting insert failure.

### Why HTTP 302?
The requirement does not specify permanent redirects. A temporary redirect is a conservative prototype choice and can be changed after product semantics are confirmed.

### What is the biggest production bottleneck?
The redirect path is read-heavy, while synchronous analytics updates add a write to every redirect. At scale, caching and asynchronous analytics would reduce pressure on the primary database.

### What security concerns did you consider?
Only HTTP and HTTPS destination URLs are accepted. Short codes are generated using a cryptographically secure random source, and database uniqueness is enforced to handle collisions. The current prototype does not yet implement authentication, quotas, or rate limiting. A public production deployment should add these safeguards along with abuse detection, monitoring, and operational controls.

### What did AI actually do?
AI was used for candidate design, code scaffolding, test generation, review and documentation. The engineer selected the design, reviewed generated code, added/changed safeguards, and defined acceptance criteria. This is the evidence the assignment is asking for.

### What would you ask the client before productionizing it?
Ask for traffic targets, availability/SLOs, RTO/RPO, analytics requirements, data retention/privacy, authentication, abuse policy, URL expiry, custom aliases and regional deployment expectations.

## 16. Known limitations

This is an assignment prototype, not a complete internet-scale production service. It does not implement authentication, distributed caching, multi-region failover, advanced abuse detection, custom aliases, expiry, or detailed privacy-aware analytics. Those are deliberate scope boundaries and should be discussed as production evolution items.
