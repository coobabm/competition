# App acquisition actual UI candidate

Status: prepared and statically inspected; Unity compilation, runtime checks and screenshot review are **UNRUN**. This directory contains staging artifacts only. No Unity process, model inference or compiler was started to produce this package.

## Import and run

1. Apply `app-acquisition-followup.patch` to the current production checkout. It contains minimal changes to three existing files. Do not copy the full baseline/candidate versions over concurrent production work.
2. Copy the two files listed in `new-files.json` from `candidate/`, preserving their relative paths and meta GUID.
3. Root runs Unity serially from clean EditMode with `-executeMethod AppAcquisitionActualUiChecks.Run` and without `-quit`, or selects **Tools / Local Verification / App acquisition actual LCD**. The existing `BookLibraryUiChecks.Run` wrapper prepares real LCD input and restores editor scene/input settings.
4. The dedicated SessionState request is `SidebarLayoutChecks.AppAcquisitionActualUi`. The version guard rejects an incomplete Sidebar hook import. Sidebar phase **19** is selected before the other requests; it allows **900 seconds** for the complete bilingual run.
5. Read `LocalVerification/SidebarLayout/<timestamp>/report.json`, the screenshots and `app-acquisition-actual-ui.txt`. The specific marker and shared `passed.txt` are emitted only after this coroutine completes; their existence is not a substitute for screenshot review. Until root runs the candidate, neither marker nor a pass is claimed.

The callable coroutine defaults to `elapsedDelayFixtures=true`, `branchVariant=0`. The menu explicitly uses the elapsed fixture. Every script's eligible time is advanced explicitly; natural **180-second Miaoyu and 240-second Game Hub waits**, and the shorter eligibility waits, are not verified. The 2.5 MB/s download progression remains natural host Update progression. Downloaded bytes, finished installation receipts, selected choices, link messages and completed scripts are never injected. A runner choosing `elapsedDelayFixtures=false` must provide a longer overall timeout.

## Prepared coverage matrix

Every row runs first in Chinese and then English. The selected branch index is `branchVariant % optionCount`; unselected branches are not covered.

| Runtime script | Prepared actual path | Execution status |
|---|---|---|
| `taohuo_intro` | Native taskbar attention when YY is hidden, authored dialogue/private thoughts, actual player choice, link, real browser bytes, Next/Install/Finish/Open and newly visible desktop icon | Unrun |
| `datapack_intro` | Private shortage prompt, actual Ask later, real isolated archive reload, explicit neutral Ask group, authored dialogue and player choices, sticky todo | Unrun; no data-file import claim |
| `zhongbao_intro` | Explicit stage 3 precondition, authored dialogue and both choice rounds, link, actual download/install/open/icon | Unrun; no contract signing claim |
| `miaoyu_intro` | Explicit elapsed eligibility, authored dialogue and choice, link, actual download/install/open/native Commander icon | Unrun; natural 180-second wait skipped |
| `enterprise_intro` | Explicit stage 4 precondition, authored dialogue and choices, sticky todo | Unrun; no business completion claim |
| `gamecenter_intro` | Explicit elapsed eligibility, authored dialogue and choice, native preinstalled Game Hub link opens its window | Unrun; natural 240-second wait skipped; no reinstall claim |

The fixture also prepares actual choice-save and Finish-save failure/retry checks, partial-byte archive save/reload, saved six-script completion across owner changes, exactly four persisted application cards, a separate actual Skip branch and a final return to the genuine completed save. Skip must offer its link/todo while leaving the application uninstalled and emitting no selected player choice.

## Honest fixture and interaction boundaries

- Household power/date/wealth/postprologue readiness, stage 2 then 3 then 4, teaching preconditions and eligibility elapsed time are explicit fixtures. The fresh acquisition state comes from `XgSim`; Game Hub is its only tracked preinstalled acquisition app. This does not prove natural stage progression.
- The shared Main/LCD runner disables model/narrative drivers. This helper temporarily disables controller simulation Update while retaining production acquisition-host Update, real disk writes, YY rendering and native window/gate updates. Original simulation, language, disk setting and enabled states are restored in `finally`.
- Ordinary choice, link, download, installer, Open, Skip and desktop-icon actions use `FilesWindowUiChecks.Click`, which routes real LCD pointer events. Native window animation readiness is awaited. YY scroll positioning is explicit geometry setup, not a claim of mouse-wheel coverage. Dialogue reveal/advance uses the real thought control.
- Save failures are injected through the real `ChapterOneRuntime.Saving` event, followed by real retry clicks. Writes and SaveSlot/LoadArchive use the isolated `LocalVerification` save directory. No player save is an intended target.
- The network manager's connected flag is explicitly changed to exercise the production paused-byte consumer. This is not a click through network settings.
- The external `StoryFocus.ExclusiveProbe` is explicitly held/released while thought/download UI is active. The assertions cover halted active time, hidden/removed UI and stale thought callbacks. Reader/settings/pause/minimize UI clicks are not covered here.
- Retained UnityEvents are invoked directly only in separate adversarial stale-callback tests. They are not counted as successful ordinary player UI actions.
- Screenshots are evidence to inspect after execution. This package does not assert that generated text is unclipped, all TMP glyphs exist, or all layouts are visually correct before that review.

## Minimal production fixes included

`XgAppAcquisitionHost.cs`: retain the existing download/install page until the player leaves it, so the next optional shortage thought does not cover Finish/Open.

`XgAppAcquisitionHost.Ui.cs`: route installed Miaoyu through its existing native `MiaoyuView.Open("used")`; Miaoyu has no ordinary router binding. Both an installed saved link and the finished wizard share this path. Zhongbao retains its actual dynamically registered router binding.

`SidebarLayoutChecks.cs`: independent phase19 request, version constant, coroutine, scoped report, 900-second timeout, dedicated marker and cleanup. Existing checks remain their own branches.

## Static validation

`prepare_patch.py` rebuilds minimal diffs against current production, records production byte hashes in `manifest.json`, uses deterministic GUIDs for new assets, and writes UTF-8 LF candidate files. `SHA256SUMS` records candidate bytes. `git apply --check` was successful at preparation time. These checks do not establish Unity compilation or runtime success.
