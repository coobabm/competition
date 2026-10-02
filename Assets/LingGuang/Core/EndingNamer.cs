using System;
using System.Collections.Generic;
using System.Linq;

namespace LingGuang.Core
{
    /// <summary>§13 (P1, simplified): names the grown network from whole-run statistics. Classification only, no rule effects.</summary>
    public static class EndingNamer
    {
        // [bright 0..2][warm 0..2] -> three names for 浓/中/淡
        static readonly string[,][] Colors =
        {
            { new[] { "朱砂", "杏黄", "缃色" }, new[] { "松花", "竹青", "月白" }, new[] { "群青", "天水碧", "霜色" } },
            { new[] { "胭脂", "檀色", "藕荷" }, new[] { "秋香", "驼色", "苍色" }, new[] { "靛青", "黛蓝", "丁香" } },
            { new[] { "绛紫", "赭石", "栗色" }, new[] { "黛绿", "墨灰", "烟灰" }, new[] { "藏青", "鸦青", "玄青" } },
        };

        static readonly Dictionary<Region, string> RegionTrait = new Dictionary<Region, string>
        {
            { Region.I, "敏于世界" }, { Region.K, "一直在行动" }, { Region.C, "追随心之所向" }, { Region.F, "深思熟虑" }, { Region.T, "珍藏往事" },
        };

        public static readonly Dictionary<string, string> ColorHex = new Dictionary<string, string>
        {
            { "朱砂", "#ff461f" }, { "杏黄", "#ffa631" }, { "缃色", "#f0c239" }, { "松花", "#bce672" }, { "竹青", "#789262" }, { "月白", "#d6ecf0" },
            { "群青", "#4c8dae" }, { "天水碧", "#5aa4ae" }, { "霜色", "#e9f1f6" }, { "胭脂", "#9d2933" }, { "檀色", "#b36d61" }, { "藕荷", "#e4c6d0" },
            { "秋香", "#d9b611" }, { "驼色", "#a88462" }, { "苍色", "#75878a" }, { "靛青", "#177cb0" }, { "黛蓝", "#425066" }, { "丁香", "#cca4e3" },
            { "绛紫", "#8c4356" }, { "赭石", "#845a33" }, { "栗色", "#60281e" }, { "黛绿", "#426666" }, { "墨灰", "#758a99" }, { "烟灰", "#8c8c8c" },
            { "藏青", "#2e4e7e" }, { "鸦青", "#424c50" }, { "玄青", "#3d3b4f" },
        };

        public static string LastColorName;

        public static (string title, string body) Describe(RunState s)
        {
            var st = s.stats;
            int rounds = Math.Max(1, s.history.Count);
            // 明暗
            float bright = 0;
            foreach (var r in s.history)
            {
                float ratio = r.target > 0 ? (float)r.score / r.target : 0;
                if (!r.passed) bright -= 2;
                else if (ratio >= 1.5f) bright += 1;
                else if (ratio < 1.2f) bright -= 1;
            }
            bright = bright / rounds - 0.1f * st.warnings;
            int bi = bright > 0.3f ? 0 : bright < -0.3f ? 2 : 1;
            // 冷暖
            float warm = 2f * st.roundsWithFamiliar / rounds + 0.5f * st.knownCount - 1f * st.broken - 2f * (rounds - st.roundsWithFamiliar) / rounds;
            int wi = warm > 1f ? 0 : warm < -0.5f ? 2 : 1;
            // 浓淡
            float dense = st.patterns.Count / 5f + Math.Min(1f, st.maxOvershoot / 3f) + Math.Min(1f, st.reverbCount / 30f) + Math.Min(1f, (st.itemsUsed + st.drugsUsed) / 20f);
            int di = dense > 1.6f ? 0 : dense < 0.9f ? 2 : 1;
            string color = Colors[bi, wi][di];
            LastColorName = color;

            string connect = st.familiarEver >= 5 ? "群星" : st.knownCount >= 1 ? "知交" : st.charactersMet >= 6 && st.familiarEver <= 1 ? "过客如云" : st.familiarEver == 0 ? "独光" : "相识";
            st.patterns.TryGetValue("long", out int nLong);
            st.patterns.TryGetValue("diverge", out int nDiv);
            st.patterns.TryGetValue("converge", out int nCon);
            string way;
            int maxPattern = Math.Max(nLong, Math.Max(nDiv, nCon));
            if (st.reverbCount >= 8 && st.reverbCount > maxPattern) way = "回响";
            else if (maxPattern == 0) way = "微光";
            else if (nLong == maxPattern) way = "远行";
            else if (nDiv == maxPattern) way = "绽放";
            else way = "汇流";

            // personality: brightest region by growth
            var growth = new Dictionary<Region, float>();
            foreach (Region r in new[] { Region.I, Region.K, Region.C, Region.F, Region.T })
            {
                var cells = s.board.AllCells().Where(c => s.board.regionOf[c] == r).ToHashSet();
                int sum = s.edges.Values.Where(e => s.sparks.ContainsKey(e.from) && cells.Contains(s.sparks[e.from].cell)).Sum(e => e.count);
                growth[r] = cells.Count > 0 ? sum / (float)cells.Count : 0;
            }
            var top = growth.OrderByDescending(kv => kv.Value).First();
            string trait = top.Value > 0 ? RegionTrait[top.Key] : "尚未成形";

            var textures = new List<string>();
            if (st.broken > 0 && st.knownCount > st.broken) textures.Add("金缮");
            if (st.broken > 0 && st.knownCount <= st.broken) textures.Add("冰裂");
            if (st.longRelation) textures.Add("年轮");
            if (st.warnings >= 3) textures.Add("烟痕");
            if (st.hiddenCollected >= 3) textures.Add("星点");
            var lines = new List<string>
            {
                $"底色 <b>{color}</b>    连接 <b>{connect}</b>    方式 <b>{way}</b>" + (textures.Count > 0 ? $"    纹理 {string.Join("·", textures)}" : ""),
                $"性格：{trait}",
                $"走过 {s.history.Count} 轮，到达{s.cfg.stageNames[Math.Min(s.stage, 5)]}",
                $"回路型：{string.Join("  ", st.patterns.Select(kv => $"{PatternName(s, kv.Key)}×{kv.Value}"))}",
                $"回荡 {st.reverbCount} 次    最大超出 {st.maxOvershoot:0.0} 倍    遇见流光 {st.charactersMet} 道",
            };
            return ($"{color} · {connect} · {way}", string.Join("\n", lines));
        }

        static string PatternName(RunState s, string id) => s.cfg.patterns.FirstOrDefault(p => p.id == id)?.name ?? id;
    }
}
