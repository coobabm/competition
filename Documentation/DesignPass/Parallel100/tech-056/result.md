# tech-056：Transformer 文案核对

日期：2026-10-13（Asia/Singapore）。范围仅 `transformer`，生产代码只读；未读取新的策划正文。先核对了 `Documentation/DesignPass/Agents/skilltree-task-001.md`，其通用架构说明和折扣显示已实现，本交付不重复实现它们。

## 结论与交付

名称 `Transformer` 正确；目录解释「只用注意力，整句一起算。」及英文 `Attention only. The whole sentence at once.` 可保留。当前 `XgTechTreeCopy.Effect` 对所有架构使用同一句通用说明，未交代 Transformer 的轮时长、序列上限加法项、生效时机和免费获得路径，不能完整说明本节点真实变化。

`changes.patch` 仅在 `XgTechTreeCopy.Effect` 的通用 Arch 分支前添加 transformer 专属分支，保留名称、解释、现有规模附加行和页面的能力门槛折扣行，不改模拟数值或其他词条。补丁已做静态上下文适用检查，未应用至生产。

- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`
- 基线 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`
- 上下文：原第 146 行 `if (node.kind == XgNodeKind.Arch) return T(sim, ...);`；插在该行前，仍处于 `Effect` 方法。
- 所有核对依赖的 SHA256 在 `source-hashes.json`。整合时如哈希不同，应重核专属分支及其依赖，不能整文件覆盖。

## 可直接采用的中文文案

**正式名称：** Transformer

**解释：** 只用注意力，整句一起算。

**效果：** 购买后加入两条训练线的候选结构，无需手动装备；系统在轮次之间按数据、模型上限和显存选择，未必立即采用。拥有后，下方能力门槛折扣和预训练规模倍数立即计入，并附送多头注意力、层归一化和残差连接标记。实际采用时：每轮基础时长从 0.6 秒变为 0.18 秒；资源争用或阻塞仍可能延长实际耗时，自动训练尝试间隔不变。序列数据的准确率上限公式加 6 个百分点（与通用加法项相加，受数据集上限限制），不再受非注意力结构的翻译惩罚；成绩需训练后评估，并非立即加分。自动换结构通常重练，已有迁移学习则保留高于随机水平的 60% 数值进度。不购买也可在达标并确认进入第六阶段时免费获得。

现有附加行继续显示：预训练有效规模 ×10（与其他道具倍数相乘）。页面现有能力行显示第六项能力的参数门槛 ×0.6、数据门槛 ×0.6；不要再重复添加一组折扣。

## Ready-to-use English copy

**Name:** Transformer

**Explanation:** Attention only. The whole sentence at once.

**Effect:** Buying adds a candidate architecture for both training tracks, with no manual equip step. Between epochs, the system chooses using data, model caps and VRAM; it may not select Transformer immediately. Ownership immediately applies the ability-threshold discounts and pre-training scale multiplier shown below, and grants the multi-head attention, layer normalization and residual-connection markers. When selected, base epoch duration changes from 0.6 s to 0.18 s; resource contention or blockers can still extend elapsed time, and automatic training attempt intervals are unchanged. Sequence datasets gain 6 percentage points in the accuracy-ceiling formula (added to the common additive bonuses and capped by the dataset); non-attention translation penalties no longer apply. Train and evaluate to improve scores; this is not an instant score bonus. Automatic architecture changes normally restart learning; transfer learning retains 60% of numerical progress above chance. It is also granted free when you meet the requirements and confirm entry into stage 6.

Keep the existing appended line: Effective pre-training scale ×10 (multiplies with other items). Keep the page's existing sixth-ability discount row: parameters ×0.6, data ×0.6.

## 原文件证据与边界

下列路径除特别说明均相对于 `Assets/LingGuangV05/XingGuang/Core/`。行号对应本次哈希基线。

