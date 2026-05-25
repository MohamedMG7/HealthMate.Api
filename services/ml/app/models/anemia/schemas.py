from datetime import datetime
from typing import Literal

from pydantic import BaseModel, Field


class AnemiaFeatures(BaseModel):
    # Field ranges are wide sanity bounds, NOT clinical reference ranges.
    # No patient_id by design - PHI minimization at the wire format.
    hb: float = Field(ge=0, le=30, description="Hemoglobin (g/dL)")
    rbc: float = Field(ge=0, le=15, description="Red blood cells (10^6/uL)")
    pcv: float = Field(ge=0, le=70, description="Packed cell volume (%)")
    mch: float = Field(ge=0, le=50, description="Mean corpuscular hemoglobin (pg)")
    mchc: float = Field(ge=0, le=50, description="Mean corpuscular hemoglobin concentration (g/dL)")


class AnemiaPrediction(BaseModel):
    anemia: bool
    confidence: float | None = Field(
        default=None,
        ge=0.0,
        le=1.0,
        description="Probability that the patient has anemia, if the underlying model supports it.",
    )
    model_name: Literal["anemia"] = "anemia"
    model_version: str
    predicted_at: datetime
