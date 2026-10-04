using System;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The 2016 notification-area crowd (design v1.1 §3): 酷狗, 网易云音乐, 暴风影音, TGP and 有道词典 sit next to
    /// the clock. Hovering names them; clicking makes each say something of its time as a tray popup.
    /// </summary>
    public sealed class TrayApps : MonoBehaviour
    {
        static string T(string zh, string en) => GameText.T(zh, en);

        struct App { public string mark, name, nameEn; public Color color; public Func<string> say; }

        public static void Install()
        {
            var taskbar = GameObject.Find("Taskbar");
            if (taskbar == null || taskbar.transform.Find("Tray Apps 2016") != null) return;
            var area = taskbar.transform.Find("Aero Notification Area") as RectTransform;
            float right = area != null ? area.rect.width : 242;
            var row = PrologueDesk.Rect("Tray Apps 2016", taskbar.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-right - 160, 0), new Vector2(-right - 4, 0));
            row.gameObject.AddComponent<TrayApps>();
            var apps = new[]
            {
                new App { mark = "酷", name = "酷狗音乐", nameEn = "KuGou Music", color = new Color32(30, 130, 230, 255), say = () => T("猜你喜欢：《告白气球》《演员》《小幸运》……", "You may like: Love Confession, Actor, Little Happiness…") },
                new App { mark = "云", name = "网易云音乐", nameEn = "NetEase Cloud Music", color = new Color32(200, 30, 40, 255), say = () => T("每日歌曲推荐已更新。今天的乐评：「我很好，就是有点想你。」", "Daily picks updated. Today's top comment: \"I'm fine, just missing you a bit.\"") },
                new App { mark = "暴", name = "暴风影音", nameEn = "Baofeng Player", color = new Color32(40, 60, 90, 255), say = () => T("左眼键：一键开启高清增强。", "Left-eye key: one click for HD enhancement.") },
                new App { mark = "T", name = "TGP · 腾讯游戏平台", nameEn = "TGP · Tencent Games", color = new Color32(25, 25, 30, 255), say = () => T("你的好友「老周」正在玩：守望先锋。", "Your friend Zhou is playing: Overwatch.") },
                new App { mark = "有", name = "有道词典", nameEn = "Youdao Dictionary", color = new Color32(220, 40, 40, 255), say = () => T("划词翻译已开启。今日单词：attention /əˈtenʃn/ n. 注意", "Select-to-translate is on. Word of the day: attention /əˈtenʃn/ n. 注意") },
            };
            for (int i = 0; i < apps.Length; i++)
            {
                var app = apps[i];
                float x = 6 + i * 31;
                var icon = PrologueDesk.Rect(app.name, row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, -12), new Vector2(x + 24, 12));
                var img = PrologueDesk.Fill(icon, app.color);
                var b = icon.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                b.onClick.AddListener(() => PrologueDirector.Desk?.Popup(T(app.name, app.nameEn), app.say(), 6));
                var t = PrologueDesk.Rect("Mark", icon, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
                t.font = PrologueDesk.CjkFont(); t.text = "<b>" + app.mark + "</b>"; t.fontSize = 15; t.color = Color.white; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
                UiTip.Add(icon, app.name, app.nameEn);
            }
        }
    }
}
