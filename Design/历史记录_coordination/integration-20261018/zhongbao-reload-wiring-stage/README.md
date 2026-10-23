# Zhongbao reload failure: missing wiring lifecycle in frozen QA fixture

Static diagnosis and a QA-only candidate. Production files and the failed artifacts are unchanged. No Unity/.NET or model inference was run. Import only `zhongbao-reload-wiring.patch`; do not replace the whole candidate file. The original run remains failed.

## Evidence

Original run: `LocalVerification/SidebarLayout/20261018-214800-639`, Unity `6000.4.5f1`. Failure is the aggregate assertion in `ZhongbaoBusinessUiChecks.cs:259`.

`analyze_failed_artifacts.py` reads the original atomic save envelopes, verifies each SHA256 payload checksum, then compares the signed archive and restored primary. Results are saved to `existing-failure-evidence.json` with exact file hashes:

- Slot 2: balance 10000, zero signed contracts, totalIncome/totalEarned 0.
- Slot 3 and restored primary: balance **10290**, totalIncome **290**, totalEarned **290**, gameSeconds **0**, exact signed IDs `cheque, acrostic, danmaku`, exact four best-checkpoint records and model IDs.
- The **entire household payload and nested lab payload are equal**, not just the fields in the failed assertion. There is no evidence of double advances, lost orders, money rounding or economic time advancement.
- Saved `wiring.placed` is empty throughout the original run despite the fixture calling these checkpoints deployed/powered.
- Original `...02-signed-danmaku.png` shows three signed cards paying `+¥0.0/s`. The original failure screenshot shows the same three cards paying `+¥0.7/s`, `+¥0.5/s`, `+¥1.0/s`. These are rounded versions of the core factor-1 rates 0.7333333333, 0.5384615385, 1.0; their sum is 2.2717948718.

## Cause

The frozen actual-UI fixture compares rates computed at different wiring initialization states:

1. `ZhongbaoBusinessUiChecks.Frames` disables `ctl.enabled` and the runtime's economic tick. `Bind` restores TestMode=false only after `SetSimulationForTests` has rebound the UI.
2. `XgSim.ContractIncome` multiplies the quality-derived contract rate by `WireSpeed("contract:" + id)` (`XgSim.cs:1012–1018`). `WireSpeed` returns a cached node's factor when a graph exists; otherwise it returns 1 (`XgSim.Wiring.cs:249–253`). It is not a persisted contract/money field.
3. UI reads such as the header's `Sim.Wiring` can construct a preview graph with `BuildWiring(wiringHost, false)` before a real host is attached. With no placement, deployed models are offline (`Not on a card`), so candidate-model task factors are 0 (`Settle`, around lines 785–806).
4. Normal `XingGuangController.Update` calls the lab tick; `TickWiring` rebuilds with the real host and `sync=true`, automatically places models and recomputes factors. That lifecycle is intentionally absent from this QA run. A reload can instead have no cached graph, whose documented default factor is 1. The original aggregate assertion incorrectly reports all of this as an order/balance restoration failure.

The exact original in-memory graph was not dumped, so this diagnosis does not invent one. Empty placements, unchanged disk payloads, visible rates and the source path identify a missing derived-wiring lifecycle in the frozen fixture. The candidate adds live pre/post graph probes to verify the chain during root's next actual run.

## Candidate

Only `Assets/LocalBuild/Editor/ZhongbaoBusinessUiChecks.cs` changes:

- Explicitly calls production `RefreshWiring(ctl.Host)` after a live fixture bind, after actual signing, and after real archive loading. This is the normal graph/automatic-placement operation omitted when the controller tick is disabled. It does not invoke Tick, award money, change clock, or fabricate an income receipt.
- Requires an actual powered rig and online fixture checkpoints. Asserts graph preparation preserves exact wallet, lab income, household earnings, signed IDs, checkpoint JSON and economic seconds.
- Both sides of the reload rate comparison use the actual powered graph; the existing `1e-8` rate tolerance is unchanged.
- Splits the aggregate reload assertion into specific failures. Wallet equality is strengthened to exact equality. Signed IDs/count, checkpoints, income totals and frozen time remain exact checks. Duplicate actual disabled-button click checks remain intact.
- Records before-swap, after-load-before-preparation and after-load-prepared live graph factors/rates and exact persisted business values in `Save/zhongbao-reload-zh.json` and `...-en.json`. Diagnostics are written incrementally before further assertions.
- Scope explicitly names graph preparation as a fixture; it still makes no continuous accrual, natural progression, training or drag-placement claim.

Root run: existing **Tools / Local Verification / Zhongbao business actual LCD**, execute method `ZhongbaoBusinessUiChecks.Run`. Keep the existing failed artifact as failed. The candidate needs serial Unity compilation/run and screenshot review; this directory contains no passing runtime result.
