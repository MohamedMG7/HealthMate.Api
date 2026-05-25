# Anemia model

**Status: GRANDFATHERED under Plan #2.** This model predates the FastAPI
sidecar refactor and was inherited as a pair of `.pkl` files (`model.pkl`,
`scaler.pkl`) with no training script, no dataset reference, and no held-out
metrics. The original invocation lived in `src/HealthMate.Application/MLModels/Animea.py`
(removed in Plan #2) and was launched as a Python subprocess by the .NET ML manager.

`CONSTITUTION.md` requires every new clinical ML model to ship with a `train.py`,
an `evaluate.py`, and a `metadata.json` containing real metrics. The anemia
model does not satisfy this — it is exempt as the single grandfathered case so
the project can keep functioning while the replacement is built.

## What we know about the v0 artifact

Confirmed by inspecting the pkl in Plan #2 Phase 0.3:

| Property | Value |
| --- | --- |
| Classifier | `sklearn.ensemble.RandomForestClassifier` |
| Scaler | `sklearn.preprocessing.StandardScaler` |
| Feature columns | `Hb`, `RBC`, `PCV`, `MCH`, `MCHC` (in this order) |
| Classes | `[0, 1]` |
| `predict_proba` | available |
| Trained sklearn | `1.3.2` |
| Runtime sklearn | `>=1.3.2,<1.6` (warnings on unpickle, predictions stable) |

## Bug fix on de-grandfathering (raw-class mapping)

The original `Animea.py` printed the raw class, and `MachineLearningManager`
treated `"0"` as anemia=true (`prediction = result.Trim() == "0"`). Plan #2's
Phase 3 smoke test confirmed this is **clinically backwards**:

| Input | Raw class | `predict_proba[1]` | Legacy `.NET` output | Correct |
| --- | --- | --- | --- | --- |
| Hb=4.5 (severely anemic) | 1 | 0.996 | `anemia=False` | `anemia=True` |
| Hb=14.0 (normal) | 0 | 0.004 | `anemia=True` | `anemia=False` |

The FastAPI predictor (`predictor.py`) flips the mapping (`raw == 1 -> anemia=true`)
and the legacy inverted mapping is treated as a bug, not a contract. This is a
deliberate behavioural change. The PR for Plan #2 must call this out so that
any downstream consumer (clinicians who learned to invert the EDEngine output,
dashboards, alerts) is not blindsided.

The v0 artifact itself is unchanged; only the interpretation in code is corrected.
v1 (when trained) should pick an obvious convention and document it once.

## TODO(maintainer): de-grandfather

1. Source a CBC anemia dataset. Strong candidates use the same 5 features:
   Kaggle has several "anemia disease" or "anemia CBC" datasets that match this
   shape. Pick one with a clear licence and document the URL + licence at the
   top of this README.
2. Fill in `train.py` to load that dataset, fit a classifier and scaler, and
   write `artifacts/anemia/v1/{model.pkl, scaler.pkl, metadata.json}`. Use
   `joblib.dump` for new artifacts (sklearn-recommended) rather than `pickle`.
3. Fill in `evaluate.py` to compute `accuracy`, `precision`, `recall`, `f1`,
   `auc` on a held-out fold; write these into `metadata.json.metrics`, along
   with `trained_at`, `framework_version`, and the held-out split definition.
4. Update `predictor.py` (or introduce a version selector in `main.py`) to
   load v1 instead of v0.
5. Drop `"grandfathered": true` from `metadata.json` and remove the
   `constitution_exception` entry.
6. Until step 5 is done, **no new clinical model may be added**
   (CONSTITUTION rule). See `services/ml/README.md` for the model-addition
   recipe.
