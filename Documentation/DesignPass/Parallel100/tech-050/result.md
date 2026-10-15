# tech-050：sft（监督微调）文案核对

结论：交付仅针对 `XgTechTreeCopy.cs` 中 sft 的两处文案修正，未改生产源码或模拟数值。现有通俗解释正确，予以保留；正式名称补为“监督微调（SFT）”，效果补充购买生效时点、两条训练线、乘法叠加、取整限制、离线例外及预训练边界。此节点是 Research / Feel，不是可装备架构。

## 可直接采用的中英文文案

中文：

- 正式名称：监督微调（SFT）
- 通俗解释：用指令和示范答案教模型按要求回答。
- 真实效果：购买后无需装备，两条训练线从下一次轮次结算起，训练卡数计算值 ×1.05（+5%）；与其他处理量倍率相乘，本节点只计一次。实际卡数仍受取整和上下限影响，离线模拟不享受此加成。成绩需训练后评估；不直接提高预训练进度或有效规模。

English:

- Name: Supervised fine-tuning (SFT)
- Plain explanation: Teach the model to follow requests using instructions and example answers.
- Effect: No equipment step is needed after purchase. From the next epoch completion, both training tracks multiply the calculated card count by 1.05 (+5%). This stacks multiplicatively with other throughput factors and applies once. Rounding and card limits still apply; offline simulation receives no bonus. Train and evaluate to see score changes. This does not directly increase pre-training progress or effective scale.

解释描述技术概念；本游戏没有通过 sft 购买动作执行真实指令微调或直接提高聊天、指令遵循、推理正确率的实现，不应把概念句当作能力承诺。

## 解锁、购买、生效与单位

| 项目 | 当前代码行为 |
| --- | --- |
| 可见与购买 | 未拥有时第 6 阶段可见；要求已拥有 Transformer 且资金足够。可见或满足前置本身不给加成。 |
| 价格 | 目录基价 ¥25,000；第 6 阶段系数为 1，现行 `NodeCost` 再乘 0.25 并向上取整，实际 ¥6,250。价格保留由 UI 动态显示，不写死进补丁。 |
| 生效时点 | 成功购买将 id 加入 `S.unlocked`，`Has("sft")` 即为真，无装备或开关要求。卡数在 `TrainEpoch` 结算时计算，包含购买前已开始而尚未结算的轮次。 |
| 作用范围 | 视觉与序列训练共用 `SpeedResearch`；手动与自动轮次均经过该公式。不是只作用于 Transformer，也不是轮次时长缩短 5%。 |
| 单位 | 1.05 是无量纲倍率，作用于取整前的张/轮计算值。不是准确率 +5 个百分点，也不保证每轮显示卡数恰好 +5%。 |
| 叠加 | 与其他已生效处理量因子相乘。两个 1.05 因子得到 1.1025，而不是 1.10。本节点按布尔拥有状态计一次，无法重复购买叠层。 |
| 卡数边界 | `Math.Round` 后限制在 4–320 张/轮；离线模拟提前返回固定 12 张/轮，不经过该倍率。 |
| 训练后结果 | 购买会调用评估，但 sft 没有直接成绩加值。实际学习结果须在训练和评估中观察，不能保证提分。 |
| 预训练 | `TickFinale` 的工作量为 `host.Compute * dt`，没有 sft；`ScaleItems` 也没有 sft。普通训练多处理卡片可能间接影响训练进度，不等于独立预训练速度或规模 ×1.05。 |

## 原文件证据（相对于工程根目录）

