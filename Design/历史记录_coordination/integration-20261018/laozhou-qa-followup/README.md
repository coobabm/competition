# LZ1 followup — prepared source, 2026-10-19

This package follows the already integrated `laozhou-qa-stage` main package. It does not replace that package. No production files were changed here; no commit, Unity invocation, build, screenshot capture or runtime test was performed for this followup.

## Apply independently

- `production.patch`: 4 runtime files. Check/apply from `competition-latest-20261016` using `git apply --check --ignore-space-change --whitespace=nowarn ../.coordination/integration-20261018/laozhou-qa-followup/production.patch`, then the same command without `--check` if integrating.
- `qa.patch`: 3 Editor paths (Sidebar dispatcher, new QA source and its meta). Apply after the runtime patch. It can be deferred while maintaining the low-load restriction.
- `legacy.patch`: 2 paths, prepared after the parent applied `production.patch`; safe legacy API NaN/overfitting replies plus a focused regression. Apply independently; do not reapply `production.patch`.
- `base/` records exact source inputs; `candidate/` contains resulting files; `manifest.json` records SHA256 values. Prefer patch hunks when another agent has updated a shared file. In particular preserve later C6/K04 work in `LlmPersonas.cs`, C3 work in the Sidebar dispatcher, and all `YYChatView.Learning.cs` changes.

Both patches passed `git apply --check --ignore-space-change --whitespace=nowarn` against production before handoff. This is applicability verification only, not C# compilation or Unity behavior verification.

## Runtime decisions and fixes

1. `ZhouCardLoan.Update`: an active script-owned `ChoicesFor == LaoZhou` blocks the loan, while an unanswered optional knowledge question does not. This closes the stage-3 progression hold without changing loan state transitions or rewards.
2. `LlmPersonas.Conversation`: old `byAi` player rows are excluded from Lao Zhou's prompt. Genuine player rows and observed Lao Zhou rows remain. Legacy visible history is untouched.
3. `TiebaHub.Discussion`: generic threads no longer gain invented player reputation or read/post history from stage. Neutral fallback responses are bilingual. Small Zhou's separate authored translation branch is preserved.
4. `TiebaView`: when Q1 was completed in YY, the original thread and discussion show the actual canonical chosen branch with an explicit “Only visible to you” indicator. It is a read-only local UI reminder; no public conversation/evidence record is appended, and the question is not settled twice. Default A recorded for this display interpretation, consistent with the approved shared-question design.
5. Latest explicit user A: stage 4 retains the existing recent persisted 8-message context, which may span days. Its Chinese/English persona wording now says to use only provided recent conversation, not invent missing old events, and distinguishes that context from stage-5 long-term retrieval. No history-window or gameplay mechanism was changed.

Reviewer P3 is addressed narrowly in the separate `legacy.patch`: the legacy API gives relevant bilingual public explanations of NaN and overfitting, selected only by the actual question. The regression pairs topic relevance with output invariance across private progress/temperature/NaN-count changes. Existing identity/finale branches remain. Repository review found no active production caller for that API; this does not change active YY fallback wiring or broadly rewrite reply content.

## Prepared actual-UI verification

Editor entry point: `LaoZhouQuestionsUiChecks.Run`, menu **Tools / Local Verification / Lao Zhou questions actual LCD**. It uses the existing isolated Main Sidebar fixture and its `LocalVerification/SidebarLayout/<timestamp>/Save` directory. Execute only after the user/parent releases the current no-Unity restriction; there is no passing runtime result for this package yet.

The LZ1 entry point records the prior Editor scene/profile/save overrides and frame settings, refuses dirty scene loss, uses a clean empty scene when the original setup has no saved scene paths, creates virtual Mouse/Keyboard devices only when missing, and caps PlayMode at 30 FPS with vSync off. Failure cleanup attempts always exit PlayMode; scene restoration failure still exits batch mode with failure. These protections are prepared, not a runtime safety certification.

The source prepares:

- Chinese/English × three Q1 public choices: actual LCD acceptance and branch clicks, same-frame duplicate clicks, authored public response, once-only YY quotation, unread delivery, real `Runtime.SaveSlot(2)`/`LoadArchive(2)` and Unity JSON state restoration.
- Chinese/English × three Q1 YY choices: actual YY click, then thread/discussion private completion record, no reoffered question, zero public messages, one YY response.
- Chinese/English × three Q2 choices with Q1 unanswered: actual LCD button choices and duplicate guards.
- Real scheduler suppression by StoryFocus and actual reading flag; Q2 may arrive after those clear with Q1 unanswered.
- Same-count real archive swaps followed by an old still-rendered actual LCD click before redraw, for YY choice, Tieba choice and acceptance; fresh controls work after redraw.
- An ordinary actual input/send followed immediately by archive reload, before its reply delay, then waiting to ensure no old queued reply arrives. The test does not call a model.
- Real installed `ZhouCardLoan.Update`/`Ask` starts with Q1 unanswered at stage-3 exam readiness. Fixture parameters are explicit setup, not evidence of naturally trained progression; it does not invoke `CardLoanStart` directly.
- Synthetic prompt-boundary inspection verifies generated legacy player text is absent and genuine player text retained; reflective fallback/context checks verify stage independence. These are explicitly source/prompt fixtures, separate from user input claims.
- Choice layout and Chinese glyph assertions plus actual LCD screenshots at defined checkpoints.

All user choices/send actions in this mode route through `FilesWindowUiChecks.Click`, including the existing LCD mapping, raycast-target check and pointer-down/up delivery. Fixture navigation, window selection, language/state setup and input-text assignment are explicit deterministic preparation. No `onClick.Invoke` is present in the new QA source. Existing unrelated Sidebar modes are unchanged.

## Remaining verification limits

The newly prepared C# files have been source-audited against referenced APIs, not compiled. Actual scene animations, overlays, layout overflow, screenshot appearance, disk roundtrip and scheduler timing remain unexecuted. The six-minute-to-eight-minute fixture timeout is a bound, not a timing guarantee. If Unity exposes a failed prerequisite, retain its failure/report rather than weakening actual input or passing assertions. The 37-test and full source compilation results of the earlier main package do not certify these followup changes.
