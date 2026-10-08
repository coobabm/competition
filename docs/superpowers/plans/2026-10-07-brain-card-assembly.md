# Brain Card Assembly Implementation Plan

> **For agentic workers:** Use parallel ownership and regression-first validation; preserve all existing uncommitted work.

**Goal:** Redesign the native 大脑 page as a genuinely interactive card composition workbench.

**Architecture:** Keep DreamOS/uGUI and the original Concepts/Cortex/Atlas views. Use existing XgTrialDraft and atomic TryApplyTrial as the sole staging/apply path; add only thin card mapping rules. Two actual training tracks, not four invented independent models. No new save format or dependencies.

**Tech Stack:** Unity 6000.4.5f1, uGUI/TMP, pure C#, NUnit and existing LCD pointer QA helpers.

**Spec:** User request: “重新设计大脑的界面 需要那种能拼凑的卡片式交互”. Default design: library → drag ghost → compatible socket highlight → snap → staged preview → explicit apply. Retain click-to-place alternative and optional-card removal.

## Constraints and review focus
- Keep 接线 and all unrelated dirty files untouched. No global configuration, real AI request, scene YAML, disk-save fixtures, or commits.
- Preserve mandatory architecture/activation cards, lock unavailable/incompatible/built-in cards, reject stale/busy drafts before writing.
- Apply is not training. Architecture change restarts the training line; do not claim measured prediction gains or concept deletion.
- Screen-camera-correct dragging through the physical LCD bridge; cancel on tab switch/disable/Escape without mutation.
- Readable Chinese, scrollable full library, bounds at native and reduced window sizes, no dangling drag ghosts.

## Ownership and gates
- [x] Core worker: XgBrainCards.cs and XgBrainCardTests.cs only; failing tests before implementation, then focused .NET suite.
- [x] UI worker: XgBoardPage.cs, Cortex.cs, new Assembly.cs and XgBrainCardInput.cs only. Native controls and old-view integration.
- [x] Root: missing assembly baseline, source backup, isolated Play fixture and real LCD pointer probes.
- [x] Root: Unity import/Console, focused EditMode tests, real drag/click/invalid-drop/apply/stale/busy checks and screenshots; independent scoped review.

## Evidence log
- Active project verified via process and MCP: /Users/kk/competition, Main.unity, Unity 6000.4.5f1. Initial Play flags: useDiskSave=true, tickEnabled=true.
- Pre-existing dirty state and clean Board sources backed up to /Users/kk/unity/brain-cards-before-20261007.
- New behavior acceptance: card placement changes draft only; explicit Apply changes current training configuration; invalid drop and cancellation change neither.

- Final gate: BLOCKED by new out-of-scope XgEraAdvantageBridge.cs:144 -> missing TiebaView.OpenPrivate; do not report full-project build success. Card tests and Play evidence preserved.
