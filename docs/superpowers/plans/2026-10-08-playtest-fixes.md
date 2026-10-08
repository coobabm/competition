# Playtest Defect Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Tasks are coordinated by root; only root controls Unity.

**Goal:** Fix the confirmed YY startup bridge, CJK data-title clipping, cold-start initialization faults, and desktop watermark layering without changing gameplay balance or existing player progress.

**Architecture:** Reuse existing idempotent Attach and DreamOS initialization/data access patterns. Adjust the minimum layout constants and hierarchy order. Preserve source dirty work and keep original saves outside all test writes.

**Tech Stack:** Unity 6000.4.5f1, C#, uGUI/TextMeshPro, existing NUnit EditMode and explicit live-Play assertions.

**Spec:** /Users/kk/unity/reports/LingGuang-playtest-20261008-0246/游戏试玩报告.md and user's approval to fix those issues.

## Global Constraints
- No new dependency, no broad refactor, no commit/push without a further request.
- Do not change girlfriend/LLM files being edited by Claude.
- Root owns Unity, watermark runtime adapter and scene preservation; workers edit assigned disjoint C# files only.
- Preserve existing source modifications, PlayerPrefs, original save and .bak, DreamOS user data.
- Actual test counts and runtime evidence are required; zero tests is not a pass.

## Review Focus
- ComputerCaseMenu additive entry and direct Main both initialize exactly one YY bridge.
- Chinese names fit current shipped CJK metrics in normal and maximized layouts, without shrinking fonts or disabling truncation.
- Missing download keys, early localized button refresh, and invalid/empty network indices do not throw or discard valid data.
- Watermark is above wallpaper but below application windows; scene serialization changes remain scoped.
- New and existing saves retain progress after Play; unrelated Claude edits are not reverted.

### Task 1: YY bridge
- [ ] Add failing natural-startup live Play assertion and direct-Main attribute guard in MainSceneEntryTests.
- [ ] Root observes RED on the unmodified startup implementation.
- [ ] Add existing XgYyTalk.Attach after XingGuangController.Attach in MainSceneEntry.
- [ ] Verify natural bridge/Hub registration and idempotence from both entry paths.

### Task 2: Data titles
- [ ] Add XgDataPageLayoutTests with actual CJK font, real row builders, normal/maximized widths and visible-glyph assertions.
- [ ] Root runs four RED cases before production edit.
- [ ] Increase only RowHeight/OfferHeight as necessary to fit full line metrics.
- [ ] Run GREEN tests and capture normal/maximized actual data pages.

### Task 3: Startup faults
- [ ] Read exact exception stacks; add dedicated regression tests before production fixes.
- [ ] Confirm missing download-state behavior, early ButtonManager UI refresh and invalid network state with failing tests.
- [ ] Apply minimum safe-default/lifecycle/index fixes at the responsible layer; no exception swallowing or logging suppression.
- [ ] Confirm EventSystem duplication source and fix only if supported by evidence.
- [ ] Verify targeted tests plus empty data-directory and existing-save Play.

### Task 4: Watermark
- [ ] Add preview-scene hierarchy-order regression and observe RED.
- [ ] Use the existing AeroLcdSourceBridge.Start to place Aero Edition Mark after Wallpaper and before Apps & Windows at startup; keep Main.unity byte-identical after native SaveScene produced unrelated layout noise.
- [ ] Make the existing style builder emit the same order.
- [ ] Verify GREEN, actual maximized-window screenshot, and Main.unity byte-identical to the captured pre-fix baseline.

### Integration / delivery
- [ ] Independent diff review and actual targeted test results.
- [ ] Safe save-isolated Play from menu and direct Main, verify repaired interactions and runtime exception sampling.
- [ ] Restore original saves and DreamOS data, byte-verify, record remaining baseline failures separately.
- [ ] Deliver local fix summary and evidence. Do not automatically restart completed GitHub automation.

## Verified implementation adjustment
Native hierarchy reorder itself saved a 1-line diff, but subsequent Unity layout serialization produced more than 1500 unrelated field changes. Noisy versions are backed up in the evidence folder. Main was restored byte-identically; the existing scene-local runtime UI adapter now applies the same ordering at Start. Tests invoke the actual adapter on a preview scene and remain RED before this code change. New EditMode fixtures own disposable additive scenes and no longer call global Canvas.ForceUpdateCanvases.
