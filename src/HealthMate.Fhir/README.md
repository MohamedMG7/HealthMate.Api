# HealthMate.Fhir

This module exposes the first HealthMate FHIR R4 facade: `Patient` only.

Supported endpoints:

- `GET /fhir/metadata`
- `GET /fhir/Patient/{id}`
- `GET /fhir/Patient?...`
- `POST /fhir/Patient`
- `PUT /fhir/Patient/{id}` with `If-Match`
- `DELETE /fhir/Patient/{id}`
- `GET /fhir/Patient/{id}/_history`
- `GET /fhir/Patient/{id}/_history/{vid}`
- `POST /fhir/Patient/$validate`

Supported Patient search parameters are `_id`, `_lastUpdated`, `name`, `identifier`, `birthdate`, and `gender`. Paging uses `_count` and `_offset`; sorting supports `name`, `birthdate`, and `_lastUpdated`.

Out of scope here: SMART on FHIR, XML, conditional create, `_include`, `_revinclude`, subscriptions, `$everything`, bulk export, terminology operations, and resources other than `Patient`.

The module does not reference `HealthMate.Infrastructure`. Entity access flows through `IFhirPatientStore`; the in-process EF adapter lives under `HealthMate.Infrastructure/Fhir`.

Extension list:

- `http://healthmate.app/fhir/StructureDefinition/patient-governorate`

## Known caveats

- **`IsVerified` is admin-managed.** The FHIR write path (POST and PUT) never sets it. Patients created via FHIR land unverified, and a FHIR PUT will not flip an existing patient's verification flag. Run the admin verification flow to mark a FHIR-imported patient as verified.
- **`NationalIdImageUrl` placeholder on POST.** FHIR-created patients receive a placeholder image URL (`fhir_patient_national_id.png`). There is no FHIR-side upload yet; pair POST with a follow-up upload through the admin tooling before treating the record as verifiable. PUT does not modify this field on existing patients.
- **`name`, `telecom`, `Weight`, `Height`, `ApplicationUserId` are read-through-only.** Reads serialize them from the joined `ApplicationUser`; writes do not propagate back. Profile/account updates flow through the existing `/api/Auth/*` surface until a future plan unifies the write path.

Manual conformance check:

- Run the API and request `/fhir/metadata`.
- Save a representative `Patient` response and `OperationOutcome` response.
- Run `fhir-validator-cli` against those JSON files with the R4 definitions.
