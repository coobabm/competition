# 阶段科普题：一次训练 +5% 奖励接入审阅

日期：2026-10-19。只读 production 训练路径；未改生产，未运行Unity、模型或测试。需求以主控最新说明为准：科普问答，答对替换旧+¥20为下一轮小幅训练加成，一次、不叠加。

## 推荐结论

不要给valAcc、Board.Accuracy、考试分数、样本库存或现金直接乘1.05。推荐将奖励解释为“下一轮学习投入/效率 +5%，一次”，在成功开始时绑定到一条训练轨道，在该轮真正结算时应用并消费。

最小接入边界是 XgSim.TrainEpoch 中计算cards之后、TrainBoard之前；公共CardsPerEpoch保持纯查询，因为PaceSeconds也调用它。仅在结算处增加实际训练卡数，会真实增加概念板学习工作，分数继续由既有训练/评估路径产生。

如果产品要求当前模拟曲线也明确得到+5%的正向学习效率，则增加第二个局部接入点：StepAccuracy只在非发散分支计算run.lastRate时，按保留的baseCards算原速率，再乘一次1.05；真实板训练用boostedCards。不要把boostedCards传给GainRate后再乘1.05，否则未触顶时模拟部分可能得到约10.25%的重复加成。

这两种实现区别应明确：
- 只有多处理5%卡数：最少改动、实际训练工作可核对；GainRate的上下限可能使模拟曲线没有差异。
- 多处理5%卡数 + 模拟学习率乘1.05：最贴近目前“训练收益+5%”的承诺，并覆盖现有两套训练表现；需保留baseCards避免双算。推荐采用此方案时把卡片文案写成“答对了：下一轮学习效率 +5%（一次，不叠加）”，不要写“准确率提升5%”。

## 当前调用链与语义

1. BeginEpoch（EpochAction.cs:30）是正式玩家/自动化入口。检查剧情/知识卡模态、轨道是否正在训练、训练解锁、数据和资源，再设置epochActive、规划时长，最后发EpochStarted。
2. TickEpochs按两轨本帧快照推进。结束时先把epochActive清false，再在timedEpochLearning上下文内调用TrainEpoch。当前没有独立FinishEpoch方法。
3. TrainEpoch（XgSim.cs:646）再次检查资源；计算CardsPerEpoch，抽本轮发散风险，未发散时TrainBoard；推进steps/epoch，结算电费，再StepAccuracy、评估/现象/答卷、历史、Assess、进阶，最后发EpochDone。
4. TrainBoard（Board.cs:225）循环cards次调用Board.Train，更新boardTrain及S.boardCards等。Board.Accuracy(TestSet,Knobs)用于真实概念板现象观察。这里多训练的工作确实发生，不能用显示数值代替。
5. run.valAcc主要由AutoModel.StepAccuracy模拟曲线推进；Evaluate只约束范围、计算天花板与练习分数，不会把Board.Accuracy直接赋给valAcc。不要把run.valAcc称为真实模型准确率。
6. Assess并非每轮必发生，现金还依赖刷新纪录/等级。把奖励加到Assess.pay，会变成“下一次有收益评估加钱”，不符合下一轮训练。
7. TickFinale的预训练是持续work积分，没有epoch边界；Project也是独立训练工作。最小方案只覆盖BeginEpoch/TrainEpoch普通一轮，卡片提示应明确。如果要覆盖预训练，需另定义一个有界训练段；不能简单让任意下一帧dt消费整个奖励。

## 一次、不叠加：推荐状态设计

只需一个全局奖励令牌，而不是每轨一个独立奖励。可用新增可序列化字段：
- quizTrainingBonusPending：true表示有一个令牌，包含已预留但未结算的状态。
- quizTrainingBonusOwner：0未预留；1视觉轨；2序列轨。用0作默认值可兼容旧档。

答对：原本无令牌才置pending=true，owner=0；若已pending或已被某轨预留，保持现状，不刷新、不额外排队。多题答对仍都收录知识，不反复发钱。

