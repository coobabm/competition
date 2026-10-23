# Ordinary Player: explicit verification save directory

Candidate only; no production modifications, Unity, compilation, Player, model, or tests executed. Apply `release-verification-dir.patch` from the production project root, then root must compile and run the authored QA serially. This is file-data isolation, not a claim of complete Player smoke success.

Usage after importing and building this candidate:

```text
MyComputerAlive.exe --verification-save-dir "C:\absolute\test-only\new-or-marked-directory" -logFile "C:\absolute\evidence\player.log"
```

No RECORDING_PREVIEW, Windows account creation, company/product changes, registry identity changes, or model start by the tool are involved. Without the option, existing normal save paths remain selected. The option is intentionally `--verification-save-dir` followed by a separate value; equals syntax is rejected instead of silently ignored.

## Confirmed existing storage behavior

AtomicSaveStore's constructor only normalizes a directory and appends chapter-one.sav. It does not create or preflight the directory. Save() catches write failures and returns false, preserving original data; it never falls back to LocalLow. The place needing protection is ChapterOneRuntime's empty-override fallback before constructing that store.

The new resolver runs before that construction (even before the runtime sets initialized). It caches any explicit-option error and continues throwing on all later accesses. A BeforeSplashScreen hook logs and requests exit 64; because Quit is asynchronous, the runtime, DreamOS and model bootstrap independently access the resolver and refuse to proceed after rejection. Invalid arguments cannot produce a null override and accidentally open the normal store.

## Directory policy

- One explicit option, nonempty absolute local Windows path. Missing/duplicate/equals/relative/drive-relative/UNC/device paths and drive roots reject.
- Reject current persistentDataPath, known earlier installed identity roots (RippleLocalPreview/LingGuang-Playability-20261008 and DefaultCompany/competition), and the running build/project directory, including their ancestors and descendants.
- Reject reparse points in the existing ancestor chain and anywhere inside a reused directory; reject short-name `~`, ADS, trailing-dot/space aliases and invalid characters. No links are followed when scanning a marked tree.
- First use accepts a missing or empty directory only. Writes `.lingguang-verification-save` with a format tag, exact canonical root and nonce. Existing nonempty data without that marker is never adopted. Existing marker must match this exact directory, be valid and small; a copied marker from another path is rejected.
- Directory creation, marker read/write and a new owned write-probe must succeed. Failure propagates; no normal-storage fallback, overwrite of old marker, or deletion of user data. The write-probe is the only file removed, after successful exclusive CreateNew.
- Run one verification Player at a time. This is not an adversarial concurrent filesystem sandbox; do not externally change links/mounts while it runs.

## Redirected data

The supplied directory is the AtomicSaveStore root directly: chapter-one.sav, backup/archive/slots and player-profile.json remain in that one override. Character/AI/YY state serialized inside GameState continues to use the same runtime save. Return-to-title and scene reload reuse the same static verified path; a later process may reuse the directory only with its bound valid marker.

DreamOS profile categories go into `DesktopProfile/` via the actual InitializeProfile path; model embeddings cache goes into `ModelCache/`; Music2016's guide/local-music folder goes into `Music2016/`. These last two avoid hidden normal-persistentDataPath writes during the requested model/audio smoke. StreamingAssets model/native libraries remain read-only production sources. XgMemoryNotebook already suppresses standalone text export for a redirected store; that existing guard is retained.

The candidate does not virtualize Unity PlayerPrefs/HKCU: title/graphics/language/music settings may still update ordinary preference keys, and Unity may maintain native window preferences. It does not modify their identity. If the acceptance constraint includes those preference values, that is an additional explicit isolation task; file-save isolation must not be described as HKCU-zero-write. Legacy Emergence/LingGuang scenes are not included by the ordinary two-scene IntegratedReleaseBuild and are outside this candidate.

Explicit snapshot import/export remains an intentional user-selected external operation. Do not perform it during this smoke; this flag redirects the automatic game-owned stores, not arbitrary user export destinations. No existing player save/profile is imported by the switch.

## Low-load QA prepared for root

`VerificationSavePathsTests` adds 14 filesystem-only NUnit cases under the existing Persistence.Tests assembly. Each uses its own temp tree and a protected sentinel; no game scene/model. Cases cover disabled/no side effects, fresh directory marker + normal AtomicSaveStore/PlayerProfileStore placement, empty directory, nonempty unmarked refusal, missing/duplicate/equals/relative/UNC arguments, protected ancestry, copied marker, file blockers/creation failure and path aliases. Teardown verifies the protected sentinel then removes only its verified named temp tree. No tests were executed by this agent.

Root should compile, run this fixture, then in the final Player serially verify invalid-path exit/log and absence of normal save modification; verify valid-path title→start saves, profile and DreamOS data inside the supplied root; close/relaunch to check marked reuse. Compare ordinary-save files read-only before/after, never restore by overwriting them. Reparse rejection is implemented but requires a separately controlled Windows link test; it is not claimed tested here. Runtime callback timing, built-Player exit and full save/profile reload also remain unverified until root runs them.

Package contents: five existing files (ChapterOneRuntime, DreamOSDataManager, LocalEmbedder, LocalLlm, Music2016), two new C# files and their metas. `package.py` checks delimiters/baseline drift and generates the patch; static checks do not establish compilation. `build.py` is one-shot; LocalLlm's additional bootstrap guard was added after it, so do not regenerate candidates.
