# Selective source staging audit

No `git add`, commit, restore, reset, fetch, production write, Unity, Player, model, or .NET command was performed by this agent. This directory contains only independent review artifacts.

## Ready-to-use exact path lists

`stage-source-paths.txt` and `stage-source-paths.json` contain the same **531 exact repository-relative paths**, sorted and UTF-8 without BOM. The TXT file uses LF. There are **131 tracked modifications + 400 untracked authored source/resource/documentation paths**. All new non-meta Assets files have matching meta files.

| Group | Selected |
| --- | ---: |
| Assets | 504 |
| Design | 25 |
| Packages | 2 |

The split lists are `stage-tracked-paths.*` and `stage-untracked-source-paths.*`. Preserve earlier integrated runtime code, tests, editor verification/build tooling, resource JSON, the eight licensed photos and their credits, Unity metadata, design decisions, and source/rights records. A previous-turn file was not discarded merely because this agent did not independently re-review every line. Inclusion is not a claim that every selected file was individually code-reviewed in this audit.

The root may execute the following after confirming the worktree has not acquired further changes. **This command is supplied for the root and was not executed here.** The pathspec file avoids Windows command-length limits and `--literal-pathspecs` treats every listed path literally.

```powershell
git -C 'C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016' --literal-pathspecs add --pathspec-from-file='C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-selective-staging-audit-stage/stage-source-paths.txt'
```

The list selects the content present when the root eventually runs that command; it is not a frozen content snapshot. Review newly changed files before reuse. Do not replace it with `git add .`, a directory-wide glob, or forced inclusion of ignored paths.

## Explicit exclusions

`exclude-tracked-generated.*` contains these six paths, rechecked against both HEAD and the earlier `ending-archive-actual-ui-stage/RELEASE_ASSET_AUDIT.md`:

| Path | Verified current difference |
| --- | --- |
| Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset | Generated font-feature additions only |
| Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginMono SDF.asset | Generated feature additions; empty fallback becomes one null fileID item |
| Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginSerif SDF.asset | Only empty fallback becomes one null fileID item |
| Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/ReadingSerif.asset | Generated texture/character/glyph/atlas rectangle data |
| Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/UiSans.asset | Generated texture/character/glyph/feature/atlas rectangle data |
| Assets/LingGuangV05/CaseMenu/Art/Materials/Decor_Ivory Ceramic.mat | Insignificant serialization precision in one color component |

`font-setting-comparison.json` and `generated-font-audit.json` verify that font source, atlas population, multi-atlas, feature-generation switch, clear-on-build, atlas dimensions, padding and render mode remain unchanged. Null fallback serialization is not an added font reference. No intentional font-performance configuration is excluded.

`exclude-tracked-line-endings.*` contains six additional paths whose bytes become exactly equal to HEAD after CRLF/LF normalization:

- Assets/Emergence/VisualDemos/NeuronLife/SparkNeuronDemoFont.asset
- Assets/HolenderGames/UpgradesTreePro/Fonts/Hardpixel-nn51 SDF.asset
- Assets/Settings/PC_RPAsset.asset
- Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
- Packages/Aura/.gitignore
- ProjectSettings/GraphicsSettings.asset

These are separate from the six generated-content exclusions above. They produce no normalized content patch. No files were restored here.

`exclude-untracked-local.*` contains `.vsconfig`, the local Visual Studio workload configuration. It is not needed for the release source change and is not currently ignored. Leave it unstaged; a future local exclude rule is optional. No ignore file was changed.

`ignored-existing-paths.*` records the collapsed already-ignored paths without opening them. The selection excludes `.coordination`, LocalVerification, Library, Build, Logs, Temp, UserSettings, generated IDE projects, downloaded LlamaLib runtime, model weights and their metas. It also excludes secret-file extensions. No ignored directory was force-selected. The audit artifacts themselves are outside the production repository and are not in the source list.

## Sensitive-value scan

The bounded scan examined **533 UTF-8 text files / 34,888 lines**: only added lines in tracked diffs, and full text of untracked source/design files. Patterns covered recognizable API/access tokens, private-key headers, credential-bearing URLs and long literal secret assignments. **No candidate secret matches were found.** `sensitive-scan-locations.json` retains locations only and contains no matched values or context.

Eight image files were treated as binary; three large generated font assets were outside the text size bound and are excluded from staging. Unchanged tracked text, ignored files, external paths and GGUF were not scanned. A bounded pattern scan is not proof that every possible secret is absent.

One nonportable `file:` string occurs in a design-history line, reported only by file/line in the scan report. It is not an active package dependency and did not match a secret pattern. Current `Packages/manifest.json` and `packages-lock.json` use public Git URLs with fixed commits. Their portability correction is intentionally selected; the old asset audit's absolute dependency-path finding is stale.

## Local remote-tracking state

At the final read, branch `main`, HEAD and cached `origin/main` are all `2041e21d97709c8d7f72bc9e0596b002c6653058`, ahead/behind **0/0**. The parent reports it fetched at 07:23 local and found no change; this agent performed no fetch. Cached remote-tracking equality does not certify the remote has not changed since that fetch.

`audit.json` records final counts, timestamps, list digest and reference state. `status-inventory.json` and `tracked-diff-summary.json` preserve the reviewed path inventory. The worktree's status paths were rechecked immediately before writing the final lists and still matched the audit snapshot.
