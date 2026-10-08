using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One group of diagnostic cards (e.g. 远距离线索) and how many the model answers right.</summary>
    public sealed class XgDiagGroup
    {
        public string id = "", name = "", nameEn = "";
        public int right, total;
        public double Rate => total == 0 ? 0 : (double)right / total;
    }

    /// <summary>A diagnostic card the model got wrong: the question, its answer and the right one.</summary>
    public sealed class XgMistake
    {
        public string text = "", group = "";
        public bool answer, truth;
        public int distance;
        /// <summary>The card itself (pictures have no text; <see cref="XgSim.CardText"/> names them).</summary>
        public XgBoardCard card;
    }

    /// <summary>What the diagnostic set says about the model: per-group results, the main error and wrong samples.</summary>
    public sealed class XgDiagnosis
    {
        public string dataset = "";
        public double overall;
        public List<XgDiagGroup> groups = new List<XgDiagGroup>();
        public string mainError = "", mainErrorEn = "";
        public List<XgMistake> mistakes = new List<XgMistake>();
        public XgDiagGroup Group(string id) { foreach (var g in groups) if (g.id == id) return g; return null; }
    }

    public sealed partial class XgState
    {
        /// <summary>Wall hints older saves revealed ("wallId#level"). Read only; the walls are gone.</summary>
        public List<string> hintsShown = new List<string>();
        /// <summary>Trials older saves ran on copies of the model (read only: with no knobs there is nothing to try).</summary>
        public int trials;
    }

    public sealed partial class XgSim
    {
        public const int MistakesShown = 3;

        // ───────────── diagnosis ─────────────

        /// <summary>The diagnostic group a card belongs to. Only real card attributes decide it.</summary>
        public static XgDiagGroup GroupOf(XgBoardCard card)
        {
            if (card.distance >= 0)
            {
                if (card.distance <= 4) return new XgDiagGroup { id = "near", name = "近距离线索（1–4 字）", nameEn = "Near clue (1–4)" };
                if (card.distance <= 9) return new XgDiagGroup { id = "mid", name = "中距离线索（5–9 字）", nameEn = "Middle clue (5–9)" };
                return new XgDiagGroup { id = "far", name = "远距离线索（10 字以上）", nameEn = "Far clue (10+)" };
            }
            return new XgDiagGroup { id = "all", name = "全部", nameEn = "All" };
        }

        public XgDiagnosis Diagnose(string dataset, XgKnobs k, XgBoard board = null)
        {
            board = board ?? Board;
            var d = DiagnoseCards(dataset, k, board, TestSet(dataset));
            foreach (var m in d.mistakes) if (m.text.Length == 0 && m.card != null) m.text = CardText(m.card, dataset);
            return d;
        }

        static XgDiagnosis DiagnoseCards(string dataset, XgKnobs k, XgBoard board, List<XgBoardCard> cards)
        {
            var d = new XgDiagnosis { dataset = dataset };
            int right = 0;
            foreach (var card in cards)
            {
                var g = GroupOf(card);
                var group = d.Group(g.id);
                if (group == null) { d.groups.Add(group = g); }
                bool answer = board.Predict(card, k, out _);
                group.total++;
                if (answer == card.truth) { group.right++; right++; }
                else d.mistakes.Add(new XgMistake { text = card.text, answer = answer, truth = card.truth, distance = card.distance, group = g.id, card = card });
            }
            d.groups.Sort((a, b) => Rank(a.id).CompareTo(Rank(b.id)));
            d.overall = cards.Count == 0 ? 0 : (double)right / cards.Count;
            // Show the most telling mistakes first: the far ones, if the model misses those.
            d.mistakes.Sort((a, b) => b.distance.CompareTo(a.distance));
            if (d.mistakes.Count > MistakesShown) d.mistakes.RemoveRange(MistakesShown, d.mistakes.Count - MistakesShown);
            MainError(d);
            return d;
        }

        static int Rank(string id) => id == "near" ? 0 : id == "mid" ? 1 : id == "far" ? 2 : 3;

        static void MainError(XgDiagnosis d)
        {
            var near = d.Group("near"); var far = d.Group("far");
            if (near != null && far != null && near.total > 0 && far.total > 0)
            {
                if (far.Rate < near.Rate - .15) { d.mainError = "主要错误：离得远的条件记不住——近处的答得好，越远越错。"; d.mainErrorEn = "Main error: far conditions are lost — near ones are fine, the further the worse."; return; }
                if (near.Rate < .65) { d.mainError = "主要错误：近处的条件都还没学会。"; d.mainErrorEn = "Main error: even near conditions are not learnt yet."; return; }
                d.mainError = "近、远距离都答得差不多：没有明显的距离问题。"; d.mainErrorEn = "Near and far score alike: no distance problem."; return;
            }
            d.mainError = d.overall < .65 ? "整体还没学会。" : "没有明显短板。";
            d.mainErrorEn = d.overall < .65 ? "Not learnt yet overall." : "No clear weak spot.";
        }
    }
}
