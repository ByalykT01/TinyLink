# TinyLink

.NET 10 ASP.NET Core URL shortener API with PostgreSQL.

## Commands

```bash
dotnet build                          # build solution
dotnet test                           # run tests (unit + integration; integration needs Docker)
dotnet run --project TinyLink.Api     # run API (launches on http://localhost:5292)
dotnet watch run --project TinyLink.Api  # hot-reload
```

## Architecture

- **`TinyLink.Api/`** — Web API project (net10.0)
  - `Program.cs` — registration order: ForwardedHeaders → Documentation → ErrorHandling → Options → Persistence → Services → RateLimiter; middleware order: ForwardedHeaders → ErrorHandling → Scalar (dev) → RateLimiter; calls `MigrateDatabaseAsync` at startup
  - `Features/Links/` — vertical-slice feature folder for URL shortening
    - `LinkEndpoints.cs` — maps `POST /api/links` (create), `DELETE /api/links/{code:length(7)}` (delete with Bearer token), and `GET /{code:length(7)}` (redirect)
    - `CreateLink.cs` — validates URL + optional `expiresAt`, gets next id/code from `ShortCodeAllocator`, defaults expiry to +7 days, returns 201 Created with `Location` header and `deleteToken`
    - `DeleteLink.cs` — validates Bearer token against stored `DeleteTokenHash`, soft-deletes link (sets `DeletedAt`), invalidates cache; returns 204 or 404
    - `RedirectToTarget.cs` — resolves code via `LinkResolver` → 302 Redirect, 410 Gone when expired/deleted, 404 otherwise; sets `Cache-Control` (`no-store` for live/404, `public, max-age=86400` for 410)
    - `LinkResolver.cs` — `HybridCache`-backed resolution (30s TTL); returns `LinkResolution` record
    - `LinkResolution.cs` — record: `Exists`, `TargetUrl`, `ExpiresAt`, `DeletedAt`; static `NotFound`
    - `UrlPolicy.cs` — `TryNormalize`: absolute http/https only, no embedded credentials, max 2000 chars
    - `DeleteToken.cs` — generates 32-byte URL-safe token, stores SHA-256 hash; constant-time verification
    - `DeletedLinkCleanup.cs` — deletes links where `DeletedAt` or `ExpiresAt` older than retention window
    - `DeletedLinkCleanupWorker.cs` — `BackgroundService` running cleanup on interval (default 1h)
  - `ShortCodes/` — short-code pipeline
    - `Base62.cs` — Base62 encode/decode: 7-char codes, `Domain = 62^7 = 3_521_614_606_208`
    - `Cipher.cs` — keyed Feistel permutation (HMAC-SHA256 round function, 10 rounds, 21-bit halves) to make sequential ids look random; key from `ShortCodes:Key` config (base64)
  - `Data/ShortCodeAllocator.cs` — pulls next `link_code_req` sequence value (`nextval` via raw SQL), permutes with `Cipher`, returns `(Id, Code)`
  - `Data/ApplicationDbContext.cs` — EF Core context; `Id` not DB-generated, unique index on `ShortCode` (7), `TargetUrl` maxed at `UrlPolicy.MaxLength` with URI↔string conversion, indexes on `DeletedAt`/`ExpiresAt` (filtered), defines `link_code_req` sequence (id range `[1, Domain - 1]`)
  - `Models/Link.cs` — `Id`, `ShortCode`, `TargetUrl`, `DeleteTokenHash` (bytea), `CreatedAt`, `ExpiresAt`, `DeletedAt`
  - `Options/DatabaseOptions.cs` — config section `"Database"`, DataAnnotations-validated, connection string via `ToConnectionString`
  - `Options/LinkCleanupOptions.cs` — config section `"LinkCleanup"`, `Interval` (default 1h), `Retention` (default 7d)
  - `Extensions/` — C# extension members (`extension(IHostApplicationBuilder)` syntax), each adds one concern
    - `ServicesExtensions.cs` — registers `TimeProvider.System`, `Cipher` (singleton), `UrlPolicy` (singleton), `ShortCodeAllocator` (scoped), `HybridCache`, `LinkResolver` (singleton), `DeletedLinkCleanup` (singleton), `DeletedLinkCleanupWorker` (hosted); throws if `ShortCodes:Key` not configured
    - `RateLimiterExtensions.cs` — token-bucket limiter (`CreateLinkPolicy`), partitioned by client IP (IPv6 /64): **configurable burst/per-minute** (defaults 20/20), `QueueLimit = 0`, rejection status **429** with `Retry-After` and ProblemDetails
    - `ErrorHandlingExtensions.cs` — `AddProblemDetails` with traceId/exception detail in dev; exception handlers: `ClientDisconnectedExceptionHandler` (narrow), `DatabaseExceptionHandler` (broad)
    - `ObservabilityExtensions.cs` — OpenTelemetry tracing (AspNetCore, HttpClient, Npgsql, custom source) + metrics, OTLP exporter
    - `ForwardedHeadersExtensions.cs` — configures forwarded headers for reverse proxy support
