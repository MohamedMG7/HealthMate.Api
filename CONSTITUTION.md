# HealthMate Constitution

## Mission
HealthMate exists to make free, open EHR/CDSS technology available to everyone who can benefit from a shorter road to diagnosis. It is born from the founder's own decade-long diagnostic journey and is aimed first at Egypt.

## Who We Serve
HealthMate serves providers first: doctors and small clinics. Patient-facing surfaces and Arabic/RTL support come later.

## In Scope
Patient records, encounters, conditions, observations, lab tests, prescriptions, medical documents, mental health assessments, provider-patient messaging, AI clinical assistance through Sina, and classical ML decision support.

## Out Of Scope For Now
Billing and insurance, pharmacy marketplaces, telemedicine video, scheduling, claims, ad-supported features, and anything that monetises patient data.

## Architecture Principles
HealthMate is a modular monolith on .NET 10 with PostgreSQL. Module boundaries are in-process and exposed through `AddXxxModule` extensions. The Python ML model will move to a FastAPI sidecar when it lands. Do not introduce microservices without written justification. The default deployment must not require paid-only dependencies. Vendor lock-in must be clearly disclosed; Gemini is optional and should remain swappable.

## Values
Patient safety beats developer convenience. Simple beats clever. Boring beats novel. New behaviour needs tests. Schema changes need migrations. Security is on by default.

## Do
Open an issue before any non-trivial change. Cite the patient's own record for any clinical claim Sina makes. Add at least one test for any new manager method. Add a migration for any schema change. Keep PRs under about 400 lines where possible. Ask for review on anything touching auth, prescriptions, or ML.

## Never
Bypass role checks or `[Authorize]` attributes. Log PHI such as names, DOB, NationalId, free-text symptoms, or conversation contents in any log line, error message, or telemetry. Embed real patient data in tests, fixtures, seed files, or commit messages. Add a paid-only dependency without an opt-out path. Add an ML model without a training script, an evaluation script, and recorded metrics on a held-out set. Ship a Sina response that gives a definitive diagnosis or prescribes without citing the patient's chart. Introduce a microservice split without a written design doc. Weaken Identity password rules. Commit secrets. Use `git push --force` on `main`.

## Disclaimer
HealthMate is decision support, not a substitute for clinical judgement. Sina does not diagnose; it assists.

## Governance
The maintainer and reviewers approve PRs. Disagreements escalate to issue discussion. Code of Conduct: Contributor Covenant.
