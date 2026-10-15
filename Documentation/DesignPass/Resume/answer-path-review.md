# 1:082 答题路径只读审计

日期：2026-10-13（Asia/Singapore）。仅审计当前开启条目及其 A7 引用：答题后显示输入到输出的已用格子路径，持续 0.4 秒，不以对错决定是否出现，颜色表达是／否。不读取其他策划条目，不修改生产、玩法或壁纸。以下是供主聊天实施的方案，尚未实现或运行。

## 结论与最小接入

应在现有 `Board.Predict → Score` 的同一次实际计算中采集值快照，保存到当前 Answer 的局部变量；在这道题结算完成后发一个只用于表现的答题事件。不要订阅所有 Predict、不要缓存全局 LastPrediction、不要在事件里重读 Card(desk)，也不要在训练后重新计算 Matches 来假装它们是刚才用过的格子。

建议最小改动面是 `XgBoard.cs` 的可选采集参数、`XgSim.Teaching.cs` 的兼容重载、`XgSim.Answer` 的局部快照及结算事件。若本次“每答一题”包含当前页面可走的全部手动入口，再在 `AnswerFamily` 和 `AnswerQueued` 的成功结算出口补同一事件，给出明确的无本地概念路径。壁纸实现仍由主聊天独占。

1. 保留旧 `Predict(card, knobs, out guessed)` 接口和所有结果；增加能返回不可变 path snapshot 的重载。两者走同一份原 Elements、Gate、Matches、Score 算法；无观察者的旧入口不必创建路径集合。
2. Score 的现有循环加可选 collector；只在实际执行的读出分支中复制概念标识和值，不另跑一份筛选算法、不改变遍历顺序或浮点加法顺序。采集不更新权重、seen、trainedCards、links、board.cards 或任何 RNG。
3. 让 Answer 现有的 `TeachingGuess(card, out taughtGuess)` 那一次调用接收快照；保留其现有初始化行为和调用顺序。不要为了快照额外调用 TeachingGuess（该方法会初始化 checkups），也不要为了“只读”改变已有初始化逻辑。普通 UI 查询及检验题预测继续用旧重载。
4. Answer 中把原 card 的 id/dataset、提交的 yes、预测结果、快照存为局部值。原先 TeachBoard、收支、连击、NewCard、ObserveGhost 顺序全部保留；在正常结算出口同步发一次事件。不得用 result.correct 或 result.accepted 作总开关：普通 Answer 当前没有设置 accepted，答错／超时仍是一次真实提交。
5. Family 的提前返回和队列 AnswerQueued 是独立出口。若纳入当前范围，在各自结算成功后各发一次；TryEvaluateTeachingGuess 包装层不再重复发答题事件，保留已有 LessonTaught 原合同。

## 为什么必须采集实际 Score 分支

依据：`XgBoard.cs:400` 的 Matches、`:449` 的 Score、`:516` 的 Predict。

- Matches 是候选匹配，不等同全部被使用。Score 先找第一个 `pinned && abs(w)>1e-3` 的命中并立即返回 `w*100`。这个分支只有该固定规则实际决定读出；不能点亮其他 pinned 命中或普通候选。
- 普通 Score 跳过 `abs(w)<=1e-3`，之后计算 `w*act` 及可能的跨层 relay/garble，再累计。应在此处采集，保留原顺序。不能先按 layer/id 排序后再计算；排序仅可发生在已经复制好的表现数据上。
- 已计算但最后 vote 为零的项与被阈值直接跳过的项不同。若字段叫“used/readout”，可保存实际执行的该项及 vote；若渲染只标非零贡献，应在表现中依据已记录的 vote 选择，不能据此改 guessed。极小 act、正负抵消、零总分也不能伪造为未预测。
- Gate、特征工程、位置／可达性决定实际 Elements 和匹配，直接从原始 question 字符串找关键词会错。Dropout 在训练阶段使用，不能给 Predict 路径另加训练 dropout 过滤。
- guessed 是 Score 的 topLayer 判定。无有效权重时 Predict 用 `(seed & 1)==0` 给出确定性猜测，概念集合应为空；不要点亮全区域作为占位。
- `XgLink` 表示训练生成的关联强度，不是这次 Predict 遍历的因果边。当前读出并没有按链接做逐层图遍历，不能仅因两个概念有 XgLink 就宣称那条边被这次推理使用。最小诚实表现是输入 → 本次读出的概念 → 输出；若画原关联线，只能当作已有结构背景，不标作实际推理证据。

## 快照与事件数据合同

建议至少区分以下值：

