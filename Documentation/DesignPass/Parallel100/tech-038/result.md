# tech-038 · Adam 词条核对

日期：2026-10-13（Asia/Singapore）。结论：确认需要一份仅作用于 Adam 的文案补丁；尚未整合到生产文件。

## 范围与去重

只核对 adam、其购买入口和真实训练调用链。已核对已有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`；本目录此前不存在。既有通用优化器效果正确说明了处理量和互斥，但未说明 Adam 的失稳阈值、自动生效时机，解释也未区分理论与简化实现。没有另起聊天/agent、改模拟数值、运行 Unity/游戏、读写玩家存档、提交或推送。没有打开新的策划正文；一次记录搜索意外命中了已有逐条清单中的 Adam 行，未沿该清单继续读取，结论只依赖生产代码。

## 可直接采用的中英文文案

正式名称：Adam 优化器（自适应矩估计）

通俗解释：Adam 的原理是：记住最近往哪边改、改动有多大，再为每个参数选择步幅。游戏中的简化效果见下方。

真实效果：第四阶段、拥有 RMSProp 后可购买；购买后自动用于两条训练线，无需装备。相对基础 SGD，训练处理量 ×2；实际卡数受算力、模型大小、取整和上限影响。训练时，导致大脑失稳的步幅阈值 ×1.25，与批量归一化和裁剪的阈值倍率相乘；仍可能失稳。优化器仅取最快已拥有项，不彼此叠加；处理量倍率与其他训练倍率相乘。训练结算采用当前效果，成绩需训练后评估。学习率标签沿用 RMSProp 的显示比例，不表示实际步幅缩小。

Name: Adam optimizer (Adaptive Moment Estimation)

Explanation: Adam works by remembering recent update directions and sizes to choose a step for each parameter. Its simplified game effects are listed below.

Effect: Available from stage 4 after RMSProp. Buying automatically applies it to both training tracks; no equipping is needed. Relative to base SGD, training throughput is ×2; actual card counts depend on compute, model size, rounding and caps. During training, the step threshold for brain instability is ×1.25, multiplying with batch normalization and clipping threshold factors; instability is still possible. Only the fastest owned optimizer applies, without stacking optimizers; throughput multiplies with other training factors. Training uses the current effects at settlement; evaluate after training for scores. Learning-rate labels keep the RMSProp display scale; this does not shrink the actual step.

## 证据与边界

以下路径均相对 `Assets/LingGuangV05/XingGuang/`，行号为交付时原文件行号。

| 事项 | 代码依据与结论 |
| --- | --- |
| 节点身份与价格 | `Core/XgCatalog.cs:369–370,425,647–648`：adam 是 Optimizer 研究节点，不是架构；第4阶段、前置 rmsprop，speed=2、stability=3。R 的报价5000先经 StagePrice[4]=0.7（348、356行），再经 `XgSim.Progression.cs:131–137` 的0.25，当前实付 ¥875；不能把 Research.cost=150 当科技页价格。价格已有页面动态显示，本补丁不硬编码。 |
| 购买而非看见即生效 | `Core/XgSim.Progression.cs:140–164` 和 `XgSim.cs:194–205,252–279`：须阶段、前置、资金均满足；成功扣费才加入 unlocked。购买调用自动配置和评估；没有单独装备步骤。仅显示/解锁购买资格不赋予加成。 |
| 自动采用、两线共享 | `Core/XgSim.cs:318–347`：遍历已拥有优化器，仅取 speed 最大者。Adam 当前2大于RMSProp1.6和Momentum1.3。OptSpeed和OptimizerSteadiness是全局读取，各训练线共用。各优化器处理量和稳定性不相乘。 |
| 处理量单位 | `Core/XgSim.Board.cs:191–199`：2是无量纲因子，与架构、研究、进度、手动/自动等因子相乘，结果单位为张/轮，四舍五入并限4–320张。相比必需前置RMSProp的1.6，在其他输入固定且取整/限幅前是增加25%，并非再翻倍。不能承诺准确率翻倍、轮时长减半。OfflineSimulation专用分支固定12张/轮，跳过处理量倍率；不把它当正常游戏增益。 |
| 失稳阈值 | `Core/XgSim.cs:344`、`Core/XgSim.Board.cs:94–116`、`Core/XgBoard.cs:130–139,553`：Adam steadiness=1.25，乘入 TearAt；与BatchNorm1.5、启用裁剪2相乘后，仍受深度、跳连及循环结构影响。阈值对应学习步幅×误差的绝对值，不是成功概率、准确率或掉分概率增加25%。相对RMSProp的1.15，阈值是1.25/1.15，而非在RMSProp上再乘1.25。 |
| 安全学习率与显示 | `Core/XgSim.cs:331–340,568–582`：Adam和RMSProp均仅将RateLabel缩放为RateValues的0.01，Knobs.lr仍取RateValues；RMSProp→Adam不会独自触发标签100倍变化。`Core/XgSim.AutoModel.cs:201–211`按SafeLr自动选离散学习率，阈值提高可能跨档，也可能完全不变。 |
| 何时参与训练 | `Core/XgSim.EpochAction.cs:13–25,39–49`启动轮次不训练，完成时调用TrainEpoch；`Core/XgSim.cs:649–668`在结算时读取CardsPerEpoch并TrainBoard，再更新步数和评估。Adam研究购买不受AnyEpochActive的架构/项目限制，因此进行中的轮次也会在随后结算时读取新效果；已结算轮次不追溯。自动配置在活动轮次期间跳过（AutoModel:219–221），到结算再配置。 |
| 没有真实逐参数Adam更新 | `Core/XgBoard.cs:535–565`使用共同的pull公式，未维护Adam的一阶/二阶矩或偏差校正；`Core/XgSim.Board.cs:113–115`只传入steadiness和常规lr。补丁把解释明确标为Adam原理，不能把理论当已实现算法。 |
| stability=3的地位 | `Core/XgSim.cs:346,554–582`：Stability可乘BatchNorm1.5；在Hazard旧公式和UseBoard=false的SafeLr分支使用。当前默认UseBoard=true（Board:41），真实轮次处理量见上；不把“稳定性3”宣称为当前游戏稳定三倍或风险除以三。 |
| 神秘性 | `Core/XgTechTreeCopy.cs:15,55`已有null/NodeMystery/Atlas提前返回；补丁保留。`Desktop/XgTechTreePage.cs:748–764`也先处理未发现图鉴和神秘节点。Adam本身没有NodeMystery特例，其阶段可见性仍由原页面和NodeVisible决定，不改发现条件。 |

## 补丁与整合

- `changes.patch`：仅目标 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`，3处小范围上下文修改：专业名、原理解释、Adam专属效果分支（位于通用优化器分支前）。其他优化器不改。
- 目标原始字节 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `source-hashes.json`记录所有直接核对依赖的SHA256。应用前复核当前源码；如其他聊天已改相邻区域，按语义整合这3处，不覆盖文件。倍率2继续从research.speed读取；1.25目前只在模拟getter中以字面量定义，补丁按核实数值描述，不读取sim.OptimizerSteadiness来描述未购买Adam（那会错误显示当前优化器）。
- 无需修改XgCatalog、XgSim、XgBoard、存档或其他词条。公开API与节点ID保持原样。

