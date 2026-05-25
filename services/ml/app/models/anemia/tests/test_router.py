"""End-to-end router test against the real grandfathered artifact."""

from fastapi.testclient import TestClient


def test_router_requires_auth(client: TestClient) -> None:
    r = client.post(
        "/v1/predict/anemia",
        json={"hb": 12.0, "rbc": 4.5, "pcv": 38.0, "mch": 28.0, "mchc": 32.0},
        headers={"Authorization": ""},
    )
    assert r.status_code == 401


def test_router_severe_anemia_returns_true(client: TestClient) -> None:
    # Clearly synthetic severely-anemic CBC (Hb 4.5 g/dL).
    # Never use real patient data (CONSTITUTION).
    r = client.post(
        "/v1/predict/anemia",
        json={"hb": 4.5, "rbc": 3.2, "pcv": 22.0, "mch": 18.0, "mchc": 28.0},
    )
    assert r.status_code == 200
    body = r.json()
    assert body["anemia"] is True
    assert body["model_name"] == "anemia"
    assert body["model_version"] == "v0"
    assert body["confidence"] is None or 0.0 <= body["confidence"] <= 1.0


def test_router_normal_cbc_returns_false(client: TestClient) -> None:
    r = client.post(
        "/v1/predict/anemia",
        json={"hb": 14.0, "rbc": 4.8, "pcv": 42.0, "mch": 30.0, "mchc": 34.0},
    )
    assert r.status_code == 200
    assert r.json()["anemia"] is False


def test_router_rejects_out_of_range_values(client: TestClient) -> None:
    r = client.post(
        "/v1/predict/anemia",
        json={"hb": -1.0, "rbc": 4.5, "pcv": 38.0, "mch": 28.0, "mchc": 32.0},
    )
    assert r.status_code == 422
