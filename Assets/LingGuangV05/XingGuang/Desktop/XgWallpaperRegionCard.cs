using HongmengOS.Aero2010;
using LingGuangV05.Desktop.Story;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A small read-only explanation, owned by the desktop rather than the pinned wallpaper.</summary>
    public sealed class XgWallpaperRegionCard : MonoBehaviour
    {
        XgBrainWallpaper wallpaper;
        XgWallpaperConcepts concepts;
        XingGuangController controller;
        RectTransform card, desktop, close;
        TMP_Text title, body;
        AeroLcdSourceBridge bridge;
        object save;
        Vector2 requested;
        float nextRead;
        int selected = -1;
        public bool IsOpen => card != null && card.gameObject.activeSelf;
        public string Title => title != null ? title.text : "";
        public string Body => body != null ? body.text : "";
        public RectTransform Card => card;
        public RectTransform CloseButton => close;

        void Ensure()
        {
            if (card != null) return;
            wallpaper = GetComponent<XgBrainWallpaper>(); concepts = GetComponentInChildren<XgWallpaperConcepts>();
            desktop = (RectTransform)transform.parent;
            bridge = FindAnyObjectByType<AeroLcdSourceBridge>();
            controller = FindAnyObjectByType<XingGuangController>();
            card = PrologueDesk.Rect("Brain Region Card", desktop, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(390, 260));
            card.pivot = new Vector2(0, 1);
            PrologueDesk.Fill(card, new Color32(19, 29, 42, 250));
            card.gameObject.AddComponent<Outline>().effectColor = new Color32(91, 128, 147, 255);
            title = Text("Title", new Vector2(18, -54), new Vector2(-50, -12), 19);
            title.enableAutoSizing = true; title.fontSizeMin = 13; title.fontSizeMax = 19;
            body = Text("Body", new Vector2(18, -240), new Vector2(-18, -66), 16);
            body.lineSpacing = 5;
            close = PrologueDesk.Rect("Close", card, Vector2.one, Vector2.one, new Vector2(-42, -42), new Vector2(-10, -10));
            var closeImage = PrologueDesk.Fill(close, new Color32(36, 49, 65, 255));
            var button = close.gameObject.AddComponent<Button>(); button.targetGraphic = closeImage; button.onClick.AddListener(Hide);
            var glyph = PrologueDesk.Rect("Glyph", close, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            glyph.font = PrologueDesk.CjkFont(); glyph.text = "×"; glyph.fontSize = 20; glyph.alignment = TextAlignmentOptions.Center; glyph.raycastTarget = false;
            glyph.enableAutoSizing = true; glyph.fontSizeMin = 14; glyph.fontSizeMax = 20; glyph.textWrappingMode = TextWrappingModes.NoWrap;
            card.gameObject.SetActive(false);
        }

        TMP_Text Text(string name, Vector2 min, Vector2 max, int size)
        {
            var rect = PrologueDesk.Rect(name, card, new Vector2(0, 1), Vector2.one, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = PrologueDesk.CjkFont();
            text.fontSize = size; text.color = new Color32(220, 233, 239, 255);
            text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false; return text;
        }

        public void Show(int region, Vector2 sourcePosition, Camera camera)
        {
            if (region < 0 || region >= 4) return;
            Ensure(); selected = region;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop, sourcePosition, camera, out var local)) return;
            requested = local - desktop.rect.min + new Vector2(18, -18);
            save = CurrentSave();
            card.gameObject.SetActive(true); card.SetAsLastSibling(); Refresh();
        }

        object CurrentSave()
        {
            return controller != null ? controller.Sim?.S : null;
        }

        void Refresh()
        {
            title.text = XgWallpaperConcepts.RegionName(selected) + " · " + concepts.RegionStructure(selected);
            body.text = concepts.RegionLore(selected);
            float textHeight = body.GetPreferredValues(body.text, 354, 0).y;
            float height = Mathf.Clamp(textHeight + 92, 172, Mathf.Min(480, desktop.rect.height - 100));
            card.sizeDelta = new Vector2(390, height);
            body.rectTransform.offsetMin = new Vector2(18, -height + 20);
            card.anchoredPosition = new Vector2(Mathf.Clamp(requested.x, 12, desktop.rect.width - 402), Mathf.Clamp(requested.y, height + 62, desktop.rect.height - 12));
        }

        public void Hide() { if (card != null) card.gameObject.SetActive(false); selected = -1; }

        void Update()
        {
            if (!IsOpen) return;
            if (wallpaper == null || !wallpaper.Reveal.Visible || save != CurrentSave()) { Hide(); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Hide(); return; }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && bridge != null && bridge.inputSurface != null
                && bridge.inputSurface.TryMap(Mouse.current.position.ReadValue(), out var position)
                && !RectTransformUtility.RectangleContainsScreenPoint(card, position, bridge.sourceCamera)) { Hide(); return; }
            if (Time.unscaledTime >= nextRead) { nextRead = Time.unscaledTime + .2f; Refresh(); }
        }

        void OnDisable() { Hide(); }
        void OnDestroy() { if (card != null) Destroy(card.gameObject); }
    }
}
