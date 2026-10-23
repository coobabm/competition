# Binary teaching tools — staged only

Read fourth-round §3.3 and Training Techniques §§3.1,3.3 as referenced; the newest rule keeps truth judgments independent of AI answers. No Unity started and no main edits.

`binary-teaching-tools.patch` contains independent changes to the Swipe class, core new partial, Hall toolbar/new partial, and sorting/overlay Tick guards. It does not duplicate the prior photo patch (both touch XgPackContentGames in disjoint classes). Apply by diff, not whole-file copy. XgSim.cs and payment/Answer rules untouched.

Implemented scope: spam/headline/review binary desks. Purchase bias adds a persistent per-dataset .05–.95 threshold slider. It changes actual Board model suggestion, never the statement truth. Sigmoid enables confidence from logistic actual Board score, not C.Skill random rolls. This is a model readout, not measured accuracy. Hall toggle orders eligible desks by actual candidate uncertainty and subsequent text rounds choose the least certain of 12 eligible real phrases. Data-cleaning and hard-review trays are based on actual disagreements/mistakes and gated independently. Their overlay uses actual yes/no judgment; wrong review stays pending, correct resolves, duplicate grants nothing. Modal overlay pauses underlying question Tick; stale save callbacks are guarded.

Validation: Core and Desktop isolated compilers pass. 89 pure-core assertions pass including trained p=.944, real threshold response, statement truth unchanged, purchase gates, old null defaults, per-dataset independence, candidate-min ranking, future/family exclusion, review dedupe/wrong/right/no currency mint, and save roundtrip.

`TeachingToolsInteractionChecks.cs` compiles; not run. Execute Run after parent applies. Tests actual Slider callbacks, confidence text, wrong answer→tray→wrong/correct/duplicate review buttons and unsure-sort UI. It does not yet cover narrow-screen placement or all buy-before/after screenshots.

Still incomplete, do not mark entire tool requirement done: classic yes/no page needs threshold/real-confidence integration; all nonbinary pack answers still have mixed heuristic confidence; suspect tray currently reviews actual player-labelled binary cards, not historical imported noisy datasets; no overnight automatic cleaning; no four-image augmentation yet; true shifted-image phenomenon remains separate work. No actual saved noisy-label count is silently decremented because these manual rejected answers were never accepted training noise.
