"""Create a reviewable four-layer-card candidate without writing Assets or starting Unity."""
from pathlib import Path
import hashlib
import json
import difflib

root = Path(__file__).resolve().parents[4]
out = Path(__file__).resolve().parent
source = root / 'Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs'
before = source.read_text(encoding='utf-8-sig')
text = before
def replace(old, new):
    global text
    assert text.count(old) == 1, old[:100]
    text = text.replace(old, new)

replace('SquareSize = 56, SlotWidth = 128, RowHeight = 118', 'SquareSize = 56, SlotWidth = 320, RowHeight = 194')
replace('public const int Columns = 5;', 'public const int Columns = 2;')
start = text.index('        void MakeNode(XgTechEntry e)')
end = text.index('        void MakeLinks(', start)
text = text[:start] + '        void MakeNode(XgTechEntry e) => MakeTechCard(e);\n\n' + text[end:]
replace('            var rows = new Dictionary<string, int>();', '            var rows = new Dictionary<string, int>();\n            var heights = new Dictionary<string, float>();')
replace('                slot[e] = new Vector2Int(c, r);', '                slot[e] = new Vector2Int(c, r);\n                string rowKey = bandId + ":" + r;\n                heights.TryGetValue(rowKey, out float currentHeight);\n                heights[rowKey] = Mathf.Max(currentHeight, TechCardHeight(e) + 20);')
replace('            float top = TopMargin;', '            float top = TopMargin + growthHeaderHeight - 154;')
replace('                bandTop[lane] = top; bandHeight[lane] = LaneHeader + count * RowHeight;', '                bandTop[lane] = top; bandHeight[lane] = LaneHeader;\n                for (int row = 0; row < count; row++) bandHeight[lane] += heights[lane + ":" + row];')
replace('                float y = bandTop[bandId] + LaneHeader + kv.Value.y * RowHeight + SquareSize * .5f + 6;', '                float y = bandTop[bandId] + LaneHeader + TechCardHeight(kv.Key) * .5f + 6;\n                for (int row = 0; row < kv.Value.y; row++) y += heights[bandId + ":" + row];')
replace('-treeHeight - SquareSize * .5f);', '-treeHeight - TechCardHeight(entry) * .5f);')
replace('            if (hasSecret) treeHeight += RowHeight + 20;', '            if (hasSecret)\n            {\n                float secretHeight = RowHeight;\n                foreach (var entry in XgTechTree.Entries) if (entry.First.kind == XgNodeKind.Secret) secretHeight = Mathf.Max(secretHeight, TechCardHeight(entry));\n                treeHeight += secretHeight + 20;\n            }')
replace('            BuildTooltip();', '            BuildTooltip();\n            BuildTechDetailsClose();')
replace('            header.text = "<b>"', '            LayoutTechCards();\n            header.text = "<b>"')
replace('            else v.cost.text = Hex(status == XgSim.NodeStatus.Buyable ? XgDark.Money : status == XgSim.NodeStatus.TooExpensive ? Unaffordable : XgDark.Dim) + (cost > 0 ? "¥" + Money(cost) : T("免费", "Free")) + "</color>";', '            else v.cost.text = Hex(status == XgSim.NodeStatus.Buyable ? XgDark.Money : status == XgSim.NodeStatus.TooExpensive ? Unaffordable : XgDark.Dim) + (cost > 0 ? "¥" + Money(cost) : T("免费", "Free")) + "</color>";\n            PaintTechCard(v);')
replace('            if (!built || v == null) return;', '            if (!built || v == null) return;\n            if (detailsOpen) return;')
replace('v.square.DOScale(1.08f, .12f)', 'v.square.DOScale(1.015f, .12f)')
replace('leave.callback.AddListener(_ => tipExitAt = Time.unscaledTime + .35f);', 'leave.callback.AddListener(_ => { if (!detailsOpen) tipExitAt = Time.unscaledTime + .35f; });')
replace('        void HideTooltip()\n        {\n            tipExitAt = -1;', '        void HideTooltip()\n        {\n            detailsOpen = false;\n            tipExitAt = -1;')
replace('            Vector2 top = root.InverseTransformPoint(v.square.TransformPoint(new Vector3(0, SquareSize * .5f, 0)));\n            Vector2 bottom = root.InverseTransformPoint(v.transform.TransformPoint(new Vector3(0, -SquareSize * .5f - 62, 0)));', '            Vector2 top = root.InverseTransformPoint(v.square.TransformPoint(new Vector3(0, v.square.rect.yMax, 0)));\n            Vector2 bottom = root.InverseTransformPoint(v.square.TransformPoint(new Vector3(0, v.square.rect.yMin, 0)));')
replace('        void FillTooltip(XgTechNodeView v)\n        {', '        void FillTooltip(XgTechNodeView v)\n        {\n            RefreshTechDetails();')
replace('            body.Append(XgTechTreeCopy.Note(shownNode, Sim));', '            body.Append(detailsOpen ? XgTechCardCopy.Science(e, Sim) : XgTechTreeCopy.Note(shownNode, Sim));')
replace('            tipName.text = XgTechTree.Name(e, Sim);', '            tipName.text = detailsOpen ? XgTechCardCopy.Name(e, Sim) : XgTechTree.Name(e, Sim);')
replace('            float w = XgTechTooltipGeometry.Width(bounds.width, TooltipWidth);', '            float w = XgTechTooltipGeometry.Width(bounds.width, TooltipWidth);\n            bounds = TechTooltipBounds(v, bounds, w);\n            if (bounds.height <= 8) { HideTooltip(); return; }')
replace('            float h = Mathf.Min(tipContent.rect.height, bounds.height - 8);', '            float h = Mathf.Min(tipContent.rect.height + (detailsOpen ? 36 : 0), bounds.height - 8);')
replace('Mathf.Max(0, tipContent.rect.height - h)', 'Mathf.Max(0, tipContent.rect.height - h + (detailsOpen ? 36 : 0))')
replace('body.Append(lit || ability ? T(node.note, node.noteEn) + "\\n\\n<color=#6F95A5>" + Sim.Why(node, Host) + "</color>"', 'body.Append(lit || ability ? XgTechCardCopy.Science(e, Sim) + "\\n\\n<color=#6F95A5>" + Sim.Why(node, Host) + "</color>"')
replace(': Lang.T("还没发生过。规则叠在一起，它自己会冒出来；第一次发生时会记进图鉴。"));', ': detailsOpen ? XgTechCardCopy.Science(e, Sim) : Lang.T("还没发生过。规则叠在一起，它自己会冒出来；第一次发生时会记进图鉴。"));')
(out / source.name).write_text(text, encoding='utf-8')
(out / 'production.patch').write_text(''.join(difflib.unified_diff(before.splitlines(True), text.splitlines(True), fromfile='a/' + source.relative_to(root).as_posix(), tofile='b/' + source.relative_to(root).as_posix())), encoding='utf-8')
runner = root / 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs'
runner_before = runner.read_text(encoding='utf-8-sig')
runner_after = runner_before
anchor = '            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "growth" && step >= 220)'
assert runner_after.count(anchor) == 1
runner_after = runner_after.replace(anchor, '            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "tech-cards" && step >= 250)\n            {\n                TickTechCards(controller, runtime); return;\n            }\n' + anchor)
anchor = '                    if (mode == "growth") step = 220;'
assert runner_after.count(anchor) == 1
runner_after = runner_after.replace(anchor, anchor + '\n                    if (mode == "tech-cards") step = 250;')
(out / runner.name).write_text(runner_after, encoding='utf-8')
(out / 'runner.patch').write_text(''.join(difflib.unified_diff(runner_before.splitlines(True), runner_after.splitlines(True), fromfile='a/' + runner.relative_to(root).as_posix(), tofile='b/' + runner.relative_to(root).as_posix())), encoding='utf-8')
manifest = { 'baseline': {item.relative_to(root).as_posix(): hashlib.sha256(item.read_bytes()).hexdigest() for item in [source, runner]}, 'candidate': {} }
for item in out.glob('*.cs'): manifest['candidate'][item.name] = hashlib.sha256(item.read_bytes()).hexdigest()
(out / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print('Candidate written; Assets unchanged.')
