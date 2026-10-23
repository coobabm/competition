# Files feedback candidate (staging only)

## Findings
- Files had no DownloadFinished subscription, so closed folder has no completion notice.
- Train drop already has a success-count toast. Invalid same-save payload and partial batch failure had no explanation.
- Old Data page completion float still says + samples before installation; corrected by separate patch.

## Minimal integration
1. Copy XgFilesWindow.Feedback.cs into the Desktop folder.
2. Apply data-feedback.patch: 2 lifecycle hooks in XgFilesWindow.cs + XgFilesTrainDrop messages. UI agent is editing the main Files class; merge hooks rather than overwrite it.
3. Apply download-no-premature-samples.patch separately (same file as pace patch but distinct method).

Notification uses existing DesktopNotifications.Notify, which already honors muted audio, timing budget, cutscene queue and click-only opening. No focus is acquired on download arrival. It reads ready count after the core commits completion. Bind switches unsubscribe the prior simulation; destruction unsubscribes. Old loaded files are not replayed as new completion events. Clicking checks source identity and opens packs only if permitted. An arrival before buying Data Import tells the player which node is required.

Install partial failures report installed and skipped counts plus closed/expired/gate reason; already-installed/stale IDs report unavailable files. Unrelated native drags and cross-save payloads stay silent. No new install audio is added: current core toast + page feedback are retained, and tray sound uses the established central service.

Verification: patch apply --check passed before final message-only tweak. No Unity/compiler/run. Parent should run drag lifecycle checks and visually check closed/minimized folder, multiple completions, muted audio, held cutscene and save switch. Taskbar/tray notification is existing tray popup, not a new flashing taskbar badge.
