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
    /// Original period-neutral 2016 office tower and desk. No monitor, cameras, input or scene ownership.
    /// Coordinates are metres; the table surface is y=0 and the tower front faces negative Z.
    /// Only CaseMenuSet under the supplied parent (or the active scene root) is replaced on rebuild.
    /// </summary>
    public static class CaseMenuModelBuilder
    {
        public const string RootName = "CaseMenuSet";
        public const string ArtRoot = "Assets/LingGuangV05/CaseMenu/Art";
        private const string FontPath = "Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset";
        private static readonly Quaternion FaceUp = Quaternion.Euler(90, 0, 0);
        private static readonly Color Letter = new Color(.68f, .72f, .71f, 1);

        public static GameObject Build(Transform parent)
        {
            EnsureFolder(ArtRoot + "/Meshes");
            EnsureFolder(ArtRoot + "/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Case menu requires a Lit shader.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) ?? TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("Case menu requires the installed Desktop CJK or TMP default font.");

            // Narrow replacement scope: never touch a similarly named object beneath another parent.
            var oldRoots = new List<GameObject>();
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                    if (parent.GetChild(i).name == RootName) oldRoots.Add(parent.GetChild(i).gameObject);
            }
            else
            {
                foreach (GameObject candidate in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (candidate.name == RootName) oldRoots.Add(candidate);
            }
            foreach (GameObject old in oldRoots) Undo.DestroyObjectImmediate(old);
            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(root, "Build physical case menu set");

            var set = new ModelSet(root.transform, font, shader);
            BuildTower(set);
            BuildDesk(set);
            BuildKeyboardAndMouse(set);
            BuildLampAndStationery(set);
            BuildCables(set);
            set.SaveBatches();
            EditorUtility.SetDirty(root);
            return root;
        }

        private static void BuildTower(ModelSet s)
        {
            var steel = s.Batch("Case sheet metal and folded edges", "Powder Coated Charcoal", "242A2C", .29f, .45f);
            var panel = s.Batch("Removable side and front fascia", "Graphite Side Panels", "1A1F22", .27f, .32f);
            var seam = s.Batch("Panel seams and recessed wells", "Deep Recess Rubber", "070B0E", .18f);
            var satin = s.Batch("Brushed rims and fasteners", "Satin Gunmetal", "6D777B", .46f, .82f);
            var slots = s.Batch("Fastener slots", "Black Fastener Engraving", "080B0C", .16f, .10f);
            var grill = s.Batch("Front intake horizontal slats", "Intake Slat Edges", "2B3336", .37f, .55f);
            var mesh = s.Batch("Inset expanded intake mesh", "Intake Woven Metal", "182124", .27f, .62f);
            var usb = s.Batch("USB 3 blue contact inserts", "USB Blue Insulator", "164B7C", .27f);
            var contacts = s.Batch("USB contact pins", "Nickel Contacts", "A99F7E", .62f, .88f);
            var rubber = s.Batch("Four rubber isolation feet", "Rubber Feet", "101518", .12f);

            steel.Box(new Vector3(-.78f, .555f, .17f), new Vector3(.48f, 1.02f, .90f), .012f);
            // Side panels sit outside the folded chassis, with a fine black perimeter gap.
            seam.Box(new Vector3(-1.022f, .552f, .184f), new Vector3(.009f, .975f, .852f), .003f);
            panel.Box(new Vector3(-1.028f, .552f, .184f), new Vector3(.009f, .962f, .835f), .003f);
            seam.Box(new Vector3(-.538f, .552f, .184f), new Vector3(.009f, .975f, .852f), .003f);
            panel.Box(new Vector3(-.532f, .552f, .184f), new Vector3(.009f, .962f, .835f), .003f);
            panel.Box(new Vector3(-.78f, .555f, -.281f), new Vector3(.454f, .992f, .045f), .012f);
            seam.Box(new Vector3(-.993f, .549f, -.307f), new Vector3(.004f, .925f, .005f), .001f);
            seam.Box(new Vector3(-.567f, .549f, -.307f), new Vector3(.004f, .925f, .005f), .001f);
            steel.Box(new Vector3(-.78f, 1.064f, .174f), new Vector3(.434f, .006f, .832f), .002f);

            // Original unbranded optical drive: tray seam, eject switch and tiny access LED.
            seam.Box(new Vector3(-.78f, .990f, -.307f), new Vector3(.366f, .058f, .009f), .002f);
            panel.Box(new Vector3(-.78f, .991f, -.314f), new Vector3(.348f, .044f, .010f), .002f);
            slots.Box(new Vector3(-.78f, 1.008f, -.321f), new Vector3(.318f, .0025f, .002f), .0006f);
            satin.Box(new Vector3(-.635f, .984f, -.321f), new Vector3(.026f, .007f, .003f), .001f);
            s.Text("Optical drive DVD legend", "DVD  /  MULTI", new Vector3(-.863f, .991f, -.321f), .075f, new Vector2(.115f, .016f), new Color(.40f, .45f, .46f));

            BuildButton(s, true, new Vector3(-.78f, .89f, -.31f), .048f);
            BuildButton(s, false, new Vector3(-.78f, .74f, -.31f), .033f);
            s.Text("Start engraved Chinese legend", "开始游戏", new Vector3(-.652f, .893f, -.31f), .255f, new Vector2(.139f, .040f), Letter);
            s.Text("Start engraved English legend", "P O W E R", new Vector3(-.652f, .865f, -.31f), .085f, new Vector2(.120f, .016f), new Color(.43f, .51f, .52f));
            s.Text("Settings engraved Chinese legend", "设置", new Vector3(-.653f, .746f, -.31f), .245f, new Vector2(.132f, .035f), Letter);
            s.Text("Settings engraved English legend", "S E T U P", new Vector3(-.651f, .722f, -.31f), .080f, new Vector2(.115f, .015f), new Color(.43f, .49f, .50f));
            // Fine front-panel crease separates controls from the I/O bank.
            slots.Box(new Vector3(-.78f, .669f, -.309f), new Vector3(.349f, .0016f, .003f), .0005f);

            for (int i = 0; i < 2; i++)
            {
                float x = -.905f + i * .067f;
                seam.Box(new Vector3(x, .621f, -.311f), new Vector3(.043f, .020f, .011f), .002f);
                satin.Box(new Vector3(x, .621f, -.315f), new Vector3(.039f, .016f, .009f), .0014f);
                slots.Box(new Vector3(x, .621f, -.322f), new Vector3(.033f, .011f, .008f), .001f);
                usb.Box(new Vector3(x, .6185f, -.327f), new Vector3(.029f, .0043f, .005f), .0007f);
                for (int p = 0; p < 4; p++) contacts.Box(new Vector3(x - .010f + .0065f * p, .619f, -.330f), new Vector3(.0021f, .0012f, .002f), .0002f);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = -.712f + i * .057f;
                satin.Ring(new Vector3(x, .621f, -.319f), .0105f, .0064f, .008f, 24);
                slots.Cylinder(new Vector3(x, .621f, -.317f), .0062f, .012f, 24);
            }
            s.Text("Front USB and audio legends", "SS  USB 3.0                AUDIO", new Vector3(-.784f, .592f, -.311f), .079f, new Vector2(.34f, .020f), new Color(.40f, .48f, .49f));
            seam.Box(new Vector3(-.78f, .331f, -.309f), new Vector3(.366f, .413f, .023f), .012f);
            // Staggered rings supply actual perforation depth beneath the slats; one combined renderer.
            for (int row = 0; row < 19; row++)
            for (int col = 0; col < 15; col++)
            {
                float x = -.936f + col * .0215f + (row % 2) * .01075f;
                if (x > -.624f) continue;
                mesh.Ring(new Vector3(x, .143f + row * .020f, -.324f), .010f, .0073f, .0012f, 6);
            }
            for (int row = 0; row < 18; row++)
                grill.Box(new Vector3(-.78f, .155f + row * .0208f, -.333f), new Vector3(.346f, .0058f, .012f), .002f);
            satin.Box(new Vector3(-.78f, .096f, -.309f), new Vector3(.104f, .020f, .004f), .001f);
            s.Text("Original understated case badge", "L I N G G U A N G", new Vector3(-.78f, .096f, -.312f), .066f, new Vector2(.106f, .013f), new Color(.12f, .16f, .18f));
            s.Text("Case serial inscription", "LG / 16     ·     WORKSTATION", new Vector3(-.78f, .063f, -.307f), .064f, new Vector2(.29f, .014f), new Color(.30f, .37f, .39f));

            // Side intake: pressed louvres rather than luminous sci-fi decoration.
            for (int row = 0; row < 16; row++)
                seam.Box(new Vector3(-1.034f, .236f + row * .016f, .321f), new Vector3(.004f, .004f, .244f), .001f);
            for (int end = 0; end < 2; end++)
            for (int high = 0; high < 2; high++)
            {
                float y = .102f + high * .897f, z = -.184f + end * .724f;
                Quaternion side = Quaternion.Euler(0, 90, 0);
                satin.Cylinder(new Vector3(-1.036f, y, z), .010f, .003f, 20, side);
                slots.Box(new Vector3(-1.038f, y, z), new Vector3(.001f, .0021f, .012f), .0003f);
            }
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                rubber.Box(new Vector3(-.78f + sx * .174f, .035f, .17f + sz * .340f), new Vector3(.078f, .040f, .098f), .010f);
            // Rear I/O and PSU remain recognizable from camera movement and cast plausible cable shadows.
            seam.Box(new Vector3(-.885f, .695f, .625f), new Vector3(.155f, .388f, .010f), .005f);
            seam.Box(new Vector3(-.78f, .154f, .626f), new Vector3(.346f, .135f, .012f), .005f);
            satin.Ring(new Vector3(-.695f, .839f, .631f), .084f, .077f, .006f, 40);
            for (int i = -3; i <= 3; i++) slots.Box(new Vector3(-.695f, .839f + i * .018f, .633f), new Vector3(.135f, .003f, .007f), .001f);
        }

        private static void BuildButton(ModelSet s, bool start, Vector3 position, float radius)
        {
            string prefix = start ? "Start" : "Settings";
            Transform button = Empty(s.Root, prefix + "Button", position);
            var well = new CaseMenuMeshBuilder(); well.Cylinder(new Vector3(0, 0, .008f), radius + .011f, .016f, 48);
            MeshObject(button, prefix + "ButtonWell", well.Save(prefix + " Button Well"), s.Material("Button Well", "070C10", .26f));
            var bezel = new CaseMenuMeshBuilder(); bezel.Ring(new Vector3(0, 0, .001f), radius + .009f, radius + .004f, .005f, 56);
            MeshObject(button, prefix + "BrushedBezel", bezel.Save(prefix + " Brushed Bezel"), s.Material("Button Brushed Metal", "5E6D74", .57f, .84f));
            var ring = new CaseMenuMeshBuilder(); ring.Ring(new Vector3(0, 0, -.004f), radius + .0037f, radius + .0010f, .003f, 64);
            Material glow = s.Material(prefix + " Ring Emission", start ? "4EA7CA" : "AEBBAE", .38f, .25f,
                start ? new Color(.065f, .32f, .52f) : new Color(.16f, .20f, .17f));
            MeshObject(button, prefix + "Ring", ring.Save(prefix + " Light Ring"), glow);
            var cap = new CaseMenuMeshBuilder();
            cap.Cylinder(new Vector3(0, 0, -.006f), radius, .013f, 64);
            cap.Cylinder(new Vector3(0, 0, -.013f), radius - .0018f, .004f, 64);
            GameObject key = MeshObject(button, prefix + "Keycap", cap.Save(prefix + " Moving Keycap"),
                s.Material(start ? "Start Key Brushed Face" : "Settings Key Brushed Face", start ? "35454F" : "353D42", .49f, .62f));
            if (start)
            {
                Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/HongmengOS/Aero2010/Models/Meshes/Power Symbol LED Lens.asset");
                if (source != null)
                {
                    GameObject glyph = MeshObject(key.transform, "StartPowerGlyph", source, glow);
                    glyph.transform.localPosition = new Vector3(0, -.003f, -.0172f);
                    glyph.transform.localScale = Vector3.one * .84f;
                }
                else s.Text("Start power glyph fallback", "I", new Vector3(0, 0, -.018f), .23f, new Vector2(.04f, .04f), Letter, key.transform);
            }
            else
            {
                var glyph = new CaseMenuMeshBuilder();
                glyph.Ring(new Vector3(0, 0, -.0172f), .011f, .0067f, .0007f, 24);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    glyph.Box(new Vector3(Mathf.Cos(a) * .012f, Mathf.Sin(a) * .012f, -.0173f), new Vector3(.0052f, .0041f, .001f), .0003f, Quaternion.Euler(0, 0, i * 45));
                }
                MeshObject(key.transform, "SettingsGearGlyph", glyph.Save("Settings Gear Engraving"), s.Material("Settings Engraving", "899A9F", .33f, .35f));
            }
        }

        private static void BuildDesk(ModelSet s)
        {
            var oak = s.Batch("Solid walnut desktop", "Oiled Dark Walnut", "4A3428", .34f);
            var edge = s.Batch("Desk apron and edge joints", "Walnut Edge Grain", "30231D", .26f);
            var grain = s.Batch("Subtle walnut open grain", "Walnut Dark Pores", "39291F", .23f);
            var legs = s.Batch("Desk square steel legs", "Black Desk Steel", "171C1E", .25f, .5f);
            var wall = s.Batch("Warm plaster background", "Warm Grey Plaster", "514D47", .05f);
            var floor = s.Batch("Warm office floor", "Office Floor", "24201C", .20f);
            var trim = s.Batch("Background skirting and panel joints", "Wall Trim", "393A37", .13f);
            oak.Box(new Vector3(0, -.043f, .20f), new Vector3(3.6f, .086f, 1.85f), .018f);
            edge.Box(new Vector3(0, -.092f, .20f), new Vector3(3.52f, .024f, 1.78f), .007f);
            // Deterministic, restrained wood pores avoid a procedural zebra-stripe surface.
            for (int i = 0; i < 25; i++)
            {
                float z = -.674f + i * .069f;
                float x = Mathf.Sin(i * 4.31f) * .55f;
                float length = .28f + (.5f + .5f * Mathf.Sin(i * 2.7f)) * .63f;
                grain.Box(new Vector3(x, .0005f, z), new Vector3(length, .0007f, .0012f), .0002f);
            }
            for (int i = -1; i <= 1; i += 2)
            {
                for (int end = -1; end <= 1; end += 2)
                    legs.Box(new Vector3(i * 1.56f, -.444f, .20f + end * .68f), new Vector3(.065f, .70f, .065f), .006f);
                legs.Box(new Vector3(i * 1.56f, -.744f, .20f), new Vector3(.065f, .047f, 1.42f), .006f);
                legs.Box(new Vector3(i * 1.56f, -.129f, .20f), new Vector3(.065f, .046f, 1.42f), .006f);
            }
            wall.Box(new Vector3(0, .70f, 1.49f), new Vector3(7.2f, 3.20f, .15f), .01f);
            floor.Box(new Vector3(0, -.825f, .1f), new Vector3(7.2f, .06f, 6.0f), .004f);
            trim.Box(new Vector3(0, -.723f, 1.398f), new Vector3(7.2f, .126f, .037f), .004f);
            trim.Box(new Vector3(-1.95f, .70f, 1.409f), new Vector3(.005f, 2.92f, .008f), .001f);
            trim.Box(new Vector3(2.12f, .70f, 1.409f), new Vector3(.005f, 2.92f, .008f), .001f);
        }

        private static void BuildKeyboardAndMouse(ModelSet s)
        {
            var shell = s.Batch("Keyboard low profile housing", "Keyboard Graphite", "20292D", .28f);
            var key = s.Batch("Keyboard combined keycaps", "Keycap ABS", "343E43", .33f);
            var modifier = s.Batch("Keyboard modifier caps", "Modifier Keycap ABS", "283035", .27f);
            var keyGaps = s.Batch("Keyboard recessed deck and mouse seam", "Keyboard Gaps", "070D10", .12f);
            var pad = s.Batch("Stitched charcoal mouse pad", "Mousepad Fabric", "1B2427", .05f);
            var stitch = s.Batch("Mouse pad edge stitching", "Mousepad Seam", "334044", .11f);
            var mouse = s.Batch("Sculpted wired mouse shell", "Mouse Soft Touch ABS", "263136", .38f);
            var wheel = s.Batch("Mouse wheel and details", "Wheel Rubber", "0B1013", .09f);
            Vector3 kb = new Vector3(.34f, .035f, -.367f);
            shell.Box(kb, new Vector3(.94f, .046f, .295f), .015f);
            keyGaps.Box(kb + new Vector3(0, .023f, 0), new Vector3(.897f, .008f, .251f), .003f);
            for (int row = 0; row < 5; row++)
            for (int column = 0; column < 15; column++)
            {
                if (row == 0 && column > 2 && column < 10) continue;
                float x = kb.x - .431f + column * .044f, z = kb.z - .103f + row * .047f;
                (column == 0 || column == 14 || row == 0 ? modifier : key).Box(new Vector3(x, .069f + row * .0011f, z), new Vector3(.037f, .022f, .037f), .0032f);
            }
            modifier.Box(new Vector3(kb.x - .142f, .070f, kb.z - .103f), new Vector3(.299f, .020f, .037f), .004f);
            for (int row = 0; row < 5; row++)
            for (int col = 0; col < 4; col++)
                key.Box(new Vector3(kb.x + .289f + col * .043f, .069f + row * .0011f, kb.z - .103f + row * .047f), new Vector3(.036f, .022f, .037f), .003f);
            // A few readable markings instead of 90 extra text renderers.
            s.Text("Keyboard top legends", "ESC    1    2    3    4    5    6    7    8    9    0    -    =", new Vector3(.191f, .087f, -.281f), .063f, new Vector2(.635f, .018f), new Color(.67f, .71f, .72f), null, FaceUp);
            s.Text("Keyboard QWERTY legends", "Q    W    E    R    T    Y    U    I    O    P", new Vector3(.18f, .085f, -.319f), .073f, new Vector2(.51f, .019f), new Color(.62f, .68f, .70f), null, FaceUp);
            s.Text("Keyboard middle legends", "A    S    D    F    G    H    J    K    L", new Vector3(.19f, .085f, -.364f), .073f, new Vector2(.49f, .019f), new Color(.62f, .68f, .70f), null, FaceUp);
            s.Text("Keyboard lower legends", "Z    X    C    V    B    N    M", new Vector3(.19f, .083f, -.411f), .073f, new Vector2(.40f, .019f), new Color(.62f, .68f, .70f), null, FaceUp);
            pad.Box(new Vector3(1.036f, .006f, -.329f), new Vector3(.427f, .012f, .372f), .005f);
            for (int i = -1; i <= 1; i += 2)
            {
                stitch.Box(new Vector3(1.036f + i * .205f, .0123f, -.329f), new Vector3(.001f, .001f, .347f), .0002f);
                stitch.Box(new Vector3(1.036f, .0123f, -.329f + i * .176f), new Vector3(.406f, .001f, .001f), .0002f);
            }
            mouse.Ellipsoid(new Vector3(1.034f, .044f, -.310f), new Vector3(.067f, .034f, .098f));
            keyGaps.Box(new Vector3(1.034f, .076f, -.278f), new Vector3(.0021f, .004f, .075f), .0007f);
            wheel.Cylinder(new Vector3(1.034f, .078f, -.266f), .013f, .012f, 24, Quaternion.Euler(0, 90, 0));
            for (int i = 0; i < 6; i++) wheel.Box(new Vector3(1.034f, .087f, -.278f + i * .004f), new Vector3(.013f, .0015f, .0018f), .0003f);
        }

        private static void BuildLampAndStationery(ModelSet s)
        {
            var metal = s.Batch("Desk lamp enamel shade base and arm", "Lamp Enamel", "33423E", .43f, .48f);
            var brass = s.Batch("Desk lamp brass joints", "Aged Brass", "8E7751", .53f, .81f);
            var interior = s.Batch("Lamp cream interior", "Lamp Interior", "C6BFA6", .28f);
            var lampEmission = s.Batch("Lamp warm diffuser", "Lamp Warm Emission", "E8D1A1", .30f, 0, new Color(.65f, .43f, .21f));
            var paper = s.Batch("Notebook cream page block", "Ivory Paper", "C6B99D", .06f);
            var notebook = s.Batch("Closed cloth notebook", "Notebook Linen", "455753", .13f);
            var elastic = s.Batch("Notebook elastic and pen", "Stationery Black", "1D2423", .17f);
            metal.Cylinder(new Vector3(-1.46f, .021f, .631f), .153f, .041f, 48, FaceUp);
            brass.Cylinder(new Vector3(-1.46f, .050f, .631f), .025f, .032f, 32, FaceUp);
            metal.Tube(new[] { new Vector3(-1.46f, .055f, .63f), new Vector3(-1.46f, .45f, .63f), new Vector3(-1.46f, .57f, .54f), new Vector3(-1.46f, .83f, .39f) }, .011f, 12);
            brass.Cylinder(new Vector3(-1.46f, .45f, .63f), .027f, .040f, 32, Quaternion.Euler(0, 90, 0));
            metal.Frustum(new Vector3(-1.46f, .858f, .356f), .068f, .153f, .153f, 48, FaceUp);
            interior.Frustum(new Vector3(-1.46f, .855f, .356f), .063f, .147f, .145f, 48, FaceUp);
            brass.Ring(new Vector3(-1.46f, .780f, .356f), .153f, .145f, .004f, 48, FaceUp);
            lampEmission.Cylinder(new Vector3(-1.46f, .791f, .356f), .134f, .004f, 40, FaceUp);
            Empty(s.Root, "LampWarmLightMount", new Vector3(-1.46f, .771f, .356f));
            // Keep the right-hand surface calm; stationary does not intersect the pointer path.
            Vector3 book = new Vector3(1.405f, .025f, .289f);
            notebook.Box(book, new Vector3(.325f, .041f, .442f), .007f);
            paper.Box(book + new Vector3(.002f, -.002f, -.001f), new Vector3(.307f, .027f, .422f), .003f);
            notebook.Box(book + new Vector3(0, .019f, 0), new Vector3(.325f, .005f, .442f), .002f);
            elastic.Box(book + new Vector3(.111f, .023f, 0), new Vector3(.008f, .002f, .444f), .0005f);
            s.Text("Notebook embossed cover", "FIELD NOTES\n2016", book + new Vector3(-.022f, .025f, .040f), .108f, new Vector2(.25f, .06f), new Color(.60f, .64f, .56f), null, FaceUp);
            elastic.Cylinder(new Vector3(1.631f, .015f, .243f), .007f, .252f, 12);
            brass.Cylinder(new Vector3(1.631f, .015f, .102f), .004f, .029f, 12);
        }

        private static void BuildCables(ModelSet s)
        {
            var cable = s.Batch("Bent rear power keyboard mouse cables", "Cable PVC", "0E1519", .24f);
            var plugs = s.Batch("Cable moulded plugs", "Connector ABS", "182126", .29f);
            Cable(new[] { new Vector3(-.79f, .148f, .657f), new Vector3(-.78f, .144f, .77f), new Vector3(-.65f, .036f, .91f), new Vector3(-.45f, .012f, 1.13f), new Vector3(-.40f, -.30f, 1.21f) }, .009f);
            Cable(new[] { new Vector3(-.884f, .730f, .662f), new Vector3(-.87f, .69f, .83f), new Vector3(-.50f, .060f, .89f), new Vector3(-.10f, .012f, .71f), new Vector3(.46f, .25f, .644f) }, .006f);
            Cable(new[] { new Vector3(.41f, .042f, -.216f), new Vector3(.42f, .014f, -.15f), new Vector3(.25f, .014f, -.07f), new Vector3(-.21f, .014f, .20f), new Vector3(-.47f, .014f, .78f), new Vector3(-.873f, .601f, .655f) }, .0038f);
            Cable(new[] { new Vector3(1.034f, .039f, -.211f), new Vector3(1.04f, .016f, -.12f), new Vector3(.95f, .013f, .08f), new Vector3(.80f, .013f, .29f), new Vector3(.64f, .014f, .69f), new Vector3(-.20f, .014f, .87f), new Vector3(-.875f, .556f, .655f) }, .003f);
            plugs.Box(new Vector3(-.79f, .148f, .661f), new Vector3(.043f, .047f, .068f), .006f);
            plugs.Box(new Vector3(-.884f, .730f, .652f), new Vector3(.065f, .031f, .050f), .005f);
            void Cable(Vector3[] knots, float radius)
            {
                var points = new List<Vector3>();
                for (int i = 0; i < knots.Length - 1; i++)
                {
                    Vector3 a = knots[Mathf.Max(i - 1, 0)], b = knots[i], c = knots[i + 1], d = knots[Mathf.Min(i + 2, knots.Length - 1)];
                    for (int step = 0; step < 8; step++)
                    {
                        float t = step / 8f;
                        Vector3 point = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
                        // Catmull-Rom may undershoot between a raised plug and a table-level knot.
                        if (point.z < 1.10f && point.z > -.72f && Mathf.Abs(point.x) < 1.79f) point.y = Mathf.Max(radius + .001f, point.y);
                        points.Add(point);
                    }
                }
                points.Add(knots[knots.Length - 1]);
                cable.Tube(points, radius);
            }
        }

        private sealed class ModelSet
        {
            internal readonly Transform Root;
            private readonly TMP_FontAsset font;
            private readonly Shader shader;
            private readonly List<(string name, CaseMenuMeshBuilder geometry, Material material)> batches = new List<(string, CaseMenuMeshBuilder, Material)>();
            private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
            internal ModelSet(Transform root, TMP_FontAsset font, Shader shader) { Root = root; this.font = font; this.shader = shader; }
            internal CaseMenuMeshBuilder Batch(string name, string materialName, string hex, float smoothness, float metallic = 0, Color emission = default)
            {
                var geometry = new CaseMenuMeshBuilder();
                batches.Add((name, geometry, Material(materialName, hex, smoothness, metallic, emission)));
                return geometry;
            }
            internal Material Material(string name, string hex, float smoothness, float metallic = 0, Color emission = default)
            {
                if (materials.TryGetValue(name, out Material cached)) return cached;
                string path = ArtRoot + "/Materials/" + name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
                material.shader = shader;
                ColorUtility.TryParseHtmlString("#" + hex, out Color color);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emission);
                    if (emission.maxColorComponent > 0) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
                }
                material.enableInstancing = true;
                EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
                materials.Add(name, material);
                return material;
            }
            internal void SaveBatches()
            {
                foreach (var batch in batches) MeshObject(Root, batch.name, batch.geometry.Save(batch.name), batch.material);
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

        private static Transform Empty(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = Vector3.one;
            return go.transform;
        }
        private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            Transform node = Empty(parent, name, Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = node.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            return node.gameObject;
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
