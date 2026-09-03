# TinyLink

A URL shortener whose codes come from a keyed Feistel permutation of a database
sequence. Collisions are impossible by construction, and a code reveals neither
how many links exist nor the order in which they were created.

Built with .NET 10, ASP.NET Core minimal APIs, EF Core and PostgreSQL.

Status: working prototype. Links can be created, resolved and deleted. Deleted
and expired links are permanently removed by a background worker.

## Demo

Create a link:

```bash
curl -i -X POST http://api.localhost/api/links \
  -H 'Content-Type: application/json' \
  -d '{"url":"https://example.com/hello"}'
```

```http
HTTP/1.1 201 Created
Location: /FKVY4mF
Content-Type: application/json
{
  "shortCode": "FKVY4mF",
  "expiresAt": "2026-08-31T13:15:13.5216146+00:00",
  "deleteToken": "LX4Yp8M6hR3qK1vN9sT2wB7cD0fG5jUeA6iO4zPqW"
}
```

Save the deletion token. It is returned only when the link is created.

Open the short link:

```bash
curl -i http://api.localhost/FKVY4mF
```

```http
HTTP/1.1 302 Found
Location: https://example.com/hello
Cache-Control: no-store
```

Delete it:

```bash
curl -i -X DELETE http://api.localhost/api/links/FKVY4mF \
  -H 'Authorization: Bearer LX4Yp8M6hR3qK1vN9sT2wB7cD0fG5jUeA6iO4zPqW'
```

```http
HTTP/1.1 204 No Content
```

The same short link now returns `410 Gone`.

## How the short codes work

TinyLink encrypts a counter. PostgreSQL hands out 1, 2, 3 and onward, then a
keyed cipher scrambles that number into another number in the same range.

The scrambling is one-to-one, so two counter values cannot produce the same
code. No uniqueness check or collision retry is needed before insertion. The
unique index on `ShortCode` remains as a final safety net.

```text
nextval('link_code_req') ──► Cipher.Permute ──► Base62.Encode ──► "FKVY4mF"
      counter                  bijection            7 chars
```

The cipher is a ten-round Feistel network over 42 bits, with HMAC-SHA256 as its
round function. AES has a 128-bit block, which is much larger than the space
represented by a seven-character Base62 code.

Outputs outside the Base62 range are passed through the permutation again until
they fit. See `Cipher.cs` for the implementation.

This is not a standardized format-preserving encryption construction. NIST FF1
and FF3-1 are the appropriate choices where a certified construction is
required.

## Prerequisites

Docker Engine and Docker Compose v2 are required to run the stack. The
integration tests also require a working Docker daemon.

To build or test outside Docker, install the .NET SDK version pinned in
`global.json` (`10.0.110`). The key bootstrap script requires `openssl`.

Ports 80 and 8080 for Traefik and 5555 for PostgreSQL must be available.

## Quickstart

```bash
git clone https://github.com/ByalykT01/TinyLink.git
cd TinyLink
cp .env.example .env
./scripts/set-secret.sh
docker compose up --build
```

The bootstrap script writes `ShortCodes__Key` into `.env`.

