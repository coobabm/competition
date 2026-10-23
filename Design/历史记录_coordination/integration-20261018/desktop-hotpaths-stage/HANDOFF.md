# Desktop hot-path optimization

Apply desktop-hotpaths.patch (--ignore-space-change), separately copy DesktopHotpathChecks.cs to Assets/LocalBuild/Editor. UI and QA independent compile passed; no Unity run, no measured post-change timings.

Evidence: latest parsed raw 20261018-072513-455-a0e1e4 shows StudyDesktopGates.LateUpdate ~.379–.413ms/frame, DesktopWindowDefaults ~.368–.398, XingGuangController.Update ~.356–.611. These inclusive markers don't prove their child allocation distribution; changes target directly observed repeated work, then require reprofile.

Changes:
- Shared DesktopWindowIndex refreshes native WindowManager discovery every .5s and bridge discovery each1s, retaining previous discovery bounds and inactive/dynamically added objects. Reuses scene roots, component buffers and results via GetComponentsInChildren(List); replaces three independent global array-producing/sorting finds. Does not change default dimensions, offsets, resize bounds, first-open event, or animations.
- StudyDesktopGates.Allows uses existing controller registry; cached Desktop List reference. Stage gating still evaluates/blocks existing windows each LateUpdate and icon changes still share one scan batch.
- SyncProfile compares cached scalar fields and ordered tone words with no concatenation, numeric formatting, string.Join or temporary empty lists on unchanged calls. In-place tone edits, sim replacement and locale changes remain detected. Copies only when values change.
- SaveSlotsPanel/StudyDesktopGates attachment moves from every Update to controller initialization. Both independently keep their own initialization retry behavior.

Run DesktopHotpathChecks.Run in Edit Mode: inactive isolated fixture, actual 2000 SyncProfile calls and GC.GetAllocatedBytesForCurrentThread, profile mutations/null-list/locale, shared discovery result vs real global windows, cached scans, added/destroyed inactive windows, existing default size and preserving player placement. Native first-open animation itself still requires Main verification. Allocation check is strictly this narrow unchanged-profile method, not overall frame GC/FPS. Root owns OriginCurtain/OtherCutscene fixes and those are not duplicated here.
