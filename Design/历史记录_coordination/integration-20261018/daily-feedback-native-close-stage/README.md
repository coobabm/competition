# Native close: clear pending rhythm

One product file, no new test or test edit. Apply only after the current Daily run ends, as requested by the serial owner.

Confirmed statically: WindowManager.CloseWindow sets isOn=false synchronously then invokes onClose; XingGuangView.Update returns when Visible=false, so TickRhythm cannot clear a previously pending accent. A quick close/reopen inside its deadline can otherwise replay it. The candidate keeps transient-toast clearing and adds the existing CancelRhythmFeedback call in a named native close handler. It changes no simulation, save or epoch data and removes that same listener on destruction.

Restore paths: TaskbarButton.Initialize's normal onClick and context header both call WindowManager.OpenWindow; XingGuangController.Open calls ChapterOneDesktopRouter.Open. Native OpenWindow sets isOn=true, activates the GameObject, invokes onOpen and starts In. The existing NativeWindowOpened reset therefore covers native taskbar restoration. This candidate does not change that path.

Static package and git apply --check only; no Unity, Player, C#/.NET compilation/tests, model or audio run. Runtime close-with-pending accent remains unverified; the current Daily suite checks minimize, not that distinct race.
