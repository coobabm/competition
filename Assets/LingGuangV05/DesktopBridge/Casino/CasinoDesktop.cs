using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Casino
{
    /// <summary>Local-only browser registration and midgame ad. Never opens a real URL.</summary>
    [DisallowMultipleComponent]
    public sealed class CasinoDesktop : MonoBehaviour
    {
        public static CasinoDesktop Instance { get; private set; }
        WebBrowserManager browser;
        WebBrowserLibrary original, library;
        GameObject template;
        StoryDesktopPresenter presenter;
        XingGuangController controller;
        RectTransform ad;
        bool english;
        Coroutine navigation;
        XgSim bound;

        public static CasinoDesktop Install(StoryDesktopPresenter host)
        {
            var c = host.GetComponent<CasinoDesktop>() ?? host.gameObject.AddComponent<CasinoDesktop>();
            c.presenter = host;
            c.Register();
            return c;
        }

        void Awake() { Instance = this; }
        static string T(string zh, string en) => GameText.T(zh, en);

        void Register()
        {
            if (library != null) return;
            browser = FindAnyObjectByType<WebBrowserManager>(FindObjectsInactive.Include);
            if (browser == null || browser.webLibrary == null) return;
            original = browser.webLibrary;
            library = Instantiate(original);
            library.name = original.name + " (local casino)";
            library.hideFlags = HideFlags.DontSave;
            // Copy the list explicitly: the asset on disk and other browser instances remain untouched.
            library.webPages = new List<WebBrowserLibrary.WebPage>(original.webPages);
            template = new GameObject("Casino Local Page Template", typeof(RectTransform));
            template.SetActive(false);
            template.transform.SetParent(transform, false);
            var rect = (RectTransform)template.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            template.AddComponent<CasinoPage>();
            foreach (string host in new[] { "888.vip", "www.888.vip" })
                foreach (string prefix in new[] { "", "http://", "https://" })
                    foreach (string suffix in new[] { "", "/" })
                        library.webPages.Add(new WebBrowserLibrary.WebPage { pageURL = prefix + host + suffix,
                            pageTitle = "888 VIP", pageContent = template, pageIcon = original.homePage.pageIcon, pageSize = .1f });
            browser.webLibrary = library;
        }

        public static bool OpenAddress(string address)
        {
            if (!CasinoRules.IsAddress(address) || Instance == null) return false;
            return Instance.OpenSite();
        }

        public bool OpenSite()
        {
            Register();
            if (browser == null || library == null) return false;
            if (navigation != null) StopCoroutine(navigation);
            navigation = StartCoroutine(Navigate());
            return true;
        }

        IEnumerator Navigate()
        {
            var window = browser.GetComponentInParent<WindowManager>(true);
            // DreamOS Start can close a never-opened window (disableAtStart). Warm it before the explicit open.
            if (!browser.gameObject.activeSelf) { browser.gameObject.SetActive(true); yield return null; }
            // Minimize leaves isOn=true; an explicit visit must restore and focus it too.
            if (window != null) window.OpenWindow();
            yield return null; // let the native browser initialize its first tab
            if (browser != null && browser.isActiveAndEnabled && browser.currentTabs.Count > 0) browser.OpenPage("888.vip");
            navigation = null;
        }

        bool Held()
        {
            var p = presenter != null ? presenter.GetComponent<PrologueDirector>() : null;
            return p != null && (p.Running || p.EndingPlaying) || presenter != null && presenter.NotificationsHeld
                || AutoLabelEpiphany.Playing || AiJoinsYy.Playing || LoveQuestionCutscene.Playing
                || presenter != null && presenter.GetComponent<OriginCurtain>() != null && presenter.GetComponent<OriginCurtain>().Playing;
        }

        void Update()
        {
            if (controller == null) controller = FindAnyObjectByType<XingGuangController>();
            if (controller == null || controller.Sim == null || controller.runtime == null || controller.runtime.Sim == null) return;
            var sim = controller.Sim;
            if (!ReferenceEquals(sim, bound)) { bound = sim; if (ad != null) Destroy(ad.gameObject); ad = null; }
            bool held = Held() || controller.runtime.Sim.InPrologue || !controller.runtime.Sim.AppInstalled || controller.runtime.TestMode;
            sim.AdvanceCasinoAd(Mathf.Min(Time.unscaledDeltaTime, 2), held);
            bool show = !held && sim.S.stage >= 3 && !sim.S.chapterComplete && sim.Casino.adVisible;
            if (!show) { if (ad != null) ad.gameObject.SetActive(false); return; }
            if (ad != null && english != GameText.IsEnglish) { Destroy(ad.gameObject); ad = null; }
            if (ad == null) BuildAd();
            if (ad != null)
            {
                ad.gameObject.SetActive(true);
                // Normal tray notices keep their own hit area and lifetime. Move the advert above them.
                var tray = PrologueDirector.Desk?.layer.Find("Tray Popup Area");
                bool trayShowing = tray != null && tray.gameObject.activeInHierarchy && tray.childCount > 0;
                float bottom = trayShowing ? 270 : 62;
                ad.offsetMin = new Vector2(-334, bottom); ad.offsetMax = new Vector2(-14, bottom + 190);
            }
        }

        void BuildAd()
        {
            var desk = PrologueDirector.Desk;
            if (desk == null) return;
            english = GameText.IsEnglish;
            // Independent from timed tray notifications: a miss is not a dismissal and blocks no other windows.
            ad = PrologueDesk.Rect("Casino Advert", desk.layer, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-334, 62), new Vector2(-14, 252));
            PrologueDesk.Fill(ad, new Color32(240, 191, 92, 255));
            var inner = PrologueDesk.Rect("Advert Content", ad, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            PrologueDesk.Fill(inner, new Color32(90, 8, 27, 255));
            LingGuangV05.Desktop.Media.DesktopMedia.Paint(inner, "casino");
            PrologueDesk.Fill(PrologueDesk.Rect("Copy shade", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0, 0, 0, .18f), false);
            desk.Text(PrologueDesk.Rect("Brand", inner, new Vector2(0, 1), Vector2.one, new Vector2(14, -35), new Vector2(-28, -5)), T("888国际娱乐", "888 VIP CLUB"), 21, CasinoPage.Gold, TextAlignmentOptions.MidlineLeft);
            var pitch = desk.Text(PrologueDesk.Rect("Pitch", inner, Vector2.zero, Vector2.one, new Vector2(14, 66), new Vector2(-14, -43)), T("缺钱吗？\n来两把试试手气", "Short on cash?\nTry your luck."), 22, Color.white, TextAlignmentOptions.Center);
            pitch.outlineWidth = .18f; pitch.outlineColor = new Color32(20, 10, 25, 230);
            var visit = PrologueDesk.Rect("Visit Casino", inner, Vector2.zero, new Vector2(1, 0), new Vector2(30, 30), new Vector2(-30, 62));
            var fill = PrologueDesk.Fill(visit, CasinoPage.Gold);
            var button = visit.gameObject.AddComponent<Button>(); button.targetGraphic = fill;
            button.onClick.AddListener(() => OpenSite()); // does NOT dismiss the persistent ad
            desk.Text(PrologueDesk.Rect("Text", visit, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("进入大厅  888.vip", "ENTER  888.vip"), 17, new Color32(60, 12, 20, 255), TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Rect("Notice", inner, Vector2.zero, new Vector2(1, 0), new Vector2(6, 3), new Vector2(-6, 26)), T("游戏内广告", "In-game advert"), 12, Color.white, TextAlignmentOptions.Center);
            var close = PrologueDesk.Rect("Close Casino Advert", ad, Vector2.one, Vector2.one, new Vector2(-20, -20), new Vector2(-2, -2));
            var img = PrologueDesk.Fill(close, new Color32(95, 37, 42, 255));
            var x = close.gameObject.AddComponent<Button>(); x.targetGraphic = img;
            var owner = bound;
            x.onClick.AddListener(() =>
            {
                if (controller == null || !ReferenceEquals(controller.Sim, owner)) return;
                owner.CloseCasinoAd(); controller.runtime.MarkDirty();
                if (ad != null) ad.gameObject.SetActive(false);
            });
            desk.Text(PrologueDesk.Rect("X", close, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "x", 12, Color.white, TextAlignmentOptions.Center);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (browser != null && browser.webLibrary == library) browser.webLibrary = original;
            if (library != null) Destroy(library);
            if (template != null) Destroy(template);
            if (ad != null) Destroy(ad.gameObject);
        }
    }
}
