# Shelter

Booking platform for shelters and other outdoor accommodations. ASP.NET Core 10 +
Postgres/PostGIS API + React 19 frontend.

This file is the local-development setup guide. For architecture and conventions, see
`Api/CLAUDE.md` (backend) and `App/CLAUDE.md` (frontend).

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0** | `dotnet --version` should print `10.0.x`. |
| `dotnet ef` tool | **10.0** | `dotnet tool install --global dotnet-ef` (or `update`). |
| Node.js | **20+** | Anything 20.x or newer; LTS recommended. |
| Docker Desktop | recent | Used for Postgres (custom PostGIS image) and Azurite. |
| Google Maps API key | — | Get one at <https://console.cloud.google.com/google/maps-apis>. Enable Maps JavaScript API + Places API. Create a Map ID for vector maps. |

## Quick start (fresh checkout)

```bash
# 1. Bring up Postgres + Azurite (first run builds the PostGIS image; ~1 min)
cd Api
docker compose up -d --build

# 2. Trust the dev HTTPS cert (one-time per machine)
dotnet dev-certs https --trust

# 3. Restore + run the API (auto-applies migrations + seeds the ShelterOwner role)
dotnet run --project Shelter.Api

# 4. In a second terminal: install + run the frontend
cd ../App
npm install
cp .env.development .env.development.local   # only if .env.development.local doesn't exist
# edit .env.development.local — paste your Google Maps API key + Map ID (see step 5)
npm run dev
```

API: <http://localhost:44330> (Swagger UI at `/swagger`).
Frontend: <http://localhost:3000>.

### 5. Frontend env file

`App/.env.development` is committed with empty placeholders. Vite loads
`App/.env.development.local` on top (gitignored), which is where the real values go:

```env
VITE_API_BASE_URL=http://localhost:44330
VITE_GOOGLE_MAPS_API_KEY=AIza...your-key...
VITE_GOOGLE_MAP_ID=your-map-id
```

The Zod-validated `env` loader at `App/src/shared/config/env.ts` will throw at boot
with a clear message if a value is missing.

## Optional: SendGrid for email confirmation

Without configuration, registration emails are written to the API console
(`LoggingEmailSender` fallback). For real delivery, sign up at <https://sendgrid.com>,
verify a Single Sender (your own email is fine — see `thesis/...` notes), and stash the
key in `dotnet user-secrets` so it never enters the repo:

```bash
cd Api/Shelter.Api
dotnet user-secrets init    # idempotent; adds a UserSecretsId to the csproj
dotnet user-secrets set "SendGrid:ApiKey"    "SG.xxxxxxxxxx"
dotnet user-secrets set "SendGrid:FromEmail" "the-address-you-verified@example.com"
dotnet user-secrets set "SendGrid:FromName"  "Shelter"
```

The DI registration in `Shelter.Infrastructure/Settings/SendGridSettings.cs` swaps to
`SendGridEmailSender` automatically when `SendGrid:ApiKey` is non-empty.

## Common dev tasks

### Run tests

```bash
cd Api
dotnet test tests/Shelter.UnitTests/Shelter.UnitTests.csproj           # Tier 1: ~50 ms
dotnet test tests/Shelter.IntegrationTests/Shelter.IntegrationTests.csproj  # Tier 3: ~2 s, spins a Postgres container
```

### Regenerate frontend types after API contract changes

The API project emits OpenAPI JSON at build time:

```bash
cd Api
dotnet build Shelter.Api/Shelter.Api.csproj /t:GenerateOpenApiDocuments
                                            # -> Api/openapi/Shelter.Api.json

cd ../App
npm run gen:api   # -> App/src/shared/api/types/paths.d.ts
```

Commit both generated files so a fresh checkout can build the frontend without the API
toolchain (see `App/CLAUDE.md` § "API client").

### Add a database migration

```bash
cd Api
dotnet ef migrations add <Name> -p Shelter.Infrastructure -s Shelter.Api
# Review the generated Up()/Down() before committing.
```

In Development the API auto-applies pending migrations on startup, so the next
`dotnet run` picks them up.

### Lint / build the frontend

```bash
cd App
npm run lint
npm run build      # tsc -b && vite build
```

## Resetting the database

Three options, ranked by how destructive they are:

