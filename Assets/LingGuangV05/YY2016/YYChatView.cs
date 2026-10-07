using System;
using DesktopArt = LingGuangV05.Desktop.Media.DesktopMedia;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// YY in the style of a 2016 PC chat client: blue profile header, session list on the left, conversation on the
    /// right with round avatars, left/right bubbles, time separators, file cards, a tool strip, input box and send button.
    /// Replaces the old native Messaging content inside the same window (icon, taskbar button and window unchanged).
    /// </summary>
    public sealed partial class YYChatView : MonoBehaviour, IDesktopAppView
    {
        static class C
        {
            public static readonly Color Page = new Color32(244, 247, 251, 255);
            public static readonly Color Side = new Color32(236, 243, 250, 255);
            public static readonly Color HeadTop = new Color32(41, 182, 246, 255);
            public static readonly Color HeadBottom = new Color32(16, 132, 220, 255);
            public static readonly Color Line = new Color32(220, 228, 238, 255);
            public static readonly Color Ink = new Color32(34, 40, 52, 255);
            public static readonly Color Muted = new Color32(128, 138, 154, 255);
            public static readonly Color Mine = new Color32(18, 183, 245, 255);
            public static readonly Color Theirs = Color.white;
            public static readonly Color Selected = new Color32(214, 233, 252, 255);
            public static readonly Color Badge = new Color32(244, 67, 54, 255);
            public static readonly Color Online = new Color32(76, 175, 80, 255);
        }

        const float SideWidth = 244, RowHeight = 64, HeaderHeight = 58, ToolHeight = 34, InputHeight = 96, BottomHeight = 44;

        YYChatHub hub;
        WindowManager window;
        TMP_FontAsset font;
        Sprite laoZhouSprite, meSprite;
        RectTransform root, contactList, scrollContent, messageArea, choiceStrip, peerAvatar;
        string peerAvatarId = "";
        string shownChoices = "";
        ScrollRect scroll;
        TMP_Text peerName, peerSign, typing, meName, memoryLine;
        TMP_InputField input;
        readonly List<RectTransform> contactRows = new List<RectTransform>();
        string lastSignature = "";
        bool built, dirty = true, stickBottom = true;
        float nextBuild;
        readonly List<(YYMessage message, RectTransform fill, TMP_Text status, float width)> liveFiles = new List<(YYMessage, RectTransform, TMP_Text, float)>();

        public bool IsOpen => LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(this);

        // ───────────── bootstrap ─────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.TestMode) return;
            var router = runtime.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            var binding = router != null ? router.Get(YYChatHub.AppId) : null;
            if (binding == null || binding.Window == null) return;
            if (binding.Window.windowContainer != null && binding.Window.windowContainer.GetComponentInChildren<YYChatView>(true) != null) return;
            runtime.EnsureInitialized();
            var hub = runtime.GetComponent<YYChatHub>() ?? runtime.gameObject.AddComponent<YYChatHub>();
            hub.Initialize(runtime, router);

            var window = binding.Window;
            LingGuangV05.Desktop.XingGuang.XingGuangController.KeepOpenOnFirstStart(window);
            var content = window.windowContainer != null ? window.windowContainer.Find("Content") as RectTransform : null;
            if (content == null) return;
            TMP_FontAsset font = null;
            var shop = router.Get("xunbao");
            if (shop != null && shop.Content != null) foreach (var t in shop.Content.GetComponentsInChildren<TMP_Text>(true)) { font = t.font; break; }

            // Sprites from the old native panel, then retire it.
            Sprite lz = Panda(content, "Lao Zhou Panda"), me = Panda(content, "My Profile Panda");
            foreach (Transform child in content) child.gameObject.SetActive(false);
            var shell = FindAnyObjectByType<YY2010ShellController>(FindObjectsInactive.Include);
            if (shell != null)
            {
                if (shell.FriendsWindow != null && shell.FriendsWindow.isOn) shell.FriendsWindow.CloseWindow();
                Destroy(shell);
            }
            if (binding.DesktopShortcut != null)
            {
                binding.DesktopShortcut.onClick = new Button.ButtonClickedEvent();
                binding.DesktopShortcut.onClick.AddListener(() => router.Open(YYChatHub.AppId));
            }
            foreach (var label in window.windowContainer.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.transform.IsChildOf(content)) continue;
                var localized = label.GetComponent<DesktopLocalizedText>();
                if (localized != null) Destroy(localized);
                if (label.name == "Aero Window Title") label.text = "YY";
                else if (label.name == "Status") label.text = Lang.T("YY 2016 · 消息只保存在本机");
                if (font != null) label.font = font;
            }
            var view = content.gameObject.GetComponent<YYChatView>() ?? content.gameObject.AddComponent<YYChatView>();
            view.Setup(hub, window, font, lz, me ?? lz);
            binding.UseCustomView(view);
        }

        /// <summary>The old panels draw the panda as a child under a circular mask; take the picture, not the mask.</summary>
        static Sprite Panda(Transform root, string holder)
        {
            var h = FindDeep(root, holder);
            if (h == null) return null;
            Sprite any = null;
            foreach (var img in h.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img.sprite.texture == null) continue;
                if (img.sprite.texture.name.ToLowerInvariant().Contains("panda")) return img.sprite;
                if (img.transform != h) any = any ?? img.sprite;
            }
            return any;
        }

        void Setup(YYChatHub chat, WindowManager win, TMP_FontAsset fontAsset, Sprite lz, Sprite me)
        {
            hub = chat; window = win; font = fontAsset; laoZhouSprite = DesktopArt.Avatar("laozhou") ?? lz; meSprite = DesktopArt.Avatar("me") ?? me;
            hub.View = this;
            hub.Changed += () => dirty = true;
            GameText.Changed += OnLanguage;
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = C.Page;
        }

        void OnDestroy() { GameText.Changed -= OnLanguage; if (hub != null && hub.View == this) hub.View = null; }
        void OnLanguage() { lastSignature = ""; dirty = true; }

        public bool EnsureReady()
        {
            if (hub == null) return false;
            if (!built) Build();
            return true;
        }

        public void OnOpened(string tab)
        {
            LingGuangV05.Desktop.XingGuang.XingGuangController.EnsureShown(hub, window);
            if (!string.IsNullOrEmpty(tab) && YYChatHub.Contact(tab) != null) hub.Select(tab);
            hub.MarkSeen();
            lastSignature = ""; dirty = true; stickBottom = true;
            Rebuild();
        }

        // ───────────── layout ─────────────

        void Build()
        {
            built = true;
            root = Rect("YY2016", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Fill(root, C.Page);

            // left: profile header, search, sessions
            var side = Rect("Side", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(SideWidth, 0));
            Fill(side, C.Side);
            var head = Rect("Profile", side, new Vector2(0, 1), Vector2.one, new Vector2(0, -78), Vector2.zero);
            var grad = head.gameObject.AddComponent<YYRoundRect>();
            grad.radius = 0; grad.gradient = true; grad.top = C.HeadTop; grad.color = C.HeadBottom;
            Avatar(head, meSprite, "我", new Color32(255, 255, 255, 255), new Vector2(14, -14), 48);
            meName = Text(Rect("Name", head, new Vector2(0, 1), new Vector2(1, 1), new Vector2(72, -40), new Vector2(-10, -14)), "", 17, Color.white, TextAlignmentOptions.MidlineLeft);
            meName.fontStyle = FontStyles.Bold;
            Text(Rect("Status", head, new Vector2(0, 1), new Vector2(1, 1), new Vector2(72, -64), new Vector2(-10, -42)), "", 13, new Color(1, 1, 1, .85f), TextAlignmentOptions.MidlineLeft).name = "MeStatus";
            levelRow = Rect("Level", head, new Vector2(0, 1), new Vector2(1, 1), new Vector2(130, -38), new Vector2(-6, -18));
            var search = Rect("Search", side, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -114), new Vector2(-10, -86));
            var sbg = search.gameObject.AddComponent<YYRoundRect>(); sbg.radius = 4; sbg.color = Color.white; sbg.border = 1; sbg.borderColor = C.Line;
            Text(Rect("Hint", search, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-8, 0)), "", 13, C.Muted, TextAlignmentOptions.MidlineLeft).name = "SearchHint";
            contactList = Rect("Sessions", side, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -122));
            var sideLine = Rect("Divider", root, new Vector2(0, 0), new Vector2(0, 1), new Vector2(SideWidth - 1, 0), new Vector2(SideWidth, 0));
            Fill(sideLine, C.Line);

            // right: header, messages, tools, input, buttons
            var main = Rect("Conversation", root, Vector2.zero, Vector2.one, new Vector2(SideWidth, 0), Vector2.zero);
            var header = Rect("Header", main, new Vector2(0, 1), Vector2.one, new Vector2(0, -HeaderHeight), Vector2.zero);
            Fill(header, Color.white);
            Fill(Rect("Line", header, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), C.Line);
            peerAvatar = Rect("PeerAvatar", header, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -48), new Vector2(54, -8));
            peerName = Text(Rect("Name", header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(66, -34), new Vector2(-180, -4)), "", 19, C.Ink, TextAlignmentOptions.MidlineLeft);
            peerName.fontStyle = FontStyles.Bold;
            peerName.overflowMode = TextOverflowModes.Overflow; peerName.textWrappingMode = TextWrappingModes.NoWrap;
            peerSign = Text(Rect("Sign", header, Vector2.zero, new Vector2(1, 0), new Vector2(66, 4), new Vector2(-180, 24)), "", 13, C.Muted, TextAlignmentOptions.MidlineLeft);
            typing = Text(Rect("Typing", header, new Vector2(1, 0), Vector2.one, new Vector2(-176, 0), new Vector2(-16, 0)), "", 13, C.Muted, TextAlignmentOptions.MidlineRight);
            // 灵光 only: 「它记得」 (stage 5+) at the bottom right, its persona on its name; both explain themselves on hover.
            memoryLine = Text(Rect("Memory", header, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-250, 3), new Vector2(-16, 20)), "", 12, C.Muted, TextAlignmentOptions.MidlineRight);
            memoryLine.textWrappingMode = TextWrappingModes.NoWrap;
            UiTip.Add(memoryLine, () => LingGuangShown ? hub.LingGuangTalk.MemoryTip() : "");
            UiTip.Add(peerName, () => LingGuangShown ? hub.LingGuangTalk.PersonaTip() : "");

            float bottom = BottomHeight + InputHeight + ToolHeight;
            messageArea = Rect("Messages", main, Vector2.zero, Vector2.one, new Vector2(0, bottom), new Vector2(0, -HeaderHeight));
            Fill(messageArea, C.Page);
            scroll = messageArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var viewport = Rect("Viewport", messageArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            Fill(viewport, new Color(0, 0, 0, 0));
            scrollContent = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            scrollContent.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport; scroll.content = scrollContent;
            scroll.onValueChanged.AddListener(v => stickBottom = v.y <= .02f);

            var tools = Rect("Tools", main, Vector2.zero, new Vector2(1, 0), new Vector2(0, BottomHeight + InputHeight), new Vector2(0, bottom));
            Fill(tools, Color.white);
            Fill(Rect("Line", tools, new Vector2(0, 1), Vector2.one, new Vector2(0, -1), Vector2.zero), C.Line);
            string[] toolNames = { "Emoji", "File", "Shot", "Shake", "History" };
            for (int i = 0; i < toolNames.Length; i++)
            {
                string id = toolNames[i];
                var b = FlatButton(tools, "", () => OnTool(id), 14, C.Muted, new Color(0, 0, 0, 0));
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, .5f);
                rt.offsetMin = new Vector2(10 + i * 74, -14); rt.offsetMax = new Vector2(10 + i * 74 + 70, 14);
                b.name = "Tool" + id;
            }

            // Scripted replies (the prologue): buttons over the tool strip.
            choiceStrip = Rect("Choices", main, Vector2.zero, new Vector2(1, 0), new Vector2(0, BottomHeight + InputHeight), new Vector2(0, bottom));
            Fill(choiceStrip, new Color32(236, 244, 255, 255));
            choiceStrip.gameObject.SetActive(false);

            var inputBox = Rect("Input", main, Vector2.zero, new Vector2(1, 0), new Vector2(0, BottomHeight), new Vector2(0, BottomHeight + InputHeight));
            Fill(inputBox, Color.white);
            input = BuildInput(inputBox);

            var bar = Rect("Bottom", main, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, BottomHeight));
            Fill(bar, Color.white);
            var send = FlatButton(bar, "", Send, 15, Color.white, C.Mine);
            Place((RectTransform)send.transform, new Vector2(1, .5f), new Vector2(-104, -15), new Vector2(-14, 15));
            send.name = "Send";
            var close = FlatButton(bar, "", () => { if (window.isOn) window.CloseWindow(); }, 15, C.Ink, new Color32(238, 241, 245, 255));
            Place((RectTransform)close.transform, new Vector2(1, .5f), new Vector2(-196, -15), new Vector2(-112, 15));
            close.name = "Close";
            Text(Rect("Hint", bar, Vector2.zero, new Vector2(1, 1), new Vector2(14, 0), new Vector2(-210, 0)), "", 12, C.Muted, TextAlignmentOptions.MidlineLeft).name = "EnterHint";

            foreach (var c in YYChatHub.Contacts) contactRows.Add(BuildContactRow(c));
            foreach (var focus in GetComponentsInChildren<Selectable>(true)) AddFocus(focus.gameObject);
            AddFocus(gameObject);
        }

        TMP_InputField BuildInput(RectTransform box)
        {
            var go = Rect("Field", box, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6)).gameObject;
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var area = Rect("Text Area", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, C.Muted, TextAlignmentOptions.TopLeft);
            var text = Text(Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, C.Ink, TextAlignmentOptions.TopLeft);
            text.richText = false;
            var field = go.AddComponent<TMP_InputField>();
            field.textViewport = area; field.textComponent = text; field.placeholder = placeholder;
            field.fontAsset = font; field.pointSize = 15;
            field.lineType = TMP_InputField.LineType.MultiLineSubmit;
            field.characterLimit = 300; field.richText = false;
            field.onSubmit.AddListener(_ => Send());
            return field;
        }

        RectTransform BuildContactRow(YYContact c)
        {
            var row = Rect("Session_" + c.id, contactList, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var bg = row.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0);
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
            var cb = button.colors; cb.highlightedColor = new Color(1, 1, 1, .5f); cb.normalColor = Color.white; cb.pressedColor = new Color(.9f, .95f, 1f); button.colors = cb;
            button.onClick.AddListener(() => { hub.Select(c.id); stickBottom = true; dirty = true; });
            Avatar(row, SpriteFor(c), Initial(c), c.color, new Vector2(12, -10), 44, !c.online && !c.group);
            var name = Text(Rect("Name", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(66, -32), new Vector2(-54, -10)), "", 15, C.Ink, TextAlignmentOptions.MidlineLeft);
            name.name = "Name";
            Text(Rect("Preview", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(66, -54), new Vector2(-14, -32)), "", 13, C.Muted, TextAlignmentOptions.MidlineLeft).name = "Preview";
            Text(Rect("Time", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-54, -30), new Vector2(-10, -10)), "", 12, C.Muted, TextAlignmentOptions.MidlineRight).name = "Time";
            var badge = Rect("Badge", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-34, -54), new Vector2(-12, -34));
            badge.gameObject.AddComponent<YYRoundRect>().color = C.Badge;
            badge.GetComponent<YYRoundRect>().radius = 10;
            Text(Rect("Count", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12, Color.white, TextAlignmentOptions.Center).name = "Count";
            return row;
        }

        // ───────────── refresh ─────────────

        void Update()
        {
            if (!built || !IsOpen) return;
            hub.MarkSeen(); // Also runs after the opening/minimize animation becomes visible.
            if (typing != null) typing.text = hub.IsTyping(hub.S.selected) ? Lang.T("对方正在输入…") : "";
            foreach (var f in liveFiles) UpdateFile(f.message, f.fill, f.status, f.width);
            if (dirty && Time.unscaledTime >= nextBuild) Rebuild();
        }

        void Rebuild()
        {
            if (!built) return;
            dirty = false; nextBuild = Time.unscaledTime + .1f;
            if (IsOpen) hub.MarkSeen();
            meName.text = GameText.T("我", "Me");
            var meStatus = root.Find("Side/Profile/MeStatus") ?? FindDeep(root, "MeStatus");
            if (meStatus != null) meStatus.GetComponent<TMP_Text>().text = Lang.T("● 在线  ·  今天也要好好学习");
            DrawLevel();
            var hint = FindDeep(root, "SearchHint"); if (hint != null) hint.GetComponent<TMP_Text>().text = Lang.T("搜索：联系人、群");
            var enter = FindDeep(root, "EnterHint"); if (enter != null) enter.GetComponent<TMP_Text>().text = Lang.T("Enter 发送  ·  只是本地预设回复，不连网");
            SetButton("Send", GameText.T("发送(S)", "Send"));
            SetButton("Close", Lang.T("关闭(C)"));
            string[] tools = { "Emoji", "File", "Shot", "Shake", "History" };
            string[] toolText = { Lang.T("☺ 表情"), Lang.T("▤ 文件"), Lang.T("✂ 截图"), Lang.T("〰 抖一抖"), Lang.T("◷ 记录") };
            for (int i = 0; i < tools.Length; i++) SetButton("Tool" + tools[i], toolText[i]);
            if (input.placeholder is TMP_Text ph) ph.text = Lang.T("说点什么……");
            RebuildChoices();

            // sessions, sorted by latest message
            var order = new List<YYContact>();
            foreach (var c in YYChatHub.Contacts) if (hub.IsVisible(c)) order.Add(c);
            order.Sort((a, b) => Latest(b).CompareTo(Latest(a)));
            foreach (var row in contactRows) row.gameObject.SetActive(order.Exists(c => row.name == "Session_" + c.id));
            for (int i = 0; i < order.Count; i++)
            {
                var c = order[i];
                var row = contactRows.Find(r => r.name == "Session_" + c.id);
                row.offsetMin = new Vector2(0, -(i + 1) * RowHeight); row.offsetMax = new Vector2(0, -i * RowHeight);
                bool on = hub.S.selected == c.id;
                row.GetComponent<Image>().color = on ? C.Selected : new Color(0, 0, 0, 0);
                var conv = hub.Conversation(c.id);
                var last = conv.messages.Count > 0 ? conv.messages[conv.messages.Count - 1] : null;
                row.Find("Name").GetComponent<TMP_Text>().text = GameText.T(c.name, c.nameEn) + (c.group ? "" : c.online ? "" : Lang.T("  <size=12><color=#9AA4B2>[离线]</color></size>"));
                row.Find("Name").GetComponent<TMP_Text>().richText = true;
                var previewLabel = row.Find("Preview").GetComponent<TMP_Text>();
                YYFaceRenderer.Prepare(previewLabel);
                previewLabel.text = last == null ? YYFaceRenderer.Markup(GameText.T(c.signature, c.signatureEn)) : Preview(last);
                row.Find("Time").GetComponent<TMP_Text>().text = last == null ? "" : YYChatHub.Clock(last.gameSeconds);
                var badge = row.Find("Badge");
                badge.gameObject.SetActive(conv.unread > 0 && !on);
                badge.Find("Count").GetComponent<TMP_Text>().text = conv.unread > 99 ? "99+" : conv.unread.ToString();
            }

            var contact = YYChatHub.Contact(hub.S.selected);
            if (contact == null || !hub.IsVisible(contact)) contact = YYChatHub.Contacts[0];
            peerName.text = GameText.T(contact.name, contact.nameEn);
            if (peerAvatarId != contact.id)
            {
                peerAvatarId = contact.id;
                for (int i = peerAvatar.childCount - 1; i >= 0; i--) Destroy(peerAvatar.GetChild(i).gameObject);
                Avatar(peerAvatar, SpriteFor(contact), Initial(contact), contact.color, Vector2.zero, 40, !contact.online && !contact.group);
            }
            peerSign.text = (contact.group ? Lang.T("群 · ") : contact.online ? Lang.T("<color=#4CAF50>●</color> 在线 · ") : Lang.T("○ 离线 · ")) + GameText.T(contact.signature, contact.signatureEn);
            if (contact.id == YYChatHub.GirlfriendId && hub.Girlfriend != null) peerSign.text = hub.Girlfriend.StatusLine();
            peerSign.richText = true;
            string remembers = contact.id == YYChatHub.LingGuangId && hub.LingGuangTalk != null ? hub.LingGuangTalk.MemoryLine() : "";
            memoryLine.text = remembers;
            memoryLine.gameObject.SetActive(remembers.Length > 0);

            var current = hub.Conversation(contact.id);
            string signature = contact.id + ":" + current.messages.Count + ":" + FileStates(current) + ":" + GameText.LanguageId + ":" + Mathf.RoundToInt(messageArea.rect.width) + RatingStamp(contact.id, current);
            if (signature != lastSignature) { lastSignature = signature; BuildMessages(current); }
        }

        void BuildMessages(YYConversation conv)
        {
            for (int i = scrollContent.childCount - 1; i >= 0; i--) Destroy(scrollContent.GetChild(i).gameObject);
            liveFiles.Clear();
            float width = Mathf.Max(300, messageArea.rect.width);
            float maxBubble = Mathf.Min(430, width * .62f);
            float y = 16;
            double lastTime = double.NegativeInfinity;
            foreach (var m in conv.messages)
            {
                if (m.gameSeconds - lastTime > 300 || YYChatHub.Day(m.gameSeconds) != YYChatHub.Day(lastTime))
                {
                    y += TimeStamp(y, YYChatHub.Day(m.gameSeconds) + "  " + YYChatHub.Clock(m.gameSeconds), width) + 10;
                    lastTime = m.gameSeconds;
                }
                if (m.kind == YYKind.System) { y += SystemLine(y, m.text, width) + 12; continue; }
                bool mine = m.from == YYChatHub.Me;
                var who = YYChatHub.Contact(m.from);
                var photo = AuthoredPhoto(m);
                float h = m.kind == YYKind.File ? FileCard(y, m, mine, who, width) : IsSticker(m) ? StickerBubble(y, m, mine, who, width) : photo != null ? PhotoBubble(y, m, who, maxBubble, photo) : Bubble(y, m, mine, who, width, maxBubble);
                y += h + 14;
            }
            scrollContent.sizeDelta = new Vector2(0, y + 8);
            if (stickBottom) Canvas.ForceUpdateCanvases();
            if (stickBottom) scroll.verticalNormalizedPosition = 0;
        }

        float TimeStamp(float y, string text, float width)
        {
            var t = Text(Rect("Time", scrollContent, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-90, -y - 22), new Vector2(90, -y)), text, 12, Color.white, TextAlignmentOptions.Center);
            var bg = Rect("Pill", scrollContent, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-62, -y - 21), new Vector2(62, -y - 1));
            var r = bg.gameObject.AddComponent<YYRoundRect>(); r.radius = 10; r.color = new Color32(206, 214, 226, 255);
            bg.SetSiblingIndex(t.transform.GetSiblingIndex());
            return 22;
        }

        float SystemLine(float y, string text, float width)
        {
            Text(Rect("System", scrollContent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -y - 20), new Vector2(-40, -y)), text, 12, C.Muted, TextAlignmentOptions.Center);
            return 20;
        }

        float Bubble(float y, YYMessage m, bool mine, YYContact who, float width, float maxBubble)
        {
            const float avatar = 36, gap = 10, padX = 12, padY = 9;
            string body = m.text ?? "";
            bool focusMarkup = mine && !string.IsNullOrEmpty(m.attentionWord);
            if (focusMarkup) body = LingGuangV05.XingGuang.XgSpeechPolicy.Underline(body, m.attentionWord);
            var probe = Text(Rect("Text", scrollContent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero), BubbleText(body, focusMarkup), 15, mine ? Color.white : C.Ink, TextAlignmentOptions.TopLeft);
            YYFaceRenderer.Prepare(probe);
            YYFaceRenderer.AddHoverNames(probe, body);
            Vector2 pref = probe.GetPreferredValues(BubbleText(body, focusMarkup), maxBubble - padX * 2, 0);
            float tw = Mathf.Min(maxBubble - padX * 2, Mathf.Max(18, pref.x + 2));
            float th = probe.GetPreferredValues(BubbleText(body, focusMarkup), tw, 0).y;
            float bw = tw + padX * 2, bh = th + padY * 2;
            bool group = who != null && who.group;
            float nameH = group ? 18 : 0;
            float left = mine ? width - 16 - avatar - gap - bw : 16 + avatar + gap;
            if (!mine) Avatar(scrollContent, MessageSprite(m, who), Initial(who), who != null ? who.color : C.Muted, new Vector2(16, -y), avatar);
            else Avatar(scrollContent, meSprite, "我", C.Mine, new Vector2(width - 16 - avatar, -y), avatar);
            if (group && !mine && HasSenderLabel(body))
            {
                int end = body.IndexOf(']');
                Text(Rect("Sender", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - 16), new Vector2(left + 200, -y)), body.Substring(1, end - 1), 12, C.Muted, TextAlignmentOptions.MidlineLeft);
                body = body.Substring(end + 1).TrimStart();
                probe.text = BubbleText(body, focusMarkup);
                pref = probe.GetPreferredValues(BubbleText(body, focusMarkup), maxBubble - padX * 2, 0);
                tw = Mathf.Min(maxBubble - padX * 2, Mathf.Max(18, pref.x + 2));
                th = probe.GetPreferredValues(BubbleText(body, focusMarkup), tw, 0).y;
                bw = tw + padX * 2; bh = th + padY * 2;
            }
            else nameH = 0;
            var bubble = Rect("Bubble", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - nameH - bh), new Vector2(left + bw, -y - nameH));
            var shape = bubble.gameObject.AddComponent<YYRoundRect>();
            shape.radius = 8; shape.color = mine ? C.Mine : C.Theirs;
            if (!mine) { shape.border = 1; shape.borderColor = C.Line; }
            YYGirlfriend.StyleBubble(m, shape, probe);
            bubble.SetSiblingIndex(probe.transform.GetSiblingIndex());
            var rt = probe.rectTransform;
            rt.offsetMin = new Vector2(left + padX, -y - nameH - padY - th); rt.offsetMax = new Vector2(left + padX + tw, -y - nameH - padY);
            if (!mine && m.from == YYChatHub.LingGuangId) Rating(m, left + bw + 8, -y - nameH - bh);
            return Mathf.Max(avatar, bh + nameH);
        }

        // ───────────── 灵光: 赞 / 踩 and its notes ─────────────

        bool LingGuangShown => hub != null && hub.S != null && hub.S.selected == YYChatHub.LingGuangId && hub.LingGuangTalk != null;

        /// <summary>Redraws 灵光's messages when a rating is given or its latest reply becomes rateable.</summary>
        string RatingStamp(string id, YYConversation conv)
        {
            if (id != YYChatHub.LingGuangId || hub.LingGuangTalk == null) return "";
            int rated = 0; bool open = false;
            foreach (var m in conv.messages) { if (m.rating != 0) rated++; else if (m.from == id && !open && hub.LingGuangTalk.CanRate(m)) open = true; }
            return ":r" + rated + (open ? "+" : "");
        }

        /// <summary>赞 / 踩 beside 灵光's latest reply (each is a tone card in the lab), or what was given before.</summary>
        void Rating(YYMessage m, float x, float bottom)
        {
            var talk = hub.LingGuangTalk;
            if (m.rating != 0)
            {
                Text(Rect("Rated", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, bottom), new Vector2(x + 90, bottom + 20)),
                    m.rating > 0 ? GameText.T("已赞", "Rated up") : GameText.T("已踩", "Rated down"), 12, m.rating > 0 ? C.Online : C.Badge, TextAlignmentOptions.BottomLeft);
                return;
            }
            if (talk == null || !talk.CanRate(m)) return;
            float w = GameText.IsEnglish ? 52 : 36;
            for (int i = 0; i < 2; i++)
            {
                bool up = i == 0;
                var b = FlatButton(scrollContent, up ? GameText.T("赞", "Up") : GameText.T("踩", "Down"), () => { talk.Rate(m, up); lastSignature = ""; dirty = true; }, 13, up ? C.Online : C.Badge, Color.white);
                var r = (RectTransform)b.transform;
                r.anchorMin = r.anchorMax = new Vector2(0, 1);
                r.offsetMin = new Vector2(x + i * (w + 6), bottom); r.offsetMax = new Vector2(x + i * (w + 6) + w, bottom + 24);
                b.name = up ? "RateUp" : "RateDown";
                var face = b.GetComponent<YYRoundRect>(); face.border = 1; face.borderColor = C.Line;
                UiTip.Add(r, () => GameText.T("给它这句回复打分：每一下都是一张语气卡，它的语气习惯也跟着学。", "Rate this reply: each one is a tone card, and its tone habits learn from it too."));
            }
        }

        // Exact sender + prewritten-phrase whitelist. byAi only excludes the AI speaking for the player;
        // it does not identify whether a contact's reply came from a script or a language model.
        static Sprite AuthoredPhoto(YYMessage message)
        {
            if (message == null || message.kind != YYKind.Text || message.byAi) return null;
            if (message.from == YYChatHub.GirlfriendId
                && (message.text == "[图片] 宿舍门口那只橘猫" || message.text == "[图片] the cat at the dorm gate"))
                return DesktopArt.Picture("chat_qingwen_cat");
            // DeliverEra writes these lines to the netbar group with a localized author prefix.
            if (message.from == YYChatHub.EraGroup
                && (message.text == "[阿杰] [图片：葛优躺] 今天的我。" || message.text == "[A-Jie] [Image: Ge You slouch] Me today."))
                return DesktopArt.Picture("chat_geyou");
            return null;
        }

        float PhotoBubble(float y, YYMessage message, YYContact who, float maxBubble, Sprite photo)
        {
            const float avatar = 36, left = 16 + avatar + 10, padding = 6;
            bool group = who != null && who.group;
            float nameHeight = group ? 18 : 0;
            string caption = message.text;
            if (group && HasSenderLabel(caption))
            {
                int end = caption.IndexOf(']');
                Text(Rect("Sender", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - 16), new Vector2(left + 240, -y)),
                    caption.Substring(1, end - 1), 12, C.Muted, TextAlignmentOptions.MidlineLeft);
                caption = caption.Substring(end + 1).TrimStart();
            }
            bool illustratedMeme = message.from == YYChatHub.EraGroup;
            float noteHeight = illustratedMeme ? 30 : 0;
            float photoWidth = Mathf.Min(240, maxBubble - padding * 2);
            float photoHeight = Mathf.Min(220, photoWidth * photo.rect.height / photo.rect.width);
            float height = photoHeight + 44 + noteHeight + padding * 2;
            Avatar(scrollContent, MessageSprite(message, who), Initial(who), who != null ? who.color : C.Muted, new Vector2(16, -y), avatar);
            var card = Rect("Authored Photo", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - nameHeight - height), new Vector2(left + photoWidth + padding * 2, -y - nameHeight));
            var shape = card.gameObject.AddComponent<YYRoundRect>();
            shape.radius = 8; shape.color = C.Theirs; shape.border = 1; shape.borderColor = C.Line;
            var picture = Rect("Photograph", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(padding, -padding - photoHeight), new Vector2(-padding, -padding));
            var image = picture.gameObject.AddComponent<Image>();
            image.sprite = photo; image.preserveAspect = true; image.raycastTarget = false;
            var captionLabel = Text(Rect("Caption", card, Vector2.zero, new Vector2(1, 0), new Vector2(10, 6 + noteHeight), new Vector2(-10, 44 + noteHeight)),
                caption, 14, C.Ink, TextAlignmentOptions.MidlineLeft);
            captionLabel.richText = false; captionLabel.enableAutoSizing = true; captionLabel.fontSizeMin = 11; captionLabel.fontSizeMax = 14;
            if (illustratedMeme)
                Text(Rect("Illustration Notice", card, Vector2.zero, new Vector2(1, 0), new Vector2(10, 4), new Vector2(-10, 30)),
                    GameText.T("情景配图 · 非原影视截图", "Illustrative scene · not a film still"), 10, C.Muted, TextAlignmentOptions.MidlineLeft).richText = false;
            return height + nameHeight;
        }

        float FileCard(float y, YYMessage m, bool mine, YYContact who, float width)
        {
            const float avatar = 36, gap = 10, cw = 300, ch = 104;
            float left = mine ? width - 16 - avatar - gap - cw : 16 + avatar + gap;
            Avatar(scrollContent, mine ? meSprite : MessageSprite(m, who), mine ? "我" : Initial(who), mine ? C.Mine : who != null ? who.color : C.Muted, new Vector2(mine ? width - 16 - avatar : 16, -y), avatar);
            var card = Rect("File", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - ch), new Vector2(left + cw, -y));
            var shape = card.gameObject.AddComponent<YYRoundRect>(); shape.radius = 8; shape.color = Color.white; shape.border = 1; shape.borderColor = C.Line;
            var icon = Rect("Icon", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -60), new Vector2(50, -14));
            icon.gameObject.AddComponent<YYFileIcon>();
            Text(Rect("Ext", icon, Vector2.zero, Vector2.one, new Vector2(0, 2), new Vector2(0, -24)), "EXE", 9, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            Text(Rect("Name", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(62, -36), new Vector2(-12, -12)), m.file, 16, C.Ink, TextAlignmentOptions.MidlineLeft).fontStyle = FontStyles.Bold;
            var status = Text(Rect("Status", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(62, -58), new Vector2(-12, -38)), "", 12, C.Muted, TextAlignmentOptions.MidlineLeft);
            var track = Rect("Track", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(14, 46), new Vector2(-14, 50));
            Fill(track, new Color32(229, 234, 240, 255));
            var fill = Rect("Fill", track, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            Fill(fill, C.Mine);
            string label = m.fileState == YYFileState.Done ? Lang.T("打开") : m.fileState == YYFileState.Receiving ? Lang.T("接收中…") : Lang.T("接收");
            var button = FlatButton(card, label, () => { hub.AcceptFile(m); dirty = true; }, 14, Color.white, C.Mine);
            Place((RectTransform)button.transform, new Vector2(1, 0), new Vector2(-96, 10), new Vector2(-14, 38));
            button.interactable = m.fileState != YYFileState.Receiving;
            AddFocus(button.gameObject);
            float trackWidth = cw - 28;
            liveFiles.Add((m, fill, status, trackWidth));
            UpdateFile(m, fill, status, trackWidth);
            return ch;
        }

        void UpdateFile(YYMessage m, RectTransform fill, TMP_Text status, float width)
        {
            if (fill == null) return;
            float t = m.sizeMB <= 0 ? 1 : (float)(m.received / m.sizeMB);
            if (m.fileState == YYFileState.Done) t = 1;
            fill.offsetMax = new Vector2(width * t, 0);
            string size = m.sizeMB.ToString("0.0") + " MB";
            status.text = m.fileState == YYFileState.Done ? size + Lang.T(" · 已接收，在桌面上")
                : m.fileState == YYFileState.Receiving ? m.received.ToString("0.0") + " / " + size + " · " + YYChatHub.TransferMBps.ToString("0.0") + " MB/s"
                : size + Lang.T(" · 对方给你发送了文件");
        }

        // ───────────── actions ─────────────

        void Send()
        {
            if (input == null) return;
            string text = input.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            hub.Send(hub.S.selected, text);
            input.text = "";
            input.ActivateInputField();
            stickBottom = true; dirty = true;
        }

        void OnTool(string id)
        {
            if (id == "Emoji") { ToggleFacePicker(); return; }
            if (id == "Shake") { StartCoroutine(Shake()); return; }
            if (id == "History") { scroll.verticalNormalizedPosition = 1; stickBottom = false; return; }
            var conv = hub.Conversation(hub.S.selected);
            conv.messages.Add(new YYMessage { from = "system", kind = YYKind.System, text = Lang.T("本地版 YY 暂不支持这个功能。"), gameSeconds = hub.runtime.Sim.S.gameSeconds });
            dirty = true;
        }

        System.Collections.IEnumerator Shake()
        {
            var target = window.windowContainer;
            Vector2 origin = target.anchoredPosition;
            for (int i = 0; i < 10; i++)
            {
                target.anchoredPosition = origin + new Vector2((i % 2 == 0 ? 1 : -1) * 8, (i % 3 - 1) * 3);
                yield return new WaitForSecondsRealtime(.03f);
            }
            target.anchoredPosition = origin;
        }

        // ───────────── level (太阳等级) ─────────────

        RectTransform levelRow;
        int drawnLevel = -1;

        /// <summary>
        /// The 2016 online-time level: a star is one level, four stars a moon, four moons a sun, four suns a crown.
        /// "我" has used YY for years (level 30), and every two hours online in the game adds a level.
        /// </summary>
        public static int Level(double gameSeconds) => 30 + (int)(Math.Max(0, gameSeconds) / 7200);

        void DrawLevel()
        {
            if (levelRow == null || hub == null || hub.runtime == null || hub.runtime.Sim == null) return;
            int level = Level(hub.runtime.Sim.S.gameSeconds);
            if (level == drawnLevel) return;
            drawnLevel = level;
            foreach (Transform child in levelRow) Destroy(child.gameObject);
            int crowns = level / 64, suns = level % 64 / 16, moons = level % 16 / 4, stars = level % 4;
            float x = 0;
            void Icon(Color fill, Color? bite, float size)
            {
                var rt = Rect("Icon", levelRow, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, -size / 2), new Vector2(x + size, size / 2));
                var img = rt.gameObject.AddComponent<Image>(); img.sprite = LingGuangV05.Desktop.Story.PrologueDesk.Circle(); img.color = fill; img.raycastTarget = false;
                if (bite.HasValue)
                {
                    var b = Rect("Bite", rt, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-size * .2f, -size * .4f), new Vector2(size * .6f, size * .4f));
                    var bi = b.gameObject.AddComponent<Image>(); bi.sprite = img.sprite; bi.color = bite.Value; bi.raycastTarget = false;
                }
                x += size + 2;
            }
            for (int i = 0; i < crowns; i++) Icon(new Color32(255, 200, 40, 255), null, 15);
            for (int i = 0; i < suns; i++) Icon(new Color32(255, 140, 30, 255), null, 13);
            for (int i = 0; i < moons; i++) Icon(new Color32(255, 236, 140, 255), C.HeadBottom, 12);
            for (int i = 0; i < stars; i++) Icon(new Color32(255, 255, 210, 255), null, 7);
            LingGuangV05.Desktop.UiTip.Add(levelRow, () => GameText.T("等级 " + Level(hub.runtime.Sim.S.gameSeconds) + " · 在线越久等级越高（4 星 = 1 月亮，4 月亮 = 1 太阳）",
                "Level " + Level(hub.runtime.Sim.S.gameSeconds) + " · grows with time online (4 stars = 1 moon, 4 moons = 1 sun)"));
            if (levelRow.GetComponent<Image>() == null) { var hit = levelRow.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); }
        }

        // ───────────── helpers ─────────────

        double Latest(YYContact c)
        {
            var conv = hub.Conversation(c.id);
            return conv == null || conv.messages.Count == 0 ? double.NegativeInfinity : conv.messages[conv.messages.Count - 1].gameSeconds;
        }

        static string FileStates(YYConversation conv)
        {
            int s = 0; foreach (var m in conv.messages) if (m.kind == YYKind.File) s = s * 3 + (int)m.fileState;
            return s.ToString();
        }

        static string Preview(YYMessage m)
        {
            return PreviewLine(m); // YYChatView.Faces.cs: inline faces, stickers, never cuts a face code in half
        }

        /// <summary>Generated human identities; the AI keeps its narrative 8×8 「0」 (AiJoinsYy).</summary>
        Sprite SpriteFor(YYContact c)
        {
            if (c == null) return null;
            if (c.id == YYChatHub.LaoZhou) return laoZhouSprite;
            if (c.id == YYChatHub.LingGuangId) return LingGuangV05.Desktop.Story.AiJoinsYy.PixelZero();
            return DesktopArt.Avatar(c.id);
        }

        Sprite MessageSprite(YYMessage message, YYContact contact)
        {
            string body = message.text ?? "";
            if (contact != null && contact.group && HasSenderLabel(body))
                return DesktopArt.Avatar(body.Substring(1, body.IndexOf(']') - 1));
            return SpriteFor(contact);
        }

        static string Initial(YYContact c) { if (c == null) return "?"; string n = GameText.T(c.name, c.nameEn); return n.Length > 0 ? n.Substring(0, 1) : "?"; }

        void Avatar(RectTransform parent, Sprite sprite, string letter, Color color, Vector2 topLeft, float size, bool grey = false)
        {
            var holder = Rect("Avatar", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, topLeft.y - size), new Vector2(topLeft.x + size, topLeft.y));
            var circle = holder.gameObject.AddComponent<YYCircle>();
            circle.color = grey ? new Color32(190, 196, 206, 255) : color;
            if (sprite != null)
            {
                var mask = holder.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
                circle.color = Color.white;
                var img = Rect("Picture", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
                if (grey) img.color = new Color(.7f, .7f, .7f);
            }
            else Text(Rect("Letter", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), letter, size * .45f, Color.white, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            circle.raycastTarget = false;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent) { if (child.name == name) return child; var found = FindDeep(child, name); if (found != null) return found; }
            return null;
        }

        void RebuildChoices()
        {
            if (choiceStrip == null) return;
            string target = hub.S.selected;
            var choices = hub.ChoicesOf(target);
            bool on = choices != null && choices.Length > 0;
            string signature = on ? target + ":" + string.Join("|", choices) : "";
            choiceStrip.gameObject.SetActive(on);
            if (signature == shownChoices) return;
            shownChoices = signature;
            for (int i = choiceStrip.childCount - 1; i >= 0; i--) Destroy(choiceStrip.GetChild(i).gameObject);
            if (!on) return;
            float x = 10;
            foreach (var choice in choices)
            {
                string text = choice;
                var b = FlatButton(choiceStrip, text, () => { hub.Send(target, text); stickBottom = true; dirty = true; }, 14, Color.white, C.Mine);
                var rt = (RectTransform)b.transform;
                float width = 28 + text.Length * (GameText.IsEnglish ? 7.5f : 15f);
                rt.anchorMin = rt.anchorMax = new Vector2(0, .5f);
                rt.offsetMin = new Vector2(x, -13); rt.offsetMax = new Vector2(x + width, 13);
                b.name = "Choice";
                x += width + 10;
            }
        }

        void SetButton(string name, string label)
        {
            var b = FindDeep(root, name);
            if (b == null) return;
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null && t.text != label) t.text = label;
        }

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 offMin, Vector2 offMax) { rt.anchorMin = rt.anchorMax = anchor; rt.offsetMin = offMin; rt.offsetMax = offMax; }

        static Image Fill(RectTransform rt, Color color) { var img = rt.gameObject.AddComponent<Image>(); img.color = color; return img; }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        Button FlatButton(Transform parent, string label, UnityAction onClick, float size, Color ink, Color fill)
        {
            var rt = Rect("Button", parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var shape = rt.gameObject.AddComponent<YYRoundRect>(); shape.radius = 4; shape.color = fill;
            var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = shape;
            var cb = button.colors; cb.highlightedColor = new Color(.92f, .96f, 1f); cb.pressedColor = new Color(.8f, .86f, .95f); cb.disabledColor = new Color(.8f, .8f, .8f); button.colors = cb;
            button.onClick.AddListener(onClick);
            var t = Text(Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), label, size, ink, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        void AddFocus(GameObject target)
        {
            var focus = target.GetComponent<ChapterOneWindowFocus>() ?? target.AddComponent<ChapterOneWindowFocus>();
            focus.Window = window;
        }
    }
}
