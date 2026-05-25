"""Shared fixtures for the ML service tests.

The fixtures load the FastAPI app against the real `services/ml/artifacts/`
directory so tests exercise the same wiring as production. The bearer token is
set to a synthetic string and the matching header is preset on the TestClient.
"""

from __future__ import annotations

import os
from collections.abc import Iterator
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

_TEST_TOKEN = "test-token"


@pytest.fixture(scope="session")
def artifacts_path() -> Path:
    return (Path(__file__).resolve().parent / "artifacts").resolve()


@pytest.fixture(scope="session")
def auth_token() -> str:
    return _TEST_TOKEN


@pytest.fixture(scope="session")
def client(artifacts_path: Path) -> Iterator[TestClient]:
    os.environ["ML_ARTIFACTS_PATH"] = str(artifacts_path)
    os.environ["ML_ENV"] = "production"
    os.environ["ML_AUTH_TOKEN"] = _TEST_TOKEN

    # Reset cached registry between sessions (otherwise re-running tests in the
    # same process double-registers the model and crashes lifespan).
    from app import registry as registry_module

    registry_module._registry = registry_module.ModelRegistry()

    from app.main import app

    with TestClient(app) as c:
        c.headers["Authorization"] = f"Bearer {_TEST_TOKEN}"
        yield c
