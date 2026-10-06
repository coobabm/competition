using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    /// <summary>Fictional OS boot presentation confined to the native LCD, not a loading-screen camera.</summary>
    public sealed class CaseMenuBootScreen : MonoBehaviour
    {
        RectTransform root, progressFill;
        CanvasGroup cover, content;
        Image background;
        TextMeshProUGUI stage, percent;
        int lastPercent = -1, lastStage = -1;
        static readonly string[] Stages = { "正在检测硬件", "正在初始化设备", "正在载入系统", "正在准备桌面" };
        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public void Build(RectTransform display, TMP_FontAsset font)
        {
            if (root != null || display == null) return;
            root = Rect("LCD Boot Screen", display);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();
            cover = root.gameObject.AddComponent<CanvasGroup>();
            cover.blocksRaycasts = false; cover.interactable = false;
            background = root.gameObject.AddComponent<Image>();
            background.color = Color.black; background.raycastTarget = false;
            var body = Rect("Boot Content", root);
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
            body.offsetMin = body.offsetMax = Vector2.zero;
            content = body.gameObject.AddComponent<CanvasGroup>();
            content.blocksRaycasts = false; content.interactable = false;
            float h = Mathf.Max(200, display.rect.height);
            Label("Boot Firmware", body, "HONGMENG SYSTEM", font, h * .021f, new Vector2(.5f, .79f), new Vector2(.8f, .07f), new Color(.46f, .61f, .68f));
            Label("Boot Logo", body, "hongmengos", font, h * .080f, new Vector2(.5f, .60f), new Vector2(.85f, .17f), new Color(.80f, .90f, .95f));
            Label("Boot Subtitle", body, "PERSONAL WORKSTATION", font, h * .019f, new Vector2(.5f, .505f), new Vector2(.8f, .07f), new Color(.39f, .55f, .64f));
            var track = Rect("Boot Progress Track", body);
            track.anchorMin = new Vector2(.32f, .39f); track.anchorMax = new Vector2(.68f, .397f);
            track.offsetMin = track.offsetMax = Vector2.zero;
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(.13f, .23f, .29f); trackImage.raycastTarget = false;
            progressFill = Rect("Boot Progress", track);
            progressFill.anchorMin = Vector2.zero; progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
            var fill = progressFill.gameObject.AddComponent<Image>();
            fill.color = new Color(.36f, .72f, .87f); fill.raycastTarget = false;
            stage = Label("Boot Stage", body, Stages[0], font, h * .030f, new Vector2(.5f, .33f), new Vector2(.8f, .09f), new Color(.66f, .79f, .85f));
            percent = Label("Boot Percent", body, "0%", font, h * .021f, new Vector2(.5f, .265f), new Vector2(.8f, .07f), new Color(.40f, .56f, .64f));
            Label("Boot Footer", body, "LG / 16     ·     SYSTEM STARTUP", font, h * .017f, new Vector2(.5f, .10f), new Vector2(.8f, .065f), new Color(.29f, .42f, .49f));
            Hide();
        }

        public void Show()
        {
            if (root == null) return;
            root.gameObject.SetActive(true); cover.alpha = 1;
            lastPercent = lastStage = -1;
            SetProgress(0, 0);
        }

        public void SetProgress(float progress, float luminance)
        {
            if (root == null) return;
            progress = float.IsNaN(progress) ? 0 : Mathf.Clamp01(progress);
            luminance = float.IsNaN(luminance) ? 0 : Mathf.Clamp01(luminance);
            background.color = Color.Lerp(Color.black, new Color(.012f, .025f, .035f), luminance);
            content.alpha = luminance;
            progressFill.anchorMax = new Vector2(progress, 1);
            // This final reveal is within the monitor glass only; the camera never cuts or fades.
            cover.alpha = 1 - CaseMenuMotion.SmootherStep(Mathf.InverseLerp(.94f, 1f, progress));
            int value = Mathf.RoundToInt(progress * 100);
            if (lastPercent != value) { lastPercent = value; percent.text = value + "%"; }
            int index = progress < .24f ? 0 : progress < .50f ? 1 : progress < .85f ? 2 : 3;
            if (lastStage != index) { lastStage = index; stage.text = Stages[index]; }
        }

        public void Hide() { if (root != null) root.gameObject.SetActive(false); }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static TextMeshProUGUI Label(string name, Transform parent, string value, TMP_FontAsset font, float size,
            Vector2 center, Vector2 fraction, Color color)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = center - fraction * .5f; rect.anchorMax = center + fraction * .5f;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size; text.color = color; text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        void OnDestroy()
        {
            if (root == null) return;
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
        }
    }
}
