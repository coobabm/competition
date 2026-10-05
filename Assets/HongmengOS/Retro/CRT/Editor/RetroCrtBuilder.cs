using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HongmengOS.Retro.Editor
{
    /// <summary>Builds only presentation siblings around an existing, interactive desktop viewport.</summary>
    public static class RetroCrtBuilder
    {
        private const string RootPath = "Assets/HongmengOS/Retro/CRT";
        private static readonly Color Ink = Hex("48473E");
        private static readonly Color Cream = Hex("D5CBB1");
        private static readonly Color Highlight = Hex("F8F1DC");
        private static readonly Color Shadow = Hex("746D59");

        public static void Build(RectTransform monitor, RectTransform viewport, TMP_FontAsset font)
        {
            if (monitor == null || viewport == null)
                throw new ArgumentNullException(monitor == null ? nameof(monitor) : nameof(viewport));
            if (font == null) font = TMP_Settings.defaultFontAsset;
            Material glassMaterial = GetGlassMaterial();
            RemoveGenerated(monitor, "Retro CRT Casing");
            RemoveGenerated(monitor, "Retro CRT Glass");

            RectTransform shell = Empty(monitor, "Retro CRT Casing", Vector2.zero, Vector2.one);
            shell.SetAsFirstSibling();
            Undo.RegisterCreatedObjectUndo(shell.gameObject, "Build CRT monitor casing");
            var group = shell.gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            // Full-bleed back housing: the chassis reaches all four outer screen edges.
            Face(shell, "Aged injection-moulded housing", 0, 0, 1, 1, 0, 12,
                Hex("C7BEA5"), Hex("A3987E"), Highlight, Hex("565448"));
            Face(shell, "Front fascia / broad compound bevel", .008f, .012f, .992f, .987f, 43, 17,
                Hex("E4DBC2"), Hex("BFB49A"), Hex("FFF7E1"), Hex("82775E"));
            Face(shell, "Fascia parting seam / upper", .021f, .966f, .979f, .968f, 1, 0, Shadow, Shadow, Shadow, Shadow);
            Face(shell, "Fascia seam / right highlight", .984f, .055f, .986f, .950f, 1, 0, Highlight, Highlight, Highlight, Highlight);
            Face(shell, "Recessed display pocket", .030f, .128f, .833f, .949f, 64, 15,
                Hex("8A826D"), Hex("554F43"), Hex("9B917B"), Hex("F1E6CB"));
            Face(shell, "Moulded dark graphite CRT bezel", .039f, .145f, .824f, .937f, 55, 13,
                Hex("333C38"), Hex("111B18"), Hex("62685B"), Hex("080D0B"));
            Face(shell, "Inner glass gasket", .048f, .162f, .817f, .927f, 48, 5,
                Hex("070F0C"), Hex("16211B"), Hex("0B120E"), Hex("6A7466"));
            Face(shell, "Phosphor glass bed", .053f, .171f, .813f, .920f, 44, 0,
                Hex("17372F"), Hex("071C17"), Color.black, Color.black);

            // Fine tooling marks and edge wear make the casing read as hardware, not a flat picture frame.
            Face(shell, "Bezel crown reflection", .075f, .930f, .785f, .932f, 1, 0,
                new Color(.8f,.85f,.73f,.23f), new Color(.8f,.85f,.73f,.23f), Color.white, Color.black);
            Face(shell, "Recess lower light-catching lip", .073f, .149f, .788f, .152f, 1, 0,
                Hex("626959"), Hex("626959"), Color.white, Color.black);
            Face(shell, "Right panel mould seam", .838f, .062f, .840f, .942f, 1, 0,
                Hex("A99E82"), Hex("A99E82"), Color.white, Color.black);
            Face(shell, "Right panel seam light", .840f, .062f, .841f, .942f, 1, 0,
                Highlight, Highlight, Highlight, Highlight);

            Label(shell, "Top manufacturer print", "HONGMENG  ·  PERSONAL COMPUTER SYSTEMS", .063f, .950f, .68f, .974f, 13, Ink, font, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Label(shell, "Model number print", "HM–98", .866f, .917f, .971f, .952f, 31, Ink, font, TextAlignmentOptions.Center, FontStyles.Bold);
            Label(shell, "Model type print", "COLOUR DISPLAY TERMINAL", .850f, .891f, .983f, .918f, 11, Ink, font);
            Rule(shell, "Control label rule", .859f, .883f, .975f, .885f, Ink);
            Label(shell, "Series label", "PROFESSIONAL  SERIES", .850f, .846f, .983f, .876f, 13, Ink, font, TextAlignmentOptions.Center, FontStyles.Bold);

            // Controller wiring is owned by the desktop rig; these stable node names expose hardware controls.
            Face(shell, "Control bank inset", .855f, .320f, .980f, .821f, 11, 3,
                Hex("C8BEA3"), Hex("C1B699"), Hex("A89F86"), Hex("F2E8CF"));
            Label(shell, "Control bank title", "PICTURE ADJUST", .860f, .780f, .975f, .808f, 12, Ink, font, TextAlignmentOptions.Center, FontStyles.Bold);
            Knob(shell, "FOCUS", .918f, .676f, font, -26f, "SOFT", "SHARP");
            Knob(shell, "BRIGHTNESS", .918f, .464f, font, 22f, "MIN", "MAX");
            Label(shell, "Calibrated print", "FACTORY CALIBRATED  /  1998", .850f, .294f, .983f, .318f, 10, Ink, font);

            // Oscilloscope-inspired graticule legend sits on the physical control panel.
            Face(shell, "Graticule metal plaque", .860f, .185f, .976f, .280f, 4, 2,
                Hex("7B8068"), Hex("5E6654"), Hex("D9D9B6"), Hex("474C3F"));
            for (int x = 0; x <= 8; x++)
                Rule(shell, "Graticule vertical " + x, .869f + x * .0124f, .201f, .8696f + x * .0124f, .265f,
                    new Color(.79f,.84f,.65f,x == 4 ? .62f : .24f));
            for (int y = 0; y <= 4; y++)
                Rule(shell, "Graticule horizontal " + y, .869f, .201f + y * .016f, .9685f, .2017f + y * .016f,
                    new Color(.79f,.84f,.65f,y == 2 ? .62f : .24f));
            for (int s = 0; s < 26; s++)
            {
                float x = .872f + s * .0036f;
                float y = .233f + Mathf.Sin(s * .41f) * .018f;
                Face(shell, "Etched calibration trace " + s, x, y, x + .0037f, y + .0014f, 0, 0,
                    Hex("D3D9A0"), Hex("D3D9A0"), Color.white, Color.black);
            }
            Label(shell, "Timing label", "SYNC  •  31.5 kHz", .862f, .163f, .977f, .184f, 10, Ink, font);

            // A broad, carefully detailed lower chin balances the deep glass recess.
            Face(shell, "Logo badge shadow", .061f, .047f, .217f, .109f, 6, 3,
                Hex("817C69"), Hex("77715F"), Hex("8C846C"), Highlight);
            Face(shell, "Logo aluminium badge", .063f, .051f, .215f, .106f, 3, 2,
                Hex("DDDCC5"), Hex("ADAE99"), Hex("F7F5E1"), Hex("555F56"));
            Label(shell, "Badge brand", "HONGMENG", .074f, .070f, .207f, .102f, 23, Hex("2C4943"), font,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Label(shell, "Badge system line", "HM–98  /  MICROCOMPUTER", .075f, .054f, .207f, .073f, 9.5f, Ink, font,
                TextAlignmentOptions.MidlineLeft);
            Label(shell, "Lower chin system descriptor", "MULTISCAN COLOUR  ·  HIGH RESOLUTION", .242f, .083f, .62f, .11f, 12, Ink, font,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Label(shell, "Asset serial label", "S/N  HM98–040128   |   REV. 2.1", .243f, .030f, .610f, .049f, 10, Ink, font,
                TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < 31; i++)
            {
                float x = .245f + i * .0113f;
                Face(shell, "Lower cooling slot " + (i + 1).ToString("00"), x, .055f, x + .0057f, .076f,
                    2.7f, 1.4f, Hex("484C40"), Hex("78765E"), Hex("9C947A"), Hex("E6DCC2"));
            }
            Label(shell, "Energy compliance", "LOW RADIATION\nENERGY SAVING", .652f, .055f, .775f, .101f, 10, Ink, font,
                TextAlignmentOptions.MidlineLeft);
            Face(shell, "Power switch recess", .858f, .063f, .919f, .123f, 12, 4,
                Hex("716A56"), Hex("716A56"), Hex("82785E"), Highlight);
            Face(shell, "Power switch key", .864f, .072f, .913f, .117f, 8, 4,
                Hex("E8DEC6"), Hex("C2B79B"), Hex("FFF7E0"), Hex("83765C"));
            Label(shell, "Power switch marker", "I / O", .868f, .078f, .909f, .110f, 15, Ink, font,
                TextAlignmentOptions.Center, FontStyles.Bold);
            Face(shell, "Power lamp surround", .937f, .085f, .950f, .108f, 20, 2,
                Hex("686650"), Hex("6D6B57"), Hex("776F59"), Highlight);
            Face(shell, "Power lamp green lens", .940f, .090f, .947f, .103f, 20, 1,
                Hex("CCF888"), Hex("62AD48"), Hex("EEFFBA"), Hex("416935"));
            Label(shell, "Power lamp caption", "POWER", .927f, .055f, .969f, .078f, 10, Ink, font);

            Screw(shell, "Upper left chassis screw", .022f, .957f, 27f);
            Screw(shell, "Upper right chassis screw", .977f, .959f, -18f);
            Screw(shell, "Lower left chassis screw", .025f, .033f, -14f);
            Screw(shell, "Lower right chassis screw", .975f, .034f, 32f);
            Screw(shell, "Instrument bank screw top", .863f, .811f, -37f, 9f);
            Screw(shell, "Instrument bank screw lower", .972f, .332f, 14f, 9f);

            viewport.SetParent(monitor, false);
            viewport.anchorMin = new Vector2(.055f, .175f);
            viewport.anchorMax = new Vector2(.811f, .916f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.localScale = Vector3.one;
            viewport.SetAsLastSibling();

            RectTransform glass = Empty(monitor, "Retro CRT Glass", viewport.anchorMin, viewport.anchorMax);
            Undo.RegisterCreatedObjectUndo(glass.gameObject, "Add CRT glass overlay");
            var glassGroup = glass.gameObject.AddComponent<CanvasGroup>();
            glassGroup.interactable = true;
            glassGroup.blocksRaycasts = true;
            RawImage glassFace = glass.gameObject.AddComponent<RawImage>();
            glassFace.color = Color.white;
            glassFace.material = glassMaterial;
            glassFace.raycastTarget = true;
            glassFace.uvRect = new Rect(0f, 0f, 1f, 1f);
            glass.SetAsLastSibling();
            CrtAspectLayout layout = monitor.GetComponent<CrtAspectLayout>();
            if (layout == null) layout = monitor.gameObject.AddComponent<CrtAspectLayout>();
            layout.Configure(monitor, viewport, glass,
                (RectTransform)shell.Find("Recessed display pocket"),
                (RectTransform)shell.Find("Moulded dark graphite CRT bezel"),
                (RectTransform)shell.Find("Inner glass gasket"),
                (RectTransform)shell.Find("Phosphor glass bed"),
                (RectTransform)shell.Find("Bezel crown reflection"),
                (RectTransform)shell.Find("Recess lower light-catching lip"));
            EditorUtility.SetDirty(monitor.gameObject);
            AssetDatabase.SaveAssets();
        }

        private static void Knob(RectTransform shell, string title, float x, float y,
            TMP_FontAsset font, float pointerAngle, string low, string high)
        {
            RectTransform root = Empty(shell, title + " Knurled Dial", new Vector2(x, y), new Vector2(x, y));
            root.sizeDelta = new Vector2(176f, 176f);
            PixelFace(root, "Cast dial shadow", 4f, -6f, 116f, 116f, 58f, 5f,
                Hex("928B73"), Hex("7C775F"), Hex("A9A087"), Hex("B8AD91"));
            PixelFace(root, "Knurled outer rim", 0, 0, 108f, 108f, 54f, 5f,
                Hex("5B5D50"), Hex("343A31"), Hex("8A8A76"), Hex("252D26"));
            for (int i = 0; i < 36; i++)
            {
                float angle = i * 10f;
                float rad = angle * Mathf.Deg2Rad;
                CrtShape ridge = PixelFace(root, "Rim knurl " + i, Mathf.Sin(rad) * 48f, Mathf.Cos(rad) * 48f,
                    2.1f, 8f, .6f, .5f, Hex("8A8B75"), Hex("6B6F5D"), Hex("A2A38B"), Hex("2C372D"));
                ridge.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            PixelFace(root, "Recessed bakelite dial face", 0, 0, 87f, 87f, 43.5f, 7f,
                Hex("73796A"), Hex("414A3F"), Hex("ABB09A"), Hex("29352D"));
            PixelFace(root, "Dial centre cap", 0, 0, 60f, 60f, 30f, 1.5f,
                Hex("737A69"), Hex("4C5848"), Hex("848F76"), Hex("3C4738"));
            float pointer = pointerAngle * Mathf.Deg2Rad;
            CrtShape line = PixelFace(root, "Ivory dial pointer", Mathf.Sin(pointer) * 31f, Mathf.Cos(pointer) * 31f,
                3.4f, 18f, 1f, .5f, Hex("E4E2BF"), Hex("B9C39B"), Hex("ECECCE"), Hex("6E7A5D"));
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, -pointerAngle);
            for (int i = 0; i <= 10; i++)
            {
                float angle = -135f + i * 27f;
                float radians = angle * Mathf.Deg2Rad;
                CrtShape mark = PixelFace(root, "Calibrated tick " + i,
                    Mathf.Sin(radians) * 72f, Mathf.Cos(radians) * 72f, i % 5 == 0 ? 2.4f : 1.1f,
                    i % 5 == 0 ? 10f : 6f, 0, 0, Ink, Ink, Ink, Ink);
                mark.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            PixelLabel(root, "Dial label", title, 0, 100f, 220f, 28f, 16f, Ink, font, FontStyles.Bold);
            PixelLabel(root, "Minimum legend", low, -50f, -71f, 76f, 19f, 10f, Ink, font);
            PixelLabel(root, "Maximum legend", high, 50f, -71f, 76f, 19f, 10f, Ink, font);
        }

        private static void Screw(RectTransform parent, string name, float x, float y, float angle, float diameter = 13f)
        {
            RectTransform holder = Empty(parent, name, new Vector2(x, y), new Vector2(x, y));
            holder.sizeDelta = new Vector2(diameter, diameter);
            PixelFace(holder, "Countersunk socket", 0, 0, diameter + 5, diameter + 5, diameter, 2f,
                Hex("8D846C"), Hex("A79B7C"), Hex("958A70"), Highlight);
            PixelFace(holder, "Steel screw head", 0, 0, diameter, diameter, diameter * .5f, 2f,
                Hex("B1B099"), Hex("8A8C77"), Hex("D8D7BE"), Hex("5D6354"));
            CrtShape slot = PixelFace(holder, "Screwdriver slot", 0, 0, diameter * .67f, 1.7f, .4f, 0,
                Hex("525B4F"), Hex("525B4F"), Color.black, Color.black);
            slot.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private static CrtShape Face(RectTransform parent, string name, float x0, float y0, float x1, float y1,
            float radius, float bevel, Color top, Color bottom, Color light, Color dark)
        {
            RectTransform rect = Empty(parent, name, new Vector2(x0, y0), new Vector2(x1, y1));
            CrtShape graphic = rect.gameObject.AddComponent<CrtShape>();
            graphic.Configure(radius, bevel, top, bottom, light, dark);
            return graphic;
        }

        private static CrtShape PixelFace(RectTransform parent, string name, float x, float y, float w, float h,
            float radius, float bevel, Color top, Color bottom, Color light, Color dark)
        {
            CrtShape shape = Face(parent, name, .5f, .5f, .5f, .5f, radius, bevel, top, bottom, light, dark);
            shape.rectTransform.sizeDelta = new Vector2(w, h);
            shape.rectTransform.anchoredPosition = new Vector2(x, y);
            return shape;
        }

        private static void Rule(RectTransform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            Face(parent, name, x0, y0, x1, y1, 0, 0, color, color, color, color);
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, string value,
            float x0, float y0, float x1, float y1, float size, Color color, TMP_FontAsset font,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Empty(parent, name, new Vector2(x0, y0), new Vector2(x1, y1));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
            return label;
        }

        private static void PixelLabel(RectTransform parent, string name, string value, float x, float y,
            float w, float h, float size, Color color, TMP_FontAsset font, FontStyles style = FontStyles.Normal)
        {
            TextMeshProUGUI label = Label(parent, name, value, .5f, .5f, .5f, .5f, size, color, font,
                TextAlignmentOptions.Center, style);
            label.rectTransform.sizeDelta = new Vector2(w, h);
            label.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        private static RectTransform Empty(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Material GetGlassMaterial()
        {
            const string path = RootPath + "/CRT Glass.mat";
            var shader = Shader.Find("HongmengOS/Retro/CRT Glass");
            if (shader == null) throw new InvalidOperationException("CRT Glass shader has not imported. Refresh assets before building the shell.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "CRT Glass · subtle static phosphor" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_ScanLines", 410f);
            material.SetFloat("_ScanStrength", .095f);
            material.SetFloat("_Vignette", .30f);
            material.SetFloat("_Reflection", .095f);
            material.SetFloat("_Focus", .52f);
            material.SetFloat("_Brightness", 1f);
            material.SetFloat("_Curvature", .10f);
            material.SetFloat("_Bloom", .18f);
            material.SetFloat("_Halation", .10f);
            material.SetFloat("_Chromatic", .48f);
            material.SetFloat("_Phosphor", .035f);
            material.SetFloat("_Power", 1f);
            material.SetFloat("_Flash", 0f);
            material.SetFloat("_Collapse", 1f);
            material.SetFloat("_Aspect", 4f / 3f);
            material.SetFloat("_FlipY", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemoveGenerated(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
