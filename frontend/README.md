# TinyLink frontend

Minimal React frontend for the TinyLink URL shortener. Vite + React + TypeScript,
hand-written CSS on a plain white system-font theme, organized in vertical
slices mirroring the backend's `Features/` layout. No component library, no
icon library, no webfonts.

## Stack

| Concern | Choice |
|---|---|
| Build / dev | Vite 8 |
| UI | React 19, hand-written CSS (`src/app/styles/`), system font stack |
| Type | System stack regular; bold wordmark + section headings only; system mono for data |
| Routing | react-router-dom (`/` shorten+delete, `/:code` open helper, `*` 404) |
| Server state | Plain `fetch` per slice (two endpoints — no data-fetching lib) |
| Tests | Vitest + Testing Library (client + validation units) |

## Run

```bash
cd frontend
cp .env.example .env   # optional; defaults target the compose stack
npm install
npm run dev            # http://localhost:5173
```

Prerequisite: the backend reachable at `http://api.localhost`
(`docker compose up` from the repo root, then Traefik routes `api.localhost`).

Useful scripts: `npm test`, `npm run typecheck`, `npm run lint`, `npm run build`.

## How it talks to the API

- `POST /api/links` → create; `DELETE /api/links/{code}` (Bearer token) → delete.
  See `src/features/{shorten-link,delete-link}/api.ts`.
- `GET /api/links/{code}` → status check; the `/:code` route renders the
  short link when active and real gone/unknown pages on `410`/`404`
  (`src/features/redirect/`). The raw `GET /{code}` is a `302` the browser
  follows transparently, so it is only ever used for plain navigation.
- Dev uses the Vite proxy (`/api`, `/healthz` → `http://api.localhost`); set
  `VITE_API_BASE_URL` for split-origin deploys, which requires the API's CORS
  policy (wired in `TinyLink.Api`, origins from `Frontend:Origins` config).
- Short links open at `VITE_REDIRECT_BASE_URL` (default `http://api.localhost`),
  since only the API origin serves redirects.

## Layout

```
src/
├── app/            # router, styles (tokens + components), AppShell layout
├── shared/         # config, api client + problem+json errors, small UI atoms
│                   # (FlagBlock, CopyControl, CodeCells, HealthStatus)
├── features/
│   ├── shorten-link/  # POST slice: form, result panel (token shown once), page
│   ├── delete-link/   # DELETE slice: token form
│   └── redirect/      # open-link helper route
└── test/           # vitest setup
```

## Design (v3)

Plain white tool: near-black system text, one link-blue accent for links and
the primary button only, hairline dividers between unboxed sections, rounded
inputs and buttons, no shadows, no animation. Errors are small red sentences
under their field; expiry, rate-limit, and trace info are small gray inline
text. The only icons are copy, copied-check, and external-link — small gray
inline SVGs. The result state is three plain lines (short URL, code, token),
each with a copy icon.

Deliberately out of scope for v1: link history (the backend has no list
endpoint), custom expiries (server defaults to 7 days), Docker packaging.
