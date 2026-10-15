# tech-029：learnrule（学习规则）

结论：需补足当前科技页文案；提交只涉及 `XgTechTreeCopy.cs` 两处 learnrule 分支的补丁，未修改生产源码。名称「感知机学习规则 / Perceptron learning rule」准确，保留目录回退，不新增名称覆盖。未阅读新策划正文；已有任务记录检索未发现本节点已有交付。

## 可直接采用的中英文文案

中文名称：感知机学习规则

中文解释：原理：判断错了，就调整各条线索的分量，让下次判断更接近正确答案。

中文效果：购买即生效，无需装备：两条训练线的手动和自动训练，每轮处理卡数在取整和限幅前 ×1.15（+15%），与其他训练处理量倍率相乘。常规训练每轮最终为 4–320 张卡，实际增量可能不足 15%；不缩短每轮时长，也不直接增加准确率，成绩需训练后评估。

English name: Perceptron learning rule

English explanation: The principle: after a wrong prediction, adjust the weight of each clue to move the next prediction toward the correct answer.

English effect: Active on purchase, with no equip step: on both tracks, manual and automatic training multiply cards per epoch by 1.15 (+15%) before rounding and limits; this multiplies with other training-throughput factors. Each normal epoch processes 4–320 cards, so the actual gain may be less than 15%. Epoch duration is unchanged, and accuracy is not directly increased; train and evaluate to see the score.

补丁中的 320 使用现有 `XgSim.MaxCardsPerEpoch` 常量插入，避免增加一处上限硬编码。文案中只泛称其他倍率，不列出未发现的后续科技名称。

## 确认的问题与改动范围

- 原解释没有区分「感知机学习规则原理」和「买这个节点才开始纠错学习」。实际 `XgBoard.Train` 在未拥有 learnrule 时也可更新权重，且使用连续误差，答对也可能更新。加上「原理」前缀，避免把现实算法的条件宣称为本节点新启用的机制。
- 原效果 `Speed(sim, 1.15)` 的倍率、取整与上限提醒正确，不需要改模拟数值。它遗漏本任务要求的购买生效时点、无需装备、适用两条训练线、乘法叠加及卡/轮单位，故改成本节点专属效果文案。公共 `Speed` 和其他词条保持原样。
- 名称沿 `XgCatalog.ResearchItem → ResearchNode → sim.NodeName` 返回，准确，无需改动。

## 原文件证据与生效合同

以下路径均相对工程根目录，行号对应 `source-hashes.json` 快照。

| 证据 | 对本节点的结论 |
| --- | --- |
| `Core/XgCatalog.cs:306–309, 369–380, 615`（Core 前缀完整为 `Assets/LingGuangV05/XingGuang/Core/`） | learnrule 是 Research 节点，stage=1，parent=weights；正式中英文名来自 Research 表。不是可切换网络架构，也不是装备。 |
| `Core/XgCatalog.cs:348–356`；`Core/XgSim.Progression.cs:131–148` | 阶段 1 价格系数为 1；目录基础价 40，经 `ceil(cost × .25)` 得当前购买价 ¥10。可见不等于拥有，不触发加成。价格已有 UI 显示，补丁不重复硬编码。 |
| `Core/XgSim.cs:188, 194–205, 252–279` | 阶段可见、拥有 weights、经费足够才能购买；扣款成功即加入 unlocked。`Has` 从 unlocked 判断，因此购买成功立即启用一次倍率；再买时为 Owned，不能重复堆叠。购买会评估已有模型，但没有为 learnrule 直接增加准确率或免费训练的专门分支。 |
| `Core/XgSim.Progression.cs:268–269` | `Has("learnrule") ? 1.15 : 1` 与其他训练研究因子相乘；无架构或训练线限制。例如已有前置 weights 的合并因子为 1.1 × 1.15 = 1.265，而不是 1.25。本次购买相对购买前增加的是未取整处理量的 15%。 |
| `Core/XgSim.Board.cs:191–199` | 常规 `CardsPerEpoch` 将该倍率乘入 `n`；手动/自动都适用，之后 `Math.Round` 并限至 4–320 张/轮。最终整数增幅可小于或大于 15%，触顶后可能完全不变，不能承诺每轮多 15% 整数卡数。 |
| `Core/XgSim.cs:639–675`；`Core/XgSim.EpochAction.cs:13–49` | 实时轮次完成时才调用 TrainEpoch 并计算卡数，随后训练、增加 steps、评估。购买时已在进行的轮次若之后正常完成，也在结算时读取新倍率；不是启动轮次时锁定旧倍率。仍须满足训练解锁、资源和阻塞条件。 |
| `Core/XgSim.EpochAction.cs:10, 38` | 轮时长与进度公式不读取 learnrule；不会直接把等待时间缩短 15%。 |
| `Core/XgBoard.cs:535–562`；`Core/XgSim.Board.cs:211–225` | 权重更新读取 `truth - tanh(score)`、学习率与其他旋钮；未检查 learnrule，未用 `step.correct` 限定只有错题才更新。故解释只描述专业概念，购买效果只承诺处理量倍率。 |
| `Core/XgSim.cs:554–557` | 旧/辅助 StepsPerSecond 也乘 ProgressionSpeed；当前实际轮次使用 CardsPerEpoch，不应把该函数的步/秒作为科技页主单位。 |
| `Core/XgSim.Finale.cs:75–84` | 预训练规模道具表没有 learnrule，无额外预训练规模倍率可宣称。 |
| `Core/XgTechTreeCopy.cs:13–16, 47–59`；`Core/XgTechTree.cs:146`；`Desktop/XgTechTreePage.cs:758–772` | 现有名称/说明接入与 mystery 提前返回保留。当前 learnrule 本身没有特殊 mystery 条件，按既有阶段可见性显示；不扩大公开范围。 |

