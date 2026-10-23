# R2: final execution checklist prepared; release acceptance pending

2026-10-19. This update changes only `COMMANDS.md` and `STATUS.md` in this coordination directory. It does not execute the release checklist. No Unity, Player, model, audio recorder, build or packager was launched; no real save or registry key was accessed; no production files, evidence.json or Claude E1/E2/E3 deliverables were changed; nothing was published.

## Current gates

- D1 and casino actual acceptance have not passed. Do not substitute compilation, source review or historical fixtures for those results.
- The fresh release Player, actual audio/listening, native inference and exact-binary save/exit/reload behavior remain unverified. Cancellation remains unverified unless actually exercised and traced.
- The font owner reports source-manifest synchronization; an independent reviewer is auditing that delivery. This update neither repeats that audit nor declares the font/notice gate passed. New packager checks are requirements, not evidence they have run successfully.
- Claude E1 (latest-version save/exit integrity), E2 (remaining Tieba refresh/lifecycle) and E3 (ending QA and acceptance mapping) are already separately assigned. Parent owns review/integration and execution. No duplicate implementation or test suite is requested here.
- Final package and publication remain pending. Keep required QA, source freeze, build, exact-binary smoke, notice closure, packaging and publication as distinct acceptance steps.

## Current support and protection boundary

Support only the currently designed latest version. Preserve `DefaultCompany/competition`; do not integrate D5 migration, scan or move legacy-save paths, change release identity, or delete current saves. VerificationSavePaths retains legacy path names only as overlap-rejection boundaries; their presence is not migration support or permission to read those locations.

The user's ordinary-save authorization is read-only protection verification. COMMANDS now restricts it to `LocalLow/DefaultCompany/competition/LingGuangV05`, collecting existence, relative paths, lengths and SHA256 before/after the isolated run. It covers all files under that save tree, including slots/chapters/endings and snapshot-archive blocks missed by the old extension filter. It writes reports only into the new evidence directory, rejects reparse paths and leaves missing roots missing. No save content dump, game loading of ordinary saves, repair, copy, migration, move, replacement or deletion is authorized by this check. It does not certify DreamOS profiles outside that tree, music/cache or all user files.

`--verification-save-dir` redirects chapter data, DreamOS profile, model cache and Music2016, but **not PlayerPrefs**. Ordinary startup can still mutate `HKCU\Software\DefaultCompany\competition`. The helper's Snapshot/Compare read that exact key; Restore writes/deletes values and can remove an originally absent product key. Ordinary-save read-only authorization does not imply registry authorization. COMMANDS makes the Player startup and snapshot/restore sequence conditional on the parent confirming existing authorization for that exact scope. This document creates no new approval requirement for already authorized work, but does not invent authorization that is absent. Preserve the private CLIXML outside the public package.

Interactive UI operation uses **sky**, including observation, focus, input and screenshots. The consistency checklist's PowerShell window-helper commands are superseded. If sky is unavailable, retain pending acceptance rather than switch to a PowerShell window assistant. Hidden non-UI process/evidence helpers are distinct from operating the visible game.

## Script and source checks in this update

- Read `IntegratedReleaseBuild.cs`: entry point `IntegratedReleaseBuild.Run`; version `2026.10.19.1`; Windows x64/Mono/D3D11; graphicsJobs=false; exactly ComputerCaseMenu/Main; BuildOptions.None. It preserves company/product and restores its temporary settings. The planned command pins Unity 6000.4.5f1 and requires a fresh successful build record and matching pass log; now it explicitly rejects identity other than DefaultCompany/competition.
- Source capture covers Assets/Packages/ProjectSettings/Design, omitting GGUF and files above 16 MiB. Source must remain frozen through BuildPlayer; captured HEAD alone does not represent dirty working changes. Exactly one canonical built GGUF is required. No model binary was hashed in this update.
- Read VerificationSavePaths and persistence path sources: validated absolute opt-in path, marker and fail-closed isolation; slots, chapters, endings and snapshot blocks stay under the save store. These are source facts, not a new runtime isolation pass.
- Verified actual PlayerPrefs guard parameters: mandatory `-Action Snapshot|Compare|Restore`, `-BuildResult`, `-SnapshotPath`; Restore also needs `-ExitedPlayerProcessId`. Guard checks exact key/SID/build hash and requires the recorded PID to be absent before restore. Snapshot/restore is still not runtime-tested by this update.
- Verified native summary parameters `-TracePath` and `-PlayerLog`; audio Python accepts `--inspect`, `--record`, `--seconds` (1–30) and `--output` (new absolute WAV). Audio capture and listening remain future work.
- Calculated only the small packaging-script hashes: both `release-package-final-candidate/Package-VerifiedRelease-LowLoad.ps1` and `group-art/release-review/Package-VerifiedRelease-LowLoad.ps1` match **f1c46fdbb9f0089a0e5b248b5a7659d19844f2abfae23ffa273850447dad60f7**. COMMANDS pins the final-candidate path and verifies the hash before future execution.
- Read the packager parameter block: mandatory `BuildDirectory`, `OutputDirectory`, `ProjectDirectory`, `LlmUnityDirectory`, `NativeLicenseDirectory`; optional `SevenZip`. The script requires a new output directory, validates build/source linkage, model, artwork, fonts and Oxygen attribution/source assets, copies notices, creates split volumes and performs a 7z integrity test. None of these packaging gates has been executed by this update. A checksum match establishes script identity only.
- The five PowerShell code blocks in COMMANDS were checked with the PowerShell parser only, with zero syntax errors. Parsing does not execute commands or establish runtime success.

## Behavioral checklist corrections and evidence limits

Use `.coordination/integration-20261018/release-smoke-consistency-stage/RELEASE-SMOKE-CHECKLIST.md` for behavioral/native/audio requirements, with COMMANDS taking precedence for initialization, packager SHA/path, sky operation and ordinary-save scope. Do not repeat its setup/snapshot or run its window-helper blocks. Its old `42769a...` packager checksum is obsolete. Its historical THIRD_PARTY import prose is not an instruction to reapply old patches.

Keep the previously reviewed generic-contact correction: use an already visible `oldfriend` or script-free `cousin` (`contactKind=other`) for one deliberate free-text request. Qingwen's YYGirlfriend route bypasses the required generic hub same-ID request/delivery trace. Require the full same-ID native lifecycle plus actual visible reply and timeline; if no eligible contact is available, retain the limitation without progression injection. A plausible bubble, native-ready log or structurally valid trace alone is not a native delivery pass.

Prior source-only inventory and helper parse observations remain historical evidence, not fresh runtime acceptance. Do not promote previous Editor results or packaging success into new Player/audio/native/cancellation claims. COMMANDS is the reviewable plan for parent execution; all current gates above remain open until their actual evidence is supplied.
