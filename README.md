# HealthMate

HealthMate.Api is the backend for the open-source HealthMate EHR/CDSS for doctors and small clinics. It focuses on patient records, encounters, clinical observations, lab tests, prescriptions, documents, messaging, mental health assessments, and decision-support features. Read `CONSTITUTION.md` before contributing.

## Architecture
HealthMate.Api is a .NET 10 modular monolith backed by PostgreSQL, with modules composed in-process through `AddXxxModule` extensions. The Angular 21 frontend lives in the separate `HealthMate.WebApp` repository. Classical ML will move to a Python FastAPI sidecar in a later plan; Sina remains optional and Gemini-backed for now.

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
| `Gemini__ApiKey` | Optional Gemini API key for Sina |
| `Gemini__BaseUrl` | Gemini API base URL |
| `EmailSettings__Email` | SMTP sender email |
| `EmailSettings__Password` | SMTP password |
| `Cors__AllowedOrigins__0` | First allowed frontend origin |
| `POSTGRES_USER` | Docker PostgreSQL user |
| `POSTGRES_PASSWORD` | Docker PostgreSQL password |
| `POSTGRES_DB` | Docker PostgreSQL database |

ASP.NET Core reads nested environment variables with double underscores, such as `Jwt__Key`.

## Tests
```bash
dotnet test HealthMate.sln
```

## Project Layout
| Path | Purpose |
| --- | --- |
| `src/HealthMate.Api` | API host and controllers |
| `src/HealthMate.Application` | Application managers and DTOs |
| `src/HealthMate.Infrastructure` | EF Core, entities, repositories, migrations |
| `tests/HealthMate.Tests` | Test baseline |
| `deploy/` | Deployment support |

## Known Limitation
The EDEngine anemia ML endpoint requires a local Python install and `.pkl` files in the source tree. It is not expected to work inside the API Docker image until the FastAPI sidecar lands.

## Contributing
Read `CONTRIBUTING.md`, `AGENTS.md`, and `CONSTITUTION.md`.

## License
MIT. See `LICENSE`.