BeginEpoch：所有启动校验和PlanPace成功后、EpochStarted事件前，如果pending且owner=0，就把owner绑定当前track+1。若当前已有训练在跑，答题不会追溯给那轮加成；另一轨随后成功开始可领取，两个Begin按调用顺序只会领取一次。

TrainEpoch：结算只使用与自己轨道相符的预留令牌。已经在跑但未预留的轮，哪怕更早结束，也不能抢走pending。

注意timedEpochLearning：由于TickEpochs清epochActive后才调用TrainEpoch，不能用“此刻epochActive=false”来判断这是新训练。直接调用TrainEpoch的测试/确定性路径可视为零时长的一轮，在所有前置校验通过后领取未绑定令牌；但需要明确排除timedEpochLearning所标记的已有在途轮，且不能抢其他轨已预留的令牌。

在实际训练提交点清pending/owner，且在可能发出事件的后续代码前完成，以免重入、回调或存档把已使用令牌保留下来。不要在纯查询、UI刷新、GainRate预览、CardsPerEpoch或BeginEpoch失败时消费。

## 失败、停顿、取消与存读档

| 情况 | 建议行为 |
|---|---|
| 启动被锁/资源不足/已在跑 | 不预留、不消费。 |
| 已预留轮因断电、接线、剧情、知识卡等暂时停顿 | 保持预留；这是暂停，防止另一轨抢走。 |
| TickEpochs完成后TrainEpoch复检却返回null | 解绑定，令牌回到pending，不应吞掉奖励。 |
| TearChance判定本轮发散，TrainBoard没有执行 | 推荐回到pending，等待下一次真正学习；这不额外发令牌。也可定义“一次尝试即用掉”，但更容易让玩家以为奖励未生效。 |
| 实际TrainBoard已经执行，部分Board.Train返回diverged | 令牌应算已使用，不能无条件退款并保留额外训练结果。当前TrainBoard返回torn>0，但调用者未使用这个返回值；不要把e.diverged误当完整的真实板失败标记。 |
| 学习执行后模拟曲线drop/rolledBack或已经达天花板 | 消费一次；实际学习投入已经发生，不保证分数上涨。只对正向效率加成，不扩大惩罚、不自动消除失败。 |
| 取消/读档丢弃尚未完成轮 | 将owner清0，pending保留，奖励退回待用；不复制额外令牌。 |
| 正常结算后保存/重载 | pending=false；不能从已答对历史重新授予奖励。 |
| 旧档 | 新字段默认false/0，不追溯把旧quizAnswered兑换成新奖励。 |

关键现状：XgSim.Repair（XgSim.cs:166–169）明确强制清除两轨未完成的epochActive/epochProgress；未完成训练不会续跑。因此只把bonus放在run上且Begin时删pending，会在读档时吞奖励。使用上述全局pending+owner时，Repair把owner清0即可自然退回，不需要改变现有训练读档规则。不要在每次InitializeTrainingLessons里清owner，该方法在UI中频繁调用。

消费必须发生在一轮训练真正进行的同一同步调用内；对于异常中断，不建议盲目finally退款，因为板权重或统计可能已部分变化。至少不要让成功后的EpochDone监听器抛异常重新变出奖励。

## +5%计算的边界

