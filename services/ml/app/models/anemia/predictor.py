import json
import logging
import pickle  # noqa: S403 - required for grandfathered .pkl artifact
import time
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

import pandas as pd

from app.models.anemia.schemas import AnemiaFeatures, AnemiaPrediction

logger = logging.getLogger("healthmate.ml.anemia")

# Feature column order MUST match the StandardScaler's feature_names_in_
# from the grandfathered artifact. Confirmed in Plan #2 Phase 0.3.
_FEATURE_COLUMNS = ["Hb", "RBC", "PCV", "MCH", "MCHC"]

# The model's positive class for "patient has anemia" is 1.
# The legacy .NET code shipped an inverted mapping
# (`prediction = result.Trim() == "0"`), which meant healthy CBCs were flagged
# as anemic and severely anemic CBCs were marked healthy. See
# README.md "Bug fix on de-grandfathering" for the smoking gun (Hb=4.5 returned
# `anemia=False, confidence=0.0`; Hb=14 returned `anemia=True, confidence=0.99`).
# We correct the mapping here rather than preserve the bug, because patient
# safety beats bug-for-bug compatibility (CONSTITUTION).
_ANEMIA_POSITIVE_RAW_CLASS = 1


class AnemiaPredictor:
    name = "anemia"

    def __init__(self, artifacts_dir: Path) -> None:
        self._artifacts_dir = artifacts_dir
        self._model: Any
        self._scaler: Any
        self._metadata: dict[str, Any]
        self.version: str
        self._has_proba: bool
        self._load()

    def _load(self) -> None:
        model_path = self._artifacts_dir / "model.pkl"
        scaler_path = self._artifacts_dir / "scaler.pkl"
        meta_path = self._artifacts_dir / "metadata.json"

        with model_path.open("rb") as f:
            # encoding="latin1" matches the original Animea.py loader.
            self._model = pickle.load(f, encoding="latin1")  # noqa: S301
        with scaler_path.open("rb") as f:
            self._scaler = pickle.load(f, encoding="latin1")  # noqa: S301
        with meta_path.open(encoding="utf-8") as f:
            self._metadata = json.load(f)

        self.version = self._metadata["version"]
        self._has_proba = hasattr(self._model, "predict_proba")

    def predict(self, features: AnemiaFeatures) -> AnemiaPrediction:
        started = time.perf_counter()
        # Use DataFrame with column names so StandardScaler.feature_names_in_ matches.
        frame = pd.DataFrame(
            [
                {
                    "Hb": features.hb,
                    "RBC": features.rbc,
                    "PCV": features.pcv,
                    "MCH": features.mch,
                    "MCHC": features.mchc,
                }
            ],
            columns=_FEATURE_COLUMNS,
        )
        scaled = self._scaler.transform(frame)

        raw_class = int(self._model.predict(scaled)[0])
        anemia = raw_class == _ANEMIA_POSITIVE_RAW_CLASS

        confidence: float | None = None
        if self._has_proba:
            proba = self._model.predict_proba(scaled)[0]
            # proba index aligned with model.classes_
            classes = list(self._model.classes_)
            try:
                idx = classes.index(_ANEMIA_POSITIVE_RAW_CLASS)
                confidence = float(proba[idx])
            except ValueError:
                confidence = None

        latency_ms = (time.perf_counter() - started) * 1000.0
        # Never log feature values - PHI minimization (Constitution + AGENTS.md).
        logger.info(
            "anemia predicted",
            extra={
                "model_name": self.name,
                "model_version": self.version,
                "anemia": anemia,
                "latency_ms": round(latency_ms, 2),
            },
        )

        return AnemiaPrediction(
            anemia=anemia,
            confidence=confidence,
            model_version=self.version,
            predicted_at=datetime.now(UTC),
        )
