# Validation artifacts

Temporary Play-mode probes are archived here after validation and intentionally excluded from Assets / Player assemblies.

- `Aero2010PlayQa.cs`: screen-coordinate EventSystem integration regression and icon loader/header checks.
- `AeroContrastPlayQa.cs`: waits until the simulated OS is active, opens the 13 applications, records theme references and light-text candidates, captures screenshots. Light candidates on intentionally dark surfaces are not failures; inspect their screenshots.
- `AeroIconLifetimeQa.cs`: runtime clone + RefreshBindings + loader destruction checks.
- `AeroAudioProbe.cs`: checks nonzero audio DSP samples, not subjective sound quality.
- `fetch_oxygen.rb`: pinned artwork retrieval/provenance helper.

Results are in `Evidence/HongmengOS-Aero2010/`. The archived startup-timing report records a probe run that invoked Calculator before its desktop parent became active; the probe was corrected to await OS readiness. This is not used as a passing result.

These are Editor integration/visual checks. They do not constitute OS-level mouse automation, platform Player validation, or a general accessibility certification.
