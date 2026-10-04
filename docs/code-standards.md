# Code Standards — Omnia

## Source of truth

At planning time (2026-10-01) the repository contained **no application code**, only the spec
(`Architecture.MD`, `README.MD`). These standards are therefore seeded from the spec plus standard
.NET / Avalonia conventions. Re-run the code-standards scan once real code exists to reconcile
against actual practice.

## C# / .NET style

- **PascalCase:** classes, records, interfaces (`I`-prefixed), enums, public members, methods, namespaces,
  properties, and any type bound in XAML.
- **camelCase:** local variables, parameters, and private fields.
  - Note: the original spec (a Dart/Flutter project) allowed camelCase *methods*; for .NET we follow the
    platform convention and use **PascalCase methods**. This is a deliberate deviation.
- **No comments unless truly necessary.** Prefer clear names over explanatory comments.
- **Line length:** keep under 150 characters.
- Enable **nullable reference types**. Avoid the null-forgiving `!` operator unless the invariant is obvious
  and locally justified.
- Async methods are suffixed `Async`, return `Task`/`Task<T>`, and accept a `CancellationToken` when the
  operation can be cancelled. Do not use `ConfigureAwait(false)` in ASP.NET Core.
- Use `var` only when the type is apparent from the right-hand side.

## Project conventions

- One public type per file; the file name matches the type name.
- **DTOs** are `record` types in `Omnia.Shared` and carry no EF attributes.
- **API controllers** are thin: validate, delegate to a service, return a result. Business logic lives in
  `Services/`.
- **EF Core** entity types and the `DbContext` live under `Omnia.Api/Data`, with migrations in
  `Data/Migrations`. Entity types are not exposed over the wire — map to shared DTOs.
- **Secrets** (JWT key, connection string) come from configuration/environment variables, never source.
- Client ViewModels depend on interfaces only (`IApiClient`, `IRealtimeClient`, `IClipboardService`,
  `ITokenStore`), never on concrete HTTP/SignalR types.

## API conventions

- Routes are lowercase and plural (`/clips`).
- Errors are returned as RFC 7807 `ProblemDetails` with correct status codes
  (400 validation, 401 unauthenticated, 404 not found, 409 conflict, 500 unexpected).
- All clip operations are scoped to the authenticated user id.

## Testing

- Framework: **xUnit**.
- Test names: `MethodUnderTest_Scenario_ExpectedResult`.
- API integration tests use `WebApplicationFactory`; the database runs via **Testcontainers** (real
  PostgreSQL), so Docker must be available.
- Client ViewModel tests run against fake `IApiClient` / `IRealtimeClient` implementations — no network.