| 数据 | 用途与限制 |
| --- | --- |
| cardId、dataset、region | 来自原题的复制值。普通 XgAnswer.cardId 当前未赋值，不能拿它代替原 card.id。 |
| submittedYes | Answer 收到的最终标签；family 中 true 的业务意思是 junk。不是正确性。 |
| hasPrediction、predictionYes、guessed | 模型实际预测与猜测标记。无本地预测时不伪造。 |
| source/reason | 例如 local-board、manual-no-prediction、family-opinion、law、queue-external、shutdown。只用于区分可证明的路径来源。 |
| used concept records | 复制 id/region/layer；要定位合并或替代键时再复制 key/alt、选中的 element indices 或 element ids。需要诊断才附 weight/act/vote/pinned。不得保留可变 XgConcept/Match/List 引用。 |
| outcome（可选） | XgAnswer 是值类型，可复制用于诊断；表现不能因 correct=false 就取消路径，也不能把 subjective 当答错。 |

最小数据可只保留真正使用的概念 ID 与预测输出；若结算中的 TeachBoard 可能挤掉旧格子，为了事件后仍能解释这次输入，应同时保留 region/layer 等不依赖活对象的定位信息。展示时若原 ID 已不在壁纸布局中，允许缺席或用快照布局；不能把新占位格或训练后新增格冒认成刚才用到的旧格。

集合应私有复制后以只读视图暴露，或用只读值记录配独立数组并不向回调暴露可写底层存储。core 事件不依赖 Unity、时间或颜色；0.4 秒及是／否颜色由壁纸按接收事件的 unscaledTime 控制。

特别注意教学语义：`TryEvaluateTeachingGuess` (`XgSim.Teaching.cs:46`) 将“它答对／它答错”转换成 `guessWasRight ? shownGuess : !shownGuess` 后传入 Answer。后者可能与模型真正输出相反。因此快照必须保留 predictionYes 与 submittedYes，不能只存一个含糊的 yes。若路径代表模型的这次推理输出，应按 predictionYes 上色；无本地模型路径、仅显示人工提交时才用 submittedYes。始终不得用 correct 来决定是／否色。

## 实际入口边界

| 入口 | 当前代码事实 | 建议事件／路径 |
| --- | --- | --- |
| 普通 Answer (`XgSim.cs:1385`) | 先 GhostBeforeAnswer、TeachingGuess；正确且非 law 才 TeachBoard；随后 NewCard。 | 结算一次事件，不论 correct；捕获现有 TeachingGuess 的训练前路径。 |
| TryEvaluateTeachingGuess | 校验 cardId 和当前 guess，失败直接 false；成功只调用一次 Answer。LessonTaught 仅正确时触发。 | 复用 Answer 的事件。拒绝过期卡／过期预测时零事件；不要把 LessonTaught 改成“每答一题”。 |
| family (`XgSim.Family.cs:23`) | Answer 早返；记录个人态度和收入，不预测、不训练、不计对错／准确率。 | 若属于当前全部手动题范围，发一次 opinion 事件、空概念路径。不能为做动画额外 Board.Predict 或训练。 |
| law | TeachingGuess 明确排除 law；正确只领钱，无样本、无 TeachBoard。 | 发一次真实提交事件，但本地概念路径为空。非教学 Ghost 可能用旧准确率公式，也不是 Board 推理证据。 |
| shutdown | TeachingGuess 排除；Answer 按提交值改 truth，后续可能训练 seed。 | 不能把事后训练过的 seed 当作答前预测使用格子。空推理路径，保留选择。 |
| AnswerQueued (`XgSim.Collaboration.cs:419`) | 从队列删除一次后结算；用预存判断或人工标签，无 Board.Predict/TeachBoard。 | 若覆盖页面全部手动入口，在原出口发一次 queue 事件，空本地路径。重复 cardId 拒绝后零事件。 |
| Route／AutoRouted | 使用已完成外部／本地判断结果并发自动处理事件；有 offline simulation 路径。 | 不能把后台路由或离线补算伪装成当前手动答题。若后续要显示自动推理，须在其实际判断来源采集并传 ticket/cardId，属于额外范围。 |
| Accuracy、AddCheckup、诊断、Train 内 Score | 都会频繁调用预测／评分，但不是当前一次玩家提交。 | 不在 Board 层广播表现事件，不产生答题闪光。 |
| ChatLabel、Captcha、自动 TickCard 超时 | 未提供同一 Board 的实际答题路径；后者没有本次提交 yes。 | 当前方案不捏造概念／答案；是否纳入其他类型任务应另定范围。 |

非教学 GhostFor 使用准确率与 card.roll 的公式，并非 Board.Predict；TeachMode=false 时不能仅为了让格子亮而临时算一遍 Board 再称其为实际使用。`Board` getter 还可能创建 S.board 及 rebuild 索引，空路径分支不应为了动画去访问它。

## 关键验证边界

