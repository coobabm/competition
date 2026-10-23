# Public reading interaction candidate

Staging-only candidate. No production writes, Unity/.NET launch, inference, commit or fetch. No runtime pass claim.

## Import

Apply `public-reading-interaction.patch` as minimal hunks to the two existing files. Copy only the new source and meta listed in `new-files.json` from `new/`. Do not copy whole baseline/candidate existing files: root edits are concurrent.

`SidebarLayoutChecks` gets a distinct phase 20 hook, timeout 240 seconds, scoped report/captures and cleanup. `Run()` refuses a partial import missing this hook instead of accidentally falling into BookLibrary's suite.

Unity entry: `PublicReadingInteractionUiChecks.Run` (without `-quit`, normal shared runner graphics/input requirements). Root must run serially and inspect screenshots. Outputs are under the new `LocalVerification/SidebarLayout/<stamp>/` run.

## Runtime patch

- Separates `PresentationHeld` from pre-scheduling `InputBusy()` and tray busy state. A pointer-down must not deactivate the popup button that needs its corresponding pointer-up.
- Resolves the tray before scheduler advancement. Existing tray activity earns no safe time and resets the 30-safe-second quiet interval. Never puts the popup's own `tray.IsBusy` into its presentation callback.
- `LifeEventOpen` and `LifeEventResultPending` block new optional prompts and invalidate queued/displayed optional prompts. Invalidation releases the native tray for important queued notices; merely hiding a held optional notice would block its queue. The accepted optional delivery keeps its one-stage receipt, avoiding a repeated interruption; public archives remain available.
- Existing owner token, once-per-stage, throttles, archive and private-chat behavior are preserved.

## Runnable evidence scope

- **Three photos, six captures:** actual native YY contact LCD clicks, imported resource sprite identity, distinct textures, aspect/visibility, exact bilingual caption, no text truncation and viewport bounds. Messages are explicit fixtures copied verbatim from the three authored era events. This is neither natural delivery nor all 13 required pictures. Imported photos are licensed third-party images, not generated art; credits already live in `GROUP_CHAT_PHOTO_CREDITS.txt`.
- **Notifier:** production `Update`, actual tray, focus on a native TMP field, synthetic `InputState.Change` mouse button state, and production LCD surface pointer-down/up across at least three real frames. Automatic InputSystem UI modules are disabled only during this portion to avoid duplicate events. This is actual native UI routing with a synthetic input fixture, not physical OS input.
- **Timing:** pure scheduler exact 29/30 boundary and background check; actual live tray busy earns no safe time; native close then a naturally elapsed 30-safe-second interval before actual optional prompt acceptance. Stage, date and initial elapsed eligibility remain explicit fixtures.
- **Priority:** explicit life-open and result-pending fixtures block scheduling. An explicit life-open state invalidates a displayed optional popup and allows a queued priority-labelled tray fixture to render. The real life-event presenter is frozen, so this is queue-priority evidence, not a natural life-event or economic outcome.
- Independent controller/economic/story/daily/app drivers are frozen, no model runs. Finally restores source owner, locale, driver/input state, native windows and cleans test-owned mouse/transient tray.

The package has only static review and patch applicability checks until root runs Unity. Any compile or runtime failure must be reported as such; the runner cannot create `passed.txt` before its entire iterator finishes. Six photo and five notifier captures plus a failure capture are available for visual review.
