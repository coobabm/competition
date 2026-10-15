# tech-030 — bias（偏置）词条核对

结论：名称与通俗解释正确，保留；效果说明有真实概念板代码依据，但漏掉了数值准确率上限计算中的 0.02 加成，以及购买后自动启用的时机。交付仅修改 `XgTechTreeCopy.Effect` 中 bias 一行的 `changes.patch`，未应用到生产源码，不改模拟数值。

## 可直接采用的三行文案

中文：

1. 正式名称：偏置
2. 通俗解释：让判断的门槛可以移动，不必总从同一个起点划线。
3. 真实效果：购买后自动启用，无需装备：后续训练可学习判断门槛。两条训练线的准确率上限计算加入 2 个百分点，与同类加成相加，再受模型、数据和上限限制；不是购买即提分，也不会每轮再加 2 个百分点。成绩变化需训练后评估。

English:

1. Name: Bias
2. Explanation: Move the decision threshold instead of always drawing the boundary from the same starting point.
3. Effect: Automatically enabled on purchase; no equipment step is needed. Further training can learn a decision threshold. Add 2 percentage points in both tracks' accuracy-ceiling calculation, summed with similar bonuses before model, data and ceiling limits apply. This does not instantly raise the score or add another 2 points each epoch. Train and evaluate to see the result.

## 生效、单位与叠加合同

- 节点是研究项 `bias`，不是 `XgArch.bias`。后者是不同架构的容量误差系数，不能当成本节点效果。
- 第一阶段可见，前置是已拥有 `perceptron`。满足可见性、前置和经费后购买；只是看到节点或满足前置不会启用。目录基础价格 20，阶段倍率 1，当前 `NodeCost` 再乘 0.25 向上取整，所以实际购买价为游戏币 ¥5。价格仍由现有 UI 动态呈现，不硬编码进词条。
- 成功购买时将 id 加入 `S.unlocked`，立即供两个训练线读取。无需装备、切换架构或额外开关；已拥有状态阻止重复购买。本节点没有多级堆叠。
- 数值路径：购买调用 `Evaluate` 刷新 `run.ceiling`，并不直接给 `run.valAcc` 加 0.02。后续 `StepAccuracy` 才按与上限的差距推进，也可能退步。若购买同时触发自动配置，模型重配也可能影响成绩，不应归因为固定 +2。
- 0.02 是准确率的 **2 个百分点**，不是乘 1.02，不是 1000 分制固定 +2 分。它与已拥有 step 的 0.01、sigmoid 的 0.01先相加（三者合计 0.04），随后仍经过结构封顶、长句衰减、翻译系数、数据误差下限和其他当前效果。每次重算同一个上限，不逐轮累计加成。
- 概念板路径：`Knobs` 直接读取 `Has("bias")`；启用后，每张卡的输入包含激活值为 1 的常量偏置元素。后续训练在容量允许时创建/匹配偏置概念并调整其权重，参与判断总分；不是立即给所有答案额外加固定分。偏置概念不参与两两合并候选。是否答对仍由题目、已有权重及其他规则决定。
- 已学偏置是各区域的概念权重，常量激活值 1 不代表准确率 100%、额外样本或额外参数倍率。

## 原文件证据

以下路径相对 `Assets/LingGuangV05/XingGuang/`；完整 SHA256 记录在 `source-hashes.json`。

| 文件与行号 | 核对依据 |
| --- | --- |
| `Core/XgCatalog.cs:348–381,613` | 第一阶段研究节点、perceptron 前置、基础价格；名称为偏置 / Bias。旧目录说明为“准确率 +2%”，科技树覆盖文案应避免沿用该模糊单位。 |
| `Core/XgSim.Progression.cs:131–148` | 当前实际价格及阶段可见性。 |
| `Core/XgSim.cs:188,194–205,252–279` | Has、购买门槛、不可重复购买、写入已拥有、自动配置与刷新派生数值。 |
| `Core/XgSim.Progression.cs:255–265` | bias +0.02、同类相加、结构/长句/翻译/数据限制。 |
| `Core/XgSim.AutoModel.cs:356–368,492–526` | 上限公式调用上述修正；每轮向上限推进或按风险退步。 |
| `Core/XgSim.cs:517–541,639–675,685–691` | 当前 Evaluate 刷新上限；LegacyEvaluate 是旧曲线入口；训练同时驱动概念板与数值路径；分数另有映射。 |
| `Core/XgSim.Board.cs:94–116,211–232` | bias 自动开关来自所有权；训练卡应用当前 Knobs。 |
| `Core/XgBoard.cs:263–267,449–470,535–571` | 恒定偏置元素、权重投票、容量允许时建概念与学习；排除偏置的组合候选。 |
| `Core/XgTechTreeCopy.cs:13–16,45–52,77,168` | 名称默认回落到目录；解释有效；当前效果遗漏数值路径；神秘节点在文案分发前回落。 |
| `Core/XgTechTree.cs:146`；`Desktop/XgTechTreePage.cs:758–772` | 科技树名称与说明已接入 Copy，神秘提示分支先执行。 |

任务开始时 tech-030 目录无已有交付。已查看的相邻任务记录只确认它们处理其他节点，没有发现本节点的重复交付；本次不修改其他记录。

## 补丁与整合

- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`
- 基线 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`
- 补丁有前后各三行上下文，仅替换 `Effect` 的 bias 分支。名称、解释、神秘节点保护和其他词条均保留。
- 无新增接口依赖。整合者确认当前源码未改变该分支后应用；并行文案可能改变全文件哈希，不能未经复核直接覆盖整份文件。

## 检查边界与待验证

静态检查：已沿购买、上限、每轮训练、概念板与 UI 调用链核对；`git apply --check` 成功，仅验证补丁可应用，未实际应用。

静态边界推导（并非游戏运行结果）：

- 无封顶/衰减时，上限计算中 0.70 加偏置得到 0.72；不是 0.714。
- 感知机处理 logic 且没有人工特征时，0.54 和加成后 0.56 分别受 0.55 封顶，最终差仅 0.01；原本已达封顶时差可为 0。
- 普通 RNN 的长句场景，同一初值且未触碰其他限幅时，此项差值变为 `0.02 × 0.9^10`，不是完整 0.02。
- 未购买、仅满足前置时不会插入偏置元素；购买后未继续训练时，不保证已学得偏置权重。
- 重复刷新上限不会逐次累加 0.02；不能宣传“每轮固定上涨 2 个百分点”。

实际运行：未运行 C#、Unity 或游戏，未触碰玩家存档。视觉验收：未执行。待主聊天统一编译，并检查中英文 tooltip 长文换行、前置未满足/可购买/已拥有状态及神秘节点占位不泄漏。实际场景再核对购买后上限刷新、成绩非固定即时增加、后续训练产生的偏置行为。

本结论仅覆盖 bias 词条，不代表整份策划或全局原则完成。

结束前复核：source-hashes.json 中九个依赖文件的 SHA256 均未变化；再次执行补丁可应用检查成功。
