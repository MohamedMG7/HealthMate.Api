# HealthMate ML sidecar

Python FastAPI service that owns clinical ML inference. The .NET API talks to
it over HTTP on the internal docker network; nothing routes here from the
public internet by design.

## What's here

```
services/ml/
  app/                FastAPI application
    main.py           Routes, lifespan, exception handler
    config.py         Env-driven settings (pydantic-settings)
    security.py       Bearer-token verification dependency
    registry.py       In-process model registry
    schemas/          Cross-cutting response schemas
    models/<name>/    One folder per ML model (see "Adding a new model")
  artifacts/<name>/<version>/   Model artifacts + metadata.json (volume-mounted)
  tests/              Top-level health, models-listing, fixtures
  pyproject.toml      Dependencies (managed with uv)
  Dockerfile          Multi-stage build (uv install -> slim runtime)
```

## Local dev

Requires Python 3.12 and [`uv`](https://github.com/astral-sh/uv). Plain pip
works too if you `uv export > requirements.txt` first.

```powershell
cd services/ml
uv sync                                    # install deps + create .venv
$env:ML_ARTIFACTS_PATH = (Resolve-Path .\artifacts).Path
$env:ML_ENV = "development"                # disables auth, exposes /docs
uv run uvicorn app.main:app --reload --port 8000
```

Quick checks:

```powershell
curl http://localhost:8000/health                                                     # {"status":"ok"}
curl http://localhost:8000/v1/models                                                  # lists registered models
curl -X POST http://localhost:8000/v1/predict/anemia -H "Content-Type: application/json" `
  -d '{"hb":4.5,"rbc":3.2,"pcv":22.0,"mch":18.0,"mchc":28.0}'                         # {"anemia":true, ...}
```

Tests + lint + types:

```powershell
uv run pytest -v
uv run ruff check .
uv run ruff format --check .
uv run mypy app
```

## Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `ML_ARTIFACTS_PATH` | `/artifacts` | Where the service looks for `<name>/<version>/{model.pkl, scaler.pkl, metadata.json}` triplets. |
| `ML_AUTH_TOKEN` | `` (empty) | Bearer token required on every endpoint except `/health`. Empty in `development` skips auth for local curl. Required in `production`. |
| `ML_ENV` | `development` | `development` exposes `/docs` + `/redoc` and allows empty auth token. `production` requires the token and hides docs. |
| `ML_LOG_LEVEL` | `INFO` | Standard `logging` level string. |

Logs **never** record feature values, request bodies, or response payloads. The
unhandled-exception handler also strips this. Per `CONSTITUTION.md`: ML services
must never log feature values, patient IDs, or response bodies.

## Adding a new model (mandatory recipe)

`CONSTITUTION.md` requires every new clinical model to ship with reproducible
training, evaluation, and recorded metrics. The anemia model is the **only**
grandfathered exception (see `app/models/anemia/README.md`); until that's
de-grandfathered, no other clinical model may be added.

When you do add one, follow this recipe:

1. **Source a dataset** with a clear license. Document URL + license at the
   top of `app/models/<name>/README.md`.
2. **`app/models/<name>/train.py`** loads the dataset, fits a classifier and
   scaler, and writes `artifacts/<name>/v1/{model.pkl, scaler.pkl, metadata.json}`
   deterministically. Use `joblib.dump` for new artifacts (sklearn-recommended).
3. **`app/models/<name>/evaluate.py`** computes `accuracy`, `precision`,
   `recall`, `f1`, `auc` on a held-out fold and writes them into
   `metadata.json.metrics`, with the held-out split definition recorded.
4. **`app/models/<name>/predictor.py`** loads the artifact and exposes
   `predict(features) -> Response`. Never log feature values.
5. **`app/models/<name>/schemas.py`** declares `<Name>Features` (request) and
   `<Name>Prediction` (response). Use `Field(ge=..., le=...)` for sanity bounds,
   not clinical reference ranges. **Do not include `patient_id`** — that's PHI
   minimization, see `CONSTITUTION.md`.
6. **`app/models/<name>/router.py`** declares `POST /v1/predict/<name>` with
   `verify_token` as a dependency.
7. **Register the model in `app/main.py`** — explicit import, explicit
   `registry.register()` in the lifespan. No auto-discovery.
8. **Tests** under `app/models/<name>/tests/`:
   - Predictor test with a tiny synthetic dummy artifact built in-test (don't
     load real pkls in unit tests).
   - Router test against the real artifact, using clearly synthetic feature
     values (`hb=4.5`, etc.) — never realistic patient data.
   - A PHI-no-leak test that asserts feature values never appear in `caplog`.
9. **On the .NET side**: add a method to `IMlGateway`, extend
   `IMachineLearningManager`, and either extend `EDEngineController` or add a
   new controller for the model.
10. **Update the root `README.md`** model table and this file's contributor
    list if you become a maintainer.

## Models

| Name | Version | Status | See |
| --- | --- | --- | --- |
| anemia | v0 | grandfathered | [`app/models/anemia/README.md`](app/models/anemia/README.md) |
