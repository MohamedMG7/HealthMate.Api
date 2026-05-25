# AGENTS.md

## Read First
Read `CONSTITUTION.md`, then this file. The Constitution's Never list is binding.

## What This Repo Is
HealthMate.Api is an ASP.NET Core 10 modular monolith API with PostgreSQL, a Python FastAPI sidecar for ML inference (`services/ml/`), and optional Gemini integration for Sina. The Angular 21 frontend lives in a separate `HealthMate.WebApp` repository.

## Where Things Live
| Path | Purpose |
| --- | --- |
| `src/HealthMate.Api` | ASP.NET Core API, controllers, middleware, app configuration |
| `src/HealthMate.Application/Modules/*` | Manager interfaces, managers, DTOs, and application module registration |
| `src/HealthMate.Application/Manager/MachineLearningManager/` | `IMlGateway` HTTP client to the FastAPI sidecar, thin adapter `MachineLearningManager` |
| `src/HealthMate.Infrastructure/Modules/*` | EF Core context, entities, configurations, repositories, and infrastructure module registration |
| `services/ml/` | Python FastAPI sidecar (own README) |
| `services/ml/app/models/<name>/` | Per-model Python code: predictor, schemas, router, train, evaluate |
| `services/ml/artifacts/<name>/<version>/` | Model artifacts + `metadata.json` (volume-mounted in compose) |
| `tests/` | xUnit integration and unit tests |
| `deploy/` | Local deployment support such as nginx config |

## Commands
Build (.NET): `dotnet build HealthMate.sln`

Test (.NET): `dotnet test HealthMate.sln`

Add migration: `dotnet ef migrations add <Name> --project src/HealthMate.Infrastructure --startup-project src/HealthMate.Api`

Run with Docker: `docker compose up --build`

Python ML deps: `cd services/ml && uv sync`

Test ML: `cd services/ml && uv run pytest`

Lint ML: `cd services/ml && uv run ruff check . && uv run ruff format --check . && uv run mypy app`

Run ML locally: `cd services/ml && uv run uvicorn app.main:app --reload --port 8000` (set `ML_ARTIFACTS_PATH` first)

## Hard Rules For Agents
Never invent endpoints, entity names, or DTOs. Grep first; if it does not exist, ask in the PR description before adding.

Never write tests with names, NationalIds, or free-text symptoms that look real. Use obvious fakes such as `Patient_Zero` and `00000000000000`.

Never disable a test or skip an `[Authorize]` to make a build pass.

When adding a service, register it in the owning module's `DependencyInjection.cs`. Do not edit `Program.cs` directly for module wiring.

When changing entity shape, generate a migration in the same PR.

If unsure, leave a `// TODO(agent):` comment and surface it in the PR description rather than guessing.

## ML-Specific Rules
Adding a new ML model REQUIRES:
1. A `train.py` that produces the artifact deterministically.
2. An `evaluate.py` that computes and records metrics on a held-out set.
3. A `metadata.json` next to the artifact with `dataset_source`, `framework_version`, `metrics`, and `trained_at`.
4. A row added to the root `README.md` Models table.

No exceptions. The anemia model is the only grandfathered case; see `services/ml/app/models/anemia/README.md`. Until anemia is de-grandfathered, no other clinical model may be added.

ML services must never log feature values, patient IDs, or response bodies. The `IMlGateway` request type must never carry `PatientId`. If you find yourself adding one, stop — the .NET edge is the right place for any patient-id-coupled work.

Predictor unit tests must build their own synthetic dummy artifact inside the test (`tmp_path` fixture) rather than loading the real grandfathered pkl. Router tests against the real artifact must use clearly synthetic feature values (`hb=4.5`, etc.) — never realistic patient data.

## Sina-Specific Rules
Adding a new LLM provider requires implementing `IClinicalLlmClient`, adding it to provider selection, adding fixture-based adapter tests, and documenting the model/version in the PR. Provider-specific types must stay under `src/HealthMate.Sina/Llm/Providers/`.

Adding a new Sina tool requires implementing `ISinaTool`, registering it in `AddSinaModule`, depending only on `ISinaClinicalReader`, and adding a unit test. Tools must never depend on HealthMate.Infrastructure or `I*Repo` directly.

`src/HealthMate.Sina/HealthMate.Sina.csproj` must not reference `HealthMate.Infrastructure`; this is the compile-time extraction guard.

Never call `IClinicalLlmClient.GenerateAsync` outside `SinaManager`. All Sina calls must pass through the manager so tool limits, safety filters, citation checks, and persistence apply.

Never inline raw patient PHI in Sina code, tests, fixtures, or logs. Tools return data; logs should only record non-PHI operational fields such as `patientId`, `hcpId`, `provider`, `toolName`, `latencyMs`, and `success`.

`IDrugInteractionLookup` is the boundary for upgrading to a real interaction database. Do not bake interaction logic into individual tools.

## Out Of Bounds
Never push to `main`, never run destructive DB commands, and never modify `CONSTITUTION.md` or `AGENTS.md` without an explicit human request.