## 验证状态与待验证

- 已完成：只读静态调用链与数值核对；补丁生成时所有已有依赖哈希与读取时一致；仅本专属目录写入。
- 本聊天未编译C#，未执行模拟器、Unity或真实游戏，没有运行PASS或视觉PASS结论。既有任务记录的测试结果不是本补丁的验证结果。
- 待整合负责人：统一编译并使用隔离内存状态检查阶段3/4、缺RMSProp/已拥有RMSProp、资金不足/足够、重复购买；单纯可见不得改变训练效果，购买后选Adam，重复购买不得再次扣费或叠加。
- 真实边界：同配置检查RMSProp→Adam原始处理量1.25倍，但4张下限、320张上限及四舍五入可令实际比例不同；专用OfflineSimulation应仍为12。对TearAt检查恰等阈值不diverge、超过阈值才diverge，以及BN/clip组合；不要用“始终不会失稳”作为预期。
- 生效时机：在活动轮次中购买Adam，验证完成结算采用新倍率，过去轮次不重算；分别核对两线、手动/自动轮。SafeLr档位不一定改变，RateLabel缩放仍为0.01。
- 视觉验收由主聊天：中英文名称和较长效果段落无裁切；第4阶段前的可见范围、未发现节点名称/说明/价格保持原有遮蔽。
- 仅标记tech-038静态核对交付完成；不标记全局原则、其他词条或整份文档完成。

结束前复核：source-hashes.json 所列11个依赖均未变化；补丁仅含一个目标文件，中英文UTF-8文本可读。补丁适配检查为静态检查，不代表编译或运行验证。
