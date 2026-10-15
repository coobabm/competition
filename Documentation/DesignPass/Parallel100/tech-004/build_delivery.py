"""Read production sources; write only this task's delivery directory."""
from pathlib import Path
import hashlib
import difflib
import json

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
BASE = ROOT / 'Assets/LingGuangV05/XingGuang'
TARGET = 'Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs'
paths = ['Core/' + n for n in ['XgTechTreeCopy.cs', 'XgCatalog.cs', 'XgSim.cs', 'XgSim.AutoModel.cs', 'XgSim.Progression.cs', 'XgSim.Board.cs', 'XgBoard.cs', 'XgSim.Abilities.cs', 'XgSim.StoryPacing.cs', 'XgTechTree.cs', 'XgSim.Finale.cs']] + ['Desktop/XgTechTreePage.cs']
hashes = {p: hashlib.sha256((BASE / p).read_bytes()).hexdigest().upper() for p in paths}
source = (ROOT / TARGET).read_text(encoding='utf-8-sig')
zh_explanation = '先把输入的一句话压成一份固定大小的摘要，再根据摘要组织输出；摘要装不下的细节可能丢失。'
en_explanation = 'Compress the input sentence into a fixed-size summary, then build the output from it; details that do not fit may be lost.'
zh_effect = '购买后加入序列训练线的候选结构，无需手动装备；系统只在轮次之间按数据与显存选用一种结构，各架构效果不叠加。选用后，需要编码—解码的数据不再额外计入 0.5 的结构误差项（不是准确率增加 50 个百分点）；翻译评分上限仍受 ×0.75 的瓶颈系数影响。训练处理量系数为 ×0.85，与其他处理量系数相乘；每轮卡数仍受算力、取整和上限影响。切换结构需要重新训练，再看评估成绩。'
en_effect = 'Buying adds a candidate for the sequence track; no manual equip is needed. Between epochs, the system selects one architecture for the data and VRAM; architecture effects do not stack. When selected, data requiring an encoder-decoder no longer adds the 0.5 structural error term (not a 50-percentage-point accuracy gain); the translation score ceiling still has a ×0.75 bottleneck factor. The training-throughput factor is ×0.85 and multiplies with other throughput factors; cards per epoch still depend on compute, rounding and caps. Train again after switching architecture, then check the evaluation score.'
anchor = '        static string Explanation(XgNode node, XgSim sim)\n        {\n'
assert source.count(anchor) == 1
updated = source.replace(anchor, anchor + '            if (node.kind == XgNodeKind.Arch && node.id == "seq2seq")\n                return T(sim, "' + zh_explanation + '", "' + en_explanation + '");\n', 1)
anchor = '            if (node.kind == XgNodeKind.Arch) return T(sim, "加入可用结构，系统按数据和显存选择合适的模型；训练后再看评估成绩。", "Add an available architecture. The system selects a model for the data and VRAM; check its score after training.");'
assert updated.count(anchor) == 1
updated = updated.replace(anchor, '            if (node.kind == XgNodeKind.Arch && node.id == "seq2seq")\n                return T(sim, "' + zh_effect + '", "' + en_effect + '");\n' + anchor, 1)
patch = ''.join(difflib.unified_diff(source.splitlines(True), updated.splitlines(True), fromfile='a/' + TARGET, tofile='b/' + TARGET))
(OUT / 'changes.patch').write_text(patch, encoding='utf-8')
(OUT / 'source-sha256.json').write_text(json.dumps(hashes, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(OUT / 'copy.md').write_text('# Seq2Seq 可采用文案\n\n仅对已显示节点呈现。名称沿用当前实现。\n\n## 中文\n\n1. 正式名称：序列到序列模型\n2. 通俗解释：' + zh_explanation + '\n3. 真实效果：' + zh_effect + '\n\n## English\n\n1. Name: Sequence-to-sequence model\n2. Explanation: ' + en_explanation + '\n3. Effect: ' + en_effect + '\n', encoding='utf-8')
assert updated.count('node.kind == XgNodeKind.Arch && node.id == "seq2seq"') == 2
assert source.count('if (node == null || sim.NodeMystery(node) || XgSim.IsAtlas(node))') == updated.count('if (node == null || sim.NodeMystery(node) || XgSim.IsAtlas(node))') == 2
assert all(hashlib.sha256((BASE / p).read_bytes()).hexdigest().upper() == h for p, h in hashes.items())
print('Delivery generated; source hashes unchanged during generation. Static text checks only; no C# compilation or game execution.')
