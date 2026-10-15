# tech-014 · batchnorm（批量归一化）

## 结论与范围

仅审查 batchnorm。现有正式名称、通俗解释、训练处理量 ×1.2、有限架构可承受层数 +6，以及自动追加的预训练有效规模 ×1.5 均有代码依据。需要补齐购买后自动启用、训练回落风险与层间传递效果；只交付 `changes.patch`，未修改生产文件或模拟数值。

查阅已有 `Documentation/DesignPass/进度.md` 与 `队列.md` 的当前任务记录：科技树首项仍由整合聊天统一验收。本交付不勾选全局条目，不代表整份策划完成。交付前专属目录没有已有结果；没有启动游戏、Unity、编译器，也未接触玩家存档。

## 可直接采用的中文文案

**正式名称：** 批量归一化

**通俗解释：** 把一批数据的信号范围调齐，让层与层之间更容易配合。

**实际效果：** 购买后两条训练线自动启用，无需装备；仅达到解锁条件还不生效。训练处理量 ×1.2，与其他处理量倍率相乘；每轮卡数仍受算力、取整和上限影响。有限制的结构可承受层数 +6；仍须购买层数上限，并有足够显存。每轮成绩回落的各项风险合计先 ×0.8，再按 45% 封顶；不是直接减少 20 个百分点。大脑训练可承受的更新步幅 ×1.5，并减轻普通层之间的信号衰减和传递扰动；不能让阶跃激活传回误差。后续训练使用这些效果，成绩需训练后评估，不保证立即提分。

预训练有效规模 ×1.5（与其他道具倍数相乘）。

## Ready-to-use English copy

**Formal name:** Batch normalization

**Plain explanation:** Align signal ranges across a batch so successive layers work together more easily.

**Actual effect:** Automatically enabled for both training tracks after purchase; no equipment step is needed. Meeting unlock requirements alone grants no effect. Training throughput ×1.2, multiplying with other throughput bonuses; cards per epoch still depend on compute, rounding and caps. Architectures with a depth limit tolerate 6 more layers; purchased layer caps and available VRAM still apply. The combined risk of a score drop per epoch is multiplied by 0.8 before the 45% cap; this is not a reduction of 20 percentage points. Brain training tolerates update steps ×1.5 and reduces signal loss and distortion between ordinary layers; it cannot make a step activation pass errors backward. Subsequent training uses these effects. Train and evaluate to see the score; an immediate improvement is not guaranteed.

Effective pre-training scale ×1.5 (multiplies with other items).

最后一行由 `XgTechTreeCopy.Note` 的现有 ScaleItems 循环追加，补丁不要再重复写一遍。名称和解释沿用现有字符串，无需为了统一缩写另外修改。

## 原文件证据与单位

下列路径均相对工程 `Assets/LingGuangV05/XingGuang/`，行号对应交付时读取的源码；完整 SHA256 见 `source-hashes.json`。

