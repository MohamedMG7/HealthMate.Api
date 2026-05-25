# AGENTS.md

## Read First
Read `CONSTITUTION.md`, then this file. The Constitution's Never list is binding.

## What This Repo Is
HealthMate.Api is an ASP.NET Core 10 modular monolith API with PostgreSQL, a planned Python FastAPI sidecar for ML, and optional Gemini integration for Sina. The Angular 21 frontend lives in a separate `HealthMate.WebApp` repository.

## Where Things Live
| Path | Purpose |
| --- | --- |
| `src/HealthMate.Api` | ASP.NET Core API, controllers, middleware, app configuration |
| `src/HealthMate.Application/Modules/*` | Manager interfaces, managers, DTOs, and application module registration |
| `src/HealthMate.Infrastructure/Modules/*` | EF Core context, entities, configurations, repositories, and infrastructure module registration |
| `tests/` | xUnit integration and unit tests |
| `deploy/` | Local deployment support such as nginx config |

## Commands
Build: `dotnet build HealthMate.sln`

Test: `dotnet test HealthMate.sln`

Add migration: `dotnet ef migrations add <Name> --project src/HealthMate.Infrastructure --startup-project src/HealthMate.Api`

Run with Docker: `docker compose up --build`

## Hard Rules For Agents
Never invent endpoints, entity names, or DTOs. Grep first; if it does not exist, ask in the PR description before adding.

Never write tests with names, NationalIds, or free-text symptoms that look real. Use obvious fakes such as `Patient_Zero` and `00000000000000`.

Never disable a test or skip an `[Authorize]` to make a build pass.

When adding a service, register it in the owning module's `DependencyInjection.cs`. Do not edit `Program.cs` directly for module wiring.

When changing entity shape, generate a migration in the same PR.

If unsure, leave a `// TODO(agent):` comment and surface it in the PR description rather than guessing.

## Out Of Bounds
Never push to `main`, never run destructive DB commands, and never modify `CONSTITUTION.md` or `AGENTS.md` without an explicit human request.
