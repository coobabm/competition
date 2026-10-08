using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The emergence cutscene (参数量与数据量主线 §8, §10). When an ability emerges the lab's window darkens: the
    /// training curve runs flat and then kinks upward, the brain lights up (the 皮层拓扑 graphic), the ability's cell
    /// in the six-cell table turns gold, and it says one line in its new ability. The inner monologue is the story
    /// beat lg.emerge:N, which waits for it (the story director treats it as busy). It starts after a story cutscene on
    /// screen (the month card) has finished. Click or Esc skips; it waits while the window is hidden and never blocks
    /// the simulation.
    /// </summary>
    public sealed class XgEmergenceCutscene : MonoBehaviour
    {
        const float FadeSeconds = .35f, KneeAt = 1.4f, LineAt = 2.6f, CloseAfter = 7.5f, StartDelay = .8f;

        static XgEmergenceCutscene active;
        /// <summary>A cutscene is on screen: the story waits for it, the inner voice too.</summary>
        public static bool Playing => active != null && active.Showing;
        const int FlatPoints = 26, RisePoints = 8;

        XingGuangView view;
        XgUi ui;
        XgSim bound;
        RectTransform overlay;
        CanvasGroup group;
        TMP_Text kicker, title, stats, line, hint;
        XgChartGraphic chart;
        XgCortexGraphic cortex;
        readonly Image[] cells = new Image[XgSim.AbilityCount];
        readonly TMP_Text[] cellTexts = new TMP_Text[XgSim.AbilityCount];
        readonly Queue<int> pending = new Queue<int>();
        readonly List<float> train = new List<float>(), val = new List<float>();
        List<float> fullTrain, fullVal;
        int ability;
        float startedAt = -1, closeAt = -1, queuedAt = -1;
        bool kneeHit, lineShown;
        string spoken = "";

        public static XgEmergenceCutscene Install(XingGuangView view, RectTransform root, TMP_FontAsset font)
        {
            var c = view.gameObject.GetComponent<XgEmergenceCutscene>() ?? view.gameObject.AddComponent<XgEmergenceCutscene>();
            c.view = view;
            c.ui = new XgUi(font, view.Controller != null ? view.Controller.Window : null);
            c.Build(root);
            active = c;
            return c;
        }

        /// <summary>A cutscene is on screen (or fading out).</summary>
        public bool Showing => overlay != null && overlay.gameObject.activeSelf;

        void Build(RectTransform root)
        {
            overlay = Rect("Emergence", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            group = overlay.gameObject.AddComponent<CanvasGroup>();
            var back = Panel(overlay, new Color(XgCortexGraphic.N.BgTop.r, XgCortexGraphic.N.BgTop.g, XgCortexGraphic.N.BgTop.b, .95f));
            back.raycastTarget = true;
            var skip = overlay.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(Skip);

            kicker = ui.Text(Rect("Kicker", overlay, new Vector2(0, 1), Vector2.one, new Vector2(40, -64), new Vector2(-40, -26)), "", 18, XgCortexGraphic.N.TextMuted, TextAlignmentOptions.MidlineLeft);
            title = ui.Text(Rect("Title", overlay, new Vector2(0, 1), Vector2.one, new Vector2(40, -116), new Vector2(-40, -62)), "", 40, XgCortexGraphic.N.Gold, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            stats = ui.Text(Rect("Stats", overlay, new Vector2(0, 1), Vector2.one, new Vector2(40, -146), new Vector2(-40, -116)), "", 16, XgCortexGraphic.N.Text, TextAlignmentOptions.MidlineLeft);

            // The training curve: flat, then the kink.
            var plot = Rect("Curve", overlay, new Vector2(0, 0), new Vector2(.42f, 1), new Vector2(40, 150), new Vector2(-10, -170));
            Panel(plot, XgCortexGraphic.N.Glass).raycastTarget = false;
            var chartRt = Rect("Chart", plot, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -28));
            chart = chartRt.gameObject.AddComponent<XgChartGraphic>();
            chart.raycastTarget = false; chart.Capacity = FlatPoints + RisePoints;
            ui.Text(Strip("Label", plot, 4, 22, 10, 10), T("训练曲线", "Training curve"), 13, XgCortexGraphic.N.TextMuted, TextAlignmentOptions.MidlineLeft);

            // The brain, every region lit at once.
            var map = Rect("Brain", overlay, new Vector2(.42f, 0), Vector2.one, new Vector2(10, 150), new Vector2(-40, -170));
            Panel(map, XgCortexGraphic.N.BgBottom).raycastTarget = false;
            var brain = Rect("Cortex", map, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            cortex = brain.gameObject.AddComponent<XgCortexGraphic>();
            cortex.raycastTarget = false;
            cortex.Font = ui.font;

            // The ability table in one row: the new cell turns gold.
            var strip = Rect("Table", overlay, Vector2.zero, new Vector2(1, 0), new Vector2(40, 84), new Vector2(-40, 138));
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                var cell = Rect("Cell" + (i + 1), strip, new Vector2(i / 6f, 0), new Vector2((i + 1) / 6f, 1), new Vector2(4, 0), new Vector2(-4, 0));
                cells[i] = Panel(cell, XgCortexGraphic.N.Dim);
                cells[i].raycastTarget = false;
                cellTexts[i] = ui.Text(Rect("Text", cell, Vector2.zero, Vector2.one, new Vector2(8, 2), new Vector2(-8, -2)), "", 15, XgCortexGraphic.N.Text, TextAlignmentOptions.Center);
                cellTexts[i].enableAutoSizing = true; cellTexts[i].fontSizeMin = 10; cellTexts[i].fontSizeMax = 15;
            }

            line = ui.Text(Rect("Line", overlay, Vector2.zero, new Vector2(1, 0), new Vector2(40, 34), new Vector2(-220, 80)), "", 24, Color.white, TextAlignmentOptions.MidlineLeft);
            hint = ui.Text(Rect("Hint", overlay, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-220, 34), new Vector2(-40, 80)), "", 13, XgCortexGraphic.N.TextMuted, TextAlignmentOptions.MidlineRight);
            overlay.gameObject.SetActive(false);
        }

        void OnDestroy() { Unbind(); if (active == this) active = null; }

        void Unbind()
        {
            if (bound != null) bound.AbilityEmerged -= OnAbility;
            bound = null;
        }

        void OnAbility(int emerged) { if (!pending.Contains(emerged)) { pending.Enqueue(emerged); queuedAt = Time.unscaledTime; } }

        /// <summary>Another full-window moment is on: the story's own cutscene (the month card), a love question, an insight card.</summary>
        bool OtherCutscene()
        {
            var presenter = FindAnyObjectByType<LingGuangV05.Desktop.Story.StoryDesktopPresenter>();
            if (presenter != null && presenter.CutscenePlaying) return true;
            if (LoveQuestionCutscene.Playing) return true;
            var holo = view.Holo;
            return holo != null && holo.Showing;
        }

        void Skip() { if (Showing && closeAt < 0) closeAt = Time.unscaledTime; }

        void Update()
        {
            if (view == null) return;
            var sim = view.Sim;
            if (sim != bound) { Unbind(); bound = sim; if (bound != null) bound.AbilityEmerged += OnAbility; }
            if (!Showing)
            {
                // The month card the story raises in the same moment goes first; this follows it.
                if (pending.Count > 0 && view.Visible && Time.unscaledTime - queuedAt >= StartDelay && !OtherCutscene()) Begin(pending.Dequeue());
                return;
            }
            if (!view.Visible) return; // paused while the window is hidden
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) Skip();
            float now = Time.unscaledTime;
            if (closeAt >= 0)
            {
                float k = Mathf.Clamp01((now - closeAt) / FadeSeconds);
                group.alpha = 1 - k;
                if (k >= 1) overlay.gameObject.SetActive(false);
                return;
            }
            float t = now - startedAt;
            group.alpha = Mathf.Clamp01(t / FadeSeconds);
            DrawCurve(t);
            cortex.Animate();
            if (!kneeHit && t >= KneeAt) Knee();
            if (lineShown)
            {
                int chars = Mathf.Clamp(Mathf.FloorToInt((t - LineAt) * 9), 0, spoken.Length);
                line.text = "<color=#8494C8>" + Profile() + "：</color>" + spoken.Substring(0, chars);
            }
            else if (t >= LineAt) lineShown = true;
            if (t >= CloseAfter) closeAt = now;
        }

        string Profile() => view.Sim.Profile.name.Length > 0 ? view.Sim.Profile.name : T(AppNames.AiZh, AppNames.AiEn);

        void Begin(int emerged)
        {
            ability = emerged;
            var sim = view.Sim;
            startedAt = Time.unscaledTime; closeAt = -1; kneeHit = false; lineShown = false;
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            view.Juice.BringToFront();
            group.alpha = 0;
            kicker.text = T("它学会了", "It learned");
            title.text = XgSim.AbilityName(emerged, sim.English);
            stats.text = T("参数 ", "Parameters ") + XgSim.ParamsText(sim.TrainedParamsK) + T(" · 样本 ", " · samples ") + XgSim.SamplesText(sim.TrainedSamples)
                + "   <color=#8494C8>" + T(XgSim.AbilityUnlocks[emerged], XgSim.AbilityUnlocksEn[emerged]) + "</color>";
            hint.text = T("点击或按 Esc 继续", "Click or press Esc");
            spoken = Spoken(emerged);
            line.text = "";
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                bool lit = sim.HasAbility(i + 1) && i + 1 != emerged;
                cells[i].color = lit ? new Color32(120, 96, 30, 255) : XgCortexGraphic.N.Dim;
                cellTexts[i].text = (i + 1) + " · " + XgSim.AbilityName(i + 1, sim.English);
                cellTexts[i].color = lit ? XgCortexGraphic.N.Star : XgCortexGraphic.N.TextMuted;
            }
            BuildCurve(emerged);
            SetBrain();
            view.Juice.Flash(new Color32(255, 240, 200, 255), .2f, .5f);
            view.Juice.Play(XgJuice.Sfx.Id.Whoosh);
        }

        /// <summary>What it says in its new ability: a choice, a word, a sentence, a memory, then everything.</summary>
        string Spoken(int emerged)
        {
            var sim = view.Sim;
            switch (emerged)
            {
                case 2: return T("是。否。……还有别的。", "Yes. No. … There is more.");
                case 3: return sim.FirstTrack == "vision" ? T("我。看。", "I. See.") : T("我。读。", "I. Read.");
                case 4: return T("你说过的话，我记得。", "I remember what you said.");
                case 5: return T("那页乱码……我读得出一行了。", "That garbled page… I can read a line of it.");
                default: return T("一起亮了。", "All of it, lit at once.");
            }
        }

        /// <summary>A flat, noisy curve that kinks upward: the moment the scale crossed the line.</summary>
        void BuildCurve(int emerged)
        {
            fullTrain = new List<float>(); fullVal = new List<float>();
            var rng = new System.Random(emerged * 7919 + 13);
            float flat = .18f + .04f * emerged;
            for (int i = 0; i < FlatPoints; i++)
            {
                float v = flat + (float)rng.NextDouble() * .05f + i * .001f;
                fullTrain.Add(v + .03f); fullVal.Add(v);
            }
            for (int i = 1; i <= RisePoints; i++)
            {
                float k = 1 - Mathf.Pow(1 - i / (float)RisePoints, 2.2f);
                float v = Mathf.Lerp(flat + .05f, .9f, k);
                fullTrain.Add(Mathf.Min(.98f, v + .03f)); fullVal.Add(v);
            }
            train.Clear(); val.Clear();
        }

        void DrawCurve(float t)
        {
            float flatShare = FlatPoints / (float)(FlatPoints + RisePoints);
            float reveal = t < KneeAt ? flatShare * Mathf.Clamp01(t / KneeAt) : flatShare + (1 - flatShare) * Mathf.Clamp01((t - KneeAt) / .6f);
            int n = Mathf.Clamp(Mathf.CeilToInt(reveal * fullVal.Count), 2, fullVal.Count);
            if (n == val.Count) return;
            train.Clear(); val.Clear();
            for (int i = 0; i < n; i++) { train.Add(fullTrain[i]); val.Add(fullVal[i]); }
            chart.SetData(train, val, 0);
            if (n > FlatPoints) chart.PulseLastSegment(1);
        }

        /// <summary>The knee: the cell lights, the screen knocks, it is there.</summary>
        void Knee()
        {
            kneeHit = true;
            var cell = cells[ability - 1];
            cell.color = new Color32(255, 206, 90, 255);
            cellTexts[ability - 1].color = new Color32(40, 28, 0, 255);
            var at = view.Juice.At(cell.rectTransform);
            view.Juice.Play(XgJuice.Sfx.Id.Fanfare);
            view.Juice.Shockwave(at, XgPalette.Gold, 260, .5f, 12);
            view.Juice.Knock(cell.rectTransform, .25f, Vector2.zero, .4f);
            if (!view.Sim.S.reduceFx)
            {
                view.Juice.Burst(at, 36, XgPalette.Gold, XgJuice.Shape.Star, 420);
                view.Juice.Flash(Color.white, .12f, .55f);
                view.Juice.HitStop(120);
            }
        }

        /// <summary>The brain as the 皮层拓扑 page draws it, every region training at once.</summary>
        void SetBrain()
        {
            var sim = view.Sim;
            cortex.Reduced = sim.S.reduceFx;
            bool one = ability >= XgSim.AbilityCount;
            cortex.UnifiedTitle = T("全皮层同步点亮", "The whole cortex lit at once");
            cortex.SetUnified(one, true);
            cortex.UnifiedCells.Clear();
            for (int i = 0; i < cortex.Regions.Length; i++)
            {
                var r = cortex.Regions[i];
                var concepts = new List<XgConcept>(sim.Board.Concepts(r.id));
                concepts.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
                string arch = r.id == XgSim.ToneRegion ? (concepts.Count > 0 ? "tone" : "") : sim.RegionWiringOrLast(r.id)?.id ?? "";
                cortex.SetArch(i, one ? "transformer" : arch, false);
                r.training = true;
                r.empty = concepts.Count == 0;
                r.cells.Clear();
                for (int c = 0; c < Math.Min(XgBoardPage.CortexCells, concepts.Count); c++)
                {
                    var concept = concepts[c];
                    float strength = Mathf.Clamp01((float)(Math.Abs(concept.w) / 2));
                    r.cells.Add(concept.seed ? new XgCellLook(XgCellKind.Seed, strength) : concept.alt.Length > 0 ? new XgCellLook(XgCellKind.Superposed, strength)
                        : new XgCellLook(concept.w >= 0 ? XgCellKind.Yes : XgCellKind.No, strength));
                }
                for (int c = 0; c < 8 && cortex.UnifiedCells.Count < XgCortexGraphic.UnifiedCellCount; c++)
                    cortex.UnifiedCells.Add(c < r.cells.Count ? r.cells[c] : new XgCellLook(XgCellKind.Dim, 0));
                cortex.RegionNames[i] = XgSim.RegionName(r.id, Lang.English);
                r.chip = XgSim.RegionName(r.id, Lang.English);
                r.chipTopo = ""; r.chipChange = "";
            }
            cortex.Animate();
        }
    }
}
