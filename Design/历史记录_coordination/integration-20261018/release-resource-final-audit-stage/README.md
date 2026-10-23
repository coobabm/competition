# Final release attribution: lightweight recheck

Scope: current integration's new photo assets, source/rights records, fonts, and the packaging copy paths. Read-only against production and packaging helpers; no model or GGUF reads, no image rehash, no Unity/Player/.NET, no build or archive operation. This is a source-and-script audit, not inspection of a newly built release archive.

## Result

No confirmed case was found where this round's required photo attribution exists only in Design and is omitted by the selected `Package-VerifiedRelease-LowLoad.ps1` path. All eight newly added photographs have author/source/license/change records and explicit copy steps. The main photo credits point readers to the two added credit files.

There is one concrete alternative-path gap: the older `Package-VerifiedRelease.ps1` only copies `PHOTO_CREDITS.txt`; it does not copy the two new credit files that document the trial and group-chat photographs. Its main credit file now references those absent files. Use the LowLoad helper for this release. If the old helper must remain an accepted entry point, transplant the LowLoad helper's three-file `$photoCredits` validation/copy loops into it. No product change is needed.

## New photo assets and distribution paths

All source assets are under `Assets/LingGuangV05/Resources/LingGuangV05/Media/Photos/`; credits are in their parent `Media/` directory. `B` below means the final copied game directory: `<OutputDirectory>/payload/<BuildDirectory basename>`.

| Asset | Author / permission recorded | Credit file copied to B/ThirdPartyNotices/ |
| --- | --- | --- |
| trial_netbar.jpg | User:Vmenkov / CC BY-SA 3.0 | ENDING_TRIAL_PHOTO_CREDITS.txt |
| trial_gpubox.jpg | Evan-Amos / public-domain dedication | ENDING_TRIAL_PHOTO_CREDITS.txt |
| trial_westlake.jpg | Hermann Luyken / CC0 | ENDING_TRIAL_PHOTO_CREDITS.txt |
| trial_serverroom.png | Akramusns / CC BY-SA 4.0 | ENDING_TRIAL_PHOTO_CREDITS.txt |
| trial_drafts.jpg | Vyacheslav Argenberg / CC BY 4.0 | ENDING_TRIAL_PHOTO_CREDITS.txt |
| chat_lili_qinghai.jpg | Jucember / CC BY-SA 3.0, including cropped adaptation | GROUP_CHAT_PHOTO_CREDITS.txt |
| chat_ajie_1080.jpg | Manifest Interactive / CC BY 2.0 | GROUP_CHAT_PHOTO_CREDITS.txt |
| chat_ajie_noodles.jpg | William Christiansen / CC BY 2.0 | GROUP_CHAT_PHOTO_CREDITS.txt |

The two credit files include source URLs, license URLs (or the public-domain source-page statement), and modifications. The credits distinguish licensed reference photos from fictional characters' own photographs and from newly AI-generated images. The GPU/West Lake/handwritten-sheet trial descriptions were explicitly adjusted by D016; the outdated original 2/5 staging README must not be used as the current shipping inventory.

The LowLoad helper checks all three credit files before creating output, copies the raw build, then copies all three to `B/ThirdPartyNotices/`. Resources embedding alone is not being counted as a readable standalone credit file; the explicit packaging copy supplies those files.

## Other source/rights records

| Material | Actual shipping route | Finding |
| --- | --- | --- |
| Library: 100 titles/authors, 92 original guides + 8 retained excerpts | `XgBooks.Library.Data.cs` compiles metadata/rights; `BoduView.Library.Metadata.cs` contains all eight original-text source URLs and renders them with bibliographic links via `BoduView.Library.cs` | The full CSV audit remains in Design, but author/material/source attribution is also in the product. No new Design-only attribution gap found. Copying the CSV as `B/ThirdPartyNotices/LIBRARY_SOURCES.csv` is optional documentation completeness, not a blocker established by this audit. |
| Epilogue public discussion sources S01–S10 | `Resources/LingGuangV05/Epilogue/ending_public_discourse.json` contains bilingual authors, publication and URLs; `XgPublicDiscourseDesktop.Sources` renders them through Bodu | All ten have those fields. Design is not the only copy. The discussions are authored contextual text, not copied paper figures or authentic user comments. |
| MiSans, Inter, Source Han Serif | `Assets/StreamingAssets/Licenses/Fonts/` → `B/MyComputerAlive_Data/StreamingAssets/Licenses/Fonts/` through the raw-build recursive copy | FONT_CREDITS, source manifest, Inter/OFL text, Source Han Serif/OFL text and MiSans PDF are present. No separate Design-only dependency. |
| Existing OriginSerif opening font | Bundled font/TMP resource, with the same OFL/copyright text already in the above SourceHanSerif-LICENSE.txt | Compared the two small license texts: normalized text is identical, byte difference is formatting. Do not misreport its separate source license filename as a missing legal notice. |
| Hongmeng/Lucide and Blackjack | LowLoad copies `Assets/HongmengOS/Licenses` and `Assets/LingGuangV05/ThirdParty` recursively to `B/ThirdPartyNotices/` | Explicit copy paths remain present. Oxygen notices/licenses already live under StreamingAssets. |
| Qwen3 model | Existing model-side LICENSE/MODEL_CARD in StreamingAssets → copied raw build | LowLoad requires both files. GGUF was not opened or rehashed in this audit. |
| LLMUnity/USearch/native runtime | Explicit LowLoad copies to `B/ThirdPartyNotices/LLMUnity-*`, `USearch-LICENSE`, and `LlamaLib-Native/` | Existing packaging requirement retained; no new native archive audit is claimed. |

## Minimal release actions

1. Keep `Package-VerifiedRelease-LowLoad.ps1` as the selected packager. If anyone proposes the older helper, first align its new-photo credit checks and copies; do not distribute a package whose main credits link to absent files.
2. After the actual build/package, inspect the final `B/ThirdPartyNotices/` for all three photo credit files and the StreamingAssets font/model notices. This read-only existence/content check remains pending; a script path audit is not proof that a specific final archive already contains them.
3. No product or Design edits are recommended for the eight new photos. No additional license download was needed. Optional standalone library/source-index copies may improve readability but should not be represented as missing attribution already required by these observed materials.
