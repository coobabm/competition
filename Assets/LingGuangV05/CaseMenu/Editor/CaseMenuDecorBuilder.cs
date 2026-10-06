using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LingGuangV05.CaseMenu.Editor
{
    /// <summary>
    /// Menu-owned wall prints and lived-in programmer desk props. Never creates lights, colliders or input.
    /// Existing decor survives missing poster imports; generated assets are updated without changing GUIDs.
    /// </summary>
    public static class CaseMenuDecorBuilder
    {
        public const string RootName = "ProgrammerDeskDecor";
        private const string PosterFolder = CaseMenuModelBuilder.ArtRoot + "/Posters/";
        private static readonly Quaternion FaceUp = Quaternion.Euler(90, 0, 0);

        public static GameObject Build(Transform parent)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (parent != null && parent.gameObject.scene != scene)
                throw new ArgumentException("Programmer desk decor must be built in the active scene.", nameof(parent));

            string[] posterNames = { "ThreeBody", "Turing", "VonNeumann" };
            string[] posterFiles = { "ThreeBody-v2.png", "Turing-v2.png", "VonNeumann.png" };
            var textures = new Texture2D[posterNames.Length];
            for (int i = 0; i < posterNames.Length; i++)
            {
                string path = PosterFolder + posterFiles[i];
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (textures[i] == null)
                    throw new InvalidOperationException("Cannot build programmer desk decor: missing or unimported poster texture at " + path + ". Existing decor has not been removed.");
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Programmer desk decor requires a Lit shader.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset") ?? TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("Programmer desk decor requires the existing Desktop CJK or TMP default font.");
            EnsureFolder(CaseMenuModelBuilder.ArtRoot + "/Meshes");
            EnsureFolder(CaseMenuModelBuilder.ArtRoot + "/Materials");

            var replacements = new List<GameObject>();
            if (parent == null)
            {
                foreach (GameObject candidate in scene.GetRootGameObjects())
                    if (candidate.name == RootName) replacements.Add(candidate);
            }
            else
            {
                for (int i = 0; i < parent.childCount; i++)
                    if (parent.GetChild(i).name == RootName) replacements.Add(parent.GetChild(i).gameObject);
            }
            foreach (GameObject previous in replacements) Undo.DestroyObjectImmediate(previous);
            Transform root = Empty(parent, RootName, Vector3.zero);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build programmer desk decor");
            var context = new DecorContext(root, shader, font);
            BuildPosters(context, posterNames, textures);
            BuildCoffee(context);
            BuildBookStack(context);
            BuildStickyNotes(context);
            BuildOpenNotebook(context);
            context.SaveBatches();
            EditorUtility.SetDirty(root.gameObject);
            return root.gameObject;
        }

        private static void BuildPosters(DecorContext c, string[] names, Texture2D[] textures)
        {
            Material paper = c.Material("Paper Edges", "8C8472", .13f);
            Material tape = c.Material("Warm Masking Tape", "AA9A72", .08f);
            Material curl = c.Material("Paper Curl Shadow", "5C594C", .08f);
            Vector3[] centres = { new Vector3(-.80f, 1.55f, 1.399f), new Vector3(0, 1.50f, 1.398f), new Vector3(.76f, 1.50f, 1.399f) };
            float[] widths = { .55f, .44f, .46f };
            float[] tilts = { 2f, -1f, .6f };
            for (int i = 0; i < names.Length; i++)
            {
                float width = widths[i], height = width * 1.5f;
                Transform mount = Empty(c.Root, "PosterMount_" + names[i], centres[i]);
                // Tiny misalignment keeps these attached paper prints, not a showroom picture wall.
                mount.localRotation = Quaternion.Euler(0, 0, tilts[i]);
                CaseMenuMeshBuilder backing = c.Batch("Poster Backing " + names[i], paper, mount);
                backing.Box(new Vector3(0, 0, .0024f), new Vector3(width + .002f, height + .002f, .003f), .0005f);
                var surface = new CaseMenuMeshBuilder();
                surface.Quad(new Vector3(-width / 2, -height / 2, 0), new Vector3(width / 2, -height / 2, 0),
                    new Vector3(width / 2, height / 2, 0), new Vector3(-width / 2, height / 2, 0), Vector3.back);
                Mesh posterMesh = surface.Save("Decor_Poster Surface " + names[i]);
                // Generic Quad reverses its four vertices to obtain a negative-Z winding.
                // The resulting order is lower-left, upper-left, upper-right, lower-right.
                posterMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                EditorUtility.SetDirty(posterMesh);
                AssetDatabase.SaveAssetIfDirty(posterMesh);
                Material print = c.Material("Poster Print " + names[i], "F4F1E9", .16f);
                if (print.HasProperty("_BaseMap")) print.SetTexture("_BaseMap", textures[i]);
                if (print.HasProperty("_MainTex")) print.SetTexture("_MainTex", textures[i]);
                if (print.HasProperty("_EmissionMap")) print.SetTexture("_EmissionMap", textures[i]);
                if (print.HasProperty("_EmissionColor"))
                {
                    print.SetColor("_EmissionColor", new Color(.13f, .13f, .13f));
                    print.EnableKeyword("_EMISSION");
                }
                SaveMaterial(print);
                MeshObject(mount, "Poster_" + names[i], posterMesh, print);
                CaseMenuMeshBuilder corners = c.Batch("Poster Tape " + names[i], tape, mount);
                if (i == 1)
                {
                    corners.Box(new Vector3(-.044f, height / 2 - .003f, -.003f), new Vector3(.058f, .024f, .0007f), .0002f, Quaternion.Euler(0, 0, 86));
                    corners.Box(new Vector3(width / 2 - .007f, -height / 2 + .020f, -.003f), new Vector3(.050f, .018f, .0007f), .0002f, Quaternion.Euler(0, 0, 40));
                }
                else
                {
                    corners.Box(new Vector3(-width / 2 + .014f, height / 2 - .012f, -.003f), new Vector3(.070f, .019f, .0007f), .0002f, Quaternion.Euler(0, 0, i == 0 ? 32 : 22));
                    corners.Box(new Vector3(width / 2 - .014f, height / 2 - .013f, -.003f), new Vector3(.068f, .019f, .0007f), .0002f, Quaternion.Euler(0, 0, i == 0 ? -27 : -39));
                }
                CaseMenuMeshBuilder edge = c.Batch("Poster Lower Paper Curl " + names[i], curl, mount);
                edge.Tube(new[] { new Vector3(-width / 2 + .010f, -height / 2, .001f), new Vector3(-width / 3, -height / 2 - .001f, -.001f),
                    new Vector3(width / 3, -height / 2, -.001f), new Vector3(width / 2 - .012f, -height / 2 + .001f, .001f) }, .0015f, 6);
            }
        }

        private static void BuildCoffee(DecorContext c)
        {
            Material ivory = c.Material("Ivory Ceramic", "CFD0BD", .56f);
            Material coffee = c.Material("Coffee Surface", "24150D", .79f);
            Material print = c.Material("Mug Ink", "4D625D", .21f);
            CaseMenuMeshBuilder body = c.Batch("Ceramic Mug And Handle", ivory);
            Vector3 centre = new Vector3(1.32f, .108f, -.040f);
            body.Ring(centre, .071f, .062f, .165f, 48, FaceUp);
            body.Cylinder(new Vector3(centre.x, .029f, centre.z), .071f, .010f, 48, FaceUp);
            body.Ring(new Vector3(centre.x, .192f, centre.z), .072f, .061f, .005f, 48, FaceUp);
            var handlePoints = new List<Vector3>();
            for (int i = 0; i <= 24; i++)
            {
                float angle = Mathf.Lerp(-128f, 128f, i / 24f) * Mathf.Deg2Rad;
                handlePoints.Add(new Vector3(1.417f + Mathf.Cos(angle) * .056f, .110f + Mathf.Sin(angle) * .062f, centre.z));
            }
            body.Tube(handlePoints, .010f, 12);
            CaseMenuMeshBuilder liquid = c.Batch("Still Coffee Surface", coffee);
            liquid.Cylinder(new Vector3(centre.x, .178f, centre.z), .0608f, .003f, 48, FaceUp);
            CaseMenuMeshBuilder mugMark = c.Batch("Mug Printed Underline", print);
            mugMark.Box(new Vector3(centre.x, .066f, -.1118f), new Vector3(.051f, .002f, .0004f), .0001f);
            c.Text("Mug Code Print", "{ code }", new Vector3(centre.x, .106f, -.112f), .088f, new Vector2(.102f, .034f), new Color(.26f, .35f, .31f));
        }

        private static void BuildBookStack(DecorContext c)
        {
            Material pages = c.Material("Book Page Edges", "B9B5A0", .08f);
            Material[] covers = {
                c.Material("Book Rust Cloth", "704438", .16f),
                c.Material("Book Deep Blue Cloth", "344E5A", .15f),
                c.Material("Book Muted Olive Cloth", "59604E", .15f)
            };
            string[] titles = { "OPERATING SYSTEMS", "ALGORITHMS", "C / POINTERS" };
            float[] thickness = { .050f, .047f, .043f };
            float bottom = .003f;
            for (int i = 0; i < 3; i++)
            {
                Transform book = Empty(c.Root, "ProgrammerBook_" + i, new Vector3(1.38f + (i - 1) * .012f, bottom + thickness[i] / 2, .775f));
                book.localRotation = Quaternion.Euler(0, i == 0 ? 2 : i == 1 ? -2.5f : 1, 0);
                CaseMenuMeshBuilder cover = c.Batch("Book Covers " + i, covers[i], book);
                cover.Box(new Vector3(0, -thickness[i] / 2 + .002f, 0), new Vector3(.343f, .004f, .401f), .001f);
                cover.Box(new Vector3(0, thickness[i] / 2 - .002f, 0), new Vector3(.343f, .004f, .401f), .001f);
                cover.Box(new Vector3(0, 0, -.199f), new Vector3(.343f, thickness[i], .008f), .0017f);
                CaseMenuMeshBuilder page = c.Batch("Book Page Block " + i, pages, book);
                page.Box(new Vector3(0, 0, .001f), new Vector3(.330f, thickness[i] - .010f, .384f), .0017f);
                c.Text("Book Spine " + i, titles[i], new Vector3(0, 0, -.204f), i == 0 ? .080f : .091f,
                    new Vector2(.310f, thickness[i] * .75f), new Color(.83f, .81f, .69f), book);
                if (i == 2)
                    c.Text("Original Technical Book Cover", "C\nPOINTERS & MEMORY", new Vector3(0, thickness[i] / 2 + .0008f, -.030f), .110f,
                        new Vector2(.292f, .126f), new Color(.80f, .82f, .72f), book, FaceUp);
                bottom += thickness[i] + .001f;
            }
        }

        private static void BuildStickyNotes(DecorContext c)
        {
            Material yellow = c.Material("Sticky Note Yellow", "C7B36D", .08f);
            Material sage = c.Material("Sticky Note Sage", "96AD8C", .08f);
            Material ink = c.Material("Pencil Grey", "475348", .12f);
            CaseMenuMeshBuilder yellowPaper = c.Batch("Yellow Wall And Desk Notes", yellow);
            CaseMenuMeshBuilder sagePaper = c.Batch("Sage Wall Note", sage);
            yellowPaper.Box(new Vector3(1.215f, .768f, 1.396f), new Vector3(.144f, .130f, .002f), .0005f, Quaternion.Euler(0, 0, -4));
            sagePaper.Box(new Vector3(1.384f, .704f, 1.394f), new Vector3(.139f, .130f, .002f), .0005f, Quaternion.Euler(0, 0, 5));
            c.Text("Wall Sticky TODO", "TODO\nfix bug\ncommit", new Vector3(1.215f, .768f, 1.393f), .132f,
                new Vector2(.119f, .106f), new Color(.24f, .28f, .19f), null, Quaternion.Euler(0, 0, -4));
            c.Text("Wall Sticky Git Status", "git status\n> clean", new Vector3(1.384f, .704f, 1.391f), .119f,
                new Vector2(.119f, .106f), new Color(.24f, .29f, .24f), null, Quaternion.Euler(0, 0, 5));
            yellowPaper.Box(new Vector3(-.071f, .0026f, -.593f), new Vector3(.149f, .002f, .101f), .0005f, Quaternion.Euler(0, -8, 0));
            c.Text("Desk Sticky Debug Note", "TODO\nfix bug / commit", new Vector3(-.071f, .0042f, -.590f), .104f,
                new Vector2(.133f, .083f), new Color(.24f, .28f, .19f), null, Quaternion.Euler(0, -8, 0) * FaceUp);
            CaseMenuMeshBuilder ticks = c.Batch("Sticky Note Pencil Ticks", ink);
            ticks.Box(new Vector3(1.175f, .721f, 1.392f), new Vector3(.006f, .002f, .0004f), .0001f, Quaternion.Euler(0, 0, 35));
        }

        private static void BuildOpenNotebook(DecorContext c)
        {
            Transform notebook = Empty(c.Root, "Open Programmer Notebook", new Vector3(-1.35f, .008f, -.080f));
            notebook.localRotation = Quaternion.Euler(0, -6, 0);
            Material cover = c.Material("Notebook Kraft Cover", "7D684D", .17f);
            Material paper = c.Material("Notebook Open Pages", "D0C9AD", .07f);
            Material graphite = c.Material("Notebook Graphite Lines", "637064", .12f);
            Material pencilWood = c.Material("Pencil Painted Cedar", "BD8A4A", .28f);
            Material pencilLead = c.Material("Pencil Graphite", "292D2A", .30f);
            CaseMenuMeshBuilder backing = c.Batch("Open Notebook Kraft Cover", cover, notebook);
            backing.Box(Vector3.zero, new Vector3(.376f, .006f, .312f), .002f);
            CaseMenuMeshBuilder sheets = c.Batch("Open Notebook Facing Pages", paper, notebook);
            sheets.Box(new Vector3(-.093f, .006f, 0), new Vector3(.180f, .005f, .297f), .001f);
            sheets.Box(new Vector3(.093f, .006f, 0), new Vector3(.180f, .005f, .297f), .001f);
            CaseMenuMeshBuilder marks = c.Batch("Notebook Ruling And Flowchart", graphite, notebook);
            marks.Box(new Vector3(0, .010f, 0), new Vector3(.002f, .0004f, .290f), .0001f);
            for (int line = 0; line < 10; line++)
                marks.Box(new Vector3(-.093f, .0092f, -.116f + line * .023f), new Vector3(.146f, .0003f, .00055f), .00008f);
            // A hand-sized three-node flow chart, drawn geometrically rather than generated text textures.
            for (int node = 0; node < 3; node++)
            {
                float z = -.084f + node * .082f;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    marks.Box(new Vector3(.093f + sign * .051f, .0092f, z), new Vector3(.0008f, .0003f, .042f), .00008f);
                    marks.Box(new Vector3(.093f, .0092f, z + sign * .021f), new Vector3(.102f, .0003f, .0008f), .00008f);
                }
                if (node < 2)
                {
                    marks.Box(new Vector3(.093f, .0092f, z + .041f), new Vector3(.0008f, .0003f, .040f), .00008f);
                    marks.Box(new Vector3(.089f, .0092f, z + .058f), new Vector3(.011f, .0003f, .0008f), .00008f, Quaternion.Euler(0, -42, 0));
                    marks.Box(new Vector3(.097f, .0092f, z + .058f), new Vector3(.011f, .0003f, .0008f), .00008f, Quaternion.Euler(0, 42, 0));
                }
            }
            Color noteInk = new Color(.29f, .35f, .31f);
            c.Text("Notebook Code Sketch", "while (working)\n{\n  test();\n  learn();\n  commit();\n}", new Vector3(-.093f, .0102f, -.012f), .078f,
                new Vector2(.154f, .217f), noteInk, notebook, FaceUp);
            string[] flow = { "READ", "TEST", "COMMIT" };
            for (int node = 0; node < 3; node++)
                c.Text("Notebook Flow " + node, flow[node], new Vector3(.093f, .0102f, -.084f + node * .082f), .064f,
                    new Vector2(.093f, .030f), noteInk, notebook, FaceUp);
            CaseMenuMeshBuilder pencil = c.Batch("Notebook Cedar Pencil", pencilWood, notebook);
            Quaternion diagonal = Quaternion.Euler(0, -13, 0);
            pencil.Cylinder(new Vector3(.175f, .017f, -.010f), .0048f, .222f, 6, diagonal);
            CaseMenuMeshBuilder tip = c.Batch("Notebook Pencil Point", pencilLead, notebook);
            tip.Frustum(new Vector3(.203f, .017f, -.124f), .001f, .0048f, .017f, 6, diagonal);
        }

        private sealed class DecorContext
        {
            internal readonly Transform Root;
            private readonly Shader shader;
            private readonly TMP_FontAsset font;
            private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
            private readonly List<(string name, Transform parent, CaseMenuMeshBuilder geometry, Material material)> batches =
                new List<(string, Transform, CaseMenuMeshBuilder, Material)>();

            internal DecorContext(Transform root, Shader shader, TMP_FontAsset font) { Root = root; this.shader = shader; this.font = font; }
            internal CaseMenuMeshBuilder Batch(string name, Material material, Transform parent = null)
            {
                var geometry = new CaseMenuMeshBuilder();
                batches.Add((name, parent == null ? Root : parent, geometry, material));
                return geometry;
            }
            internal Material Material(string name, string hex, float smoothness)
            {
                name = "Decor_" + name;
                if (materials.TryGetValue(name, out Material cached)) return cached;
                string path = CaseMenuModelBuilder.ArtRoot + "/Materials/" + name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
                material.shader = shader;
                ColorUtility.TryParseHtmlString("#" + hex, out Color color);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
                material.enableInstancing = true;
                SaveMaterial(material);
                materials.Add(name, material);
                return material;
            }
            internal void SaveBatches()
            {
                foreach (var batch in batches) MeshObject(batch.parent, batch.name, batch.geometry.Save("Decor_" + batch.name), batch.material);
            }
            internal void Text(string name, string content, Vector3 position, float fontSize, Vector2 size, Color color, Transform parent = null, Quaternion rotation = default)
            {
                var go = new GameObject(name, typeof(TextMeshPro));
                go.transform.SetParent(parent == null ? Root : parent, false);
                go.transform.localPosition = position;
                go.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
                TextMeshPro text = go.GetComponent<TextMeshPro>();
                text.font = font; text.text = content; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
                text.color = color; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow;
                text.rectTransform.sizeDelta = size; text.raycastTarget = false;
                MeshRenderer renderer = text.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }

        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            Transform node = Empty(parent, name, Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = node.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            node.gameObject.isStatic = true;
            return node.gameObject;
        }
        private static Transform Empty(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = Vector3.one;
            return go.transform;
        }
        private static void SaveMaterial(Material material)
        {
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