Database migrations are applied when the API starts. Once the containers report
healthy, the [Demo](#demo) commands should work as written.

In the Development environment, the Scalar API reference is available at `/scalar`.

## Routing

Traefik fronts the stack and discovers services through Docker labels. The API
does not publish its container port directly and is available through:

```text
http://api.localhost
```

The Traefik dashboard is available at:

```text
http://traefik.localhost
```

It can also be opened directly at:

```text
http://localhost:8080/dashboard/
```

Browsers and most system resolvers direct domains under `.localhost` to
`127.0.0.1`. If yours does not, add `api.localhost` and `traefik.localhost` to
`/etc/hosts`.

Because the API has no fixed host port, several instances can run behind Traefik:

```bash
docker compose up -d --scale tinylink=3
```

Traefik distributes requests between them.

The included dashboard and plain HTTP configuration are intended for local
development. A public deployment should use HTTPS and should not expose the
Traefik dashboard without authentication.

## Configuration

Settings can be provided through environment variables. Use `__` to represent
nested configuration sections. `.env.example` contains the values needed by
Docker Compose.

| Variable | Required | Default | Notes |
| --- | --- | --- | --- |
| `ShortCodes__Key` | yes | — | Base64 containing at least 32 bytes. The service refuses to start without it |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | yes | — | Used by the PostgreSQL container |
| `Database__Host` | yes | — | `postgres` under Compose or `localhost` when running the API directly |
| `Database__Port` | yes | — | `5432` inside Compose or `5555` from the host |
| `Database__Name`, `Database__User`, `Database__Password` | yes | — | Must match the corresponding PostgreSQL settings |
| `ASPNETCORE_ENVIRONMENT` | no | `Production` | `compose.override.yaml` sets this to `Development` |
| `RateLimiting__CreateLink__Burst` | no | `20` | Maximum token-bucket capacity |
| `RateLimiting__CreateLink__PerMinute` | no | `20` | Token refill rate |
| `LinkCleanup__Interval` | no | `1h` | Background cleanup run interval |
| `LinkCleanup__Retention` | no | `7d` | Age after which soft-deleted/expired links are removed |
| `ForwardedHeaders__KnownNetworks__0` | no | — | CIDR range of trusted proxies (e.g., `172.16.0.0/12`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | no | — | OpenTelemetry collector endpoint (e.g., `http://otel:4318`) |

The short-code key and database password are secrets. The development values
from the repository should not be used in a public deployment.

## API

| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/links` | `201` with the short code, expiry and deletion token; `400` for an invalid target; `429` when rate limited |
| `GET` | `/{code}` | `302` to the target; `410` if expired or deleted; `404` if unknown |
| `DELETE` | `/api/links/{code}` | `204` with a valid deletion token; `404` if the code or token is invalid |
| `GET` | `/healthz` | Reports database connectivity |

Targets must be absolute `http` or `https` URLs, may not contain embedded
credentials, and are limited to 2,000 characters.

`expiresAt` is optional during creation and defaults to seven days from the
current time.

Errors are returned as `application/problem+json` and include a `traceId` that
can be matched with application logs.

## Deletion tokens

Every link receives a random 32-byte deletion token when it is created. The
complete token is returned once and is never stored by the API.

TinyLink stores only its SHA-256 hash. A supplied token is decoded, hashed and
compared with the stored hash using a constant-time comparison.

The token is sent in the authorization header:

```http
Authorization: Bearer <token>
```

It should not be placed in the URL because URLs commonly appear in browser
history, proxy logs and server access logs.

Deletion is idempotent. Repeating a deletion with the correct token returns
`204 No Content`. Missing, malformed and incorrect tokens return the same `404
Not Found` response as an unknown code.

There is no token recovery mechanism. If the token is lost, the link cannot be
deleted through the API.

The local demo uses HTTP for convenience. Deletion tokens must only be sent
over HTTPS outside local development.

## Redirect cache

Redirect lookups use ASP.NET Core `HybridCache`. Cached results remain in the
local process for 30 seconds.

The cache prevents repeated requests for the same short code from querying
PostgreSQL each time. It also combines concurrent cache misses for the same
code into one database lookup.

Unknown codes are removed from the cache immediately. This prevents requests
containing continuously changing codes from filling the cache with short-lived
`404` entries.

Creating or deleting a link invalidates its cached result.

The cache is currently local to each API process. When several API replicas are
running, invalidation affects only the replica that handled the change. Another
replica may retain an old result for up to the 30-second cache lifetime. A
shared distributed cache or invalidation channel would be needed for immediate
consistency between replicas.

## Design notes

Response cache headers depend on the result:

- A `302` response uses `no-store` because a link can expire or be deleted.

- A `410` response uses `public, max-age=86400` because expiry and deletion are permanent.

- A `404` response uses `no-store` so a code created later is not hidden by a cached miss.

Timestamps are stored as PostgreSQL `timestamptz` values and read back as UTC.
Npgsql rejects non-UTC `DateTimeOffset` values, so creation normalizes
timestamps before storing them.

Rate limiting partitions IPv6 clients by `/64` rather than by complete address.
A subscriber commonly controls an entire `/64` and could otherwise avoid limits
by cycling through addresses.

Forwarded headers are accepted only from configured proxy networks. This allows
rate limiting to use the original client address without trusting arbitrary
`X-Forwarded-For` headers from the internet.

The sequence is bounded at 62⁷−1 in the migration, preventing it from
generating values that cannot be represented by a seven-character code.

## Observability

OpenTelemetry tracing and metrics are built in. Configure `OTEL_EXPORTER_OTLP_ENDPOINT` to send data to a collector (e.g., `http://otel:4318` in the Compose stack). The default configuration instruments:

- ASP.NET Core requests (excluding `/healthz`)
- Outgoing HTTP client calls
- Npgsql database commands
- Custom `TinyLink.Api` activity source

Traefik is also configured to export traces and metrics to the same OTLP endpoint.

## Tests

```bash
dotnet test
```

Unit tests cover Base62 encoding, the Feistel permutation, URL validation and
deletion-token handling.

Integration tests use `WebApplicationFactory` and a real PostgreSQL 17
container provided by Testcontainers. They cover link creation, redirects,
expiration, deletion, cache invalidation, validation errors and rate limiting.

No locally installed PostgreSQL server is required, but Docker must be running.

CI runs the same test suite on every push. Compiler and analyzer warnings are
treated as errors under `AnalysisMode=All`.

## Troubleshooting

**My change did not take effect.**

Docker Compose can reuse an existing image. If a rebuild fails, an older
container may still start.

Run:

```bash
docker compose up --build
```

Check that the image was rebuilt successfully. Warnings are treated as errors,
so an unused variable is enough to stop the build.

**`ShortCodes:Key` is not configured.**

The key is missing from `.env` or user secrets. Run:

```bash
./scripts/set-secret.sh
```

**A port is already allocated.**

Another process is using port 80, 8080 or 5555. Stop that process or change the
host-side port in `compose.yaml`.

**Traefik returns 404 for `api.localhost`, or the request times out.**

Check that `traefik.docker.network` on the `tinylink` service matches the network to which Traefik is attached:

```bash
docker network ls
```

Also check that `loadbalancer.server.port` points to port 8080, which is the
port used inside the API container.

**Tests fail with Docker connection errors.**

Testcontainers needs a running Docker daemon. Start Docker Desktop or
`dockerd`, then run the tests again.

**Migration errors appear after pulling schema changes.**

The existing PostgreSQL volume may contain an incompatible development schema.
To discard the local database and recreate it:

```bash
docker compose down -v
docker compose up --build
```

This permanently removes the local database volume.

## Roadmap

A background worker permanently removes soft-deleted and expired links on a configurable schedule (default: every hour, retention: 7 days).

Possible later additions include:

- An authenticated endpoint for changing a link’s target or expiration.

- Coordination of cache invalidation between API replicas.

- Custom short-code aliases.

- Stronger administration through API keys or user accounts.

## License

[MIT](LICENSE)
