# tech-008 — step（阶跃激活函数）

结论：确认一处效果文案错误，交付仅修改 XgTechTreeCopy.Effect 中 step 分支的 changes.patch。未修改任何生产文件、模拟数值或其他词条。名称与通俗解释正确，原样保留。sigmoid 仅从共享 case 拆出，其返回文案不变。

## 可直接采用的中英文三项文案

| 项目 | 中文 | English |
| --- | --- | --- |
| 正式名称 | 阶跃激活函数 | Step activation function |
| 通俗解释 | 像开关一样：线索总分过门槛就开，否则就关。 | Like a switch: turn on when the combined clues cross the threshold, otherwise stay off. |
| 真实效果 | 购买后自动计入准确率上限公式：+1 个百分点（加 0.01，非乘 1.01）；与同类加成相加，再受结构、数据集等限制。无需装备；继续训练并评估才可能提高成绩，不保证每轮提分。阶跃原本即可使用，购买不会新增激活函数。 | Buying this automatically adds 1 percentage point (+0.01, not ×1.01) to the accuracy-ceiling formula. It adds to similar bonuses before architecture, dataset and other limits apply. No equipping is needed; further training and evaluation may improve the score, but gains are not guaranteed each epoch. Step activation is already available before purchase. |

## 代码依据与生效时机

路径均相对于工程根目录，SHA256 见 source-hashes.json（含补丁目标当前源文件）。

- XgCatalog.cs:380、616：step 是阶段 1、前置 perceptron 的 Research 节点，不是 Arch 架构；目录原价 30。StagePrice[1]=1，XgSim.Progression.cs:131–137 的 NodeCost 再乘 0.25 向上取整，当前实际购买价为 ¥8；由现有价格 UI 展示，不在文案里硬编码。
- XgSim.cs:188、198–204、252–280：仅看见节点不算拥有；前置、阶段、经费满足后一次购买，写入 S.unlocked，后续状态为 Owned，不可重复购买叠加。不需要装备。购买调用 AutoConfigureAll 和 Evaluate，并非训练轮次。
- XgSim.Progression.cs:255–265：ProgressionAccuracy 中 Has("step") 加 .01，与 bias 的 .02、sigmoid 的 .01 先相加，随后受结构上限、长句衰减、翻译系数与数据集 floorError 截断。是准确率量纲中的一个百分点，不是当前准确率乘 1.01，也不是评分固定加 1 分。
- XgSim.AutoModel.cs:356–367：CeilingAcc 使用上述 ProgressionAccuracy，随后还处理错标/近亲繁殖惩罚、学习率重启消耗品及上下界。因此不能承诺最终显示上限总是恰好 +1 个百分点。加成不检查当前激活函数，自动选用别的激活函数后仍保留此所有权加成。
- XgSim.cs:517–527：Evaluate 更新 run.ceiling，不把该 .01 直接加到 run.valAcc。一般保持同一模型时，购买后现有成绩不立即增加。购买可能触发自动配置，不能扩大成“购买后所有状态绝对不变”。
- XgSim.cs:650–678 与 XgSim.AutoModel.cs:491–530：实际训练同时运行棋盘训练与 StepAccuracy；成绩按上限差距逐步靠近，且存在退步分支，每轮评估。不能因 UseBoard=true 就把 +.01 认定成废弃旧公式，也不能保证训练一次就拿满加成。
- XgSim.Board.cs:78–81：ActivationOwned(0) 无条件为 true；BestActivation 不检查 Has("step")，故当前“增加可用激活函数”不成立。AutoTechniques（XgSim.AutoModel.cs:199–213）自动选最佳已拥有激活配置，不提供 step 道具装备增益。
- XgBoard.cs:150–158、550–560：实际使用阶跃时 G=0，较低层的误差更新因子为零；顶层指数为零，不能声称“整个网络完全无法训练”。这是既有激活规则，不是购买新增效果。现有“开关”解释是通俗原理，不声称游戏逐神经元执行完整的真实 AI 阶跃数学模型。

## 原文案问题及补丁边界

XgTechTreeCopy.cs:169 原 step/sigmoid 共享的“增加可用激活函数”错误描述 step 的购买收益，同时漏掉当前仍在使用的 .01 上限加成。补丁只拆出 step 并替换其效果文字，正式名称（:25）和解释（:78）不变。不修改 Catalog 内的旧描述或任何其他显示页面，避免越出本任务范围。

XgTechTreeCopy.Name/Note 入口仍保留 NodeMystery/IsAtlas 回退；XgTechTree.Visible 继续委托 NodeVisible；XgTechTreePage:758–766 的神秘节点提前返回不变。本任务文案不列出未来科技名字，避免提前揭示其他节点。

## 去重、静态检查与待验证

- 已检查现有进度/队列的任务记录及 Parallel100 专属交付情况；开始核对时不存在 tech-008 既有交付，也未发现已提交的同项修正。没有阅读新的策划正文作为实施依据。
- 静态检查：上述真实生产调用链已经逐段核对；只生成局部文案补丁。补丁适配及结束前哈希复核记录见文末。
- 实际运行：未运行 Unity、游戏、C# 编译或模拟，不标记运行 PASS。未读取或更改玩家存档。
- 视觉验收：未执行；由整合负责人验证中英文悬浮说明的换行、长文案可读性，以及未发现节点仍隐藏。
- 待整合：整合负责人核对 source-hashes.json 与目标上下文后采用 changes.patch；若目标有并行修改，应只合入 step 分支。
- 建议统一验证边界：阶段/前置未满足时不可购买；满足条件的一次购买；重复购买不叠加；同模型买前买后 valAcc 不直接 +.01；固定其他因素且未触及限制时 CeilingAcc 增 .01；感知机逻辑上限 .55/数据集上限等边界可能吞掉增量；与其它同类项相加；已自动使用其他激活函数时 step 加成仍在；训练退步分支不承诺提分。

本交付仅覆盖 step，未宣称整份文档或全局原则完成。

补丁适配检查：局部补丁只读适配检查成功（git apply --check，容忍源文件 CRLF 空白差异），未应用到生产源码。

结束前复核：source-hashes.json 所列 9 个依赖的 SHA256 均与交付快照一致。
