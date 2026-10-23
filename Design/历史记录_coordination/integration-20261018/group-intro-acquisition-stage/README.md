# Six group introductions and explicit app acquisition

Staging candidate only. Production was read, never written. Unity, C# compilation, play mode and NUnit were not run by this agent.

## Integration

1. With Unity closed, check `group-intro-acquisition.patch` against the current production checkout, then apply that minimal patch. It changes 13 existing files. Do not copy existing candidate files over newer production files.
2. Copy only the new assets and their `.meta` files listed in `new-files.json` from `candidate/` into matching production paths. The six C# assets include one Editor NUnit test file; the two JSON assets are runtime Resources copies.
3. If production changed, re-run `prepare_patch.py` against it, inspect the minimal hunks, run `validate_package.py`, then `git apply --check`. The generator asserts exact replacement counts and fails instead of silently adapting unrelated changes.
4. `manifest.json` binds existing baselines/candidates; `SHA256SUMS` binds every candidate asset. Source/adaptation provenance is in `prepare_data.py` and `authored-dialogue-adaptations.diff`.

## Behavior and provenance

- Runtime catalogs derive from `Design/data/app_unlocks.json`, `Design/data/group_intro_scripts.json` and 第七轮 design. Design originals are unchanged. All six scripts have actual choice buttons, player lines, ordered replies and saved progress. Skip offers remaining real links/todos without inventing player choices or marking the script read.
- Fresh games preinstall Game Hub only, per the user's later override. AI chess stage 2 remains the existing production gate. Game Hub's authored sixth conversation opens the already-installed native window. Its todo also observes a genuinely visible native Game Hub opened through other normal entry points.
- Taohuo stage 2, Bodu Crowd stage 3, Miaoyu stage 3 require an offered group link, native browser Download, online active-time bytes at 2.5 MB/s, then separate Next / Install / Finish clicks in the install wizard. The final click enables the existing icon pop/balloon. Desktop, taskbar, native window, Start search and router gates use the receipt. Downloads and install steps survive close/reload.
- Public sample discussion requires the player's explicit neutral group question. Private sample shortage only offers a private thought. NPC text is checked with the existing privacy sanitizer; existing LlmPersonas already permits public crowdsourcing without knowledge of the player's AI.
- Missing acquisition state or Unity's entirely default empty nested-object placeholder is the explicit legacy-schema migration marker. A genuinely initialized fresh state always has a Game Hub receipt and cannot match this marker. Old stage-2 Taohuo, stage-3 Miaoyu and stage-4 Crowd entitlements are retained (`legacyGranted`); related scripts use `legacySkipped`. These flags do not fabricate offered links, bytes, install steps, completed/read scripts or player clicks. Future not-yet-unlocked apps still use the new acquisition path.
- Group choices pause ordinary free typing, group presets and pending group replies. Other incoming group text waits in the saved deferred queue. Real incoming committed lines use YY unread/notification sound/popup and a short noninteractive taskbar pulse. Date gates avoid Lili before joining and GTX 1060 before release.
- Private thoughts type at 30 chars/s, click-to-reveal then continue, with a separate Check group button. External story, stage, pause, minimize, reader, settings, save panel, life event, ending preparation and epilogue holds suspend optional UI. No self-owned StoryFocus lock. Closures verify save/state/panel/generation; failed saves roll back acquisition and chat changes, invalidate thought callbacks and allow retry.
- Sticky todos finish from actual installation, actual installed data file, an actual existing signed contract, or an actual Game Hub opening. Data-file todo opens the real Files window; enterprise todo opens actual crowd contracts.

## Authorized content adaptations

- Removed unsupported Taohuo ¥10 signup voucher.
- D017: root authorized stage-3 basic existing contracts and stage-4 advanced introduction. Dialogue now accurately describes acceptance, signing bonus and continuous delivery/pay. Removed unsupported manual per-question pay, ¥3000/week, three-star credit, random 100 / five-error rejection and subcontract promises. Root owns the actual contract/economy/UI changes; this patch only moves the feature entry stage and replaces the old misleading unlock balloon.
- Game Hub's download is replaced by opening the preinstalled app, per user override.
- Generic Files-folder wording replaces the hardcoded `len` name. Bodu Crowd English name normalized.
- The installation wizard is a child panel inside the existing native browser; no new native OS window or browser backend is introduced.

## Verification and remaining runtime work

Static checks passed on the final package: minimal patch applicability, baseline/candidate SHA256, UTF-8 LF, unique new GUIDs, six unique scripts, only implemented step types, option counts and bilingual reply parity. `XgAppAcquisitionTests` contains 15 NUnit cases, all **unrun**, including actual Unity JsonUtility deserialize/serialize/deserialize paths for missing/empty/default-version placeholders and a fresh game roundtrip that must not gain apps.

Performance inspection: `AllowsWindow` is used on explicit native OpenWindow and Start-menu filtering. It enumerates `DesktopWindowIndex.GetBindings`, whose `EnsureSeed` scans only the first access, scene lifecycle registration or changed runtime owner. Repeated calls prune the cached HashSet and enumerate the cached List; they do not call a full-scene search or GetComponentsInChildren unless seeding/owner registration is actually needed. The acquisition host's window lookup and gate scan run at most once per second. This is source evidence, not a measured profiler result.

Root should compile/run the tests and perform real UI checks, with an isolated disk-backed save (the optional host intentionally does not drive TestMode saves):

1. Fresh save: Game Hub visible; three downloadable apps absent from desktop/taskbar/Start/native launch. Old saves preserve earlier access without fake historical clicks.
2. Play all six intros using each branch across independent saves; verify actual sent choice, typing delay, link and todo. Skip/reload midway must not duplicate lines or choices.
3. Ask later sends nothing. Public neutral question starts data-pack dialogue; raw sample shortage never starts NPC knowledge of the player's AI.
4. Click actual link, observe browser, download bytes/progress, disconnect/pause/minimize/reload, then click all three wizard actions. Icon, native taskbar and todo change only after Finish. Verify actual Files and crowd routes.
5. Interrupt self/choice/download with stage/reader/settings/save/life event/ending UI and switch saves. No hidden callback may act on replacement state; close/reopen restores progress.
6. Verify native YY taskbar pulse, notification sound/unread, typography and small-window layouts; no screenshots, runtime success or profiler evidence are claimed here.

C7 risk-forecast independent consumer and authoritative anniversary source remain outside this package and are not declared complete.
