using System.Collections;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator DustScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining training, XgWallpaperConcepts concepts)
    {
        TrainingSceneCase("Stardust: explicit 512/1600/16000 real-concept boundary boards, capped rendering without deleting learned data");
        var sim = controller.Sim; var saved = sim.S.board;
        var fixture = new XgBoardState(); string[] regions = { "vision", "sequence", "tone", "logic" };
        int saturatedVertices = 0;
        try
        {
            sim.S.board = fixture;
            foreach (int count in new[] { 512, 1600, 16000 })
            {
                while (fixture.concepts.Count < count)
                {
                    int id = fixture.concepts.Count + 1;
                    fixture.concepts.Add(new XgConcept { id = id, region = regions[id % 4], key = "dust-" + id,
                        w = id % 2 == 0 ? .6 : -.6, s = 4, pinned = id == 1500 });
                }
                fixture.nextId = count + 1;
                yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
                Check(concepts.Cells.Count == 512 && concepts.CollapsedCount == count - 512,
                    "Foreground and collapsed counts account for every concept: " + count);
                int total = 0, groups = 0;
                for (int region = 0; region < 4; region++)
                {
                    total += concepts.CollapsedInRegion(region); groups += concepts.DustGroups(region);
                    Check(concepts.DustGroups(region) <= 16, "Dust group cap holds for region " + region + " at " + count);
                }
                Check(total == count - 512 && groups <= 64, "All overflow is summarized within 64 dust groups");
                var mesh = concepts.canvasRenderer.GetMesh();
                Check(mesh != null && mesh.vertexCount < 65000, "Actual full concept mesh remains below Unity UI vertex limit: " + count);
                if (count == 1600) saturatedVertices = mesh.vertexCount;
                if (count == 16000) Check(mesh.vertexCount <= saturatedVertices + 512,
                    "Ten times as many stored concepts does not grow the saturated dust mesh");
                if (count > 512) Check(concepts.Pins.Count == 1 && concepts.TryLocalPosition(1500, out _),
                    "Protected late concept stays in foreground while other concepts become dust");
                string state = TrainingSceneState(controller, runtime);
                yield return new WaitForSecondsRealtime(.2f);
                Check(state == TrainingSceneState(controller, runtime) && fixture.concepts.Count == count,
                    "Dust rendering never deletes or edits learning state: " + count);
                yield return new WaitForEndOfFrame(); TrainingSceneCapture(training, "concept-dust-" + count);
                if (count == 1600)
                {
                    var draw = typeof(XgWallpaperConcepts).GetMethod("DrawDust", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    using (var vertices = new UnityEngine.UI.VertexHelper())
                    {
                        draw.Invoke(concepts, new object[] { vertices, concepts.rectTransform.rect, 1f });
                        Check(vertices.currentVertCount > 0, "Normal mode renders the existing overflow dust");
                        vertices.Clear(); controller.View.Juice.Reduced = true;
                        draw.Invoke(concepts, new object[] { vertices, concepts.rectTransform.rect, 1f });
                        Check(vertices.currentVertCount == 0 && concepts.CollapsedCount == count - 512,
                            "Reduced mode omits all dust particles while preserving the overflow accounting");
                    }
                    yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
                    TrainingSceneCapture(training, "reduced-static-concepts-no-dust");
                    Check(state == TrainingSceneState(controller, runtime), "Reduced dust switch does not change learning state");
                    controller.View.Juice.Reduced = false;
                    yield return new WaitForSecondsRealtime(.15f);
                }
            }
            foreach (var concept in fixture.concepts) concept.pinned = concept.id <= 512;
            for (int a = 1; a <= 512; a++) for (int hop = 1; hop <= 4; hop++)
                fixture.links.Add(new XgLink { a = a, b = (a + hop - 1) % 512 + 1, c = 1 });
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Check(concepts.Pins.Count == 512 && concepts.Links.Count == 2048 && concepts.canvasRenderer.GetMesh().vertexCount < 65000,
                "Maximum foreground pins plus maximum visible links still fit one Unity UI mesh");
            TrainingSceneCapture(training, "concept-dust-max-pins-links");
            concepts.enabled = false;
            Check(concepts.CollapsedCount == 0, "Disable clears dust aggregates");
            concepts.enabled = true;
            sim.S.board = new XgBoardState(); yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Check(concepts.CollapsedCount == 0 && concepts.Cells.Count == 0, "Fresh board never inherits previous stardust");
        }
        finally { concepts.enabled = true; sim.S.board = saved; }
        yield return new WaitForSecondsRealtime(.2f);
    }
}
