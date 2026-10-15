# tech-051：cot（思维链）核查结果

日期：2026-10-13（Asia/Singapore）  
范围：仅 cot 的科技页名称、解释与真实效果；生产源码只读。

## 结论

**当前 XgTechTreeCopy 的名称、通俗解释和效果准确，无需改动，不提交 changes.patch。** 名称回退为「思维链 / Chain of thought」；解释是该术语的通俗定义；效果单独写为训练处理量 ×1.05，并注明卡数受算力、取整和上限影响、成绩需训练后评估。它没有将数学/逻辑答题提升或可见推理过程列作游戏效果。

已有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md` 已核实并修正支线研究的效果范围，本任务不重复劳动。旧 XgCatalog.effect 中“预训练 +5%”仍不符合当前预训练进度公式，但科技页经 XgTechTreeCopy.Note 覆盖后不使用该旧句；不在本任务修改共享目录或其他页面。

下面是可直接采用的补充文案，用于说明时机和叠加规则，不是必须修复的错误。没有修改 Assets、模拟数值、全局清单或其他任务目录。

## 可直接采用的中英文文案

仅在现有页面判定可见/已发现后展示。

**中文**

- 正式名称：思维链
- 通俗解释：思维链的意思是把解题过程分成几步，先写中间推理，再给答案。
- 效果：购买后自动生效，无需装备。视觉和序列训练每轮卡数的计算量 ×1.05（增加 5%），与其他训练倍率相乘；在购买后的训练结算时体现，手动、自动训练均适用。实际卡数仍受算力、取整和每轮 4–320 张限制，可能暂时不变；成绩需训练后评估。此项不直接加快预训练进度条，也不解锁可见的逐步推理输出。

**English**

- Name: Chain of thought
- Plain explanation: Chain of thought means breaking a problem into steps, writing intermediate reasoning before giving the answer.
- Effect: Applies automatically after purchase; no equipment step is needed. Multiplies the card-count calculation for each vision or sequence training epoch by 1.05 (+5%), stacking multiplicatively with other training factors. Applies when training settles after purchase, for both manual and automatic epochs. Actual card counts still depend on compute, rounding and the 4–320 cards-per-epoch limits, so they may remain unchanged. Train and evaluate to see any score change. This item does not directly speed up the pre-training progress bar or unlock visible step-by-step reasoning.

离线补算的附注（若界面展示离线效果，必须带上）：离线模拟每轮固定 12 张，本项不会增加离线每轮卡数。  
Offline note: Offline simulation uses a fixed 12 cards per epoch; this item does not increase that count.

## 生效条件、单位和叠加方式

1. cot 是 Research 节点、Feel 类研究，不是可切换的网络架构。位于阶段 6，前置为 sft。达到显示或购买条件不等于已获得效果；成功购买写入 unlocked 后 Has("cot") 才成立。目录基础价 40000，经 StoryResearchPrice=0.25 后当前实际价格为 ¥10000。价格由现有页面动态展示，不建议写死进效果。
2. 一次性拥有，不需要装备、选中或另行训练才能启用倍率；Has 是布尔判断，同一项没有重复层数。再次购买被 Owned 状态拦截。
3. 下一次训练结算按当时已拥有项计算 CardsPerEpoch。BeginEpoch 不预存卡数，TickEpochs 完成后才调用 TrainEpoch，因此若训练进行中成功购买，当前未结算轮次也能使用倍率；已经结算的轮次不追补。
4. ×1.05 是卡数公式中、取整与截断之前的无量纲乘数。可见计量为「张训练卡/轮」，不是准确率增加 5 个百分点、固定多 5 张、每秒多 5 张或轮次耗时减少 5%。
5. FeelSpeed 各已拥有项连乘，并与 SpeedResearch、优化器、结构等公式因子相乘。只比较前置 sft 与 cot，两项因子乘积为 1.05×1.05=1.1025，不是 1.10；其他条件固定时，新购 cot 仍只增加一个 ×1.05 因子。这里引用 sft 仅作内部叠加证据，不扩展其文案。
6. 在 StepsPerSecond 查询公式中同样乘入 SpeedResearch，但当前 XingGuang 源码检索未见该方法调用；不能据此承诺页面显示每秒步数加成。预训练 TickFinale 使用 compute×dt/PretrainWork，ScaleItems 也没有 cot，故没有直接预训练速率或有效规模倍数。
7. 购买路径本身会 AutoConfigureAll 并 Evaluate；不能承诺购买瞬间分数绝不变化。cot 专属分支只提供处理量倍率，不能保证因此直接提分或显示推理过程。

## 原文件证据

以下路径以 `Assets/LingGuangV05/XingGuang/` 为前缀，行号为本次快照。

| 文件位置 | 核对依据 |
| --- | --- |
| Core/XgCatalog.cs:306–309、451、612 | ResearchNode 映射；cot 前置/阶段/价格；Feel 分类和中英文名称。 |
| Core/XgTechTreeCopy.cs:15、45、51–59、106、125–126、167 | 名称回退与神秘守卫；术语解释；Speed(1.05) 及取整/上限说明；cot 不在 ScaleItems，未附加预训练规模说明。 |
| Core/XgSim.Collection.cs:70–81 | Has("cot") 时 s *= 1.05，各 Feel 项连乘。 |
| Core/XgSim.cs:188、194–205、252–279、347 | 所有权、购买拦截、写入 unlocked、购买后配置/评估；FeelSpeed 合入 SpeedResearch。 |
| Core/XgSim.cs:554–557、639–675 | 查询公式；训练结算计算卡数、增加 steps、训练并评估。 |
| Core/XgSim.Board.cs:42–43、191–199 | 离线 12 张早返回；在线公式、四舍六入五成双的 Math.Round、下限 4 和上限 320。 |
| Core/XgSim.EpochAction.cs:10、13–25、29–50 | 轮次耗时不读 cot；开始时不锁定卡数，完成时调用 TrainEpoch。 |
| Core/XgSim.Finale.cs:75–84、196–198 | cot 不在规模因子表；预训练进度公式不读 FeelSpeed。 |
| Core/XgSim.Progression.cs:131–148 | 实际价格倍率；未拥有 cot 在阶段 6 之前不显示。 |
| Core/XgTechTree.cs:93–109、127–137、144–146 | 单项 0/1 所有权、统一购买、可见性以及名称接入。 |
| Desktop/XgTechTreePage.cs:426–430、758–772 | 隐藏不可见节点并清除钉住；神秘提示早退；正常节点使用新文案。 |

cot 自身没有额外“？？？”状态；NodeMystery 当前仅特判 label.auto。其神秘性依赖阶段隐藏，不应扩大称为 cot 专属占位功能。无需改变任一守卫、提前展示名称/数值或公开后续节点。

## 验证状态及待验证边界

- 静态核查：已沿目录、购买、卡数计算、轮次结算、预训练、页面显示读取与交叉比对。
- 实际运行：未运行 Unity、游戏或 C# 测试；没有读取真实玩家存档。已有任务的编译记录不算本任务运行结果。
- 视觉验收：未执行；中英文补充文案较长，需整合负责人检查最小窗口下的提示尺寸与换行。
- 无生产补丁待应用。补充文案由整合负责人按页面篇幅决定是否采用，不能将本结论标为整份策划完成。

建议主聊天用隔离内存状态验证下列真实边界（均为待运行，以下仅列静态预期）：

| 场景 | 静态预期 |
| --- | --- |
| 阶段 5 未拥有；阶段 6 缺前置；阶段 6 已有前置但钱不足 | 分别隐藏、不可购、不可购；均无 cot 倍率。 |
| 成功购买、重复购买、未装备 | 成功后自动获得一次倍率；重复购买不叠加；无需装备。 |
| 两条训练线，手动/自动；保留其他状态只改变 cot | 未取整公式都增加 ×1.05；别让算力/连击等变化混淆比较。 |
| 公式未乘 cot 前为 4；为 10；为 20；已到上限 320 | 对照输出分别可为 4→4、10→10（10.5 取偶）、20→21、320→320，不保证每轮数字增长。 |
| 轮次中购买、结算后购买 | 尚未结算轮次读取新倍率；已结算轮次不追补。 |
| OfflineSimulation=true | 每轮仍固定 12 张。 |
| 固定算力和规模的预训练进度 | cot 不直接改变进度增量或规模倍数。 |
| 已发现节点中英文提示；未发现节点 | 正确说明处理量，不承诺准确率/可见推理；保持原有阶段隐藏。 |

## 源码快照与结束复核

完整 SHA256 见同目录 `source-hashes.json`。XgTechTreeCopy.cs 本次核查原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。整合时如源码变化，应重新核对本节点分支，不能整文件覆盖。

结束前重新计算上述 10 个生产依赖文件的 SHA256，均与本次快照一致；未发现核查期间依赖变化。此项是文件一致性检查，不代表运行或视觉验收。

阅读边界记录：早期对 DesignPass 的 cot 关键词检索误返回逐条清单中的一个命中行；未继续打开该清单或新策划正文，该行不作为本结论依据。随后任务记录检索限定在 Agents/Parallel100。

