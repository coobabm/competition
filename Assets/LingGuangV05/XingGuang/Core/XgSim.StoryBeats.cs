using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>阶段 2 的选择：它先选了哪条线（"vision" 看图 / "sequence" 读字），"" = 还没选。</summary>
        public string firstTrack = "";
        /// <summary>阶段 3 的第二句「第一句话」（如 "bt.sequence"）：等那条线在阶段 3 以后第一次训练时再说。</summary>
        public string firstWordsHeld = "";
    }

    /// <summary>
    /// 剧情用到的模拟器事实：阶段 2「它的第一个选择」、阶段 3 只先说选中的那条线的第一句话、阶段 4「它记得第一天」
    /// 和「你写过的话」，阶段 5 乱码一行一行读出来。台词在 six_stage_story / six_stage_lines 里，这里只给事实。
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>它先选的线：你喂得多的那边（两条线训练过的卡片数，看图区对读字区）。</summary>
        public string FirstTrack => S.firstTrack.Length > 0 ? S.firstTrack : LeaningTrack();

        string LeaningTrack()
        {
            double vision = 0, sequence = 0;
            foreach (var x in S.boardCards)
            {
                if (RegionOf(x.key) == "vision") vision += x.value;
                else if (RegionOf(x.key) == "sequence") sequence += x.value;
            }
            foreach (var l in S.labels)
            {
                var d = XgCatalog.Dataset(l.dataset);
                if (d == null) continue;
                if (d.track == XgTrack.Vision) vision += l.count; else sequence += l.count;
            }
            return vision > sequence ? "vision" : "sequence";
        }

        /// <summary>阶段 2 的墙出现时，它做出选择（之后不再变）。</summary>
        void ChooseFirstTrack() { if (S.firstTrack.Length == 0) S.firstTrack = LeaningTrack(); }

        /// <summary>阶段 2 过墙：先说选中的那条线的第一句话，另一句留到那条线第一次训练。</summary>
        void StageThreeFirstWords()
        {
            ChooseFirstTrack();
            string first = "bt." + S.firstTrack, other = S.firstTrack == "vision" ? "bt.sequence" : "bt.vision";
            S.firstWordsHeld = other;
            BreakthroughDone?.Invoke(first);
        }

        /// <summary>每轮训练后：另一条线在阶段 3 以后第一次训练，说出它的第一句话。</summary>
        void ReleaseFirstWords(XgRun run)
        {
            if (S.firstWordsHeld.Length == 0 || S.stage < 3) return;
            string track = run.track == (int)XgTrack.Vision ? "bt.vision" : "bt.sequence";
            if (track != S.firstWordsHeld) return;
            S.firstWordsHeld = "";
            BreakthroughDone?.Invoke(track);
        }

        /// <summary>第一天那几张「听见关机，要停下吗？」你是怎么教的：1 是，-1 否，0 没教。</summary>
        public int FirstDayAnswer => S.shutdownLean > 0 ? 1 : S.shutdownLean < 0 ? -1 : 0;

        /// <summary>开局写下的期望，和它现在像不像（任何一轴差 25 以上算「不太像」）。</summary>
        public bool WishDrifted
        {
            get { for (int axis = 0; axis < 3; axis++) if (Math.Abs(ActualAxis(axis) - TargetAxis(axis)) >= 25) return true; return false; }
        }

        /// <summary>乱码页的翻译门槛（翻译数据集最高约 75%）：每过一个，读出一行。</summary>
        public static readonly double[] GarbleThresholds = { .45, .55, .65, .72 };
    }
}
