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
            self.title = ui.Text(Strip("Title", box, 16, 36, 20, 20), "", 25, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            self.next = ui.Text(Strip("Next", box, 62, 98, 20, 20), "", 18, XgDark.Ink, TextAlignmentOptions.TopLeft);
            self.next.enableAutoSizing = true; self.next.fontSizeMin = 14; self.next.fontSizeMax = 18;
            self.progress = ui.Text(Strip("Progress", box, 166, 42, 20, 20), "", 16, XgDark.Good, TextAlignmentOptions.MidlineLeft);
            var grid = Rect("LearningCells", box, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -294), new Vector2(-20, -220));
            for (int i = 0; i < 32; i++)
            {
                float x = i % 8 / 8f, y = 1 - i / 8 / 4f;
                self.cells.Add(Panel(Rect("Cell" + i, grid, new Vector2(x, y - .25f), new Vector2(x + .125f, y), new Vector2(2, 2), new Vector2(-2, -2)), XgDark.Track));
            }
            self.response = ui.Text(Strip("Reaction", box, 310, 116, 20, 20), "", 16, XgDark.Muted, TextAlignmentOptions.TopLeft);
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
            title.text = T("你在教它认识世界", "You are teaching it about the world");
            chat.label.text = T("和它说句话 →", "Talk to it →");
            brain.label.text = T("看看它记住了什么", "Look inside its brain");
            next.text = firstLesson
                ? T("它先猜，你判对错。\n猜对按「猜对了」，\n猜错按「猜错了」。", "It guesses; you judge.\nRight? Choose Guessed right.\nWrong? Choose Guessed wrong.")
                : sim.S.handCorrect == 0
                ? T("它先猜。\n猜对就确认，猜错就帮它改。\n每教一张，它的脑子都会改变。", "It guesses first.\nConfirm it or correct it.\nEvery lesson changes its brain.")
                : ready && sim.S.epochs == 0 ? T("样本够了。\n让它把你教的东西练一遍，\n然后听听它怎么回答。", "Enough examples.\nLet it practise your lessons,\nthen listen to its answers.")
                : T("教它 → 训练 → 和它聊\n你喂的内容，会慢慢变成\n它认识的东西和说话的样子。", "Teach → Train → Talk\nYour lessons shape what it knows\nand how it speaks.");
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
            response.text = firstLesson
                ? T("判断准确时，它会学下这张卡，\n桌面壁纸也会亮一下。\n然后继续看下一张。", "An accurate judgment teaches it.\nThe wallpaper briefly lights up.\nThen try the next card.")
                : sim.S.teachingCorrections == 0
                ? T("绿格偏向「是」，红格偏向「否」。\n纠正时亮起的格子，就是它刚学的东西。", "Green leans Yes; red leans No.\nCells light up when your lesson changes them.")
                : T("你已帮它纠正 ", "You corrected ") + sim.S.teachingCorrections + T(" 次。\n它连续答对 ", " times.\nIts correct streak: ") + sim.S.modelStreak
                  + (Time.unscaledTime < flash ? T("\n它的想法正在改变。", "\nIts ideas are changing.") : T("\n练完后，去问它刚才那道题。", "\nAfter training, ask it about that card."));
        }
    }
}
