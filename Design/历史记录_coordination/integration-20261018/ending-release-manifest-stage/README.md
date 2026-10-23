# R2 virtual manifest and source bibliography candidate

Only `candidate/` has been edited. No production import, Unity/Player execution, C# compilation or NUnit execution has been performed. Candidate tests are supplied for the serial owner.

## Decision and scope

There is no actual exported model package represented by the existing virtual publication transaction. This candidate therefore **does not provide or claim a real model version, weight-file checksum, download, export or upload**. It provides version `xg.virtual-release-manifest/1` of a real game-save metadata snapshot. This is the deliberately limited implementation of R2's version/checksum explanation. Actual model-package verification remains unavailable.

The confirmation page briefly says that a release manifest will be saved in Bodu's discussion archive. The player-facing link/title is “发布清单 / Release manifest”; the detail page uses “清单版本 / Manifest version” (display value 1) and “校验码 / Checksum”. Its only scope note is “仅校验发布说明，不包含模型权重文件。 / Checks the release notes only, excluding model weight files.” A verified manifest shows its five public fields as normal prose rather than raw JSON. Verification failures remain visible, and old saves say “这次发布没有保存清单。 / No manifest was saved for this release.”

Encoding/schema internals, canonical JSON, absence of real exports/uploads, distinctions from authored dialogue hashes, and the fact that integrity does not establish authorship are developer documentation, not player-page exposition. No external publication API is called. This UI revision changes only BoduView.PublicDiscourse.cs and EndingPreparationPanel.cs; the director diff remains limited to the release method's optional capabilities parameter/field and the public-model release projection, with no replay/date-method changes.

## What is persisted and hashed

On a fresh `public_model` commit, the existing detached release candidate receives three optional immutable strings: `publicManifestVersion`, `publicManifestSnapshot`, and `publicManifestSha256`. `publicCapabilities` is also persisted, so all five fields already shown in the confirmation preview are represented: product, description, capabilities, limitations, maintenance/license. The UI host supplies capabilities through a new trailing optional director argument, preserving existing callers.

Canonical snapshot format:

- JSON object with fixed field order: schema, kind, modelFileStatus, product, description, capabilities, limitations, maintenanceAndLicense.
- `schema` is `xg.virtual-release-manifest/1`, `kind` is `virtual_public_release_manifest`, `modelFileStatus` is `not_exported`.
- Two-space field indentation, LF line separators, UTF-8 bytes without BOM, no final LF. Quotes/backslashes are escaped; U+0000–U+001F are lowercase `\uXXXX`. Other characters remain as authored.
- SHA-256 covers only those exact snapshot bytes. It excludes itself, private memory/teaching/chat data, player identity, save identity and prerequisite/completion/public event receipts. It is **not a hash of a model file, the whole save, or a dialogue record**.
- The persisted strings preserve the language of the confirmed preview. UI labels remain bilingual; switching language does not translate, regenerate or rewrite the historical snapshot.

The version denotes the manifest schema, not an invented model build. The existing `ReleaseOutcome.Copy()` safely copies these immutable strings. Caller-supplied manifest fields are discarded and rebuilt on a fresh commit, with the same atomic save/rollback as the release. Non-R2 commits clear incoming manifest fields. Duplicate commits preserve their existing result. No eligibility, completion or date gate is tightened.

Old R2 saves without these fields show “No manifest was saved for this release”; reading, replay and duplicate commit do not backfill them. Incomplete, unsupported or changed manifest data shows an explicit unverified state. The optional personal letter appendix remains **disabled/unconnected**, and no private appendix evidence is invented.

## Bibliography attribution

All 10 source records receive optional `authorsZh`, `authorsEn`, and `publication`. Missing metadata remains accepted by the loader, preserving old catalogs. Shared source rendering adds bilingual labels to all existing adapters. All 48 record objects, source IDs/URLs/date gates, and the dialogue whitelist remain unchanged.

Evidence retained here:

- S01: Nature's page returned a client challenge; Crossref DOI metadata (`S01-crossref.txt`) confirms David Silver and coauthors, Nature volume 529/pages 484–489, 2016. UI abbreviates “et al.” / “等”.
- S02: current official page requests returned HTTP 403. Existing earlier source text `.coordination/integration-20261018/laozhou-news-stage/source-text/tay_microsoft.txt` contains title, 2016-03-25 date, and Peter Lee byline. `S02-author-evidence.txt` records that path and byline only. This is a prior saved official-page observation, not a successful new fetch.
- S03, S04, S05, S07: exact linked arXiv v1 pages' `citation_author` metadata. S05 abbreviates the long list; other lists are complete. `Lukasz Kaiser` follows S07's v1 metadata spelling.
- S06: Stanford AI100 page's recommended citation names Peter Stone and colleagues, “Artificial Intelligence and Life in 2030,” the 2015–2016 Study Panel, Stanford University. UI abbreviates this list. Its existing conservative 2016-12-31 availability gate is unchanged.
- S08–S10: existing linked Hacker News threads confirmed accessible. Attribution is explicitly to thread contributors for discussion context, not to paper authors or Microsoft article authors. No real commenter identities or comments are copied into dialogue.

`source-fetch.json` logs this run's fetch results; downloaded HTML is read-only attribution evidence, not shipped game content.

## Validation and remaining runtime work

`static_verify.py` checks the unchanged 48 discussion records, old source fields/date gates, metadata completeness, and independent canonical fixture SHA256. `package.py` checks C# delimiter balance and baseline drift and packages exact path changes. These are static checks only, not C# type checking or runtime evidence.

Five supplied NUnit cases (four methods): canonical Unicode/escaped-content save roundtrip and tampering; exact transaction rollback/retry and incoming-snapshot replacement; legacy completed release without backfill; online and local routes with no R2 manifest. Fixed canonical fixture is `canonical-fixture.json`, SHA256 `748e5aefb159128f3cca9cfd1dd051108eb9c28282be7c3a043f30e786bbfdff`. Tests have **not been run**.

Serial owner should compile/run the new cases with the existing epilogue/catalog suite, then exercise R2 confirmation, archive link/back, both UI languages, old R2 save, and checksum mismatch in the actual LCD. Layout, clicks, persistence reload and stale-owner behavior are not claimed runtime-verified by this candidate.
