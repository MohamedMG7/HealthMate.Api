import json
import pickle
import pandas as pd
import sys
import os

# Get the directory where the script is located
script_dir = os.path.dirname(os.path.abspath(__file__))

# Load model
model_path = os.path.join(script_dir, "anemia_model.pkl")
with open(model_path, "rb") as f:
    model = pickle.load(f, encoding="latin1")

# Load scaler
scaler_path = os.path.join(script_dir, "scaler.pkl")
with open(scaler_path, "rb") as f:
    scaler = pickle.load(f)

# Read JSON input
input_file = sys.argv[1]
with open(input_file, "r") as f:
    data = json.load(f)

# Extract features into a DataFrame (with feature names)
try:
    input_df = pd.DataFrame([{
        "Hb": data["HB"],
        "RBC": data["RBC"],
        "PCV": data["PCV"],
        "MCH": data["MCH"],
        "MCHC": data["MCHC"]
    }])
except KeyError as e:
    print(f"Missing key in JSON input: {e}")
    sys.exit(1)

# Scale and predict
input_scaled = scaler.transform(input_df)
prediction = model.predict(input_scaled)

# Output prediction result
print(int(prediction[0]))