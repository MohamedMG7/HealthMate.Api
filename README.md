# HealthMate

HealthMate.Api is the backend for the open-source HealthMate EHR/CDSS for doctors and small clinics. It focuses on patient records, encounters, clinical observations, lab tests, prescriptions, documents, messaging, mental health assessments, and decision-support features. Read `CONSTITUTION.md` before contributing.

## Architecture
HealthMate.Api is a .NET 10 modular monolith backed by PostgreSQL, with modules composed in-process through `AddXxxModule` extensions. The Angular 21 frontend lives in the separate `HealthMate.WebApp` repository. Classical ML runs as a Python FastAPI sidecar (`services/ml/`) on the internal docker network; the .NET API talks to it through an `IMlGateway` HTTP client. Sina is a provider-side clinical assistant in `src/HealthMate.Sina/` with session memory, tool calling, citations, and Gemini/OpenAI adapters behind one LLM contract.

## Quick Start With Docker
```bash
cp .env.example .env
docker compose up --build
```

Swagger: `http://localhost:8080/swagger`

Fill `Jwt__Key` in `.env` before using auth-protected flows. Generate one with PowerShell: `[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))`.

## Quick Start With SDK
```bash
docker compose up -d postgres
dotnet restore HealthMate.sln
dotnet ef database update --project src/HealthMate.Infrastructure --startup-project src/HealthMate.Api
dotnet run --project src/HealthMate.Api
```

## Configuration
| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Sina__Provider` | Sina LLM provider: `Gemini` or `OpenAi` |
| `Sina__MaxToolCallsPerTurn` | Maximum Sina tool calls per provider message turn |
| `Sina__Gemini__ApiKey` | Optional Gemini API key for Sina |
| `Sina__Gemini__BaseUrl` | Gemini API base URL |
| `Sina__Gemini__Model` | Gemini model name |
| `Sina__OpenAi__ApiKey` | Optional OpenAI API key for Sina |
| `Sina__OpenAi__BaseUrl` | OpenAI-compatible API base URL |
| `Sina__OpenAi__Model` | OpenAI model name, default `gpt-4o-mini` |
| `EmailSettings__Email` | SMTP sender email |
| `EmailSettings__Password` | SMTP password |
| `Cors__AllowedOrigins__0` | First allowed frontend origin |
| `POSTGRES_USER` | Docker PostgreSQL user |
| `POSTGRES_PASSWORD` | Docker PostgreSQL password |
| `POSTGRES_DB` | Docker PostgreSQL database |
| `ML_AUTH_TOKEN` | Bearer token the ML sidecar requires. Must match `MlService__AuthToken`. |
| `MlService__BaseUrl` | URL the API uses to reach the ML sidecar (`http://ml:8000` in compose). |
| `MlService__AuthToken` | Bearer token the API sends to the ML sidecar. Must match `ML_AUTH_TOKEN`. |

ASP.NET Core reads nested environment variables with double underscores, such as `Jwt__Key`.

## Tests
```bash
dotnet test HealthMate.sln
```

## Sina Clinical Assistant
Sina is session-based and provider-only. It preloads a chart summary, persists turns to PostgreSQL, can call structured chart tools, and asks the selected LLM provider through `IClinicalLlmClient`.

| Endpoint | Purpose |
| --- | --- |
| `POST /api/Sina/sessions` | Open or resume a patient/provider Sina session. Body: `{ "patientId": 42 }`. |
| `POST /api/Sina/sessions/{sessionId}/messages` | Send a user message. Body: `{ "content": "..." }`. |
| `DELETE /api/Sina/sessions/{sessionId}` | Close a Sina session. |

Sina sends substantially more chart context to the configured LLM provider than the old prompt-only flow. Use provider accounts and project settings that match the deployment's privacy and BAA/data-handling requirements.

## Project Layout
| Path | Purpose |
| --- | --- |
| `src/HealthMate.Api` | API host, controllers, and API middleware |
| `src/HealthMate.Domain` | Domain primitives, aggregate roots, value objects, and repository ports |
| `src/HealthMate.Application` | Commands, queries, handlers, validation/logging pipeline, remaining legacy managers |
| `src/HealthMate.Infrastructure` | EF Core, legacy entities, repository adapters, migrations |
| `src/HealthMate.Sina` | Sina LLM contracts, tools, session orchestrator, and extraction boundary |
| `tests/HealthMate.Tests` | Test baseline |
| `services/ml/` | Python FastAPI sidecar for ML inference. See `services/ml/README.md`. |
| `deploy/` | Deployment support |

New aggregate work should follow the Patient aggregate pattern: invariants in Domain, use cases in Application handlers, EF adapters/configuration in Infrastructure, and thin API controllers.

## Models

| Name | Version | Status | Where it lives |
| --- | --- | --- | --- |
| anemia | v0 | grandfathered | `services/ml/app/models/anemia/` + `services/ml/artifacts/anemia/v0/` |

Adding a new clinical model is gated by `CONSTITUTION.md`: every new model needs a `train.py`, an `evaluate.py`, and a `metadata.json` with held-out metrics. The recipe lives in `services/ml/README.md`. Until anemia is de-grandfathered, no other model may be added.

## Contributing
Read `CONTRIBUTING.md`, `AGENTS.md`, and `CONSTITUTION.md`.

## License
MIT. See `LICENSE`.
