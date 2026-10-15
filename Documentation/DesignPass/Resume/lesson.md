# 第 13 行恢复开发：首分钟教学

日期：2026-10-13。只读取本条已有整合记录及 lesson-017 场景合同，未读取下一条策划。本轮是实现及验收入口交付，不能据此勾选第 13 行完成。

## 查到并修复的实际缺口

- `XgTeachCompanion.Update` 原先首次正确后就显示“教它→训练→和它聊”，4 个样本后把训练列为主目标；`XgLabelPage.RefreshSteps` 同时持续催促训练。首分钟内现在持续说明如何评价显示的猜测，样本只呈现已有数量。训练、聊天、脑图按钮保留，训练达到原条件即可点击，只在这一分钟使用普通按钮色。
- 原 `OpeningSequence` 只排序月卡、AI 加入 YY、晴雯等，未保证首课一分钟。新增 `XgFirstLessonFocus` / `XgFirstLessonWindow`：只由真实 setup 首次回答完成时武装，直到教学卡实际呈现且窗口前台、按钮可用的后一帧才开始计时。60 秒按 unscaledTime 计算；换卡、刷新、四张快速回答、主动训练及重复武装同一实例不会重置；更换模拟实例时清理。内存状态，不读写玩家档，不暂停电费或模拟时钟，不锁导航。
- 主聊天扩展授权后，只在已证明的入口接入：`OpeningSequence.Turn`、`AiJoinsYy`、`StoryDesktopPresenter.ExternalBusy` / `NotificationsHeld`、`YYGirlfriend.HerTurn`。外来演出和通知延后；YY 的 impatience 与无演出降级等待在保护期内不累积，避免一分钟结束立刻绕过月卡顺序。未改变普通 `OpeningQuiet` 规则、剧情内容，未恢复老周知情剧情。实际 60 秒后完整释放顺序仍须场景观察。
- 教学反馈原位只有 34 高，英文误判说明叠加解释与出处可超过数行，而且下一卡立即出现。教学反馈明确以“上一题 / Last card”开头，保持两行：判定结果及正确答案、实际扣款/连击结算；纠正不再追加重复的“纠错”尾巴。36 号截图复核失败后继续改为独立 50 高浅色底板、深色加粗字、13–16 字号以及 5 秒停留；只改教学结算，非教学反馈沿用原样。宽窄中英仍需实际字体/截图检查。
- `XgLessonAnswerInput` 增加取消与应用失焦清理已捕获输入；原卡捕获、消费一次及呈现帧保护继续保留。
- 主动聊天边界确实原先失败：`XgYyTalk.Open()` 在首次自动加入之前只能回到 home。现在 `Open(playerRequested: true)` 只由教学伴随“和它说句话”按钮显式调用，在首课已武装但未呈现、或正在计时期间，玩家均可直接进入现有 YY 联系人。创建一次已加入状态并使用原 Select / router，不新增剧情或首次问候、不缩短保护。其他自动调用默认 false。`AiJoinsYy.Update` 原有已加入检查会清理待演出请求，防止重复首次演出。此入口不发送任何聊天消息。

## 已执行验证

1. `Documentation/DesignPass/Resume/focus-checks/FocusChecks.csproj` 实际运行退出 0：`FIRST_MINUTE_FOCUS_CHECKS_PASS assertions=11`。直接编译生产 focus 文件，仅用空 `XgSim` 身份类型替身检查时钟/实例隔离，不替代真实模拟器或 Unity 场景。
2. 使用当前 Unity 6000.4.5 的 Roslyn 与 Bee 响应文件，独立编译完整 `Assembly-CSharp` 源列表退出 0，再以新输出 ref 编译完整 Editor 列表及 FirstMinute 候选退出 0。输出仅在 `LocalVerification/DesignPass/ResumeLessonCompile/`，未替换 Library 程序集；日志为 `runtime.log`、`editor.log`。这不是 Unity 导入或运行通过。
3. 已跟踪生产差异的 `git -c core.whitespace=cr-at-eol diff --check` 通过。新增 source/meta 与测试候选已完整写入。
4. `DesignFirstLessonChecks` 增补真实 XgSim 身份下首分钟生命周期及 input cancel/focus 断言；新增检查由原 Run 入口调用。主聊天此前通过的是增补前版本；本轮新增断言待主聊天重新运行。

## 给主聊天的真实场景入口

`Assets/LocalBuild/Editor/DesignWallpaperSmoke.FirstMinute.cs` 是独立 partial；主文件不在本组修改范围。完整候选保留在本目录，随后一次性复制入 Assets；复制时 SHA256 为 `E56DB1A4E19997B1EFE851E9DB9B161209A559BDD9B02C7295C0F107C651631C`。

