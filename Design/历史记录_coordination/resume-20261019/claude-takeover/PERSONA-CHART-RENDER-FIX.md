# Personality archive blank chart fix

The actual zh screenshot from `visual-run-3/Evidence/BrainWindow/20261019-135403-663/persona-archive-zh.png` shows visible axis labels/legend/history but no chart geometry. Positive RectTransform dimensions and successful UI callbacks did not verify rendering.

## Confirmed source cause

`XgUi.Rect` creates only a RectTransform and copies the parent layer. `XgPersonaArchiveWindow.Create` adds `XgPersonaArchiveGraphic` to that object. Unlike the project's other custom graphics, this class did not declare CanvasRenderer as a required component.

Installed UGUI `Graphic.cs` requires RectTransform only. Its `Rebuild(CanvasUpdate)` returns immediately when `canvasRenderer == null || canvasRenderer.cull` (package source line 646). Therefore this custom Graphic silently submits no mesh, while TMP child objects render via their own CanvasRenderers. This fits the screenshot exactly; no material/UV/cull workaround is necessary.

## Minimal product change

`Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.PersonaArchive.cs` now declares `[RequireComponent(typeof(CanvasRenderer))]` on `XgPersonaArchiveGraphic`. New runtime construction consequently installs the renderer before the Graphic is enabled. No chart geometry, color, text, layer, material or layout logic was changed. Existing project custom graphics already use the same pattern.

## QA correction

Parent authorized editing `Assets/LocalBuild/Editor/BrainWindowPlayQa.PersonaArchive.cs`. `CheckArchive` now calls `CheckChartRendering` before the existing rendered-desktop capture:

- Force normal canvas rebuild and retrieve the actual submitted mesh with CanvasRenderer.GetMesh.
- Verify a renderer exists, parent canvas is active, renderer is not culled, submitted material/shader exist, own/inherited alpha are positive.
- Verify at least the base chart's 56 vertices / 84 triangle indices, at least 56 nontransparent vertices, and positive submitted mesh bounds.
- Persist both language diagnostics as `persona-chart-zh.json` and `persona-chart-en.json` before assertions. Existing screenshot capture remains intact and still needs visual review.

The desktop sticky note covering the archive close button in the screenshot was not changed; this task addresses the chart disappearance only.

`git diff --check` passed for both files. No Unity launch or compile was performed by this subagent. Parent was notified that the files are released for the next guarded visual run; rendered confirmation remains pending that run.

QA4 reported CS1501 because this Unity 6 CanvasRenderer exposes `GetMesh()` returning Mesh, not the fill-argument overload. The QA now reads that returned mesh directly, with a null guard, and never modifies or destroys it. The temporary mesh allocation and DestroyImmediate cleanup were removed. Product RequireComponent remains unchanged. No Unity was launched for this correction; parent owns the next compile/run. A repeat git diff check was unavailable because the workspace root was no longer recognized as a Git repository.
