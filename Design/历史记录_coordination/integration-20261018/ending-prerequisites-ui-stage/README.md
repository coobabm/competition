# E-C2：发布前置与上线夜 UI 候选

状态：源码候选完成，尚未挂入正式桌面。未运行 Unity、.NET、模型推理或完整通关。root 负责单进程编译、接线与真实 UI 验收。冻结 E-C 来信包未改动。

## 范围

- `XingGuang/Core/EndingPrerequisitesUiContract.cs`（命名空间 `LingGuangV05.XingGuang`）：无Unity依赖的显示 DTO、动作协议和单次在途会话。含实际 runtime.Sim 身份、实际 XgState 身份、revision；拒绝旧档、隐藏、重复回调，保存失败保留原快照与选择，命令列表/快照深复制，可选内容 null 安全。纯协议位于Core asmdef，NUnit测试不引用Assembly-CSharp；三个Unity面板保持Desktop目录。
- `EndingPreparationPanel.cs`：SI 阅读、路线选择、真实记忆原句/日期/来源跳转、固定三槽、永久钉卡确认、公开字段预览和正式提交确认。关机卡占一槽；不计算关机卡面或 AI 决策。
- `EndingTrialPanel.cs`：真实六份卷面与三关入口。UI 只展示核心已保存 pendingTrial 投影；逐小题选择、Mark 题 0/1、原文查看、照片解析/原照片说明、交卷后原结果。末页最多 40 字，核心保存后显式打开 SI；真实记忆发现也有玩家按钮。题目、选项、旁注、评分和 AI 末页句均来自 E-D 原内容，UI 不生成题面、不评分。
- `EndingLaunchNightPanel.cs`：左队列（最多显示前八条）、中间单条问答和来源原因、右电表/温度/已答，开始/限流/暂停/拔电源/继续。无替 AI 回答入口。每次 Advance ≤1 秒，通常每 0.5 秒一次，仅页面可见且显式开始/恢复后运行。保存失败冻结本地时钟，隐藏不累计补时。reduceFx 显示完整回答，没有粒子、晃动、模糊或逐字动画。
- 十个 NUnit 纯协议测试已准备，未执行。它们用显式假持久化回调，不能作为实际存盘或真实 UI 验收证据。
- `legacy-rejection-gate.patch`：三个单行共享修改，永久退役旧退稿 beat 的边界防御。详见审计。

## 接入约定

1. 在正式结束入口创建一个空 RectTransform（建议至少 800×600），在同一对象添加 `EndingTrialPanel`，调用 `Build(area, existingChineseFont, approvedPhotoResolver)`；照片解析器只接受原题 photoId 对应的既有资源，缺失时显示原 photoDescription，不加载远程图。过渡到信/发布时同样挂 `EndingPreparationPanel`，在开夜入口挂 `EndingLaunchNightPanel`。使用既有桌面 Canvas/EventSystem/LCD 输入桥，不另开 Canvas。关闭或最小化时禁用此根对象。
2. 调用 `Bind(snapshot, callbacks)`。`owner` 必须是当前 `runtime.Sim`，`stateOwner` 是其当前真实 XgState；不能传预览存档。`IsCurrent` 同时检查两项引用和版本。正式入口不得提供测试快照。
3. `Execute(request, done)` 逐项映射核心动作。只在原有 AtomicSaveStore 成功且核心状态稳定后调用 `done(success=true,snapshot=新完整快照)`；失败由核心先回滚，再回 `success=false,error=双语可读原因`。例如缺少五条真实 AI 回复与实际保存失败要明确区分，不直接把内部 code 显示给玩家。UI 不直接调用 SaveStore、chapterComplete、标题路由或模型服务。必须由适配器再次检查动作对应的核心前置，不能相信显示布尔值或任意客户端 IDs。
4. 委托在 Unity 主线程完成。读取中的页面只通过新的 Bind 更新，运行期间不要以定时 Bind 重置正在等待的事务。成功回调已更新 UI；只有存档切换/显式打开/外部状态改变才重绑。隐藏事务可能仍在核心完成；重开时必须重新捕获核心快照，不能继续使用隐藏前快照。
5. `OpenSource(owner,stateOwner,revision,id)` 映射既有答题记录/YY 来源导航；`OpenPrerequisites` 回真实三关页，`OpenPreparation` 转 SI/发布页。`FilterText` 与 `FilterReviewedText` 都必须配置；缺失时正式动作不执行。本仓库没有原文指定的 `ContentFilter` / `LlmContentGuard` 类型，D014 新增独立 `XgEndingTextGuard` 候选，见同级 ending-text-guard-stage。动态文本委托使用其 Filter；`EndingUiText.reviewed=true` 只能来自已审稿资源或动态片段已预先清洗的原卷投影，使用 FilterReviewed，保留原题判断意义和2017史料。UI 不再额外调用2016词库，否则会破坏分流。所有 TMP 禁用 richText。

