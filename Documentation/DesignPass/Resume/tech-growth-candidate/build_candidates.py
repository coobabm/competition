"""Prepare reviewable fragments outside Assets; never apply them or launch Unity."""
from pathlib import Path
import difflib
import hashlib
import json

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
manifest = []

def prepare(relative, edits):
    source = ROOT / relative
    original = source.read_text(encoding="utf-8-sig")
    candidate = original
    for before, after in edits:
        if candidate.count(before) != 1:
            raise AssertionError(f"Expected one fragment in {relative}: {before[:90]!r}; got {candidate.count(before)}")
        candidate = candidate.replace(before, after, 1)
    patch = "".join(difflib.unified_diff(original.splitlines(True), candidate.splitlines(True),
                                      fromfile="a/" + relative, tofile="b/" + relative, n=3))
    (OUT / (source.stem + ".patch")).write_text(patch, encoding="utf-8", newline="\n")
    # The View/Frame are shared: retain only small diff fragments, never a full replacement file.
    if source.name == "XgTechTreePage.cs":
        (OUT / source.name).write_text(candidate, encoding="utf-8")
    manifest.append({"path": relative, "sha256": hashlib.sha256(source.read_bytes()).hexdigest(), "fragments": len(edits)})

prepare("Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs", [
    ("public sealed class XgTechTreePage : XgPage", "public sealed partial class XgTechTreePage : XgPage"),
    ("LaneGap = 12, TopMargin = 64;", "LaneGap = 12, TopMargin = 180;"),
    ("// 科技 has its own nav row (养成), so the page needs no way back to 道具.", "// One growth route now owns technologies, items and ability progress."),
    ("content.sizeDelta = new Vector2(StageWidth * 2, treeHeight);", "content.sizeDelta = new Vector2(StageWidth * XgSim.AbilityCount + 40, treeHeight);"),
    ("for (int i = 0; i < XgCatalog.StageNames.Length; i++) BuildStageHeader(i);", "for (int i = 0; i < XgSim.AbilityCount; i++) BuildStageHeader(i);"),
    ('foreach (var e in XgTechTree.Entries) MakeNode(e);', 'foreach (var e in XgTechTree.Entries) if (e.First.kind != XgNodeKind.Ability) MakeNode(e);'),
    ("            BuildEraCard();\n            built = true;", "            BuildEraCard();\n            BuildGrowthShelf();\n            built = true;"),
    ('string bandId = XgCatalog.Band(e.Lane), key = e.Stage + ":" + bandId;', 'if (e.First.kind == XgNodeKind.Ability) continue; // The route headers now display these abilities.\n                string bandId = XgCatalog.Band(e.Lane), key = GrowthColumn(e) + ":" + bandId;'),
    ('parent.Stage == e.Stage && XgCatalog.Band(parent.Lane)', 'GrowthColumn(parent) == GrowthColumn(e) && XgCatalog.Band(parent.Lane)'),
    ('float x = kv.Key.Stage * StageWidth + 24', 'float x = GrowthColumn(kv.Key) * StageWidth + 24'),
    ('                position[kv.Key] = new Vector2(x, -y);\n            }\n        }',
     '                position[kv.Key] = new Vector2(x, -y);\n            }\n            bool hasSecret = false;\n            foreach (var entry in XgTechTree.Entries)\n                if (entry.First.kind == XgNodeKind.Secret)\n                {\n                    position[entry] = new Vector2((GrowthColumn(entry) + 1) * StageWidth - SlotWidth * .5f - 24, -treeHeight - SquareSize * .5f);\n                    hasSecret = true;\n                }\n            if (hasSecret) treeHeight += RowHeight + 20;\n        }'),
    ('var title = ui.Text(TopLeft("StageHeader" + i, content, i * StageWidth + 24, 12, StageWidth - 40, 36), "", 21, XgDark.Ink, TextAlignmentOptions.MidlineLeft);',
     'var title = BuildGrowthAbility(i);'),
    ('if (e.Stage == i && XgCatalog.Band(e.Lane) == lane)', 'if (e.First.kind != XgNodeKind.Ability && GrowthColumn(e) == i && XgCatalog.Band(e.Lane) == lane)'),
    ('miniMap = Strip("DiscoveredStages", root, 48, 28, 16, 310);', 'miniMap = Strip("DiscoveredStages", root, 48, 28, 12, 310);'),
    ('                bool visible = i <= stage;\n                pair.Value.gameObject.SetActive(visible);\n                if (visible) pair.Value.text = (i == 0 ? "⓪ " : i + " · ") + T(XgCatalog.StageNames[i], XgCatalog.StageNamesEn[i]) + (i > 0 ? "  <size=14><color=#6F95A5>" + XgCatalog.ShownYear(XgCatalog.StageYears[i]) + "</color></size>" : "");',
     '                bool visible = true; // The six known ability names are the route; catalog mystery nodes retain their own visibility.\n                pair.Value.gameObject.SetActive(visible);'),
    ('var button = ui.Button(miniMap, "", () => FocusStage(column), 13);', 'var button = ui.Button(miniMap, "", () => FocusGrowthAbility(column + 1), 13);'),
    ('button.rt.anchoredPosition = new Vector2(i * 56, 0); button.rt.sizeDelta = new Vector2(50, 25);', 'button.rt.anchoredPosition = new Vector2(i * 35, 0); button.rt.sizeDelta = new Vector2(31, 25);'),
    ('mini.Set(i == 0 ? Lang.T("标注") : i.ToString(), true, i == stage ? XgDark.AccentSoft : XgDark.Button);',
     'mini.Set((i + 1).ToString(), true, i + 1 == Sim.NextAbility ? XgDark.AccentSoft : XgDark.Button);'),
    ('title.gameObject.SetActive(i <= stage);\n                if (i <= stage) title.GetComponent<TMP_Text>().text = LaneTitle(parts[1], i);',
     'bool visible = GrowthLaneVisible(parts[1], i);\n                title.gameObject.SetActive(visible);\n                if (visible) title.GetComponent<TMP_Text>().text = LaneTitle(parts[1], i);'),
    ('content.sizeDelta = new Vector2((stage + 1) * StageWidth + 40, treeHeight);\n            frontierWall.gameObject.SetActive(stage < XgCatalog.StageNames.Length - 1);',
     'content.sizeDelta = new Vector2(XgSim.AbilityCount * StageWidth + 40, treeHeight);\n            frontierWall.gameObject.SetActive(false);'),
    ('miniMap.gameObject.SetActive(stage > 1); toolbar[1].rt.gameObject.SetActive(stage > 1);', 'toolbar[1].rt.gameObject.SetActive(true);\n            RefreshGrowth();'),
    ('header.text = "<b>" + Lang.T("科技")', 'header.text = "<b>" + T("成长", "Growth")'),
    ('public void FocusFrontier() => FocusStage(Sim.S.stage);', 'public void FocusFrontier() => FocusGrowthAbility(Sim.NextAbility > 0 ? Sim.NextAbility : XgSim.AbilityCount);'),
    ('        public RectTransform NodeTarget(string id)\n        {\n            var v = views.Find',
     '        public RectTransform NodeTarget(string id)\n        {\n            if (id != null && id.StartsWith("ab.") && int.TryParse(id.Substring(3), out int ability)) return GrowthAbilityTarget(ability);\n            var v = views.Find'),
    ('        public bool FocusNode(string id)\n        {\n            if (!built) return false;',
     '        public bool FocusNode(string id)\n        {\n            if (!built) return false;\n            if (id != null && id.StartsWith("ab.") && int.TryParse(id.Substring(3), out int ability) && GrowthAbilityTarget(ability) != null)\n            { FocusGrowthAbility(ability); return true; }'),
])

