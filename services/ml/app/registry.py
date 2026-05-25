from dataclasses import dataclass
from datetime import datetime
from typing import Any, Protocol


class Predictor(Protocol):
    name: str
    version: str

    def predict(self, features: Any) -> Any: ...


@dataclass
class ModelEntry:
    name: str
    version: str
    loaded_at: datetime
    predictor: Predictor


class ModelRegistry:
    def __init__(self) -> None:
        self._entries: dict[str, ModelEntry] = {}

    def register(self, entry: ModelEntry) -> None:
        if entry.name in self._entries:
            raise ValueError(f"Model '{entry.name}' is already registered.")
        self._entries[entry.name] = entry

    def get(self, name: str) -> ModelEntry:
        if name not in self._entries:
            raise KeyError(f"Model '{name}' is not registered.")
        return self._entries[name]

    def list(self) -> list[ModelEntry]:
        return list(self._entries.values())


_registry = ModelRegistry()


def get_registry() -> ModelRegistry:
    return _registry