| 项目 | 代码依据 | 准确含义 |
| --- | --- | --- |
| 节点和名称 | `XgCatalog.cs:145–146,300–303,437` | 架构节点 target 为 transformer；shared=true，两条线均可使用；阶段5、前置 attention。正式名无需另造译名。 |
| 价格与购买 | `XgCatalog.cs:348,356,437`；`XgSim.Progression.cs:131–160`；`XgSim.cs:193–208,252–280` | 基础价格30000，阶段5价格系数1，再乘 StoryResearchPrice=.25，现价¥7500；须可见、已拥有前置、出现阶段5想法或已到阶段6、经费足够。发现/可见不等于拥有。界面已有价格和条件，文案不硬编码售价。 |
| 自动采用 | `XgSim.AutoModel.cs:166–189,219–247`；`XgSim.cs:275–279` | 购买调用 AutoConfigureAll；活动轮跳过配置。可用架构按等级、参数、上限等比较，并考虑显存及已买尺寸上限。无需手动装备，不能承诺两线必换。 |
| 换结构进度 | `XgSim.AutoModel.cs:251–280` | 自动更换时 KeepProgress(0)，有 transfer 则 .6，保留的是超过随机基线的进度；旧配置迁移有不丢进度的特殊分支。因此写“通常”，不将全部路径一概描述成清零。 |
| 轮时长 | `XgSim.cs:24`；`XgSim.EpochAction.cs:10–43`；`XgSim.Wiring.cs:297–301` | 基础时长=.6×.3=.18秒，减少70%；BeginEpoch 将时长写入该轮。实际推进还乘训练资源因子、受 Blocker 限制。与自动尝试间隔、每轮训练收益、处理卡数不是同一量，不承诺总吞吐×3.33。 |
| 序列上限 | `XgSim.Progression.cs:255–265`；`XgSim.AutoModel.cs:334–368,492` | transformer且数据track=Sequence时acc加.06，再受数据集floorError裁切；现行 CeilingAcc 使用该方法，StepAccuracy 逐轮接近上限。是6个百分点，不是6%乘法或直接+60评分；评分按各数据集范围换算。通用bias/step/sigmoid加法项可同算；实际更换的总差异还受参数、结构与噪声等影响。 |
| 翻译例外 | `XgSim.Progression.cs:242,263` | transformer与attention均免除translate准确率×.75项；超过20长度且为后半段的translation卡不再×.5。此为免除指定惩罚，不保证翻译全对；从attention换来没有额外再解除一次。 |
| 门槛 | `XgSim.Abilities.cs:48–50,68,137–153,169–170,204–223` | 拥有即令第六能力参数和有效样本门槛各×.6：320K→192K参数，600000→360000有效样本。K为千参数；两门槛分别缩减，不能说只需36%参数或36%数据。Discount按适用条目相乘；当前第六能力只有本项。达标仍须该阶段已训练过并由玩家确认推进。 |
| 训练后的参数计入 | `XgSim.Abilities.cs:42,184–188` | TrainedGrade=1且shapeRounds>0才记录有效训练参数；购买/采用更大模型本身不等于参数进度马上达标。 |
| 免费路径及附属项 | `XgSim.Stages.cs:58–79`；`XgSim.Progression.cs:199–206,587–596` | 从阶段5确认进入6触发CompleteTransformer，免费grant Transformer、多头、位置编码、层归一化、残差、预热，并设置两线arch再Evaluate。普通购买仅随附multihead/layernorm/residual；不能将免费路径的position/warmup也承诺为购买赠品。后续自动配置仍可能重新择优。 |
| 规模叠加 | `XgSim.Finale.cs:75–111`；`XgTechTreeCopy.cs:58–60` | Has(transformer)令规模乘积×10，无需装备或先训练；与其他已拥有项目相乘，只计一次。ScaleBars以未折扣的第六能力基础门槛归一化、每条封顶1.5。免费获得的warmup另有×1.5；残差标记residual不是规模×2来源，×2检查的是resnet。规模不是算力、准确率或即时预训练完成倍数。 |
| 结构实现 | `XgSim.Board.cs:71,83,104,112`；`XgBoard.cs:294` | 采用时AnyToAny、多头分支及skip实际生效；拥有Transformer也使SkipOwned为真，自动技术配置可启用skip。层归一化标记未见独立数值消费，不把真实理论中的LayerNorm收益当成已实现奖励。 |
| 模型参数 | `XgCatalog.cs:145`；`XgSim.cs:351–368`；`XgSim.AutoModel.cs:334–350` | 参数量=深度×宽度²×12，ParamsK再÷1000；bias=.2用于上限公式误差项；span/maxDepth=999为配置值，仍受尺寸上限、数据和显存限制。不能承诺“参数直接×12”“无限层”或准确率恒定值。 |
| 神秘性 | `XgTechTreeCopy.cs:15,51`；`XgSim.cs:246–250`；`XgSim.Progression.cs:140–149`；`Desktop/XgTechTreePage.cs:428,758–764` | 当前NodeMystery仅用于未发现label.auto，Transformer按阶段可见并另设想法购买门槛。补丁不改这些规则，不提前展示未来节点，不添加未发现附属节点的额外效果说明。 |

`GpuUtilization`虽对Transformer返回1，但全Core搜索未找到调用消费者；不据此承诺显卡利用率变成100%。不将目录的理论说明扩写成尚未实现的真实AI能力。

## 已做检查与待验证

已做：只读静态调用链核对；`git apply --check` 对本补丁返回0（仅检查补丁上下文）；结束前依赖SHA256复核，结果见 `verification.md`。未运行编译、模拟测试、Unity或游戏，未做截图和视觉验收；不把其他任务记录中的运行结果算作本任务验证。

待整合负责人处理：

1. 核对当前哈希与本补丁，采纳专属分支；统一编译。无需改Catalog、XgSim或存档。
2. 用隔离内存状态核对未到阶段5不可见；已可见但尚未出现想法不可买；出现想法但无attention仍不可买；满足条件恰好¥7500可买；已有不重复收费。
3. 核对购买后但未被选中时，门槛和规模立即生效，而当前非Transformer轮时长不变；正在进行的轮保持已存duration，后续新轮采用后为.18秒；拥塞/阻塞、自动尝试间隔仍按原规则。
4. 固定结构/数据输入比较序列和视觉的ProgressionAccuracy加法项，验证仅序列+.06、接近上限会裁切、翻译长度20与21且secondHalf真/假的边界；再区分自动上限与当前已训练成绩，不能仅比较不同架构的最终分数便断言固定+6个百分点。
5. 核对门槛191999/192000参数、359999/360000有效样本的边界、stageEpochs=0不可推进以及确认按钮；免费路线无需购买，且购买赠品与第六阶段赠品不同。预训练规模×10、重复拥有不叠加，residual标记不误当resnet×2。
6. 中英文实际科技页长文排版/滚动、折扣行重复与裁切、未发现内容的视觉检查由主聊天完成。本交付不标记整项原则或整份策划完成。
