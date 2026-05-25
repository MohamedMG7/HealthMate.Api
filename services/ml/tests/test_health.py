from fastapi.testclient import TestClient


def test_health_returns_ok(client: TestClient) -> None:
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json() == {"status": "ok"}


def test_health_does_not_require_auth(client: TestClient) -> None:
    r = client.get("/health", headers={"Authorization": ""})
    assert r.status_code == 200
