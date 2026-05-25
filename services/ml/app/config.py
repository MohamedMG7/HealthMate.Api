from pathlib import Path
from typing import Literal

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    ML_ARTIFACTS_PATH: Path = Path("/artifacts")
    ML_AUTH_TOKEN: str = ""
    ML_ENV: Literal["development", "production"] = "development"
    ML_LOG_LEVEL: str = "INFO"


def get_settings() -> Settings:
    return Settings()
