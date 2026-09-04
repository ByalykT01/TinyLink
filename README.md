# TinyLink

A URL shortener that turns links into 7-character codes without leaking
anything about the database behind them. Codes are collision-free by
construction: a keyed cipher permutes a PostgreSQL sequence, so there's no
uniqueness-retry loop and nothing enumerable to guess.

Built with .NET 10, ASP.NET Core minimal APIs, EF Core and PostgreSQL. Currently live at
`https://mdvault.app`, and runs locally with one command.

[![CI](https://github.com/ByalykT01/TinyLink/actions/workflows/ci.yml/badge.svg)](https://github.com/ByalykT01/TinyLink/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](global.json)

## Contents

- [Features](#features)
- [Architecture](#architecture)
- [Quickstart](#quickstart)
- [Demo](#demo)
- [Routing](#routing)
- [How the short codes work](#how-the-short-codes-work)
- [Configuration](#configuration)
- [API](#api)
- [Deletion tokens](#deletion-tokens)
- [Redirect cache](#redirect-cache)
- [Design notes](#design-notes)
- [Observability](#observability)
- [Tests](#tests)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

## Features

| Area | Behavior |
| --- | --- |
| Shorten | `POST /api/links` returns `201` with the code, expiry and a deletion token |
| Redirect | `GET /{code}` returns `302` (`no-store`); `410` once a link's expired or deleted |
| Delete | Bearer-token deletion, idempotent; only the token's SHA-256 hash is ever stored |
| Collision-free codes | Feistel-permuted sequence, so there's no retry loop and no guessable IDs |
| Expiry and cleanup | 7-day default expiry; a background worker sweeps deleted/expired rows after a 7-day retention window |
| Rate limiting | Token bucket per client IP, IPv6 `/64`-aware, `429` with `Retry-After` |
| Cached redirects | `HybridCache`, 30-second TTL, immediate negative-cache eviction |
| Migrations on boot | EF Core applies pending migrations at startup |

## Architecture

```mermaid
flowchart LR
    Client --> Traefik
    Traefik --> API["TinyLink API"]
    API --> PG[("PostgreSQL")]
    API -.-> OTLP["OTLP collector"]
```

Traefik handles routing (and TLS in production). The API itself is stateless, so you can run as many replicas as you want behind it. PostgreSQL is the source of truth for links, codes and hashes.

## Quickstart

You'll need Docker Engine with Compose v2. To build or test outside Docker, install the .NET SDK version pinned in `global.json` (`10.0.110`); the key bootstrap script also needs `openssl`.

```bash
git clone https://github.com/ByalykT01/TinyLink.git
cd TinyLink
cp .env.example .env
./scripts/set-secret.sh
docker compose up --build
```

The bootstrap script writes `ShortCodes__Key` into `.env`, and migrations run automatically when the API starts. Once the containers report healthy, the commands in [Demo](#demo) will work as written.

Ports used locally: `80` and `8080` (Traefik), `5555` (PostgreSQL). In Development, the Scalar API reference is served at `/scalar`.

## Demo

These examples hit production directly, so there's nothing to set up locally first. To run them against a local stack instead, swap `https://mdvault.app` for `http://api.localhost` (see [Quickstart](#quickstart)).

Create a link:

```bash
curl -i -X POST https://mdvault.app/api/links \
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

Hang on to that deletion token. It's only returned once, at creation.

Open the short link:

```bash
curl -i https://mdvault.app/FKVY4mF
```

```http
HTTP/1.1 302 Found
Location: https://example.com/hello
Cache-Control: no-store
```

Delete it:

```bash
curl -i -X DELETE https://mdvault.app/api/links/FKVY4mF \
  -H 'Authorization: Bearer LX4Yp8M6hR3qK1vN9sT2wB7cD0fG5jUeA6iO4zPqW'
```

```http
HTTP/1.1 204 No Content
```

The link now returns `410 Gone`. (Production redirects plain HTTP to HTTPS, so the `https://` URLs above are required there. Locally, plain `http://api.localhost` is fine.)

## Routing

Traefik sits in front of the stack and discovers services through Docker labels. The API doesn't publish its container port directly, so you reach it through `http://api.localhost`. The Traefik dashboard lives at `http://traefik.localhost`, or directly at `http://localhost:8080/dashboard/`.

Browsers and most system resolvers point `.localhost` domains at `127.0.0.1` automatically. If yours doesn't, add `api.localhost` and `traefik.localhost` to `/etc/hosts`.

Since the API has no fixed host port, you can run several instances behind Traefik:

```bash
docker compose up -d --scale tinylink=3
```

Traefik load-balances between them.

The bundled dashboard and plain-HTTP setup are meant for local development only. A public deployment should run HTTPS and keep the Traefik dashboard behind auth (or off the internet entirely).

## How the short codes work

TinyLink encrypts a counter. PostgreSQL hands out 1, 2, 3, and so on, and a keyed cipher scrambles each number into another number within the same range.

Because the scrambling is one-to-one, two different counter values can never land on the same code. There's no uniqueness check or collision retry needed before insertion. The unique index on `ShortCode` is still there as a safety net, but it shouldn't ever need to catch anything.

```text
nextval('link_code_req') ──► Cipher.Permute ──► Base62.Encode ──► "FKVY4mF"
      counter                  bijection            7 chars
```

The cipher is a ten-round Feistel network over 42 bits, using HMAC-SHA256 as its round function. AES wasn't a fit here: its 128-bit block is far bigger than the space a seven-character Base62 code can represent.

Any output that lands outside the Base62 range just gets run through the permutation again until it fits.

Worth noting: this isn't a standardized format-preserving encryption construction. If you need something certified, NIST FF1 or FF3-1 are the right tools for that. See `Cipher.cs` for the actual implementation.

## Configuration

Settings come in through environment variables, with `__` representing nested configuration sections. `.env.example` has the values Docker Compose needs.

| Variable | Required | Default | Notes |
| --- | --- | --- | --- |
| `ShortCodes__Key` | yes | — | Base64, at least 32 bytes. The service won't start without it |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | yes | — | Used by the PostgreSQL container |
| `Database__Host` | yes | — | `postgres` under Compose, or `localhost` when running the API directly |
| `Database__Port` | yes | — | `5432` inside Compose, `5555` from the host |
| `Database__Name`, `Database__User`, `Database__Password` | yes | — | Must match the PostgreSQL settings above |
| `ASPNETCORE_ENVIRONMENT` | no | `Production` | `compose.override.yaml` sets this to `Development` |
| `RateLimiting__CreateLink__Burst` | no | `20` | Max token-bucket capacity |
| `RateLimiting__CreateLink__PerMinute` | no | `20` | Token refill rate |
| `LinkCleanup__Interval` | no | `1h` | How often the cleanup worker runs |
| `LinkCleanup__Retention` | no | `7d` | How long soft-deleted/expired links stick around before removal |
| `ForwardedHeaders__KnownNetworks__0` | no | — | CIDR range of trusted proxies (e.g. `172.16.0.0/12`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | no | — | OpenTelemetry collector endpoint (e.g. `http://otel:4317`; exports over gRPC by default) |

Standard `OTEL_EXPORTER_OTLP_*` variables (headers, timeouts) work too.

The short-code key and database password are secrets, so don't reuse the values in this repo for a public deployment.

## API

| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/links` | `201` with the short code, expiry and deletion token; `400` for an invalid target; `429` when rate limited |
| `GET` | `/{code}` | `302` to the target; `410` if expired or deleted; `404` if unknown |
| `DELETE` | `/api/links/{code}` | `204` with a valid deletion token; `404` if the code or token is wrong |
| `GET` | `/healthz` | Reports database connectivity |

Targets must be absolute `http` or `https` URLs, can't contain embedded credentials, and top out at 2,000 characters.

`expiresAt` is optional on creation and defaults to seven days out.

Errors come back as `application/problem+json` with a `traceId` you can match against application logs.

## Deletion tokens

Every link gets a random 32-byte deletion token at creation time. The full token is handed back once and is never stored by the API. Only its SHA-256 hash is: a supplied token gets decoded, hashed and compared against that stored hash using a constant-time comparison.

Send it as a bearer token:

```http
Authorization: Bearer <token>
```

Don't put it in the URL. URLs have a habit of ending up in browser history, proxy logs and server access logs.

Deletion is idempotent, so repeating it with the right token just returns `204 No Content` again. A missing, malformed or wrong token gets the same `404 Not Found` you'd see for an unknown code.

There's no recovery mechanism for a lost token. If it's gone, that link can't be deleted through the API.

The demo above uses HTTP for convenience, but outside local development, deletion tokens should only ever travel over HTTPS.

## Redirect cache

Redirect lookups go through ASP.NET Core's `HybridCache`, with results held in the local process for 30 seconds.

That keeps repeated requests for the same code from hitting PostgreSQL every time, and it also collapses concurrent cache misses for the same code into a single database lookup.

Unknown codes are evicted from the cache immediately, so a stream of made-up or constantly changing codes can't fill it up with short-lived `404` entries.

Creating or deleting a link invalidates its cached entry right away.

One caveat: the cache is local to each API process. With several replicas running, invalidation only clears the replica that handled the change, so another replica can keep serving a stale result for up to 30 seconds. Getting immediate consistency across replicas would need a shared distributed cache or an invalidation channel between them.

## Design notes

Cache headers depend on what happened:

- `302` uses `no-store`, since a link can still expire or get deleted later.
- `410` uses `public, max-age=86400`, because expiry and deletion are permanent, so there's no point re-checking.
- `404` uses `no-store` too, so a code created later doesn't stay hidden behind a cached miss.

Timestamps are stored as PostgreSQL `timestamptz` and always read back as UTC. Npgsql rejects non-UTC `DateTimeOffset` values, so timestamps get normalized before they're written.

Rate limiting partitions IPv6 clients by `/64` rather than by individual address, since a subscriber typically controls a whole `/64` and could otherwise dodge limits just by cycling addresses within it.

Forwarded headers are only trusted from configured proxy networks, so rate limiting can use the real client address without trusting arbitrary `X-Forwarded-For` headers from anyone on the internet.

The sequence is capped at 62⁷−1 in the migration, so it can never produce a value too large for a seven-character code.

## Observability

OpenTelemetry tracing, metrics and logs are wired up in `Program.cs`. Point `OTEL_EXPORTER_OTLP_ENDPOINT` at a collector (e.g. `http://otel:4317` in the Compose stack; it exports over gRPC by default). The service name comes from `OTEL_SERVICE_NAME` and defaults to `TinyLink.Api`. Out of the box it instruments:

- ASP.NET Core requests (excluding `/healthz`)
- Outgoing HTTP client calls
- Npgsql database commands
- EF Core operations (SQL text is left out, so URLs never end up in spans)
- A custom `TinyLink.Api` activity source
- Runtime metrics (GC, thread pool, CPU)
- Application logs (formatted, with scopes, trace-correlated)

If no endpoint is configured, exporters just fail silently and requests aren't affected. Traefik is set up to export to the same OTLP endpoint too.

## Tests

```bash
dotnet test
```

Unit tests, no Docker needed:

```bash
dotnet test --filter "FullyQualifiedName!~Integration"
```

Integration tests need Docker (Testcontainers spins up a real PostgreSQL 17 container):

```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

Unit tests cover Base62 encoding, the Feistel permutation, URL validation, deletion-token handling, option validation and exception classification. Integration tests cover link creation, redirects, expiration, deletion, cache invalidation, validation errors, rate limiting and background cleanup.

Load scenarios live in `k6/`; run them with `k6 run -e BASE_URL=http://api.localhost k6/<scenario>.js`.

CI builds Release with warnings treated as errors, runs the unit tests, boots the full Compose stack behind health gates, and runs integration tests against it. On pushes to `main`, it also ships the tested image straight to production. More on that below.

## Deployment

Production runs at `https://mdvault.app`, behind Cloudflare (TLS, HTTP redirected to HTTPS) and Traefik with Let's Encrypt via Cloudflare's DNS challenge. It shares the server's PostgreSQL instance through a dedicated role and database, and since migrations run at startup, deploys don't need a manual database step.

On every push to `main`, once the full test suite is green, CI streams the tested image straight to the server: `docker save` piped over SSH into `docker load`, tagged as `tinylink:latest`. The server-side Compose file and `.env` belong to the server and are never touched by CI; restarting the service is what picks up the new image.

Setting up a new server takes three steps:

1. Point a DNS A record at the server. The domain needs to live in Cloudflare, or Let's Encrypt issuance will fail (check `docker logs traefik` for ACME errors if it does).
2. Create the service directory with a Compose file that joins the Traefik network, plus a `.env` holding `DOMAIN`, the database credentials, and a one-time `ShortCodes__Key` (`openssl rand -base64 32`). Templates are in `compose.vps.yaml` and `.env.vps.example`.
3. Start it with `docker compose up -d` and check that `GET /healthz` returns `200`.

## Troubleshooting

**My change didn't take effect.**

Docker Compose can reuse an existing image if a rebuild fails silently, leaving an older container running. Run:

```bash
docker compose up --build
```

and check that the rebuild actually succeeded. Warnings are treated as errors, so even an unused variable is enough to break the build.

**`ShortCodes:Key` is not configured.**

The key's missing from `.env` or user secrets. Run `./scripts/set-secret.sh`.

**A port is already allocated.**

Something else is using port 80, 8080 or 5555. Stop it, or change the host-side port in `compose.yaml`.

**Traefik returns 404 for `api.localhost`, or the request just times out.**

Check that `traefik.docker.network` on the `tinylink` service matches the network Traefik is actually attached to (`docker network ls`), and that `loadbalancer.server.port` points to 8080, which is the port used inside the API container.

**Tests fail with Docker connection errors.**

Testcontainers needs a running Docker daemon. Start Docker Desktop (or `dockerd`) and try again.

**Migration errors show up after pulling schema changes.**

The local PostgreSQL volume probably has an old, incompatible dev schema. To wipe it and start clean:

```bash
docker compose down -v
docker compose up --build
```

This permanently deletes the local database volume, so don't run it if you care about what's in there.

**The VPS is still running the old image after a push.**

The image only ships on green `main` builds. Check the Actions run first, then on the server confirm the image actually arrived (`docker images tinylink`) and restart the service (`docker compose up -d`).

**HTTPS fails, or the browser complains about the certificate.**

The domain needs a DNS A record pointing at the server, and its zone has to be managed by Cloudflare, since Traefik issues certificates through Cloudflare's DNS challenge. Check `docker logs traefik` for ACME errors mentioning your domain.

## Roadmap

Things that might land later:

- An authenticated endpoint for changing a link's target or expiration.
- Cache invalidation coordinated across API replicas.
- Custom short-code aliases.
- Stronger administration via API keys or user accounts.

## Contributing

1. Fork the repo and branch off: `git checkout -b feature/my-change`.
2. Build Release (warnings are errors): `dotnet build -c Release`.
3. Run the tests: `dotnet test` (integration tests need a running Docker daemon).
4. Open a PR against `main` describing the change, and make sure CI is green.

## License

[MIT](LICENSE)
