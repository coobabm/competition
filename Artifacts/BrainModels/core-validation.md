# Saved-model brain assembly — Core validation

## Delivered behavior

- Persistent `XgState.brainModules` stores one explicit model choice per dataset. Multiple datasets can coexist in the same brain region.
- Missing selection preserves legacy Best / BestAcc behavior. `modelId = 0` is an explicit removal and never silently follows a later Best. Manual choices remain pinned to their saved model ID.
- `BrainModelAccuracy` is the new installed-model capability resolver. Existing `BrainAccuracy` remains the separate recent real-LLM judgment statistic, unchanged.
- Installation/removal changes only assembly selections: no LoadModel, training parameters, global board, Best, histories, wallet, RNG, order wiring, or income wiring are changed.
- Invalid/missing/duplicate checkpoint IDs, unknown catalog references, track mismatches, invalid shapes, and invalid/nonfinite numeric metadata are rejected before any selection write. A corrupt explicit selection fails closed rather than following Best.
- Valid settings-only checkpoints may be installed without retained weights. They are game skill checkpoints, not separately loaded or combined language models.
- Manual and effective legacy installations are protected against deletion, automatic cleanup, and weight trimming without being automatically starred.
- SaveModel protects its newly created checkpoint through the current cleanup pass, including Assess before it binds the new entry to Best. Fully protected repositories may temporarily exceed MaxModels rather than publishing/deploying a deleted checkpoint.
- Persona gating and game strength use the selected checkpoint capability. A trusted-catalog-only, at-most-six-item, at-most-512-character capability manifest is injected through PersonaBoard, the common real chat / stand-in caller path; PersonaPrompt does not duplicate it. Custom checkpoint names never enter that manifest.

## Files

Added (with .meta):
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Core/XgSim.BrainModels.cs`
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Tests/Editor/XgBrainModelTests.cs`

Surgical integration only:
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Core/XgSim.cs`: deletion/cleanup/trim protections and just-created checkpoint protection.
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Core/XgSim.Persona.cs`: selected-capability resolver and common PersonaBoard manifest insertion.
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Core/XgSim.Games.cs`: selected-capability resolver only.

## Verification

- Initial TDD RED: 25 failed / 25 total against empty API stubs (`core-red.log`).
- Explicit-empty manifest regression: 1 failed before fix (`core-empty-manifest-red.log`).
- PersonaBoard real-caller regression: 1 failed before fix (`core-persona-board-red.log`).
- Protected-full-repository SaveModel and Assess regressions: 2 failed before fix (`core-protected-save-red.log`).
- Final related suite: **89 passed / 89 total, 0 failed, 0 skipped** (`core-related.log`, `core-related.trx`). It contains all 27 new model-assembly cases plus existing PersonaPrefix, SpeechPolicy, Games, and Collaboration tests.
- Two existing CS0649 warnings remain in XgSubcontractTests.Host.compute and XgProgressionTests.Host.blocker; no new compile warnings.
- No seven-minute full suite rerun was requested for this replacement implementation. No Unity Editor control, Play validation, or commits were performed by this subtask.
- Serialization coverage is a standard-library round trip of the public module list plus simulation reinitialization alongside the existing repository/Best lists; it is not a Unity disk-save/load test.

Command:
```sh
dotnet test Tools/XingGuang/tests/Tests.csproj --no-restore --filter 'FullyQualifiedName~XgBrainModelTests|FullyQualifiedName~XgPersonaPrefixTests|FullyQualifiedName~XgSpeechPolicyTests|FullyQualifiedName~XgGamesTests|FullyQualifiedName~XgCollaborationTests' --logger 'trx;LogFileName=core-related.trx' --results-directory /Users/kk/competition/Artifacts/BrainModels --verbosity minimal
```

## Complexity and boundary

For M repository checkpoints, S explicit selections, B best rows, and D catalog datasets: a selected lookup/install is O(M + S + B) time with O(1) additional storage; listing/prompt generation is O(D * (M + S + B)) with O(D) result storage and a 512-character prompt bound. Repository cleanup can be quadratic in M because it validates protected selections per candidate; the existing ordinary repository cap is 80 and protected overflow already exists for stars. No extra cache, dependency, or global synchronization was added. Calls remain on the existing simulation/UI thread.
