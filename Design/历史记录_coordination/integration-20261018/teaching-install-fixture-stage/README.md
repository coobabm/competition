# Teaching all-packs installation fixture candidate

Staged only and paused at the user's request. No production changes, Unity, Player, .NET, model, new task, Git staging or commit. Plain `git apply --check` passed. Patch SHA256: `3ffab3b6f746765f07eea8c790558e657029accba27cf4eb6b76e1f031ea1be4`.

## Actual evidence

The parent-run `LocalVerification/TeachingFullRounds/20261018-233728-939/report.json` stops on `Fixture cannot select make24: 在资料文件夹下载后拖进练页安装`. Its saved fixture has `dataFilesVersion=1`, `pack.make24` in unlocked, and an empty `installedTeachingNodes` list. Earlier Chinese spam_swipe/minesweeper correct and wrong rounds retain both footer lines and pass bounds checks; arith_mole completes settlements. The run does not establish all remaining packs or English passed.

The existing QA creates a new XgSim (which already migrates the data-files schema), then manually grants research/pack ownership. Current production `TeachingPackBlocker` additionally requires `TeachingNodeInstalled` for paid packs. Schema-one fixtures correctly receive no legacy installation grant on reload. This is a stale fixture precondition, not evidence that the production gate should be loosened.

## Candidate

Only `Assets/LocalBuild/Editor/TeachingFullRoundChecks.cs` changes:

- The explicitly all-packs fixture adds the installed teaching-node receipt for each paid pack, deduplicated, alongside its existing manually granted ownership. This is the same receipt production InstallDataFile writes and TeachingNodeInstalled reads. It is declared fixture state, not a claimed real acquisition transaction. No file-download progress, schema downgrade, production bypass, fake UI click or production code change is introduced.
- After normal serialization and BindSave, every paid receipt and all-pack production availability are asserted before starting the UI loop. The existing SelectTeachingPack gate remains in use for every pack.
- Report scope and the fixture comment explicitly disclose installed receipt seeding and exclude purchase/download/install coverage. Correct/wrong gameplay, footer text/gray/glyph/mask checks, timing and the bilingual loop remain unchanged.

Static reads and clean patch application are complete. Runtime compilation and all 19 packs in both languages remain pending after the user's pause. The previous selective staging list already contains this file, so no new source path is needed. Do not represent this unimported candidate or the failing historical report as a completed test pass.
