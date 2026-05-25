"""Evaluation entry point for the anemia model.

See train.py for the grandfathered-status context. Evaluation cannot run until
a training script and a documented held-out dataset exist.
"""


def evaluate() -> None:
    raise NotImplementedError(
        "Anemia evaluation script not implemented. "
        "See services/ml/app/models/anemia/README.md for the dataset acquisition TODO. "
        "Do not add a new clinical model until this is resolved (CONSTITUTION rule)."
    )


if __name__ == "__main__":
    evaluate()
