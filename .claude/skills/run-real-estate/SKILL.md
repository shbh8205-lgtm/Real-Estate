---
name: run-real-estate
description: Build, run, and smoke-test the Real Estate app — the .NET 8 API (RealEstate.Api, https://localhost:7144) and the Angular frontend (RealEstate-Angular, http://localhost:4200). Use when asked to run, start, launch, build, serve, or smoke-test this app, or to verify the AI chat works end-to-end.
---

# Run: Real Estate

Two units that run together:

- **API** — .NET 8 / EF Core / MediatR, SQL Server **LocalDB**, OpenRouter-backed AI chat. Listens on `https://localhost:7144` (and `http://localhost:5022`).
- **Frontend** — Angular (standalone + signals), dev server on `http://localhost:4200`, calls the API at `https://localhost:7144`.

The agent path is the API + the **`smoke.ps1`** driver (register → login → AI chat). All paths below are relative to the repo root. Commands are **Windows PowerShell**.

## Prerequisites

- .NET 8 SDK, Node.js + npm, SQL Server **LocalDB** (`sqllocaldb info` should list `MSSQLLocalDB`).
- EF Core tools — verify with `dotnet ef --version`. If missing: `dotnet tool install --global dotnet-ef`.
- A trusted HTTPS dev cert (or use `curl -k`): `dotnet dev-certs https --trust`.

## Setup (once)

Secrets are intentionally **blank** in `RealEstate.Api/appsettings.json` — set them via user-secrets (they stay out of git):

```powershell
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-v1-YOUR-KEY" --project RealEstate.Api
dotnet user-secrets set "Jwt:Key" "a-long-random-string-at-least-32-chars" --project RealEstate.Api
dotnet user-secrets list --project RealEstate.Api   # verify both are set
```

Create/upgrade the database (the app does **not** auto-migrate at startup):

```powershell
dotnet ef database update --project RealEstate.Infrastructure --startup-project RealEstate.Api
```

## Run — agent path (API + smoke driver)

Launch the API (explicit env form = no auto-browser). Run it in a background shell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "https://localhost:7144;http://localhost:5022"
dotnet run --project RealEstate.Api --no-launch-profile
```

Wait for `Now listening on: https://localhost:7144`, then drive it end-to-end:

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/run-real-estate/smoke.ps1
```

Expected output ends with `SMOKE OK: chat is working end-to-end (1 suggestion(s)).` and exit code 0. The driver registers a user, logs in, and POSTs a Hebrew "apartment in Tel Aviv" chat message; success means the AI parsed it **and** the cross-language city match returned the (English-addressed) Tel Aviv listing. The Hebrew request body lives in `chat-body.json` (POSTed with `curl --data-binary`).

## Run — frontend (Angular)

```powershell
cd RealEstate-Angular
npm install        # only if node_modules is missing
npm start          # ng serve -> http://localhost:4200
```

Wait for `Application bundle generation complete`, then open `http://localhost:4200` (login, then the chat page). The frontend talks to the already-running API on `:7144`.

## Gotchas (battle scars from this session)

- **Chat falls back to "קושי זמני להתחבר…"** → OpenRouter returned **402 insufficient credits**: the request must cap `max_tokens` (else it assumes the model's 65K max, which a free account can't afford). Already fixed in `ChatHandler.cs`/`OpenAiPropertyAnalyst.cs`. `smoke.ps1` fails loudly if this regresses.
- **No Hebrew literals in `.ps1` files.** Windows PowerShell 5.1 reads BOM-less `.ps1` in the local codepage and corrupts UTF-8 Hebrew (parser errors). Keep Hebrew in separate UTF-8 `.json` fixtures and POST with `curl --data-binary @file`. Same reason inline `curl -d '{...עברית...}'` yields a 400 "could not be converted to String" — the console codepage mangles it.
- **App does not auto-migrate.** `Program.cs` has no `db.Database.Migrate()`; run `dotnet ef database update` yourself or the first DB call 500s.
- **`UseHttpsRedirection` is on.** Hitting `http://localhost:5022` 307-redirects to https; just call `https://localhost:7144` directly (with `curl -k` for the dev cert).
- **CORS allows only `http://localhost:4200`.** The frontend must run on that exact origin.
- **City matching is cross-language** (Hebrew query ↔ English address) via the AI returning both languages plus an alias map in `ChatHandler.cs`. Seeded data mixes languages (e.g. `Dizengoff 100, Tel Aviv` vs `הזית, 8, עפולה`).

## Troubleshooting

| Symptom | Fix |
|---|---|
| Login returns empty / 401 | `Jwt:Key` not set in user-secrets (it's blank in appsettings) |
| Chat returns the "קושי זמני" fallback | OpenRouter 402 (no credits) or bad/empty `OpenRouter:ApiKey`; verify with a direct `curl` to `https://openrouter.ai/api/v1/chat/completions` including `"max_tokens":1000` |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef` |
| Port 7144 already in use | stop the other API instance |
| `smoke.ps1` parser errors | you reintroduced a non-ASCII literal into the `.ps1` — keep it ASCII-only |

## Human path

`dotnet run --project RealEstate.Api --launch-profile https` opens Swagger in a browser; Ctrl-C to stop. Useful for manual poking, not for headless/agent runs.