```bash
# 1. Nuke everything (volumes + data + schema). Recreated on next dotnet run.
cd Api
docker compose down -v && docker compose up -d
dotnet run --project Shelter.Api

# 2. Drop + recreate the DB without touching containers (faster).
docker exec -it shelter-postgres psql -U shelter -d postgres -c \
  "DROP DATABASE shelter WITH (FORCE); CREATE DATABASE shelter OWNER shelter;"
# Then restart `dotnet run` — auto-migrate runs again.

# 3. Truncate row data, keep schema + Identity roles.
docker exec -it shelter-postgres psql -U shelter -d shelter <<'SQL'
TRUNCATE TABLE
  "ReviewPictures","Reviews","Bookings","ShelterPictures","Shelters","Assets",
  "AspNetUserRoles","AspNetUserClaims","AspNetUserLogins","AspNetUserTokens","AspNetUsers"
RESTART IDENTITY CASCADE;
SQL
```

Default to option 1 unless you have a specific reason to keep something. Option 3 is the
same approach the integration tests use via Respawn.

## Common gotchas

- **Port 5432 already in use** — usually a native Postgres install (EnterpriseDB on macOS
  is a common culprit). Diagnose:

  ```bash
  sudo lsof -nP -iTCP:5432 -sTCP:LISTEN
  ```

  Stop a launchd-managed Postgres with `sudo launchctl bootout system /Library/LaunchDaemons/postgresql-<n>.plist`
  + `sudo launchctl disable system/postgresql-<n>` for a permanent fix. Or switch the
  Compose mapping to `5433:5432` and update `Api/Shelter.Api/appsettings.Development.json`'s
  connection string.

- **Vite running on the wrong port** — must be 3000. The API's CORS allow-list only
  includes `http://localhost:3000` and `https://localhost:3001`; the Vite default 5173
  fails preflight. The dev script in `App/vite.config.ts` pins port 3000.

- **API HTTPS cert untrusted** — the API listens on HTTPS (`:44330`) by default; if your
  browser refuses the cert, run `dotnet dev-certs https --trust` once.

- **PostGIS image build fails on first run** — the Compose stack builds
  `Api/docker/postgres/Dockerfile` (official `postgres:18` + `apt-get install
  postgresql-18-postgis-3`). Needs network access on first build. After the build is
  cached, plain `docker compose up -d` reuses it.

- **OpenAPI document didn't update after backend changes** — incremental builds sometimes
  skip the document-emit hook. Force it:

  ```bash
  dotnet build Api/Shelter.Api/Shelter.Api.csproj /t:GenerateOpenApiDocuments
  ```

  Then re-run `npm run gen:api`.

- **`dotnet ef` not found** — install the tool: `dotnet tool install --global dotnet-ef`.

## Project layout

```
Api/                  ASP.NET Core 10 Web API (Minimal APIs, vertical slices)
  Shelter.Domain/     Aggregates, value objects (zero external deps)
  Shelter.App/        Use-case handlers, DTOs, persistence interfaces
  Shelter.Infrastructure/  EF Core, Postgres, Azurite, SendGrid
  Shelter.Api/        HTTP endpoints, Program.cs, OpenAPI emission
  tests/              Tier 1 unit + Tier 3 integration tests
  docker/postgres/    Custom postgres:18 + PostGIS Dockerfile
  docker-compose.yml  Local Postgres + Azurite stack

App/                  React 19 + Vite 7 frontend (Feature-Sliced Design)
  src/features/       auth, shelters, bookings, reviews, map, search
  src/pages/          Route shells (thin)
  src/shared/         API client, generated types, UI primitives, utils

thesis/               Project documentation, requirements spec, test report
CLAUDE.md             Project-root TODOs against the requirements spec
Api/CLAUDE.md         Backend architecture guide
App/CLAUDE.md         Frontend architecture guide
```

## Further reading

- `Api/CLAUDE.md` — backend architecture, vertical-slice conventions, persistence layering, auth model, geospatial setup.
- `App/CLAUDE.md` — frontend architecture, state boundaries, API client, contract deltas.
- `thesis/testing-strat.md` — testing tiers, why no in-memory database tier, sweepline extraction.
- `thesis/test-report.md` — traceability of requirements → verification.
- `thesis/api-design.md`, `thesis/app-design.md` — design rationale per project.
