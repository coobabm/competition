using System;
using DesktopArt = LingGuangV05.Desktop.Media.DesktopMedia;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Bodu
{
    /// <summary>
    /// 摆渡 (design v1.1 §3, §8 E-2, §10.2 #7 #9): the 2016 search engine. Front page with this month's news and the
    /// shop's new arrivals; search over news and forum threads. Searching the long number opens the garbled page
    /// the SI typed in the prologue: at stage 5 it decodes line by line with the translation accuracy, a faint
    /// countdown runs to New Year's Eve, and after the final test the AI reads the SI's letter from it. 「三体」 is a
    /// login page that lets in only those who keep trying (BoduView.Santi.cs). Lives in the desktop's spare native
    /// Widget Library window.
    /// </summary>
    public sealed partial class BoduView : MonoBehaviour
    {
        public const string NativeWindow = "Widget Library";
        static readonly Color Blue = new Color32(51, 102, 204, 255), Ink = new Color32(34, 34, 34, 255), Muted = new Color32(120, 120, 120, 255), Link = new Color32(26, 13, 171, 255);

        WindowManager window;
        TMP_FontAsset font;
        RectTransform root, body;
        TMP_InputField search;
        ScrollRect scroll;
        RectTransform content;
        readonly List<(TMP_Text label, LayoutElement layout, float width)> newsRows = new List<(TMP_Text, LayoutElement, float)>();
        string page = "home", query = "";
        string signature = "";
        float nextRefresh;
        int letterShown;
        bool dead;
        float nextLetterLine;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static BoduView Install()
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == NativeWindow);
            var holder = window != null ? window.transform.Find("Container/Content") : null;
            if (holder == null) return null;
            var view = holder.gameObject.GetComponent<BoduView>() ?? holder.gameObject.AddComponent<BoduView>();
            view.window = window;
            view.Rename();
            GameText.Changed += view.Rename;
            return view;
        }

        void OnDestroy() { GameText.Changed -= Rename; }

        void Rename()
        {
            if (window == null) return;
            string name = Lang.T("摆渡");
            foreach (var title in window.GetComponentsInChildren<TMP_Text>(true)) if (title.name == "Aero Window Title") title.text = name;
            var icon = GameObject.Find("Desktop List/" + NativeWindow);
            var tex = Resources.Load<Texture2D>("LingGuangV05/Forum/bodu_icon");
            Sprite sprite = tex != null ? Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f)) : Icon();
            if (icon != null) Relabel(icon, name, sprite);
            if (window.taskbarButton != null) Relabel(window.taskbarButton.gameObject, name, sprite);
        }

        static Sprite icon;
        static Sprite Icon()
        {
            if (icon != null) return icon;
            // A paw-free blue "摆" on white: drawn once at runtime.
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                bool inside = (x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f) < 30 * 30;
                tex.SetPixel(x, y, inside ? new Color32(51, 102, 204, 255) : new Color(0, 0, 0, 0));
            }
            tex.Apply();
            icon = Sprite.Create(tex, new UnityEngine.Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
            icon.hideFlags = HideFlags.DontSave;
            return icon;
        }

        static void Relabel(GameObject entry, string name, Sprite sprite)
        {
            foreach (var c in entry.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string type = c.GetType().Name;
                if (type == "DesktopLocalizedText" || type == "AppElement" || type == "LocalizedObject") c.enabled = false;
            }
            var bm = entry.GetComponent<ButtonManager>();
            if (bm != null) { bm.buttonText = name; if (sprite != null) bm.buttonIcon = sprite; bm.UpdateUI(); }
            foreach (var t in entry.GetComponentsInChildren<TMP_Text>(true)) if (t.name == "Title" || t.name == "Text") t.text = name;
            if (sprite != null) foreach (var img in entry.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") img.sprite = sprite;
        }

        void Start()
        {
            font = PrologueDesk.CjkFont();
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            root = PrologueDesk.Rect("Bodu", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Color.white);
            UiFitScale.Attach(root, window, new Vector2(1266, 763));
            var top = PrologueDesk.Rect("Top", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -64), Vector2.zero);
            PrologueDesk.Fill(top, new Color32(245, 245, 245, 255));
            var logo = Text(PrologueDesk.Rect("Logo", top, Vector2.zero, new Vector2(0, 1), new Vector2(18, 0), new Vector2(130, 0)), "", 30, Blue, TextAlignmentOptions.MidlineLeft);
            logo.fontStyle = FontStyles.Bold; logo.name = "Logo";
            var box = PrologueDesk.Rect("Search", top, new Vector2(0, 0), new Vector2(1, 1), new Vector2(140, 12), new Vector2(-140, -12));
            PrologueDesk.Fill(box, Color.white);
            box.gameObject.AddComponent<Outline>().effectColor = new Color32(180, 180, 180, 255);
            var area = PrologueDesk.Rect("Text Area", box, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2));
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(PrologueDesk.Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 17, Muted, TextAlignmentOptions.MidlineLeft);
            var tx = Text(PrologueDesk.Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 17, Ink, TextAlignmentOptions.MidlineLeft);
            tx.richText = false; tx.textWrappingMode = TextWrappingModes.NoWrap;
            search = box.gameObject.AddComponent<TMP_InputField>();
            search.textViewport = area; search.textComponent = tx; search.placeholder = ph; search.fontAsset = font; search.pointSize = 17;
            search.characterLimit = 60; search.lineType = TMP_InputField.LineType.SingleLine; search.richText = false;
            search.onSubmit.AddListener(_ => Search());
            var go = PrologueDesk.Rect("Go", top, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-132, 12), new Vector2(-18, -12));
            var goImg = PrologueDesk.Fill(go, Blue);
            var gb = go.gameObject.AddComponent<Button>(); gb.targetGraphic = goImg; gb.onClick.AddListener(Search);
            Text(PrologueDesk.Rect("Text", go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 17, Color.white, TextAlignmentOptions.Center).name = "GoText";
            body = PrologueDesk.Rect("Body", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -64));
            if (query.Length > 0) search.text = query;
        }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        void Search()
        {
            query = (search.text ?? "").Trim();
            if (LingGuangV05.Desktop.Casino.CasinoDesktop.OpenAddress(query)) return;
            page = query.Length == 0 ? "home" : "search";
            signature = "";
        }

        public void Open(string q)
        {
            if (LingGuangV05.Desktop.Casino.CasinoDesktop.OpenAddress(q)) return;
            if (window != null && !window.isOn) window.OpenWindow();
            if (search != null) search.text = q;
            query = q; page = string.IsNullOrEmpty(q) ? "home" : "search"; signature = "";
        }

        GameState Save() { var rt = FindAnyObjectByType<ChapterOneRuntime>(); return rt != null && rt.Sim != null ? rt.Sim.S : null; }

        void Update()
        {
            if (root == null || window == null || !window.isOn) return;
            var lab = TiebaHub.Lab();
            // The AI reads the letter line by line (§8 E-2).
            if (page == "number" && lab != null && lab.S.letterRead && Time.unscaledTime >= nextLetterLine && letterShown < lab.LetterLines().Count + 1)
            { letterShown++; nextLetterLine = Time.unscaledTime + 2.2f; signature = ""; }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .5f;
            var s = Save();
            string now = page + "|" + query + "|" + GameText.IsEnglish + "|" + (s != null ? GameCalendar.Now(s).ToString("MMdd") : "") + "|" + (lab != null ? lab.S.stage + "|" + lab.GarbleLinesRead + "|" + lab.S.letterRead + "|" + lab.S.ending + "|" + letterShown : "");
            if (now == signature) return;
            signature = now;
            Redraw(s, lab);
        }

        void LateUpdate()
        {
            if (window == null || !window.isOn) return;
            for (int i = 0; i < newsRows.Count; i++)
            {
                var row = newsRows[i];
                if (row.label == null || row.layout == null) continue;
                float width = row.label.rectTransform.rect.width;
                if (width <= 0 || Mathf.Approximately(width, row.width)) continue;
                // Measure only after layout has its real width, and again when the window resizes.
                row.layout.preferredHeight = Mathf.Max(92, row.label.GetPreferredValues(row.label.text, width, 0).y + 8);
                newsRows[i] = (row.label, row.layout, width);
            }
        }

        void Redraw(GameState s, XgSim lab)
        {
            var logo = root.Find("Top/Logo")?.GetComponent<TMP_Text>(); if (logo != null) logo.text = Lang.T("摆渡");
            var goText = root.GetComponentsInChildren<TMP_Text>(true); foreach (var t in goText) if (t.name == "GoText") t.text = Lang.T("摆渡一下");
            ((TMP_Text)search.placeholder).text = Lang.T("搜点什么……");
            newsRows.Clear();
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            NewScroll();
            var today = s != null ? GameCalendar.Now(s).Date : GameCalendar.Start.Date;
            if (page == "home") Home(today);
            else if (page == "number") Number(today, lab);
            else if (page == "santi") Santi();
            else if (page == "santi.game") SantiGame();
            else Results(today, lab);
        }

        void NewScroll()
        {
            var area = PrologueDesk.Rect("Scroll", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = PrologueDesk.Rect("Viewport", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            PrologueDesk.Fill(viewport, new Color(0, 0, 0, 0));
            content = PrologueDesk.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 16, 24); layout.spacing = 12;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
        }

        TMP_Text Line(string text, float size, Color color, Action click = null)
        {
            var rt = PrologueDesk.Rect("Line", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = Text(rt, text, size, color, TextAlignmentOptions.TopLeft);
            if (click != null) { t.raycastTarget = true; var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = t; b.onClick.AddListener(() => click()); }
            return t;
        }

        static string Date(DateTime d) => GameText.IsEnglish ? d.ToString("MMM d", CultureInfo.InvariantCulture) : d.Month + "-" + d.Day;

        static string NewsArt(string text)
        {
            string s = (text ?? "").ToLowerInvariant();
            if (s.Contains("alphago") || s.Contains("围棋")) return "go";
            if (s.Contains("显卡") || s.Contains("gtx") || s.Contains("radeon")) return "gpu";
            if (s.Contains("手机") || s.Contains("iphone") || s.Contains("note 7")) return "phone";
            if (s.Contains("电影") || s.Contains("你的名字") || s.Contains("cinema")) return "cinema";
            if (s.Contains("购物") || s.Contains("双11") || s.Contains("双 11")) return "shopping";
            if (s.Contains("人工智能") || s.Contains("机器人") || s.Contains("deepmind")) return "ai";
            if (s.Contains("航天") || s.Contains("天宫") || s.Contains("太空")) return "space";
            if (s.Contains("游戏") || s.Contains("守望") || s.Contains("skt")) return "esports";
            return "news";
        }

        void NewsRow(string text, string key)
        {
            if (DesktopArt.Picture(key) == null) { Line(text, 18, Ink); return; }
            var row = PrologueDesk.Rect("NewsRow", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var label = Text(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(128, 4), new Vector2(0, -4)), text, 18, Ink, TextAlignmentOptions.TopLeft);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 92;
            newsRows.Add((label, layout, -1));
            var picture = PrologueDesk.Rect("Illustration", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -68), new Vector2(108, 0));
            DesktopArt.Paint(picture, key);
            Text(PrologueDesk.Rect("Caption", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -87), new Vector2(108, -69)), Lang.T("配图"), 11, Muted, TextAlignmentOptions.Center);
        }

        void Home(DateTime today)
        {
            if (DesktopArt.Picture("news") != null)
            {
                var banner = PrologueDesk.Rect("NewsBanner", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                banner.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
                var art = PrologueDesk.Rect("Illustration", banner, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(136, 0));
                DesktopArt.Paint(art, "news");
                Text(PrologueDesk.Rect("Caption", banner, Vector2.zero, Vector2.one, new Vector2(156, 0), Vector2.zero), Lang.T("看见世界的每一天\n<size=12>配图 · 非新闻现场照片</size>"), 21, Blue, TextAlignmentOptions.MidlineLeft);
            }
            Line("<b>" + Lang.T("摆渡新闻") + "</b>  <size=14><color=#787878>" + today.ToString(GameText.IsEnglish ? "MMMM d, yyyy" : "yyyy 年 M 月 d 日", CultureInfo.InvariantCulture) + "</color></size>", 22, Ink);
            int n = 0;
            foreach (var e in EraContent.Events.Visible(EraEvents.News, today))
            {
                if (n++ >= 10) break;
                NewsRow("<color=#1A0DAB>" + EraContent.Title(e) + "</color>" + (e.text.Length > 0 ? "\n<size=14><color=#555555>" + EraContent.Text(e) + "</color></size>" : "") + "\n<size=12><color=#787878>" + Date(e.date) + "</color></size>", NewsArt(e.title + " " + e.text));
            }
            var shop = EraContent.Events.Visible(EraEvents.Taobao, today);
            if (shop.Count > 0)
            {
                Line("<b>" + T(AppNames.ShopZh + " · 新品上架", AppNames.ShopEn + " · new arrivals") + "</b>", 20, Ink);
                for (int i = 0; i < Math.Min(5, shop.Count); i++)
                    Line(EraContent.Title(shop[i]) + "  <color=#E06000>¥" + shop[i].price.ToString("0", CultureInfo.InvariantCulture) + "</color>  <size=13><color=#787878>" + EraContent.Text(shop[i]) + T("（在「" + AppNames.ShopZh + "」下单）", " (order on " + AppNames.ShopEn + ")") + "</color></size>", 16, Ink);
            }
        }

        static string Digits(string q) { var sb = new StringBuilder(); foreach (char c in q) if (char.IsDigit(c)) sb.Append(c); return sb.ToString(); }

        void Results(DateTime today, XgSim lab)
        {
            if (Digits(query) == Prologue.LongNumber)
            {
                // §8 E-5 (E2): after "谢谢你。" the page is gone; a later visit finds a 404.
                dead = lab != null && lab.S.ending.Length > 0 && !(lab.S.ending == "E1" || lab.S.ending == "E3-1");
                page = "number"; letterShown = 0; signature = ""; Number(today, lab); return;
            }
            if (query.Contains("三体") || query.IndexOf("three-body", StringComparison.OrdinalIgnoreCase) >= 0 || query.IndexOf("three body", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Line("<color=#1A0DAB><u>" + Lang.T("三体 · 游戏登录") + "</u></color>\n<size=13><color=#787878>" + Lang.T("需要 V 装备与邀请码。") + "</color></size>", 18, Ink, () => { page = "santi"; signature = ""; });
            }
            int found = 0;
            foreach (var e in EraContent.Events.Visible(EraEvents.News, today))
                if (Matches(EraContent.Title(e) + EraContent.Text(e)) && found++ < 10) NewsRow("<color=#1A0DAB>" + EraContent.Title(e) + "</color>\n<size=12><color=#787878>" + Lang.T("摆渡新闻") + " · " + Date(e.date) + "</color></size>", NewsArt(e.title + " " + e.text));
            var hub = TiebaHub.Instance;
            if (hub != null)
                foreach (var t in hub.Library.Threads)
                    if (Core.Forum.ForumLibrary.Visible(t, hub.Context()) && Matches(T(t.title, t.titleEn)) && found++ < 16)
                    {
                        var thread = t;
                        Line("<color=#1A0DAB>" + T(t.title, t.titleEn) + "</color>\n<size=12><color=#787878>" + T("摆渡贴吧", "Tieba") + " · " + Date(t.date) + "</color></size>", 18, Ink, () => hub.View?.Open("thread", thread.id));
                    }
            if (found == 0 && !query.Contains("三体")) Line(T("很抱歉，没有找到与「" + query + "」相关的网页。", "No pages found for \"" + query + "\"."), 17, Muted);
        }

        bool Matches(string text) => query.Length > 0 && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>The page behind the long number (the prologue's garbled page).</summary>
        void Number(DateTime today, XgSim lab)
        {
            var rng = new System.Random(Prologue.LongNumber.GetHashCode());
            const string pool = "锟斤拷烫屯ãâ€œ¤§¶ÿþ鎴戠殑鏄剧崱鍦ㄥ摢閲▒░▓█";
            var garble = new StringBuilder();
            for (int line = 0; line < 8; line++) { for (int i = 0; i < 26 + rng.Next(14); i++) garble.Append(pool[rng.Next(pool.Length)]); garble.Append('\n'); }
            if (dead)
            {
                Line("<size=60><b>404</b></size>\n" + Lang.T("页面不存在。"), 18, Muted);
                return;
            }
            Line("<color=#909090>" + garble + "</color>", 16, Muted);
            if (lab != null && lab.S.letterRead)
            {
                // §8 E-2: it reads the rest out loud, one line at a time.
                var lines = lab.LetterLines();
                var sb = new StringBuilder("<b>" + Lang.T("它读给你听：") + "</b>\n");
                for (int i = 0; i < Math.Min(letterShown, lines.Count); i++) sb.Append(lines[i]).Append('\n');
                if (lab.S.ending.Length > 0 && letterShown > lines.Count) sb.Append("\n<b>").Append(lab.LetterLastLine()).Append("</b>");
                Line(sb.ToString(), 18, Ink);
                if (letterShown > lines.Count) Line("<i><color=#787878>" + lab.LetterCaption() + "</color></i>", 15, Muted);
            }
            else if (lab != null && lab.LetterReady)
            {
                Line("<color=#1A0DAB><u>" + Lang.T("【让它读】") + "</u></color>", 18, Ink, () => { if (lab.ReadLetter()) { letterShown = 0; nextLetterLine = 0; signature = ""; } });
            }
            else if (lab != null && lab.GarbleLinesRead > 0)
            {
                var sb = new StringBuilder("<b>" + Lang.T("它试着翻译了一下：") + "</b>\n");
                for (int i = 0; i < lab.GarbleLinesRead; i++) sb.Append(lab.GarbleLine(i)).Append('\n');
                if (lab.GarbleLinesRead < 4) sb.Append("<color=#909090>").Append(Lang.T("（剩下的还读不出来。翻译再准一点。）")).Append("</color>");
                Line(sb.ToString(), 17, Ink);
            }
            // §10.2 #7: a faint countdown, one less every day, zero on New Year's Eve.
            int days = Math.Max(0, (int)(GameCalendar.Ending - today).TotalDays);
            Line("<align=right><color=#F2F2F2>" + days + "</color></align>", 12, Muted);
        }
    }
}