- **`TinyLink.Tests/`** — xUnit + FluentAssertions + NSubstitute test project
  - `Unit/ShortCodes/Base62Tests.cs` — encode/decode round-trips and invalid input
  - `Unit/ShortCodes/CipherTests.cs` — injectivity, determinism, domain bounds, distinct keys, key validation
  - `Unit/Features/Links/UrlPolicyTests.cs` — normalization + rejection cases
  - `Unit/Features/Links/DeleteTokenTests.cs` — generation, matching, constant-time verification, malformed input
  - `Unit/Options/LinkCleanupOptionsTests.cs` — defaults + startup validation via real `AddOptions` registration
  - `Unit/Extensions/ExceptionHandlerTests.cs` — client-disconnect (499) and database (409/503/false) classification
  - `Integration/Data/DeletedLinkCleanupTests.cs` — cutoff boundaries (older/at/newer), active kept, expired removed, second run zero, concurrent runs
  - `Integration/Data/ShortCodeAllocatorTests.cs` — distinct sequence-derived values matching `Base62(Cipher.Permute(id))`
  - `Integration/Data/ApplicationDbContextTests.cs` — Testcontainers PostgreSQL (`postgres:17-alpine`) via `PostgresFixture`/`PostgresCollection`: property round-trip, duplicate short code rejection, required/max-length `TargetUrl`, UTC timestamps, non-UTC offset rejection
  - `Integration/Api/ApiFixture.cs` — `WebApplicationFactory<Program>` + Testcontainers PostgreSQL, `HttpClient` with `AllowAutoRedirect = false`, helpers for rate-limit overrides and DB context access
  - `Integration/Api/CreateLinkEndpointTests.cs` — create link, validation, explicit UTC expiry, past-expiry 400, rate limiting
  - `Integration/Api/DeleteLinkEndpointTests.cs` — delete with valid/invalid/malformed/missing token, expired-link delete, concurrent deletes, idempotency
  - `Integration/Api/RedirectEndpointTests.cs` — 302/410/404, negative-cache eviction, cache headers, cache invalidation on delete
  - `Integration/Api/RateLimitingEndpointTests.cs` — token-bucket behavior, Retry-After header
  - `Integration/Api/ProblemDetailsTests.cs` — ProblemDetails traceId for 400/404/429

## Configuration

- `ShortCodes:Key` — base64 32-byte HMAC key for the Feistel cipher; set via `UserSecretsId` (`./set-secret.sh`) or `SHORTCODES__KEY` in `.env`
- `RateLimiting:CreateLink:Burst` / `RateLimiting:CreateLink:PerMinute` — token bucket parameters (defaults 20/20)
- `LinkCleanup:Interval` / `LinkCleanup:Retention` — background cleanup schedule (defaults 1h/7d)
- Database config via `.env` (double-underscore env binding) for Docker Compose; `appsettings.Development.json` for local dev
- OpenTelemetry via standard env vars (`OTEL_EXPORTER_OTLP_ENDPOINT`, etc.)

## Database

- PostgreSQL, configured via `appsettings.Development.json` (section `"Database"`) for local dev
- Docker Compose exposes PostgreSQL on host port **5555**
- `seed.sql` creates the `backend` user (not auto-loaded; commented out in compose.yaml)
- Health check at `/healthz` uses `AspNetCore.HealthChecks.NpgSql`
- **7 migrations**: `InitialMigration`, `AddOptionality`, `AddLinkCodeSequence` (sequence max = `Domain - 1`), `AlignTargetUrlWithUrlPolicy` (TargetUrl `2048` → `2000`), `AddDeleteTokenHash` (bytea), `AddDeletedLinkCleanupIndex` (filtered), `AddExpiredLinkCleanupIndex` (filtered), `PinTargetUrlConversion` (explicit URI↔string converter)
- `MigrateDatabaseAsync` in `PersistenceExtensions.cs` **called at startup** (Program.cs:16); integration tests run migrations themselves

## API Docs

- Scalar UI at `/scalar` (dev only, via `DocumentationExtensions.cs`)
- OpenAPI spec at `/openapi/v1.json` (OpenAPI 3.0)

## Notable

- Uses the **new `.slnx` solution format** (not legacy `.sln`)
- Docker base image: `mcr.microsoft.com/dotnet/aspnet:10.0` / sdk:10.0
- `AddControllers()`/`MapControllers()` remain in Program.cs but there are no controller classes
- Integration tests (`Testcontainers.PostgreSql`) require Docker to run
- Redirect endpoint cannot be exercised from Scalar UI — browser follows redirects; test with `curl -i` and no `-L`
- Delete endpoint returns delete token only on creation; token is Base64Url-encoded 32 bytes, verified via SHA-256 hash