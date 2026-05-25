from datetime import datetime

from pydantic import BaseModel


class HealthResponse(BaseModel):
    status: str


class ModelListing(BaseModel):
    name: str
    version: str
    loaded_at: datetime


class ErrorResponse(BaseModel):
    error: str
    detail: str | None = None