| UI action | 核心适配 |
| --- | --- |
| BeginTrial | E-D BeginTrial(token, request.question 0..5)，持久化后投影原 sheet；菜单不提供 fake sheet |
| SubmitTrial | E-D SubmitTrial(token, request.attemptId, request.picks)；Choice 原选项下标，Mark 0=不标记/1=标记错误。UI 不带答案键或星数阈值 |
| SubmitFinalPage | E-D SubmitFinalPage(token, request.value)，原评分三关全过才开；失败保留输入草稿 |
| DiscoverMemories | E-D DiscoverMemories(token)，只由玩家按钮发起真实记录扫描，UI 不合成卡或来源 |
| ConfirmSiRead | E-D ConfirmSiLetterRead(token)，仅 UI 实际展示并到达末尾后可点 |
| ConfirmMemoryReview | E-D ConfirmMemoryReview(token, viewed IDs)，核心核对所需卡集合 |
| ChooseRoute / AskAi | E-D ChooseRoute / AskAiToChoose；路线必须持久锁定后回包；AI 两条引用与卡面由核心给出 |
| PinThree | E-D PinThree(token, exactly 3 IDs)，核心核对唯一性/来源/关机槽，保存后 locks |
| CommitRelease | E-A 发布拥有者；重新验证 E-D BuildReleasePrerequisites、route 与 request.previewId。R4 只记录本机选择，无公开产物 |
| StartNight | E-D night.Begin(token)，绑定实际上线回执；canStart 不从路线猜测 |
| AdvanceNight / PauseNight / ThrottleNight / PullPower | 对应 E-D night 操作；UI 不计算概率、温度、电费、结束判定或关机台词 |
| Continue | 已发布的预览页显式进入相应后续；夜晚页要求 night.canContinue。核心按当前阶段核验；不能仅看见末帧就通关 |

`EndingPublicPreview` 只含产品名、说明、能力、限制、维护/许可，以及内部预览 ID/verified。适配器必须在生成它时只读取允许公开的值，并审查这些字符串本身；DTO 没有玩家姓名、YY、邮箱、聊天、记忆、硬件字段，但这不是对字段内容的个人信息检测器。公开页面不会遍历整个存档。local_only 完全不渲染公开字段。

夜间 `powerWh/temperatureC/answered/total/question/answer/reason/stopLine/queue` 全部由核心提供。`total` 应为真实资源池的 40，UI 不写死或伪造。日期字段是核心确定的显示日期。来源原因必须真实绑定记忆来源；红色原因同时有“被带偏”文本，不靠颜色传达。

## 退稿边界审计与对冻结 E-C README 的更正

正常路径已经停用旧链：`XgStoryBridge.LegacyEndingBeats` 包含 `ending_reject/ending_card/ending_title_yes/ending_title_no`；ApplyMigration 与 RecoverPendingSignals 均标记为 fired。OnExperiment 发送数字序号，不发送 completed；transformer 旧信号与旧 ending 广播也已停用。在针对 Story 和桌面目录的搜索中未找到新的 `lg.project:completed` 正常发射源。

因此不能把旧通知表述为已复现的正常流程缺陷。风险只在外部显式旧信号先于桥的首帧迁移时：StoryDirector 的执行顺序为 -1400，桥为普通 Update，StoryRunner 对已经开始/排队的 beat 不再检查 fired，开始的 Title 后仍可能走 notify。这是静态边界分析，未声称已在 Unity 复现。

root 明确选择永久退役语义。本候选登记 `labLegacyEndingEnabled`；未绑定 LabFacts 时沿用已知 lab 变量的 0，已绑定桥也固定返回 0；`ending_reject.when` 使用该条件。原 `six_reject_notice` 文本和原迁移逻辑不改。将来真正投稿后退稿应另建新 beat 和真实投稿凭据，不打开这个门。没有新增伪投稿字段。已有 Mail 的真实投稿检查仍是独立防御。

## 原文 §9 对照

已读当前 `XgSim.Finale.cs`（六题、SI 信、旧五卡与委托条件）、`XgFinalePage.cs`、`XgSim.AnswerTrace.cs`、`stage_quiz.json`、EraLexicon 与 Story 调度相关文件。当前资产目录未找到原文点名的 `XgSim.Trial.cs/XgTrialPanel.cs/XgTrialFile.cs/final_trial.json/LaoZhouInsight.cs/shutdown_hints.json/YYChatView.Filter.cs`；实际 LlmSeat 枚举为 LingGuang/Girlfriend/Others，未有 LaoZhou 枚举项。没有为缺失原文件仿造同名假实现。原三轴规则、三红队关卡、真实记忆生成、40 请求池与关机公式由 E-D 负责，此 UI 不复制或改变它们。原 stage3–5 玩法文件没有修改。

## root 验收清单（尚未执行）

1. 编译新文件及旧 Story 的严格 JSON 验证；运行 `EndingPrerequisitesUiTests` 十例。实际三关六题按原卷顺序作答，核对交卷得分与 Mark 扣错标规则，失败重做保留原核心提示；照片/原文可读，末页保存再开 SI。
2. 实际 LCD 输入：红队未齐禁读 SI；SI 长文滚到底才可确认；真实记忆来源打开正确记录；三槽包含关机卡；第四张不可加；永久确认后不可改；AI 两条真引用完整可见。
3. 三路线正式预览、字段隐私、R4 无发布；改档/重复点击/异步迟到回调/真实保存故障不跨档不提前发布；读回持久状态再开面板。
4. 核心完整 40 请求自然推进；同一时刻只有一条回答；限流/暂停/三种拔电源路径符合核心；source 原因；reduceFx 静态可读；隐藏→重开不自动补时；实际失败→继续按钮重试。
5. 核对低分辨率布局、中文字体、长记忆/长问题溢出、长英文翻译、键盘焦点与滚动。此候选未取得真实截图或 UI pass，不把静态检查称为可玩验收。

`delivery.patch` 为新文件 + 三个共享门禁 hunk；也提供 `new-ui-files.patch` 和单独 gate patch。普通 `git apply --check` 已通过一次；最终哈希见 manifest/READY。生产文件未被本包覆盖。
