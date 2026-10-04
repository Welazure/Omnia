# Omnia — Architecture

## Overview

Omnia is a cross-platform synchronized clipboard. A user signs in with an email and password, and every device
signed into that account shares a single real-time clip list. Short text, URLs, and temporary notes typed on one
device appear on the others within seconds.

The original concept used a shared 4-digit room PIN with anonymous access and a Supabase backend. This version
replaces that with user accounts, so the account itself is the sync channel and the PIN is no longer needed.

## Stack

| Layer            | Technology                                                        |
|------------------|-------------------------------------------------------------------|
| Backend          | ASP.NET Core Web API (.NET 10)                                    |
| Persistence      | PostgreSQL 16 + Entity Framework Core (Npgsql)                    |
| Realtime         | ASP.NET Core SignalR (WebSockets)                                 |
| Auth             | Email + password, JWT bearer tokens                               |
| Client           | Avalonia UI 11 (Desktop, Android, iOS, Browser/WASM)              |
| Client MVVM      | CommunityToolkit.Mvvm                                             |
| Deployment       | Docker Compose (PostgreSQL + API)                                 |
| UI language      | English                                                           |

## Solution Structure

```
Omnia.sln
├─ src/
│  ├─ Omnia.Shared/          # contracts shared by API and client (no dependencies on either)
│  │    Contracts/  ClipDto, CreateClipRequest, RegisterRequest, LoginRequest, AuthResponse
│  │    Realtime/   IClipClient (hub methods), HubRoutes
│  │    Http/       ApiRoutes (route constants)
│  ├─ Omnia.Api/             # ASP.NET Core Web API
│  │    Endpoints/  AuthController, ClipsController
│  │    Hubs/       ClipHub : Hub<IClipClient>
│  │    Services/   IAuthService, IClipService (+ implementations)
│  │    Data/       OmniaDbContext, Migrations/
│  │    Auth/       JwtTokenService, PasswordHasher
│  │    Program.cs
│  └─ Omnia.Client/          # Avalonia UI (shared: all UI and logic live here)
│       Views/      LoginView, ClipboardView, AccountView
│       ViewModels/ LoginViewModel, ClipboardViewModel, AccountViewModel
│       Services/   IApiClient, IRealtimeClient, IClipboardService, ITokenStore
│       App.axaml
├─ src/
│  ├─ Omnia.Client.Desktop/  # platform head
│  ├─ Omnia.Client.Android/  # platform head
│  ├─ Omnia.Client.iOS/      # platform head
│  └─ Omnia.Client.Browser/  # platform head (WebAssembly)
└─ tests/
   └─ Omnia.Api.Tests/       # xUnit: service + endpoint integration tests
```

The client follows Avalonia's multi-platform layout (the `avalonia.xplat` template): a shared `Omnia.Client`
library holds every View, ViewModel, and Service, and thin platform "head" projects bootstrap it for each target.
Platform-specific service implementations (clipboard, token storage) are selected in each head.

Boundaries:

- `Omnia.Shared` depends on nothing in the solution. Both API and client compile against it, so the wire format
  cannot drift between the two.
- `Omnia.Api` owns the database and all business rules. The client never talks to PostgreSQL directly.
- Client ViewModels depend only on interfaces (`IApiClient`, `IRealtimeClient`, `IClipboardService`, `ITokenStore`),
  never on concrete HTTP or SignalR types, so they are unit-testable with fakes.

## Domain Model

Two tables. There is no `Room` table and no PIN.

### Users

| Column       | Type        | Notes                        |
|--------------|-------------|------------------------------|
| Id           | uuid        | Primary key                  |
| Email        | text        | Unique, case-insensitive     |
| PasswordHash | text        | ASP.NET Core password hash   |
| CreatedAt    | timestamptz |                              |

### Clips

| Column    | Type        | Notes                                    |
|-----------|-------------|------------------------------------------|
| Id        | uuid        | Primary key                              |
| UserId    | uuid        | Foreign key to Users.Id, indexed         |
| Content   | text        | The clip text                            |
| DeviceId  | text        | Nullable; identifies the sending device  |
| CreatedAt | timestamptz |                                          |

Rules:

- A clip belongs to exactly one user and is only ever visible to that user.
- Deleting a clip is permanent (hard delete).
- Every query is scoped by the authenticated user's `UserId`.

## Authentication

- `POST /auth/register` and `POST /auth/login` return a signed JWT and user info.
- Passwords are hashed with `Microsoft.AspNetCore.Identity.PasswordHasher`; the plaintext is never stored.
- The JWT carries the user id (`sub`) and email, and is signed with an HMAC secret from configuration.
- REST endpoints are protected with `[Authorize]` and the JWT bearer handler.
- The SignalR hub is authenticated with the same token, passed via the `access_token` query string.
- Tokens are stored client-side through `ITokenStore` (OS-protected file on desktop, `SecureStorage` on mobile,
  browser storage on WASM).

## REST API Surface

