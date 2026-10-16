using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Visible output ports follow the same stage clamp as free AI conversation.</summary>
    public sealed class XgWallpaperOutputs : MonoBehaviour
    {
        XingGuangController controller;
        readonly RectTransform[] ports = new RectTransform[4];
        readonly TMP_Text[] labels = new TMP_Text[4];
        int stage;
        bool english;
        public int Stage => stage;
        public int PortCount => stage == 1 ? 2 : stage == 2 ? 4 : 1;
        public string FormLabel => Form(stage);

        public static string Form(int value)
        {
            switch (XgSpeechPolicy.Stage(value))
            {
                case 1: return GameText.T("是/否", "Yes/No");
                case 2: return GameText.T("多选一", "Pick one");
                case 3: return GameText.T("看图说词", "Picture to word");
                case 4: return GameText.T("短句", "Short sentences");
                case 5: return GameText.T("长记忆·翻译", "Memory · translation");
                default: return GameText.T("全部放开", "Everything");
            }
        }

        public static void Install(XingGuangController controller, RectTransform root)
        {
            var view = root.gameObject.AddComponent<XgWallpaperOutputs>();
            view.controller = controller;
            for (int i = 0; i < view.ports.Length; i++)
            {
                var port = PrologueDesk.Rect("Port " + i, root, Vector2.one * .5f, Vector2.one * .5f,
                    new Vector2(-32, -22), new Vector2(150, 22));
                view.ports[i] = port;
                for (int ring = 3; ring >= 0; ring--)
                {
                    float radius = 6 + ring * 8;
                    var dot = PrologueDesk.Rect("Glow" + ring, port, new Vector2(0, .5f), new Vector2(0, .5f),
                        new Vector2(32 - radius, -radius), new Vector2(32 + radius, radius)).gameObject.AddComponent<Image>();
                    dot.sprite = PrologueDesk.Circle(); var tint = XgCortexGraphic.N.Teal;
                    dot.color = new Color(tint.r, tint.g, tint.b, ring == 0 ? 1 : .025f + .02f * (3 - ring));
                    dot.raycastTarget = false;
                }
                var label = PrologueDesk.Rect("Form", port, Vector2.zero, Vector2.one, new Vector2(68, 0), Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
                label.font = PrologueDesk.CjkFont(); label.fontSize = 15;
                label.color = new Color32(158, 205, 208, 255); label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap; label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 15;
                label.raycastTarget = false; view.labels[i] = label;
            }
            view.Refresh();
        }

        void Update()
        {
            int current = XgSpeechPolicy.Stage(controller?.Sim?.S.stage ?? 1);
            if (stage != current || english != GameText.IsEnglish) Refresh();
        }

        void Refresh()
        {
            stage = XgSpeechPolicy.Stage(controller?.Sim?.S.stage ?? 1); english = GameText.IsEnglish;
            int count = PortCount;
            for (int i = 0; i < ports.Length; i++)
            {
                ports[i].gameObject.SetActive(i < count);
                ports[i].anchoredPosition = new Vector2(59, ((count - 1) * .5f - i) * 45);
                labels[i].text = stage <= 2 ? i == 0 ? GameText.T("是", "Yes") : i == 1 ? GameText.T("否", "No")
                    : i == 2 ? GameText.T("不确定", "Unsure") : "……" : FormLabel;
            }
        }
    }
}