接口 `RunFirstMinute()` 与 `TickFirstMinute(controller, runtime, brain)`。主入口完成原隔离 case 0 后直接设 `step=200`，不要经过会直接 `CompletePrologue` 的 case 11。partial 由协程推进，处理异常和结束清理。

验收从已隔离且位于 setup 之前的内存新局开始：关闭该场景模型服务以明确走离线行为；恢复 `TestMode=false`；准备安装完成后的真实 setup 界面，输入字段并通过现有 `Click` 桌面 inputSurface 中继点击确认与首次按钮。StoryDirector 在 setup 前恢复；PrologueDirector 在 setup 完成后恢复，以避免重放安装前整段剧情。首卡出现之后保留正常剧情/通知调度，逐帧观察到保护自然结束，真实点击教学按钮四次并检查每次只有一个学习事件；期间主动打开训练、AI 聊天并回到教学页，验证不必等 60 秒、没有重复问候。finally 恢复测试接管前组件与 TestMode 状态，清理观察订阅。

此场景是“真实 setup 至首分钟”，不是完整自然序章启动。日志仍把以下部分留待验收：

- `first-minute-before-answer`、每十秒观察图、`first-minute-finished`；自然前四卡若有真实纠正，保存 `first-minute-corrected`，没有则日志明确要求额外 B 场景，不能硬按猜错冒充纠正。
- 逐帧检测剧情是否运行以及演出是否打断；每十秒记录时间、页签、学习与纠正次数。首次卡、后续卡和 UI 判断走真实按钮，不调用核心回答 API。
- 该入口不覆盖完全不答题的独立 60 秒 C 场景，不覆盖 1280×720/1920×1080 中英全部组合，不证明壁纸从默认窗口后可见、不替代主聊天壁纸脉冲像素检查，也不证明玩家实际阅读体验。截图需要人工查看。
- 用户主动聊天后的其余主页/框架聊天入口没有改动；首课主动解锁入口严格限制为本条教学伴随按钮。默认 Open 路径仍须联系人已加入，避免自动 OriginCurtain 等调用绕过首课保护。

## 剩余真实验收

按 `Parallel100/lesson-017/scene-acceptance.md` 执行 A、B、C、D。尤其记录新局实际 t0、默认窗口是否遮挡壁纸、纠正首个可渲染帧与回落、误判无成功闪、窄窗英文两行可读性、当前主体操作仍为判断猜测、首课保护期间训练/聊天实际可达、换档不会遗留保护、60 秒释放后没有同时弹出所有外来内容。主聊天统一 Unity 和截图；本组没有启动 Unity、访问真实玩家存档、打包、提交或推送。

## 候选补充（主聊天冻结后，未复制入 Assets）

按主聊天要求，只在 `DesignWallpaperSmoke.FirstMinute.candidate.cs` 补足后续场景。这个候选已用当前完整 Editor 源列表独立编译退出 0，日志 `LocalVerification/DesignPass/ResumeLessonCompile/candidate-editor.log`；编译列表排除已部署 FirstMinute.cs 再加入候选，避免重复类型方法。主聊天通知前不覆盖正在验收的 Assets 版本。

- `RunFirstMinuteIdle()`：独立隔离新局，完整 60 秒不作答、不导航，断言仍未结算任何手工答案。通过 SessionState 区分场景，主调度仍使用 `first-minute` 模式，无需新增主 switch 分支。
- `RunFirstMinuteCorrection()`：完成真实 setup、尚未学习任何卡时只替换 SMS 输入（seed 9137，truth=true，固定中奖短信）；现场验证生产模型确实猜 false，再走真实“猜错了”按钮，检查学习事件与壁纸即时脉冲，并保存纠正截图。这是确定性 UI 场景，不记为自然首卡。
- `RunFirstMinutePendingChat()`：真实 setup 的 Light 协程刚结束时读取 focus 的 presentedAt，必须仍为 NaN；显式调用用户聊天动作 `Open(playerRequested:true)`，进入真正 YY 输入框、填写一句话并通过真实 Send 按钮发送，等待生产离线回复。回复到达后再次核对保护仍待首卡呈现，再返回教学页开始完整一分钟。这个时序断言若在实际执行顺序下不成立会 FAIL，不伪造“未呈现”状态；它是主动 UI 动作 API 的边界探针，不冒充玩家点击不可见按钮。
- 普通四卡场景也增加真正 YY 输入/Send/离线回信检查，截图 `first-minute-offline-chat`，确认建立联系人以后确实可以交谈。只给游戏内虚构 AI 发送本地测试消息，无外部模型服务，不发送第三方消息。

