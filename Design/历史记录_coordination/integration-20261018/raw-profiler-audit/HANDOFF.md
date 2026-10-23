# Offline raw profiler audit

Staged new Editor file: ProfilerRawAudit.cs. Copy into Assets/LocalBuild/Editor only after the active capture exits. No compilation or Unity launched by this agent.

Run in EditMode with recording off:
-executeMethod ProfilerRawAudit.Run -profilerArchiveRoot "LocalVerification/IntegrationProfiler/20261018-113929-404-63a1f6" -quit

If root omitted, selects lexically latest COMPLETE capture only. A new raw-analysis-TIMESTAMP subdirectory holds outputs; never overwrites raw, JSON, old hotspot reports or prior analysis. A missing/empty raw, failed load, empty loaded frame range, zero valid frames or absent Main Thread fails instead of producing Frames:0 success.

Outputs per phase:
- markers.tsv: every valid thread/marker, actual per-thread valid-frame denominator, calls, inclusive mean, maximum SINGLE sample with exact frame and ancestor path, self mean (NA when child timing invalid/overlapping).
- frames.tsv: every valid thread-frame, frame duration, largest sample and path. Sort main-thread frame durations to locate spikes, then inspect matching peaks.
- sample-peaks.tsv: >=2ms script/audio/GC/log/TMP samples and ANY >=2ms self time, retaining frame/thread/path.
- anomalies.tsv: negative/nonfinite raw sample times excluded and recorded; invalid original TimeNanoseconds JSON mean/max flagged separately (e.g. Audio.Thread). Does not convert negatives into zero or claim audio is fixed.
- scope.txt: source path, loaded range, valid frames, actual thread count, invalid sample count.

APIs checked against Unity 6000.4.5f1's UnityEditor.CoreModule.xml: frameTimeMs/threadGroupName/threadName/threadId and GetSampleChildrenCountRecursive. No compile validation yet. Self time subtracts immediate children only; negative/overlap or invalid-child results are marked NA, not clamped into fabricated evidence.

New capture scope caveat: parent observed repeated missing ✗ warnings on Probe/UiSans during train-auto. This raw is BEFORE glyph fix. Attribute slow frames using exact script/log/self sample paths rather than call all of it general rendering. No music continuity claim from audio-marker mean/max; negative recorder means explicitly invalidate those counters.
