using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// The broadband line on the home app's 用电账单 tab (节奏重做 §2.1): the current line, its speed and monthly fee,
    /// and an upgrade button named "BuyBroadband" (the guide's locator) showing the price and the new monthly fee, or
    /// why it is not on offer (<see cref="XgSim.BroadbandBlocker"/>). The home app's controls are authored in the scene,
    /// so this card is added in code under its meter panel, on an always-active object next to the router.
    /// </summary>
    public sealed class HomeBroadbandCard : MonoBehaviour
    {
        public const string ButtonName = "BuyBroadband";
        const string HomeApp = "home";
        static readonly Color Ink = new Color32(40, 44, 52, 255), Muted = new Color32(120, 126, 138, 255), Accent = new Color32(60, 140, 231, 255),
            Disabled = new Color32(200, 204, 210, 255), Soft = new Color32(242, 246, 252, 255);

        ChapterOneDesktopRouter router;
        XingGuangController lab;
        RectTransform card;
        TMP_Text title, body, buyLabel;
        Button buy;
        Image buyFace;
        TMP_FontAsset font;
        float next;

        static string T(string zh, string en) => GameText.T(zh, en);
        static string Yuan(double v) => "¥" + Math.Ceiling(v - 1e-9).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);

        public static void Install(ChapterOneDesktopRouter router)
        {
            if (router == null) return;
            var c = router.GetComponent<HomeBroadbandCard>() ?? router.gameObject.AddComponent<HomeBroadbandCard>();
            c.router = router;
        }

        void Update()
        {
            // Until the card exists, look every frame (the guide rings it a few frames after opening the app).
            if (card == null) { TryBuild(); if (card == null) return; }
            if (Time.unscaledTime < next || !card.gameObject.activeInHierarchy) return;
            next = Time.unscaledTime + .4f;
            Refresh();
        }

        void TryBuild()
        {
            var app = router != null ? router.Get(HomeApp)?.NativeApp : null;
            if (app == null || !app.IsInitialized || app.Bindings == null) return;
            GameObject page = null;
            foreach (var p in app.Bindings.pages) if (p != null && p.id == "power") page = p.page;
            if (page == null) return;
            var meter = Deep(page.transform, "NativeHouseholdMeter") as RectTransform;
            var parent = meter != null ? meter : page.transform as RectTransform;
            if (parent == null) return;
            font = PrologueDesk.CjkFont();

            // The meter picture keeps the top of its panel; the line takes the bottom.
            var art = meter != null ? Deep(meter, "MeterArt") as RectTransform : null;
            if (art != null)
            {
                art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
                art.offsetMin = new Vector2(0, -132); art.offsetMax = new Vector2(0, -4);
                var img = art.GetComponent<Image>(); if (img != null) img.preserveAspect = true;
            }
            card = PrologueDesk.Rect("Broadband", parent, Vector2.zero, new Vector2(1, 0), new Vector2(10, 10), new Vector2(-10, 122));
            PrologueDesk.Fill(card, Soft, true);
            title = Text(PrologueDesk.Rect("Title", card, new Vector2(0, 1), Vector2.one, new Vector2(12, -30), new Vector2(-12, -6)), 16, Ink);
            body = Text(PrologueDesk.Rect("Body", card, Vector2.zero, Vector2.one, new Vector2(12, 50), new Vector2(-12, -32)), 13, Muted);
            var b = PrologueDesk.Rect(ButtonName, card, Vector2.zero, new Vector2(1, 0), new Vector2(12, 8), new Vector2(-12, 44));
            buyFace = PrologueDesk.Fill(b, Accent, true);
            buy = b.gameObject.AddComponent<Button>();
            buy.targetGraphic = buyFace;
            buy.onClick.AddListener(Upgrade);
            buyLabel = Text(PrologueDesk.Rect("Label", b, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0)), 15, Color.white);
            buyLabel.alignment = TextAlignmentOptions.Center;
            UiTip.Add(card, "下载数据走家里的宽带：线越快，数据页的下载队列走得越快。月租每月 1 号从家里余额扣。灵光「数据」页顶部的「升级宽带」按钮买的是同一条线。",
                "Data comes down the home line: the faster it is, the faster the data page's queue moves. The fee is charged on the 1st of each month. The Upgrade line button at the top of 灵光's Data page buys the same line.");
            Refresh();
        }

        TMP_Text Text(RectTransform rt, float size, Color color)
        {
            var go = rt.gameObject;
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size; t.color = color; t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        static Transform Deep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        void Refresh()
        {
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            var sim = lab != null ? lab.Sim : null;
            if (sim == null)
            {
                title.text = T("宽带 · ADSL 20M", "Broadband · 20M ADSL");
                body.text = T("灵光还没装好。", "灵光 is not installed yet.");
                SetButton(false, T("暂不能升级", "No upgrade yet"));
                return;
            }
            int tier = sim.BroadbandTier;
            bool en = GameText.IsEnglish;
            title.text = "<b>" + T("宽带 · ", "Broadband · ") + XgSim.BroadbandName(tier, en) + "</b>  <size=13><color=#787E8A>"
                + XgUi.N(sim.LineMBps, "0.#") + " MB/s · " + T("月租 ", "") + Yuan(XgSim.BroadbandMonthly[tier]) + T("", " a month") + "</color></size>";
            var why = sim.BroadbandBlocker(out string whyEn);
            if (why != null)
            {
                body.text = T(why, whyEn) + (sim.LineQueueSeconds > 0 ? T(" · 排着的下载还要 ", " · queue needs ") + XgSim.ClockText(sim.LineQueueSeconds) : "");
                SetButton(false, T(why, whyEn));
                return;
            }
            int up = tier + 1;
            double price = sim.NextBroadbandPrice;
            body.text = T("升级到 ", "Upgrade to ") + XgSim.BroadbandName(up, en) + "：" + XgUi.N(XgSim.BroadbandMBps[up], "0.#") + " MB/s"
                + T("，快 " + XgUi.N(XgSim.BroadbandMBps[up] / sim.LineMBps, "0") + " 倍，月租 ", ", " + XgUi.N(XgSim.BroadbandMBps[up] / sim.LineMBps, "0") + "× faster, ") + Yuan(XgSim.BroadbandMonthly[up]) + T("", " a month")
                + (sim.BroadbandBottleneck ? "\n<color=#DC3C32>" + T("现在宽带是瓶颈：排着的下载还要 ", "The line is the bottleneck: the queue needs ") + XgSim.ClockText(sim.LineQueueSeconds) + "</color>" : "");
            bool rich = lab.Host != null && lab.Host.Money + 1e-9 >= price;
            SetButton(rich, rich ? T("升级 " + Yuan(price) + "（装机 + 首月）· 之后月租 " + Yuan(XgSim.BroadbandMonthly[up]), "Upgrade " + Yuan(price) + " (install + month 1) · then " + Yuan(XgSim.BroadbandMonthly[up]) + "/mo")
                : T("钱不够：升级要 " + Yuan(price), "Not enough: upgrade costs " + Yuan(price)));
        }

        void SetButton(bool ok, string text)
        {
            buy.interactable = ok;
            buyFace.color = ok ? Accent : Disabled;
            buyLabel.text = text;
            buyLabel.color = ok ? Color.white : Ink;
        }

        void Upgrade()
        {
            var sim = lab != null ? lab.Sim : null;
            if (sim == null || lab.Host == null) return;
            sim.BuyBroadband(lab.Host);
            next = 0;
            Refresh();
        }
    }
}
