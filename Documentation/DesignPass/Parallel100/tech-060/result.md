# tech-060 · GoogLeNet 文案核查

日期：2026-10-13（Asia/Singapore）。范围仅为 `googlenet`，未读取新策划正文。

## 结论

当前 `XgTechTreeCopy` 的名称、通俗解释和通用架构效果没有确认的错误，无需修改，不生成 changes.patch。名称沿目录显示 GoogLeNet；解释为“大小窗口一起看，参数少”；效果明确“加入可用结构，系统按数据和显存选择合适的模型；训练后再看评估成绩”，没有承诺购买立即切换、提分或获得固定准确率。以下双语详文可直接采用，属于可选说明完善，不是缺陷补丁。

已核对既有 `Documentation/DesignPass/Agents/skilltree-task-001.md`：已完成通用架构效果及页面接入，不重复实现。检索已有 Parallel100 的 result.md 未发现 GoogLeNet 专项交付。未改生产源码、全局清单或其他目录。

## 可直接采用的中英文文案

### 中文

**正式名称：GoogLeNet**

**通俗解释：** 一种视觉卷积网络：像同时用大小不同的观察窗找线索，兼顾细节和整体。这里说明的是架构思路。

**效果：** 购买后加入视觉模型的可选结构，无需手动装备。系统会在两轮训练之间，根据当前数据、已解锁的层数与宽度以及可用显存自动选型；买到后不一定选中它。采用时，结构本身最多支持 22 层，实际层数仍受已解锁上限和显存限制。模型参数按“层数 × 每层宽度² × 1.2”计数；1.2 是参数计数系数，不是准确率或收益加成。训练处理量的架构倍率为 ×1，与其他训练倍率相乘；同时拥有多种架构不会把它们的倍率相加或相乘。成绩要经过训练和评估才能确认，购买不会直接加分；切换到新结构可能需要重新学习。

### English

**Name: GoogLeNet**

**Plain explanation:** A convolutional network for vision: like looking through large and small windows at once to find both details and broader patterns. This describes the architecture's idea.

**Effect:** Buying it adds an option for the vision model; no manual equipping is needed. Between training rounds, the system selects a model using the current data, unlocked depth and width, and available VRAM. Buying it does not guarantee selection. When selected, its base architecture limit is 22 layers; unlocked limits and VRAM still constrain the actual depth. Parameter count is calculated as layers × width² × 1.2. The 1.2 is a parameter-count coefficient, not an accuracy or income bonus. Its architecture multiplier for training throughput is ×1 and multiplies with other training factors; owning several architectures does not add or multiply their architecture factors together. Train and evaluate to see the score. Buying it does not directly add points, and switching architectures may require relearning.

这段玩家文案不列出尚未发现的其他科技名称或后续奖励。详细条件及组合规则如下，供整合时使用。

## 实际生效条件、单位及边界

- 解锁/购买：阶段 3 起显示，需已拥有前置 vgg，满足经费条件后一次性购买；当前实际价格为 `ceil(round(6000 × 1.4) × .25) = ¥2100`。`XgArch.cost = 200` 不是科技节点售价。此价格供审核，不建议重复硬编码到效果段，页面已有价格。
- 装备/时点：`BuyNode` 先记录拥有，再调用 `AutoConfigureAll`；非训练中的模型可当场重选，正在执行一轮训练的模型由 `AutoConfigure` 提前返回，待轮间重选。候选必须已拥有且匹配视觉训练线。算法依次比较预测评级、参数量、上限分数、较低 bias，并非始终采用最新购买项。
- 层数：基础结构上限 22 层。拥有 BatchNorm 时结构容许上限为 28 层（22 + 6），还要与已解锁 DepthCap 取较小值并满足显存；不是购买就送 22 层或 6 层。玩家未发现相关节点时，勿额外展示其名称与奖励。
- 参数量：`ParamsK = depth × width² × 1.2 / 1000`，返回单位 K，即千参数。这里的 width 为 `XgCatalog.Widths[run.width]`，不是数组索引，也不是概念板里除以 2 的 width。1.2 无量纲，仅采用当前架构的一个系数。与 VGG 同层数同宽度比较时，参数量为 `1.2/9 = 2/15`，约少 86.67%；自动选型可能改变形状，因此不能承诺购买后实际参数量或显存固定减少这一比例。
- 显存：当前模型训练需求（MB）为 `300 + ParamsK × .016 + max(1, depth) × width × .12`。即使同形状参数更少，固定和激活开销仍存在，不能把参数比例说成显存比例。
- 上限公式：架构 bias 为 .45，无量纲，进入参数误差项 `.45 × .25 × (CeilingScaleK / max(1e-6, neff))^.6`；不是准确率 45%、减少 45% 错误或 +45 分。数据误差、结构限制、其他已拥有修正及上下限共同决定结果，不保证优于当前模型。
- 训练处理量：架构 speed 为 1，无量纲，乘入 `CardsPerEpoch` 的各项乘积；结果仍有取整、最低 4 张及全局上限。离线模拟分支提前返回固定卡数，不走此倍率。未提供独立加速奖励。
- 训练与进度：购买会重算预测上限，但 `Evaluate` 本身不会把存储准确率直接推到上限。正常切换架构会重置高于随机基线的数值学习进度；已有迁移学习时保留这部分进度的 60%（不是保留 60% 绝对准确率）。旧版本存档首次自动接管另有保留进度例外。当前形状至少完成一轮训练，并在评估达到 C 级或以上，参数才有资格计入 P；P 取历史更大值，不把各架构参数相加。
- 真实算法边界：`WiringOf("googlenet")` 使用共享的 `LocalShared`；`Knobs` 没有 GoogLeNet 专属多尺度分支。不应把历史 Inception 的 1×1、3×3、5×5 分支、辅助分类器或自动选择尺度写成已实现的训练机制。现有短解释可作为架构原理保留。
- 独立奖励：`AbilityDiscounts` 与 `ScaleItems` 均无 googlenet 条目，不新增能力门槛折扣或预训练规模倍数。

