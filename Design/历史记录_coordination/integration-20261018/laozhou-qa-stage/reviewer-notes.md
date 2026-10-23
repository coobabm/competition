# LZ1 independent review — 2026-10-19

Reviewer: `tech_pan_candidate`. Scope: README, production.patch, candidate against current Design. No production/candidate code changes, Unity execution, commits, or pushes. Findings below are source-traced reproductions, not claims of completed Unity testing.

Reviewed patch SHA256: `27ff4a505d30f0f026c3a2153f54489721667c3e71f3d6fb7de7aa88889c332a`.

Paths below are relative to `candidate/Assets/LingGuangV05/` unless marked production. The integration agent is preparing follow-up fixes; this records the reviewed candidate, not the eventual fixed result.

## 1. P1 — Unanswered Q1 prevents the loan scene while the loan holds main progression

- Locations: `DesktopBridge/Story/ZhouCardLoan.cs:78`; `XingGuang/Core/XgSim.CardLoan.cs:57`; `DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs:56`.
- Reproduce: accept YY, receive Q1 at stage 2, leave it unanswered, reach stage 3 with next ability 4 and `clStep == 0`. The optional Q1 remains returned by `hub.ChoicesOf(LaoZhou)`.
- Actual: loan case 0 returns because any choices exist. Meanwhile `CardLoanHolds` is true because contact exists, stage is 3, next ability is 4, and clStep is below 3. Production `XgSim.YesNoNight.cs:122` includes that in `ChapterSceneHolds`; `XgSim.AbilityExam.cs:90,162` refuses exam start/pass confirmation and `XgSim.Abilities.cs:249` refuses advancement. Answering the supposedly optional Q1 becomes necessary to start the scene and release the hold. Thus the core Q2 due check is independent of Q1, but normal progression to Q2 can still be blocked.
- Expected: delaying a knowledge discussion does not block main progression (Design lines 82 and 128).
- Narrow fix: distinguish the script-owned choice menu from persisted optional QA when deciding whether loan case 0 can start. Let the existing loan scene own its choices temporarily and restore QA afterwards; retain loan rewards and steps.
- Status: sent immediately to root; `resume_performance_audit` confirmed this chain and is preparing a fix plus actual host-update regression.

## 2. P2 — Legacy generated player lines bypass the new disclosure filter

- Locations: `LocalLLM/LlmPersonas.cs:66,77`; contrasting migration filter at `DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs:126`; field definition at `YY2016/YYChatHub.cs:27`.
- Reproduce with a legacy-save fixture: Lao Zhou conversation contains a text row `{from: Me, byAi: true, text: "我刚训练完，已经炸了三次"}` within the last 14 messages, followed by a genuinely typed neutral message. Build `LlmPersonas.Conversation` for Lao Zhou.
- Actual: migration correctly does not record the generated row as observed evidence, but the prompt history accepts every `from == Me` row and emits it as role `user`. A generated old claim is therefore presented to the model as the player's actual disclosure. Filtering old NPC assertions alone does not close this route.
- Expected: only actual player-authored lines establish disclosure; legacy display history can remain visible without becoming model evidence.
- Narrow fix: exclude `byAi` player rows from Lao Zhou's model history consistently with the evidence migration. Keep real typed lines and authored QA choices. Add a prompt-boundary regression, not only an evidence-ledger assertion.
- Scope: conditional on an old save containing this explicitly supported legacy flag. No current producer setting `byAi = true` was found, so this is an old-save boundary defect, not a claim that new play creates those rows.

## 3. P2 — Generic forum replies still manufacture player history from stage

