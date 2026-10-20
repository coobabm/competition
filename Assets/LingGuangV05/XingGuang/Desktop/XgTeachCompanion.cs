using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>The teaching desk's companion: one next action and a visible, live concept board.</summary>
    public sealed class XgTeachCompanion : MonoBehaviour
    {
        IXgPageHost host;
        TMP_Text title, next, progress, response;
        XgBtn train, chat, brain;
        readonly List<Image> cells = new List<Image>();
        readonly Dictionary<int, double> weights = new Dictionary<int, double>();
        readonly Dictionary<int, float> flashes = new Dictionary<int, float>();
        XgSim owner;
        float nextRefresh, flash;

        public static void Build(RectTransform parent, IXgPageHost host, XgUi ui)
        {
            var box = Rect("TeachingCompanion", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(box, XgDark.Card);
            var self = box.gameObject.AddComponent<XgTeachCompanion>(); self.host = host;
            self.title = ui.Text(Strip("Title", box, 16, 28, 20, 20), "", 20, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            self.next = ui.Text(Strip("Next", box, 170, 28, 20, 20), "", 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            self.next.enableAutoSizing = true; self.next.fontSizeMin = 12; self.next.fontSizeMax = 15;
            self.progress = ui.Text(Strip("Progress", box, 54, 32, 20, 20), "", 16, XgDark.Good, TextAlignmentOptions.MidlineLeft);
            var grid = Rect("LearningCells", box, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -158), new Vector2(-20, -94));
            for (int i = 0; i < 32; i++)
            {
                float x = i % 8 / 8f, y = 1 - i / 8 / 4f;
                self.cells.Add(Panel(Rect("Cell" + i, grid, new Vector2(x, y - .25f), new Vector2(x + .125f, y), new Vector2(2, 2), new Vector2(-2, -2)), XgDark.Track));
            }
            self.response = ui.Text(Strip("Reaction", box, 204, 42, 20, 20), "", 16, XgDark.Muted, TextAlignmentOptions.TopLeft);
            self.response.enableAutoSizing = true; self.response.fontSizeMin = 12; self.response.fontSizeMax = 16;
            self.train = ui.Button(box, "", () => {
                var sim = host.Sim; var data = XgCatalog.Dataset(sim.S.desk);
                sim.SelectedTrack = data.track; sim.SetDataset(data.track, data.id); host.ShowTab("train");
            }, 20);
            PlaceBottom(self.train, 106, 48, 20, 20);
            self.train.rt.gameObject.SetActive(false);
            self.chat = ui.Button(box, T("和它说句话 →", "Talk to it →"), () => { if (!XgYyTalk.Open(playerRequested: true)) host.ShowTab("home"); }, 17);
            PlaceBottom(self.chat, 56, 40, 20, 20);
            self.brain = ui.Button(box, T("看看它记住了什么", "Look inside its brain"), () => host.ShowTab("board"), 15);
            PlaceBottom(self.brain, 14, 32, 20, 20);
        }

        void Update()
        {
            if (host?.Sim == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            var sim = host.Sim; var data = XgCatalog.Dataset(sim.S.desk);
            if (data == null) return;
            if (owner != sim) { owner = sim; weights.Clear(); flashes.Clear(); flash = 0; }
            int count = (int)sim.Samples(data.id);
            bool ready = count >= XgCatalog.SamplesToTrain;
            bool firstLesson = XgFirstLessonWindow.ActiveFor(sim, Time.unscaledTime);
            title.text = T("教学记录", "Teaching record");
            chat.label.text = T("和它说句话 →", "Talk to it →");
            brain.label.text = T("看看它记住了什么", "Look inside its brain");
            next.text = T("答对 ", "Correct ") + sim.S.handCorrect + T(" · 答错 ", " · Wrong ") + sim.S.handWrong;
            progress.text = T("这类样本 ", "Examples: ") + count + (firstLesson ? "" : ready ? T(" · 可以训练了", " · Ready to train") : " / " + XgCatalog.SamplesToTrain);
            train.Set(firstLesson ? T("训练 →", "Train →") : ready ? T("带它去训练 →", "Train together →") : T("再教 ", "Teach ") + (XgCatalog.SamplesToTrain - count) + T(" 张就能训练", " more cards"), ready, ready && !firstLesson ? XgDark.Accent : XgDark.Track, XgDark.Ink);
            train.rt.gameObject.SetActive(ready);
            var concepts = new List<XgConcept>(sim.Board.Concepts(XgSim.RegionOf(data.id)));
            for (int i = 0; i < cells.Count; i++)
            {
                if (i >= concepts.Count) { cells[i].color = XgDark.Track; continue; }
                var c = concepts[i];
                bool changed = !weights.TryGetValue(c.id, out double before) || System.Math.Abs(before - c.w) > .00001;
                if (changed) { flash = Time.unscaledTime + .55f; flashes[c.id] = flash; }
                weights[c.id] = c.w;
                var color = c.w >= 0 ? new Color(.25f, .75f, .52f) : new Color(.87f, .38f, .38f);
                cells[i].color = Color.Lerp(XgDark.Track, color, .3f + .7f * Mathf.Clamp01((float)System.Math.Abs(c.w)));
                if (!sim.S.reduceFx && flashes.TryGetValue(c.id, out float until) && Time.unscaledTime < until)
                    cells[i].color = Color.Lerp(cells[i].color, Color.white, (until - Time.unscaledTime) / .55f);
            }
            response.text = T("纠正 ", "Corrections ") + sim.S.teachingCorrections
                + T(" · AI 连对 ", " · AI streak ") + sim.S.modelStreak;
        }
    }
}
