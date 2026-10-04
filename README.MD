# Omnia

Omnia is a cross-platform synchronized clipboard. Sign in with an email and password on any device, and every
device signed into that account shares one real-time clip list. Type short text, a URL, or a note on your phone and
it appears on your laptop within seconds.

The account is the sync channel, so there is no room PIN, no pairing step, and nothing to configure between devices.

## Stack

- **Backend:** ASP.NET Core Web API (.NET 10)
- **Database:** PostgreSQL 16 with Entity Framework Core
- **Realtime:** ASP.NET Core SignalR
- **Auth:** Email + password with JWT bearer tokens
- **Client:** Avalonia UI 11 (Desktop, Android, iOS, Browser/WASM)
- **Client MVVM:** CommunityToolkit.Mvvm
- **Deployment:** Docker Compose

See [Architecture.MD](Architecture.MD) for the full design.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL and the API)
- Avalonia templates: `dotnet new install Avalonia.Templates`
- For Android: the Android SDK and a JDK
- For iOS: macOS with Xcode

## Quick Start

### 1. Start the backend

Create your environment file and fill in the secrets:

```bash
cp .env.example .env
```

Then build and run PostgreSQL plus the API:

```bash
docker compose up --build
```

The API listens on `http://localhost:8080` and applies EF Core migrations on startup.

To run the API locally instead of in Docker (with PostgreSQL still in Docker):

```bash
docker compose up -d postgres
dotnet run --project src/Omnia.Api
```

### 2. Run the client

Desktop:

```bash
dotnet run --project src/Omnia.Client.Desktop
```

Browser (WebAssembly):

```bash
dotnet run --project src/Omnia.Client.Browser
```

Android:

```bash
dotnet build src/Omnia.Client.Android -t:Run -f net10.0-android
```

iOS:

```bash
dotnet build src/Omnia.Client.iOS -t:Run -f net10.0-ios
```

## Configuration

Server settings come from environment variables (or `appsettings.Development.json` for local runs):

| Variable                    | Purpose                                  |
|-----------------------------|------------------------------------------|
| `ConnectionStrings__Default`| PostgreSQL connection string             |
| `Jwt__Secret`               | HMAC signing key (use a long random value)|
| `Jwt__Issuer`               | JWT issuer                               |
| `Jwt__Audience`             | JWT audience                             |

`.env.example` lists the same keys for Docker Compose. Never commit a real `.env`.

The client reads its API base URL from configuration (default `http://localhost:8080`).

## Relay Hosting

Omnia has no dependency on any external service. The API is the relay that stores clips and fans them out to a
user's devices, and you decide where it runs:

- **Self-hosted** — run the Docker Compose stack on your own machine, home server, or VPS. Point each client's API
  base URL at it. Your data stays on hardware you control.
- **Shared instance** — run one instance and point many clients at it. The same stack works; you only need to add
  TLS (a reverse proxy) and CORS for the browser client. A single instance handles small groups without any
  additional infrastructure.

Either way, accounts are isolated automatically: a device only ever receives the clips belonging to the signed-in
user.

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

Tests use `WebApplicationFactory` with `Testcontainers` to run the API against a real PostgreSQL instance, so Docker
must be running.

## Project Structure

```
Omnia.sln
├─ src/
│  ├─ Omnia.Shared/          # DTOs, routes, hub contract shared by API and client
│  ├─ Omnia.Api/             # ASP.NET Core Web API, EF Core, SignalR hub, JWT auth
│  ├─ Omnia.Client/          # Avalonia shared UI: Views, ViewModels, Services
│  ├─ Omnia.Client.Desktop/  # Avalonia desktop head
│  ├─ Omnia.Client.Android/  # Avalonia Android head
│  ├─ Omnia.Client.iOS/      # Avalonia iOS head
│  └─ Omnia.Client.Browser/  # Avalonia WebAssembly head
└─ tests/
   └─ Omnia.Api.Tests/       # xUnit integration and unit tests
```

## How It Works

1. **Sign in** — the client posts credentials to `/auth/login` and stores the returned JWT.
2. **Connect** — the client opens a SignalR connection to `/hubs/clips`, authenticated with the JWT.
3. **Sync** — typing a clip sends `POST /clips`; the server saves it and broadcasts `OnClipCreated` to every device
   on that account. Swiping a clip sends `DELETE /clips/{id}` and broadcasts `OnClipDeleted`.
4. **Recover** — if the connection drops, the client shows Reconnecting, retries, and re-fetches `GET /clips` once it
   is back online.

## Troubleshooting

- **Migrations not applied / cannot connect:** confirm the `postgres` container is healthy (`docker compose ps`) and
  that `ConnectionStrings__Default` matches the compose service name and credentials.
- **401 from the API:** the JWT secret differs between the API process and the one that issued the token; make sure
  `Jwt__Secret` is the same everywhere.
- **Hub never connects over HTTP:** SignalR needs WebSockets or a supported transport; use HTTPS outside local dev.
