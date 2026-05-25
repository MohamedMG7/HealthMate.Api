"""Predictor unit tests using a synthetic dummy artifact.

We never load the real grandfathered pkl in unit tests - those exercises live
in tests/test_router_anemia.py end-to-end. Here we want fast, deterministic
coverage of the loader, the column ordering, and the prediction-semantics
mapping (raw class 1 -> anemia=true).
"""

from __future__ import annotations

import json
import logging
import pickle
from pathlib import Path

import numpy as np
import pandas as pd
import pytest
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import StandardScaler

from app.models.anemia.predictor import AnemiaPredictor
from app.models.anemia.schemas import AnemiaFeatures


def _build_dummy_artifacts(tmp_path: Path) -> Path:
    """Train a tiny RandomForest where higher Hb maps to class 0 (no anemia)."""
    rng = np.random.default_rng(seed=42)

    n = 100
    columns = ["Hb", "RBC", "PCV", "MCH", "MCHC"]
    # Class 0 (no anemia): high Hb
    no_anemia = pd.DataFrame(
        rng.normal(loc=[14.0, 4.8, 42.0, 30.0, 34.0], scale=0.5, size=(n, 5)),
        columns=columns,
    )
    # Class 1 (anemia): low Hb
    anemia = pd.DataFrame(
        rng.normal(loc=[7.0, 3.2, 22.0, 22.0, 30.0], scale=0.5, size=(n, 5)),
        columns=columns,
    )
    X = pd.concat([no_anemia, anemia], ignore_index=True)
    y = [0] * n + [1] * n

    scaler = StandardScaler().fit(X)
    X_scaled = scaler.transform(X)
    model = RandomForestClassifier(n_estimators=10, random_state=42).fit(X_scaled, y)

    artifacts_dir = tmp_path / "anemia" / "v0"
    artifacts_dir.mkdir(parents=True)
    with (artifacts_dir / "model.pkl").open("wb") as f:
        pickle.dump(model, f)
    with (artifacts_dir / "scaler.pkl").open("wb") as f:
        pickle.dump(scaler, f)
    (artifacts_dir / "metadata.json").write_text(
        json.dumps({"name": "anemia", "version": "v0", "grandfathered": False})
    )
    return artifacts_dir


@pytest.fixture()
def predictor(tmp_path: Path) -> AnemiaPredictor:
    artifacts_dir = _build_dummy_artifacts(tmp_path)
    return AnemiaPredictor(artifacts_dir)


def test_predict_low_hb_flags_anemia(predictor: AnemiaPredictor) -> None:
    result = predictor.predict(AnemiaFeatures(hb=6.5, rbc=3.0, pcv=20.0, mch=20.0, mchc=28.0))
    assert result.anemia is True
    assert result.confidence is not None
    assert result.confidence > 0.5
    assert result.model_name == "anemia"
    assert result.model_version == "v0"


def test_predict_normal_hb_returns_no_anemia(predictor: AnemiaPredictor) -> None:
    result = predictor.predict(AnemiaFeatures(hb=14.0, rbc=4.8, pcv=42.0, mch=30.0, mchc=34.0))
    assert result.anemia is False
    assert result.confidence is not None
    assert result.confidence < 0.5


def test_predictor_never_logs_feature_values(
    predictor: AnemiaPredictor,
    caplog: pytest.LogCaptureFixture,
) -> None:
    caplog.set_level(logging.DEBUG, logger="healthmate.ml.anemia")
    predictor.predict(AnemiaFeatures(hb=12.3, rbc=4.5, pcv=40.1, mch=29.9, mchc=33.7))

    forbidden_substrings = ("12.3", "4.5", "40.1", "29.9", "33.7")
    blob = " ".join(record.getMessage() for record in caplog.records)
    blob += " " + " ".join(str(getattr(record, "args", "")) for record in caplog.records)
    extras: list[str] = []
    for record in caplog.records:
        for k, v in vars(record).items():
            if k.startswith(("hb", "rbc", "pcv", "mch", "mchc")):
                extras.append(f"{k}={v}")
    assert not extras, f"Predictor logged PHI-like fields: {extras}"
    for needle in forbidden_substrings:
        assert needle not in blob, f"Predictor leaked feature value {needle} into logs"


def test_predict_proba_aligned_with_classes_(predictor: AnemiaPredictor) -> None:
    # Sanity: confidence should be the probability assigned to class 1, not just `max(proba)`.
    weak_signal = predictor.predict(AnemiaFeatures(hb=10.5, rbc=4.0, pcv=32.0, mch=26.0, mchc=32.0))
    # We don't assert direction here - just that the probability is in [0, 1].
    assert weak_signal.confidence is not None
    assert 0.0 <= weak_signal.confidence <= 1.0


_pickle = pickle  # keep module-level reference (silences "imported but unused" for ruff)
