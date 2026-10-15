# lesson-012：教学评价的结算副作用

日期：2026-10-13（Asia/Singapore）。只核对已授权第 13 行涉及的资金、样本、权重、纠错记忆；未读取新的策划正文。

## 结论

当前实现无需生产修改，不交付 changes.patch。在正常单线程调用、有效模型状态及正常返回的宿主/事件订阅者前提下，没有发现失败或过期输入造成这四类数据的半次结算。独立内存检查直接编译引用当前生产规则，实际运行 **402 条断言通过**，见 `run.log` 的 `LESSON012_MEMORY_CHECKS_PASS assertions=402`。这不是 Unity 编译、真实游戏运行或视觉验收。

已先核对 `Documentation/DesignPass/Agents/issues-task-001.md`、既有 `DesignFirstLessonChecks.cs`，并参考已到达的 lesson-002 / lesson-005 记录。既有检查已覆盖判断映射、教学资格与复核隔离；本交付补足资金账目精确增量、权重快照、日志池、余额/扣款失败、纠错记忆满容量边界，不重复改写已有实现。

## 各分支的真实副作用

| 输入/结果 | 资金 | 样本/日志 | 权重 | 纠错记忆 |
| --- | --- | --- | --- | --- |
| 当前卡，准确认可模型猜对 | 发一次普通教学报酬；totalIncome 同步增加 | labels +1；若有待标日志则消耗 1 条，合计样本 +4，否则 +1 | TeachBoard 一次，实际训练次数 +1 | 不追加、不淘汰 |
| 当前卡，准确纠正模型猜错 | 教学报酬 ×2；totalIncome 同步增加 | 同上 | 同上，训练真值为原卡 truth | 累计 +1；追加原卡记录，超过 20 条移除最旧记录 |
| 当前卡，玩家误判 | 无报酬；按规则罚款，最多现有非负余额；仅 Spend 成功才记 totalSpent/handFines | 不增加样本、不消耗日志 | 不训练、不改变权重 | 不追加、不淘汰、不增计数 |
| 当前卡超时，即 age > timeLimit > 0 | 无报酬、不罚款 | 不变 | 不变 | 不变 |
| 旧卡号、旧预测、空 host、未知 desk、教学/棋盘关闭或当前桌已有复核卡 | 不进入 Answer，无资金结算 | 不变 | 不变 | 不变 |
| 同一张已结算卡重复提交 | 被旧卡号检查拒绝，不再次收支 | 不变 | 不变 | 不变 |

误判和超时是**已消费的一次失败**：handWrong +1、连击/模型连对归零、换卡；不是应完全静默的拒绝请求。TryEvaluateTeachingGuess 返回 true 表示请求已消费，不代表 correct=true。超时边界是严格大于：age == timeLimit 仍可正常成功。成功报酬还保留金卡/悬赏倍率，未在本任务改变。

## 源码证据

下列路径相对工程根目录；对应 SHA256 见 `dependencies-before.json`，结束前对照见 `dependency-check.json`。

- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:46–56`：先拒绝无效 host/desk、复核队列、旧卡号和变化的猜测，之后才调用 Answer；仅 correct 时发 LessonTaught。
- 同文件 `:74–82`：RememberCorrection 写原卡数据和真值，累计一次，按 20 条上限淘汰。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:1385–1447`：正确分支内才 Earn、增加收入、AddLabel、HandLabelLog、TeachBoard、RememberCorrection；错误分支只记录失败及非超时罚款。最后统一换卡。
- 同文件 `:1078–1088`、`:1102` 附近：AddLabel 精确增加一个 label；Samples 包括额外日志样本，不应只比较 labels。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Flywheel.cs:109–116`：HandLabelLog 只在正确分支调用，一条日志对应额外 3 个样本，加当前 label 共 4 个。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Spider.cs:152–159`：ChargeHandFine 将应付数裁剪到可用余额，仅在 Spend 返回 true 后增加支出和罚款账目。余额不足或拒扣不会反向触发学习。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:283–289`：TeachBoard 构造原卡训练特征，使用传入的真实标签调用 Board.Train。
- `Assets/LingGuangV05/XingGuang/Core/XgBoard.cs:516–522`、`:535–598`：Predict 不调用 Train；Train 才创建概念、更新 w/s/训练次数及 board.cards。检查快照包括 board.links 等 NonSerialized 公共字段，而非仅凭 Samples 推断没有学习。
- `Assets/LingGuangV05/XingGuang/Desktop/XingGuangHost.cs:32–51`：真实宿主 Spend 失败不减钱，成功扣款并记账；Earn 在 Live 且金额有效时入账。该宿主只静态审阅，没有构建真实运行时或访问真实钱包/存档。

## 验证产物与运行范围

- `Lesson012Checks.cs`：纯内存检查入口，可由整合负责人参考/整合；未放入 Assets。直接调用生产 XgSim，不复制生产结算算法、不替换生产模型。
- `Lesson012.csproj` / `NuGet.Config`：.NET 10 独立验证工程，引用生产纯规则源码；包源为空，无第三方包。只构建本验证程序，没有启动 Unity、场景或游戏。
- `run-checks.ps1`：重跑入口。在本目录内设置 SDK 输出/包目录并禁用开发证书初始化及工具 PATH 初始化。需本机已有 .NET 10 SDK。
- `run.log`：实际最终运行日志，退出码 0，402 条断言。16 组评价/真值/猜测/日志组合；7 类拒绝；6 组资金边界；超时临界点与超时后；每组成功/失败后还验证重复提交。失败断言比较完整学习状态；已初始化的拒绝夹具比较整个公开 XgState 与钱包字段。
- `initial-build.log`：首次独立工程漏列公共依赖，补齐引用后通过；不是生产缺陷。最终构建仍出现既有 `XgSim.DayStats.cs:33` 的 CS1718 警告，未越界修改。
- `dependencies-before.json` / `dependency-check.json`：92 个源码/记录依赖的 SHA256；结束前全部一致。

边界说明：TeachingGuess 可懒初始化 checkups，Card 可在当前卡缺失时创建新卡，因此不要把本结论扩大为“拒绝请求绝不改变任何辅助字段”。四类结算资源未在这些辅助步骤中学习/收支。检查使用已展示、已初始化的有效卡，符合页面评价入口。

这条路径也不是任意异常下的数据库事务：IXgHost.Earn 没有成功返回值，外部回调抛异常/重入、任意损坏状态、失效宿主不在本次通过的边界内。未据此制造与第一分钟评价无关的事务重构补丁。

## 待整合、待验证

无需合入生产修正。整合负责人可收录本结论与独立验证，生产源码有变动时重新比较 SHA256 并重跑。不要仅凭这份内存检查将第 13 行或整份文档标为完成。

主聊天仍需统一执行 Unity 编译、已有 DesignFirstLessonChecks.Run、真实宿主资金显示及场景/壁纸视觉验收。本任务未运行这些项目，没有截图，没有读取玩家存档，没有提交、推送、打包或向其他聊天发消息。

操作记录：交付文件与验证构建输出位于本专属目录，生产文件未修改。首次 .NET SDK 初始化日志报告开发证书安装和工具 PATH 添加失败；此副作用在首次运行时未预期，未进一步读取或改动证书，后续运行及重跑脚本已显式禁用该初始化。