这些补充目前仅编译通过，Unity 与画面状态仍为 NOT_RUN。

## 36 号真实截图失败后的修复（仍待统一复测）

主聊天的 `36-first-minute` 自然四卡逻辑检查 PASS，但原图视觉 FAIL，不能据此称首分钟通过。本组亲自查看 `WallpaperSmoke/first-minute-corrected-full.png`，确认下方绿色 toast 仍在催促“攒够 4 条样本，可以训练了”，上一题绿字也在全页半透明合成下难以阅读。

已定位并按新增精确授权修复两个遗漏入口：

1. 绿色 toast 来自 `XgSim.AddLabel` 的既有 Message，经 `XingGuangView.OnMessage` 立即展示。核心样本、解锁和消息不变；OnMessage 只在首分钟教学页识别这条中英训练解锁消息，保留原事件 feed，略过自动 toast。玩家主动进入训练页时正常展示训练消息。
2. 右上待办来自 `GuideNote.Update` → `XgGuide.Current`，并非教学伴随的文案。首分钟且教学页可见时只呈现可点击返回教学页的“它先猜，你判对错”；切进训练等主动探索页面仍用原目标生成。正在过渡的旧收样本/训练目标不会覆盖新的首课提示。未改 `XgGuide` 核心策略或 `XingGuangView.Frame.cs`。

教学反馈新增的浅底板只覆盖原反馈区域；InfoBox 下移 16，保证两行提示不重叠。成功首行写“上一题：你纠正了它 / 它答对了”，次行显示原卡正确答案和奖励；误判继续明确上一题，并保留真值与实际罚款。源色对比至少 7:1 的检查不冒充最终屏幕对比，最终全页透明合成由主聊天另行定位，本组没有强制整个页面不透明。

候选验收补漏：扫描当前教学窗口全部活动 TMP（含 toast），另外扫描 Guide Note，拒绝训练解锁/下一步训练等自动目标；刚结算与 0.6 秒后重复检查实际反馈有独立底板、明确上一题、下一卡 ID 已变化、仍显示原卡真值、实际 TMP 两行且字号≥13、每个可见字形在反馈矩形内、前后景源色对比≥7，并记录 inheritedAlpha。额外截图 `first-minute-corrected-readable-full.png` 留人工复核。

本批生产与候选 Editor 已重新独立编译退出 0，日志路径同上；`diff --check` 通过。候选 SHA256 `804677B671BC27685A76DAB96FD5F9B9EB228F6D8410A13AB7CB8683281EDF30`，仍未覆盖正在跑 37 动效验收的 Assets 测试；待主聊天部署和复测。`XgSim.Teaching.cs` 由主聊天处理答题光路，本组不编辑。

## 英文短验收补漏与 50 号结果

47 号英语验收暴露两个遗漏：伴随 chat/brain 按钮只在 Build 时翻译；最后异步全屏截图尚未写盘就结束 Editor。已在 XgTeachCompanion 原有 Update 的刷新节奏内仅更新两处 label.text，保持按钮 enabled/interactable、背景色和文字色。FirstMinute 英语场景增加三个 companion 按钮的真实英文文本、字形边界和 chat/brain 可用断言，并在最后截图后等待下一帧及完整 PNG 文件头/尾，最多 10 秒，再确认前两张英语全屏截图也完整。

两个候选均用完整 runtime / Editor 源列表独立编译通过，在主代理确认 49 退出后核对基线哈希并一次性部署。主代理报告 50 全 PASS，亲自查看最终 full PNG，确认 chat/brain 英文正确、上一题反馈清晰。本组未启动 Unity。部署后 FirstMinute SHA256 为 `5C99776F76EC7C8681247D992011C07EEB19BEC09A55006157C0A921CEC0813D`，XgTeachCompanion 为 `82813CB86A1021C813189DCEBCDA91F91C17ECDFE3D69E761D3139CB0A50E31E`。

UserManager 桌面淡入/淡出只读审查与独立回归候选另见 `desktop-fade-review.md`；该候选编译通过但 Unity NOT_RUN，目前留在 LocalVerification 并冻结，未接入或覆盖 FirstMinute，未修改生产或主框架。后续由主代理统一选择串行接入和画面验收。

## 53 号桌面动画回归完成

主代理报告 53 DesktopFade 全 PASS 且已退出：淡入/淡出终态 alpha 与输入 flags、重复调用、同帧/跨帧反向、暂停恢复和慢速动画均正确。该结果确认了主代理在 UserManager 中等待请求后的实际动画采样并核对 In/Out 状态的修复；本组只提供诊断和回归，没有运行 Unity。3:014 的 Assets 外候选准备恢复，但默认桌改动不与 Growth/Wiring 同批落地。
