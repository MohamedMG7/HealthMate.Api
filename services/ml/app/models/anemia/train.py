"""Training entry point for the anemia model.

This model is GRANDFATHERED under Plan #2: the v0 artifact predates this
refactor and was inherited without a training script or dataset reference.

Replacement procedure (required before any new clinical model is added per
CONSTITUTION.md):

  1. Source a CBC-based anemia dataset and record its URL + license in README.md.
  2. Implement train() below to load that dataset, fit a classifier and scaler,
     and write artifacts/anemia/v1/{model.pkl, scaler.pkl, metadata.json}.
  3. Implement evaluate.py to compute held-out metrics, write them into
     metadata.json, and drop the "grandfathered": true flag.
  4. Update the predictor and router to point at v1 (or use a version selector).
"""


def train() -> None:
    raise NotImplementedError(
        "Anemia training script not implemented. "
        "See services/ml/app/models/anemia/README.md for the dataset acquisition TODO. "
        "Do not add a new clinical model until this is resolved (CONSTITUTION rule)."
    )


if __name__ == "__main__":
    train()
