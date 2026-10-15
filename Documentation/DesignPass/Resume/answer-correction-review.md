# CorrectionPulls 实际写入复核与修正

2026-10-13。先按主聊天授权只读审阅 CaptureBeforeCorrection / CaptureCorrectionPulls 与实际 Board.Train，随后按明确授权完成实际写入捕获修正。只涉及当前纠正反馈真实性，不扩读策划。

## 已确认正确的范围

- 采样围绕真实 TeachBoard，位于 NewCard 之前；没有为视觉再次调用 Predict 或 Train。
- pinned 在 Train 的 R1 与 Capture 的最终遍历中均排除。
- 旧概念只发生 R4 衰减、trainedCards 未增长时被排除；异脑区也被排除。
- 不正确/超时纠正、无本地预测来源不伪造 CorrectionPulls。值与集合是复制后的不可变快照。

## 发现 1：新建而被 Dropout 跳过的概念（主聊天已修过滤）

原 Capture 仅在 existed 为 true 时要求 trainedCards 增长。Board.Train 在 R1 之前创建缺失元素，Create 的默认 strength=1、trainedCards=0；随后 Dropout 可直接 continue。因此新概念没有被拉动，却由于 s=1 不等于默认 old.s=0 而进入列表。

主聊天将门槛改为新旧概念一律 `cell.trainedCards > old.trained`，新概念 old.trained 默认 0。

候选回归：`CorrectionDropoutChecks.cs`。它使用实际 XgSim、TeachingGuess、TryEvaluateTeachingGuess、Board.Train 与内存钱包，不复刻 Dropout 哈希，不反射写权重/训练计数来冒充训练结果。

确定命中配置：spam；seed=9139；问句“恭喜您中奖，请点击领取”；truth=false；解锁 dropout；对应 run.depth=1、width 为最后索引；board.cards=15、nextId=901。id17 是首特征的 pinned 正权锚点（w=.8/s=1/trained=7），保证真实预测错误；id900 是同脑区无关词（w=.8/s=1/trained=5）。经真实教学 UI 路由纠正后，新 id905 的 trainedCards=0；旧 id900 发生衰减而计数不增；两者及 pinned17 均被过滤，实际训练的新概念保留。

当前修正源码独立纯核心编译并实际执行回归通过：`LocalVerification/DesignPass/CorrectionDropoutCompile/runner-result.log`。这是 .NET 纯核心结果，没有 Unity 或视觉验收。编译日志保留工程原有 DayStats CS1718 警告。

## 发现 2：参与 R1 但参数被夹紧、随后仅衰减（已修正）

trainedCards 增长只能证明循环经过该概念，不能证明其实际权重/强度被改变。匹配概念在 w=-WeightLimit、s=10 时，负方向拉动被限制，强度也饱和；R1 依然增加 trainedCards。恰遇第 16 轮，后续 DecayAll 改变最终 w/s，Capture 就会把这次纯衰减记录为拉动。

已用真实同一纠正路径做相邻轮对照：

| 是否发生 R4 | 训练后 w | 训练后 s | trainedCards | 是否进入 CorrectionPulls |
|---|---:|---:|---:|---|
| 第15轮，无衰减 | -4 | 10 | 5→6 | 否 |
| 第16轮，有衰减 | -3.990410792443685 | 9.976026981109213 | 5→6 | 是 |

审计程序/证据在 `LocalVerification/DesignPass/CorrectionDropoutCompile/ClampAudit.cs`、`clamp-result.log`，进程退出0表示成功复现此问题，不代表逻辑已修复。

若反馈合同是“真实发生的拉动”，应在 Board.Train 的 R1/R2 实际写入点、R4 之前获取变化证据，而不是用训练结束后的值加计数推断。该方法还能避免饱和计数的极端漏报。未建议改变训练算法、clamp、衰减或数值，仅需要观察位置准确。

上述反例对应修正前 AnswerTrace.cs：`7783F161DBD3EA12FC4F98386F498685B62C54F25E175968A57D711B880139F6`。

## 已落地的实际写入捕获

主聊天 54 退出后，对五个原文件逐一校验完整 SHA256，一次落地 Core 四文件及 AnswerTraceChecks，之后冻结 Assets。基线、最终摘要和差异存于 `CorrectionWriteCandidates/hashes.json`、`changes.patch`。

- 保留 `XgBoard.Train(card, knobs)` 双参入口，内部可选择传入临时收集列表。R1 在 pinned/Dropout 排除之后、clamp 与 strength 更新前后捕获，只有 w/s 实际改变才记录。捕获不依赖 trainedCards，因此其饱和也不会漏掉真实变化。
- 创建集合仅在有捕获列表且实际创建新 R1 概念时分配；Create 返回 null（含叠加/容量拒绝）不会标记 Created。R2 只在真实 Create 成功后记录。
- R1 新概念的 Before 是初始化后的 w=0/s=1；R2 新组合的 Before 使用缺席态 0/0。所有 After 都位于 R4 衰减之前，代表训练写入本身，不宣称是最终净状态。
- `Answer` 只在有订阅者且本题确实是纠正时分配列表并传入真实 `TeachBoard`；删除整盘前后差分推断。普通训练没有新增捕获集合，训练算式、顺序、随机数、返回值与支付逻辑保持原样。

纯核心编译及实际执行通过：`LocalVerification/DesignPass/CorrectionWriteCompile/runner-result.log`。执行的是生产 Core 候选、真实 Answer/Teaching/Train；仅测试日志与内存 JSON 克隆适配到 .NET。仍保留原有 DayStats CS1718 警告。

新增与保留的检查包括：

- 同一夹紧概念在有/无 R4 两种实际教学中均无纠正记录，训练计数仍真实增长；实际变化的阳性对照在 long.MaxValue 计数饱和时仍被捕获。
- 阳性对照在相邻衰减边界的 Before/After 完全相同；有 R4 时最终细胞数值明确不同于快照。
- 通过实际教学生成 R2 高层组合，验证 Created 与 0/0 基线，和 R1 新概念 0/1 基线，并分别覆盖有/无 R4。
- Dropout 新 id905 没有被训练，旧 id900 只衰减，pinned17 不变，均不报告纠正；实际受拉动概念保留。
- 原有不可变性、只读集合、错误玩家纠正、空来源与拒绝、事件次数、无订阅时结果/状态/RNG/金额一致性全部通过；夹紧/衰减实际路由另做同样的订阅对照。

本次没有启动 Unity；Unity 集成、视觉与物理输入验收由主聊天后续完成。不是视觉验收结论。
