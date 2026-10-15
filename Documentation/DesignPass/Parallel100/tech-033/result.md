# tech-033 — features（特征工程）核对结果

结论：名称与通俗解释可保留；效果文字需要补足生效时机、逻辑题结构限制、卡数边界与离线例外。交付 changes.patch，仅修改 XgTechTreeCopy.Effect 的 features 分支；未修改生产源码、模拟数值、其他词条或全局清单。

范围：当前《技能树词条策划》3 行要求中的单一节点 features。它是 Research（练法）而不是 Arch（架构）节点。已有 Parallel100/result.md 的 features 命中只涉及教学卡数据，没有发现本节点文案的重复交付。本报告不代表整份策划完成。

## 可直接采用的中英文文案

**正式名称**：特征工程 / Feature engineering

**通俗解释**：人先把零散线索整理成组合，再交给网络判断。

**Plain explanation**: People combine raw clues before the network makes its decision.

**真实效果**：购买后解除感知机在逻辑题上的 55% 准确率结构限制；仍需训练，成绩不保证立即提高。系统在轮次之间为感知机自动启用，无需装备；其他架构不自动启用。启用后，逻辑题增加成对线索，图像去掉杂点并对齐左上角，文字去掉填充词并整理成单词和相邻词对。正常训练每轮卡数的计算值 ×0.5，与其他卡数倍率相乘后取整，最终为 4–320 张；离线结算固定每轮 12 张，不应用此倍率。只能购买一次，效果不重复叠加。

**Effect**: Buying removes the perceptron's 55% accuracy restriction on logic tasks; further training is needed, with no guaranteed immediate score gain. The system enables it for perceptrons between rounds, with no equipment step; other architectures do not enable it automatically. When enabled, logic gains paired clues, images lose stray dots and align to the top-left, and text drops filler words and becomes single-word and adjacent-pair clues. The calculated card count per normal training round is multiplied by 0.5, multiplicatively with other card-count factors, then rounded and limited to 4–320 cards; offline simulation uses a fixed 12 cards per round without this multiplier. A one-time purchase; the effect does not stack with itself.

## 代码依据与适用边界

以下路径均相对工程根目录，行号对应核对时源码；完整 SHA256 见 source-hashes.json。

