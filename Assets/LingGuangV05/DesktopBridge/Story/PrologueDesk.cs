using System;
using System.Collections;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Left double-click, right-click and drag-end callbacks for a desktop item (the LCD surface relays click counts).</summary>
    public sealed class PrologueClick : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
    {
        public Action Open, Menu;
        public Action<Vector3> Dropped;
        Vector3 dragStart;

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right) { Menu?.Invoke(); return; }
            if (data.button == PointerEventData.InputButton.Left && data.clickCount >= 2) Open?.Invoke();
        }

        public void OnBeginDrag(PointerEventData data) { dragStart = transform.localPosition; }
        public void OnEndDrag(PointerEventData data) { Dropped?.Invoke(dragStart); }
    }

    /// <summary>
    /// Keeps an added desktop icon off the others. The native icons are laid out (and their saved positions restored)
    /// after ours are made, so an icon placed early can end up on top of one of them and swallow its double-click
    /// (the calculator could not be opened under sophon.dll). Checked twice a second; it moves only when it overlaps.
    /// </summary>
    public sealed class DesktopIconSpacing : MonoBehaviour
    {
        float next;
        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f;
            var rt = (RectTransform)transform;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (rt.parent == null || mouse != null && mouse.leftButton.isPressed) return; // not while the player is dragging
            foreach (Transform child in rt.parent)
            {
                if (child == rt || !child.gameObject.activeSelf) continue;
                if ((((RectTransform)child).anchoredPosition - rt.anchoredPosition).sqrMagnitude < 50 * 50)
                {
                    PrologueDesk.PlaceInFreeCell(rt);
                    var dragger = GetComponent<Michsky.DreamOS.ItemDragger>();
                    if (dragger != null && dragger.rememberPosition) dragger.UpdatePositionData();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Building blocks for the prologue, drawn inside the desktop canvas (so they appear on the monitor): 2016
    /// Aero-style windows, dialogs, desktop icons, the tray balloon and a fake mouse cursor. Nothing here touches
    /// DreamOS assets or saved desktop data.
    /// </summary>
    public sealed class PrologueDesk
    {
        public readonly RectTransform layer, windows, icons;
        public readonly TMP_FontAsset font;
        public readonly Sprite rounded, notepadIcon, photoIcon, browserIcon;
        public RectTransform Cursor { get; private set; }

        public static readonly Color TitleTop = new Color32(214, 229, 245, 255), TitleBottom = new Color32(185, 209, 234, 255);
        public static readonly Color Frame = new Color32(120, 150, 185, 255), Ink = new Color32(30, 30, 30, 255), Muted = new Color32(110, 110, 110, 255);

        public PrologueDesk(RectTransform desktop, RectTransform desktopList, RectTransform appsAndWindows, TMP_FontAsset fontAsset)
        {
            font = fontAsset;
            icons = desktopList;
            windows = appsAndWindows;
            // The managed desk is lost on a script reload; the scene objects are not.
            // Keep the shared layer (and other apps' popups) instead of stacking a new one.
            layer = desktop.Find("Prologue Layer") as RectTransform
                ?? Rect("Prologue Layer", desktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            for (int i = desktop.childCount - 1; i >= 0; i--)
            {
                var old = desktop.GetChild(i);
                if (old == layer || old.name != "Prologue Layer") continue;
                old.gameObject.SetActive(false); // stop the old popup controller before moving shared children
                for (int j = old.childCount - 1; j >= 0; j--)
                {
                    var child = old.GetChild(j);
                    if (child.name == "Tray Popup Area") RemoveTransient(child.gameObject);
                    else child.SetParent(layer, false);
                }
                RemoveTransient(old.gameObject);
            }
            Cursor = layer.Find("Cursor") as RectTransform;
            HideCursor();
            layer.SetAsLastSibling();
            rounded = FindSprite(desktopList, "Notepad", "Background");
            notepadIcon = FindSprite(desktopList, "Notepad", "Icon");
            photoIcon = FindSprite(desktopList, "Photo Gallery", "Icon");
            browserIcon = FindSprite(desktopList, "Web Browser", "Icon");
        }

        internal static void RemoveTransient(GameObject go)
        {
            if (go == null) return;
            // Stop intercepting input immediately, even before end-of-frame destruction.
            go.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        static Sprite FindSprite(RectTransform list, string item, string part)
        {
            var t = list.Find(item);
            if (t == null) return null;
            foreach (var img in t.GetComponentsInChildren<Image>(true)) if (img.name == part && img.sprite != null) return img.sprite;
            return null;
        }

        static TMP_FontAsset cjk;

        /// <summary>A dynamic Noto Sans CJK font asset (the desktop's own fonts have no Chinese glyphs), made once per session.</summary>
        public static TMP_FontAsset CjkFont()
        {
            if (cjk != null) return cjk;
            Font source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null) return cjk = TMP_Settings.defaultFontAsset;
            cjk = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            cjk.name = "LingGuangV05_Desktop_Cjk";
            cjk.hideFlags = HideFlags.DontSave;
            cjk.isMultiAtlasTexturesEnabled = true;
            return cjk;
        }

        // ───────────── primitives ─────────────

        public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static RectTransform Centered(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rt = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.sizeDelta = size; rt.anchoredPosition = position;
            return rt;
        }

        public static Image Fill(RectTransform rt, Color color, bool raycast = true)
        {
            var img = rt.gameObject.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.color = color; img.raycastTarget = raycast;
            return img;
        }

        public TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public Button Button(RectTransform parent, string label, Vector2 position, Vector2 size, Action onClick, Color? fill = null)
        {
            var rt = Centered("Button", parent, position, size);
            var img = Fill(rt, fill ?? new Color32(236, 236, 236, 255));
            var outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = new Color32(112, 112, 112, 255); outline.effectDistance = new Vector2(1, -1);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Text(Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), label, 18, Ink, TextAlignmentOptions.Center);
            return b;
        }

        // ───────────── windows ─────────────

        /// <summary>An Aero window: light-blue title bar, title, a red close button and a white client area. Returns the client.</summary>
        public RectTransform Window(string name, string title, Vector2 position, Vector2 size, out RectTransform root, Action onClose = null)
        {
            root = Centered(name, windows, position, size);
            root.SetAsLastSibling();
            Fill(root, Frame);
            var bar = Rect("Title", root, new Vector2(0, 1), Vector2.one, new Vector2(1, -32), new Vector2(-1, -1));
            Fill(bar, TitleBottom);
            var top = Rect("Shine", bar, new Vector2(0, .5f), Vector2.one, Vector2.zero, Vector2.zero);
            Fill(top, TitleTop, false);
            Text(Rect("Text", bar, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-60, 0)), title, 17, Ink, TextAlignmentOptions.MidlineLeft);
            var close = Rect("Close", bar, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-50, -11), new Vector2(-8, 11));
            var closeImg = Fill(close, new Color32(199, 80, 60, 255));
            Text(Rect("X", close, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "×", 18, Color.white, TextAlignmentOptions.Center);
            var shut = root;
            var cb = close.gameObject.AddComponent<Button>(); cb.targetGraphic = closeImg;
            cb.onClick.AddListener(() => { onClose?.Invoke(); if (shut != null) UnityEngine.Object.Destroy(shut.gameObject); });
            var client = Rect("Client", root, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -34));
            Fill(client, Color.white);
            return client;
        }

        public static Vector2 CloseButtonOf(RectTransform window)
        {
            var close = window != null ? window.Find("Title/Close") as RectTransform : null;
            return close != null ? (Vector2)close.position : Vector2.zero;
        }

        // ───────────── desktop icons ─────────────

        /// <summary>A desktop item in the native icon grid: 56 px icon, white label with shadow.</summary>
        public RectTransform Icon(string name, string label, Sprite sprite, Action<RectTransform> drawInstead = null)
        {
            var rt = Rect(name, icons, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.sizeDelta = new Vector2(112, 112);
            Fill(rt, new Color(1, 1, 1, 0));
            var art = Centered("Icon", rt, new Vector2(0, 14), new Vector2(56, 56));
            if (drawInstead != null) drawInstead(art);
            else { var img = Fill(art, Color.white, false); img.sprite = sprite; img.preserveAspect = true; }
            var title = Text(Centered("Title", rt, new Vector2(0, -34), new Vector2(118, 40)), label, 16, Color.white, TextAlignmentOptions.Top);
            var shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .8f); shadow.effectDistance = new Vector2(1, -1);
            rt.gameObject.AddComponent<PrologueClick>();
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            PlaceInFreeCell(rt);
            rt.gameObject.AddComponent<DesktopIconSpacing>();
            return rt;
        }

        /// <summary>The desktop grid only lays out icons when the desktop starts; later items take the first free cell, column by column.</summary>
        public static void PlaceInFreeCell(RectTransform icon)
        {
            var grid = icon.parent != null ? icon.parent.GetComponent<GridLayoutGroup>() : null;
            Vector2 cell = grid != null ? grid.cellSize : new Vector2(112, 112), spacing = grid != null ? grid.spacing : new Vector2(14, 9);
            float left = grid != null ? grid.padding.left : 25, top = grid != null ? grid.padding.top : 24;
            var others = new System.Collections.Generic.List<Vector2>();
            foreach (Transform child in icon.parent)
                if (child != icon && child.gameObject.activeSelf) others.Add(((RectTransform)child).anchoredPosition);
            bool Taken(Vector2 p) { foreach (var o in others) if ((o - p).sqrMagnitude < 50 * 50) return true; return false; }
            float height = ((RectTransform)icon.parent).rect.height;
            int rows = Mathf.Max(1, Mathf.FloorToInt((height - top) / (cell.y + spacing.y)));
            for (int col = 0; col < 14; col++)
                for (int row = 0; row < rows; row++)
                {
                    var p = new Vector2(left + cell.x / 2 + col * (cell.x + spacing.x), -(top + cell.y / 2 + row * (cell.y + spacing.y)));
                    if (Taken(p)) continue;
                    icon.anchoredPosition = p;
                    return;
                }
        }

        public static void SetIconLabel(RectTransform icon, string label)
        {
            var t = icon != null ? icon.Find("Title")?.GetComponent<TMP_Text>() : null;
            if (t != null) t.text = label;
        }

        /// <summary>An archive icon (stacked books, 2016 WinRAR).</summary>
        public void DrawArchive(RectTransform art)
        {
            Color[] books = { new Color32(150, 60, 160, 255), new Color32(60, 110, 190, 255), new Color32(60, 150, 80, 255) };
            for (int i = 0; i < 3; i++) Fill(Centered("Book", art, new Vector2(0, 16 - i * 16), new Vector2(46, 14)), books[i], false);
            Fill(Centered("Strap", art, Vector2.zero, new Vector2(8, 52)), new Color32(230, 190, 60, 255), false);
        }

        /// <summary>A plain file with no icon of its own (sophon.dll).</summary>
        public void DrawBlankFile(RectTransform art)
        {
            Fill(Centered("Edge", art, Vector2.zero, new Vector2(42, 54)), new Color32(150, 150, 150, 255), false);
            Fill(Centered("Page", art, Vector2.zero, new Vector2(40, 52)), Color.white, false);
            Fill(Centered("Fold", art, new Vector2(13, 19), new Vector2(14, 14)), new Color32(220, 220, 220, 255), false);
        }

        public void DrawRecycleBin(RectTransform art)
        {
            Fill(Centered("Lid", art, new Vector2(0, 20), new Vector2(46, 6)), new Color32(200, 215, 230, 255), false);
            Fill(Centered("Bin", art, new Vector2(0, -4), new Vector2(38, 42)), new Color32(170, 195, 220, 210), false);
            for (int i = -1; i <= 1; i++) Fill(Centered("Rib", art, new Vector2(i * 11, -4), new Vector2(3, 34)), new Color32(120, 150, 180, 255), false);
        }

        // ───────────── tray balloon ─────────────

        /// <summary>A Win7 balloon above the tray, bottom right. Fades after a few seconds.</summary>
        /// <summary>A tray message (360, 迅雷, Windows …): queued as a popup that slides up above the taskbar.</summary>
        public IEnumerator Balloon(MonoBehaviour host, string who, string text, float seconds)
        {
            StoryPopup(who, text, seconds);
            yield break;
        }

        /// <summary>
        /// Queues a 2016-style tray popup in the bottom-right corner, above the taskbar (one at a time). This is ambient
        /// chatter (ads, tray software, platform notices): the opening quiet window drops it. Story moments and answers
        /// to something the player just did use <see cref="StoryPopup"/>.
        /// </summary>
        public void Popup(string who, string text, float seconds = 7) => Queue(who, text, seconds, false);

        /// <summary>A tray popup that belongs to the story or answers the player's own action; it is never held back.</summary>
        public void StoryPopup(string who, string text, float seconds = 7) => Queue(who, text, seconds, true);

        void Queue(string who, string text, float seconds, bool story)
        {
            var popups = layer.GetComponent<TrayPopups>() ?? layer.gameObject.AddComponent<TrayPopups>();
            popups.desk = this;
            popups.Enqueue(who, text, seconds, story);
        }

        // ───────────── the hijacked cursor ─────────────

        static Sprite circle;

        /// <summary>A smooth white disc (go stones, drops), drawn once.</summary>
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - n / 2f + .5f) * (x - n / 2f + .5f) + (y - n / 2f + .5f) * (y - n / 2f + .5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - 1 - d)));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(.5f, .5f));
            circle.hideFlags = HideFlags.DontSave;
            return circle;
        }

        static Sprite arrow;

        /// <summary>The classic arrow: black outline, white fill, drawn once into a 12×17 texture (shown at 2×).</summary>
        static Sprite Arrow()
        {
            if (arrow != null) return arrow;
            string[] rows =
            {
                "X",
                "XX",
                "X.X",
                "X..X",
                "X...X",
                "X....X",
                "X.....X",
                "X......X",
                "X.......X",
                "X........X",
                "X.....XXXXX",
                "X..X..X",
                "X.X X..X",
                "XX  X..X",
                "X    X..X",
                "     X..X",
                "      XX",
            };
            int w = 12, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : ' ';
                    tex.SetPixel(x, h - 1 - y, c == 'X' ? Color.black : c == '.' ? Color.white : new Color(0, 0, 0, 0));
                }
            tex.Apply();
            arrow = Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h), new Vector2(0, 1), 1);
            arrow.hideFlags = HideFlags.DontSave;
            return arrow;
        }

        public void ShowCursor(Vector2 at)
        {
            if (Cursor == null)
            {
                Cursor = Rect("Cursor", layer, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                Cursor.pivot = new Vector2(0, 1);
                Cursor.sizeDelta = new Vector2(24, 34);
                var img = Fill(Cursor, Color.white, false);
                img.sprite = Arrow();
            }
            Cursor.gameObject.SetActive(true);
            Cursor.SetAsLastSibling();
            Cursor.anchoredPosition = at;
        }

        public void HideCursor() { if (Cursor != null) Cursor.gameObject.SetActive(false); }

        /// <summary>Where an element sits in the layer's coordinates (centre of the desktop = 0,0).</summary>
        public Vector2 LocalOf(RectTransform target, Vector2 offset = default)
        {
            if (target == null) return offset;
            return (Vector2)layer.InverseTransformPoint(target.TransformPoint(target.rect.center)) + offset;
        }

        public IEnumerator Move(Vector2 to, float seconds)
        {
            if (Cursor == null) yield break;
            Vector2 from = Cursor.anchoredPosition;
            // A hand on a mouse: ease in and out, with a slight curve.
            Vector2 bend = new Vector2(-(to - from).y, (to - from).x) * .08f;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                Cursor.anchoredPosition = Vector2.Lerp(from, to, k) + bend * Mathf.Sin(k * Mathf.PI);
                yield return null;
            }
            Cursor.anchoredPosition = to;
        }

        public IEnumerator Click(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                if (Cursor != null) Cursor.localScale = Vector3.one * .85f;
                for (float t = 0; t < .07f; t += Time.unscaledDeltaTime) yield return null;
                if (Cursor != null) Cursor.localScale = Vector3.one;
                for (float t = 0; t < .09f; t += Time.unscaledDeltaTime) yield return null;
            }
        }

        public static IEnumerator Type(TMP_Text label, string start, string text, float perChar)
        {
            for (int i = 0; i <= text.Length; i++)
            {
                if (label == null) yield break;
                label.text = start + text.Substring(0, i);
                for (float t = 0; t < perChar; t += Time.unscaledDeltaTime) yield return null;
            }
        }

        public static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        public static string T(string zh, string en) => GameText.T(zh, en);
    }
}
