# URL / pace staging, 2026-10-18

No production files changed. No Unity/compiler/check invocation by this agent.

## Integration
- Apply download-visible-pace.patch to current production (apply --check succeeded before URL addition).
- Copy XgSim.DataUrls.cs to Assets/LingGuangV05/XingGuang/Core/.
- Copy DataDownloadPaceChecks.cs and DataUrlChecks.cs to Assets/LocalBuild/Editor/.
- UI agent must expose DataUrlClue("mnist") in MNIST detail/source clue; wire PreviewDataUrl / DownloadDataUrl in URL UI. Source mapping only MNIST approved by user/root. Other sources remain Unknown until source/economy audit.
- Existing price/date/stage/node authorization routed through BuyDataOffer and BuyNode. Already queued/downloaded/installed/owned returns status; never charges again, never recreates old migrated samples. No real networking.

## Timing limits
New positive-sized files: <=10 MB 3s, <=100 MB 5s, <1 GB 8s, <=10 GB 15s, larger 30s before broadband/junk adjustments. Faster broadband cannot slow files; smaller files floor 2s, >=1GB floor15s, maximum30s. This is PER QUEUED FILE/BATCH, not aggregate multi-batch package duration. Zero-sized metadata remains instant. Old in-flight saves paceVersion0 keep previous remaining time/byte progress deliberately. New queues paceVersion1 persist. Acceleration price constants unchanged but duration-based cost naturally changes. Simulated rate displays no fake real-world MB/s claim.

## Verification scope
Pace checker tests real TickDownloads timing, save/load, small-file progress, no samples before installation and legacy queue timing. URL checker tests normalization, common spoof variants, unknown vs capability failure, no free legacy entitlement redownload. Parent must compile and run centrally, add actual available-source purchase UI check, disk-full and page-hide runtime checks. Test setup grants subsystem prerequisites and does not demonstrate natural progression.
