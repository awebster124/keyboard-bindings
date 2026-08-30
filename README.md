# Keyboard Key Remapping Service

A small ASP.NET Core service that stores and serves key remappings, with keys
identified by their USB HID usage codes (Usage Page 0x07, per the USB HID Usage
Tables spec). It currently supports the **Apex Pro Gen 3** and is built to extend to
other keyboards — see [Adding a keyboard](#adding-a-keyboard).

## Stack

- **.NET 10** / ASP.NET Core minimal API
- **SQLite** persistence via **Entity Framework Core**
- **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` for unit and integration tests

All dependencies are Microsoft-owned and free.

## Layout

```
KeyboardBindings.Api/
  Domain/         SupportedKeyboards — the keyboard allowlist
  Hid/            HidKey + HidCatalog — the 92 valid keys
  Data/           KeyMapping, AppDbContext, SQLite PRAGMA interceptor, migrations
  Contracts/      Request & response DTOs
  Services/       MappingService — validation + persistence
  Observability/  MappingMetrics — the conflict counter
  Http/           Security-headers middleware
  Program.cs      DI wiring, pipeline, endpoints
KeyboardBindings.Tests/   HID, service, API, and concurrency tests
```

## Running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) — no database
server or other infrastructure.

```bash
dotnet run --project KeyboardBindings.Api
```

On startup the app applies EF Core migrations, creating the schema and seeding
every supported keyboard with an identity mapping for each key. The SQLite database
(`keyboardbindings.db` by default, set via `ConnectionStrings:Default`) is created
automatically and persists between runs. OpenAPI is served at `/openapi/v1.json` in
Development.

Schema changes use the EF Core CLI (`dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add <Name> --project KeyboardBindings.Api
dotnet ef database update --project KeyboardBindings.Api
```

## API

Errors are returned as **RFC 7807 `application/problem+json`**.

### `GET /keyboards/{name}/mappings`

Returns every key with the key it currently emits (remapped or not).

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

### `PUT /keyboards/{name}/mappings`

Saves the **complete** set of non-identity remaps: any key not listed is reset to
emit itself, so an empty list restores defaults. `from`/`to` accept hex (`"0x04"`)
or decimal (`"4"`).

```json
{
  "mappings": [
    { "from": "0x04", "to": "0x1D" },   // A -> Z
    { "from": "0x21", "to": "0x1F" }    // 4 -> 2
  ]
}
```

| Status | Meaning |
|--------|---------|
| `204 No Content` | Saved |
| `400 Bad Request` | Invalid mappings (unknown/duplicate key, null or missing entry, over cap) |
| `404 Not Found` | Unsupported keyboard |
| `503 Service Unavailable` | Sustained write contention — retry |

Validation failures are `ValidationProblemDetails` with messages grouped under `mappings`.

### `GET /health`

`200 Healthy` when the app can reach the database, for liveness/readiness probes.

## How it works

- **Every key is stored.** Each keyboard is seeded via migration with an identity
  mapping for every key, so the table is always complete and *Get* returns the full
  keyboard. Migration-time seeding avoids any seeding race.
- **Assign is a full replace, validated all-or-nothing.** Any unknown key, duplicate
  source, null entry, or over-cap size rejects the whole request and persists
  nothing — so stored state is a direct function of the last successful request
  (predictable and idempotent).
- **Concurrency is last-write-wins.** Each row carries an optimistic-concurrency
  token stamped on every save. On a conflict the service reloads and reapplies the
  request (safe, since it's a full replacement); retries are bounded, and sustained
  contention returns **503 + `Retry-After`**.
  Every conflict increments a counter (`MappingMetrics`, via `dotnet-counters`).
  SQLite runs in **WAL** mode with a `busy_timeout`, so readers don't block the
  writer and a writer waits briefly instead of failing with `SQLITE_BUSY`.

## Security

- **Allowlisted, parameterized input** — keyboard names matched against a fixed
  allowlist; key codes parsed to `byte`; all DB access via EF Core LINQ; DTOs mapped
  explicitly (no mass-assignment).
- **Bounded payloads** — 64 KB body cap; `mappings` rejected above the key count.
- **Transport & headers** — HTTPS redirect always; HSTS outside Development;
  `X-Content-Type-Options: nosniff` on every response.
- **No CORS**; **errors don't leak internals** (generic 500 in Production).
- **No auth — deferred** — Anyone who can reach the service can read or
  overwrite any keyboard's mappings. Next steps (bearer tokens / OIDC)
  would scope mappings per user.

## Adding a keyboard

1. Add it to `SupportedKeyboards.All` (gates validation).
2. Generate a seed migration: `dotnet ef migrations add Add<Model>`.

Skipping step 2 leaves the model valid but unseeded; reads fall back to identity and
a write recreates the missing row (with a warning), but the migration is the
intended path.

## Testing

```bash
dotnet test
```

Covers HID parsing; service logic against real SQLite (remap, full-replace,
validation, case-insensitive names, missing-row recovery); concurrency (conflict
detection and last-write-wins); and the HTTP endpoints end-to-end via
`WebApplicationFactory`. 

Tests never touch the real database — unit tests use an
in-memory SQLite connection, integration tests a throwaway temp file.
