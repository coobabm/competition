using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LingGuangV05.Core.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// Turns the procedural YY faces (<see cref="YYFaceArt"/>) into one atlas texture and a runtime TMP sprite asset,
    /// and turns chat text into TMP markup that shows the faces inline: <c>&lt;sprite name="smile"&gt;</c>.
    /// Text runs are wrapped in &lt;noparse&gt; so whatever the player or a model typed is shown literally.
    /// </summary>
    public static class YYFaceRenderer
    {
        public const int CellPx = YYFaceArt.Cell;
        const int Pitch = CellPx + 2, Columns = 8;
        /// <summary>Faces are drawn a little taller than the text, like the 2016 clients.</summary>
        public const float InlineScale = 1.4f;
        /// <summary>Two-frame faces animate in chat bubbles at this rate (frames per second).</summary>
        const int AnimationFps = 2;

        static Texture2D atlas;
        static TMP_SpriteAsset asset;
        static readonly Dictionary<string, int> characterIndex = new Dictionary<string, int>();
        static readonly Dictionary<string, Rect> cellRect = new Dictionary<string, Rect>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> stickerSprites = new Dictionary<string, Sprite>();

        /// <summary>The face atlas (built on first use).</summary>
        public static Texture2D Atlas { get { Ensure(); return atlas; } }

        /// <summary>The runtime sprite asset; assign it to any TMP text that shows chat lines.</summary>
        public static TMP_SpriteAsset Asset { get { Ensure(); return asset; } }

        // ───────────── markup ─────────────

        /// <summary>Chat text → TMP markup: literal text plus inline faces. Unknown [codes] stay text.</summary>
        public static string Markup(string plain, bool animate = false)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            var sb = new StringBuilder(plain.Length + 32);
            foreach (var run in YYFaces.Parse(plain))
            {
                if (run.IsFace) sb.Append(Tag(run.Face, animate));
                else AppendLiteral(sb, run.Text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// For text that is already safe markup (for example the attention underline, which escapes angle brackets
        /// itself): only the face codes are swapped for sprite tags.
        /// </summary>
        public static string FaceTags(string markup, bool animate = false)
        {
            if (!YYFaces.ContainsFace(markup)) return markup ?? "";
            var sb = new StringBuilder(markup.Length + 32);
            foreach (var run in YYFaces.Parse(markup)) sb.Append(run.IsFace ? Tag(run.Face, animate) : run.Text);
            return sb.ToString();
        }

        /// <summary>
        /// A one-line preview: at most <paramref name="maxChars"/> characters, where a face counts as one and is never
        /// cut in half. Newlines become spaces.
        /// </summary>
        public static string PreviewMarkup(string plain, int maxChars)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            plain = plain.Replace("\r", "").Replace("\n", " ");
            var sb = new StringBuilder();
            int used = 0;
            foreach (var run in YYFaces.Parse(plain))
            {
                if (used >= maxChars) { sb.Append('…'); return sb.ToString(); }
                if (run.IsFace) { sb.Append(Tag(run.Face, false)); used++; continue; }
                string t = run.Text;
                if (used + t.Length > maxChars) { AppendLiteral(sb, t.Substring(0, maxChars - used)); sb.Append('…'); return sb.ToString(); }
                AppendLiteral(sb, t);
                used += t.Length;
            }
            return sb.ToString();
        }

        /// <summary>Sets a TMP text to chat content with inline faces.</summary>
        public static void SetText(TMP_Text label, string plain, bool animate = false)
        {
            if (label == null) return;
            Prepare(label);
            label.text = Markup(plain, animate);
        }

        /// <summary>Lets a TMP text show faces: rich text on, this sprite asset attached.</summary>
        public static void Prepare(TMP_Text label)
        {
            if (label == null) return;
            label.richText = true;
            var a = Asset;
            if (a != null && label.spriteAsset != a) label.spriteAsset = a;
        }

        static string Tag(YYFace face, bool animate)
        {
            Ensure();
            if (asset == null) return Literal(face.Code);
            if (animate && YYFaceArt.Frames(face) > 1 && characterIndex.TryGetValue(face.Id, out int first))
                return "<sprite anim=\"" + first + "," + (first + 1) + "," + AnimationFps + "\">";
            return "<sprite name=\"" + face.Id + "\">";
        }

        static string Literal(string text) { var sb = new StringBuilder(); AppendLiteral(sb, text); return sb.ToString(); }

        static void AppendLiteral(StringBuilder sb, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            // A closing noparse typed into the chat must not end the literal run early.
            sb.Append("<noparse>").Append(text.Replace("</noparse>", "</no\u200Bparse>")).Append("</noparse>");
        }

        // ───────────── sprites for UI images (picker, tips) ─────────────

        /// <summary>A UI sprite of one face (first frame).</summary>
        public static Sprite SpriteFor(YYFace face)
        {
            if (face == null) return null;
            Ensure();
            if (sprites.TryGetValue(face.Id, out var s) && s != null) return s;
            if (atlas == null || !cellRect.TryGetValue(face.Id, out var r)) return null;
            s = Sprite.Create(atlas, r, new Vector2(.5f, .5f), 100);
            s.name = "yyface_" + face.Id; s.hideFlags = HideFlags.DontSave;
            sprites[face.Id] = s;
            return s;
        }

        /// <summary>The picture of a 斗图 sticker (the caption is drawn over it by the view).</summary>
        public static Sprite StickerSprite(YYSticker sticker, int size = 96)
        {
            if (sticker == null) return null;
            string key = sticker.Id + "@" + size;
            if (stickerSprites.TryGetValue(key, out var s) && s != null) return s;
            var tex = ToTexture(YYFaceArt.DrawSticker(sticker, size), "yysticker_" + sticker.Id);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
            s.name = tex.name; s.hideFlags = HideFlags.DontSave;
            stickerSprites[key] = s;
            return s;
        }

        // ───────────── atlas and sprite asset ─────────────

        static void Ensure()
        {
            if (atlas != null && asset != null) return;
            try { Build(); }
            catch (Exception error) { Debug.LogWarning("YY faces: could not build the face atlas: " + error.Message); }
        }

        /// <summary>Cells in atlas order: every face's first frame, then the second frames.</summary>
        static List<(YYFace face, int frame)> Cells()
        {
            var cells = new List<(YYFace, int)>();
            foreach (var face in YYFaces.All)
            {
                cells.Add((face, 0));
                // Animated faces keep their frames next to each other: TMP's anim tag plays consecutive indexes.
                if (YYFaceArt.Frames(face) > 1) cells.Add((face, 1));
            }
            return cells;
        }

        static void Build()
        {
            var cells = Cells();
            int rows = (cells.Count + Columns - 1) / Columns;
            int width = Columns * Pitch, height = rows * Pitch;
            var pixels = new Color32[width * height];
            characterIndex.Clear(); cellRect.Clear();
            var glyphs = new List<TMP_SpriteGlyph>();
            var characters = new List<TMP_SpriteCharacter>();
            for (int i = 0; i < cells.Count; i++)
            {
                var (face, frame) = cells[i];
                var canvas = YYFaceArt.DrawFace(face, CellPx, frame);
                var bytes = canvas.ToBytes();
                int cx = (i % Columns) * Pitch + 1;
                int cy = height - ((i / Columns) + 1) * Pitch + 1; // texture rows run bottom-up
                for (int y = 0; y < CellPx; y++)
                    for (int x = 0; x < CellPx; x++)
                    {
                        int s = (y * CellPx + x) * 4;
                        pixels[(cy + (CellPx - 1 - y)) * width + cx + x] = new Color32(bytes[s], bytes[s + 1], bytes[s + 2], bytes[s + 3]);
                    }
                string name = frame == 0 ? face.Id : face.Id + "_" + (frame + 1);
                if (frame == 0) { characterIndex[face.Id] = i; cellRect[face.Id] = new Rect(cx, cy, CellPx, CellPx); }
                // Bearing puts about a fifth of the face below the baseline so it sits centred on CJK text.
                var metrics = new GlyphMetrics(CellPx, CellPx, 0, CellPx * .78f, CellPx + 2);
                var glyph = new TMP_SpriteGlyph((uint)i, metrics, new GlyphRect(cx, cy, CellPx, CellPx), 1f, 0);
                glyphs.Add(glyph);
                var character = new TMP_SpriteCharacter(0xFFFE, glyph) { name = name, scale = InlineScale };
                characters.Add(character);
            }

            atlas = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "YY Faces Atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            atlas.SetPixels32(pixels);
            atlas.Apply(false, false);

            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "YY Faces";
            asset.hideFlags = HideFlags.DontSave;
            // Runtime assets have no serialized version; mark it current so TMP does not try to "upgrade" the empty
            // legacy list over our tables.
            MarkCurrentVersion(asset);
            asset.spriteSheet = atlas;
            asset.spriteGlyphTable.AddRange(glyphs);
            asset.spriteCharacterTable.AddRange(characters);
            asset.material = CreateMaterial(atlas);
            asset.UpdateLookupTables();
        }

        static void MarkCurrentVersion(TMP_SpriteAsset a)
        {
            for (var type = a.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("m_Version", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field == null) continue;
                field.SetValue(a, "1.1.0");
                return;
            }
        }

        static Material CreateMaterial(Texture tex)
        {
            var shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null && TMP_Settings.defaultSpriteAsset != null && TMP_Settings.defaultSpriteAsset.material != null)
                shader = TMP_Settings.defaultSpriteAsset.material.shader;
            if (shader == null) shader = Shader.Find("UI/Default");
            var m = new Material(shader) { name = "YY Faces Material", hideFlags = HideFlags.DontSave };
            m.SetTexture(ShaderUtilities.ID_MainTex, tex);
            return m;
        }

        static Texture2D ToTexture(YYCanvas canvas, string name)
        {
            int n = canvas.Size;
            var bytes = canvas.ToBytes();
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int s = (y * n + x) * 4;
                    pixels[(n - 1 - y) * n + x] = new Color32(bytes[s], bytes[s + 1], bytes[s + 2], bytes[s + 3]);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // ───────────── evidence ─────────────

        /// <summary>
        /// Writes the atlas (and a sticker sheet next to it) as PNG. Safe in Edit mode; used for visual checks,
        /// e.g. Evidence/DesignV11/yy_faces_atlas.png. Returns the atlas path.
        /// </summary>
        public static string WriteAtlasPng(string path)
        {
            Ensure();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, atlas.EncodeToPNG());
            int s = 96, cols = 6, rows = (YYStickers.All.Count + cols - 1) / cols;
            var sheet = new Texture2D(cols * s, rows * s, TextureFormat.RGBA32, false);
            var clear = new Color32[cols * s * rows * s];
            sheet.SetPixels32(clear);
            for (int i = 0; i < YYStickers.All.Count; i++)
            {
                var tex = StickerSprite(YYStickers.All[i], s).texture;
                sheet.SetPixels32((i % cols) * s, (rows - 1 - i / cols) * s, s, s, tex.GetPixels32());
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path), "yy_stickers.png"), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            return path;
        }

        /// <summary>
        /// Hovering a face inside <paramref name="label"/> shows its name (UiTip). Only for read-only text such as
        /// chat bubbles: the label becomes a raycast target.
        /// </summary>
        public static void AddHoverNames(TMP_Text label, string raw)
        {
            if (label == null || !YYFaces.ContainsFace(raw)) return;
            label.raycastTarget = true;
            var hover = label.GetComponent<YYFaceHover>() ?? label.gameObject.AddComponent<YYFaceHover>();
            hover.Label = label;
            UiTip.Add(label, hover.TipText);
        }

        /// <summary>The face a sprite character of the runtime asset draws ("bye" and its second frame "bye_2").</summary>
        public static YYFace FaceOfSprite(TMP_SpriteCharacter sprite)
        {
            string name = sprite != null ? sprite.name ?? "" : "";
            int frame = name.IndexOf('_');
            return YYFaces.ById(frame > 0 ? name.Substring(0, frame) : name);
        }

        /// <summary>Drops the cached atlas (after the art changes in the editor).</summary>
        public static void Reset()
        {
            atlas = null; asset = null;
            sprites.Clear(); stickerSprites.Clear(); characterIndex.Clear(); cellRect.Clear();
        }
    }

    /// <summary>Tracks which inline face the pointer is over so the hover note can name it.</summary>
    public sealed class YYFaceHover : MonoBehaviour, UnityEngine.EventSystems.IPointerMoveHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public TMP_Text Label;
        YYFace current;

        public string TipText() => current == null ? null : current.Name(LingGuangV05.Runtime.GameText.IsEnglish) + "  " + (LingGuangV05.Runtime.GameText.IsEnglish ? current.EnAlias : current.Code);

        public void OnPointerMove(UnityEngine.EventSystems.PointerEventData e)
        {
            YYFace face = null;
            if (Label != null)
            {
                var cam = e.enterEventCamera;
                int i = TMP_TextUtilities.FindIntersectingCharacter(Label, e.position, cam, true);
                var info = Label.textInfo;
                if (i >= 0 && info != null && i < info.characterCount && info.characterInfo[i].elementType == TMP_TextElementType.Sprite)
                    face = YYFaceRenderer.FaceOfSprite(info.characterInfo[i].textElement as TMP_SpriteCharacter);
            }
            if (face == current) return;
            current = face;
            // Restart the note so moving from one face to the next names the new one.
            var tip = GetComponent<UiTip>();
            if (tip == null) return;
            tip.OnPointerExit(e);
            if (face != null) tip.OnPointerEnter(e);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { current = null; }
    }
}
