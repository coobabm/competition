# Vocabulary incremental prewarm candidate — NOT RUN

Status: staging only. No production files edited, no Unity launched, no compilation or runtime tests performed. `git apply --check` accepted this patch after the training-signature reuse patch in an isolated `merge-check` copy; that is only patch compatibility evidence.

## Integrate

Apply `vocabulary-prewarm.patch` by hunks. Do not copy the staged whole `XgBoardData.cs` or `XgTestSetPreparation.cs` over production: those snapshots do not contain the other agent's training-signature reuse edits. Verified patch order in scratch: `testset-prewarm-cache-review/reuse-training-signatures.patch`, then this patch. The former agent's `TestSetPreparationChecks.cs` remains untouched by this patch.

Changes:

- `XgBoardData.cs`: one word, `partial`; original synchronous Vocabulary function unchanged.
- New `XgBoardData.VocabularyPreparation.cs`: internal pending iterator owned only by the current test-set job. One call to Pick / one unique bigram count / one final candidate word per work unit. No lock spans a yield/frame. An already complete synchronous entry terminates redundant pending work and retains its identity.
- `XgTestSetPreparation.cs`: two private fields and a pre-training vocabulary phase in Advance. A finished vocabulary is published before generating any training/candidate cards. A zero quota creates no vocabulary work.
- New isolated EditMode check entry, with .meta files.

## Exactness and ownership

The iterator uses the identical process-local `new Random(dataset.GetHashCode() & 0x7fffffff)`, 4000 original `XgMemes.Pick(dataset, r, XgMemes.MaxLevel, 20161231, "")` calls (including the original `includeFamily=true` default), original HashSet insertion and enumeration order, original Bigrams iterator (per-phrase de-duplication), full dictionary counts, threshold >=2, and final dictionary enumeration order. No sorted or occurrence-ordered surrogate is used.

Only a fully built vocabulary is published under the original dictionary lock, and never replaces an entry made by the synchronous path. Before completion, the original synchronous Vocabulary still sees a miss and runs its full original code. No global pending data exists. CancelTestSetPreparation drops the sole job owner, so cancellation, dataset/context changes and the existing OnDisable/OnDestroy cancellation cannot continue or publish abandoned private work. A fully completed vocabulary may remain globally cached, as explicitly permitted.

Routing mirrors actual `Make` early returns and `FromCard` branches, so danmu/translate and all vision/logic/explicit sequence tasks do no vocabulary prewarm. spam/headline/review/sense and default Words routing do.

## Verification entry points — supplied, NOT executed

After integrating in an isolated project, first run `VocabularyPreparationChecks.MeasureFirstPick` after a fresh domain reload (menu: LingGuang / Verification / Vocabulary first Pick initialization timing). It times explicit type-initializer request, first Pick and second Pick; cold type-initializer status is conditional on it not already having run in that domain.

Then run `VocabularyPreparationChecks.Run` (menu: LingGuang / Verification / Vocabulary preparation cold warm cancellation). It reports real-function cache-cold/warm Vocabulary and TestSet timings; incremental first/max Advance(1), total and unit counts; exact full ordered cards; vocabulary membership and enumeration order; only-Words routing; quota zero; 4000-pick boundary; cancellation at 1/3999/4000/4001 units; dataset/owner isolation; complete-vocabulary cancellation; synchronous fallback during partial build with singleton identity preserved; chunks 7/32/4096 and exam purpose. Existing global dictionary references are restored in finally. Cache-cold tests do not reset CLR/JIT initialization. Test success logs and timing files are emitted only if that entry actually runs successfully.

The new guard is derived from the finite bank: `4000 + 2 * sum(max(0, text.Length-1)) + 1 + 4096 + 1600 + 400 + 1`. Every selected distinct phrase belongs to Bank; its unique bigrams cannot exceed its character-pair positions; final dictionary entries cannot exceed their total. This bounds vocabulary work plus all original card phases. The existing TestSetPreparationChecks Build guard needs no change because Reference runs first and warms Vocabulary; its Finish fixtures use mnist/meme/logic, which never use Words.

## Remaining cold work

`XgMemes` is a single partial static type with static initializers for Topics, HotWords, Danmu, Spam, Headline, Review, Chinglish and Sense (the latter from `XgMemes.Sense.cs`). Its first access can initialize all those arrays and phrase objects together. This candidate does NOT divide that CLR static initializer; the first incremental Pick can still carry type initialization, JIT, List allocation/scanning, collection growth, or GC. Pick itself has no lazy 4000-call cache: each call gets Bank, builds a candidate List, scans weights, and chooses. No new reduced-bank or altered-Pick optimization is included.

Consequently this removes the known single-step 4000-Pick loop, but cannot claim every Advance(1) is below 1ms without actual profiling. The prewarmer's 1ms budget remains a cooperative check between indivisible units.
