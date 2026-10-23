# XgDataEconomyTests: six failures, read-only production audit

Evidence: `takeover-editmode-2.xml`, 2026-10-19 13:21:55–13:23:13 UTC, **295 selected, 289 passed, 6 failed**. These tests entered the filter through the `EconomyTests` substring. This report does not relabel the Unity run as passed.

Production project: `competition-latest-20261016`. All source paths below are relative to `Assets/LingGuangV05/`. No production file was edited and no Unity process was launched by this audit.

## Conclusions for each failure

| Test | First failing assertion | Conclusion and minimal repair |
|---|---|---|
| `AJunkPackDownloadsThenAddsSamplesAndNoise` | Tests/Editor/XgDataEconomyTests.cs:135, expected progress .5, actual .0416666666667 | **Existing product estimate mismatch as well as stale later installation assumptions.** CIFAR is 165 MB; junk is 330 MB. `Offer.downloadSec` uses the old formula, rounding .99 to 1 second. Actual `paceVersion=1` download is 12 seconds. Half the advertised second therefore yields .5/12 = .0416667. Fix the live estimate only; retain real half-progress assertion. After completion, assert zero samples, explicitly install, then retain all original samples/noise/ownership assertions. |
| `BuyingThePublicOfferIsBuyingTheTreeNode` | :111, expected `Owns("spam")` immediately after buying | Stale installation assumption. `Has("spam.pack")` correctly passed; `Owns` requires installed batches. Assert purchased entitlement, downloaded-but-uninstalled zero samples, install, then preserve `Owns`, balance and duplicate-purchase checks. **Price assertion :112 was not reached in the failing run.** |
| `DataCleaningHalvesJunkAndCrowdNoiseButNotPublicNoise` | :229, expected 3,600,100 noise rows, actual 100 | Incomplete fixture: `owned` and `dataOffers` contain entitlements, but no pack is installed. Only crowd noise 100 counts. Acquire/download/install both packs at the supported stage, then retain the original public/junk/crowd cleaning arithmetic. |
| `LaoZhouOffersImageNetThroughHisSchoolMail` | :334, expected 1,200,000 samples after download, actual 0 | Stale installation assumption. All prior explicit-request, delayed-answer, duplicate-request, single-reply, price/noise comparison, purchase entitlement and duplicate-public-offer checks passed. Keep every one of those gates. Download then explicitly install all ten files before the original sample assertions. At tier 0 the current real story queue takes 10 × 30 = 300 seconds; old estimate is 307.2, so the old 308.2-second tick already finishes the files. The failed zero is installation, not insufficient ticking. |
| `SuperMembershipFinishesAJunkDownload` | :155, expected 100,000 samples, actual 0 | Stale installation assumption. Acceleration and charge checks preceding the failure passed. Acceleration stores a ready file; it does not install. Assert pending-install zero samples, install, retain original samples and second-acceleration rejection checks. |
| `TheNoiseShareCountsEveryRowNotJustHandLabels` | :178, expected pack noise 120, actual 0 | Incomplete fixture: only `owned.Add("mnist")`, hand labels and label noise. Buy/download/install MNIST, then retain original denominator and noise assertions. |

## Source evidence and provenance

