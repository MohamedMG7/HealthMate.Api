from fastapi.testclient import TestClient


def test_models_listing_requires_auth(client: TestClient) -> None:
    r = client.get("/v1/models", headers={"Authorization": ""})
    assert r.status_code == 401


def test_models_listing_with_invalid_token(client: TestClient) -> None:
    r = client.get("/v1/models", headers={"Authorization": "Bearer wrong-token"})
    assert r.status_code == 401


def test_models_listing_returns_anemia(client: TestClient) -> None:
    r = client.get("/v1/models")
    assert r.status_code == 200
    payload = r.json()
    assert any(m["name"] == "anemia" and m["version"] == "v0" for m in payload)
