# Archive runner explicit Main bootstrap

One-line, editor-runner-only follow-up to the already imported archive actual-UI candidate. Original report `LocalVerification/EndingArchiveActual/20261018-221742-167/report.json` remains failed. Its completed-source title archive/replay/all three public letters read-only checks passed; it then timed out at `pre-choice Main loaded` with zero runtime errors.

Cause: the fixture replaces completed working data with the actual copied pre-choice slot, then reloads Main in Single mode during the same Play session. `XingGuangController.Attach` is a RuntimeInitializeOnLoadMethod and only covers the first scene at Play startup. `MainSceneEntry` explicitly documents this lifecycle and provides the production bootstrap. The physical title preloader calls it in `CaseMenuMainDisplay.Configure`; this independent runner omitted it for its explicit fixture reload.

Fix: immediately after that scene load, require `MainSceneEntry.EnsureInitialized()` to succeed. This installs the real production desktop/controllers without changing game state, bypassing gates or replacing the real LCD/title input. The runner's existing sceneLoaded isolation has already installed disabled model components before the bootstrap, so this does not launch inference. All downstream assertions remain exact.

Apply `ending-archive-main-bootstrap.patch`, then rerun the same `EndingArchiveActualUiRunner.Run` under the serial owner. Production `git apply --check` passed. No Unity/compilation/runtime tests were run by this agent. The parent owns the actual rerun.
