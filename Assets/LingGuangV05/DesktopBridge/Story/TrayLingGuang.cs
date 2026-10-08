using LingGuangV05.Desktop.LLM;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// A notification-area chip left of the 2016 tray apps: a green dot and 「灵光 就绪」 while the local model is ready
    /// (<see cref="LocalLlm.Ready"/>). Hidden while the model is off, starting or failed, and until 灵光.exe is installed
    /// (it must not give the story away during the prologue).
    /// </summary>
    public sealed class TrayLingGuang : MonoBehaviour
    {
        static string T(string zh, string en) => GameText.T(zh, en);

        CanvasGroup group;
        TMP_Text label;
        ChapterOneRuntime runtime;
        float next;

        public static void Install()
        {
            var taskbar = GameObject.Find("Taskbar");
            if (taskbar == null || taskbar.transform.Find("Tray LingGuang") != null) return;
            var area = taskbar.transform.Find("Aero Notification Area") as RectTransform;
            float right = area != null ? area.rect.width : 242;
            // TrayApps takes the 160 px left of the notification area; this sits just left of them.
            var chip = PrologueDesk.Rect("Tray LingGuang", taskbar.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-right - 262, 0), new Vector2(-right - 166, 0));
            var c = chip.gameObject.AddComponent<TrayLingGuang>();
            c.group = chip.gameObject.AddComponent<CanvasGroup>();
            c.group.alpha = 0; c.group.blocksRaycasts = false;
            var text = PrologueDesk.Rect("Label", chip, Vector2.zero, Vector2.one, new Vector2(2, 0), Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = PrologueDesk.CjkFont(); text.fontSize = 13; text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
            c.label = text;
            PrologueDesk.Fill(chip, new Color(0, 0, 0, 0));
            UiTip.Add(chip, "本机模型已就绪：灵光说的话由它生成。", "The local model is ready: 灵光's words come from it.");
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var llm = LocalLlm.Instance;
            bool installed = runtime != null && runtime.Sim != null && runtime.Sim.AppInstalled && !runtime.Sim.InPrologue;
            bool show = installed && llm != null && llm.Ready;
            group.alpha = show ? 1 : 0;
            group.blocksRaycasts = show;
            if (show) label.text = "<color=#50DC78>●</color> " + T("灵光 就绪", "灵光 ready");
        }
    }
}