- `XingGuang/Core/XgSim.cs:204`: ownership usable by training requires `InstalledBatches > 0` (or installed extra pack).
- `XingGuang/Core/XgSim.DataFiles.cs:27–39`: installed batches/offers gate public and junk data; :56–75 is the actual install API; :88–107 preserves whole-pack entitlement and existing prices while queuing the full teaching pack.
- `XingGuang/Core/XgSim.DataSources.cs:312–347`: samples and public/junk pack noise count installed data. :494–496 contains the old live duration estimate. :574 routes public purchase through `BuyNode`. :594–595 queues the home-line ImageNet story batch by batch.
- `XingGuang/Core/XgSim.Downloads.cs:256–267`: new paced duration/rate. :346–350 sets `paceVersion=1`. :362–372 stores bytes and a ready file, explicitly leaving installation to the player. :430–440 acceleration calls that same arrival path.
- `DesktopBridge/Taohuo/TaohuoView.Data.cs:69` displays `o.downloadSec` as “下载约 … 秒”; the mismatch is player-facing.
- `git diff HEAD` was empty for DataSources, Downloads, DataFiles and XgDataEconomyTests. `XgSim.cs` changes are persona/archive or quiz handling; `Owns` is unchanged. `XgSim.Progression.cs` only adds persona archive branches in this batch; ordinary data pricing is unchanged. Thus these observed failures have no identified causal connection to the current Tieba/brain integration.
- Design/Claude协作分工_剩余任务各半_20261019.md:18 explicitly preserves existing prices and requires the player to drag data to install. Design/Claude协作总交接_半量分工_20261018.md:133 records paid import, MNIST original-price qualification, and download→drag installation as already integrated foundations.

## Price interpretation

Do not change prices to make tests pass. The ordinary spam dataset node has `cost=300` (XgCatalog.cs:391). The existing ordinary-data branch applies `StoryResearchPrice=.25`, yielding **¥75** with no relic/coupon discount (XgSim.Progression.cs:137–147); the ordinary offer catalog also has ¥75 (DataSources.cs:200). The “approved ordinary price” exceptions in NodeCost apply to listed teaching/board/library/import/etc nodes and are not a blanket exemption for every legacy data pack. The original-price MNIST qualification/import requirements should remain untouched. The candidate explicitly pins spam's current ¥75 and checks offer price against NodeCost before purchase, retaining the original balance assertions.

## Engineering candidate (outside production)

Folder: `data-economy-candidate/` alongside this report.

- `XgSim.DataSources.cs`: only new-purchase estimated duration and the campus reply's displayed duration. Public purchases estimate the full file, junk uses the same paced multiplier, home-line story sums every batch, campus uses its actual paced delivery. `downloadLeft` remains actual queue ETA.
- Deliberately **does not rewrite DataOfferCatalog's legacy duration values**: `ItemRate` still consumes them for `paceVersion=0` campus saves. Rewriting those definitions would change old-save speed and violate the scope. The live Offer estimate describes a fresh purchase; legacy active queue ETA remains authoritative.
- `XgDataEconomyTests.cs`: adds real private `TickDownloads` fixture helper and manual `InstallDataFile` helper, fixes all six fixtures, retains original behavioral checks. The two accounting-only noise fixtures now acquire their real files. The install helper checks missing prerequisite and duplicate rejection.
- `XgOfferDownloadEstimateTests.cs`: compares live estimate with actual queue ETA and actual completion boundary for six public/junk/story offers at three broadband tiers; pins CIFAR junk 12s, whole CIFAR public 8s, ImageNet story 300/150/150s, campus 15s. Also verifies old campus 120s timing survives and a new 1 MB file queues with 3s ETA and advances through the actual private tick.
- `data-economy.patch`: review/apply artifact covering the three source/test files. Parent owns integration; candidate is not applied by this agent.
- `Checks.csproj`, `Program.cs`, `checks.log`: isolated .NET core verification, no production output directories. This is not a substitute for parent-run Unity import/EditMode validation.

No legacy timer, queue rate, price, balance formula, automatic installation, Lao Zhou knowledge gate, request or reply delay was changed.

## Candidate verification result

Isolated .NET execution completed with exit 0: **34/34 passed** (all 29 `XgDataEconomyTests`, including the six corrected cases, plus five parameterized/new timing tests). The three tier cases cover 18 fresh purchase/download scenarios. Output is `data-economy-candidate/checks.log`. Compilation reported only existing core warnings. `git apply --check` for the candidate patch passed; the production-source diff remains empty for the audited four files. Unity revalidation remains the parent's next integration step.
