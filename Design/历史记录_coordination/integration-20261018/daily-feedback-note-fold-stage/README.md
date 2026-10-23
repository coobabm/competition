# Daily feedback: normal guide Fold plus explicit GameView fixture

Two editor QA files only. No product changes; production files and historical failure evidence are untouched. No Unity/Player, C# compilation/tests, model or audio execution was started.

## Confirmed failure

`DailyFeedbackActual/20261018-231632-303/report.json` correctly targets native `ChapterOneMinimize`, with source pixel `(1112, 668)`, but its actual production LCD raycast hits `Guide Note/Paper/Task 0`. The screenshot independently shows the note covering the lab caption. Guide depth is 692 and Minimize depth 567. The target is active, interactable, raycastable, and inside the single source LCD bridge.

The complete GameView image is 482x797; the four source-LCD captures are 1280x720. The failure appears in source-LCD geometry too, so the outer GameView size does not explain away or fix the occlusion. This runner previously merely opened/focused the existing GameView and accepted any size above 320x200. It inherited the current editor window/free-aspect selection.

## Minimal candidate

- After dismissing the real assessment, before the hidden-epoch Train click, inspect the installed guide's visible Task 0. If expanded, click its existing `Paper/Title Bar/Fold` button through the same `Tap` → `LcdActualUiProbe.WaitForClick` → `FilesWindowUiChecks.Click` path. Capture before/after LCD images, require the task row to hide and the still-visible note/title to shrink, and verify Fold stays raycastable. Already-folded/absent notes need no action.
- Existing GuideNote behavior changes collapsed height to `TitleHeight + 2` (28 authored pixels) and hides task rows. The runner does not assign collapsed state, disable the note, clear raycasts, reorder siblings, invoke `onClick` directly, or bypass the Minimize probe. Actual native Minimize must still pass its original production raycast and click checks during the epoch.
- Set an explicit, disclosed 1280x720 GameView fixture via the same Unity editor size API already used in `PlayerKnowledgeMenuInputQa`. Save the old selection, create a uniquely named custom size, wait for actual Screen dimensions, then restore the prior selection and remove only this run's tagged custom size at cleanup. Close the view only if this runner originally created it. Source LCD mapping is unchanged.

The normal Fold operation addresses the actual obstruction; the fixed size makes physical-frame evidence reproducible. Neither change claims a product defect was fixed. The historical failed report stays failed.

## Validation

`audit.py` records hashes for all original evidence and verifies image dimensions from PNG headers; it only writes `failure-audit.json` in this stage. `package.py` statically checks delimiters and baseline drift, then produces `daily-feedback-note-fold.patch`.

Remaining runtime validation belongs to the serial owner: confirm actual Fold input hits Fold, before/after geometry clears the native caption, actual Minimize hits ChapterOneMinimize, hidden/reduced completion assertions pass, final save records purchases/epochs, and cleanup restores the prior GameView selection without removing unrelated sizes. No runtime pass is claimed here.