- Core 文件前缀：Assets/LingGuangV05/XingGuang/Core/。
- XgCatalog.cs:306–309、383、620–622：ResearchNode 使用 ResearchItem 的正式名称；features 前置 bias，阶段 1，基础价格 ¥60。不是“特征组合”架构，不需要另加一个架构节点或改名。
- XgSim.cs:194–205、252–278：可见、前置满足且资金足够才能购买；付款后加入 unlocked。已拥有时状态为 Owned，阻止再购。因此“解锁”是这次购买，不存在第二次装备消费；只可见/满足前置尚不生效。
- XgSim.Progression.cs:140–148：features 遵守阶段可见性。XgTechTreeCopy.cs:15、53 的神秘节点/图鉴保护不变；XgTechTree.cs:129 和 XgTechTreePage.cs:758–762 的可见/神秘分支不变。未给未发现节点新增名称或描述。
- XgSim.cs:273–276；XgSim.AutoModel.cs:199–211、219–246：购买触发 AutoConfigureAll，再 Evaluate；AutoConfigure 遇 epochActive 跳过，轮次之间再配置。AutoTechniques 仅令已拥有 features 的 perceptron 启用，自动选到其他架构则关闭；所以购买并不保证所有模型使用该技巧。XgSim.cs:651 在训练前再次自动配置。
- XgSim.Board.cs:86–89、108：实际开关是 FeaturesOwned && run.features；运行标志本身不能绕过所有权。此底层条件不检查架构，但正常自动配置只对感知机启用，文案据此写“其他架构不自动启用”，而非声称底层禁止其他架构。
- XgSim.Progression.cs:255–268：perceptron + logic + 未拥有 features 时将准确率限制至 .55。这个判断依据所有权，独立于 run.features；购买立即解除这项结构限制，不是准确率 +55%、+45 个百分点或保证满分。
- XgSim.AutoModel.cs:356–368；XgSim.cs:517–528、655–668：CeilingAcc 使用该结构限制；Evaluate 重算 ceiling，不把解除限制直接加到 valAcc。训练通过 StepAccuracy 改变成绩，同时还受参数、数据、污染及数据集边界影响。卡数影响训练进展，不能等同于成绩增长必定减半。
- XgBoard.cs:263–265、330–369：启用时才做人工特征转换。逻辑保留原特征并增加无位置特征的两两组合；视觉去掉有位置的 StrayDot、以最小 x/y 平移到左上角（不是居中）；序列对主序列删除填充词，再产生单词和过滤后相邻词对，保留无位置提示与第二序列。未承诺长距离理解或真实 AI 理论中的额外能力。
- XgBoard.cs:373–378、228：人工生成的文本特征不再参与进一步组合；全连接时有位置的线索还受同一序列、横纵距离各不超过 2 的邻域限制。此为内部实现边界，无新增加成。通俗效果中的“文字”是概述，不应被扩写为翻译两侧全部去噪或无限词序能力。
- XgSim.Board.cs:42–43、191–199：正常手动/自动训练在未取整的卡数计算中乘 0.5，和算力、优化器、研究、连击/自动等因素相乘；Math.Round 默认中点取偶，再限制至 4–320 张。离线提前返回 12。最终显示卡数不保证严格等于关闭时的一半。
- XgTechTreeCopy.cs:83、170：原解释正确；原效果遗漏上述时间和边界，且只说明组合线索，没有提及当前实际使用的逻辑题结构限制。
- XgTechTree.cs:146、XgTechTreePage.cs:772：名称及详情已接入 Copy，无需添加第二套展示入口。

## 整合与验证

1. 只采用 changes.patch 的 features 分支。补丁头含目标路径与源 SHA256，并保留相邻上下文。其他并行任务也可能修改此文件；若整个文件哈希不同，先比较 features 分支及相关实现，不覆盖其他节点改动。
2. 静态核对：已沿目录定义、购买、自动配置、效果开关、卡数、准确率上限和展示入口核对。补丁适用性检查结果见文末。依赖结束复核见 source-recheck.md。
3. 实际运行：未启动 Unity/游戏，未编译 C#，未执行模拟测试，未接触真实玩家存档。不得将下列待验证项记为已运行 PASS。
4. 视觉验收：未执行；由整合聊天检查中英文长文换行、滚动及详情遮挡。此补丁比原文更长，不能仅靠静态检查断言显示完整。

待统一验证的真实边界（使用隔离临时状态）：

- 未拥有但 run.features=true，Knobs.features 仍为 false；只达到前置但未付款时无效果。购买成功后不可重复购买/叠加。
- 空闲感知机购买后自动启用；活跃轮次购买后该轮配置不被中途改写，轮次结束后的配置启用。自动选择其他架构时 features=false。
- 相同模型和数据条件下，购买前逻辑题上限受 .55 限制；购买后仅撤去该限制，仍受其他 ceiling 因素制约。保持结构不变时不能断言 valAcc 购买瞬间增加。
- 卡数未取整值 101：关闭 101，启用 50（50.5 中点取偶）；未取整值 5：关闭 5，启用最终 4；未取整值 1000：关闭和启用最终均 320。离线开启/关闭均 12。这些是边界预期，不是已运行结果。
- 两个无位置逻辑线索增加一条组合；含位置线索不应进入该两两组合。仅有杂点的图像不会生成可见笔画；带平移的有效图形去噪后对齐左上角。填充词夹在两个主序列词之间时，组合过滤后的相邻词；第二序列与无位置提示保持。
- 中英文名称、解释、效果均显示；未发现/不可见节点的原有保护不被文案补丁绕过。

静态补丁检查：git apply --check 返回退出码 0；未实际应用补丁。依赖结束复核：9 个依赖文件均与本次记录哈希一致。