离线边界：`Core/XgSim.Board.cs:193` 在 `OfflineSimulation` 为 true 时提前返回 12 张/轮，绕过所有处理量倍率。这是已确认的代码分支；全 Assets 的赋值检索未发现 `OfflineSimulation = ...` 调用，不能据此宣称产品已经提供离线训练。文案按常规训练描述，集成时若另行启用该模式，应明确其不享受倍率。

## 补丁基线与整合

- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
- 当前源文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `changes.patch` 包含精确旧行和前后上下文，仅替换 Explanation 与 Effect 两个 learnrule 分支。完整依赖哈希在 `source-hashes.json`。
- 整合前先比较目标哈希。若其他词条的并行整合造成哈希不同，只核对这两个旧分支和相关模拟合同后合入，不覆盖整份源文件，不重复应用已等价修改。
- 不需要新接口或生产依赖；不改 XgCatalog、模拟数值、全局清单或其他词条。

## 验证状态与待验证项

静态检查：已核对目录、购买、拥有状态、训练调用、取整/限幅、名称与说明接入及神秘性返回路径。已执行 `git apply --check --ignore-space-change --ignore-whitespace`，补丁上下文检查退出码为 0；该操作没有应用补丁，不等于 C# 编译成功。

实际运行：未运行 Unity、游戏或模拟测试；未读取真实玩家存档。视觉验收：未执行。统一编译与场景验收由整合负责人完成。

整合后建议使用临时测试状态验证真实边界（以下尚未运行，不标记 PASS）：

1. stage=1、缺 weights 或余额不足时不能获得倍率；已有 weights 且 ¥10 足够时购买一次，无需装备；再次购买不扣款、不再次叠加。
2. 两训练线各选未触顶的稳定配置，分别检查手动/自动轮次，购买后未取整处理量为购买前 1.15 倍；整数卡数按实际结算比较，不能要求恰好 15%。
3. 独立边界预期：未取整数为 4 时，乘 1.15 后四舍五入为 5，增幅可大于 15%；已经达到 320 的配置购买后仍最多 320。没有训练完成时，购买不增加 steps。
4. 轮次进行中购买，保持配置与资源稳定；完成时应使用拥有 learnrule 后的处理量，时长参数不因该节点改变。真实得分不要求固定增加，更不能期待准确率直接加 15 个百分点。
5. 专门设置临时 `OfflineSimulation=true` 时应仍为 12 张/轮；这只验证内部边界，不证明产品存在离线训练入口。
6. 科技页中英切换检查名称、解释、效果长文本是否完整换行且不遮挡费用/条件；未发现节点继续走既有隐藏/提示逻辑。

结束复核结果记录于 `verification.txt`；仅本交付目录发生本任务写入。
