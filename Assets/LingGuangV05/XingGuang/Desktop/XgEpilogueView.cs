using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Bounded presentation UI: no particles, model calls, per-frame layout or economic time changes.</summary>
    public sealed partial class XgEpilogueView : MonoBehaviour
    {
        TMP_Text heading, body, date, error;
        XgBtn next, inbox, back, browse;
        Action nextAction, inboxAction, backAction, browseAction;
        public bool Showing => gameObject.activeSelf;
        public static XgEpilogueView Create(RectTransform parent, TMP_FontAsset font)
        {
            var root = XgUi.Rect("Epilogue", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var v = root.gameObject.AddComponent<XgEpilogueView>(); v.Build(root, font); root.gameObject.SetActive(false); return v;
        }
        void Build(RectTransform root, TMP_FontAsset font)
        {
            var ui = new XgUi(font, null, true);
            XgUi.Panel(root, new Color32(9, 14, 21, 255));
            date = ui.Text(XgUi.Rect("Date", root, new Vector2(0, 1), Vector2.one, new Vector2(32, -52), new Vector2(-32, -16)), "", 16, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            heading = ui.Text(XgUi.Rect("Heading", root, new Vector2(0, 1), Vector2.one, new Vector2(32, -120), new Vector2(-32, -58)), "", 28, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            body = ui.Text(XgUi.Rect("Body", root, Vector2.zero, Vector2.one, new Vector2(32, 132), new Vector2(-32, -132)), "", 19, XgDark.Ink, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true; body.fontSizeMin = 13; body.fontSizeMax = 19;
            error = ui.Text(XgUi.Rect("Status", root, Vector2.zero, new Vector2(1, 0), new Vector2(32, 80), new Vector2(-32, 124)), "", 15, XgDark.Bad, TextAlignmentOptions.MidlineLeft);
            next = Button(ui, root, 0, .32f, AdvancePresentation);
            inbox = Button(ui, root, 0, .64f, () => inboxAction?.Invoke());
            back = Button(ui, root, .66f, 1, () => backAction?.Invoke());
            browse = Button(ui, root, .33f, .65f, () => browseAction?.Invoke());
            BuildPresentation(root, font);
        }
        static XgBtn Button(XgUi ui, RectTransform root, float start, float end, Action callback)
        {
            var b = ui.Button(root, "", () => callback(), 15);
            b.rt.anchorMin = new Vector2(start, 0); b.rt.anchorMax = new Vector2(end, 0);
            b.rt.offsetMin = new Vector2(24, 24); b.rt.offsetMax = new Vector2(-24, 70);
            b.label.enableAutoSizing = true; b.label.fontSizeMin = 10; b.label.fontSizeMax = 15; return b;
        }
        public void Show(ReleaseOutcome release, EpilogueState ep, bool english, Action advance, Action openInbox, Action close, bool readOnly = false, Action browsePublic = null, bool reduceFx = false, XgEpilogueCard[] publicCards = null)
        {
            ResetPresentation();
            nextAction = advance; inboxAction = openInbox; backAction = close;
            browseAction = browsePublic;
            gameObject.SetActive(true); transform.SetAsLastSibling(); error.text = "";
            date.text = ep.historicalReview ? (english ? "HISTORICAL VIEW · 13 JUNE 2017" : "历史回看 · 2017 年 6 月 13 日")
                : (english ? "EPILOGUE · 13 JUNE 2017" : "尾声 · 2017 年 6 月 13 日");
            bool letter = ep.phase == "letter" || ep.phase == "reading";
            next.Show(!letter); inbox.Show(letter); back.Show(true);
            browse.Show(!letter && browsePublic != null && ep.phase != "date");
            browse.Set(english ? "Browse discussions" : "看看网络讨论", true);
            next.Set(english ? "Continue" : "继续", true);
            inbox.Set(readOnly ? (english ? "Read public letter" : "阅读公开来信") : (english ? "Public letters: save a local copy in the inbox" : "公开来信：保存本地副本到游戏邮箱"), true);
            back.Set(readOnly ? (english ? "Close replay" : "关闭回看") : (english ? "Back to desktop" : "先回桌面"), true);
            if (ep.phase == "date")
            {
                heading.text = english ? "Another morning" : "又一个早晨";
                body.text = english ? "The screen lights up. Outside, people are leaving for work.\n\nA new paper appeared yesterday. Today, there are still ordinary things to do."
                    : "屏幕亮起来。窗外，有人赶着上班。\n\n昨天，一篇新的论文发表。今天，日子仍有日子的事情。";
            }
            else if (ep.phase == "headline")
            {
                heading.text = english ? "A sequence model without recurrence" : "一种不依赖循环的序列模型";
                body.text = english ? "Historical record · Preprint first published: 2017-06-12 (UTC)\n\nA preprint titled Attention Is All You Need was made public on June 12. Its authors propose a sequence architecture built around attention and report results and training methods for machine translation. What work it might improve, and where its limits lie, are questions the discussion is only beginning to explore.\n\nSource: arXiv:1706.03762 v1"
                    : "公开史实 · 预印本首次公开：2017-06-12（UTC）\n\n一篇名为《Attention Is All You Need》的预印本于6月12日公开。研究团队提出主要依靠注意力机制处理序列的新架构，并报告了机器翻译实验中的结果与训练方式。它能把哪些工作做得更好，又有哪些地方尚不清楚，讨论才刚开始。\n\n来源：arXiv:1706.03762 v1";
            }
            else if (letter)
            {
                heading.text = english ? "Three ways, three public letters" : "三种做法，三封公开来信";
                body.text = english ? "Lao Zhou has published three letters for everyone to read. This computer recommends one first.\n\nSaving a local copy sends no email address and does not tell the author what you chose. Opening it does not complete the ending.\n\nYou can return to the desktop and continue later."
                    : "老周写了三封所有人都能读的公开来信。本机先推荐其中一封。\n\n保存的是公开文章的本地副本。不发送邮箱地址，也不会告诉作者你选择了什么。打开信件不会通关。\n\n你可以先回桌面，稍后继续。";
            }
            else
            {
                bool local = release.route == "local_only";
                heading.text = local ? (english ? "Local note · Only visible to you" : "本机备忘 · 仅自己可见")
                    : release.route == "online" ? (english ? "An open door" : "开着的门") : (english ? "Copies beyond your hands" : "离开手的副本");
                body.text = local ? (english ? "The files remain on this computer. No public announcement was made.\n\nThe world keeps moving. The questions left here can still be examined." : "文件留在这台电脑里，没有生成公开公告。\n\n世界继续向前。留在这里的问题，还可以接着检验。")
                    : release.route == "online" && release.launchNight?.ending == "power_cut"
                    ? (english ? "The power was cut during launch night. The service is stopped.\n\nOutside, another morning begins. The questions left on this computer are still here." : "上线夜里，你拔掉了电源。服务已经停止。\n\n窗外，又一个早晨开始了。留在这台电脑里的问题，还在这里。")
                    : release.route == "online" ? (english ? "In-game publication · The service page is open. Its capabilities, limitations and maintenance notes are there for visitors to read.\n\nAn open door also leaves room for questions." : "游戏内发布 · 服务页已经开放。能力、限制和维护说明都留在那里，供来访的人阅读。\n\n一扇开着的门，也给问题留着位置。")
                    : (english ? "In-game publication · The model download page and notes are available. The published terms describe its permitted uses.\n\nA copy may go further than its author can follow." : "游戏内发布 · 模型下载页与说明已经公开。页面上的条款写明允许的用途。\n\n一个副本，也许会去到作者跟不上的地方。");
            }
            ConfigurePresentation(ep, english, reduceFx, publicCards);
        }
        public void SetError(string message) { error.text = message ?? ""; }
        public void Hide() { ResetPresentation(); nextAction = inboxAction = backAction = browseAction = null; gameObject.SetActive(false); }
    }
}
