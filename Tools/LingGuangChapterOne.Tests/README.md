# Chapter-one Core proof

Run from any directory:

```sh
dotnet test /Users/kk/competition/Tools/LingGuangChapterOne.Tests/LingGuangChapterOne.Tests.csproj
```

The harness links the production, Unity-independent files under `Assets/LingGuangV05/Core/` and the same `CoreTests.cs` that Unity runs. Existing v0.8 Core and its harness are untouched.

## Evidence

- `red.log` / `TestResults/red.trx`: initial 30 tests failed against explicit throwing simulation stubs; compilation succeeded.
- `safety-red.log` / `TestResults/safety-red.trx`: two additional genuine failures demonstrated reentrant duplicate submission and duplicate route validation gaps.
- `settlement-red.log` / `TestResults/settlement-red.trx`: two genuine failures demonstrated power-off weight loss and replay from a correction-callback snapshot.
- `green-37.log` / `TestResults/green-37.trx`: 37 passing tests after fixes.
- Unity compilation, JsonUtility roundtrip, Editor tests and desktop Play Mode are separate parent-owned gates. This dotnet result is not a claim of Unity runtime proof.

## Deliberate chapter-one rulings

- Starter gift: one 512M/105W GPU, one two-slot case, ¥650, five editable hidden nodes and directed input-to-both-output routes. Default name is `the code` until renamed. Tunables are public fields of `GameConfig`.
- Human labels award samples and points only when correct. Correcting a wrong simulated suggestion doubles samples and weakens its recorded directed inference route. No repeated card ID can award twice, including synchronous event reentrancy or a snapshot taken from a correction callback.
- The brain is a gameplay simulation, not real neural inference. Seeded suggestion correctness uses the numerical learning curve; displayed confidence is explicitly simulated and is not calibrated model probability. There are 32 authored email explanations; numbered instances are generated indefinitely. The 20-email exam uses distinct templates within each attempt.
- The board uses axial six-neighbor adjacency on q=0..6, r=0..4. Hidden nodes are editable only in q=1..5. Ports are protected. Fan-out/convergence recognize an exact four-hidden-node connected component with precisely three outward/inward spokes, including rotations/reflections; port edges do not count toward that component. Larger or extra-edge motifs are not counted.
- Every pulse uses bounded queue traversal (each neuron fires at most once per pulse), threshold, charge leakage, fatigue, forward conduction, strengthening and powered-idle synapse decay. Recurrent paths cannot loop forever. Reaching an output awards a training step. Both output reachability and min(samples, parameter capacity, steps) affect accuracy; a disconnected board earns no job income.
- Job income begins after three reviewed cards. Exam gates are 12 reviewed cards, 20 output-reaching training steps, at least 65% accuracy, both output paths and working power. Upgrades are helpful but not compulsory. The regression explicitly proves 14 correct reviews followed by 90 simulated seconds reaches the gate from a fresh game without purchases.
- Exam scoring judges the player's answer, with an AI suggestion shown. Failure preserves training and permits retry. The 16/20 passing result grants ¥800 once; chapter remains 1 and chapter two is not implemented.
- Game days represent 24 hours of electricity at ¥0.52/kWh and last 120 real simulation seconds. A daily bill auto-pays when affordable; otherwise income and training stop. Manual correct labels can repay up to ¥1 of actual debt directly per unique card, never create cash; selling spare GPUs is another recovery path. The last gifted GPU cannot be sold. Powered-off weights are preserved while temperature cools.
- Time is advanced in persisted 0.25-second quanta. This yields at most 0.25 s visible latency and deterministic results independent of Tick partition. Invalid/negative/nonfinite deltas do nothing; one call is capped at Config.maxTickSeconds. Fractional time, heartbeat accumulator, active card, RNG, exam deck mask/index/score, route, bill and energy all persist.
- Restores strictly reject invalid versions, nonfinite/negative values, duplicate cells/IDs/routes, malformed/dangling edges, excessive memory, missing ports, invalid question/exam progress and oversized collections with ArgumentException. The parent runtime owns quarantine/backup/fresh-state recovery.

## Cost and boundaries

For N <= 35 neurons and E <= 210 edges, a heartbeat is O(N + E) time and O(N + E) transient storage (queue entries are bounded by edges). A topology edit rebuilds reachability in O(N + E) and exact hidden-component motifs with a simple O(N*E) scan. Time integration is O(ceil(seconds/.25) * (N+E) + heartbeats*(N+E)); all board allocations are bounded. UI events fire once per Tick plus real pulses, rather than once per physics substep.

The model is single-owner/main-thread only. It has no locking, network calls, Unity references, generic event bus, real LLM, random hardware failures or later-chapter content. DTO fields are serialization-accessible; UI must call commands rather than mutate those fields. Human play/balance judgment is not replaced by this deterministic walkthrough.
