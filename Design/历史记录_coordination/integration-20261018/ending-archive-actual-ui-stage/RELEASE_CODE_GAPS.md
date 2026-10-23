# Current seven-document code-gap audit

Read-only review on 2026-10-19. No production edits and no Unity, compile, Player, model, audio, or tests. Consulted the seven document mappings in `acceptance-inventory/逐项验收清单.md`, current production entry paths, `Design/整合记录.md`, `Design/Codex接管协作队列_20261019.md`, and `Design/六小时整合决策记录_20261019.md`. Newer explicit decisions override the older seven documents. This is a bounded static review, not a whole-game or natural-playthrough certification.

## Player blockers

No new confirmed player-blocking defect was established in the inspected entry paths. The observed archive run failure was a QA lifecycle omission, not an identified product scene-start bug: production `CaseMenuMainDisplay.Configure` already calls `MainSceneEntry.EnsureInitialized`, while the runner's later explicit Main fixture reload omitted it. A one-line runner-only patch is delivered in `../ending-archive-main-bootstrap-stage/`.

Do not turn unrun scenarios into either passes or product bugs. Root owns the ongoing archive/native replay runs and final Player build. Portable package references remain root's concrete release-source action, as recorded in `RELEASE_ASSET_AUDIT.md`.

## Concrete remaining nonblocking feedback differences

1. **Unknown AI answers remain purple in pack UIs.** `Design/开局流程简化_教练科技.md` §4 asks for a gray “……不知道” when confidence is below 55%. `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgPackPuzzles.cs:43` (`ShowAi`) and `:59` (`Choices`) choose the unknown text but pass `purple` to the actual text widget. `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgPackContentGames.cs:32` creates the swipe prediction text in purple; `RefreshPrediction` at `:45` only changes text. This is an observable presentation difference, not a purchase, score, progress or save blocker.
2. **Pack result feedback does not retain the AI's outcome mark.** D02 §4 asks for a post-answer ✓/× on the AI answer. `XgPackPuzzles.cs:28` (`Submit`) records the actual `aiText`, but its visible `last` string is only the player's correctness plus explanation. `:66` (`Tick`) rebuilds on the next frame, replacing the AI answer; no retained post-answer AI correctness mark is present in this base implementation. The previous player's result remains in `LastAnswer`, so this does not prevent continued answering. The explicit latest user rule **点完立刻下一题** supersedes D02's old 0.25s/1s dwell: immediate progression is intentional, not a defect. Fixing the retained feedback does not justify adding delay or rerunning unrelated already-passing modules.

An isolated five-file candidate for both presentation differences is delivered in `../pack-ai-feedback-stage/`. It captures the actual displayed prediction before player settlement and retains a static bilingual verdict across the unchanged next-frame rebuild. It changes neither training nor RNG behavior. Packaging and clean-application checks passed; compilation and rendered UI remain unverified until the integration owner runs them. These findings describe the inspected baseline; do not keep reporting them as open after the candidate is imported and verified.

## Old gap claims that current code no longer supports

`pack-rounds/DESIGN_GAPS.md` is an older snapshot and should not be presented as the current missing-feature list:

- `XgPackContentGames.cs:18` loads the real browser-news catalogue for headline cards before `PickTeachingPhrase`.
- `XgPackContentGames.cs:35` animates falling scam cards, parks them before controls, and deliberately does not settle slow reading. This matches the newer countdown-only-bonus rule; do not reintroduce timeout failure from an old visual spec.
- `XgPackContentGames.cs:226` selects original/parody poetry every fourth question, using `XgTeachingPoetry`.
- `XgPackContentGames.cs:247` selects dated danmu content, and `:259` scrolls and loops the same unanswered item.
- `XgActionGames.cs:557` creates the golden mole milestone and `:466` shows its extra point feedback. The older claim that no golden event exists is false for current code.

These source observations avoid repeating resolved work. They do not add runtime pass claims or certify every detail of those features.

## Release evidence boundaries already concrete in source

- `Assets/LingGuangV05/YY2016/YYChatHub.cs:429` substitutes fallback when the native result is null; `:442` can also substitute a repeated AI answer. A visible YY response plus a model-ready log therefore cannot prove that particular final-Player response came from native inference. Retain the existing release smoke plan's `unverified_native_origin` unless real request-origin evidence is captured. This is an evidence limitation, not proof of broken chat; do not rerun the already-passing Editor scheduling suite merely to relabel it Player coverage.
- `Assets/LingGuangV05/Runtime/Persistence/VerificationSavePaths.cs` isolates file stores only and expressly leaves company/product and PlayerPrefs unchanged. Final smoke should use the imported `--verification-save-dir` implementation; the old dedicated-Windows-account section in the smoke checklist is superseded by its own banner and the newer isolation candidate.
- The known 10/13 missing group images remain outside this audit's work by parent instruction. No image-generation or key work was attempted.

The source-backed observations above are the findings. Stale inventory “待确认” rows are not promoted wholesale into new must-fix issues, and this review does not claim the seven documents are fully complete.
