A URL shortener whose codes come from a keyed Feistel permutation of a database
sequence. Collisions are impossible by construction, and a code gives away
neither how many links exist nor what order they were created in. Built on
.NET 10, ASP.NET Core minimal APIs, EF Core and PostgreSQL.

Status: working prototype. Creating links and redirecting both work. The
[Roadmap](#roadmap) covers what's missing.

## Demo

```bash
curl -i -X POST http://api.localhost/api/links \
  -H 'Content-Type: application/json' \
  -d '{"url":"https://example.com/hello"}'
```

```http
HTTP/1.1 201 Created
Location: /FKVY4mF
Content-Type: application/json
{"shortCode":"FKVY4mF","expiresAt":"2026-08-31T13:15:13.5216146+00:00"}
```

```bash
curl -i http://api.localhost/FKVY4mF
```

```http
HTTP/1.1 302 Found
Location: https://example.com/hello
Cache-Control: no-store
```

## How the short codes work

TinyLink encrypts the counter. Postgres hands out 1, 2, 3 and onward, then a
keyed cipher scrambles that number into another one in the same range. The
scrambling is one-to-one, so two counter values can never produce the same
code. No uniqueness check before the insert, no retry after it, and the unique
index on `ShortCode` is only a safety net.

```
nextval('link_code_req') ──► Cipher.Permute ──► Base62.Encode ──► "FKVY4mF"
      counter                  bijection            7 chars
```

The cipher is a ten-round Feistel network over 42 bits with HMAC-SHA256 as its
round function, hand-rolled because AES's 128-bit block is far too wide for a
seven-character code. The handful of outputs that fall outside the Base62 range
get re-encrypted until they fit. See `Cipher.cs`.

It isn't a standardized construction. NIST's FF1 and FF3-1 are the certified
options if you need one.

## Prerequisites

Docker Engine and Compose v2 run the stack, and the integration tests need them
too. To build or test outside Docker you want the .NET SDK version pinned in
`global.json` (10.0.110). The key bootstrap script calls `openssl`.

Ports 80 and 8080 (Traefik) and 5555 (Postgres) have to be free.

## Quickstart

```bash
git clone https://github.com/ByalykT01/TinyLink.git
cd TinyLink
cp .env.example .env
./scripts/set-secret.sh    # writes ShortCodes__Key into .env
docker compose up --build
```

Migrations are applied at startup. Once the containers report healthy the
[Demo](#demo) commands work as written. In Development the Scalar API reference
is served at `/scalar`.

## Routing

Traefik fronts the stack and picks up services from Docker labels, so the API
has no published host port of its own and answers on `http://api.localhost`.
The Traefik dashboard is at `http://traefik.localhost`, or on
`http://localhost:8080/dashboard/` if you'd rather go straight at it.
Browsers and most libc resolvers send anything under `.localhost` to
127.0.0.1. If yours doesn't, add `api.localhost` and `traefik.localhost` to
`/etc/hosts`.

Since the API binds no host port, you can run several copies and Traefik will
round-robin across them:

```bash
docker compose up -d --scale tinylink=3
```

## Configuration

Every setting can come in as an environment variable, with `__` for nesting.
`.env.example` has the full set.

| Variable | Required | Default | Notes |
| --- | --- | --- | --- |
| `ShortCodes__Key` | yes | — | Base64, 32 bytes or more. The service refuses to start without it. Written by `scripts/set-secret.sh` |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | yes | — | Read by the Postgres container |
| `Database__Host` | yes | — | `postgres` under Compose, `localhost` when you run the API directly |
| `Database__Port` | yes | — | `5432` inside the Compose network, `5555` from the host |
| `Database__Name`, `Database__User`, `Database__Password` | yes | — | Have to match the `POSTGRES_*` values |
| `ASPNETCORE_ENVIRONMENT` | no | `Production` | `compose.override.yaml` sets `Development` |
| `RateLimiting__CreateLink__Burst` | no | `20` | Token bucket capacity |
| `RateLimiting__CreateLink__PerMinute` | no | `20` | Refill rate |

`ShortCodes__Key` is the only real secret here.

## API


| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/links` | `201` with `Location` and the short code; `400` on an invalid target; `429` when rate limited |
| `GET` | `/{code}` | `302` to the target; `410` if expired or deleted; `404` if unknown |
| `GET` | `/healthz` | Database connectivity |

Targets have to be absolute `http` or `https` URLs, 2000 characters at most,
with no embedded credentials. `expiresAt` is optional on creation and defaults
to seven days out.

Errors come back as `application/problem+json` with a `traceId` you can grep
the logs for.

## Design notes

The cache headers are chosen per status code. A `302` is `no-store`, since the
target can change or expire under it. A `410` gets `public, max-age=86400`,
because expiry is permanent and worth caching. A `404` is `no-store` again, so
that a code created later isn't shadowed by a cached miss.

Timestamps are `timestamptz` and always read back as UTC. Npgsql rejects a
non-UTC `DateTimeOffset` outright, so creation normalizes on the way in.
Rate limiting partitions IPv6 clients by `/64` instead of by full address. One
subscriber usually holds an entire /64 and could otherwise cycle through
billions of buckets.

The sequence is bounded at 62⁷−1 in the migration, so the counter can't outrun
what seven characters can represent.

## Tests

```bash
dotnet test
```

Unit tests cover Base62 and the URL policy. Integration tests run against a
real PostgreSQL 17 container through Testcontainers, so no local database is
needed, though the Docker daemon has to be up. CI runs the same suite on every
push with warnings as errors under `AnalysisMode=All`.

## Troubleshooting

**My change didn't take effect.**

`docker compose up` will reuse the existing image, and when a rebuild fails it
cheerfully starts the previous one. Use `docker compose up --build` and check
that the build actually succeeded.
Warnings are errors here, so an unused variable is enough to fail it.
`InvalidOperationException: ShortCodes:Key is not configured.` The key is
missing from `.env` or from user-secrets. Run `./scripts/set-secret.sh`. Port
already allocated. Something else holds 80, 8080 or 5555. Change the host
side of the mapping in `compose.yaml`.

**Traefik answers `api.localhost` with a 404, or the request times out.**

Check that `traefik.docker.network` on the `tinylink` service matches the
network Traefik is actually attached to (`docker network ls`), and that
`loadbalancer.server.port` is the port the app listens on inside the container,
8080.

**Tests fail with Docker connection errors.**

Testcontainers needs a running daemon. Start Docker Desktop or `dockerd` and
try again. Migration errors after pulling schema changes. The Postgres volume
still has the old schema in it. `docker compose down -v` drops it, then start
again.

## Roadmap

Left out for now, roughly in the order I'd add them: tests for `Cipher` and
`ShortCodeAllocator`; endpoint tests through `WebApplicationFactory`; a delete
endpoint (the `DeletedAt` column exists and redirects honour it, nothing writes
it yet); click counting; caching on the redirect path; a cleanup job for expired
rows; API keys.

Caching and metrics wait until something measures the read path.

## License

[MIT](LICENSE)
