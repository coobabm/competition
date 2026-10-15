# 1:082 核心快照与答题事件交付

日期：2026-10-13（Asia/Singapore）。本轮经主聊天授权，从前一份只读审计实施最小核心接口；不实现壁纸 UI，不启动 Unity，不读其他策划，不改玩法或存档 schema。

## 已落地合同

- `XgBoard.cs`：原 Score/Predict 接口保留；新 `Predict(card, knobs, out guessed, out XgPredictionTrace)` 通过同一份 Elements/Gate/Matches/Score 算法采集。原调用不分配 UsedConcept 集合。固定规则原先首个命中即 return 的分支只采这一条；普通分支只采实际越过权重阈值的读出项，保持求和与遍历顺序。
- `XgSim.AnswerTrace.cs`：get-only `XgUsedConcept`（Id、Layer、Region、Key、Alt、Weight、Activation、Vote、Pinned）；不可变 `XgPredictionTrace`（AnswerYes、Guessed、Score、TopLayer、UsedConcepts）；`XgAnswerTrace`（CardId、Dataset、Region、SubmittedYes、Settled、Source、Prediction、Outcome）。集合复制为不可写视图，无活概念引用、无存档字段。
- 事件 `XgSim.CardAnswered`：普通正确、错误、已过时限提交及家庭意见结算后各一次；保留原结果／付款／训练／NewCard 顺序。拒绝的 host/desk 或教学旧卡／旧预测不发；教学包装层不重复发。普通 Outcome 的历史 accepted=false/cardId=0 原样保留，事件用 Settled=true 与原 CardId 明确表达已经结算。
- `XgSim.cs`：Answer 现有 TeachingGuess 调用取得训练前快照；仅有事件订阅者时采集。普通没有 Board 预测、family、law、shutdown 使用空 Prediction，Source 明确区分。family 在原早返处包装发布，未改 Family 实现。
- `XgSim.Teaching.cs` 重载由主聊天协调并落地，本 agent 未修改该文件。接口 `internal bool TeachingGuess(XgCard, out bool guess, out XgPredictionTrace trace, bool captureTrace)` 已核对：原初始化／guard 保留，末尾只执行一次原预测或带快照预测。

Prediction.AnswerYes 是模型实际输出，SubmittedYes 是最终提交标签；教学“它答错”可能让两者相反。壁纸要表达模型推理路径时应使用 Prediction.AnswerYes，不能按 Outcome.correct 配是／否颜色。无本地预测时不虚构模型格子。

`AnswerQueued` 所在 Collaboration.cs 不在本轮授权文件范围，此次没有发新的 CardAnswered；外部自动 Route、检验题、训练 Score、纯刷新预测也不发此事件。主聊天如扩大到队列，应单独在确实接受并结算后的出口接入，不把等待／拒绝报告为完成。

## 检查及实跑

安装入口：`Assets/LocalBuild/Editor/AnswerTraceChecks.cs` 的 `AnswerTraceChecks.Run()`。候选副本保留于 `Documentation/DesignPass/Resume/AnswerTraceCandidates`，与安装版一致。覆盖：

- 采集／不采集预测结果一致、普通正负读出与阈值排除、pinned 短路、空猜测、不可写集合、后续概念变更不污染快照。
- 正确／错误 × 提交是／否四组：原题 ID、一次事件、训练前 Weight；有无表现订阅者的返回结果、完整内存状态（包含 RNG）与钱包一致。
- 教学事件一次、原 LessonTaught 成功合同不变、predictionYes 与 submittedYes 的反向案例、旧卡重复拒绝。
- family/law/shutdown/manual 空本地路径；家庭意见不变成准确率结果；law 不加样本；超时已提交仍一次事件。
- null host、无效 desk、过时显示预测零事件／零额外状态改变；单独预测／显示查询不广播结算。

已用 Unity 安装内的 Roslyn 独立编译真实 `LingGuangV05.XingGuang.Core` 源列表，退出 0；独立 checks 编译退出 0。产物仅在 `LocalVerification/DesignPass/AnswerTraceCompile`，未替换 Library。

按主聊天追加要求，随后用 .NET 8 独立 runner 实际运行同一份 checks，加载上一步真实纯核心程序集与现有 LingGuangV05.Core；退出 0，输出 `ANSWER_TRACE_CORE_CHECKS_PASS`，证据 `runner-result.log`。没有启动 Unity，也没有模拟／替换 Board、Predict、Answer、训练或钱包规则。只有测试自身的日志与内存 JSON 克隆在 `ANSWER_TRACE_STANDALONE` 条件下适配到 Console 和 System.Text.Json（仅公开可序列化字段，排除 NonSerialized）；因此该运行不是 Unity JsonUtility 或真实玩家存档序列化验收。

静态工具准备曾选错 Assembly-CSharp 响应文件（核心实际有独立 asmdef），及把 runtime native dll 混作编译引用；两项都在独立验证目录纠正，最终核心/检查/runner 均通过。失败日志保留，未修改生产来规避工具配置问题。

尚待主聊天在 Unity 正式执行 Run，接入壁纸并验证 0.4 秒真实帧、可见性／换 Sim／销毁订阅、颜色与路径截图。此交付只证明核心规则和事件检查通过，不宣称 UI 或全条目完成。