1. 同输入下旧 Predict 与带快照 Predict 的 answer/guessed/score 一致；预测前后 Board 全部概念字段、计数、链接及 RNG 状态不变。覆盖 wiring/Gate/特征工程/relay，采集与不采集不得改变浮点求和顺序。
2. 固定规则命中：多个 pinned 与普通概念同时匹配，snapshot 只有原循环实际选择的第一条固定规则；普通分支包含真实读出的正／负项，排除阈值跳过项。空概念、抵消为零、极小 act 保留原 guessed/输出。
3. 正确／错误 × 是／否四组都发一次，事件颜色字段仍是回答输出，不是正确性。教学“它答错”能验证 submittedYes 与 predictionYes 相反时无混淆。
4. 正确提交会 Train 并 NewCard：事件 cardId 必须是原题；snapshot 不受随后权重变化、新增、合并、挤出、重新初始化影响。不允许事件里调用 Card(desk) 把新题混进来。
5. stale cardId、stale shownGuess、host null、无效 desk、重复 queue id 均无事件／无额外状态改变；family/law/shutdown/queue 保留原付款、样本、态度、连击等结果，不为路径引入预测或训练。
6. 训练、检验、hover/Refresh 的 TeachingGuess、每帧 UI 查询均不能触发答题路径；同一教学操作不会因 Answer + TryEvaluateTeachingGuess 重复闪两次。
7. 真实帧在 timeScale=0 下仍只持续 0.4 秒；没有按 .05 capped 动画 delta 延长它；同帧／连续答题按明确策略替换或并行，不累积不灭。窗口隐藏、组件禁用、销毁、换 Sim 应清空并解除订阅，旧 Sim 事件不能污染新存档，重开不重播。
8. 视觉确认只亮已采集格子／必要输入输出；错误答案也有等长路径，颜色表达是／否；不能拿先前 .65 秒的“正确教学后整区 glow”截图当本项通过。原 LessonTaught 的独立学习反馈如保留，应避免掩盖新路径的定位和 0.4 秒边界。

本次只写此审计文件；未改源码、未启动 Unity、未改玩法或存档，未给本条运行/视觉 PASS。

## 2026-10-13 纠正回程与容量只读追加

审查版本：`XgWallpaperAnswerPath.cs` SHA256 `2E631CD94ECA78B6E1A9706A70E20DC1AB727C2B1BB01021463178F174B27677`；`DesignWallpaperSmoke.AnswerPathEdges.cs` SHA256 `520B48A7E7B88CA1594E957AACA338EADF3C365C7428730E4386D7CC4EAB1566`。本次只读未发现确认的呈现、清理或容量缺陷，不扩读下一条策划。

生产回程使用不可变 R1/R2 写入快照，Before/After 位于 R4 衰减之前，仅转换为临时色调，不回写板状态，也不声称是最终净值。回程跳过普通预测中间点的光圈，逐一按 pull.Id 重新定位；R2 新概念即便不在预测路径中仍可显示，LOD 缺失则跳过。禁用、销毁、换源、不可见与自然到期均清理并标记重绘；最多四条路径。

按当前 XgDraw 顶点生成方式，四条 512-cell Reduced 正向路径为 53424 顶点；即将额外 seed 位置计入 513 个映射，也仅 53528。纠正回程全光圈的保守上界仍不足 45500。新 Edges 用真实 VertexHelper.FillMesh 覆盖容量，finally 恢复原路径引用、位置映射与 Reduced 并销毁测试 Mesh，属于合理的同步合成几何证据，不是自然帧或玩家实际训练证据。

待后续覆盖的测试缺口：当前移动／旧槽替代片段重新建立的路径默认 corrected=false，直接证明的是正向路径身份跟随。尚未直接验证回程 pull 光圈随 ID 移动、遗漏映射，以及 R2 新 ID 不在 path.concepts 时依然显示；现生产循环符合这些要求。容量下界 `vertexCount > count*4` 本身也不能证明所有 changed-cell 光圈均被绘制，因为线段就可能超过此下界。本轮仅记录，未扩大 Assets 改动。

## Run59 前置路径隔离诊断

Run59 场景失败点是原合并断言 `UsedConcepts.Count > 0 && paths.Count == 1`，没有两项计数，因此不能断言其真实原因。只读确认 AnswerPaths 在四次准备教学后仅等待 0.8 秒，现成功纠正的总寿命为 Duration 0.4 + CorrectionDuration 0.6 = 1 秒，存在前置路径尚未结束的确定测试边界不足；Corrections 已等待 1.2 秒，保持不动。

按主聊天授权，仅在 AnswerPaths 检查文件保留原 0.8 秒时间点并增加路径 corrected/age/lifetime 与真实 Board.Predict used/guessed/answerYes 诊断，然后自然逐帧有界等待旧路径归零，最长 2 秒并严格断言。没有清空、改龄或延长生产路径。被测单次 UI 答题后分别严格检查 used>0 与 paths==1，并记录两项计数。诊断输出到运行目录 `answer-path-diagnostics.txt` 与 Unity 日志。源码 SHA256 `F23BC56EC9728555B1CAE50F35965885380D3BCD04F3B7FC9589D11B4511080A`，完整 Editor 独立编译通过，仅既有 obsolete 警告；未启动 Unity，实际归因待主聊天 Run60。
