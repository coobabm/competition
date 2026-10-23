# Self-play save restoration: optional inline placeholders

Minimal follow-up after `board-selfplay-variety-stage`: `board-selfplay-savefix.patch`, 3 existing files. Static delimiter check and production apply-check passed; no Unity/C#/test execution here.

Observed root failures affect only checkers/Monopoly's existing `Ludo ?? Checkers ?? Monopoly` log assertion. The likely cause is JsonUtility materializing an inactive null Ludo state as an empty inline object. This candidate repairs only **non-selected empty placeholders** in the simulation constructor. It requires zero action/counter evidence, empty traces and empty board arrays; it preserves active state, all night/rotation/round seed inputs, and any inactive engine with real arrays or logs. It does not reset an active engine or discard anomalous evidence.

The existing failing assertion is retained. Each reload test additionally compares the selected engine's complete serialized state immediately and after each of five subsequent real actions, along with deterministic seed inputs. New cases preserve inactive real traces and inactive initialized board arrays. Placeholder assertions print the remaining object's JSON on failure so root can distinguish actual inline defaults from a real state without guessing.

Root should rerun `XgBoardSelfPlayVarietyTests` serially. The placeholder representation is inferred from the two failures, not yet confirmed by runtime execution of this candidate.
