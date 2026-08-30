# Keyboard Key Remapping Service

A small ASP.NET Core web service that stores and serves key remappings for the
**Apex Pro Gen 3** keyboard. Keys are identified by their USB HID usage codes
(Usage Page 0x07, per the USB HID Usage Tables specification).

## Stack

- **.NET 9** / ASP.NET Core minimal API
- **SQLite** persistence via **Entity Framework Core**
- **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` for unit and integration tests

All dependencies are Microsoft-owned and free — nothing licensed is required.

## Project layout

```
KeyboardBindings.sln
KeyboardBindings.Api/
  Domain/         SupportedKeyboards (dependency-free reference data)
  Hid/            HidKey + HidCatalog (the 92 valid keys, per the USB HID spec)
  Data/           KeyMapping entity, AppDbContext, SQLite PRAGMA interceptor,
                  design-time factory for `dotnet ef`
  Migrations/     EF Core migrations, including the identity-mapping seed data
  Contracts/      Request & response DTOs
  Services/       MappingService (validation + persistence), MappingResult
  Observability/  MappingMetrics (the last-write-wins conflict counter)
  Http/           Security-headers middleware
  Program.cs      DI wiring, middleware pipeline, and the HTTP endpoints
KeyboardBindings.Tests/
  HidCatalogTests, MappingServiceTests, MappingsApiTests, ConcurrencyTests
```

## Running

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download) — no database
server or other infrastructure.

```bash
dotnet run --project KeyboardBindings.Api
```

On startup the app applies pending **EF Core migrations**, which create the schema
and seed every supported keyboard with an identity mapping for each key. The
SQLite database (`keyboardbindings.db`, or `ConnectionStrings:Default`) is created
automatically and **persists between runs**. OpenAPI is served at
`/openapi/v1.json` in Development.

Changing the schema needs the EF Core CLI tool (`dotnet tool install --global
dotnet-ef`):

```bash
dotnet ef migrations add <Name> --project KeyboardBindings.Api
dotnet ef database update --project KeyboardBindings.Api
```

## API

### Get all key mappings

Returns every key on the keyboard with the key it currently emits (remapped or not).

```
GET /keyboards/{name}/mappings
```

```json
{
  "keyboard": "Apex Pro Gen 3",
  "mappings": [
    { "physicalKey": { "code": 4,  "hex": "0x04", "name": "A" },
      "mappedKey":   { "code": 29, "hex": "0x1D", "name": "Z" },
      "isRemapped": true },
    { "physicalKey": { "code": 5,  "hex": "0x05", "name": "B" },
      "mappedKey":   { "code": 5,  "hex": "0x05", "name": "B" },
      "isRemapped": false }
  ]
}
```

### Assign key mappings

Validates and saves the remappings. The request is the **complete** set of
non-identity remaps: any key not listed is reset to emit itself, so an empty list
restores the keyboard to its default state.

```
PUT /keyboards/{name}/mappings
Content-Type: application/json

{
  "mappings": [
    { "from": "0x04", "to": "0x1D" },   // A -> Z
    { "from": "0x21", "to": "0x1F" }    // 4 -> 2
  ]
}
```

`from`/`to` accept hex (`"0x04"`) or decimal (`"4"`) strings.

| Status | Meaning |
|--------|---------|
| `204 No Content` | Saved successfully |
| `400 Bad Request` | Validation failed (unknown key, duplicate source, null entry, too many mappings) |
| `404 Not Found` | Unsupported keyboard |
| `503 Service Unavailable` | Sustained write contention; retry (see Concurrency) |

Errors are returned as **RFC 7807 `application/problem+json`**. Validation
failures use `ValidationProblemDetails` with messages grouped under `mappings`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid key mappings",
  "status": 400,
  "errors": { "mappings": ["mappings[0].from: '0xFF' is not a valid HID key on this keyboard."] }
}
```

### Health

`GET /health` returns `200 Healthy` when the app can reach the SQLite database
(`AddDbContextCheck`), for liveness/readiness probes.

## Concurrency (last-write-wins)

Each `KeyMapping` row carries an optimistic-concurrency token (`Version`), stamped
on every save. If another writer changes a row between our read and our save, EF
raises `DbUpdateConcurrencyException`; the service then reloads current state and
reapplies the request (deterministic, since it's a full replacement), so the latest
write wins on fresh data rather than a stale read. Retries are bounded; if
contention is somehow sustained past the budget, the request returns
**503 + `Retry-After`**.

Returning 409 for the client to retry would be redundant here: a full-replacement
PUT has nothing to reconcile, so the retry belongs on the server. Every conflict
increments a counter (`MappingMetrics`, observable via `dotnet-counters`) and logs
a warning, so contention is measurable rather than silent.

At the storage layer SQLite runs in **WAL** mode with a `busy_timeout`
(`SqlitePragmaInterceptor`): readers don't block the writer, and a writer waits
briefly instead of failing with `SQLITE_BUSY`.

## Security

- **Input is allowlisted and parameterized.** The keyboard name is matched against
  a fixed allowlist and never reaches a query; key codes are parsed to `byte`
  before use; all DB access is EF Core parameterized LINQ. DTOs are minimal records
  mapped explicitly (no mass-assignment).
- **Bounded payloads.** The body is capped at 64 KB, and the `mappings` array is
  rejected above the keyboard's key count.
- **Transport & headers.** HTTPS redirection always; HSTS in non-Development;
  `X-Content-Type-Options: nosniff` on every response.
- **CORS is intentionally omitted** — the client is desktop software, not a browser.
- **Errors don't leak internals** — a generic 500 in Production (stack traces only
  on the dev exception page).

**Deferred (deliberately): authentication & authorization.** With no auth, anyone
who can reach the service can read or overwrite any keyboard's mappings — the most
significant gap, and the natural next step (e.g. bearer tokens / OIDC), after which
mappings would be scoped per user.

## Design notes

- **Every key is stored.** Each keyboard is seeded via an EF migration (once,
  atomically) with an identity mapping for every key, so the table is complete and
  inspectable and *Get* always returns the full keyboard. Migration-time seeding
  removes any seeding race.
- **All-or-nothing validation** — a request with any unknown key, duplicate source,
  null entry, or an over-cap size is rejected whole; nothing is persisted.
- **Assign is a full replace**, making stored state a direct function of the last
  successful request (predictable and idempotent).

### Adding a keyboard model

Both steps are required:

1. Add the model to `SupportedKeyboards.All` (gates request validation).
2. Generate a migration so its rows are seeded: `dotnet ef migrations add Add<Model>`.

Skipping step 2 leaves the model valid but unseeded; the service degrades rather
than failing (reads fall back to identity, a write recreates the missing row and
logs a warning), but the seed migration is the intended path.

## Testing

```bash
dotnet test
```

Covers the HID catalog and parsing; service logic against real SQLite (remap,
full-replace, validation including null entries and over-cap size, case-insensitive
names, missing-row recovery); concurrency (token detection, last-write-wins
resolution, and stamping across all save paths); and the HTTP endpoints end-to-end
via `WebApplicationFactory` (including malformed payloads).

Tests never touch the app's real database — the service tests use an in-memory
SQLite connection, and the integration tests use a throwaway temp file deleted on
dispose.
