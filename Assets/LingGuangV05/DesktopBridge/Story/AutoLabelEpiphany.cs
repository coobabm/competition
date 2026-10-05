using System.Collections;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The beat where the protagonist realises, alone, that the trained model could do the crowd labelling.
    ///
    /// Act 1, pressure: while the player hand-labels before the idea, <see cref="XgInnerMonologue"/> picks a short
    /// thought now and then (fatigue, the late hour, money short of the power bill, a long streak) and
    /// <see cref="InnerVoice"/> shows it. The mirror (a faint 「它：是」 on the card) is <see cref="XgLabelGhost"/>.
    ///
    /// Act 2, the cutscene: when the sim raises <see cref="XgSim.Epiphany"/> (it is told to wait for us with
    /// <see cref="XgSim.DeferAutoLabelReveal"/>), this waits for a quiet moment, dims the desktop, takes the mouse
    /// and plays: two thoughts; the cursor glides to 摆渡贴吧 (its desktop icon when that is not covered, else the
    /// taskbar button), opens the 挂机脚本 thread, scrolls to 「要是脚本真认得字就好了。」 and stops on it; a jolt and
    /// 「！」; three more thoughts; then input comes back, the lab opens on the tech tree, the 「让它替我标」 flash card
    /// flips in, 自动答题 is revealed and ringed. Click or Esc skips once the first thought has been shown.
    /// If the forum cannot open, that part is skipped. Input is never left blocked: the blocked part has a time
    /// limit, and OnDisable restores everything and reveals 自动答题 if the scene could not finish.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AutoLabelEpiphany : MonoBehaviour
    {
        public const string ThreadId = "crowd_script";
        const string SparkZh = "要是脚本真认得字就好了", SparkEn = "If only the script could actually read";
        /// <summary>Seconds without a hand answer before the scene may start, the wait after which it starts anyway,
        /// and the wait after which 自动答题 is revealed without a scene (something keeps blocking it).</summary>
        const float QuietAfterAnswer = 1.2f, ForceAfter = 40f, RevealWithoutScene = 240f;
        const float BlockLimit = 40f, CardLimit = 60f;

        /// <summary>The scene is running (other story layers can wait for it).</summary>
        public static bool Playing { get; private set; }

        enum Phase { Idle, Blocked, Finishing }

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        ChapterOneDesktopRouter router;
        AeroLcdInputSurface surface;
        PrologueDirector prologue;
        XingGuangController lab;
        XgSim bound;
        readonly XgInnerMonologue monologue = new XgInnerMonologue();
        int lastHand = -1;
        float lastHandAt, pendingSince = -1;

        Phase phase;
        Coroutine timeline, finishing;
        float blockedAt;
        bool canSkip, inputBlocked, unblockPending;
        float unblockAt;
        RectTransform dim, bang;
        RectTransform shaken;
        Vector2 shakeHome;
        AudioSource sound;

        /// <summary>Adds the beat next to the story presenter (once). Call from its Start().</summary>
        public static AutoLabelEpiphany Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var beat = host.GetComponent<AutoLabelEpiphany>() ?? host.gameObject.AddComponent<AutoLabelEpiphany>();
            beat.host = host;
            return beat;
        }

        void Start()
        {
            runtime = FindAnyObjectByType<ChapterOneRuntime>();
            router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false; sound.spatialBlend = 0; sound.ignoreListenerPause = true;
        }

        void OnDisable()
        {
            // Never leave the desktop blocked or the node hidden because the scene was cut short.
            if (timeline != null) StopCoroutine(timeline);
            if (finishing != null) StopCoroutine(finishing);
            timeline = finishing = null;
            Unblock(true);
            if (phase != Phase.Idle && bound != null && bound.EpiphanyPending) bound.RevealAutoLabel();
            phase = Phase.Idle;
            Playing = false;
        }

        void OnDestroy() { if (bound != null) bound.DeferAutoLabelReveal = false; }

        bool PresenterBusy => host is StoryDesktopPresenter presenter && presenter.CutscenePlaying;
        bool ReduceMotion => host is StoryDesktopPresenter presenter && presenter.ReduceMotion;

        void Update()
        {
            if (unblockPending) FinishUnblock();
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            var sim = lab != null ? lab.Sim : null;
            if (!ReferenceEquals(sim, bound)) Bind(sim);
            if (sim == null || runtime == null || runtime.Sim == null || runtime.TestMode) return;

            if (phase == Phase.Blocked)
            {
                if (canSkip && SkipPressed() || Time.unscaledTime - blockedAt > BlockLimit) EndBlocked(true);
                return;
            }
            if (phase == Phase.Finishing) return;

            int hand = sim.S.handCorrect + sim.S.handWrong;
            if (hand != lastHand)
            {
                bool labelled = lastHand >= 0 && hand > lastHand;
                lastHand = hand; lastHandAt = Time.unscaledTime;
                if (labelled && !sim.EpiphanyDone) Think(sim);
            }

            if (!sim.EpiphanyPending) { pendingSince = -1; return; }
            if (pendingSince < 0) pendingSince = Time.unscaledTime;
            float waited = Time.unscaledTime - pendingSince;
            if (Quiet() || waited > ForceAfter && CanPlay()) StartScene(sim);
            else if (waited > RevealWithoutScene && sim.RevealAutoLabel() && lab.View != null) lab.View.Refresh(true);
        }

        void Bind(XgSim sim)
        {
            if (bound != null) bound.DeferAutoLabelReveal = false;
            bound = sim;
            if (bound != null) bound.DeferAutoLabelReveal = true;
            lastHand = -1; pendingSince = -1;
        }

        // ───────────── act 1: thoughts while labelling ─────────────

        void Think(XgSim sim)
        {
            if (runtime.Sim.InPrologue || Playing || PresenterBusy || InnerVoice.Busy) return;
            var s = runtime.Sim.S; var config = runtime.Sim.Config;
            var now = GameCalendar.Now(s);
            double projected = config != null ? s.energyKwh * config.electricityPrice * config.dayLengthSeconds / System.Math.Max(20, s.daySeconds) : 0;
            var context = new XgMonologueContext
            {
                handLabels = sim.S.handCorrect + sim.S.handWrong,
                desk = sim.S.desk ?? "",
                deskLabels = sim.Labels(sim.S.desk ?? ""),
                hour = now.Hour, minute = now.Minute,
                money = s.money, nextBill = s.billDue + projected, unpaid = s.unpaidPower,
                streak = sim.S.combo,
            };
            var line = monologue.Next(context, Time.unscaledTime);
            if (line != null) InnerVoice.Say(line.zh, line.en);
        }

        // ───────────── when to play ─────────────

        bool CanPlay()
        {
            if (runtime == null || runtime.Sim == null || runtime.Sim.InPrologue || PresenterBusy || PrologueDirector.Desk == null) return false;
            if (prologue == null) prologue = FindAnyObjectByType<PrologueDirector>();
            if (prologue != null && prologue.Running) return false;
            var holo = lab != null && lab.View != null ? lab.View.GetComponent<XgHoloCard>() : null;
            return holo == null || !holo.Showing;
        }

        bool Quiet()
        {
            if (!CanPlay() || InnerVoice.Busy || Time.unscaledTime - lastHandAt < QuietAfterAnswer) return false;
            var mouse = Mouse.current;
            return mouse == null || !mouse.leftButton.isPressed && !mouse.rightButton.isPressed;
        }

        static bool SkipPressed()
        {
            var mouse = Mouse.current; var keyboard = Keyboard.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame || keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        void StartScene(XgSim sim)
        {
            Playing = true;
            phase = Phase.Blocked;
            blockedAt = Time.unscaledTime;
            canSkip = false;
            Block();
            timeline = StartCoroutine(Timeline());
        }

        void EndBlocked(bool skipped)
        {
            if (phase != Phase.Blocked) return;
            if (timeline != null) StopCoroutine(timeline);
            timeline = null;
            if (skipped) InnerVoice.Clear();
            Unblock(false);
            phase = Phase.Finishing;
            finishing = StartCoroutine(Finish(bound));
        }

        // ───────────── the cutscene ─────────────

        IEnumerator Timeline()
        {
            var desk = PrologueDirector.Desk;
            yield return PrologueDesk.Wait(.5f);
            yield return InnerVoice.SayAndWait("它答得跟我一样……", "It answers the same as me…", 1.6f);
            canSkip = true;
            yield return InnerVoice.SayAndWait("还比我快。", "And faster.", 1.2f);

            yield return OpenThread(desk);

            // The jolt.
            yield return PrologueDesk.Wait(.6f);
            yield return Shock(desk);
            yield return InnerVoice.SayAndWait("……这说的不就是我？", "…isn't that me they're talking about?", 1.6f);
            yield return PrologueDesk.Wait(.4f);
            yield return InnerVoice.SayAndWait("不对。它不是脚本，它真的认得。", "No. It isn't a script. It really can read.", 1.8f);
            yield return InnerVoice.SayAndWait("……可行。", "…this could work.", 1.4f);
            timeline = null;
            EndBlocked(false);
        }

        /// <summary>The cursor goes to 摆渡贴吧, opens the thread, scrolls and stops on the line. Skipped if anything is missing.</summary>
        IEnumerator OpenThread(PrologueDesk desk)
        {
            var hub = TiebaHub.Instance;
            var view = hub != null ? hub.View : null;
            var window = view != null ? view.GetComponentInParent<WindowManager>(true) : null;
            if (view == null || window == null || hub.Library == null || hub.Library.Get(ThreadId) == null) yield break;
            if (!ForumLibrary.Visible(hub.Library.Get(ThreadId), hub.Context())) yield break;

            // Where to click: an uncovered desktop icon is double-clicked, otherwise the taskbar button is clicked once.
            var icon = GameObject.Find("Desktop List/" + TiebaView.NativeWindow) as GameObject;
            var iconRt = icon != null ? icon.transform as RectTransform : null;
            var task = window.taskbarButton != null && window.taskbarButton.gameObject.activeInHierarchy ? window.taskbarButton.transform as RectTransform : null;
            bool useIcon = iconRt != null && !window.isOn && !Covered(iconRt);
            var target = useIcon ? iconRt : task != null ? task : iconRt;
            if (target != null)
            {
                yield return desk.Move(desk.LocalOf(target), 1.2f);
                yield return PrologueDesk.Wait(.15f);
                yield return ClickAt(desk, useIcon || task == null ? 2 : 1);
            }
            bool wasOpen = window.isOn;
            view.Open("thread", ThreadId);
            if (wasOpen) { window.OpenWindow(); window.FocusToWindow(); } // restores a minimised window too

            // Wait for the thread to be drawn. The forum redraws itself once more within half a second of opening
            // (which resets its scroll), so the scrolling below starts after that.
            TMP_Text spark = null;
            float opened = Time.unscaledTime;
            for (float t = 0; t < 3f && spark == null; t += Time.unscaledDeltaTime) { spark = Spark(view); yield return null; }
            if (spark == null) yield break;
            while (Time.unscaledTime - opened < .6f) yield return null;
            spark = Spark(view);
            if (spark == null) yield break;
            Canvas.ForceUpdateCanvases();

            var scroll = spark.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.viewport != null)
            {
                yield return desk.Move(desk.LocalOf(scroll.viewport, new Vector2(0, -20)), .7f);
                // A few wheel notches, each eased, until the line sits a little below the middle.
                for (int notch = 0; notch < 4; notch++)
                {
                    spark = Spark(view);
                    scroll = spark != null ? spark.GetComponentInParent<ScrollRect>() : null; // a redraw replaces both
                    if (spark == null || scroll == null || scroll.viewport == null) break;
                    float goal = ScrollGoal(scroll, spark.rectTransform);
                    float from = scroll.content.anchoredPosition.y, to = Mathf.Lerp(from, goal, notch == 3 ? 1 : .45f);
                    scroll.StopMovement();
                    for (float t = 0; t < .28f; t += Time.unscaledDeltaTime)
                    {
                        if (scroll == null || scroll.content == null) break;
                        var p = scroll.content.anchoredPosition; p.y = Mathf.Lerp(from, to, 1 - Mathf.Pow(1 - t / .28f, 3)); scroll.content.anchoredPosition = p;
                        yield return null;
                    }
                    if (scroll != null && scroll.content != null) { var p = scroll.content.anchoredPosition; p.y = to; scroll.content.anchoredPosition = p; }
                    yield return PrologueDesk.Wait(.12f);
                }
            }
            spark = Spark(view);
            if (spark == null) yield break;
            var rt = spark.rectTransform;
            float width = Mathf.Min(rt.rect.width, spark.preferredWidth);
            Vector2 OnLine(float k) => desk.layer.InverseTransformPoint(rt.TransformPoint(new Vector3(rt.rect.xMin + width * k, rt.rect.center.y, 0)));
            yield return desk.Move(OnLine(.15f), .8f);
            // Read along the line, as if selecting it.
            var mark = PrologueDesk.Rect("Reading", rt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-2, -1), new Vector2(-2, 1));
            mark.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            PrologueDesk.Fill(mark, new Color(1f, .85f, .2f, .32f), false);
            for (float t = 0; t < .9f; t += Time.unscaledDeltaTime)
            {
                if (rt == null || mark == null) yield break;
                float k = Mathf.SmoothStep(0, 1, t / .9f);
                mark.offsetMax = new Vector2(width * k + 2, 1);
                desk.Cursor.anchoredPosition = OnLine(.15f + .6f * k);
                yield return null;
            }
        }

        static TMP_Text Spark(TiebaView view)
        {
            if (view == null) return null;
            foreach (var t in view.GetComponentsInChildren<TMP_Text>(false))
                if (t.text != null && (t.text.Contains(SparkZh) || t.text.Contains(SparkEn))) return t;
            return null;
        }

        /// <summary>The content offset that puts the line about 55% down the viewport, clamped to the content.</summary>
        static float ScrollGoal(ScrollRect scroll, RectTransform line)
        {
            var content = scroll.content;
            Vector3 local = content.InverseTransformPoint(line.TransformPoint(line.rect.center));
            float top = content.rect.yMax - local.y; // distance from the content's top edge
            float view = scroll.viewport.rect.height;
            float max = Mathf.Max(0, content.rect.height - view);
            return Mathf.Clamp(top - view * .55f, 0, max);
        }

        /// <summary>A desktop point is under an open window.</summary>
        static bool Covered(RectTransform target)
        {
            Vector3 p = target.TransformPoint(target.rect.center);
            foreach (var w in FindObjectsByType<WindowManager>(FindObjectsSortMode.None))
            {
                if (!w.isOn || !w.gameObject.activeInHierarchy) continue;
                var rt = w.transform as RectTransform;
                if (rt != null && rt.rect.Contains((Vector2)rt.InverseTransformPoint(p))) return true;
            }
            return false;
        }

        IEnumerator ClickAt(PrologueDesk desk, int times)
        {
            for (int i = 0; i < times; i++)
            {
                Pulse(desk);
                var clip = XgJuice.Sfx.Clip(XgJuice.Sfx.Id.Click, i % XgJuice.Sfx.VariantsPerEvent);
                if (sound != null && clip != null) sound.PlayOneShot(clip, .45f);
                yield return desk.Click(1);
            }
        }

        /// <summary>A small ring at the cursor's tip for each click.</summary>
        void Pulse(PrologueDesk desk)
        {
            if (desk.Cursor == null) return;
            var ring = PrologueDesk.Centered("Click", desk.layer, desk.Cursor.anchoredPosition, new Vector2(8, 8));
            var img = PrologueDesk.Fill(ring, new Color(1, 1, 1, .55f), false);
            img.sprite = PrologueDesk.Circle();
            desk.Cursor.SetAsLastSibling();
            StartCoroutine(Grow(ring, img));
        }

        static IEnumerator Grow(RectTransform ring, Image img)
        {
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            {
                if (ring == null) yield break;
                float k = t / .3f;
                ring.sizeDelta = Vector2.one * Mathf.Lerp(8, 40, k);
                img.color = new Color(1, 1, 1, .55f * (1 - k));
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
        }

        /// <summary>A quick jolt of the desktop, a soft flash (at least 0.3 s, never a strobe) and 「！」 by the cursor.</summary>
        IEnumerator Shock(PrologueDesk desk)
        {
            Vector2 at = desk.Cursor != null ? desk.Cursor.anchoredPosition : Vector2.zero;
            bang = PrologueDesk.Centered("Bang", desk.layer, at + new Vector2(34, 40), new Vector2(90, 90));
            var text = desk.Text(bang, "！", 72, new Color32(230, 70, 40, 255), TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = .15f; text.outlineColor = new Color32(255, 255, 255, 200);
            RectTransform flash = null; Image flashImage = null;
            if (!ReduceMotion)
            {
                flash = PrologueDesk.Rect("Flash", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                flashImage = PrologueDesk.Fill(flash, new Color(1, 1, 1, 0), false);
                shaken = desk.layer.parent as RectTransform;
                if (shaken != null) shakeHome = shaken.anchoredPosition;
            }
            if (desk.Cursor != null) desk.Cursor.SetAsLastSibling();
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                float k = t / .45f;
                if (bang != null) bang.localScale = Vector3.one * (k < .4f ? Mathf.Lerp(.3f, 1.2f, k / .4f) : Mathf.Lerp(1.2f, 1, (k - .4f) / .6f));
                if (flashImage != null) flashImage.color = new Color(1, 1, 1, .28f * Mathf.Sin(k * Mathf.PI));
                if (shaken != null) shaken.anchoredPosition = shakeHome + Random.insideUnitCircle * 9 * (1 - k);
                yield return null;
            }
            if (shaken != null) { shaken.anchoredPosition = shakeHome; shaken = null; }
            if (flash != null) Destroy(flash.gameObject);
            yield return PrologueDesk.Wait(.7f);
            for (float t = 0; t < .35f && bang != null; t += Time.unscaledDeltaTime) { text.alpha = 1 - t / .35f; yield return null; }
            if (bang != null) Destroy(bang.gameObject);
            bang = null;
        }

        /// <summary>After the blocked part: the lab on the tech tree, the flash card, then 自动答题 revealed and ringed.</summary>
        IEnumerator Finish(XgSim sim)
        {
            var c = lab;
            bool ours = c != null && sim != null && ReferenceEquals(c.Sim, sim);
            if (ours)
            {
                if (router == null || !router.Open(XingGuangController.AppId, "tree")) c.Open();
                for (float t = 0; t < 2f; t += Time.unscaledDeltaTime) { if (c.View != null && c.View.Visible) break; yield return null; }
                yield return null;
                var holo = c.View != null ? c.View.GetComponent<XgHoloCard>() : null;
                if (holo != null && c.View.Visible)
                {
                    holo.Show(XgSim.EpiphanyCardId);
                    yield return null;
                    for (float t = 0; t < CardLimit && holo != null && holo.Showing && c.View != null && c.View.Visible; t += Time.unscaledDeltaTime) yield return null;
                }
            }
            if (sim != null) sim.RevealAutoLabel();
            if (ours && c.View != null && c.View.Visible)
            {
                if (c.View.Tab != "tree") c.View.ShowTab("tree");
                c.View.Refresh(true);
                yield return null; yield return null;
                var node = XgGuideHighlight.Find(c.View, "tree", "node:label.auto");
                yield return null; // the tree glides to it
                XgGuideHighlight.Pulse(node != null ? node : XgGuideHighlight.TabButton(c.View, "tree"), 6f);
            }
            finishing = null;
            phase = Phase.Idle;
            Playing = false;
        }

        // ───────────── taking and returning the mouse ─────────────

        void Block()
        {
            var desk = PrologueDirector.Desk;
            if (surface == null) surface = FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
            if (surface != null) surface.SetInputEnabled(false);
            UnityEngine.Cursor.visible = false;
            inputBlocked = true; unblockPending = false;
            dim = PrologueDesk.Rect("Epiphany Dim", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = PrologueDesk.Fill(dim, new Color(0, 0, 0, 0), true);
            StartCoroutine(FadeDim(image, .2f, .5f));
            desk.ShowCursor(RealPointer(desk) ?? Vector2.zero);
        }

        static IEnumerator FadeDim(Image image, float to, float seconds)
        {
            float from = image != null ? image.color.a : 0;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (image == null) yield break;
                image.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            if (image != null) image.color = new Color(0, 0, 0, to);
            if (image != null && to <= 0) Destroy(image.gameObject);
        }

        /// <summary>
        /// Gives the mouse back. The desktop's input comes back once the button that skipped is released, so that
        /// click does not land on whatever is under the cursor (<paramref name="now"/> forces it at once).
        /// </summary>
        void Unblock(bool now)
        {
            if (shaken != null) { shaken.anchoredPosition = shakeHome; shaken = null; }
            if (bang != null) { Destroy(bang.gameObject); bang = null; }
            if (dim != null)
            {
                var image = dim.GetComponent<Image>();
                image.raycastTarget = false;
                if (now || !isActiveAndEnabled) Destroy(dim.gameObject); else StartCoroutine(FadeDim(image, 0, .4f));
                dim = null;
            }
            PrologueDirector.Desk?.HideCursor();
            if (!inputBlocked) return;
            UnityEngine.Cursor.visible = true;
            unblockPending = true; unblockAt = Time.unscaledTime;
            if (now) FinishUnblock();
        }

        void FinishUnblock()
        {
            var mouse = Mouse.current;
            bool held = mouse != null && mouse.leftButton.isPressed;
            if (held && Time.unscaledTime - unblockAt < 1f && isActiveAndEnabled) return;
            if (surface != null) surface.SetInputEnabled(true);
            inputBlocked = unblockPending = false;
        }

        /// <summary>The player's mouse in the desktop layer, if it is over the monitor.</summary>
        Vector2? RealPointer(PrologueDesk desk)
        {
            if (surface == null || Mouse.current == null || desk == null) return null;
            if (!surface.TryMap(Mouse.current.position.ReadValue(), out var px)) return null;
            var tex = surface.sourceCamera != null ? surface.sourceCamera.targetTexture : null;
            float w = tex != null ? tex.width : 1920, h = tex != null ? tex.height : 1080;
            var size = desk.layer.rect.size;
            return new Vector2(px.x / w * size.x - size.x / 2, px.y / h * size.y - size.y / 2);
        }
    }
}
