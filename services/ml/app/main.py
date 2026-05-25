import logging
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from datetime import UTC, datetime

from fastapi import Depends, FastAPI, Request, status
from fastapi.responses import JSONResponse

from app.config import Settings, get_settings
from app.models.anemia.predictor import AnemiaPredictor
from app.models.anemia.router import router as anemia_router
from app.registry import ModelEntry, ModelRegistry, get_registry
from app.schemas.common import ErrorResponse, HealthResponse, ModelListing
from app.security import verify_token

logger = logging.getLogger("healthmate.ml")


def _configure_logging(settings: Settings) -> None:
    logging.basicConfig(
        level=settings.ML_LOG_LEVEL,
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
    )


def _load_models(settings: Settings, registry: ModelRegistry) -> None:
    anemia_dir = settings.ML_ARTIFACTS_PATH / "anemia" / "v0"
    anemia_predictor = AnemiaPredictor(anemia_dir)
    registry.register(
        ModelEntry(
            name=anemia_predictor.name,
            version=anemia_predictor.version,
            loaded_at=datetime.now(UTC),
            predictor=anemia_predictor,
        )
    )
    logger.info(
        "model loaded",
        extra={"model_name": anemia_predictor.name, "model_version": anemia_predictor.version},
    )


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    settings = get_settings()
    _configure_logging(settings)
    _load_models(settings, get_registry())
    yield


def create_app() -> FastAPI:
    settings = get_settings()
    docs_url = "/docs" if settings.ML_ENV == "development" else None
    redoc_url = "/redoc" if settings.ML_ENV == "development" else None

    app = FastAPI(
        title="HealthMate ML",
        version="0.1.0",
        docs_url=docs_url,
        redoc_url=redoc_url,
        lifespan=lifespan,
    )

    @app.exception_handler(Exception)
    async def unhandled_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        # Deliberately omit request body, headers, and query string — they may carry PHI.
        logger.exception(
            "unhandled exception",
            extra={"path": request.url.path, "method": request.method},
        )
        return JSONResponse(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            content=ErrorResponse(error="internal_error", detail=str(exc)).model_dump(),
        )

    @app.get("/health", response_model=HealthResponse, tags=["meta"])
    def health() -> HealthResponse:
        return HealthResponse(status="ok")

    @app.get(
        "/v1/models",
        response_model=list[ModelListing],
        dependencies=[Depends(verify_token)],
        tags=["meta"],
    )
    def list_models() -> list[ModelListing]:
        return [
            ModelListing(name=e.name, version=e.version, loaded_at=e.loaded_at)
            for e in get_registry().list()
        ]

    app.include_router(anemia_router, prefix="/v1/predict", tags=["anemia"])

    return app


app = create_app()