| 源码 | 依据与适用边界 |
| --- | --- |
| Core/XgCatalog.cs:369–370、425、645–646 | `R("batchnorm", "dropout", 3000, 4)` 是第 4 阶段 research 节点，前置 dropout；它是研究技巧，不是需切换装备的架构。Research 表的 cost=90 不是实际节点购买价；本补丁不硬编码价格。 |
| Core/XgSim.Progression.cs:132–164；Core/XgSim.cs:194–205、252–276 | 阶段可见、前置已购、资金足够后才能买；购买将 id 加入 unlocked。`Has` 读取该集合；随后自动配置并评估。可购买≠拥有。 |
| Core/XgSim.AutoModel.cs:187–211、219–246；Core/XgSim.Board.cs:91–115 | 购买影响自动配置键；AutoTechniques 将 batchNormOff 设为 false。Knobs 仍需已拥有且未关闭。活动中的轮次会跳过自动重配，后续配置/训练才应用；不能承诺追溯改变已处理卡片。旧状态若手动关闭，该关闭字段只影响大脑 Knobs，不影响 Has 驱动的全局倍率。 |
| Core/XgSim.cs:347；Core/XgSim.Board.cs:190–199 | ×1.2 是无量纲处理量因子，乘其他研究/优化器/结构等因子；最终单位是卡/轮，取整并限制在 4–320。专用 OfflineSimulation 分支固定 12 卡/轮，不能推断该分支也增加 20%。自动选型还可能改变模型尺寸，所以整轮卡数不保证恰好变成原来的 1.2 倍。 |
| Core/XgSim.cs:404–409；Core/XgSim.AutoModel.cs:136–151 | +6 的单位是层，只加在有限架构承受值；无限哨兵 999 保持 999，未知架构返回 5。实际层数受已购 DepthCap 和显存约束，无免费增加六层保证。 |
| Core/XgSim.AutoModel.cs:70、454–468、492–500；Core/XgSim.cs:655–665 | 回落概率 `min(.45, sum * .8)`，其中 sum 是过拟合、噪声、漂移、跳涨和基础波动合计；比例先相加，BN 再相乘，最后封顶。每个后续训练轮以此计算是否回落；×0.8 不减少回落幅度、不保证连续不跌，也不是直接加准确率。 |
| Core/XgBoard.cs:130–139、550–560；Core/XgSim.cs:568–576 | 大脑撕裂阈值 TearAt ×1.5，与裁剪容忍 ×2、优化器 steadiness 相乘后仍受深度/循环条件影响。单位是学习步幅×误差的阈值；不是学习率固定提高 50%、不是准确率提高 50%。SafeLr 只从离散候选选择安全值。 |
| Core/XgBoard.cs:150–156、550–560 | 无跨层直连时每层误差传递因子 G 加 0.05，并封顶 1：Sigmoid .25→.30，ReLU .98→1，阶跃仍为 0。单位是比例的绝对增量（最多 5 个百分点），不是一律相对提高 5%。有有效直连时 G 已为 1。 |
| Core/XgBoard.cs:167–182、465 | 普通层原信号保留比例加 .02、封顶 1（2 个百分点）；扰动系数 .12→.095。经过 n 层保留量为 RelayKeep^n，扰动系数乘 sqrt(n)。有直连时保留为 1、扰动为 0，BN 不再叠加；没有上方层也不产生这项增益。RelayOn 默认开启。 |
| Core/XgSim.cs:346、554–581 | 兼容路径另有 Stability×1.5 和 StepsPerSecond 中的处理量×1.2；Stability 用在非 Board 的 Hazard/SafeLr 分支。不把它与 Board 的阈值 ×1.5 再相乘为 ×2.25。 |
| Core/XgSim.Finale.cs:75–111；Core/XgTechTreeCopy.cs:57–59 | 已拥有即贡献预训练道具倍率 ×1.5；与其他已拥有道具相乘一次。有效规模基于已训练参数、数据的归一化进度，并非直接增加原始参数/样本 50%；尚无训练进度时乘数也不能凭空产生规模。 |
| Core/XgTechTreeCopy.cs:15、31、51、97、181；Core/XgTechTree.cs:146；Desktop/XgTechTreePage.cs:749–772 | 名称/解释正确，旧 effect 只有处理量与层数，缺少风险和生效时点。名称与 Note 的 mystery/atlas 早退以及页面未发现分支均不改动；patch 不列出其他道具名称，不绕过发现状态。 |

## 精确补丁与整合约定

- 文件：`changes.patch`，只替换 `Core/XgTechTreeCopy.cs` 中 `Effect` 的 batchnorm case；以相邻 case 作上下文，不修改任何其他词条。
- 目标完整路径：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
- 读取版本 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- 若其他聊天改了同文件，先核对 batchnorm 的旧行和依赖语义，再只移植本 case。不要覆盖整份 XgTechTreeCopy。
- 保留 ScaleItems 的现有追加逻辑和所有未发现节点早退。本节点自身采用阶段可见性，没有专门的神秘占位状态；不新增泄露其他节点的明细。

## 验证状态与待验证边界

**已完成：静态核对。** 对照节点构造、购买链、自动配置、正常训练路径和效果公式；依赖结束前复核见 `verification.md`。补丁可应用性检查只检查文本，不代表 C# 编译或运行。

**未运行：** Unity/C# 编译、模拟执行、真实游戏交互与截图；不宣称运行 PASS。以下交给整合负责人使用隔离的合成状态验证，不接触真实玩家存档：

1. 仅符合阶段/前置而未购买无增益；购买后两条训练线自动启用。购买发生在活动轮次中时，确认后续配置生效，不追溯修改旧卡片。
2. 固定模型与算力，卡/轮未取整前 ×1.2；验证最小 4、上限 320、取整恰好不变和 OfflineSimulation=12 的边界。
3. 有限架构承受层数 +6，但 DepthCap/显存更小时不强行多六层；999 哨兵不变。
4. 固定风险分项合计 .20，BN 前后应 .20→.16；合计 .50 应 .45→.40；合计 .60 则两边均封顶 .45。这些是手工数学预期，未执行。
5. 在同样其他设置下检查 TearAt×1.5；Sigmoid G .25→.30、ReLU .98→1、Step 0→0、skip 时 G/RelayKeep 已满且 RelayNoise=0。
6. 检查有效规模乘数只 ×1.5 一次，零训练进度仍为零；中英文 tooltip 不重复显示该行。
7. 检查中英文长 tooltip 的换行、滚动/裁切和窄窗口可读性；神秘节点仍只显示占位，未发现图鉴名称与解释不泄露。视觉验收尚待主聊天截图。