以下行号对应 `source-hashes.json` 所记录的源文件快照。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306`：`ResearchNode` 从研究目录取名称和说明，类型为 Research。`:451`：sft 的前置为 transformer、阶段 6、基价 25000。`:611`：类型 Feel，旧名称为“指令微调 / Instruction tuning”，旧效果“预训练 +5%”不符合独立预训练实现。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:13`：Name 没有 sft 分支，回落到旧目录名称。`:105` 的解释可保留。`:167` 复用 `Speed(sim, 1.05)`，倍率正确，但未明确生效时点和叠加方式。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Collection.cs:70–83`：`FeelSpeed` 逐项相乘，`:79` 为 `if (Has("sft")) s *= 1.05;`。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188`：Has 检查拥有列表；`:194–205`：重复购买、阶段、前置和资金检查；`:252–280`：购买入表并重新评估，无 sft 装备流程；`:347`：`SpeedResearch` 包含 FeelSpeed；`:554–557`：旧式步数/秒接口也包含该因子；`:655–671`：当前训练按卡数结算并评估。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:191–199`：离线提前返回、卡数公式、取整及上下限；`:42–43` 定义 320 和 12。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.EpochAction.cs:10–24`：开始轮次不预存训练卡数；`:40–49`：结束时调用 TrainEpoch。因此购买中途生效由结算时状态决定。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:130–149`：现行价格系数和阶段可见规则。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs:75–85`：ScaleItems 不含 sft；`:196–198`：独立预训练工作量不引用 FeelSpeed。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:127–130,146` 与 `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:428–430,758–772`：保留可见性过滤、神秘节点提前返回、名称与说明的现有接入。

未发现 sft 是额外神秘节点：未到第 6 阶段时由 NodeVisible 隐藏；不添加跨阶段展示或新的后续节点提示。补丁保持 Name/Note 的神秘节点及图鉴保护分支原样。

## 交付与整合

- `changes.patch`：只修改科技树展示层的 sft 名称及效果；共享的 subword、cot 效果保持原样，通俗解释保持原样。
- 目标文件：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`
- 目标当前 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`
- 补丁含上下文。其余依赖原始 SHA256 见 `source-hashes.json`。如整合时有变化，应以 sft 两个分支逐处核对合并，不能覆盖整个文件或其他任务的修改。
- 目录中的旧文字以及购买提示仍通过 `sim.NodeName` 取旧名称；本任务仅交付科技树展示修正，不扩展更改全局目录和其他页面。整合负责人可记录这一显示范围，不能据此宣称所有 UI 文案都已统一。

已有记录核对：仅搜索现有 `进度.md`、`队列.md` 与 Parallel100 交付记录中的 sft、监督微调和相关接入关键词，未发现已完成的 sft 专项交付。未读取新的策划正文或后续条目。

## 验证状态与待验证项

- 静态审查：已完成目录→购买状态→处理量公式→轮次结算→科技树显示的交叉核对。
- 补丁适用性：已实际运行 `git apply --check --ignore-space-change --ignore-whitespace`，退出码 0；仅检查，未应用生产文件。
- 实际游戏运行、Unity 编译、场景截图与视觉验收：均未执行，不标记 PASS，由整合负责人统一进行。
- 结束前依赖复核结果记录在 `dependency-recheck.md`。

待统一验证的真实边界：

1. 第 5 阶段未拥有时无 sft 节点；第 6 阶段有节点但缺 Transformer 时不能购买；购买成功后中英文名称、说明显示正确。
2. 在足够资源、相同模型和手动连击条件下，对比有无 sft 的两条训练线卡数；卡数接近 4 下限或 320 上限时允许无可见增量。
3. 两个 1.05 因子应按 1.1025 进入取整前计算；不要把显示卡数的舍入差当成未叠加。重复购买不应再扣款或增加倍率。
4. 一轮进行中购买 sft，在该轮完成结算时检查加成；轮次计时长度应保持不变。
5. 自动轮次仍受 AutoEpochFactor；离线模拟固定 12 张/轮；只改 sft 拥有状态时独立预训练进度速率与有效规模不应变化。
6. 检查长说明在中文/英文提示面板内是否可读、是否截断；未发现节点和其他节点的显示行为保持现状。

本交付仅完成 tech-050 的静态文案审查与待整合补丁，不代表整份《技能树词条策划》或全局原则已完成。