prepare("Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.cs", [
    ('if (tabId == "research") tabId = "tree";', 'if (tabId == "research" || tabId == "growth") tabId = "tree";'),
    ('        public void ShowTab(string id)\n        {\n            if (id == "label") id = "teach";',
     '        public void ShowTab(string id)\n        {\n            string requested = id;\n            if (id == "items" || id == "abilities" || id == "growth") id = "tree";\n            if (id == "label") id = "teach";'),
    ('            pages[id].Shown();\n            Juice.BringToFront();\n            Refresh(true);',
     '            pages[id].Shown();\n            Juice.BringToFront();\n            Refresh(true);\n            if (requested == "items") Tree.SetGrowthShelf(true);\n            if (requested == "abilities") Tree.FocusGrowthAbility(Sim.NextAbility > 0 ? Sim.NextAbility : XgSim.AbilityCount);'),
])

prepare("Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Frame.cs", [
    ('            if (id == "label") id = "teach";\n            return id != null && navRows.TryGetValue(id, out var row)',
     '            if (id == "label") id = "teach";\n            if (id == "items" || id == "abilities" || id == "growth") id = "tree";\n            return id != null && navRows.TryGetValue(id, out var row)'),
    ('case "tree": return T("科技", "Tech");', 'case "tree": return T("成长", "Growth");'),
    ('case "tree": { int n = Items != null ? Items.TreeBuyableCount() : 0;', 'case "tree": { int n = Tree != null ? Tree.GrowthBuyableCount() : 0;'),
    ('case "tree": return T("科技：加宽、加深的上限，数据包，自动化（crontab、守护进程），研究，机房和预训练秘籍。按住节点 0.6 秒购买。", "Tech: width and depth caps, data packs, automation (crontab, daemon), research, the server room and the pre-training secret. Hold a node 0.6 s to buy.");',
     'case "tree": return T("成长：六项能力、参数和样本门槛，以及结构、技巧和数据包。点击节点购买；消耗品在右侧货架。", "Growth: six abilities, their parameter and sample thresholds, structures, techniques and data packs. Click a node to buy; consumables are on the right shelf.");'),
    ('        bool NavVisible(string id)\n        {\n            if (id == "home") return true;',
     '        bool NavVisible(string id)\n        {\n            if (id == "items" || id == "abilities") return false; // Legacy routes remain available through the single Growth row.\n            if (id == "home") return true;'),
])
(OUT / "source-manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(json.dumps({"candidate_only": True, "files": manifest}, ensure_ascii=False))
