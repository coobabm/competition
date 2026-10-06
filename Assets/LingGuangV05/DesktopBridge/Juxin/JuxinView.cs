using System;
using DesktopArt = LingGuangV05.Desktop.Media.DesktopMedia;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Juxin
{
    /// <summary>
    /// 巨信 in the style of the 2016 PC WeChat client: a dark icon bar (聊天, 朋友圈), the chat list with official accounts
    /// folded into 订阅号, and the conversation with left / right bubbles, article cards (「点击阅读原文」), red packets
    /// and the 让 AI 代我回 switch. Replies the AI sent carry a small 「由灵光代回」 mark. Lives in the desktop's spare
    /// native Photo Gallery window, renamed and re-iconed while 巨信 is open (stage 4 on) and hidden before; the scene
    /// and DreamOS assets are not changed, and the original icon comes back if a new game starts.
    /// </summary>
    public sealed class JuxinView : MonoBehaviour
    {
        public const string NativeWindow = "Photo Gallery";
        public const string Folder = "subscriptions";
        static readonly Vector2 Design = new Vector2(1266, 763);
        const float NavWidth = 64, ListWidth = 260, HeaderHeight = 56, InputHeight = 150, RowHeight = 66, MaxBubble = 520;

        static readonly Color NavBg = new Color32(40, 42, 46, 255), ListBg = new Color32(232, 231, 230, 255), ListSelected = new Color32(200, 199, 198, 255);
        static readonly Color ChatBg = new Color32(245, 245, 245, 255), Ink = new Color32(30, 30, 30, 255), Muted = new Color32(150, 150, 150, 255);
        static readonly Color Mine = new Color32(158, 234, 106, 255), Theirs = Color.white, Green = new Color32(26, 173, 25, 255), Link = new Color32(87, 107, 149, 255);
        static readonly Color Packet = new Color32(250, 157, 59, 255), PacketOpen = new Color32(251, 205, 150, 255), Badge = new Color32(240, 65, 52, 255), Line = new Color32(214, 214, 214, 255);

        JuxinHub hub;
        WindowManager window;
        TMP_FontAsset font;
        RectTransform root, list, pane, listContent, chatContent;
        ScrollRect listScroll, chatScroll;
        TMP_InputField input;
        Image navChats, navMoments;
        GameObject iconBadge, taskBadge;
        string page = "chats", thread, signature = "";
        bool inFolder, unlocked, applied;
        float nextRefresh;
        readonly List<(TMP_Text label, string zh, string en)> navLabels = new List<(TMP_Text, string, string)>();

        // The native entry as the scene has it, restored whenever 巨信 is locked again.
        GameObject icon;
        Sprite iconSprite, buttonSprite, taskSprite;
        string buttonText;

        static string T(string zh, string en) => GameText.T(zh, en);
        XgSim Sim => hub != null ? hub.Sim : null;

        public static JuxinView Install(JuxinHub hub)
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include), w => w.name == NativeWindow);
            var content = window != null ? window.transform.Find("Container/Content") : null;
            if (content == null) return null;
            var view = content.gameObject.GetComponent<JuxinView>() ?? content.gameObject.AddComponent<JuxinView>();
            view.hub = hub; view.window = window;
            view.CacheNative();
            GameText.Changed += view.OnLanguage;
            return view;
        }

        void OnDestroy() { GameText.Changed -= OnLanguage; }

        void OnLanguage() { if (unlocked) Relabel(); signature = ""; }

        // ───────────── the desktop entry ─────────────

        void CacheNative()
        {
            if (icon != null) return;
            icon = GameObject.Find("Desktop List/" + NativeWindow);
            if (icon == null)
            {
                var list = GameObject.Find("Desktop List");
                var t = list != null ? list.transform.Find(NativeWindow) : null;
                if (t != null) icon = t.gameObject;
            }
            if (icon != null)
            {
                foreach (var img in icon.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") { iconSprite = img.sprite; break; }
                var bm = icon.GetComponent<ButtonManager>();
                if (bm != null) { buttonSprite = bm.buttonIcon; buttonText = bm.buttonText; }
            }
            if (window != null && window.taskbarButton != null)
                foreach (var img in window.taskbarButton.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") { taskSprite = img.sprite; break; }
        }

        /// <summary>巨信 is there from stage 4; before that the Photo Gallery entry is hidden and keeps its own look.</summary>
        public void SetUnlocked(bool open)
        {
            if (applied && open == unlocked) return;
            applied = true; unlocked = open;
            if (open) Relabel();
            else
            {
                Restore();
                if (window != null && window.isOn) window.CloseWindow();
            }
            if (icon != null) icon.SetActive(open);
            if (window != null && window.taskbarButton != null) window.taskbarButton.gameObject.SetActive(open);
        }

        void Relabel()
        {
            if (window == null) return;
            string name = Lang.T("巨信");
            foreach (var title in window.GetComponentsInChildren<TMP_Text>(true)) if (title.name == "Aero Window Title") title.text = name;
            var sprite = AppIcon();
            if (icon != null) { Relabel(icon, name, sprite, true); iconBadge = iconBadge ?? MakeBadge(icon.transform as RectTransform, new Vector2(24, 30)); }
            if (window.taskbarButton != null) { Relabel(window.taskbarButton.gameObject, name, sprite, true); taskBadge = taskBadge ?? MakeBadge(window.taskbarButton.transform as RectTransform, new Vector2(14, 12)); }
        }

        void Restore()
        {
            if (icon != null)
            {
                Relabel(icon, buttonText, iconSprite, false);
                var bm = icon.GetComponent<ButtonManager>();
                if (bm != null && buttonSprite != null) { bm.buttonIcon = buttonSprite; bm.UpdateUI(); }
            }
            if (window != null && window.taskbarButton != null) Relabel(window.taskbarButton.gameObject, null, taskSprite, false);
            UpdateBadges(false);
        }

        static void Relabel(GameObject entry, string name, Sprite sprite, bool ours)
        {
            foreach (var c in entry.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string type = c.GetType().Name;
                if (type == "DesktopLocalizedText" || type == "AppElement" || type == "LocalizedObject") c.enabled = !ours;
            }
            var bm = entry.GetComponent<ButtonManager>();
            if (bm != null && name != null) { bm.buttonText = name; if (sprite != null) bm.buttonIcon = sprite; bm.UpdateUI(); }
            if (name != null) foreach (var t in entry.GetComponentsInChildren<TMP_Text>(true)) if (t.name == "Title" || t.name == "Text") t.text = name;
            if (sprite != null) foreach (var img in entry.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") img.sprite = sprite;
        }

        GameObject MakeBadge(RectTransform parent, Vector2 offset)
        {
            if (parent == null) return null;
            var dot = PrologueDesk.Rect("Juxin Unread", parent, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
            dot.sizeDelta = new Vector2(18, 18); dot.anchoredPosition = new Vector2(offset.x, -offset.y);
            var img = PrologueDesk.Fill(dot, Badge, false);
            img.sprite = PrologueDesk.Circle();
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        public void UpdateBadges(bool unread)
        {
            unread &= unlocked;
            if (iconBadge != null && iconBadge.activeSelf != unread) iconBadge.SetActive(unread);
            if (taskBadge != null && taskBadge.activeSelf != unread) taskBadge.SetActive(unread);
        }

        static Sprite appIcon;

        /// <summary>A green rounded square with two white speech bubbles, drawn once at runtime.</summary>
        static Sprite AppIcon()
        {
            if (appIcon != null) return appIcon;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            var green = new Color32(68, 181, 73, 255);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Rounded square, radius 14.
                    float dx = Mathf.Max(0, Mathf.Max(14 - x, x - (n - 15))), dy = Mathf.Max(0, Mathf.Max(14 - y, y - (n - 15)));
                    Color c = dx * dx + dy * dy <= 14 * 14 ? (Color)green : clear;
                    // Big bubble upper left, small bubble lower right (y grows upwards in a texture).
                    bool big = Ellipse(x, y, 26, 36, 17, 14), small = Ellipse(x, y, 41, 25, 14, 11.5f);
                    bool bigTail = x > 15 && x < 22 && y > 18 && y < 26 && x - 15 > (26 - y) * .6f;
                    if (big || small || bigTail) c = Color.white;
                    if (small && big) c = green; // a thin gap where the bubbles overlap
                    if (Ellipse(x, y, 41, 25, 12.5f, 10) && !big) c = Color.white;
                    if (Ellipse(x, y, 20, 38, 2.2f, 2.2f) || Ellipse(x, y, 31, 38, 2.2f, 2.2f) || Ellipse(x, y, 37, 26, 1.8f, 1.8f) || Ellipse(x, y, 45, 26, 1.8f, 1.8f)) c = green;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            appIcon = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
            appIcon.hideFlags = HideFlags.DontSave;
            return appIcon;
        }

        static bool Ellipse(int x, int y, float cx, float cy, float rx, float ry) { float a = (x - cx) / rx, b = (y - cy) / ry; return a * a + b * b <= 1; }

        // ───────────── layout ─────────────

        void Start()
        {
            font = PrologueDesk.CjkFont();
            foreach (Transform child in transform) child.gameObject.SetActive(false); // the native photo gallery
            root = PrologueDesk.Rect("Juxin", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, ChatBg);
            UiFitScale.Attach(root, window, Design);

            var nav = PrologueDesk.Rect("Nav", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(NavWidth, 0));
            PrologueDesk.Fill(nav, NavBg);
            var me = PrologueDesk.Rect("Me", nav, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-20, -62), new Vector2(20, -22));
            PrologueDesk.Fill(me, new Color32(90, 140, 210, 255));
            Portrait(me, "me", T("我", "Me"));
            navChats = NavButton(nav, 0, "聊", "Chat", () => { page = "chats"; signature = ""; });
            navMoments = NavButton(nav, 1, "圈", "Feed", () => { page = "moments"; signature = ""; });
            UiTip.Add(navChats, "聊天：甲方、朋友、家族群；公众号收在「订阅号」里。", "Chats: clients, friends, the family group. Official accounts are folded into Subscriptions.");
            UiTip.Add(navMoments, "朋友圈：朋友们 2016 年的日常。可以点赞。", "Moments: your friends' 2016. You can like posts.");

            list = PrologueDesk.Rect("List", root, Vector2.zero, new Vector2(0, 1), new Vector2(NavWidth, 0), new Vector2(NavWidth + ListWidth, 0));
            PrologueDesk.Fill(list, ListBg);
            var search = PrologueDesk.Rect("Search", list, new Vector2(0, 1), Vector2.one, new Vector2(12, -50), new Vector2(-12, -18));
            PrologueDesk.Fill(search, new Color32(219, 217, 216, 255));
            Text(PrologueDesk.Rect("T", search, Vector2.zero, Vector2.one, new Vector2(10, 0), Vector2.zero), "", 14, Muted, TextAlignmentOptions.MidlineLeft);
            listScroll = NewScroll(list, new Vector2(0, 0), new Vector2(0, -60), out listContent, 0, 0);

            pane = PrologueDesk.Rect("Pane", root, Vector2.zero, Vector2.one, new Vector2(NavWidth + ListWidth, 0), Vector2.zero);
            var divider = PrologueDesk.Rect("Divider", root, Vector2.zero, new Vector2(0, 1), new Vector2(NavWidth + ListWidth - 1, 0), new Vector2(NavWidth + ListWidth, 0));
            PrologueDesk.Fill(divider, Line, false);
            Redraw();
        }

        Image NavButton(RectTransform nav, int index, string zh, string en, Action click)
        {
            var rt = PrologueDesk.Rect("Nav" + index, nav, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-22, -130 - index * 60), new Vector2(22, -86 - index * 60));
            var img = PrologueDesk.Fill(rt, NavBg);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => click());
            var t = Text(PrologueDesk.Rect("T", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 20, Color.white, TextAlignmentOptions.Center);
            navLabels.Add((t, zh, en));
            return img;
        }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        ScrollRect NewScroll(RectTransform parent, Vector2 offMin, Vector2 offMax, out RectTransform content, int padX, float spacing)
        {
            var area = PrologueDesk.Rect("Scroll", parent, Vector2.zero, Vector2.one, offMin, offMax);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = PrologueDesk.Rect("Viewport", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            PrologueDesk.Fill(viewport, new Color(0, 0, 0, 0));
            content = PrologueDesk.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padX, padX, 6, 12); layout.spacing = spacing;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            return scroll;
        }

        RectTransform Row(RectTransform parent, float height, string name = "Row")
        {
            var rt = PrologueDesk.Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            return rt;
        }

        // ───────────── refresh ─────────────

        public void Open(string where = null)
        {
            if (window != null)
            {
                // Explicit visits must restore/focus minimized windows; reuse YY's first-open recovery.
                LingGuangV05.Desktop.XingGuang.XingGuangController.KeepOpenOnFirstStart(window);
                window.OpenWindow();
                LingGuangV05.Desktop.XingGuang.XingGuangController.EnsureShown(hub, window);
            }
            if (where != null) { page = "chats"; thread = where; inFolder = XgSim.JuxinIsSubscription(where); signature = ""; }
        }

        void OnDisable() { if (Sim != null) Sim.JuxinShowing = null; }

        void Update()
        {
            if (root == null || Sim == null || Sim.S.jxThreads == null || Sim.S.jxLikes == null) return;
            if (!LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this)) { Sim.JuxinShowing = null; return; }
            Sim.JuxinShowing = page == "chats" ? thread : null;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .35f;
            string now = Signature();
            if (now != signature) { signature = now; Redraw(); }
        }

        string Signature()
        {
            var s = Sim;
            int total = 0;
            foreach (var t in s.S.jxThreads) total += t.messages.Count * 31 + t.unread + t.mood * 7;
            int opened = 0;
            if (thread != null) { var t = s.JuxinThread(thread); if (t != null) foreach (var m in t.messages) if (m.opened) opened++; }
            return page + "|" + thread + "|" + inFolder + "|" + total + "|" + opened + "|" + s.JuxinAutoReply + "|" + s.JuxinCanAutoReply + "|" + s.JuxinReplying(thread) + "|" + GameText.IsEnglish + "|" + s.S.jxLikes.Count + "|" + s.Today;
        }

        void Redraw()
        {
            if (Sim == null) return;
            if (page == "chats" && thread != null && LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this)) Sim.JuxinMarkRead(thread);
            navChats.color = page == "chats" ? new Color32(70, 72, 78, 255) : NavBg;
            navMoments.color = page == "moments" ? new Color32(70, 72, 78, 255) : NavBg;
            var searchText = list.Find("Search/T")?.GetComponent<TMP_Text>();
            if (searchText != null) searchText.text = Lang.T("搜索");
            foreach (var n in navLabels) { n.label.text = T(n.zh, n.en); n.label.fontSize = GameText.IsEnglish ? 14 : 20; }
            DrawList();
            string draft = input != null ? input.text : "";
            bool focused = input != null && input.isFocused;
            for (int i = pane.childCount - 1; i >= 0; i--) Destroy(pane.GetChild(i).gameObject);
            input = null; chatScroll = null;
            if (page == "moments") DrawMoments();
            else if (thread != null && Sim.JuxinThread(thread) != null) DrawChat(Sim.JuxinThread(thread));
            else DrawEmpty();
            if (input != null) { input.text = draft; if (focused) input.ActivateInputField(); }
            if (chatScroll != null) StartCoroutine(ScrollToEnd(chatScroll));
        }

        System.Collections.IEnumerator ScrollToEnd(ScrollRect scroll)
        {
            yield return null; // the layout has its size after one frame
            if (scroll != null) scroll.verticalNormalizedPosition = 0;
        }

        // ───────────── the list ─────────────

        void DrawList()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);
            if (inFolder)
            {
                var back = Row(listContent, 40, "Back");
                var img = PrologueDesk.Fill(back, ListBg);
                var b = back.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => { inFolder = false; signature = ""; });
                Text(PrologueDesk.Rect("T", back, Vector2.zero, Vector2.one, new Vector2(14, 0), Vector2.zero), Lang.T("‹ 订阅号"), 16, Link, TextAlignmentOptions.MidlineLeft);
                foreach (var t in Sim.JuxinSubscriptions()) ThreadRow(t);
                return;
            }
            var subs = Sim.JuxinSubscriptions();
            var chats = Sim.JuxinChats();
            // 订阅号 sits among the chats by its latest post, like the 2016 client.
            bool folderDrawn = subs.Count == 0;
            foreach (var t in chats)
            {
                if (!folderDrawn && subs[0].last >= t.last) { FolderRow(subs); folderDrawn = true; }
                ThreadRow(t);
            }
            if (!folderDrawn) FolderRow(subs);
        }

        void FolderRow(List<XgJxThread> subs)
        {
            var latest = subs[0];
            var m = latest.messages[latest.messages.Count - 1];
            int unread = Sim.JuxinSubscriptionUnread;
            var row = ListRow(Lang.T("订阅号"), XgSim.JuxinName(latest.id, GameText.IsEnglish) + T("：", ": ") + Preview(m), new Color32(52, 120, 200, 255), unread, false, () => { inFolder = true; signature = ""; }, Clock(m), Folder);
            UiTip.Add(row, "公众号的推送都收在这里：甲方的签约喜报、招募、结算通知，还有摆渡新闻。", "Official accounts post here: clients' launch notices, hiring calls and settlements, plus Bodu News.");
        }

        void ThreadRow(XgJxThread t)
        {
            string name = XgSim.JuxinName(t.id, GameText.IsEnglish);
            var m = t.messages.Count > 0 ? t.messages[t.messages.Count - 1] : null;
            string preview = m == null ? "" : (m.ai ? Lang.T("[代回] ") : "") + (m.whoZh.Length > 0 ? T(m.whoZh, m.whoEn) + T("：", ": ") : "") + Preview(m);
            string id = t.id;
            ListRow(name, preview, AvatarColor(id), t.unread, thread == id && page == "chats", () => { thread = id; page = "chats"; signature = ""; }, m != null ? Clock(m) : "", id);
        }

        Image ListRow(string name, string preview, Color avatar, int unread, bool selected, Action click, string time, string identity)
        {
            var row = Row(listContent, RowHeight);
            var img = PrologueDesk.Fill(row, selected ? ListSelected : ListBg);
            var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => click());
            var colors = b.colors; colors.highlightedColor = new Color(.93f, .93f, .93f, 1); b.colors = colors;
            var av = PrologueDesk.Rect("Avatar", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -21), new Vector2(54, 21));
            PrologueDesk.Fill(av, avatar, false);
            Portrait(av, identity, name);
            var nameText = Text(PrologueDesk.Rect("Name", row, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(64, 0), new Vector2(-58, -8)), name, 16, Ink, TextAlignmentOptions.BottomLeft);
            nameText.textWrappingMode = TextWrappingModes.NoWrap; nameText.overflowMode = TextOverflowModes.Ellipsis;
            var prev = Text(PrologueDesk.Rect("Preview", row, Vector2.zero, new Vector2(1, .5f), new Vector2(64, 8), new Vector2(-12, -2)), Safe(preview), 13, Muted, TextAlignmentOptions.TopLeft);
            prev.textWrappingMode = TextWrappingModes.NoWrap; prev.overflowMode = TextOverflowModes.Ellipsis;
            Text(PrologueDesk.Rect("Time", row, new Vector2(1, .5f), new Vector2(1, 1), new Vector2(-58, 0), new Vector2(-10, -8)), time, 11, Muted, TextAlignmentOptions.BottomRight);
            if (unread > 0)
            {
                var dot = PrologueDesk.Rect("Unread", av, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(10, 10));
                var d = PrologueDesk.Fill(dot, Badge, false); d.sprite = PrologueDesk.Circle();
                Text(PrologueDesk.Rect("N", dot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), unread > 99 ? "…" : unread.ToString(CultureInfo.InvariantCulture), 11, Color.white, TextAlignmentOptions.Center);
            }
            return img;
        }

        static string Preview(XgJxMessage m)
        {
            switch ((XgJxKind)m.kind)
            {
                case XgJxKind.Article: return "[" + Lang.T("链接") + "] " + T(m.titleZh, m.titleEn);
                case XgJxKind.RedPacket: return "[" + T("巨信红包", "Red packet") + "] " + T(m.zh, m.en);
                default: return T(m.zh, m.en).Replace('\n', ' ');
            }
        }

        static string Initial(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            int dot = name.IndexOf('·');
            string s = dot >= 0 && dot + 1 < name.Length ? name.Substring(dot + 1).Trim() : name;
            return s.Length > 0 ? s.Substring(0, 1).ToUpperInvariant() : "?";
        }

        void Portrait(RectTransform holder, string identity, string name)
        {
            Sprite sprite;
            if (identity == Folder || XgSim.JuxinIsSubscription(identity))
                sprite = DesktopArt.Picture(identity != null && identity.Contains("goclub") ? "go" : "news");
            else
            {
                // Keep client identities stable across Chinese/English names and message types.
                string key = identity;
                switch (identity)
                {
                    case "dm.cheque": key = "wang"; break;
                    case "dm.zipcode": key = "liu"; break;
                    case "dm.memetag": key = "xiaolu"; break;
                    case "dm.taobao": key = "afang"; break;
                    case "dm.captcha": case "dm.fakereview": key = "laozhou"; break;
                    case "dm.goclub": case "dm.homework": case "dm.support": key = "liu"; break;
                    case "dm.parking": case "dm.civilexam": case "dm.antifraud": case "dm.sla.courier": key = "wang"; break;
                    case "dm.faceclock": key = "aunt"; break;
                    case "dm.acrostic": case "dm.sla.meme": key = "xiaolu"; break;
                    case "dm.danmaku": key = "ajie"; break;
                    case "dm.clickbait": case "dm.sla.danmu": case "dm.sla.portal": key = "dawei"; break;
                    case "dm.ime": case "dm.sla.homework": key = "cousin"; break;
                    case "dm.subtitle": key = "xiaogang"; break;
                    case "dm.sla.takeout": key = "afang"; break;
                    case "dm.sla.bank": key = "zhou_now"; break;
                    case XgSim.JxFamily: key = "dad"; break;
                }
                sprite = DesktopArt.Avatar(string.IsNullOrEmpty(key) ? name : key);
            }
            if (sprite == null)
            {
                Text(PrologueDesk.Rect("T", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Initial(name), 17, Color.white, TextAlignmentOptions.Center);
                return;
            }
            var image = PrologueDesk.Rect("Portrait", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        }

        static Color AvatarColor(string id)
        {
            switch (id)
            {
                case XgSim.JxTeam: return new Color32(68, 181, 73, 255);
                case XgSim.JxNews: return new Color32(51, 102, 204, 255);
                case XgSim.JxFamily: return new Color32(230, 120, 60, 255);
                case XgSim.JxAjie: return new Color32(70, 70, 90, 255);
                case XgSim.JxXiaogang: return new Color32(40, 150, 200, 255);
                case XgSim.JxCousin: return new Color32(220, 100, 140, 255);
            }
            int h = 0;
            foreach (char c in id) h = h * 31 + c;
            Color.RGBToHSV(new Color32(80, 140, 200, 255), out _, out float s, out float v);
            return Color.HSVToRGB(Mathf.Abs(h % 360) / 360f, s, v);
        }

        static string Safe(string s) => (s ?? "").Replace("<", "‹").Replace(">", "›");

        static string Clock(XgJxMessage m) => (m.minute / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (m.minute % 60).ToString("00", CultureInfo.InvariantCulture);

        static string Day(int yyyymmdd)
        {
            int month = yyyymmdd / 100 % 100, day = yyyymmdd % 100;
            if (month < 1 || month > 12) return "";
            return GameText.IsEnglish ? new DateTime(2016, month, Math.Max(1, Math.Min(28, day))).ToString("MMM ", CultureInfo.InvariantCulture) + day : month + "月" + day + "日";
        }

        // ───────────── a conversation ─────────────

        void DrawEmpty()
        {
            var t = Text(PrologueDesk.Rect("Empty", pane, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "<size=46><color=#C8C8C8><b>" + Lang.T("巨信") + "</b></color></size>\n<color=#B4B4B4>2016</color>", 16, Muted, TextAlignmentOptions.Center);
            t.name = "Empty";
        }

        void DrawChat(XgJxThread t)
        {
            var sim = Sim;
            bool sub = XgSim.JuxinIsSubscription(t.id);
            var header = PrologueDesk.Rect("Header", pane, new Vector2(0, 1), Vector2.one, new Vector2(0, -HeaderHeight), Vector2.zero);
            PrologueDesk.Fill(header, ChatBg);
            var rule = PrologueDesk.Rect("Rule", header, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1));
            PrologueDesk.Fill(rule, Line, false);
            string title = "<b>" + Safe(XgSim.JuxinName(t.id, GameText.IsEnglish)) + "</b>";
            if (XgSim.JuxinIsGroup(t.id)) title += " (4)";
            if (sim.JuxinReplying(t.id)) title += "  <size=13><color=#1AAD19>" + T(AppNames.AiZh + "正在代你输入…", AppNames.AiEn + " is typing for you…") + "</color></size>";
            var portrait = PrologueDesk.Rect("Portrait", header, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, -18), new Vector2(56, 18));
            Portrait(portrait, t.id, XgSim.JuxinName(t.id, GameText.IsEnglish));
            Text(PrologueDesk.Rect("Name", header, Vector2.zero, Vector2.one, new Vector2(68, 0), new Vector2(-280, 0)), title, 19, Ink, TextAlignmentOptions.MidlineLeft);
            string relation = Relation(t);
            if (relation.Length > 0)
            {
                var rel = Text(PrologueDesk.Rect("Relation", header, new Vector2(1, 0), Vector2.one, new Vector2(-300, 0), new Vector2(-20, 0)), relation, 13, Muted, TextAlignmentOptions.MidlineRight);
                rel.raycastTarget = true;
                UiTip.Add(rel, "满意度：按时回消息会涨，晾着不回、说错话会跌。满了甲方会发个辛苦费红包；高价单甲方还会顺手给平台信用分加一点。",
                    "Satisfaction: answering on time raises it; leaving them hanging or a gaffe lowers it. At the top the client sends a small thank-you red packet, and an SLA client adds a point of platform credit.");
            }

            float bottom = sub ? 60 : InputHeight;
            var body = PrologueDesk.Rect("Body", pane, Vector2.zero, Vector2.one, new Vector2(0, bottom), new Vector2(0, -HeaderHeight));
            chatScroll = NewScroll(body, Vector2.zero, Vector2.zero, out chatContent, 18, 6);
            int last = -1;
            double lastAt = double.NegativeInfinity;
            for (int i = 0; i < t.messages.Count; i++)
            {
                var m = t.messages[i];
                if (m.date != last || m.at - lastAt > 300) Stamp(m);
                last = m.date; lastAt = m.at;
                int index = i;
                switch ((XgJxKind)m.kind)
                {
                    case XgJxKind.Article: Article(m); break;
                    case XgJxKind.RedPacket: RedPacket(t, m, index); break;
                    case XgJxKind.Notice: Notice(T(m.zh, m.en)); break;
                    default: Bubble(t, m); break;
                }
            }
            if (sub) { DrawSubscriptionFooter(t); return; }
            DrawInput(t);
        }

        string Relation(XgJxThread t)
        {
            string id = t.id;
            string job = "";
            if (id.StartsWith("dm.", StringComparison.Ordinal) || id == XgSim.JxCousin && Sim.Signed("crossborder"))
            {
                string cid = id == XgSim.JxCousin ? "crossborder" : id.Substring(3);
                var c = XgCatalog.Contract(cid);
                var sla = XgSim.SlaOffer(cid);
                job = c != null ? T(c.job, c.jobEn) : sla != null ? T(sla.job, sla.jobEn) : "";
            }
            if (job.Length == 0) return "";
            var sb = new System.Text.StringBuilder();
            if (job.Length > 0) sb.Append(Lang.T("合作：")).Append(job).Append("   ");
            sb.Append(Lang.T("满意度 "));
            for (int i = -XgSim.JuxinMoodLimit; i < XgSim.JuxinMoodLimit; i++) sb.Append(i < t.mood ? "<color=#1AAD19>■</color>" : "<color=#D0D0D0>■</color>");
            return sb.ToString();
        }

        void Stamp(XgJxMessage m)
        {
            var row = Row(chatContent, 26, "Stamp");
            Text(PrologueDesk.Rect("T", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "<color=#AAAAAA>" + Day(m.date) + " " + Clock(m) + "</color>", 12, Muted, TextAlignmentOptions.Center);
        }

        void Notice(string text)
        {
            var row = Row(chatContent, 28, "Notice");
            var t = Text(PrologueDesk.Rect("T", row, Vector2.zero, Vector2.one, new Vector2(60, 0), new Vector2(-60, 0)), Safe(text), 13, Muted, TextAlignmentOptions.Center);
            float h = t.GetPreferredValues(Safe(text), 600, 0).y + 10;
            row.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(28, h);
        }

        RectTransform Avatar(RectTransform row, bool mine, string name, Color color, string identity)
        {
            var av = PrologueDesk.Rect("Avatar", row, mine ? Vector2.one : new Vector2(0, 1), mine ? Vector2.one : new Vector2(0, 1), mine ? new Vector2(-40, -40) : Vector2.zero, mine ? Vector2.zero : new Vector2(40, 40));
            av.offsetMin = mine ? new Vector2(-40, -40) : new Vector2(0, -40);
            av.offsetMax = mine ? Vector2.zero : new Vector2(40, 0);
            PrologueDesk.Fill(av, color, false);
            Portrait(av, mine ? "me" : identity, name);
            return av;
        }

        void Bubble(XgJxThread t, XgJxMessage m)
        {
            string body = LingGuangV05.Desktop.YY.YYFaceRenderer.FaceTags(Safe(m.mine ? m.zh : T(m.zh, m.en))); // YY faces inline
            bool group = XgSim.JuxinIsGroup(t.id) && !m.mine && m.whoZh.Length > 0;
            string speaker = group ? T(m.whoZh, m.whoEn) : "";
            var row = Row(chatContent, 50, m.mine ? "Mine" : "Theirs");
            var text = Text(PrologueDesk.Rect("Probe", row, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), body, 16, Ink);
            LingGuangV05.Desktop.YY.YYFaceRenderer.Prepare(text);
            Vector2 size = text.GetPreferredValues(body, MaxBubble - 28, 0);
            float w = Mathf.Min(MaxBubble, size.x + 28), h = Mathf.Max(40, size.y + 20);
            float top = group ? 20 : 0;
            float markHeight = m.ai ? 20 : 0;
            row.GetComponent<LayoutElement>().preferredHeight = top + h + markHeight + 6;
            string who = m.mine ? T("我", "Me") : group ? speaker : XgSim.JuxinName(t.id, GameText.IsEnglish);
            Avatar(row, m.mine, who, m.mine ? new Color32(90, 140, 210, 255) : group ? AvatarColor(speaker) : AvatarColor(t.id), group ? m.whoZh : t.id);
            if (group)
                Text(PrologueDesk.Rect("Speaker", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(52, -18), new Vector2(0, 0)), Safe(speaker), 12, Muted, TextAlignmentOptions.TopLeft);
            var bubble = m.mine
                ? PrologueDesk.Rect("Bubble", row, Vector2.one, Vector2.one, new Vector2(-52 - w, -top - h), new Vector2(-52, -top))
                : PrologueDesk.Rect("Bubble", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(52, -top - h), new Vector2(52 + w, -top));
            PrologueDesk.Fill(bubble, m.mine ? Mine : Theirs, m.ai);
            text.rectTransform.SetParent(bubble, false);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(14, 10); text.rectTransform.offsetMax = new Vector2(-14, -10);
            text.alignment = TextAlignmentOptions.TopLeft;
            if (!m.ai) return;
            var mark = Text(PrologueDesk.Rect("AiMark", row, Vector2.one, Vector2.one, new Vector2(-52 - 260, -top - h - 18), new Vector2(-52, -top - h - 2)),
                T("由" + AppNames.AiZh + "代回", "Replied by " + AppNames.AiEn), 11, new Color32(140, 150, 170, 255), TextAlignmentOptions.TopRight);
            mark.raycastTarget = true;
            UiTip.Add(mark, () => T("这条是" + AppNames.AiZh + "替你回的，对方以为是你本人。它偶尔会说错话，记得看一眼。", "Your AI sent this in your name; they think it was you. It sometimes says the wrong thing, so keep an eye on it."));
            UiTip.Add(bubble, () => T("这条是" + AppNames.AiZh + "替你回的。", "Your AI wrote this one."));
        }

        void Article(XgJxMessage m)
        {
            string text = "<b><size=19>" + Safe(T(m.titleZh, m.titleEn)) + "</size></b>\n<size=12><color=#AAAAAA>" + Day(m.date) + "</color></size>\n" +
                "<color=#555555>" + Safe(T(m.zh, m.en)) + "</color>";
            const float width = 560;
            var row = Row(chatContent, 120, "Article");
            var card = PrologueDesk.Rect("Card", row, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-width / 2, 0), new Vector2(width / 2, 0));
            PrologueDesk.Fill(card, Color.white);
            card.gameObject.AddComponent<Outline>().effectColor = new Color32(225, 225, 225, 255);
            bool illustrated = DesktopArt.Picture("news") != null;
            if (illustrated)
            {
                var art = PrologueDesk.Rect("Illustration", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -72), new Vector2(76, -14));
                DesktopArt.Paint(art, "news");
                Text(PrologueDesk.Rect("Caption", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -90), new Vector2(76, -74)), Lang.T("配图"), 10, Muted, TextAlignmentOptions.Center);
            }
            float inset = illustrated ? 92 : 18;
            var t = Text(PrologueDesk.Rect("T", card, Vector2.zero, Vector2.one, new Vector2(inset, 44), new Vector2(-18, -14)), text, 15, Ink);
            float h = Mathf.Max(illustrated ? 144 : 0, t.GetPreferredValues(text, width - inset - 18, 0).y + 14 + 44);
            card.offsetMin = new Vector2(-width / 2, -h);
            row.GetComponent<LayoutElement>().preferredHeight = h + 6;
            var foot = PrologueDesk.Rect("Foot", card, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 38));
            var rule = PrologueDesk.Rect("Rule", foot, new Vector2(0, 1), Vector2.one, new Vector2(18, -1), new Vector2(-18, 0));
            PrologueDesk.Fill(rule, new Color32(235, 235, 235, 255), false);
            var read = Text(PrologueDesk.Rect("Read", foot, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0)), Lang.T("阅读原文"), 14, Link, TextAlignmentOptions.MidlineLeft);
            read.raycastTarget = true;
            UiTip.Add(read, "2016 年的公众号都爱写「点击阅读原文」。……这里没有原文。", "Every 2016 official account says \"Read the original\". ...There is no original here.");
            var qr = PrologueDesk.Rect("QR", foot, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-46, 6), new Vector2(-18, -6));
            PrologueDesk.Fill(qr, new Color32(60, 60, 60, 255));
            UiTip.Add(qr, "长按识别二维码。（鼠标没有长按。）", "Long-press to scan the QR code. (A mouse cannot long-press.)");
        }

        void RedPacket(XgJxThread t, XgJxMessage m, int index)
        {
            bool group = XgSim.JuxinIsGroup(t.id) && m.whoZh.Length > 0;
            string speaker = group ? T(m.whoZh, m.whoEn) : "";
            float top = group ? 20 : 0;
            const float w = 250, h = 96;
            var row = Row(chatContent, top + h + 6, "Packet");
            Avatar(row, false, group ? speaker : XgSim.JuxinName(t.id, GameText.IsEnglish), group ? AvatarColor(speaker) : AvatarColor(t.id), group ? m.whoZh : t.id);
            if (group) Text(PrologueDesk.Rect("Speaker", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(52, -18), Vector2.zero), Safe(speaker), 12, Muted, TextAlignmentOptions.TopLeft);
            var card = PrologueDesk.Rect("Card", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(52, -top - h), new Vector2(52 + w, -top));
            var img = PrologueDesk.Fill(card, m.opened ? PacketOpen : Packet);
            string state = !m.opened ? Lang.T("领取红包") : m.got > 0 ? Lang.T("已领取 ¥") + m.got.ToString("0.00", CultureInfo.InvariantCulture) : Lang.T("手慢了，红包派完了");
            Text(PrologueDesk.Rect("T", card, Vector2.zero, Vector2.one, new Vector2(16, 28), new Vector2(-12, -10)), "<b>" + Safe(T(m.zh, m.en)) + "</b>\n<size=13>" + state + "</size>", 16, Color.white, TextAlignmentOptions.TopLeft);
            var foot = PrologueDesk.Rect("Foot", card, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 24));
            PrologueDesk.Fill(foot, Color.white, false);
            Text(PrologueDesk.Rect("T", foot, Vector2.zero, Vector2.one, new Vector2(12, 0), Vector2.zero), T("巨信红包", "Juxin red packet"), 11, Muted, TextAlignmentOptions.MidlineLeft);
            if (!m.opened)
            {
                var b = card.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                string id = t.id;
                b.onClick.AddListener(() =>
                {
                    double got = Sim.OpenRedPacket(id, index, hub.Controller != null ? hub.Controller.Host : null);
                    if (got >= 0) hub.Changed();
                    signature = "";
                });
            }
            UiTip.Add(card, group ? "群红包拼手速：发出 5 分钟后就被抢光了。" : "甲方的辛苦费：一点心意，钱不多。",
                group ? "Group red packets go to the quick: after 5 minutes they're gone." : "A small thank-you from the client.");
        }

        void DrawSubscriptionFooter(XgJxThread t)
        {
            var bar = PrologueDesk.Rect("Menu", pane, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 60));
            PrologueDesk.Fill(bar, Color.white);
            var rule = PrologueDesk.Rect("Rule", bar, new Vector2(0, 1), Vector2.one, new Vector2(0, -1), Vector2.zero);
            PrologueDesk.Fill(rule, Line, false);
            string[] zh = { "历史消息", "合作咨询", "关于我们" }, en = { "Past posts", "Business", "About us" };
            for (int i = 0; i < 3; i++)
            {
                var cell = PrologueDesk.Rect("Item" + i, bar, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 1), Vector2.zero, Vector2.zero);
                var img = PrologueDesk.Fill(cell, Color.white);
                var b = cell.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                string id = t.id, word = T(zh[i], en[i]);
                b.onClick.AddListener(() => { Sim.JuxinSend(id, word); hub.Changed(); signature = ""; });
                Text(PrologueDesk.Rect("T", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "≡ " + word, 15, Ink, TextAlignmentOptions.Center);
            }
        }

        void DrawInput(XgJxThread t)
        {
            var sim = Sim;
            var bar = PrologueDesk.Rect("Input", pane, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, InputHeight));
            PrologueDesk.Fill(bar, Color.white);
            var rule = PrologueDesk.Rect("Rule", bar, new Vector2(0, 1), Vector2.one, new Vector2(0, -1), Vector2.zero);
            PrologueDesk.Fill(rule, Line, false);

            // 让 AI 代我回 (stage 5 on).
            var toggle = PrologueDesk.Rect("AutoReply", bar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -40), new Vector2(250, -8));
            bool can = sim.JuxinCanAutoReply, on = sim.JuxinAutoReply;
            var timg = PrologueDesk.Fill(toggle, on ? Green : can ? new Color32(236, 236, 236, 255) : new Color32(245, 245, 245, 255));
            var tb = toggle.gameObject.AddComponent<Button>(); tb.targetGraphic = timg; tb.interactable = can;
            tb.onClick.AddListener(() => { sim.SetJuxinAutoReply(!sim.JuxinAutoReply); hub.Changed(); signature = ""; });
            string label = Lang.T("让 AI 代我回：") + (on ? Lang.T("开") : Lang.T("关")) + (can ? "" : Lang.T("（阶段 5）"));
            Text(PrologueDesk.Rect("T", toggle, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-8, 0)), label, 14, on ? Color.white : can ? Ink : Muted, TextAlignmentOptions.MidlineLeft);
            UiTip.Add(toggle, () => can
                ? T("打开后，" + AppNames.AiZh + "会用和「对话」页一样的人格替你回甲方和朋友的消息（不回公众号）。对方以为是你。它偶尔会说错话。",
                    "When on, " + AppNames.AiEn + " answers clients and friends for you, with the same personality as the Chat page (not official accounts). They think it's you. Sometimes it says the wrong thing.")
                : Lang.T("阶段 5 解锁：它要先学会长句子和记事。"));

            var box = PrologueDesk.Rect("Field", bar, Vector2.zero, Vector2.one, new Vector2(14, 52), new Vector2(-14, -46));
            PrologueDesk.Fill(box, Color.white);
            var area = PrologueDesk.Rect("Text Area", box, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(PrologueDesk.Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), on ? Lang.T("AI 正在替你回消息，你也可以自己打字。") : Lang.T("输入消息……"), 15, Muted, TextAlignmentOptions.TopLeft);
            var textLabel = Text(PrologueDesk.Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, Ink, TextAlignmentOptions.TopLeft);
            textLabel.richText = false;
            input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area; input.textComponent = textLabel; input.placeholder = placeholder; input.fontAsset = font; input.pointSize = 15;
            input.characterLimit = 200; input.lineType = TMP_InputField.LineType.SingleLine; input.richText = false;
            string id = t.id;
            input.onSubmit.AddListener(_ => SendTyped(id));
            var send = PrologueDesk.Rect("Send", bar, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-118, 10), new Vector2(-14, 42));
            var simg = PrologueDesk.Fill(send, new Color32(245, 245, 245, 255));
            send.gameObject.AddComponent<Outline>().effectColor = new Color32(210, 210, 210, 255);
            var sb = send.gameObject.AddComponent<Button>(); sb.targetGraphic = simg; sb.onClick.AddListener(() => SendTyped(id));
            Text(PrologueDesk.Rect("T", send, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("发送(S)", "Send (S)"), 14, Ink, TextAlignmentOptions.Center);
        }

        void SendTyped(string id)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.text)) return;
            string text = NativeLaoZhouReplies.NormalizeInput(input.text);
            input.text = "";
            if (Sim.JuxinSend(id, text)) hub.Changed();
            signature = "";
        }

        // ───────────── 朋友圈 ─────────────

        static string MomentArt(string id)
        {
            switch (id)
            {
                case "m.aj1080": return "gpu";
                case "m.czagent": return "phone";
                case "m.xgholiday": return "books";
                case "m.ajciv": case "m.xgskt": case "m.ajsteam": return "esports";
                case "m.cz11": return "shopping";
                case "m.xgname": return "cinema";
                default: return null; // Not every status is a photo post.
            }
        }

        void DrawMoments()
        {
            var sim = Sim;
            NewScroll(pane, Vector2.zero, Vector2.zero, out var content, 0, 0);
            var cover = Row(content, 200, "Cover");
            PrologueDesk.Fill(cover, new Color32(52, 74, 98, 255), false);
            if (DesktopArt.Paint(cover, "desk") != null)
                PrologueDesk.Fill(PrologueDesk.Rect("Shade", cover, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0, .04f, .1f, .62f), false);
            Text(PrologueDesk.Rect("T", cover, Vector2.zero, Vector2.one, new Vector2(30, 20), new Vector2(-30, -20)), "<size=30><b>" + Lang.T("朋友圈") + "</b></size>\n<color=#C0CCD8>" + Lang.T("2016 · 只看朋友们的") + "</color>", 16, Color.white, TextAlignmentOptions.BottomLeft);
            var posts = sim.JuxinMomentsVisible();
            if (posts.Count == 0) { var none = Row(content, 60); Text(PrologueDesk.Rect("T", none, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Lang.T("还没有动态。"), 15, Muted, TextAlignmentOptions.Center); return; }
            foreach (var p in posts)
            {
                string name = XgSim.JuxinName(p.who, GameText.IsEnglish);
                string text = Safe(T(p.zh, p.en));
                var row = Row(content, 100, "Post");
                PrologueDesk.Fill(row, Color.white, false);
                var av = PrologueDesk.Rect("Avatar", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -64), new Vector2(68, -20));
                PrologueDesk.Fill(av, AvatarColor(p.who), false);
                Portrait(av, p.who, name);
                Text(PrologueDesk.Rect("Name", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(84, -44), new Vector2(-24, -18)), "<b>" + Safe(name) + "</b>", 16, Link, TextAlignmentOptions.TopLeft);
                var body = Text(PrologueDesk.Rect("Body", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(84, -200), new Vector2(-24, -48)), text, 16, Ink, TextAlignmentOptions.TopLeft);
                float th = body.GetPreferredValues(text, 760, 0).y;
                body.rectTransform.offsetMin = new Vector2(84, -48 - th);
                string artKey = MomentArt(p.id);
                float artHeight = 0;
                if (artKey != null && DesktopArt.Picture(artKey) != null)
                {
                    var art = PrologueDesk.Rect("Illustration", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -166 - th), new Vector2(264, -58 - th));
                    DesktopArt.Paint(art, artKey);
                    Text(PrologueDesk.Rect("Caption", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -184 - th), new Vector2(264, -168 - th)), Lang.T("配图"), 11, Muted, TextAlignmentOptions.MidlineLeft);
                    artHeight = 136;
                }
                bool liked = sim.JuxinLiked(p.id);
                string likes = liked ? T("我", "Me") : "";
                Text(PrologueDesk.Rect("Date", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -78 - th - artHeight), new Vector2(400, -54 - th - artHeight)), Day(p.date) + (likes.Length > 0 ? "    <color=#576B95>♥ " + likes + "</color>" : ""), 12, Muted, TextAlignmentOptions.MidlineLeft);
                var like = PrologueDesk.Rect("Like", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -78 - th - artHeight), new Vector2(-24, -54 - th - artHeight));
                var limg = PrologueDesk.Fill(like, new Color32(246, 246, 246, 255));
                var lb = like.gameObject.AddComponent<Button>(); lb.targetGraphic = limg;
                string id = p.id;
                lb.onClick.AddListener(() => { sim.JuxinLike(id); hub.Changed(); signature = ""; });
                Text(PrologueDesk.Rect("T", like, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), liked ? T("取消", "Unlike") : T("赞", "Like"), 13, Link, TextAlignmentOptions.Center);
                row.GetComponent<LayoutElement>().preferredHeight = 96 + th + artHeight;
                var sep = Row(content, 1, "Rule");
                PrologueDesk.Fill(sep, new Color32(236, 236, 236, 255), false);
            }
        }
    }
}
