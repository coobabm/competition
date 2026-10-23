# XgSfx background preload handoff

## Scope and evidence
- Source: IntegrationProfiler/20261018-113929-404-63a1f6 raw capture (before missing-glyph fix). Training frame 121 contains SoundManager.GetHandle > SoundManager.LoadFMODSound > AsyncReadManager.SyncRequest > File.Read (16.8487 ms); assessment frames 154/270/389 also contain synchronous reads. This identifies a real candidate, not a guarantee that every read belongs to XgSfx.
- XgJuice.Sfx.EnsureClips caches AudioClip references. All 58 imported XgSfx WAVs currently disable both preloadAudioData and loadInBackground, so cached references do not establish that PCM has loaded.
- Exactly 58 WAVs, 14,899,216 source bytes (14.21 MiB), total 77.587 seconds; longest 3.798 seconds (Fanfare_v0.wav). Source-sample-count * channels * float32 estimate: 29,793,328 bytes (28.41 MiB). This is NOT measured Unity RAM; importer resampling, runtime representation and overhead can differ.

## Patch behavior
- Enable preloadAudioData and loadInBackground for only those 58 short WAVs. Preserve GUID, compression, quality, loadType and all other import options.
- Explicitly request background audio data after Resources.Load and on subsequent uses of owned standard/Combo clips. Loaded clips clear retry records; Loading skips; Unloaded/Failed retries at most every two realtime seconds via this helper. No loop, await or busy wait.
- External PlayClip path remains exactly unchanged; GameMusic/manual music/playlist untouched.
- Retry map is finite to these resources, cleared on SubsystemRegistration, and releases successful entries on subsequent access. Existing reference caches/pool lifetime unchanged.
- Existing AudioSource.Play still runs normally. The explicit helper cooldown does NOT override Unity's internal retry behavior during Play. We do not suppress or postpone gameplay callbacks/audio events to enforce an engine-wide cooldown.
- Sources, order, randomized variants, gain, pitch, durations and procedural fallbacks unchanged. Load still starts at current Sfx initialization / first combo access; no promise that the first cue has completed loading before Play. Background preload can move work and memory earlier.

## Validation status and required parent checks
- git apply --check passed against current main workspace. No compilation, Unity execution or audible playback was performed by this agent (profiling isolation requirement).
- Apply patch then Unity compilation/import. Verify importer flags on representative standard and Combo clips and observe loadState transitions; test missing source/procedural fallback, disabled-domain-reload play sessions and Failed retry without a tight loop.
- Repeat the same clean three-phase capture after glyph/identity/audio changes; inspect the exact main-thread SoundManager/SyncRequest/File.Read paths, not only averages. Compare memory separately.
- Listen/record real teach combo and training completion playback. Profiler timing does not establish audible continuity, and this patch does not claim it.
