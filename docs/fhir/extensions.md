# FHIR Extensions

## Patient Governorate

URL: `http://healthmate.app/fhir/StructureDefinition/patient-governorate`

Context: `Patient.address.extension`

Cardinality: `0..1`

Value type: `valueString`

Example:

```json
{
  "resourceType": "Patient",
  "address": [
    {
      "city": "Fake_City",
      "country": "EG",
      "extension": [
        {
          "url": "http://healthmate.app/fhir/StructureDefinition/patient-governorate",
          "valueString": "Fake_Governorate"
        }
      ]
    }
  ]
}
```

The Egyptian NationalId identifier system used by this plan is `urn:oid:1.2.818.0.1.0`. This is a placeholder for integration testing, not a registered production OID. A real OID and a formal `StructureDefinition` resource are future profile-authoring work.
