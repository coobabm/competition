# Wiring Workbench Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development; isolate file ownership and do not overwrite pre-existing edits.

**Goal:** Replace the cramped wiring presentation with a readable native Unity workbench that controls real existing tasks.

**Architecture:** Keep XgSim, DreamOS, the LCD pointer bridge and task executors. Reuse XgUi, XgCardArt.Rounded and XgPan. Replace the page's forced narrow columns with fixed-size cards and a pannable content canvas. Keep view positions local, not in saves.

**Tech Stack:** Unity 6000.4.5f1, uGUI/TMP, existing C# runtime and NUnit.

**Spec:** User's wiring request and /Users/kk/unity/model-wiring-prototype/index.html; adapt to actual in-game models, cards and task rules instead of introducing HTML demo numbers.

## Constraints / review focus
- Preserve pre-existing dirty files and the original desktop; source backup outside the Unity project.
- No dependencies, real AI calls, global configuration changes or player save writes for QA.
- Real execution vs. available/idle must be distinct; no fake success dots or fake task progress.
- Camera-correct pointer conversion through the LCD, with consistent dragging/panning/zoom.
- Clicking a wire selects it; an explicit button disconnects it.
- Large inventories must remain reachable; the initial working set is explicitly filtered, not silently removed.

## Tasks
- [x] Inspect active checkout/scene and capture baseline. Existing wiring Core suite: 10/10.
- [x] Verify red UI probes: node dragging and background panning currently absent.
- [x] Core worker: regression tests first; fix deployment inheritance, truthful task state, power loss and bounded shared training resources.
- [x] Main: native card workbench, selected-node drawer, resources drawer, click/drag wiring, pan/zoom, workspace/all/category filters.
- [x] QA worker: add isolated real LCD drag and layout probes; refuse disk-save runtime.
- [x] Compile and run focused/core regression tests; real Play screenshots and pointer interactions; restore saved Editor settings after QA.

## Boundary
The old Opus wiring remains backed up in /Users/kk/unity/model-wiring-prototype/unity-before-20261007. No scene/prefab YAML editing, no commits/pushes. Main scene source and player save files must remain unchanged. A successful build is not Play-mode proof.

## Outcome
- Unity wiring tests: 23/23. Full .NET suite: 567/596; all 29 failures reproduced against pre-change backups (0 new failures among the final failing set).
- Native LCD pointer checks and fresh layout validation completed; captured after-final.png.
- Restored disk-save/tick flags to true and returned to original ComputerCaseMenu scene; no scene asset saved.
- Small shared change: XgPan.SetView cancels previous zoom/inertia when explicitly fitting; existing pan callers unchanged.
