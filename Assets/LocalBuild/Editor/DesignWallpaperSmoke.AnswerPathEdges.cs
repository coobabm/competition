using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

public static partial class DesignWallpaperSmoke
{
    // Synchronous rendering fixtures only. The 512 IDs below are temporary position-map entries,
    // never concepts added to the board and never evidence that a player learned this many cells.
    // Call while the real scene already has a path, before yielding to another Unity frame.
    static void CheckAnswerPathEdges(XgWallpaperAnswerPath graphic)
    {
        Check(graphic != null, "Answer-path edge checks have the actual scene graphic");
        var paths = AnswerPaths(graphic);
        Check(paths.Count > 0, "Answer-path edge fixtures start from an actual answer path");
        var exemplar = paths[0];
        var controller = (XingGuangController)typeof(XgWallpaperAnswerPath)
            .GetField("controller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graphic);
        var concepts = (XgWallpaperConcepts)typeof(XgWallpaperAnswerPath)
            .GetField("concepts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(graphic);
        Check(controller?.View?.Juice != null && concepts != null && controller.Sim?.S.board != null,
            "Answer-path edge checks have the production concept mapping and effect settings");
        Check(PathField<Transform>(exemplar, "input") != null && PathField<Transform>(exemplar, "output") != null,
            "Rendering fixtures reuse real input and output transforms");
        Check(graphic.rectTransform.rect.width > 0 && graphic.rectTransform.rect.height > 0,
            "Rendering fixtures have a nonzero actual canvas area");
        var positions = (Dictionary<int, Vector2>)typeof(XgWallpaperConcepts)
            .GetField("positions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(concepts);
        var savedPositions = new Dictionary<int, Vector2>(positions);
        var savedPaths = new object[paths.Count]; paths.CopyTo(savedPaths, 0);
        var juice = controller.View.Juice;
        bool wasReduced = juice.Reduced;
        var board = controller.Sim.S.board;
        string boardBefore = JsonUtility.ToJson(board);
        var mesh = new Mesh { name = "Answer Path Synthetic Edge Check", hideFlags = HideFlags.HideAndDontSave };
        try
        {
            juice.Reduced = true;
            positions.Clear(); paths.Clear();
            int count = XgWallpaperConcepts.MeshCellLimit;
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = int.MinValue + i;
                positions.Add(ids[i], new Vector2(.2f + .6f * (i % 32) / 31, .2f + .6f * (i / 32) / 15));
            }
            for (int i = 0; i < 4; i++) paths.Add(AnswerPathEdgeNewPath(exemplar, ids));
            AnswerPathEdgeMesh(graphic, mesh);
            Check(mesh.vertexCount > count * 4 && mesh.vertexCount < 65000 && mesh.triangles.Length > 0,
                "Synthetic four-path full-LOD Reduced fixture completes real VertexHelper.FillMesh below 65000 vertices: " + mesh.vertexCount);
            foreach (object path in paths)
                Check(PathField<List<Vector2>>(path, "points").Count == count + 2,
                    "Synthetic full-LOD path retains every mapped participant and both real ports");

            var pulls = new List<XgConceptPull>();
            foreach (int id in ids)
                pulls.Add((XgConceptPull)Activator.CreateInstance(typeof(XgConceptPull), BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new object[] { new XgConcept { id = id, w = -.4, s = 2 }, .5d, 1d, false }, null));
            foreach (object path in paths)
            {
                path.GetType().GetField("corrected").SetValue(path, true);
                path.GetType().GetField("pulls").SetValue(path, pulls.AsReadOnly());
                path.GetType().GetField("started").SetValue(path, Time.unscaledTime - XgWallpaperAnswerPath.Duration - .15f);
            }
            foreach (bool reduced in new[] { true, false })
            {
                juice.Reduced = reduced;
                AnswerPathEdgeMesh(graphic, mesh);
                Check(mesh.vertexCount > count * 4 && mesh.vertexCount < 65000,
                    "Four full-LOD correction routes and all changed-cell halos stay below the real mesh limit; Reduced=" + reduced + ": " + mesh.vertexCount);
            }
            juice.Reduced = true;

            // Start with two participants, then move one while an unrelated ID takes its old slot.
            // A frozen coordinate list or a slot-based lookup must fail this case.
            paths.Clear(); positions.Clear();
            int moving = int.MinValue, staying = int.MinValue + 1, unrelated = int.MinValue + 2;
            var oldUv = new Vector2(.24f, .31f);
            var newUv = new Vector2(.43f, .73f);
            var stayUv = new Vector2(.67f, .62f);
            positions.Add(moving, oldUv); positions.Add(staying, stayUv);
            var remapped = AnswerPathEdgeNewPath(exemplar, new[] { moving, staying });
            paths.Add(remapped);
            AnswerPathEdgeMesh(graphic, mesh);
            var points = PathField<List<Vector2>>(remapped, "points");
            Check(points.Count == 4, "Synthetic identity fixture initially renders its two participants");
            Vector2 former = points[1];
            Check(AnswerPathEdgeHasVertex(mesh, former), "Initial participant has a light center in the actual mesh");
            positions[moving] = newUv;
            positions[unrelated] = oldUv;
            AnswerPathEdgeMesh(graphic, mesh);
            Vector2 moved = AnswerPathEdgePoint(graphic, concepts, newUv);
            Check(points.Count == 4 && Vector2.Distance(points[1], moved) < .001f
                && Vector2.Distance(points[1], former) > 1 && AnswerPathEdgeHasVertex(mesh, moved)
                && !AnswerPathEdgeHasVertex(mesh, former),
                "Moved participant follows its identity in the actual mesh; the unrelated occupant of its former slot receives no light center");

            // Simulate LOD/old-save absence only in the drawing map, without deleting a board cell.
            positions.Remove(moving);
            positions[unrelated] = newUv;
            positions[int.MinValue + 3] = oldUv;
            AnswerPathEdgeMesh(graphic, mesh);
            Vector2 retained = AnswerPathEdgePoint(graphic, concepts, stayUv);
            Check(points.Count == 3 && Vector2.Distance(points[1], retained) < .001f
                && AnswerPathEdgeHasVertex(mesh, retained) && !AnswerPathEdgeHasVertex(mesh, moved)
                && !AnswerPathEdgeHasVertex(mesh, former),
                "Absent participant is omitted; unrelated occupants of either old slot are not substituted");
            positions.Remove(staying);
            AnswerPathEdgeMesh(graphic, mesh);
            var input = PathField<Transform>(remapped, "input");
            var output = PathField<Transform>(remapped, "output");
            Check(points.Count == 2
                && Vector2.Distance(points[0], graphic.rectTransform.InverseTransformPoint(input.position)) < .001f
                && Vector2.Distance(points[1], graphic.rectTransform.InverseTransformPoint(output.position)) < .001f,
                "All missing participants leave only the actual input and output, with no fabricated intermediate cell");
        }
        finally
        {
            paths.Clear(); foreach (var path in savedPaths) paths.Add(path);
            positions.Clear(); foreach (var pair in savedPositions) positions.Add(pair.Key, pair.Value);
            juice.Reduced = wasReduced;
            UnityEngine.Object.DestroyImmediate(mesh);
            graphic.SetVerticesDirty();
        }
        bool pathsRestored = paths.Count == savedPaths.Length;
        for (int i = 0; pathsRestored && i < savedPaths.Length; i++)
            pathsRestored = ReferenceEquals(paths[i], savedPaths[i]);
        bool positionsRestored = positions.Count == savedPositions.Count;
        foreach (var pair in savedPositions)
            positionsRestored &= positions.TryGetValue(pair.Key, out var value) && value == pair.Value;
        Check(pathsRestored && positionsRestored && juice.Reduced == wasReduced,
            "Synthetic edge checks restore the original path objects, concept positions and Reduced setting");
        Check(ReferenceEquals(board, controller.Sim.S.board) && JsonUtility.ToJson(board) == boardBefore,
            "Synthetic rendering fixtures leave the real learned board unchanged");
    }

    static object AnswerPathEdgeNewPath(object exemplar, int[] ids)
    {
        var type = exemplar.GetType();
        var path = Activator.CreateInstance(type, true);
        type.GetField("input").SetValue(path, PathField<Transform>(exemplar, "input"));
        type.GetField("output").SetValue(path, PathField<Transform>(exemplar, "output"));
        type.GetField("concepts").SetValue(path, ids);
        type.GetField("started").SetValue(path, Time.unscaledTime - XgWallpaperAnswerPath.Duration * .25f);
        type.GetField("ink").SetValue(path, PathField<Color>(exemplar, "ink"));
        return path;
    }

    static void AnswerPathEdgeMesh(XgWallpaperAnswerPath graphic, Mesh mesh)
    {
        using (var vertices = new VertexHelper())
        {
            typeof(XgWallpaperAnswerPath).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null)
                .Invoke(graphic, new object[] { vertices });
            vertices.FillMesh(mesh); // Exercise the installed UGUI limit, not a duplicate vertex-count formula.
            Check(mesh.vertexCount == vertices.currentVertCount, "Actual answer-path vertices reach a real Unity mesh");
        }
    }

    static Vector2 AnswerPathEdgePoint(XgWallpaperAnswerPath graphic, XgWallpaperConcepts concepts, Vector2 uv)
    {
        var rect = concepts.rectTransform.rect;
        return graphic.rectTransform.InverseTransformPoint(concepts.rectTransform.TransformPoint(rect.min + Vector2.Scale(rect.size, uv)));
    }

    static bool AnswerPathEdgeHasVertex(Mesh mesh, Vector2 point)
    {
        foreach (var vertex in mesh.vertices)
            if (Vector2.Distance(vertex, point) < .001f) return true;
        return false;
    }
}
