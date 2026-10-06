using System.Collections.Generic;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Mail
{
    /// <summary>
    /// 邮件 as an ordinary 2016 inbox (the labelling game moved into 灵光). Message list on the left, reading pane on the
    /// right. Flavour only: nothing here changes game state. Read marks last for the session.
    /// </summary>
    public sealed class MailInboxView : MonoBehaviour, IDesktopAppView
    {
        sealed class Mail
        {
            public string from, fromEn, subject, subjectEn, body, bodyEn, date;
            public bool read, spam, endingOnly;
        }

        static readonly Color Page = new Color32(246, 248, 251, 255), Line = new Color32(220, 226, 235, 255), Ink = new Color32(34, 40, 52, 255),
            Muted = new Color32(125, 135, 150, 255), Accent = new Color32(33, 118, 210, 255), Selected = new Color32(222, 236, 252, 255);

        readonly List<Mail> mails = new List<Mail>
        {
            new Mail { from = "投稿平台（故事内）", fromEn = "Submission platform (fiction)", date = "次日", endingOnly = true,
                subject = "关于您的预印本提交：退回通知", subjectEn = "Your preprint submission: returned",
                body = "作者您好：\n\n本次提交未进入展示流程。\n\n反馈：没有循环的序列模型不可能工作。作者似乎不是本领域研究者。\n\n——投稿系统\n\n（以上为游戏内虚构反馈，不代表 arXiv 的实际审核制度；没有向外部平台发送任何内容。）",
                bodyEn = "Dear author,\n\nThis submission was not accepted for display.\n\nFeedback: A sequence model without recurrence cannot work. The authors appear to be outside this field.\n\n— Submission system\n\n(This is fictional game feedback, not arXiv's actual moderation policy. Nothing was sent to an external platform.)" },
            new Mail { from = "老周", fromEn = "Lao Zhou", date = "5月24日", subject = "电脑别关", subjectEn = "Leave the computer on",
                body = "装好" + AppNames.AppZh + "以后电脑别关机，它在后台学。\n\n电费我月底跟你算，显卡要是不够就去「寻宝」看看二手的，别买新的，贵。\n\n——老周\n（手机发的，有错字别管）",
                bodyEn = "Once " + AppNames.AppEn + " is installed, leave the computer on: it learns in the background.\n\nWe'll settle the power bill at the end of the month. If you need GPUs, look for second-hand ones in the shop. New ones are expensive.\n\n— Lao Zhou" },
            new Mail { from = "12306", fromEn = "12306", date = "5月28日", subject = "【铁路客服】您已成功购买车票", subjectEn = "[Railway] Ticket purchase confirmed",
                body = "尊敬的旅客：\n\n您已成功购买 6 月 18 日 G1218 次列车车票，二等座 07 车 12F 号。请携带购票时使用的有效身份证件原件乘车。\n\n本邮件由系统自动发送，请勿回复。",
                bodyEn = "Dear passenger,\n\nYour ticket for train G1218 on 18 June is confirmed: second class, car 07, seat 12F. Bring the ID you booked with.\n\nThis is an automated message; do not reply." },
            new Mail { from = "淘宝网", fromEn = "Taobao", date = "5月28日", subject = "您关注的「亮影 1080 8G」降价啦", subjectEn = "Price drop: the 8 GB GTX-class card you watched",
                body = "亲，您收藏的宝贝降价了！\n\n亮影 1080 8G 公版  ¥5299 → ¥5199\n\n库存紧张，先到先得。\n\n（此邮件为系统推送，退订请回复 TD）",
                bodyEn = "Good news! An item you saved is cheaper.\n\n8 GB flagship card  ¥5299 → ¥5199\n\nLimited stock.\n\n(Automated; reply TD to unsubscribe)" },
            new Mail { from = "教务处", fromEn = "Academic office", date = "5月27日", subject = "关于暑期科研实习报名的通知", subjectEn = "Summer research internship sign-up",
                body = "各位同学：\n\n今年暑期科研实习新增「机器学习」方向，名额 6 人。有 GPU 使用经验者优先。\n\n报名截止 6 月 10 日，请将简历发送至本邮箱。",
                bodyEn = "Students,\n\nThis summer's research internships add a machine learning track with 6 places. GPU experience preferred.\n\nApply by 10 June by replying with your CV." },
            new Mail { from = "幸运抽奖中心", fromEn = "Lucky Draw Center", date = "5月27日", subject = "恭喜！您获得 iPhone 6s 一部！！", subjectEn = "Congratulations! You won an iPhone 6s!!", spam = true,
                body = "恭喜您在本次活动中被抽中，获得 iPhone 6s 一部！\n\n请先支付运费 ¥39 至以下账户……\n\n（这封邮件看起来像垃圾邮件。）",
                bodyEn = "Congratulations, you have been selected to receive an iPhone 6s!\n\nPlease first pay ¥39 shipping to the account below…\n\n(This looks like spam.)" },
            new Mail { from = "网吧老板", fromEn = "Net café owner", date = "5月26日", subject = "网费", subjectEn = "Your tab",
                body = "上次通宵的网费还差 15，有空过来带上。\n\n另外店里新到了几台 1080，来试试。",
                bodyEn = "You still owe 15 from the last all-nighter. Bring it next time.\n\nAlso, we just got some new flagship cards in. Come try them." },
        };

        WindowManagerRef window;
        TMP_FontAsset font;
        RectTransform root, list, reader;
        TMP_Text subject, meta, body;
        readonly List<(Mail mail, RectTransform row)> rows = new List<(Mail, RectTransform)>();
        Mail current;
        bool built, rejectionShown;
        ChapterOneRuntime runtime;

        sealed class WindowManagerRef { public Michsky.DreamOS.WindowManager w; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.TestMode) return;
            var router = runtime.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            var binding = router != null ? router.Get("mail") : null;
            if (binding == null || binding.Window == null || binding.Window.windowContainer == null) return;
            var content = binding.Window.windowContainer.Find("Content") as RectTransform;
            if (content == null) return;
            LingGuangV05.Desktop.XingGuang.XingGuangController.KeepOpenOnFirstStart(binding.Window);
            TMP_FontAsset font = null;
            var shop = router.Get("xunbao");
            if (shop != null && shop.Content != null) foreach (var t in shop.Content.GetComponentsInChildren<TMP_Text>(true)) { font = t.font; break; }
            foreach (var old in content.GetComponents<MonoBehaviour>()) if (old.GetType().Name == "ChapterOneApp") old.enabled = false;
            for (int i = content.childCount - 1; i >= 0; i--) content.GetChild(i).gameObject.SetActive(false);
            foreach (var label in binding.Window.windowContainer.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.transform.IsChildOf(content)) continue;
                if (label.name != "ChapterOneTitle" && label.name != "Status") continue;
                var localized = label.GetComponent<DesktopLocalizedText>();
                if (localized != null) Destroy(localized);
                label.text = label.name == "Status" ? Lang.T("收件箱 · 本地邮件") : Lang.T("邮件  —  收件箱");
            }
            var view = content.gameObject.AddComponent<MailInboxView>();
            view.window = new WindowManagerRef { w = binding.Window };
            view.runtime = runtime;
            view.font = font;
            binding.UseCustomView(view);
        }

        public bool EnsureReady() { if (!built) Build(); return true; }

        public void OnOpened(string tab)
        {
            var runtimeHost = FindAnyObjectByType<LingGuangV05.Desktop.XingGuang.XingGuangController>();
            LingGuangV05.Desktop.XingGuang.XingGuangController.EnsureShown(runtimeHost, window.w);
            Refresh();
        }

        void Build()
        {
            built = true;
            var bg = GetComponent<Image>(); if (bg != null) bg.color = Page;
            root = Rect("Inbox", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Fill(root, Page);
            var left = Rect("List", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(380, 0));
            Fill(left, Color.white);
            Fill(Rect("Divider", root, Vector2.zero, new Vector2(0, 1), new Vector2(379, 0), new Vector2(380, 0)), Line);
            Text(Rect("Folder", left, new Vector2(0, 1), Vector2.one, new Vector2(18, -50), new Vector2(-18, -10)), "", 20, Ink, TextAlignmentOptions.MidlineLeft).name = "Folder";
            list = Rect("Rows", left, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -56));
            for (int i = 0; i < mails.Count; i++)
            {
                var m = mails[i];
                var row = Rect("Mail" + i, list, new Vector2(0, 1), Vector2.one, new Vector2(0, -(i + 1) * 76), new Vector2(0, -i * 76));
                var img = Fill(row, new Color(0, 0, 0, 0));
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                b.onClick.AddListener(() => { current = m; m.read = true; Refresh(); });
                Fill(Rect("Line", row, Vector2.zero, new Vector2(1, 0), new Vector2(16, 0), new Vector2(-16, 1)), Line);
                Text(Rect("From", row, new Vector2(0, 1), Vector2.one, new Vector2(30, -32), new Vector2(-90, -10)), "", 15, Ink, TextAlignmentOptions.MidlineLeft).name = "From";
                Text(Rect("Date", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-100, -32), new Vector2(-16, -10)), m.date, 12, Muted, TextAlignmentOptions.MidlineRight).name = "Date";
                Text(Rect("Subject", row, new Vector2(0, 1), Vector2.one, new Vector2(30, -60), new Vector2(-16, -36)), "", 13, Muted, TextAlignmentOptions.MidlineLeft).name = "Subject";
                var dot = Rect("Unread", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -25), new Vector2(20, -17));
                dot.gameObject.AddComponent<YYCircle>().color = Accent;
                rows.Add((m, row));
            }
            reader = Rect("Reader", root, Vector2.zero, Vector2.one, new Vector2(380, 0), Vector2.zero);
            subject = Text(Rect("Subject", reader, new Vector2(0, 1), Vector2.one, new Vector2(30, -70), new Vector2(-30, -20)), "", 22, Ink, TextAlignmentOptions.BottomLeft);
            subject.fontStyle = FontStyles.Bold;
            meta = Text(Rect("Meta", reader, new Vector2(0, 1), Vector2.one, new Vector2(30, -100), new Vector2(-30, -74)), "", 13, Muted, TextAlignmentOptions.MidlineLeft);
            Fill(Rect("Rule", reader, new Vector2(0, 1), Vector2.one, new Vector2(30, -112), new Vector2(-30, -111)), Line);
            body = Text(Rect("Body", reader, Vector2.zero, Vector2.one, new Vector2(30, 20), new Vector2(-30, -126)), "", 16, Ink, TextAlignmentOptions.TopLeft);
            body.lineSpacing = 12;
            current = mails[1];
            foreach (var s in GetComponentsInChildren<Selectable>(true)) AddFocus(s.gameObject);
            AddFocus(gameObject);
            GameText.Changed += Refresh;
        }

        void OnDestroy() { GameText.Changed -= Refresh; }

        bool RejectionAvailable => runtime != null && runtime.Sim != null && runtime.Sim.S.story.GetFlag("rejection_mail") > 0;
        void Update()
        {
            if (built && rejectionShown != RejectionAvailable)
            {
                rejectionShown = RejectionAvailable;
                current = rejectionShown ? mails[0] : mails[1];
                Refresh();
            }
        }

        void Refresh()
        {
            if (!built) return;
            rejectionShown = RejectionAvailable;
            if (current != null && current.endingOnly && !rejectionShown) current = mails[1];
            int unread = 0; foreach (var m in mails) if (!m.read && (!m.endingOnly || rejectionShown)) unread++;
            var folder = root.Find("List/Folder");
            if (folder != null) folder.GetComponent<TMP_Text>().text = Lang.T("收件箱") + (unread > 0 ? "  <size=14><color=#2176D2>" + unread + Lang.T(" 封未读") + "</color></size>" : "");
            int visibleIndex = 0;
            foreach (var (m, row) in rows)
            {
                bool visible = !m.endingOnly || rejectionShown;
                row.gameObject.SetActive(visible);
                if (!visible) continue;
                row.offsetMin = new Vector2(0, -(visibleIndex + 1) * 76);
                row.offsetMax = new Vector2(0, -visibleIndex * 76);
                visibleIndex++;
                row.GetComponent<Image>().color = m == current ? Selected : new Color(0, 0, 0, 0);
                var from = row.Find("From").GetComponent<TMP_Text>();
                from.text = GameText.T(m.from, m.fromEn) + (m.spam ? Lang.T("  <size=11><color=#C0392B>[疑似垃圾]</color></size>") : "");
                from.fontStyle = m.read ? FontStyles.Normal : FontStyles.Bold;
                row.Find("Subject").GetComponent<TMP_Text>().text = GameText.T(m.subject, m.subjectEn);
                row.Find("Unread").gameObject.SetActive(!m.read);
            }
            if (current != null)
            {
                current.read = true;
                subject.text = GameText.T(current.subject, current.subjectEn);
                meta.text = Lang.T("发件人：") + GameText.T(current.from, current.fromEn) + "    " + current.date;
                body.text = GameText.T(current.body, current.bodyEn);
            }
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

        static Image Fill(RectTransform rt, Color color) { var img = rt.gameObject.AddComponent<Image>(); img.color = color; return img; }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.richText = true;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        void AddFocus(GameObject target)
        {
            var focus = target.GetComponent<ChapterOneWindowFocus>() ?? target.AddComponent<ChapterOneWindowFocus>();
            focus.Window = window.w;
        }
    }
}
