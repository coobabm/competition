using System;
using DesktopArt = LingGuangV05.Desktop.Media.DesktopMedia;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Tieba
{
    /// <summary>
    /// 摆渡贴吧, the main stage (design v1.1 §3): front page with this month's hot threads, the boards (显卡吧,
    /// 人工智能吧, 三体吧, 黑暗之魂吧), threads with floors (deleted and folded floors look like 2016 tieba), user
    /// pages, the player's own page and old posts, and private messages. It lives in the desktop's spare native
    /// Reminder window, renamed and re-iconed at runtime; the scene and DreamOS assets are not changed.
    /// </summary>
    public sealed class TiebaView : MonoBehaviour
    {
        public const string NativeWindow = "Reminder";
        static readonly Color Blue = new Color32(56, 120, 230, 255), BlueDark = new Color32(36, 92, 196, 255), Page = new Color32(243, 245, 248, 255);
        static readonly Color Ink = new Color32(40, 44, 52, 255), Muted = new Color32(130, 136, 148, 255), Rule = new Color32(226, 230, 236, 255), Link = new Color32(40, 100, 200, 255);

        TiebaHub hub;
        WindowManager window;
        TMP_FontAsset font;
        RectTransform root, main, side;
        RectTransform scrollContent;
        ScrollRect scroll;
        TMP_Text navHome, navMine, navInbox;
        GameObject iconBadge, taskBadge;
        string view = "home", arg = "";
        string signature = "";
        float nextRefresh;
        TMP_InputField input;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static TiebaView Install(TiebaHub hub)
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == NativeWindow);
            if (window == null) return null;
            var content = window.transform.Find("Container/Content") as RectTransform;
            if (content == null) return null;
            var view = content.gameObject.GetComponent<TiebaView>() ?? content.gameObject.AddComponent<TiebaView>();
            view.hub = hub; view.window = window;
            hub.View = view;
            // The desktop entry is renamed now; the window content is built when it first opens.
            view.Rename();
            GameText.Changed += view.Rename;
            return view;
        }

        void Start()
        {
            font = PrologueDesk.CjkFont();
            foreach (Transform child in transform) child.gameObject.SetActive(false); // the native reminder UI
            Build();
            hub.Changed += () => signature = "";
            GameText.Changed += () => signature = "";
        }

        void OnDestroy() { GameText.Changed -= Rename; }

        /// <summary>The red dot on the desktop icon and taskbar button (called by the hub every frame, window open or not).</summary>
        public void UpdateBadges(bool unread)
        {
            if (iconBadge != null && iconBadge.activeSelf != unread) iconBadge.SetActive(unread);
            if (taskBadge != null && taskBadge.activeSelf != unread) taskBadge.SetActive(unread);
        }

        // ───────────── the desktop entry: name, icon, unread dot ─────────────

        void Rename()
        {
            if (window == null) return;
            string name = T("摆渡贴吧", "Tieba");
            var tex = Resources.Load<Texture2D>("LingGuangV05/Forum/tieba_icon");
            var sprite = tex != null ? Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f)) : null;
            foreach (var title in window.GetComponentsInChildren<TMP_Text>(true)) if (title.name == "Aero Window Title") title.text = name;
            var icon = GameObject.Find("Desktop List/" + NativeWindow);
            if (icon != null) { Relabel(icon, name, sprite); iconBadge = iconBadge ?? Badge(icon.transform as RectTransform, new Vector2(24, 30)); }
            if (window.taskbarButton != null) { Relabel(window.taskbarButton.gameObject, name, sprite); taskBadge = taskBadge ?? Badge(window.taskbarButton.transform as RectTransform, new Vector2(14, 12)); }
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

        GameObject Badge(RectTransform parent, Vector2 offset)
        {
            if (parent == null) return null;
            var dot = PrologueDesk.Rect("Tieba Unread", parent, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
            dot.sizeDelta = new Vector2(18, 18); dot.anchoredPosition = new Vector2(offset.x, -offset.y);
            PrologueDesk.Fill(dot, new Color32(230, 50, 50, 255), false);
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        // ───────────── layout ─────────────

        void OnEnable() { if (root != null) Go(view, arg); }

        void Build()
        {
            root = PrologueDesk.Rect("Tieba", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Page);
            UiFitScale.Attach(root, window, new Vector2(1266, 763));
            var top = PrologueDesk.Rect("Top", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -56), Vector2.zero);
            PrologueDesk.Fill(top, Blue);
            var logo = Text(PrologueDesk.Rect("Logo", top, Vector2.zero, new Vector2(0, 1), new Vector2(20, 0), new Vector2(260, 0)), "", 26, Color.white, TextAlignmentOptions.MidlineLeft);
            logo.fontStyle = FontStyles.Bold;
            logo.name = "Logo";
            navHome = NavButton(top, 0, () => Go("home", ""));
            navMine = NavButton(top, 1, () => Go("mine", ""));
            navInbox = NavButton(top, 2, () => Go("inbox", ""));
            side = PrologueDesk.Rect("Side", root, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0), new Vector2(190, -56));
            PrologueDesk.Fill(side, Color.white);
            main = PrologueDesk.Rect("Main", root, Vector2.zero, Vector2.one, new Vector2(200, 0), new Vector2(0, -56));
        }

        TMP_Text NavButton(RectTransform top, int index, Action click)
        {
            var rt = PrologueDesk.Rect("Nav" + index, top, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-150 * (3 - index) - 10, 8), new Vector2(-150 * (2 - index) - 20, -8));
            var img = PrologueDesk.Fill(rt, BlueDark);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => click());
            return Text(PrologueDesk.Rect("Text", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 18, Color.white, TextAlignmentOptions.Center);
        }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public void Go(string where, string what)
        {
            view = where; arg = what ?? "";
            hub.Showing = where == "chat" && LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this) ? arg : null;
            if (hub.Showing != null) hub.MarkSeen(arg);
            if (input != null) input.text = "";
            signature = "";
            Redraw();
            if (scroll != null) scroll.verticalNormalizedPosition = where == "chat" ? 0 : 1;
        }

        public void Open(string where = "home", string what = "")
        {
            if (window != null)
            {
                // Explicit visits must restore/focus minimized windows; reuse YY's first-open recovery.
                LingGuangV05.Desktop.XingGuang.XingGuangController.KeepOpenOnFirstStart(window);
                window.OpenWindow();
                LingGuangV05.Desktop.XingGuang.XingGuangController.EnsureShown(hub, window);
            }
            view = where; arg = what ?? ""; signature = "";
            if (root != null) Go(where, what);
        }

        void Update()
        {
            if (hub == null || hub.S == null) return;
            if (root == null) return;
            if (!LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this)) { hub.Showing = null; return; }
            hub.Showing = view == "chat" ? arg : null;
            if (hub.Showing != null) hub.MarkSeen(arg);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .5f;
            var c = hub.Context();
            string now = view + "|" + arg + "|" + c.stage + "|" + c.today.ToString("MMdd") + "|" + Count() + "|" + hub.IsTyping(arg) + "|" + (hub.Choices != null) + "|" + GameText.IsEnglish + "|" + (c.helpPosted ? (int)(c.secondsSinceHelp / 10) : -1);
            if (now != signature) { signature = now; Redraw(); }
        }

        int Count()
        {
            int n = hub.Unread;
            foreach (var conv in hub.S.conversations) n += conv.messages.Count * 7;
            return n;
        }

        // ───────────── pages ─────────────

        void Redraw()
        {
            var c = hub.Context();
            var logo = root.Find("Top/Logo")?.GetComponent<TMP_Text>();
            if (logo != null) logo.text = T("摆渡贴吧", "Bodu Tieba");
            navHome.text = Lang.T("首页");
            navMine.text = Lang.T("我的");
            navInbox.text = Lang.T("私信") + (hub.Unread > 0 ? " <color=#FFD24A>(" + hub.Unread + ")</color>" : "");
            DrawSide(c);
            // A half-typed message survives the redraw that a new reply causes.
            string draft = input != null ? input.text : "";
            bool focused = input != null && input.isFocused;
            for (int i = main.childCount - 1; i >= 0; i--) Destroy(main.GetChild(i).gameObject);
            input = null;
            if (view == "chat")
            {
                DrawChat(c, arg);
                if (input != null) { input.text = draft; if (focused) input.ActivateInputField(); }
                StartCoroutine(ScrollToEnd());
                return;
            }
            NewScroll(main, Vector2.zero);
            switch (view)
            {
                case "board": DrawBoard(c, arg); break;
                case "thread": DrawThread(c, arg); break;
                case "era": DrawEra(arg); break;
                case "user": DrawUser(c, arg); break;
                case "mine": DrawUser(c, ForumLibrary.Me); break;
                case "inbox": DrawInbox(c); break;
                default: DrawHome(c); break;
            }
        }

        void DrawSide(ForumContext c)
        {
            for (int i = side.childCount - 1; i >= 0; i--) Destroy(side.GetChild(i).gameObject);
            var head = Text(PrologueDesk.Rect("Head", side, new Vector2(0, 1), Vector2.one, new Vector2(16, -50), new Vector2(-10, -12)), Lang.T("我关注的吧"), 16, Muted, TextAlignmentOptions.MidlineLeft);
            int i2 = 0;
            foreach (var b in hub.Library.Boards)
            {
                var board = b;
                var row = PrologueDesk.Rect("Board", side, new Vector2(0, 1), Vector2.one, new Vector2(8, -96 - i2 * 44), new Vector2(-8, -56 - i2 * 44));
                var img = PrologueDesk.Fill(row, view == "board" && arg == board.id ? new Color32(228, 238, 255, 255) : (Color)new Color32(0, 0, 0, 0));
                var btn = row.gameObject.AddComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(() => Go("board", board.id));
                Text(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(12, 0), Vector2.zero), T(board.name, board.nameEn), 18, Link, TextAlignmentOptions.MidlineLeft);
                i2++;
            }
        }

        void NewScroll(RectTransform parent, Vector2 bottomInset)
        {
            var area = PrologueDesk.Rect("Scroll", parent, Vector2.zero, Vector2.one, new Vector2(0, bottomInset.y), Vector2.zero);
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = PrologueDesk.Rect("Viewport", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            PrologueDesk.Fill(viewport, new Color(0, 0, 0, 0));
            scrollContent = PrologueDesk.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            scrollContent.pivot = new Vector2(.5f, 1);
            var layout = scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 24, 14, 20); layout.spacing = 10;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            scrollContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = scrollContent;
        }

        RectTransform Card(Color? color = null)
        {
            var card = PrologueDesk.Rect("Card", scrollContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(card, color ?? Color.white);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12); layout.spacing = 6;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            return card;
        }

        TMP_Text Line(RectTransform parent, string text, float size, Color color, Action click = null)
        {
            var rt = PrologueDesk.Rect("Line", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = Text(rt, text, size, color, TextAlignmentOptions.TopLeft);
            if (click != null)
            {
                t.raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = t; b.onClick.AddListener(() => click());
            }
            return t;
        }

        // Portrait and illustration rows are decoration only; navigation stays on the original text.
        TMP_Text AuthorLine(RectTransform parent, string id, string text, float size, Color color, Action click = null)
        {
            var sprite = DesktopArt.Avatar(id);
            if (sprite == null) return Line(parent, text, size, color, click);
            var row = PrologueDesk.Rect("Author", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
            var portrait = PrologueDesk.Rect("Portrait", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -17), new Vector2(34, 17));
            var image = portrait.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            var label = Text(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(46, 0), Vector2.zero), text, size, color, TextAlignmentOptions.MidlineLeft);
            if (click != null) { label.raycastTarget = true; var button = label.gameObject.AddComponent<Button>(); button.targetGraphic = label; button.onClick.AddListener(() => click()); }
            return label;
        }

        void TopicLine(RectTransform parent, string key, string title, Action click = null)
        {
            if (DesktopArt.Picture(key) == null) { Line(parent, title, 19, Link, click); return; }
            var row = PrologueDesk.Rect("IllustratedTopic", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var art = PrologueDesk.Rect("Illustration", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -36), new Vector2(92, 36));
            DesktopArt.Paint(art, key);
            var label = Text(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(108, 16), Vector2.zero), title, 19, Link, TextAlignmentOptions.MidlineLeft);
            if (click != null) { label.raycastTarget = true; var button = label.gameObject.AddComponent<Button>(); button.targetGraphic = label; button.onClick.AddListener(() => click()); }
            Text(PrologueDesk.Rect("Caption", row, Vector2.zero, new Vector2(1, 0), new Vector2(108, 0), new Vector2(0, 18)), Lang.T("配图"), 11, Muted, TextAlignmentOptions.MidlineLeft);
        }

        string Name(string user)
        {
            if (user == ForumLibrary.LaoZhou && hub.S != null && hub.S.zhouGone) return Lang.T("该用户已注销");
            return hub.Library.Users.TryGetValue(user, out var u) ? T(u.name, u.nameEn) : user;
        }
        static string Date(DateTime d) => GameText.IsEnglish ? d.ToString("MMM d", CultureInfo.InvariantCulture) : d.Month + "-" + d.Day;

        void ThreadRow(ForumThread t, ForumContext c, bool showBoard)
        {
            var card = Card();
            int floors = 0; foreach (var f in t.floors) if (ForumLibrary.View(f, c) != FloorView.Hidden) floors++;
            var board = hub.Library.Boards.Find(b => b.id == t.board);
            TopicLine(card, "thread_" + t.id, "<b>" + T(t.title, t.titleEn) + "</b>", () => Go("thread", t.id));
            AuthorLine(card, t.author, (showBoard && board != null ? T(board.name, board.nameEn) + "  ·  " : "") + Name(t.author) + "  ·  " + Date(t.date) + "  ·  " + Lang.T("回复 ") + Math.Max(0, floors - 1), 14, Muted);
        }

        void DrawHome(ForumContext c)
        {
            Line(Card(new Color32(255, 250, 230, 255)), "<b>" + Lang.T("今日热议") + "</b>  " + c.today.ToString(GameText.IsEnglish ? "MMMM d, yyyy" : "yyyy 年 M 月 d 日", CultureInfo.InvariantCulture), 17, Ink);
            int n = 0;
            foreach (var e in EraContent.Events.Visible(EraEvents.Tieba, c.today))
            {
                if (n++ >= 6) break;
                var ev = e;
                var card = Card();
                TopicLine(card, "era_" + ev.id, "<b>" + EraContent.Title(ev) + "</b>", () => Go("era", ev.id));
                Line(card, Date(ev.date) + "  ·  " + EraContent.Text(ev), 14, Muted);
            }
            var recent = new List<ForumThread>();
            foreach (var t in hub.Library.Threads) if (!t.mine && ForumLibrary.Visible(t, c)) recent.Add(t);
            recent.Sort((a, b) => b.date.CompareTo(a.date));
            for (int i = 0; i < Math.Min(8, recent.Count); i++) ThreadRow(recent[i], c, true);
        }

        void DrawBoard(ForumContext c, string board)
        {
            var b = hub.Library.Boards.Find(x => x.id == board);
            Line(Card(new Color32(232, 240, 255, 255)), "<b><size=24>" + (b != null ? T(b.name, b.nameEn) : board) + "</size></b>", 20, Ink);
            if (board == "gpu" && hub.CanPostHelp)
            {
                var card = Card(new Color32(255, 248, 235, 255));
                var help = hub.Library.Get("help_bios");
                Line(card, Lang.T("发个帖子问问？") + "\n<color=#6E7480>" + T(help.title, help.titleEn) + "</color>", 16, Ink);
                Line(card, "<b>" + Lang.T("【发表】") + "</b>", 17, Link, () => { hub.PostHelp(); Go("thread", "help_bios"); });
            }
            foreach (var t in hub.Library.Board(board, c)) ThreadRow(t, c, false);
            foreach (var t in hub.Library.Mine(c)) if (t.board == board) ThreadRow(t, c, false);
        }

        void DrawThread(ForumContext c, string id)
        {
            var t = hub.Library.Get(id);
            if (t == null || !ForumLibrary.Visible(t, c)) { Line(Card(), Lang.T("帖子不存在。"), 18, Muted); return; }
            if (t.dead)
            {
                var card = Card();
                Line(card, "<size=60><b>404</b></size>", 20, Muted);
                Line(card, Lang.T("很抱歉，该贴已被删除。"), 18, Muted);
                return;
            }
            Line(Card(new Color32(232, 240, 255, 255)), "<b><size=22>" + T(t.title, t.titleEn) + "</size></b>", 20, Ink);
            int floor = 0;
            foreach (var f in t.floors)
            {
                var v = ForumLibrary.View(f, c);
                if (v == FloorView.Hidden) continue;
                floor++;
                var card = Card();
                var author = f.author;
                hub.Library.Users.TryGetValue(author, out var user);
                string head = "<b>" + Name(author) + "</b>  <color=#E08A00>Lv." + (user != null ? user.level : 1) + "</color>" + (floor == 1 ? "  <color=#3878E6>" + Lang.T("楼主") + "</color>" : "");
                AuthorLine(card, author, head, 16, Link, author == ForumLibrary.Me ? (Action)(() => Go("mine", "")) : () => Go("user", author));
                if (v == FloorView.Deleted) Line(card, Lang.T("该楼层已被删除"), 17, Muted);
                else if (v == FloorView.Folded) Line(card, Lang.T("该楼层疑似违规已被系统折叠"), 17, Muted);
                else Line(card, T(f.text, f.textEn), 18, Ink);
                string sign = user != null && user.sign.Length > 0 && v == FloorView.Shown ? "\n<size=13><color=#A0A6B0>—— " + T(user.sign, user.signEn) + "</color></size>" : "";
                Line(card, "<color=#A0A6B0>" + floor + Lang.T(" 楼") + "</color>" + sign, 13, Muted);
            }
        }

        void DrawEra(string id)
        {
            var e = EraContent.Events.Get(id);
            if (e == null) { Line(Card(), Lang.T("帖子不存在。"), 18, Muted); return; }
            Line(Card(new Color32(232, 240, 255, 255)), "<b><size=22>" + EraContent.Title(e) + "</size></b>", 20, Ink);
            var card = Card();
            AuthorLine(card, "chigua", "<b>" + Name("chigua") + "</b>  <color=#E08A00>Lv.3</color>  <color=#3878E6>" + Lang.T("楼主") + "</color>", 16, Link);
            TopicLine(card, "era_" + e.id, EraContent.Title(e));
            Line(card, EraContent.Text(e), 18, Ink);
            var reply = Card();
            AuthorLine(reply, "xiaobai", "<b>" + Name("xiaobai") + "</b>  <color=#E08A00>Lv.2</color>", 16, Link);
            Line(reply, Lang.T("前排。"), 18, Ink);
        }

        void DrawUser(ForumContext c, string id)
        {
            hub.Library.Users.TryGetValue(id, out var user);
            var head = Card(new Color32(232, 240, 255, 255));
            AuthorLine(head, id, "<b><size=24>" + Name(id) + "</size></b>  <color=#E08A00>Lv." + (user != null ? user.level : 1) + "</color>", 20, Ink);
            if (user != null && user.sign.Length > 0) Line(head, Lang.T("签名：") + T(user.sign, user.signEn), 16, Muted);
            if (hub.CanMessage(id)) Line(head, "<b>" + Lang.T("【私信】") + "</b>", 17, Link, () => Go("chat", id));
            Line(Card(new Color32(248, 248, 248, 255)), id == ForumLibrary.Me ? Lang.T("我的帖子") : Lang.T("TA 的帖子"), 16, Muted);
            var threads = id == ForumLibrary.Me ? hub.Library.Mine(c) : hub.Library.By(id, c);
            foreach (var t in threads) ThreadRow(t, c, true);
            if (threads.Count == 0) Line(Card(), Lang.T("还没有发过帖子。"), 16, Muted);
        }

        void DrawInbox(ForumContext c)
        {
            Line(Card(new Color32(232, 240, 255, 255)), "<b>" + Lang.T("私信") + "</b>", 20, Ink);
            foreach (var id in new[] { ForumLibrary.LaoZhou, ForumLibrary.ZhouNow })
            {
                if (!hub.CanMessage(id) && !(id == ForumLibrary.LaoZhou && hub.S.zhouGone)) continue;
                var conv = hub.Conversation(id);
                if (id == ForumLibrary.ZhouNow && conv.messages.Count == 0 && !hub.S.metZhouNow) continue;
                var who = id;
                var card = Card();
                string last = conv.messages.Count > 0 ? conv.messages[conv.messages.Count - 1].text : "";
                if (last.Length > 40) last = last.Substring(0, 40) + "…";
                AuthorLine(card, id, "<b>" + Name(id) + "</b>" + (conv.unread > 0 ? "  <color=#E63232>●" + conv.unread + "</color>" : ""), 18, Link, () => Go("chat", who));
                Line(card, last, 15, Muted);
            }
        }

        void DrawChat(ForumContext c, string id)
        {
            var conv = hub.Conversation(id);
            if (LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this)) hub.MarkSeen(id);
            bool choices = hub.Choices != null && hub.ChoicesFor == id;
            float bottom = choices ? 120 : 76;
            var header = PrologueDesk.Rect("Header", main, new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero);
            PrologueDesk.Fill(header, Color.white);
            Text(PrologueDesk.Rect("Name", header, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0)), "<b>" + Name(id) + "</b>" + (hub.IsTyping(id) ? "  <size=14><color=#8A90A0>" + Lang.T("对方正在输入…") + "</color></size>" : ""), 18, Ink, TextAlignmentOptions.MidlineLeft);
            var body = PrologueDesk.Rect("Body", main, Vector2.zero, Vector2.one, new Vector2(0, bottom), new Vector2(0, -44));
            NewScroll(body, Vector2.zero);
            DateTime lastDay = DateTime.MinValue;
            foreach (var m in conv.messages)
            {
                var when = GameCalendar.ClockFor(runtimeSave, Math.Max(0, m.gameSeconds));
                if (m.gameSeconds < 0) when = GameCalendar.Start.AddSeconds(m.gameSeconds);
                if (when.Date != lastDay) { lastDay = when.Date; Line(Card(new Color(0, 0, 0, 0)), "<align=center><color=#A0A6B0>" + Date(when) + "</color></align>", 13, Muted); }
                bool mine = m.from == ForumLibrary.Me;
                var card = Card(mine ? new Color32(220, 235, 255, 255) : Color.white);
                AuthorLine(card, m.from, "<b>" + (mine ? Name(ForumLibrary.Me) : Name(m.from)) + "</b>  <color=#A0A6B0>" + when.ToString("HH:mm", CultureInfo.InvariantCulture) + "</color>", 14, mine ? BlueDark : Link);
                Line(card, m.text, 17, Ink);
            }
            var bar = PrologueDesk.Rect("Input", main, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, bottom));
            PrologueDesk.Fill(bar, Color.white);
            if (choices)
            {
                float x = 12;
                foreach (var choice in hub.Choices)
                {
                    string text = choice;
                    float w = 30 + text.Length * (GameText.IsEnglish ? 8f : 16f);
                    var rt = PrologueDesk.Rect("Choice", bar, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -40), new Vector2(x + w, -8));
                    var img = PrologueDesk.Fill(rt, Blue);
                    var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => hub.Send(id, text));
                    Text(PrologueDesk.Rect("Text", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 15, Color.white, TextAlignmentOptions.Center);
                    x += w + 10;
                }
            }
            var box = PrologueDesk.Rect("Field", bar, Vector2.zero, new Vector2(1, 0), new Vector2(12, 12), new Vector2(-130, 64));
            PrologueDesk.Fill(box, new Color32(244, 246, 250, 255));
            var area = PrologueDesk.Rect("Text Area", box, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(PrologueDesk.Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Lang.T("说点什么……"), 16, Muted, TextAlignmentOptions.MidlineLeft);
            var textLabel = Text(PrologueDesk.Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16, Ink, TextAlignmentOptions.MidlineLeft);
            textLabel.richText = false;
            input = box.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area; input.textComponent = textLabel; input.placeholder = placeholder; input.fontAsset = font; input.pointSize = 16;
            input.characterLimit = NativeLaoZhouReplies.MaxInputLength; input.lineType = TMP_InputField.LineType.SingleLine; input.richText = false;
            input.onSubmit.AddListener(_ => SendTyped(id));
            var send = PrologueDesk.Rect("Send", bar, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-118, 12), new Vector2(-12, 64));
            var sendImg = PrologueDesk.Fill(send, Blue);
            var sb = send.gameObject.AddComponent<Button>(); sb.targetGraphic = sendImg; sb.onClick.AddListener(() => SendTyped(id));
            Text(PrologueDesk.Rect("Text", send, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Lang.T("发送"), 17, Color.white, TextAlignmentOptions.Center);
        }

        System.Collections.IEnumerator ScrollToEnd()
        {
            yield return null; // the layout has its size after one frame
            if (scroll != null) scroll.verticalNormalizedPosition = 0;
        }

        GameState runtimeSave => hub.runtime != null && hub.runtime.Sim != null ? hub.runtime.Sim.S : null;

        void SendTyped(string id)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.text)) return;
            hub.Send(id, input.text);
            input.text = "";
            signature = "";
        }
    }
}
