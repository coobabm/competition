# Photo library staged handoff

Status: STAGED ONLY. No main-project files changed, no Unity started. User has redirected the main UI work to an HTML preview; do not automatically merge this until the main thread resumes integration.

## Assets

`selected/teach_*.jpg`: exactly 80 actual photographs, eight categories × ten. Total 2,851,878 bytes. Each source URL and SHA256 is unique. Each category contact sheet has been visually inspected; searches returned unrelated people, prairie dogs, paper-towel dispensers, flowers and tropical fruit, which were discarded. Aircraft final selection depicts 2010 airliners; cars are Toyota Corolla E100; phones are iPhone 6 models. These objects existed in 2016. Do not treat later capture dates in source metadata as dates of fictional game news.

Copy only these 80 `teach_*.jpg` files to `Assets/LingGuangV05/Resources/LingGuangV05/Media/Photos/`. Do not copy contact sheets, search candidate images or compiler binaries into game assets. `selected/teaching_photo_manifest.json` has exact source, author, license, license URL, original URL, transformation and hash. Keep it with source provenance. Append `selected/PHOTO_CREDITS_APPEND.txt` to existing `Media/PHOTO_CREDITS.txt` (do not replace the existing file). Existing CC BY/SA attribution and license terms must remain attached to each derivative.

## Code

`integration/photo-library.patch` modifies five files by diff:
- XgTeachingPictures: new photo80/grid80 kinds use the new 80-image collection, aspect-preserving fit; legacy six-image photo/grid kinds unchanged so notebook images do not silently change.
- XgPackContentGames: captcha grid uses eight categories and independently selected photographs, no duplicate exact photo in a grid; stores exact indices for notebook reconstruction. `stage_photo` uses four choices and five distinct photos per category. Apple-branded phone questions deliberately include apple fruit as a distractor, with phone as the correct answer.
- XgPackGames: routes stage_photo to existing photo game.
- XgStageLessons: adds a fourth supplemental desk, stage_photo, stage three, ImageNet. The 19-pack manifest is unchanged; total desks become 23.
- XgSim.DataPacks: stage_photo requires ImageNet to be owned; normal stage-three gate applies first.

Core and Desktop isolated C# compilation passed; every out/refout goes to integration/, not Library.

## QA

`integration/PhotoLibraryInteractionChecks.cs` compiles against the staged binaries. Execute `PhotoLibraryInteractionChecks.Run` after the assets/code are applied, without -quit. It reuses the previously passed isolated desktop boot fixture. It checks:
- all 80 resources load, rendered-image hashes differ, and saved notebook indices roundtrip;
- six legacy photo mappings remain unchanged;
- ImageNet purchase and stage gates;
- real four-choice buttons, correct and incorrect verdicts, exact saved image pixels;
- alternating typed captcha and actual photo-grid matching, duplicate-submit guard;
- outputs all-80-rendered.png and per-answer PNG/JSON.

Not yet runtime-tested. Hardware keyboard and exact appearance of the new four-choice photo desk still require Unity acceptance. No claim of passing those before execution.

## Other handoffs

ActionGames dynamic-name patch: ../action-names/action-names.patch; isolated compile passed, delivered to root. The earlier 28×28/captcha QA passed per root at LocalVerification/VisualRaster/20261018-062933-104/report.json.

Teach tools remain a separate incomplete work item. See ../visual-raster-qa/TEACH_TOOLS_GAPS.md. No biased threshold / cleaning tray / hard review / true shift inference was implemented in this photo task.