## 源文件证据

以下路径以 `Assets/LingGuangV05/XingGuang/` 为根；SHA256 见同目录 `source-sha256.json`。

| 文件与位置 | 依据 |
| --- | --- |
| Core/XgCatalog.cs:153–154、300–303、348–362、408 | 名称/解释、视觉架构 .45 / 22 / 1.2 / 1、节点前置与阶段、价格倍率。 |
| Core/XgTechTreeCopy.cs:15、45、51–59、70、146 | 名称回退目录；解释取架构 note；效果使用通用架构文案；mystery/atlas 先回退。 |
| Core/XgSim.cs:193–205、252–278 | 购买检查、一次性拥有、自动重选与重新评估。 |
| Core/XgSim.cs:351–369、404–410 | 参数 K、显存 MB 和结构层数上限的公式。 |
| Core/XgSim.AutoModel.cs:127–145、165–184、218–281 | 自动形状、候选比较、训练中延后、换结构/进度保留。 |
| Core/XgSim.AutoModel.cs:334–367、492 起 | .45 所在的上限参数误差项；训练推进并可能退步。 |
| Core/XgSim.cs:517–528、649–668 | Evaluate 只刷新衍生量；训练后更新准确率并增加 shapeRounds。 |
| Core/XgSim.Board.cs:62–73、94–116、190–200 | LocalShared、共享 Knobs、卡数倍率与离线边界。 |
| Core/XgSim.Progression.cs:20、131–148、198–208 | 训练线匹配、实际价格、阶段可见性；无本节点专属授予。 |
| Core/XgSim.Abilities.cs:61–69、184–188；Core/XgSim.Finale.cs:75–84 | 没有本节点专属折扣/规模倍率；P 需训练及评级，取最大值。 |
| Core/XgTechTree.cs:127–130、144–147；Desktop/XgTechTreePage.cs:758–772 | 继续使用 NodeVisible、mystery 提前返回和统一文案入口。 |

## 验证与待整合

- 已完成：源码静态核对与已授权任务记录查重。未运行 Unity、游戏、编译或测试；没有视觉验收结果，不标记运行 PASS。
- 静态边界检查：未到阶段隐藏；未购前置不能购买；已拥有不能重复购买；训练中不重配；未被选中不获得架构系数；低层数上限不因 22 而突破；显存不足不保证可训练；完成训练未达到 C 不计入 P；离线固定卡数不走 speed。以上为代码分支核对，不是实际执行结果。
- 待整合：可以保留现有文案，也可选用上述双语详文；不需要应用生产补丁。若 UI 空间有限，保留现有简版，详文用作帮助内容。
- 待验证（整合负责人）：统一编译；用独立临时状态检查购买时与轮间选型；中英文提示排版、阶段隐藏及神秘占位的场景截图。不得以本文认定整份策划或全局原则完成。
- 结束前依赖复核见 `dependency-recheck.md`。