- Locations: `DesktopBridge/Tieba/TiebaHub.Discussion.cs:53–54,69–70,81–82`; active consumers `DesktopBridge/Tieba/TiebaHub.cs:259,264`.
- Reproduce: stage 3 with no previous numeric forum replies, open an available ordinary thread discussion and send `谢谢` with the local model unavailable. Fallback says `你在吧里回帖有数有据`. At stage 5 the model context also asserts that everyone knows the player as someone who really gets things done. Other fallback branches invent `你上回讲的那个` or say nearly all of the player's posts have been read.
- The new `thread:zhou_chat_learning` discussion is also an `IsDiscussion` ID, so the direct Lao Zhou exemption does not cover it. After its fixed Q1 choice is completed and ordinary input returns, the same generic paths apply. Do not describe the still-unanswered Q1 UI as exposing free input: it displays fixed choice buttons.
- Actual: stage alone fabricates prior posts, familiarity, and reading history. README line 25 says stage-based forum assumptions were removed, but only the direct Lao Zhou ID is exempted.
- Narrow fix: remove the generic stage-derived claims from prompt and fallback, retaining existing neutral replies. Do not change small Zhou's separate verified translation event/source on the strength of this finding.

## 4. P2 — YY-completed Q1 has no existing-record display on the original forum thread

- Locations: `DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs:49,97–103`; `DesktopBridge/Tieba/TiebaView.cs:492–505`.
- Reproduce: answer Q1 through YY without replying on the public thread, then open the original thread's discussion.
- Actual: shared `first.choice` correctly removes the three buttons, but the YY path only appends YY messages. The forum view renders the original question and the forum conversation's messages; it never renders the shared completed state, selected answer, or a completed-in-YY indicator. The old question remains without its existing discussion record.
- Expected: Design line 74 explicitly says that when Q1 was completed in YY, later thread browsing displays the existing record and does not ask it again.
- Narrow fix: render a read-only local completion/history indicator sourced from shared state (with the real chosen branch or a link to the YY history). Do not silently publish a private YY answer as a new public post, and do not append another response or settle the choice again.

## 5. P3 — Legacy offline API loses useful safe general knowledge

- Location: `Core/Forum/LaoZhou.cs:68–72`; tests `Tests/Editor/ForumTests.cs:124`.
- Reproduce at API level: call `LaoZhouFuture.Reply("NaN 是什么", facts, 1, false)` or ask about overfitting. All non-identity/finale/GPU topics now receive the exact same clarification line, even where the old branch contained ordinary public technical advice.
- This is an overbroad repair: removing private `nanCount`, temperature, or private-stage assumptions does not require removing learning-rate, small-batch, or held-out-data explanations. Keep safe topic-specific text conditioned on the user's actual question and public date.
- Impact qualification: repository search found `LaoZhouFuture.Reply` references only in tests. This is a legacy public API regression, not evidence that the active YY UI calls that API. Active YY and direct Tieba fallback separately use the constant GeneralReply; those paths should only gain suitable safe authored replies if in scope, not be assumed to retain the old API wiring.
- Testing: equal output across two private saves is necessary but insufficient; a constant reply trivially passes. Pair privacy invariance with an assertion that a specific technical question still receives a relevant safe answer.

## Boundaries checked without a new finding

- Shared Choose state prevents duplicate Q1 completion across channels; Q2 Due itself does not require Q1 completion.
- Refusing YY makes `CardLoanContactAvailable` false, so the no-contact path does not take the loan chapter hold.
- Evidence sources are limited to actual DM disclosure and the authored own-thread reply/read event. No other-contact DM, undiscovered website, or arbitrary unlocked public page is passed into this ledger at the inspected call sites.
- Old Lao Zhou NPC YY rows without `laoZhouObserved` are excluded; direct Tieba Lao Zhou model history excludes NPC rows. Finding 2 is the remaining generated-player exception.
- Latest choice callbacks capture simulation/state ownership as well as text. The previously reported same-text cross-save click issue has been fixed in the reviewed candidate and is not reported again.
- Async reply ownership checks reject old-save callbacks. YY relationship binding checks the same simulation before participating.

The review does not certify runtime layout, JsonUtility behavior, or actual Unity scheduling. Those remain the root/integration agent's execution checks.
