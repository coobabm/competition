"""Build a review-only GRU copy patch. Never writes production sources."""
from pathlib import Path
import difflib
import hashlib
import json

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
TARGET = "Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs"
CORE = "Assets/LingGuangV05/XingGuang/Core/"
DEPENDENCIES = [TARGET] + [CORE + name + ".cs" for name in (
    "XgCatalog", "XgSim", "XgSim.AutoModel", "XgSim.Progression",
    "XgSim.Board", "XgBoard", "XgSim.EpochAction", "XgTechTree"
)] + ["Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs"]
original = (ROOT / TARGET).read_text(encoding="utf-8-sig")
zh_explanation = "边读边筛选记忆：学到有用的线索就多留一会儿，其余线索更快淡去。"
en_explanation = "Filter memory while reading: keep clues the model has learned are useful for longer, and let other clues fade faster."
zh_effect = "购买后加入序列模型候选，无需手动装备；系统按数据和显存自动选用，正在训练时等本轮结束再切换。选用后，每轮训练卡数的结构系数为 ×1.05（LSTM 为 ×0.8），与其他处理量倍率相乘，不与其他结构叠加；实际卡数仍受取整和上限影响。同层数、同宽度时，参数量为 LSTM 的 75%。门控记忆与 LSTM 相同，仍会随距离衰减。换结构可能重置学习进度，需继续训练并评估，不保证立即提分。"
en_effect = "Buying adds GRU to the sequence-model candidates; no manual equip is needed. The system selects a model for the data and VRAM, waiting until the current epoch ends before switching. When selected, its architecture factor for training cards per epoch is ×1.05 (LSTM: ×0.8). This multiplies other throughput factors, but does not stack with other architectures; rounding and caps still apply. At the same depth and width, it has 75% of LSTM's parameters. It shares LSTM's gated-memory rule and still fades with distance. Switching architecture may reset learning progress; continue training and evaluate, with no guaranteed immediate score gain."
anchor1 = "        static string Explanation(XgNode node, XgSim sim)\n        {\n"
anchor2 = '            if (node.kind == XgNodeKind.Arch) return T(sim, "加入可用结构'
assert original.count(anchor1) == 1
assert original.count(anchor2) == 1
assert original.count('case "gru":') == 1, "Re-review changed GRU copy before rebuilding."
changed = original.replace(anchor1, anchor1 + '            if (node.kind == XgNodeKind.Arch && node.id == "gru")\n'
    + f'                return T(sim, "{zh_explanation}", "{en_explanation}");\n')
changed = changed.replace(anchor2, '            if (node.kind == XgNodeKind.Arch && node.id == "gru")\n'
    + f'                return T(sim, "{zh_effect}", "{en_effect}");\n' + anchor2)
patch = "".join(difflib.unified_diff(original.splitlines(True), changed.splitlines(True),
    fromfile="a/" + TARGET, tofile="b/" + TARGET, n=3))
(OUT / "changes.patch").write_text(patch, encoding="utf-8", newline="\n")
manifest = {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in DEPENDENCIES}
(OUT / "source-sha256.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
(OUT / "copy.md").write_text(
    "# GRU 可直接采用的文案\n\n## 中文\n\n"
    "正式名称：门控循环单元（GRU）\n\n通俗解释：" + zh_explanation + "\n\n效果：" + zh_effect
    + "\n\n## English\n\nFormal name: Gated recurrent unit (GRU)\n\nPlain explanation: "
    + en_explanation + "\n\nEffect: " + en_effect + "\n", encoding="utf-8")
print("Review artifacts created only in", OUT)
print("Target source SHA256:", manifest[TARGET])