| Method | Route             | Auth | Body                     | Result                         |
|--------|-------------------|------|--------------------------|--------------------------------|
| POST   | /auth/register    | no   | email, password          | 201 `AuthResponse` (token+user)|
| POST   | /auth/login       | no   | email, password          | 200 `AuthResponse`             |
| GET    | /clips            | yes  | —                        | 200 `ClipDto[]` (newest first) |
| POST   | /clips            | yes  | content, deviceId        | 201 `ClipDto`                  |
| DELETE | /clips/{id}       | yes  | —                        | 204                            |

Errors are returned as RFC 7807 `ProblemDetails`.

## Realtime (SignalR)

- Hub endpoint: `/hubs/clips`.
- On connect, the server adds the connection to a group named after the user id, so a user's devices only receive
  their own events.
- After a successful REST `POST` or `DELETE`, the API broadcasts to that user's group:

```
IClipClient:
    OnClipCreated(ClipDto clip)
    OnClipDeleted(Guid id)
```

- Clients reconnect automatically (with backoff). Connection state is surfaced to the UI as Connected / Reconnecting.

## Client (Avalonia)

Single-window app, minimalist dark theme (pure black background, white text), no app bar or bottom navigation.

Screens:

1. **Login / Register** — email and password fields, toggle between sign in and sign up.
2. **Clipboard (main workspace)** — opens to an autofocused text field. A list of clips below, newest first.
   Tap a clip to copy it to the OS clipboard (with a transient toast). Swipe a clip to delete it locally and on the
   server. A read-only connection indicator shows Connected (green) or Reconnecting (orange).
3. **Account** — shows the signed-in email and a sign-out button.

Client services:

| Service          | Responsibility                                                        |
|------------------|-----------------------------------------------------------------------|
| `IApiClient`     | Typed `HttpClient`: register, login, get/create/delete clips          |
| `IRealtimeClient`| Wraps `HubConnection`: connect with token, raise clip events           |
| `IClipboardService` | Sets text on the OS clipboard (platform-specific implementation)   |
| `ITokenStore`    | Persists and reads the JWT across platforms                            |

ViewModels use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm.

## Data Flow

- **Sign in:** client posts credentials → receives JWT → stores it → connects the hub with the token → loads clips
  via `GET /clips`.
- **Create clip:** client optimistically clears the input → `POST /clips` → server saves and broadcasts
  `OnClipCreated` to the user's group → every device (including the sender) appends the returned clip.
- **Delete clip:** swiping calls `DELETE /clips/{id}` → server deletes and broadcasts `OnClipDeleted` → every device
  removes the clip.
- **Reconnect:** on a dropped connection the client shows Reconnecting (orange), retries, then re-fetches `GET /clips`
  to resync once reconnected.

## Security

- HTTPS in all deployed environments.
- Passwords hashed, never logged.
- JWT signed with a server-side secret; secret and connection string come from configuration/environment, not source.
- All clip operations are scoped to the authenticated user id; there is no cross-user access path.

## Error Handling

- API returns `ProblemDetails` with appropriate status codes (400 validation, 401 unauthenticated, 409 duplicate
  email, 500 unexpected).
- Client shows a non-blocking error message and keeps the input text when a create fails, so nothing is lost.
- Failed realtime delivery is recovered by the resync-on-reconnect step.

## Testing

- `Omnia.Api.Tests` uses xUnit with `WebApplicationFactory` for endpoint integration tests and `Testcontainers` for a
  real PostgreSQL instance.
- Service-layer unit tests cover auth (register/login, duplicate email, bad password) and clip CRUD scoping.
- Client ViewModel tests run against fake `IApiClient`/`IRealtimeClient` implementations.

## Deployment

The API is a self-contained relay with no dependency on any external service. It is deployment-agnostic: the same
image runs self-hosted for a single user or as a shared instance for many. The only thing that changes between
models is where it is hosted and what the clients' API base URL points at.

`docker-compose.yml` runs two services:

- `postgres` — PostgreSQL 16 with a named volume.
- `api` — the ASP.NET Core image, running EF Core migrations on start, listening on a published port.

Configuration via environment variables (`ConnectionStrings__Default`, `Jwt__Secret`, `Jwt__Issuer`,
`Jwt__Audience`). Clients are pointed at the API base URL through client configuration.

### Deployment models

| Model | Description | Extra considerations |
|-------|-------------|----------------------|
| Self-hosted | Each user runs the stack on their own machine, home server, or VPS. Data stays on infrastructure they control. | Manual TLS (reverse proxy), backing up the Postgres volume. |
| Shared instance | One operator runs the API and many users point their clients at it. | TLS (a reverse proxy) and CORS for the browser client. |

Multi-tenancy is inherent in both models: every clip is scoped to its owning user id, and realtime events are sent
only to that user's SignalR group, so accounts are isolated from each other by construction rather than by
configuration.

### Scale

A single API instance is the intended deployment. It broadcasts to its own live connections and needs no backplane;
for a small number of users, one instance and one PostgreSQL database are sufficient. Multi-instance scaling and its
accompanying backplane are deliberately out of scope.

## Out of Scope

- Media/file sharing (images, video, documents).
- OAuth or third-party sign-in providers.
- End-to-end encryption.
- Long-term archival or folder organization; data is treated as temporary.
- Cross-user sharing of clipboards (sharing is now tied to the account, not to a shared PIN).
- Multi-instance horizontal scaling and a SignalR backplane; a single API instance is the target deployment.
