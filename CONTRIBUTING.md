# Contributing

## Local Setup
Use the quick-start in `README.md`.

## Branch Naming
Use `feat/<short>`, `fix/<short>`, `refactor/<short>`, or `docs/<short>`.

## Commits
Write imperative present tense: `add patient search index`, not `added patient search index`. Keep one logical change per commit and reference the issue number when one exists.

## Pull Requests
Use `.github/pull_request_template.md`. Explain what changed, why it changed, and how it was tested. Confirm tests were added, migrations were added when schema changed, no PHI appears in diffs, and `CONSTITUTION.md` rules were respected.

## Code Style
C# uses `dotnet format`. Frontend changes belong in the separate `HealthMate.WebApp` repository.

## Architecture
New aggregate code belongs in `src/HealthMate.Domain` with private setters, factory methods, and value objects for invariants. Use `ICommand<TResult>`/`IQuery<TResult>` plus one handler per use case in `src/HealthMate.Application`; keep EF Core and external adapters in `src/HealthMate.Infrastructure`.

## Tests And Migrations
Run `dotnet test HealthMate.sln`. Add migrations with `dotnet ef migrations add <Name> --project src/HealthMate.Infrastructure --startup-project src/HealthMate.Api`.

## New Modules Or ML Models
Open an issue with the proposal template before adding a module or ML model.
