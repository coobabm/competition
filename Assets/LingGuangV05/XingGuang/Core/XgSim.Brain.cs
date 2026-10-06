using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The protagonist has worked out that a "network" is a way of wiring 灵光's one brain (said once).</summary>
        public bool wiringVoiced;
        /// <summary>The topology each region was last wired with (same order as <see cref="XgSim.BrainRegions"/>; "" = never).</summary>
        public List<string> regionWire = new List<string>();
        /// <summary>The protagonist has seen the cortex topology map once (the shock is said once).</summary>
        public bool cortexSeen;
    }

    /// <summary>
    /// 灵光 has one brain, the concept board, in regions the way a cortex has areas: seeing (like the visual cortex),
    /// reading (like the language areas), reasoning (like the prefrontal cortex) and tone. Perceptron, LeNet, LSTM or
    /// Transformer are not other AIs: each is a way of wiring one region (who reaches whom, rule R6). The two training
    /// tracks train two regions of the same brain.
    /// </summary>
    public sealed partial class XgSim
    {
        public static readonly string[] BrainRegions = { "vision", "sequence", "logic", "tone" };

        /// <summary>A region's name, and the part of a human brain it plays.</summary>
        public static string RegionName(string region, bool english)
        {
            switch (region)
            {
                case "vision": return english ? "Seeing" : "看图区";
                case "sequence": return english ? "Reading" : "读字区";
                case "logic": return english ? "Reasoning" : "推理区";
                case "tone": return english ? "Tone" : "语气区";
                default: return region;
            }
        }

        public static string RegionLikeness(string region, bool english)
        {
            switch (region)
            {
                case "vision": return english ? "like the visual cortex" : "像视觉皮层";
                case "sequence": return english ? "like the language areas" : "像语言区";
                case "logic": return english ? "like the prefrontal cortex" : "像前额叶";
                case "tone": return english ? "like the parts that feel" : "像管情绪的那部分";
                default: return "";
            }
        }

        /// <summary>The architecture a region is wired with now (the run training it), or null when no run trains it.</summary>
        public XgArch RegionWiring(string region)
        {
            foreach (var run in Runs) if (RegionOf(run.dataset) == region) return XgCatalog.Arch(run.arch);
            return null;
        }

        /// <summary>The architecture a region is wired with now, or the one it was last wired with (it keeps its wiring when no run trains it).</summary>
        public XgArch RegionWiringOrLast(string region)
        {
            var now = RegionWiring(region);
            if (now != null) return now;
            int i = System.Array.IndexOf(BrainRegions, region);
            return i >= 0 && S.regionWire != null && i < S.regionWire.Count && S.regionWire[i].Length > 0 ? XgCatalog.Arch(S.regionWire[i]) : null;
        }

        /// <summary>Every step: remember how each trained region is wired.</summary>
        void NoteRegionWiring()
        {
            if (S.regionWire == null) S.regionWire = new List<string>();
            while (S.regionWire.Count < BrainRegions.Length) S.regionWire.Add("");
            foreach (var run in Runs)
            {
                int i = System.Array.IndexOf(BrainRegions, RegionOf(run.dataset));
                if (i >= 0 && run.arch.Length > 0) S.regionWire[i] = run.arch;
            }
        }

        /// <summary>A run of this region is in the middle of an epoch.</summary>
        public bool RegionTraining(string region)
        {
            foreach (var run in Runs) if (run.epochActive && RegionOf(run.dataset) == region) return true;
            return false;
        }

        /// <summary>The region a track's current dataset trains.</summary>
        public string RegionOfTrack(XgTrack track) => RegionOf(Run(track).dataset);
    }
}
