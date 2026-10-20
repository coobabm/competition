using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgAtlasCategory { Abilities, Phenomena, Networks }

    // A projection of existing collection rights. Opening it never discovers, collects or buys anything.
    public sealed class XgAtlasEntry
    {
        public string id, title, body;
        public int stage;
        public bool unlocked;
        public XgAtlasCategory category;
    }

    public static class XgUnifiedAtlasCatalog
    {
        public const string ViewerNode = "train.weights";
        public static bool ViewerOpen(XgSim sim) => sim != null && sim.TrainPart("weights");
        public static bool AbilityOpen(XgSim sim, int stage)
            => sim != null && stage >= 1 && stage <= XgSim.AbilityCount && sim.HasAbility(stage);
        public static bool CanReplay(XgSim sim, int stage) => ViewerOpen(sim) && AbilityOpen(sim, stage);
        public static bool NetworkOpen(XgSim sim, string id)
        {
            if (sim == null || XgCatalog.Arch(id) == null) return false;
            if (sim.Has(id)) return true;
            // Same legacy entitlement as the existing topology atlas.
            foreach (var run in sim.Runs) if (run.arch == id) return true;
            return false;
        }
        static string T(bool en, string zh, string english) => en ? english : zh;
        public static List<XgAtlasEntry> Entries(XgSim sim, XgAtlasCategory category, bool en)
        {
            var result = new List<XgAtlasEntry>();
            if (sim == null) return result;
            if (category == XgAtlasCategory.Abilities)
            {
                for (int stage = 1; stage <= XgSim.AbilityCount; stage++)
                {
                    bool open = AbilityOpen(sim, stage);
                    var node = XgCatalog.Node("ab." + stage);
                    result.Add(new XgAtlasEntry { id = "ab." + stage, stage = stage, category = category, unlocked = open,
                        title = T(en, "阶段 ", "Stage ") + stage + " · " + XgSim.AbilityName(stage, en),
                        body = open && node != null ? T(en, node.note, node.noteEn) : T(en, "获得这项能力后可以重览。", "Replay becomes available after this ability is learned.") });
                }
            }
            else if (category == XgAtlasCategory.Phenomena)
            {
                // Includes the original phenomena, collected knowledge cards and answered quizzes.
                foreach (var node in XgCatalog.Nodes)
                {
                    if (node.kind != XgNodeKind.Phenomenon) continue;
                    bool open = sim.AtlasLit(node);
                    result.Add(new XgAtlasEntry { id = node.id, stage = node.stage, category = category, unlocked = open,
                        title = open ? T(en, node.name, node.nameEn) : T(en, "尚未收录", "Not collected"),
                        body = open ? T(en, node.note, node.noteEn) : T(en, "发生并收录以后，这里才会显示内容。", "This entry appears after it has been recorded in your collection.") });
                }
            }
            else
            {
                var architectures = new List<XgArch>(XgCatalog.Archs);
                architectures.Sort((a, b) => a.year != b.year ? a.year.CompareTo(b.year) : string.CompareOrdinal(a.id, b.id));
                foreach (var arch in architectures)
                {
                    bool open = NetworkOpen(sim, arch.id);
                    result.Add(new XgAtlasEntry { id = arch.id, category = category, unlocked = open,
                        title = open ? T(en, arch.name, arch.nameEn) : T(en, "未解锁结构", "Locked network"),
                        body = open ? T(en, arch.wire, arch.wireEn) + "\n\n" + T(en, arch.note, arch.noteEn)
                            : T(en, "先在科技页解锁相应结构。已有存档的结构记录会保留。", "Unlock this network in Tech. Existing network records are preserved.") });
                }
            }
            return result;
        }
    }
}
