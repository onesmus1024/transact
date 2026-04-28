# Transact

A small full-stack sample for managing money-movement transactions:
a .NET 10 minimal API backed by EF Core, frontend by an Angular 21 SPA.

## Solution layout

| Project | Purpose |
| --- | --- |
| [Transact.Domain](Transact.Domain) | Entities (`Transaction`) and enums (`TransactionType`). |
| [Transact.Application](Transact.Application) | CQRS via MediatR — commands, queries, repository interfaces. |
| [Transact.Infrastructure](Transact.Infrastructure) | EF Core `AppDbContext`, repositories, migrations. |
| [Transact.Api](Transact.Api) | Minimal API endpoints + Swagger + CORS. |
| [Transact.Web](Transact.Web) | Angular 21 standalone SPA (Tailwind, signals). |

## Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm 10+
- (Optional) `dotnet dev-certs https --trust` if you switch the API to HTTPS
- Docker Desktop / Docker Engine + Compose v2 (for the containerized run)

## Quick start with Docker

```powershell
cp .env.example .env   # then edit .env and set a real POSTGRES_PASSWORD
docker compose up --build
```

Then open http://localhost:8080.

What this brings up:

- `db`  — Postgres 17 (data persisted in the `db-data` volume)
- `api` — .NET 10 minimal API, migrations applied on startup, listens on `:8080` inside the network
- `web` — nginx serving the built Angular SPA on host port `${WEB_PORT:-8080}` and reverse-proxying `/api/*` to the API container

The SPA detects at runtime whether it is running on the Angular dev server (port `4200`)
or in production, and switches its API base URL accordingly:

| Environment | SPA origin | `apiBaseUrl` |
| --- | --- | --- |
| `npm start` (dev) | `http://localhost:4200` | `http://localhost:5125` |
| Docker (nginx)    | `http://localhost:8080` | `/api` (proxied to `api:8080`) |

All secrets/config come from `.env`. `appsettings.json` no longer contains a connection string —
it is provided via `ConnectionStrings__DefaultConnection` from compose.

## Run the backend (without Docker)

```powershell
cd Transact.Api
dotnet run --launch-profile http
```

The API listens on `http://localhost:5125`. Swagger UI: http://localhost:5125/swagger.

In `Development` the HTTPS redirect is disabled so the Angular dev server can call
the API over plain HTTP without CORS/cert issues.

## Run the frontend (without Docker)

```powershell
cd Transact.Web
npm install   # first time only
npm start
```

The SPA runs at http://localhost:4200 and is configured (via
[`src/environments/environment.ts`](Transact.Web/src/environments/environment.ts))
to call the API at `http://localhost:5125`.

CORS for `http://localhost:4200` is enabled in [Program.cs](Transact.Api/Program.cs).

## API endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET`    | `/transactions`         | List all transactions |
| `GET`    | `/transactions/{id}`    | Get one by id |
| `POST`   | `/transactions`         | Create a transaction |
| `PUT`    | `/transactions/{id}`    | Update a transaction |
| `DELETE` | `/transactions/{id}`    | Delete a transaction |
| `GET`    | `/health`               | Health probe |

`TransactionType` is serialized as a string (`P2P`, `Merchant`, `Paybill`, `Withdrawal`).

## Build everything

```powershell
# Backend
dotnet build Transact.slnx

# Frontend
cd Transact.Web; npm run build
```

## Database / migrations

EF Core migrations live under
[Transact.Infrastructure/Persistence/Migrations](Transact.Infrastructure/Persistence/Migrations).
To add a new migration:

```powershell
dotnet ef migrations add <Name> `
  --project Transact.Infrastructure `
  --startup-project Transact.Api
```
