# tech-036：momentum（动量）词条核查

结论：现有 ×1.3 训练处理量和最快优化器优先规则正确；交付仅针对 momentum 的精确文案补丁，补足正式全称、原理与实际效果的区分、购买后生效时机及离线模拟例外。生产文件未修改，补丁待整合。

## 范围与去重

已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md` 的已实施内容，保留其中已有正确的 Speed() 说明和最快优化器规则；此前其他 tech result.md 的 momentum/动量定向搜索未发现重复交付。未读取任何新策划正文，未改其他词条、模拟数值、真实玩家存档或全局清单，未发送其他聊天消息。

## 交付

- `copy.md`：可直接采用的中英文名称、解释、效果。
- `changes.patch`：仅改 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`，含三处带上下文的修改：Name 增加 momentum 全称；Explanation 明说算法原理；在通用优化器分支前增加 momentum 专用效果。其他优化器继续使用原分支。
- `source-sha256.json`：全部 10 个核对依赖的 SHA256。
- `verification.json`：结束前依赖复核与补丁只读适用性检查记录。

补丁目标原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。整合前仍须比对最新文件及上下文；不要用旧文件整体覆盖并行变更。

## 原文件证据与时机

下列路径均相对工程根目录，除页面文件外位于 `Assets/LingGuangV05/XingGuang/Core/`。

| 证据 | 核查结论 |
| --- | --- |
| XgCatalog.cs:306–309、395、631–632 | momentum 是研究项/优化器，不是架构；第 2 阶段、前置 mlp，节点基础价格 200，speed=1.3，stability=1.4。研究对象的旧 cost=3 不是科技节点售价。 |
| XgSim.Progression.cs:131–148；XgSim.cs:194–205 | 未拥有时须阶段可见、满足前置与购买条件；当前 NodeCost 为 ceil(200×0.25)=¥50。揭示节点或达到购买条件不产生倍率。价格沿现有页面动态显示，不写死在补丁。 |
| XgSim.cs:252–278、318–347 | 成功购买加入 S.unlocked；没有装备步骤。Optimizer 每次读取时选择已拥有项中 speed 最大者。购买后自动配置并调用 Evaluate，但不凭购买承诺加分。 |
| XgSim.EpochAction.cs:34–50；XgSim.cs:639–675 | 实时训练在轮次结算调用 TrainEpoch，再计算 CardsPerEpoch；因此购买后下一次轮次结算即使用当前优化器，也可影响购买时已经开始但尚未结算的轮次。训练仍需样本、资源和运行条件。 |
| XgSim.Board.cs:190–199 | 两条训练线、手动和自动训练都进入同一公式；1.3 是取整和上下限之前的处理量倍率。最终单位张/轮，最少 4、最多 320 张；非准确率、秒数或固定多处理 30%。OfflineSimulation 分支固定 12 张，忽略该倍率。 |
| XgSim.cs:324–347 | 优化器倍率互斥取最快：momentum 1.3 被更快已拥有优化器替代，不与其相乘；与 SpeedResearch 等其他处理量因子相乘。无重复购买叠加。 |
| XgBoard.cs:535–563；XgSim.Board.cs:94–115 | 当前逐卡更新按当前误差和学习率计算 pull，未实现 momentum 历史方向状态。解释因此明确为动量算法原理，不把现实算法原理当作游戏额外效果。 |
| XgSim.cs:344–346、554–582；XgBoard.cs:130–139 | 目录 stability=1.4 会进入旧 Stability/Hazard 公式，但默认 UseBoard=true（XgSim.Board.cs:41），momentum 的 OptimizerSteadiness 仍为 1，与基础 SGD 相同。当前棋盘撕裂阈值不获 1.4 倍；不得宣传更稳 40%、风险下降 40%或安全学习率提高。 |
| XgSim.AutoModel.cs:454–476 | 当前成绩回落风险计算没有读取 momentum 或 Stability；不能把旧 Hazard 数值直接写成玩家可见掉分概率改善。全 Assets 定向搜索只找到 Hazard 与 StepsPerSecond 的定义，未找到调用。 |
| XgTechTreeCopy.cs:15、51；XgTechTree.cs:129；Desktop/XgTechTreePage.cs:758 | 保留已有 NodeMystery 提前返回与 NodeVisible 页面门控。momentum 当前不是特殊神秘节点；未到阶段依旧不显示。不新增后续节点名称或解锁路线，避免剧透。 |

## 验证状态

已执行：源码静态追踪；补丁 `git apply --check --ignore-space-change` 只读检查返回 0；结束前对 10 个依赖重算 SHA256，无变化。未把既有整合任务的运行结果记为本任务测试结果。

未执行：C# 编译、独立内存运行、Unity/游戏运行和视觉验收。没有实际运行 PASS 声明。没有为了文案改动创建重复实现公式的测试。

## 待整合与待验证

由整合负责人应用本补丁并统一编译，实际隔离场景验证：

1. 第 1 阶段隐藏；第 2 阶段未购买只揭示可见信息，倍率仍为 SGD；缺 mlp 或钱不足购买失败不生效。
2. 单独购买 momentum：训练页优化器显示变化；固定相同算力、结构、连击与资源，卡数按未限幅时约 1.3 倍增加。相同设置下准确率不承诺即时增加。
3. 手动与自动训练、视觉与序列两条线；特别在训练开始后结算前购买，核对下一结算使用新倍率；轮次耗时不应被解释为直接缩短 30%。
4. 已拥有更快优化器时 momentum 不额外叠乘；搭配其他处理量加成时相乘。低算力下取整/4 张下限、高算力下 320 张上限可能使显示卡数没有完整 30% 增量。
5. 离线模拟固定 12 张不因购买改变；同一棋盘参数下 momentum 不改变基础 SGD 的 steadiness 或 TearAt。
6. 中英文名称和较长效果段落在科技提示的最小窗口/缩放下可读；神秘节点提示保持原状。只验收本词条，不据此标记原文全局原则或整份策划完成。
