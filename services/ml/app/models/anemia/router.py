from fastapi import APIRouter, Depends

from app.models.anemia.predictor import AnemiaPredictor
from app.models.anemia.schemas import AnemiaFeatures, AnemiaPrediction
from app.registry import ModelRegistry, get_registry
from app.security import verify_token

router = APIRouter()


def _get_predictor(registry: ModelRegistry = Depends(get_registry)) -> AnemiaPredictor:
    entry = registry.get("anemia")
    predictor = entry.predictor
    assert isinstance(predictor, AnemiaPredictor)
    return predictor


@router.post(
    "/anemia",
    response_model=AnemiaPrediction,
    dependencies=[Depends(verify_token)],
    summary="Predict anemia from CBC features",
)
def predict_anemia(
    features: AnemiaFeatures,
    predictor: AnemiaPredictor = Depends(_get_predictor),
) -> AnemiaPrediction:
    return predictor.predict(features)