- CardsPerEpoch正常返回整数4至320。若先乘1.05再夹回320，高配玩家几乎完全得不到额外卡数。建议一次奖励允许上限336，正常轮仍保持320。
- 四舍五入是整数训练卡的必然量化，例如320→336、100→105、24→25；不要强制min bonus=1，否则4→5实际变成+25%。低于10张时可能取整为0，UI不要承诺准确增加一张。若同时有模拟学习效率加成，小卡数时奖励仍会体现在模拟正向效率上。
- 别把host.Compute乘1.05当作卡数+5%：卡数计算中compute的指数是0.7，得到的只是约3.47%原始卡数变化，还可能被取整/上限吃掉。
- 保留原BilledSeconds与训练时长，奖励才是同样一轮里多学习；不要通过公共CardsPerEpoch把PaceSeconds连带增大，增加电费抵消奖励。
- 不增加S.labels或SamplesEffective：这些是可用数据，不等于同一轮重复处理的训练卡。
- 同时更新TrainBoard实际cards、run.steps、e.steps、RecordTrace cards，避免“展示+5%但真实板没有多训练”。若StepAccuracy需精确乘单次因子，单独保存baseCards作曲线计算输入。
- 不修改GainRate公共查询的永久状态；把factor作为本轮局部值或私有StepAccuracy参数，以免预览/诊断/下一轮泄漏。

## 主控适合补的最小验证

1. 正确答题不再入账¥20；奖励只存一个；错误答案不发奖；重复/图鉴复习不发奖。
2. 旧在途轮结束不领取；其后成功Begin的轮领取；双轨只有一轮有bonus。
3. 额外Board.S.cards/S.boardCards、run.steps和e.steps一致；高配320→336；现金、电费和时长没有额外加成副作用。
4. 正向模拟学习效率只乘一次；真实Board.Accuracy不被强改；drop/plateau仍可发生。
5. 启动失败与结算前失败不吞奖励；正常训练结算之后不再次可用。
6. 绑定中保存再读档：训练取消而令牌退回pending；已用后读档不再发；旧档字段默认无奖励。

证据基目录：C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016/Assets/LingGuangV05/XingGuang/。
主要文件：Core/XgSim.EpochAction.cs、Core/XgSim.cs、Core/XgSim.AutoModel.cs、Core/XgSim.Board.cs、Core/XgSim.Wiring.cs、Core/XgSim.TrainPace.cs、Core/XgSim.Finale.cs、Core/XgModels.cs。


## 落地实现复核（第二次只读审阅）

已核对pending bool + 每run reserved bool方案：成功Begin预留；timedEpochLearning防止旧在途轮领取新答题奖励；pending或任一reserved时再答对不叠加；Repair先refund再取消未完成轮；TickEpochs结算返回null时refund。真实板使用round(baseCards*1.05)，曲线使用baseCards并只乘一次factor，费用和Pace未改。静态未发现这些路径的重复授予。

**发现的明确漏项：离线路径（已即时通知主控）。** 首次复读时ReserveTrainingQuizBonus与ConsumeTrainingQuizBonus均无OfflineSimulation守卫。OfflineSimulation=true时直接TrainEpoch会领取pending，OfflineCardsPerEpoch=12被变为13并消费奖励；正常Begin预留后切offline推进完成也会消费reserved。需分别守住领取和结算：离线不从pending预留，离线结算不应用factor也不丢失reserved。若离线结算已经把该轮结束，可将其reserved退回pending，避免令牌滞留到特定轨道。

其余取消调用核对：生产中切换数据、调学习率、LoadModel均在epochActive时阻止；断电/接线/剧情/知识卡使TickEpochs暂停，未取消预留；真正赋epochActive=false只见Repair与TickEpochs。未发现另一个会丢预留令牌的取消入口。

边界说明：本轮TearChance在Consume之后才抽取，因此发散会消耗令牌，即使TrainBoard被跳过。若定义为“下一次完成的尝试”这是可接受的既定行为；若希望“无学习不消耗”，才需调整，不将此列为确定bug。真实板训练以后掉分/回退不宜退款，避免已获得额外训练却重复领取。

未启动Unity、编译或测试；主控正在添加验证用例。本节不能作为运行通过证明。

**最新复读：上述offline漏项已由主控修复。** 当前Reserve在OfflineSimulation时直接返回；Consume在OfflineSimulation时Refund当前run并返回1。pending不会被领取，已预留轮离线结束也退回pending，未发现仍未解决的奖励状态问题。仅静态复核，未运行测试。
