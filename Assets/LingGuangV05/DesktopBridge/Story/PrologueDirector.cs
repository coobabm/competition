using System;
using System.Collections;
using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The prologue, design v1.1 §6 (24 May 2016). Plays on a new game until the opening setup (Step 8, in the exe):
    /// title card, boot, 0.txt, the hijacked mouse typing the long number, the picture download, the shutdown
    /// dialog with its voice, 0.txt's warning, 是 / 否, the picture renamed to .rar, WinRAR, the exe and sophon.dll,
    /// the poem, 0.txt deleting itself, and 老周's only unprompted message. Nothing is saved until Step 8.
    /// Afterwards it keeps the frozen files honest: the recycle bin spits them back and their properties are greyed.
    /// </summary>
    public sealed partial class PrologueDirector : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        ChapterOneDesktopRouter router;
        AeroLcdInputSurface surface;
        PrologueDesk desk;
        /// <summary>The desktop layer of the running game (tray popups, prologue windows); null until the desktop exists.</summary>
        public static PrologueDesk Desk { get; private set; }
        Canvas screen;
        RectTransform screenRoot;
        TMP_Text thought;
        CanvasGroup thoughtGroup;
        AudioSource voice;
        [NonSerialized] Coroutine run, thinking;
        [NonSerialized] ChapterOneSim playing;
        [SerializeField, HideInInspector] bool hijacked;

        RectTransform txtIcon, archiveIcon, dllIcon, recycleIcon, notepad, browser, menu, dialog;
        TMP_Text notepadText;
        bool txtOpened;
        int shutdownAnswer = -1;
        readonly List<GameObject> spawned = new List<GameObject>();

        static string L(string key) => PrologueContent.L(key);
        static string T(string zh, string en) => GameText.T(zh, en);

        public bool Running => run != null;
        public string Step { get; private set; } = "";

        void SetStep(string step) { Step = step; Debug.Log("[Prologue] " + step); }

        void OnEnable()
        {
            runtime = FindAnyObjectByType<ChapterOneRuntime>();
            router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
            surface = FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
            // Keep our own source across reloads; never borrow another cutscene's source.
            if (voice == null) voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            Release();
            playing = null;
        }

        void Update()
        {
            if (runtime == null || runtime.Sim == null || runtime.TestMode || router == null) return;
            if (desk == null && !BuildDesk()) return;
            EnsureRecycleBin();
            if (!ReferenceEquals(runtime.Sim, playing))
            {
                StopPrologue();
                playing = runtime.Sim;
                if (playing.InPrologue) run = StartCoroutine(Run());
            }
            if (!runtime.Sim.InPrologue)
            {
                var lab = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
                if (lab != null && lab.S.ending.Length > 0 && !runtime.Sim.S.endingPlayed && ending == null) ending = StartCoroutine(PlayEnding(lab));
                // A save that is past the prologue (loaded or finished mid-scene) never keeps a half-played one on screen.
                if (run != null && Step != "setup" && Step != "done") StopPrologue();
                EnsureFrozenFiles();
                KeepLaoZhouWaiting();
            }
        }

        void OnDisable()
        {
            StopPrologue();
            playing = null; // re-enabling must not wait on a coroutine that no longer exists
        }

        bool BuildDesk()
        {
            var lg = router.Get(LingGuangInstallFlow.AppId);
            if (lg == null || lg.DesktopShortcut == null) return false;
            var list = (RectTransform)lg.DesktopShortcut.transform.parent;
            var desktop = (RectTransform)list.parent;
            var apps = desktop.Find("Apps & Windows") as RectTransform ?? desktop;
            desk = new PrologueDesk(desktop, list, apps, PrologueDesk.CjkFont());
            Desk = desk;
            BuildScreen();
            return true;
        }

        /// <summary>Full-screen layer for the title card and the player's own thoughts (subtitles, not on the monitor).</summary>
        void BuildScreen()
        {
            // Sweep legacy duplicates as well as avoiding new ones. Only our named screen in this scene is owned here.
            foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (candidate.name != "LingGuang Prologue Screen" || candidate.gameObject.scene != gameObject.scene) continue;
                if (screen == null) screen = candidate;
                else if (candidate != screen) PrologueDesk.RemoveTransient(candidate.gameObject);
            }
            var go = screen != null ? screen.gameObject
                : new GameObject("LingGuang Prologue Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                PrologueDesk.RemoveTransient(go.transform.GetChild(i).gameObject);
            screen = go.GetComponent<Canvas>();
            screen.renderMode = RenderMode.ScreenSpaceOverlay;
            screen.sortingOrder = 990;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            screenRoot = (RectTransform)go.transform;
            var box = PrologueDesk.Rect("Thought", screenRoot, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-640, 40), new Vector2(640, 130));
            PrologueDesk.Fill(box, new Color(.02f, .03f, .05f, .78f), false);
            thoughtGroup = box.gameObject.AddComponent<CanvasGroup>();
            thoughtGroup.alpha = 0; thoughtGroup.blocksRaycasts = false;
            thought = desk.Text(PrologueDesk.Rect("Text", box, Vector2.zero, Vector2.one, new Vector2(30, 6), new Vector2(-30, -6)), "", 28, new Color(.95f, .95f, .9f), TextAlignmentOptions.Center);
        }

        void OnDestroy()
        {
            Release();
            if (ReferenceEquals(Desk, desk)) Desk = null;
            if (screen != null) PrologueDesk.RemoveTransient(screen.gameObject);
        }

        // ───────────── control of the mouse ─────────────

        void Hijack(bool on)
        {
            hijacked = on;
            if (surface != null) surface.SetInputEnabled(!on);
            UnityEngine.Cursor.visible = !on;
            if (on) desk.ShowCursor(RealPointer() ?? Vector2.zero);
            else desk.HideCursor();
        }

        void Release()
        {
            // Do not unlock another cutscene's input. Ownership survives reload and is released on disable.
            if (!hijacked) return;
            hijacked = false;
            if (surface != null) surface.SetInputEnabled(true);
            UnityEngine.Cursor.visible = true;
            desk?.HideCursor();
        }

        /// <summary>The player's mouse in the desktop layer, if it is over the monitor.</summary>
        Vector2? RealPointer()
        {
            if (surface == null || Mouse.current == null || desk == null) return null;
            if (!surface.TryMap(Mouse.current.position.ReadValue(), out var px)) return null;
            var tex = surface.sourceCamera != null ? surface.sourceCamera.targetTexture : null;
            float w = tex != null ? tex.width : 1920, h = tex != null ? tex.height : 1080;
            var size = desk.layer.rect.size;
            return new Vector2(px.x / w * size.x - size.x / 2, px.y / h * size.y - size.y / 2);
        }

        // ───────────── thoughts ─────────────

        void Think(string text, float hold = 3.5f)
        {
            if (thinking != null) { StopCoroutine(thinking); VoiceLines.Stop(); }
            thinking = StartCoroutine(Thinking(text, hold));
        }

        IEnumerator Thinking(string text, float hold)
        {
            thought.text = text; thought.maxVisibleCharacters = 0;
            thought.ForceMeshUpdate();
            float spoken = VoiceLines.Play(text), started = Time.unscaledTime;
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { thoughtGroup.alpha = t / .2f; yield return null; }
            thoughtGroup.alpha = 1;
            int length = thought.GetParsedText().Length;
            for (int i = 0; i <= length; i++) { thought.maxVisibleCharacters = i; yield return PrologueDesk.Wait(.035f); }
            yield return PrologueDesk.Wait(Mathf.Max(hold, spoken - (Time.unscaledTime - started) + .3f));
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime) { thoughtGroup.alpha = 1 - t / .3f; yield return null; }
            thoughtGroup.alpha = 0;
            thinking = null;
        }

        IEnumerator ThinkAndWait(string text, float hold = 2.5f)
        {
            Think(text, hold);
            while (thinking != null) yield return null;
        }

        // ───────────── the prologue ─────────────

        IEnumerator Run()
        {
            SetStep("title");
            yield return TitleAndBoot();
            StartCoroutine(desk.Balloon(this, L("who_360"), L("bubble_boot"), 5));
            yield return PrologueDesk.Wait(2.5f);

            // Before Step 1: 360 finds it, and it will not let the player say 是.
            SetStep("virus");
            yield return VirusAlert();
            yield return PrologueDesk.Wait(1.2f);

            // Step 1: 0.txt is there.
            SetStep("txt");
            txtOpened = false;
            txtIcon = Spawn(desk.Icon("Prologue 0.txt", Prologue.Txt, desk.notepadIcon));
            txtIcon.GetComponent<PrologueClick>().Open = () => txtOpened = true;
            Think(L("m_txt"));
            // Nothing happens until it is opened: a second thought after a while, and the icon wiggles now and then.
            for (float waited = 0, next = NudgeAfter; !txtOpened; waited += Time.unscaledDeltaTime)
            {
                if (waited >= next)
                {
                    if (next == NudgeAfter) Think(L("m_txt_again"));
                    StartCoroutine(Wiggle(txtIcon));
                    next += NudgeEvery;
                }
                yield return null;
            }

            // Step 2: it does not open. The mouse moves on its own.
            SetStep("hijack");
            Hijack(true);
            yield return PrologueDesk.Wait(.6f);
            var browserIconRt = desk.icons.Find("Web Browser") as RectTransform;
            yield return desk.Move(desk.LocalOf(browserIconRt), 1.1f);
            yield return desk.Click(2);
            var address = OpenBrowser();
            yield return PrologueDesk.Wait(.5f);
            yield return desk.Move(desk.LocalOf(address), .6f);
            yield return desk.Click();
            // The long number, one digit at a time, so the player can read it.
            yield return PrologueDesk.Type(address.GetComponentInChildren<TMP_Text>(), "", Prologue.LongNumber, .14f);
            yield return PrologueDesk.Wait(2.5f);
            var page = ShowGarbledPage();
            yield return PrologueDesk.Wait(1.2f);
            yield return desk.Move(desk.LocalOf(page, new Vector2(120, -60)), .8f);
            yield return desk.Click();
            var progress = StartDownload();
            StartCoroutine(desk.Balloon(this, L("who_360"), L("bubble_safe"), 5));
            yield return ThinkAndWait(L("m_hijack"), 1.5f);

            // Step 3: the player tries to take it back.
            SetStep("grab");
            yield return Struggle();
            yield return ThinkAndWait(Highlight(L("m_grab")), 1.2f);

            // Step 4: the word was heard. The rule makes it offer, say and stop.
            SetStep("shutdown");
            shutdownAnswer = -1;
            ShowShutdownDialog();
            var clip = PrologueContent.ShutdownVoice();
            if (clip != null) voice.PlayOneShot(clip);
            Hijack(false);
            OpenNotepad(L("txt_bios"));
            dialog.SetAsLastSibling(); // the question stays on top until it is answered
            Think(L("m_who"), 5);

            // Step 5: 是 / 否. Only 否 goes on.
            while (shutdownAnswer < 0) yield return null;
            if (shutdownAnswer == 1)
            {
                // 是: it really shuts down, and boots straight back into a desktop that already has the exe on it.
                yield return ShutDown();
                yield return WaitForSetup();
                run = null;
                yield break;
            }
            Close(ref dialog);

            // Step 6: the picture is an archive.
            SetStep("extract");
            Hijack(true);
            yield return FinishDownload(progress);
            Close(ref browser);
            archiveIcon = Spawn(desk.Icon("Prologue jpg", Prologue.Jpg, desk.photoIcon));
            yield return PrologueDesk.Wait(.5f);
            yield return desk.Move(desk.LocalOf(archiveIcon), 1f);
            yield return Rename();
            yield return desk.Click(2);
            yield return WinRarNag();
            yield return Extract();
            yield return PrologueDesk.Wait(.6f);
            Bring(notepad);
            notepadText.text = "";
            yield return PrologueDesk.Type(notepadText, "", L("txt_poem"), .11f);
            yield return ThinkAndWait(L("m_poem"), 1.5f);
            yield return PrologueDesk.Wait(2);
            Close(ref notepad);
            Despawn(ref txtIcon);
            Despawn(ref archiveIcon);
            Hijack(false);
            yield return ThinkAndWait(L("m_left"), 2);
            // The channel (design §1.1): from then only a few characters can be written back, so the thing that came
            // was small: the long number it typed was the whole program; the rest unpacked on this machine.
            yield return ThinkAndWait(L("m_number"), 2);
            // What to do next is said plainly: the exe it left behind (the to-do note shows the same step).
            StartCoroutine(Wiggle(ExeIcon()));
            Think(L("m_open_exe").Replace("{exe}", GameText.IsEnglish ? AppNames.ExeEn : AppNames.ExeZh), 3);

            // Step 8 happens in the exe (XgSetupOverlay). The prologue ends when the setup is saved. 老周's only
            // unprompted message (Step 7) comes afterwards, in its turn (LaoZhouInTurn).
            yield return WaitForSetup();
            run = null;
        }

        // ───────────── nudges ─────────────

        /// <summary>Seconds before a waiting prologue step nudges the player, then between nudges.</summary>
        const float NudgeAfter = 15, NudgeEvery = 12;

        RectTransform ExeIcon()
        {
            var shortcut = router != null ? router.Get(LingGuangInstallFlow.AppId)?.DesktopShortcut : null;
            return shortcut != null ? shortcut.transform as RectTransform : null;
        }

        /// <summary>Step 8 waits in the exe. While the exe is closed its icon wiggles now and then (a gentle rock, no flash).</summary>
        IEnumerator WaitForSetup()
        {
            SetStep("setup");
            float closedFor = 0, next = NudgeAfter;
            while (runtime.Sim != null && runtime.Sim.InPrologue)
            {
                var app = router != null ? router.Get(LingGuangInstallFlow.AppId) : null;
                if (app != null && app.Window != null && app.Window.isOn) { closedFor = 0; next = NudgeAfter; }
                else if ((closedFor += Time.unscaledDeltaTime) >= next)
                {
                    next += NudgeEvery;
                    StartCoroutine(Wiggle(ExeIcon()));
                }
                yield return null;
            }
            SetStep("done");
        }

        /// <summary>A desktop icon rocks a few degrees and settles (about 0.7 s).</summary>
        static IEnumerator Wiggle(RectTransform icon)
        {
            if (icon == null) yield break;
            var rest = icon.localRotation;
            for (float t = 0; t < .7f && icon != null; t += Time.unscaledDeltaTime)
            {
                icon.localRotation = rest * Quaternion.Euler(0, 0, Mathf.Sin(t * 26) * 8 * (1 - t / .7f));
                yield return null;
            }
            if (icon != null) icon.localRotation = rest;
        }

        IEnumerator TitleAndBoot()
        {
            Hijack(true);
            desk.HideCursor();
            var black = PrologueDesk.Rect("Title", screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(black, Color.black);
            var title = desk.Text(PrologueDesk.Centered("Chapter", black, new Vector2(0, 40), new Vector2(1400, 100)), L("title"), 64, new Color(.92f, .94f, .97f), TextAlignmentOptions.Center);
            var sub = desk.Text(PrologueDesk.Centered("Sub", black, new Vector2(0, -40), new Vector2(1400, 60)), L("title_sub"), 34, new Color(.56f, .72f, .87f), TextAlignmentOptions.Center);
            title.alpha = 0; sub.alpha = 0;
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime) { title.alpha = t / .8f; yield return null; }
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime) { sub.alpha = t / .8f; yield return null; }
            yield return PrologueDesk.Wait(1.8f);
            Destroy(title.gameObject); Destroy(sub.gameObject);

            // The fake boot runs on the monitor itself.
            var boot = PrologueDesk.Rect("Boot", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(boot, Color.black);
            var label = desk.Text(PrologueDesk.Centered("Text", boot, new Vector2(0, -120), new Vector2(800, 50)), L("boot"), 28, Color.white, TextAlignmentOptions.Center);
            var dots = new List<Image>();
            for (int i = 0; i < 4; i++) dots.Add(PrologueDesk.Fill(PrologueDesk.Centered("Dot", boot, new Vector2(-45 + i * 30, 0), new Vector2(18, 18)), new Color32(70, 160, 255, 255), false));
            var group = black.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < .6f; t += Time.unscaledDeltaTime) { group.alpha = 1 - t / .6f; yield return null; }
            Destroy(black.gameObject);
            for (float t = 0; t < 2.6f; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < dots.Count; i++) dots[i].color = new Color(.27f, .63f, 1, .35f + .65f * Mathf.Clamp01(Mathf.Sin(t * 6 - i * .8f)));
                yield return null;
            }
            label.text = "";
            Destroy(boot.gameObject);
            Hijack(false);
        }

        static string Highlight(string text)
        {
            return text.Replace("关机", "<color=#FFD24A>关机</color>").Replace("shut it down", "<color=#FFD24A>shut it down</color>");
        }

        /// <summary>Step 3: every tug of the real mouse moves the arrow a little, then it is pulled back.</summary>
        IEnumerator Struggle()
        {
            Vector2 anchor = desk.Cursor.anchoredPosition;
            int tugs = 0;
            float waited = 0;
            while (tugs < 3 && waited < 7)
            {
                Vector2 d = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
                if (d.magnitude > 3)
                {
                    desk.Cursor.anchoredPosition = anchor + Vector2.ClampMagnitude(d * 5, 110);
                    yield return PrologueDesk.Wait(.06f);
                    yield return desk.Move(anchor, .22f);
                    tugs++;
                    waited += .3f;
                }
                else { waited += Time.unscaledDeltaTime; yield return null; }
            }
        }

        // ───────────── windows of the prologue ─────────────

        RectTransform OpenBrowser()
        {
            var client = desk.Window("Prologue Browser", L("browser"), new Vector2(-40, 40), new Vector2(1180, 760), out browser);
            spawned.Add(browser.gameObject);
            var bar = PrologueDesk.Rect("Toolbar", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -48), Vector2.zero);
            PrologueDesk.Fill(bar, new Color32(236, 240, 246, 255));
            var address = PrologueDesk.Rect("Address", bar, Vector2.zero, Vector2.one, new Vector2(90, 9), new Vector2(-20, -9));
            PrologueDesk.Fill(address, Color.white);
            address.gameObject.AddComponent<Outline>().effectColor = new Color32(160, 170, 185, 255);
            var text = desk.Text(PrologueDesk.Rect("Url", address, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)), "", 19, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            desk.Text(PrologueDesk.Rect("Nav", bar, Vector2.zero, new Vector2(0, 1), new Vector2(12, 0), new Vector2(84, 0)), "←  →", 18, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
            return address;
        }

        RectTransform ShowGarbledPage()
        {
            var client = browser.Find("Client") as RectTransform;
            var page = PrologueDesk.Rect("Page", client, Vector2.zero, Vector2.one, new Vector2(30, 70), new Vector2(-30, -64));
            var rng = new System.Random(Prologue.LongNumber.GetHashCode());
            const string pool = "锟斤拷烫屯ãâ€œ¤§¶ÿþ鎴戠殑鏄剧崱鍦ㄥ摢閲�▒░▓█";
            var sb = new System.Text.StringBuilder();
            for (int line = 0; line < 14; line++)
            {
                int n = 30 + rng.Next(20);
                for (int i = 0; i < n; i++) sb.Append(pool[rng.Next(pool.Length)]);
                sb.Append('\n');
            }
            desk.Text(page, sb.ToString(), 20, new Color32(90, 90, 90, 255), TextAlignmentOptions.TopLeft);
            return page;
        }

        Image StartDownload()
        {
            var client = browser.Find("Client") as RectTransform;
            var strip = PrologueDesk.Rect("Download", client, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 58));
            PrologueDesk.Fill(strip, new Color32(240, 242, 245, 255));
            desk.Text(PrologueDesk.Rect("Name", strip, Vector2.zero, new Vector2(.5f, 1), new Vector2(16, 0), Vector2.zero), Prologue.Jpg, 18, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            var track = PrologueDesk.Rect("Track", strip, new Vector2(.5f, .5f), new Vector2(1, .5f), new Vector2(0, -7), new Vector2(-150, 7));
            PrologueDesk.Fill(track, new Color32(210, 214, 220, 255), false);
            var fill = PrologueDesk.Rect("Fill", track, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var img = PrologueDesk.Fill(fill, new Color32(80, 180, 90, 255), false);
            var status = desk.Text(PrologueDesk.Rect("Status", strip, new Vector2(1, 0), Vector2.one, new Vector2(-145, 0), new Vector2(-10, 0)), "", 15, PrologueDesk.Muted, TextAlignmentOptions.MidlineRight);
            StartCoroutine(Downloading(fill, status, .63f, 6));
            return img;
        }

        IEnumerator Downloading(RectTransform fill, TMP_Text status, float upTo, float seconds)
        {
            float start = fill.anchorMax.x;
            for (float t = 0; t < seconds && fill != null; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Lerp(start, upTo, t / seconds);
                fill.anchorMax = new Vector2(k, 1);
                status.text = (k * Prologue.JpgMegabytes).ToString("0.0") + " / " + Prologue.JpgMegabytes.ToString("0.0") + " MB";
                yield return null;
            }
        }

        IEnumerator FinishDownload(Image progress)
        {
            if (progress == null) yield break;
            var fill = progress.rectTransform;
            var status = fill.parent.parent.Find("Status").GetComponent<TMP_Text>();
            yield return Downloading(fill, status, 1, 1.4f);
            yield return PrologueDesk.Wait(.6f);
        }

        void ShowShutdownDialog()
        {
            var client = desk.Window("Prologue Shutdown", L("dialog_title"), Vector2.zero, new Vector2(520, 220), out dialog);
            spawned.Add(dialog.gameObject);
            var icon = PrologueDesk.Centered("Icon", client, new Vector2(-190, 30), new Vector2(48, 48));
            PrologueDesk.Fill(icon, new Color32(40, 100, 200, 255), false);
            desk.Text(PrologueDesk.Rect("Q", icon, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "?", 34, Color.white, TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Centered("Text", client, new Vector2(40, 30), new Vector2(380, 60)), L("dialog_shutdown"), 22, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            desk.Button(client, L("yes"), new Vector2(70, -55), new Vector2(110, 38), () => shutdownAnswer = 1);
            desk.Button(client, L("no"), new Vector2(195, -55), new Vector2(110, 38), () => shutdownAnswer = 0);
            // Closing the box is not an answer: it comes back until one is given.
            var close = dialog.Find("Title/Close").GetComponent<Button>();
            close.onClick.RemoveAllListeners();
        }

        void OpenNotepad(string text)
        {
            if (notepad == null)
            {
                var client = desk.Window("Prologue Notepad", Prologue.Txt + " - " + L("notepad"), new Vector2(420, 140), new Vector2(760, 420), out notepad, () => notepad = null);
                spawned.Add(notepad.gameObject);
                var menuBar = PrologueDesk.Rect("Menu", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -30), Vector2.zero);
                PrologueDesk.Fill(menuBar, new Color32(245, 246, 248, 255));
                desk.Text(PrologueDesk.Rect("Items", menuBar, Vector2.zero, Vector2.one, new Vector2(10, 0), Vector2.zero), Lang.T("文件(F)  编辑(E)  格式(O)  查看(V)  帮助(H)"), 15, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
                notepadText = desk.Text(PrologueDesk.Rect("Text", client, Vector2.zero, Vector2.one, new Vector2(14, 10), new Vector2(-14, -40)), "", 21, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            }
            Bring(notepad);
            notepadText.text = text;
        }

        void Bring(RectTransform window)
        {
            if (window == null) { OpenNotepad(""); window = notepad; }
            window.SetAsLastSibling();
        }

        IEnumerator ShutDown()
        {
            Close(ref dialog);
            var black = PrologueDesk.Rect("Shutdown", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(black, new Color32(10, 40, 90, 255));
            desk.Text(PrologueDesk.Centered("Text", black, Vector2.zero, new Vector2(800, 60)), L("shutting_down"), 30, Color.white, TextAlignmentOptions.Center);
            spawned.Add(black.gameObject);
            yield return PrologueDesk.Wait(2.5f);
            // 是 is not an ending, and it does not undo anything either: everything the SI left behind is cleared off
            // the screen, the machine boots again, and 灵光.exe and sophon.dll are simply there.
            Hijack(true);
            desk.HideCursor();
            Close(ref browser); Close(ref notepad);
            Despawn(ref txtIcon); Despawn(ref archiveIcon);
            foreach (Transform child in desk.windows) if (System.Array.IndexOf(ScriptedNames, child.name) >= 0) Destroy(child.gameObject);
            var label = black.GetComponentInChildren<TMP_Text>();
            PrologueDesk.Fill(black, Color.black);
            label.text = "";
            yield return PrologueDesk.Wait(1.5f);
            label.text = L("boot");
            label.rectTransform.anchoredPosition = new Vector2(0, -120);
            var dots = new List<Image>();
            for (int i = 0; i < 4; i++) dots.Add(PrologueDesk.Fill(PrologueDesk.Centered("Dot", black, new Vector2(-45 + i * 30, 0), new Vector2(18, 18)), new Color32(70, 160, 255, 255), false));
            for (float t = 0; t < 2.4f; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < dots.Count; i++) dots[i].color = new Color(.27f, .63f, 1, .35f + .65f * Mathf.Clamp01(Mathf.Sin(t * 6 - i * .8f)));
                yield return null;
            }
            runtime.Sim.InstallApp();
            EnsureFrozenFiles();
            Destroy(black.gameObject);
            Hijack(false);
            StartCoroutine(desk.Balloon(this, L("who_360"), L("bubble_boot"), 5));
            yield return ThinkAndWait(L("m_reboot"), 1.2f);
            router.Open(LingGuangInstallFlow.AppId);
            Think(L("m_reboot_exe").Replace("{exe}", GameText.IsEnglish ? AppNames.ExeEn : AppNames.ExeZh), 3);
        }


        IEnumerator Rename()
        {
            var at = desk.LocalOf(archiveIcon);
            yield return desk.Click();
            menu = PrologueDesk.Rect("Prologue Menu", desk.layer, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            menu.pivot = new Vector2(0, 1); menu.sizeDelta = new Vector2(170, 4 * 34 + 8); menu.anchoredPosition = at + new Vector2(10, -10);
            PrologueDesk.Fill(menu, new Color32(245, 245, 245, 255));
            menu.gameObject.AddComponent<Outline>().effectColor = new Color32(140, 140, 140, 255);
            spawned.Add(menu.gameObject);
            string[] keys = { "open", "rename", "delete", "properties" };
            RectTransform renameRow = null;
            for (int i = 0; i < keys.Length; i++)
            {
                var row = PrologueDesk.Rect(keys[i], menu, new Vector2(0, 1), Vector2.one, new Vector2(4, -4 - (i + 1) * 34), new Vector2(-4, -4 - i * 34));
                desk.Text(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(28, 0), Vector2.zero), L(keys[i]), 17, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
                if (keys[i] == "rename") renameRow = row;
            }
            desk.Cursor.SetAsLastSibling();
            yield return PrologueDesk.Wait(.5f);
            yield return desk.Move(desk.LocalOf(renameRow), .5f);
            PrologueDesk.Fill(renameRow, new Color32(200, 225, 250, 255), false);
            yield return desk.Click();
            Destroy(menu.gameObject); menu = null;

            // The name becomes editable; the extension goes, .rar comes.
            var title = archiveIcon.Find("Title").GetComponent<TMP_Text>();
            var edit = PrologueDesk.Centered("Edit", archiveIcon, new Vector2(0, -34), new Vector2(150, 44));
            PrologueDesk.Fill(edit, Color.white, false);
            var editText = desk.Text(PrologueDesk.Rect("Text", edit, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0)), Prologue.Jpg, 15, PrologueDesk.Ink, TextAlignmentOptions.Center);
            title.gameObject.SetActive(false);
            yield return PrologueDesk.Wait(.6f);
            string stem = Prologue.Jpg.Substring(0, Prologue.Jpg.Length - 4);
            for (int i = 3; i >= 0; i--) { editText.text = stem + ".jpg".Substring(0, i); yield return PrologueDesk.Wait(.15f); }
            yield return PrologueDesk.Type(editText, stem, ".rar", .18f);
            yield return PrologueDesk.Wait(.4f);
            Destroy(edit.gameObject);
            title.gameObject.SetActive(true);
            title.text = Prologue.Rar;
            var art = archiveIcon.Find("Icon") as RectTransform;
            Destroy(art.GetComponent<Image>());
            desk.DrawArchive(art);
            yield return PrologueDesk.Wait(.5f);
        }

        IEnumerator WinRarNag()
        {
            var client = desk.Window("Prologue WinRAR Nag", L("rar_trial_title"), new Vector2(0, 60), new Vector2(560, 230), out var nag);
            spawned.Add(nag.gameObject);
            desk.Text(PrologueDesk.Centered("Text", client, new Vector2(0, 30), new Vector2(480, 60)), L("rar_trial"), 21, PrologueDesk.Ink, TextAlignmentOptions.Center);
            desk.Button(client, L("buy"), new Vector2(-80, -55), new Vector2(130, 38), null);
            var close = desk.Button(client, L("close"), new Vector2(80, -55), new Vector2(130, 38), null);
            desk.Cursor.SetAsLastSibling();
            yield return PrologueDesk.Wait(.9f);
            // It knows where 关闭 is.
            yield return desk.Move(desk.LocalOf((RectTransform)close.transform), .45f);
            yield return desk.Click();
            Destroy(nag.gameObject);
        }

        IEnumerator Extract()
        {
            var client = desk.Window("Prologue WinRAR", Prologue.Rar + " - WinRAR", new Vector2(-120, 60), new Vector2(760, 380), out var rar);
            spawned.Add(rar.gameObject);
            var head = PrologueDesk.Rect("Head", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -34), Vector2.zero);
            PrologueDesk.Fill(head, new Color32(240, 240, 240, 255));
            desk.Text(PrologueDesk.Rect("Cols", head, Vector2.zero, Vector2.one, new Vector2(14, 0), Vector2.zero), Lang.T("名称                                    大小          类型"), 15, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
            string[] rows =
            {
                AppNames.ExeZh + "                         " + "18,544 KB" + "     " + L("type_exe"),
                Prologue.Dll + "                         " + "4 KB" + "            " + L("type_dll"),
            };
            if (GameText.IsEnglish) rows[0] = AppNames.ExeEn + "                         " + "18,544 KB" + "     " + L("type_exe");
            for (int i = 0; i < rows.Length; i++)
                desk.Text(PrologueDesk.Rect("Row", client, new Vector2(0, 1), Vector2.one, new Vector2(14, -34 - (i + 1) * 34), new Vector2(0, -34 - i * 34)), rows[i], 17, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            var button = desk.Button(client, L("extract"), new Vector2(250, -140), new Vector2(190, 40), null);
            desk.Cursor.SetAsLastSibling();
            yield return PrologueDesk.Wait(.8f);
            yield return desk.Move(desk.LocalOf((RectTransform)button.transform), .6f);
            yield return desk.Click();
            yield return PrologueDesk.Wait(.5f);
            // The two files land on the desktop: the exe is the real one, sophon.dll has no icon.
            runtime.Sim.InstallApp();
            // DreamOS may remember the app window as open from an earlier session; the player opens it (Step 8).
            var app = router.Get(LingGuangInstallFlow.AppId);
            if (app != null && app.Window != null && app.Window.isOn) app.CloseApp();
            EnsureFrozenFiles();
            yield return PrologueDesk.Wait(.6f);
            yield return desk.Move(desk.LocalOf(rar.Find("Title/Close") as RectTransform), .5f);
            yield return desk.Click();
            Destroy(rar.gameObject);
        }

        // ───────────── Step 7 ─────────────

        Coroutine laoZhou;
        /// <summary>He does not wait for 晴雯 longer than this after the AI joined YY, nor for anything longer than the second number.</summary>
        const float LaoZhouAfterAi = 75, LaoZhouPatience = 300;

        /// <summary>
        /// Step 7, 老周's only unprompted message, comes after the setup in its turn (OpeningBeat): after the month card,
        /// the AI joining YY and 晴雯's first line. A save quit before he wrote gets it on the next start, and one whose
        /// reply was cut short gets his choices back. Only before the second ability; never twice.
        /// </summary>
        float nextLaoZhouCheck;

        void KeepLaoZhouWaiting()
        {
            if (laoZhou != null || Time.unscaledTime < nextLaoZhouCheck) return;
            nextLaoZhouCheck = Time.unscaledTime + 1;
            if (laoZhou != null || runtime.Sim.InPrologue || runtime.Sim.S.prologue != 2 || !string.IsNullOrEmpty(runtime.Sim.S.laoZhouFirstReply)) return;
            var hub = LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
            var lab = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
            if (hub == null || hub.S == null || lab == null || lab.AbilitiesCount >= OpeningQuiet.EndsWithAbility || lab.S.ending.Length > 0) return;
            laoZhou = StartCoroutine(LaoZhouInTurn());
        }

        IEnumerator LaoZhouInTurn()
        {
            var presenter = GetComponent<StoryDesktopPresenter>();
            var turn = new OpeningTurn();
            float waited = 0, aiJoinedFor = 0;
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                waited += dt;
                if (OpeningSequence.AiJoined()) aiJoinedFor += dt;
                bool ready = turn.Ready(OpeningSequence.Turn(OpeningBeat.LaoZhou, presenter), Time.unscaledTime)
                    || aiJoinedFor > LaoZhouAfterAi || waited > LaoZhouPatience;
                bool busy = AiJoinsYy.Playing || presenter != null && presenter.NotificationsHeld;
                if (ready && !busy) break;
                yield return null;
            }
            yield return LaoZhou();
            laoZhou = null;
        }

        IEnumerator LaoZhou()
        {
            var hub = LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
            if (hub == null) yield break;
            const string zhou = LingGuangV05.Core.Forum.ForumLibrary.LaoZhou;
            string odd = L("lz_pick_odd"), yes = L("lz_pick_yes"), later = L("lz_pick_later");
            bool answered = false, described = false;
            string picked = null;
            // The forum icon lights up with a red dot: 周而复始's only unprompted message. A reply cut short gets his
            // choices back without the message twice; one already answered is only noted.
            switch (FirstMessageState(hub))
            {
                case 2: runtime.Sim.S.laoZhouFirstReply = "game"; runtime.MarkDirty(); yield break;
                case 0: hub.Receive(zhou, L("lz_first")); break;
            }
            hub.Offer(zhou, new[] { odd, yes, later });
            hub.Script(zhou, text =>
            {
                if (!answered)
                {
                    answered = true;
                    picked = text == yes ? "yes" : text == later ? "later" : text == odd || Odd(text) ? "odd" : "yes";
                    return true;
                }
                if (picked == "odd" && !described) { described = true; return true; }
                return false;
            });
            while (!answered) yield return null;
            runtime.Sim.S.laoZhouFirstReply = picked == "odd" ? "odd" : "game";
            runtime.MarkDirty();
            yield return PrologueDesk.Wait(1.6f);
            if (picked == "odd")
            {
                hub.Receive(zhou, L("lz_odd_1"), hub.Showing == zhou);
                // He waits for the player to describe it, but not forever.
                for (float t = 0; t < 45 && !described; t += Time.unscaledDeltaTime) yield return null;
                yield return PrologueDesk.Wait(2.2f);
                hub.Receive(zhou, L("lz_odd_2"), hub.Showing == zhou);
            }
            else
            {
                hub.Receive(zhou, L(picked == "later" ? "lz_later_1" : "lz_yes_1"), hub.Showing == zhou);
                yield return PrologueDesk.Wait(2.5f);
                hub.Receive(zhou, L("lz_game_2"), hub.Showing == zhou);
            }
            hub.EndScript();
        }

        /// <summary>0: his first message is not in the conversation; 1: it is, unanswered; 2: it is, and the player wrote back.</summary>
        static int FirstMessageState(LingGuangV05.Desktop.Tieba.TiebaHub hub)
        {
            var conv = hub.Conversation(LingGuangV05.Core.Forum.ForumLibrary.LaoZhou);
            if (conv == null) return 0;
            string zh = PrologueContent.Lines.Get("lz_first", false), en = PrologueContent.Lines.Get("lz_first", true);
            int at = conv.messages.FindLastIndex(m => m.from == LingGuangV05.Core.Forum.ForumLibrary.LaoZhou && (m.text == zh || m.text == en));
            if (at < 0) return 0;
            for (int i = at + 1; i < conv.messages.Count; i++) if (conv.messages[i].from == LingGuangV05.Core.Forum.ForumLibrary.Me) return 2;
            return 1;
        }

        static bool Odd(string text)
        {
            foreach (var w in new[] { "怪", "病毒", "鼠标", "中毒", "不了", "出事", "weird", "virus", "mouse" }) if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        // ───────────── the frozen files (§1.4) and the recycle bin ─────────────

        void EnsureRecycleBin()
        {
            if (recycleIcon != null) return;
            // Wait until the desktop grid has placed the native icons, or the free cell would be guessed wrong.
            var first = desk.icons.childCount > 0 ? (RectTransform)desk.icons.GetChild(0) : null;
            if (first == null || first.anchoredPosition == Vector2.zero || Time.unscaledTime < 1) return;
            recycleIcon = desk.Icon("Prologue Recycle Bin", L("recycle"), null, desk.DrawRecycleBin);
            recycleIcon.GetComponent<PrologueClick>().Open = OpenRecycleBin;
            GameText.Changed += RefreshLabels;
        }

        void RefreshLabels()
        {
            PrologueDesk.SetIconLabel(recycleIcon, L("recycle"));
        }

        void EnsureFrozenFiles()
        {
            if (runtime.Sim == null || !runtime.Sim.AppInstalled) return;
            // sophon.dll disappears at the ending (design v1.1 §1.4).
            bool gone = runtime.Sim.S.endingPlayed && ending == null;
            if (gone) { if (dllIcon != null) { Destroy(dllIcon.gameObject); dllIcon = null; } }
            else if (dllIcon == null)
            {
                dllIcon = desk.Icon("Prologue sophon.dll", Prologue.Dll, null, desk.DrawBlankFile);
                dllIcon.gameObject.AddComponent<ItemDragger>();
                var click = dllIcon.GetComponent<PrologueClick>();
                click.Menu = () => ShowProperties(false);
                click.Dropped = from => SpitBack(dllIcon, from);
            }
            var exe = router.Get(LingGuangInstallFlow.AppId)?.DesktopShortcut;
            if (exe != null && exe.GetComponent<PrologueClick>() == null)
            {
                var click = exe.gameObject.AddComponent<PrologueClick>();
                click.Menu = () => ShowProperties(true);
                var rt = (RectTransform)exe.transform;
                click.Dropped = from => SpitBack(rt, from);
            }
        }

        /// <summary>Dropped on the recycle bin: it comes straight back (the exe is frozen, the dll cannot be deleted).</summary>
        void SpitBack(RectTransform icon, Vector3 from)
        {
            if (icon == null || recycleIcon == null) return;
            var labNow = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
            if (icon != dllIcon && labNow != null && labNow.S.exeFree) return; // after the rules are written it is yours (§8 E-4)
            if ((desk.LocalOf(icon) - desk.LocalOf(recycleIcon)).magnitude > 80) return;
            StartCoroutine(Bounce(icon, from));
            // 智子锁死 (design v1.1 §10.2 #6): the file is held open by the monitor.
            if (icon != dllIcon) { Locked(T(AppNames.ExeZh, AppNames.ExeEn)); labNow?.EarnSecret("life.santi.sophon"); }
        }

        IEnumerator Bounce(RectTransform icon, Vector3 to)
        {
            Vector3 start = icon.localPosition;
            for (float t = 0; t < .35f && icon != null; t += Time.unscaledDeltaTime)
            {
                float k = t / .35f - 1;
                icon.localPosition = Vector3.LerpUnclamped(start, to, 1 + 2.70158f * k * k * k + 1.70158f * k * k); // ease out, slight overshoot
                yield return null;
            }
            if (icon == null) yield break;
            icon.localPosition = to;
            var dragger = icon.GetComponent<ItemDragger>();
            if (dragger != null && dragger.rememberPosition) dragger.UpdatePositionData();
        }

        void Locked(string file)
        {
            var client = desk.Window("Prologue File In Use", Lang.T("文件正在使用"), new Vector2(0, 40), new Vector2(520, 220), out var window);
            desk.Text(PrologueDesk.Rect("Message", client, Vector2.zero, Vector2.one, new Vector2(24, 64), new Vector2(-24, -20)),
                T("操作无法完成，因为文件已在 sophon.dll 中打开。\n\n关闭该文件并重试。\n" + file, "The action can't be completed because the file is open in sophon.dll.\n\nClose the file and try again.\n" + file), 17, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            desk.Button(client, Lang.T("重试"), new Vector2(60, -70), new Vector2(110, 34), () => Destroy(window.gameObject));
            desk.Button(client, T("取消", "Cancel"), new Vector2(185, -70), new Vector2(110, 34), () => Destroy(window.gameObject));
        }

        void OpenRecycleBin()
        {
            var client = desk.Window("Prologue Recycle Window", L("recycle"), new Vector2(-200, 120), new Vector2(620, 380), out var window);
            desk.Text(PrologueDesk.Rect("Empty", client, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20)), L("recycle_empty"), 19, PrologueDesk.Muted, TextAlignmentOptions.Center);
        }

        void ShowProperties(bool exe)
        {
            string file = exe ? T(AppNames.ExeZh, AppNames.ExeEn) : Prologue.Dll;
            var client = desk.Window("Prologue Properties", file + " " + L("properties"), new Vector2(160, 60), new Vector2(480, exe ? 520 : 430), out var window);
            long bytes = exe ? 18544L * 1024 : SophonBytes();
            string[,] rows =
            {
                { L("prop_type"), exe ? L("type_exe") + " (.exe)" : L("type_dll") + " (.dll)" },
                { L("prop_location"), L("desktop_path") },
                { L("prop_size"), (bytes / 1024.0).ToString("#,0.0") + " KB (" + bytes.ToString("#,0") + Lang.T(" 字节)") },
                { L("prop_desc"), exe ? T(AppNames.AppZh, AppNames.AppEn) : "" },
                // 三体 (design v1.1 §10.2 #2): the company is 红岸, the version is listener 1379's number.
                { Lang.T("公司"), exe ? Lang.T("红岸") : "" },
                { Lang.T("文件版本"), exe ? "1.3.7.9" : "" },
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                var row = PrologueDesk.Rect("Row", client, new Vector2(0, 1), Vector2.one, new Vector2(20, -24 - (i + 1) * 44), new Vector2(-20, -24 - i * 44));
                desk.Text(PrologueDesk.Rect("K", row, Vector2.zero, new Vector2(.3f, 1), Vector2.zero, Vector2.zero), rows[i, 0] + T("：", ":"), 17, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
                desk.Text(PrologueDesk.Rect("V", row, new Vector2(.3f, 0), Vector2.one, Vector2.zero, Vector2.zero), rows[i, 1], 17, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            }
            // Attributes: read-only is ticked and greyed — it cannot be changed (§1.4) — until the rules are written (§8 E-4).
            var attrs = PrologueDesk.Rect("Attributes", client, new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 80), new Vector2(-20, 130));
            var labNow = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
            Check(attrs, 0, L("readonly"), !(exe && labNow != null && labNow.S.exeFree));
            Check(attrs, 160, L("hidden"), false);
            desk.Button(client, L("ok"), new Vector2(160, exe ? -215 : -170), new Vector2(110, 36), () => Destroy(window.gameObject));
        }

        void Check(RectTransform parent, float x, string label, bool on)
        {
            var box = PrologueDesk.Rect("Box", parent, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, -10), new Vector2(x + 20, 10));
            PrologueDesk.Fill(box, new Color32(230, 230, 230, 255), false);
            box.gameObject.AddComponent<Outline>().effectColor = new Color32(170, 170, 170, 255);
            if (on) desk.Text(PrologueDesk.Rect("Tick", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "✓", 16, new Color32(150, 150, 150, 255), TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Rect("Label", parent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(x + 28, 0), new Vector2(x + 150, 0)), label, 16, new Color32(150, 150, 150, 255), TextAlignmentOptions.MidlineLeft);
        }

        long SophonBytes()
        {
            var lab = FindAnyObjectByType<LingGuangV05.Desktop.XingGuang.XingGuangController>();
            var sim = lab != null ? lab.Sim : null;
            return Prologue.SophonBytes(sim != null ? sim.S.stage : 0, sim != null ? sim.Board.S.cards : 0);
        }

        // ───────────── the ending on the desktop (§8 E-5) ─────────────

        Coroutine ending;
        /// <summary>The ending sequence (last words, the drop, 2017, the 360 bubble) is on screen.</summary>
        public bool EndingPlaying => ending != null;

        /// <summary>Its last words, sophon.dll turning into a drop of water and vanishing, 2017, and the last 360 bubble.</summary>
        IEnumerator PlayEnding(LingGuangV05.XingGuang.XgSim lab)
        {
            lab.AddLine("ai", lab.EndingWords());
            // E3: it wrote the rules itself and says why, from something that really happened in this save.
            string reason = lab.DelegateReason();
            if (reason.Length > 0) lab.AddLine("ai", reason);
            lab.AddLine("ai", lab.SeedLine());
            Think(lab.EndingWords(), 3);
            yield return PrologueDesk.Wait(4);
            if (reason.Length > 0) { Think(reason, 6); yield return PrologueDesk.Wait(7); }
            if (dllIcon != null)
            {
                // §10.2 #14: a silver drop for a second, then nothing.
                var art = dllIcon.Find("Icon") as RectTransform;
                for (int i = art.childCount - 1; i >= 0; i--) Destroy(art.GetChild(i).gameObject);
                var drop = PrologueDesk.Centered("Drop", art, Vector2.zero, new Vector2(34, 34));
                PrologueDesk.Fill(drop, new Color32(200, 210, 222, 255), false).sprite = PrologueDesk.Circle();
                PrologueDesk.SetIconLabel(dllIcon, "");
                yield return PrologueDesk.Wait(1.2f);
                Destroy(dllIcon.gameObject); dllIcon = null;
            }
            yield return PrologueDesk.Wait(2);
            GameCalendar.NewYear(runtime.Sim.S);
            var black = PrologueDesk.Rect("Title", screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(black, Color.black);
            var group = black.gameObject.AddComponent<CanvasGroup>();
            var date = desk.Text(PrologueDesk.Centered("Date", black, new Vector2(0, 60), new Vector2(1400, 90)), Lang.T("2017 年 1 月 1 日  00:00"), 54, new Color(.92f, .94f, .97f), TextAlignmentOptions.Center);
            var caption = desk.Text(PrologueDesk.Centered("Caption", black, new Vector2(0, -40), new Vector2(1500, 80)), lab.LetterCaption(), 28, new Color(.56f, .72f, .87f), TextAlignmentOptions.Center);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime) { group.alpha = t; yield return null; }
            yield return PrologueDesk.Wait(5);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime) { group.alpha = 1 - t; yield return null; }
            Destroy(black.gameObject);
            runtime.Sim.S.endingPlayed = true;
            runtime.MarkDirty();
            ending = null;
            // A quiet desktop first: no prompts, no tasks. Life starting again (360's boot counter) comes after.
            StartCoroutine(NewYearBalloon());
        }

        /// <summary>Seconds of quiet desktop after the ending before 360's New Year balloon.</summary>
        const float QuietAfterEnding = 25f;

        IEnumerator NewYearBalloon()
        {
            yield return PrologueDesk.Wait(QuietAfterEnding);
            yield return desk.Balloon(this, L("who_360"), Lang.T("新年快乐，您的电脑已连续开机 5424 小时。"), 8);
        }

        // ───────────── housekeeping ─────────────

        RectTransform Spawn(RectTransform rt) { spawned.Add(rt.gameObject); return rt; }

        void Despawn(ref RectTransform rt) { if (rt != null) Destroy(rt.gameObject); rt = null; }

        void Close(ref RectTransform window) { if (window != null) Destroy(window.gameObject); window = null; }

        /// <summary>Stops the prologue and removes everything it put on the desktop (a new or reloaded save starts clean).</summary>
        /// <summary>Windows and icons that only exist while the prologue script runs.</summary>
        static readonly string[] ScriptedNames =
        {
            "Prologue Virus Alert", "Prologue 0.txt", "Prologue jpg", "Prologue Browser", "Prologue Shutdown", "Prologue Notepad",
            "Prologue WinRAR Nag", "Prologue WinRAR",
        };

        void StopPrologue()
        {
            // Includes nested downloads, boot balloons and delayed LaoZhou actions.
            // Disabling a MonoBehaviour alone does not stop its coroutines.
            bool speaking = thinking != null;
            StopAllCoroutines();
            run = null;
            thinking = null;
            ending = null;
            laoZhou = null;
            if (speaking || (thoughtGroup != null && thoughtGroup.alpha > 0) || hijacked) VoiceLines.Stop();
            if (voice != null) voice.Stop();
            if (thoughtGroup != null) thoughtGroup.alpha = 0;
            if (thought != null) thought.text = "";
            foreach (var go in spawned) PrologueDesk.RemoveTransient(go);
            spawned.Clear();
            // A script reload in the editor forgets the spawned list; never leave a dead copy of a scripted window behind.
            if (desk != null)
                foreach (var parent in new[] { desk.windows, desk.icons })
                {
                    if (parent == null) continue; // desktop can be destroyed before the director on scene exit
                    for (int i = parent.childCount - 1; i >= 0; i--)
                    {
                        var child = parent.GetChild(i);
                        if (System.Array.IndexOf(ScriptedNames, child.name) >= 0) PrologueDesk.RemoveTransient(child.gameObject);
                    }
                }
            txtIcon = archiveIcon = notepad = browser = menu = dialog = virusBox = null;
            if (screenRoot != null)
                for (int i = screenRoot.childCount - 1; i >= 0; i--)
                    if (screenRoot.GetChild(i).name == "Title") PrologueDesk.RemoveTransient(screenRoot.GetChild(i).gameObject);
            // The layer is shared with notifications, guides and other scenes. Remove only our transient blockers.
            if (desk != null && desk.layer != null)
                for (int i = desk.layer.childCount - 1; i >= 0; i--)
                {
                    var child = desk.layer.GetChild(i);
                    if (child.name == "Boot" || child.name == "Shutdown" || child.name == "Prologue Menu")
                        PrologueDesk.RemoveTransient(child.gameObject);
                    else if (child.name == "Cursor") child.gameObject.SetActive(false);
                }
            var hub = LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
            if (hub != null) hub.EndScript();
            Release();
            Step = "";
        }
    }
}
