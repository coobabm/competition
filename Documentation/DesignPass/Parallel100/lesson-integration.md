# 首分钟引导 · 17 项交付整合

日期：2026-10-13（Asia/Singapore）。范围仍为《10.11问题_设计策划.md》第 13 行，没有阅读下一条。17/17 个 `lesson-*/result.md` 已到达并逐项审阅，未到达项：无。

状态：授权文件内的必要修正已落入生产；独立结算规则检查已运行通过。主聊天后续明确扩展本组相关生产文件的局部修改授权后，继续完成了跨文件输入/蜘蛛保护、特殊卡兼容和setup提示。壁纸接入由主聊天处理；完整首分钟调度和统一 Unity/截图验收仍有缺口，**本条未完成**。

## 逐项决定

| 交付 | 决定 | 已落地内容、理由或剩余依赖 |
| --- | --- | --- |
| lesson-001 第一张预测 | 无需生产变更；采纳补充检查 | 真实棋盘已从首卡预测，无训练门槛且不强制猜错。在 `DesignFirstLessonChecks.Run` 补同文本/种子但相反真值仍同预测，以及重复显示不加样本、手工计数或检查点。 |
| lesson-002 评价映射 | 无需变更 | 复核 `guessWasRight ? shownGuess : !shownGuess` 与 `Answer` 成功分支；八种组合已覆盖，保留真实真值与原学习规则。 |
| lesson-003 重复与过期输入 | 已采纳 | 呈现帧保护拒绝当帧未渲染新卡；同卡不变 Refresh 不重置帧。新增 `XgLessonAnswerInput.cs` 与meta，鼠标按下捕获模拟/桌/模式/卡/猜测，释放消费一次旧快照；页面拒绝上下文变化。Button保留视觉/导航但不挂业务回调，组件负责指针/Submit，数字键直接调用 `CaptureAnswer`，没有半接入。增加独立输入组件检查，实际跨帧桌面路由仍待场景验收。 |
| lesson-004 显示预测过期 | 采纳独立检查；无需生产改动 | 将实际 `Board.Train` 翻转预测、旧输入无副作用拒绝、重新获取猜测后恢复作答的检查并入唯一授权 Editor 文件，保留类 `Lesson004PredictionRecoveryChecks`，由 `DesignFirstLessonChecks.Run` 自动调用。尚未在 Unity 运行。 |
| lesson-005 待复核兼容 | 无需变更 | 当前显示语义与当前教学资格不符即拒绝；队列仍走 `AnswerQueued` 直接真值。已有同卡转队列测试保留。 |
| lesson-006 家庭态度 | 已采纳 | 合入两条家庭专用中英文悬停说明，明确偏好无对错/无罚款/不改连击。在独立检查补两种选择×真值×教学开关，分别断言仅 junk 或 important 增量，防止两次相反操作抵消方向错误。核心不变。 |
| lesson-007 法律/关机特殊卡 | 已采纳生成修正及针对性检查 | 扩展授权后重新核对并应用 `XgSim.Stages.cs` 一行条件，已有law/bounty题不再改造成shutdown，保留原特殊题语义。将候选中的生成控制组和特殊卡直接回答检查并入统一Editor入口；不纳入与此缺陷无关的长期棋盘维护检查。未改题目真值、学习规则或历史档；提前返回会改变命中悬赏时的后续随机序列，记录为已知影响。 |
| lesson-008 蜘蛛兼容 | 已采纳 | 在 `XgLabelPage.Spider.cs` 的 blocker、peek、hand press 三处拒绝当前教学评价，提供玩家判断的提示；`XgResidentSpider.cs` 每帧取消已不再允许的随机代答计划。普通/复核/特殊分支仍走原 blocker，不全局禁用教学模式下其他自动化。 |
| lesson-009 数字键 | 已适配采纳并统一入口 | 扩展授权后将 `TickKeys` 白名单直接改为label或teach，数字键调用与003相同的 `CaptureAnswer`，并删去早先临时 `TickTeachingKeys`，无重复监听。保留前台窗口/输入框/验证码/层级保护，`IsInteractable()` 尊重父CanvasGroup；Button不再有业务监听，数字键不再Invoke空回调。 |
| lesson-010 双语评价 | 无需变更 | 当前姓名、猜测、按钮、悬停均分清真值和评价；保留自定义 AI 名称。 |
| lesson-011 玩家判反 | 适配采纳；驳回冗长原文 | 加“你刚才判反了”/“You misjudged it”，保留实际猜对/猜错、题目答案和既有结算说明。未采纳追加 `Choose Guessed right/wrong` 的完整长句：反馈框高度有限，且已换卡，用命令式要求选择易指向新卡；现用过去时锚定刚才的判断。仍须窄窗英文截图核验。 |
| lesson-012 结算 | 无需生产变更；已重跑验证 | 核对92项原依赖在重跑前与交付基线一致，执行原 `run-checks.ps1`，退出0，`LESSON012_MEMORY_CHECKS_PASS assertions=402`。使用真实纯规则源码和内存钱包，未启动 Unity/游戏、未访问玩家档；不把独立 .NET 结果当作 Unity 编译或画面通过。 |
| lesson-013 事件语义 | 无需生产变更；采纳补充断言 | `LessonTaught` 唯一发布点仍在成功 `Answer` 返回后；携带旧卡。补重复旧卡提交后 pulses 数仍不变的直接断言。 |
| lesson-014 壁纸提亮 | 保留交主聊天实施 | 未修改任何壁纸源码。`changes.patch` 及新增 partial 候选在交付目录；必须使用事件原卡脑区，不能读下一卡。主聊天重新比对自己的最新壁纸源码后合并，不直接整体覆盖。 |
| lesson-015 订阅生命周期 | 保留交主聊天实施 | `XgLessonSubscription.cs` 与同步重绑合同留交主聊天，与014统一选定一个订阅所有者，不能叠加两套事件订阅。其 `SubscriptionChecks.cs` 定义模拟同名类型，**不得放入 Assets**。17条独立夹具通过为该交付报告，本轮未重新运行，不表示生产生命周期通过。 |
| lesson-016 布局/焦点 | 已采纳 | 局部教学按钮外框加选中描边，数字键提示改深色，仅作用于教学；不改变点击矩形、导航或全局尺寸。保留中英文和宽窄窗口实际截图待验收，不凭字符数改字号。 |
| lesson-017 真实场景 | 已采纳setup文案及验收脚本 | 扩展授权后重新核对并应用 `XgSetupOverlay.cs` 中英文单行toast，首次进入教学只提示“它先猜，你判对错”，保留原7秒时长。采用 `scene-acceptance.md`、`observations.csv` 为人工验收合同；其他现有剧情、训练进度提示仍可能分散首分钟目标，未加入缺乏场景证据的全局门控，不能声称整整60秒只有一件事。 |

## 核对原源码与哈希

所有生产写入前，下列 SHA256 与交付记录完全一致；修改后按当前方法上下文人工合并，没有用完整候选文件覆盖共享源码。006与016依次检查上下文后才应用。扩展授权到达后，再次复核Spider partial、ResidentSpider、SetupOverlay的哈希仍与原始记录一致；008、017、007各在 `git apply --check --ignore-space-change` 通过后应用。003按当前已含帧保护/焦点的源码手动合并剩余组件和捕获逻辑，009统一其键盘路径，011保留精简过去时文案。

| 目标（工程相对路径） | 整合前 SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Stages.cs | CE64E64CCFB1CC7C24ADAD73C771650F367DAD6E4F55E30F762AB8BDC34BEE98 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Spider.cs | A2D644947B93FCA18C64526DFB4706DEFF287169D837EF2E50255D9583F20A73 |
| Assets/LingGuangV05/XingGuang/Desktop/XgResidentSpider.cs | 5F31EF668DAE56076BE3C53A087C82746520198EAF8173CA3DDCF97832F1C48B |
| Assets/LingGuangV05/XingGuang/Desktop/XgSetupOverlay.cs | 0DD93A51E16FBB841AE4AB31CA369E45C29F6F12995894FC13D24826848C14D5 |

## 统一验证入口与剩余工作

1. 主聊天统一 Unity 编译后运行 `DesignFirstLessonChecks.Run()`：包含首卡、八种评价组合、重复/过期、复核、特殊卡排除及生成/直接回答、家庭方向、真实模型翻转恢复和输入组件捕获/单次消费/禁用/指针身份检查。通过标记 `DESIGN_FIRST_LESSON_CHECKS_PASS`；内部子检查标记 `LESSON004_PREDICTION_RECOVERY_CHECKS_PASS`、`LESSON_007_SPECIAL_CARD_CHECKS_PASS`。所有检查集中于已授权的 `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs`；输入检查只临时创建并销毁Button与不激活的EventSystem，不创建运行时或碰存档。本轮未启动此Unity入口。
2. 纯规则结算入口：`lesson-012/run-checks.ps1`，本轮已实际重跑402条。该入口不编译页面、TMP、InputSystem或新增 Editor 检查，不可据此报告它们编译通过。
3. 采用 `lesson-017/scene-acceptance.md` 真实场景脚本。额外覆盖新卡首次呈现帧输入拒绝、下一帧正常恢复、同卡不变 Refresh 不锁死、teach/label 键盘与鼠标一致、输入框和验证码、焦点失去/禁用、误判反馈指向旧卡、家庭提示等。
4. 主聊天继续整合014/015壁纸与订阅。本组003/007/008/009/017生产修改已落地，但指针取消、同帧混合输入、跨帧释放、普通/复核/特殊模式、蜘蛛动作取消与setup提示的实际桌面时序都必须统一验收；静态调用链不等于运行通过。
5. 实际截图/录像仍为 NOT_RUN；完整60秒单目标、壁纸短亮、宽窄窗口英文完整显示均未证明。原文第13行保持未完成。

未启动 Unity/游戏、未提交/推送/打包、未读写真实玩家存档。未改壁纸、XingGuangView.Frame.cs、科技树/UiTip、动效按钮/XgJuice、全局进度或队列。扩展授权中的教学相关生产文件仅做上述精确局部修改。

## 本轮收尾核验

- 在特殊卡生成条件修改之后再次运行 `lesson-012/run-checks.ps1`，退出0，402条断言通过。构建报告既有 `XgSim.DayStats.cs:33` 的CS1718警告，本组未修改该文件。
- 所有本组已跟踪生产差异的 `git -c core.whitespace=cr-at-eol diff --check` 通过；新增输入脚本、统一Editor检查和本记录无尾随空白。
- 检索确认不存在临时 `TickTeachingKeys` 或答案按钮的旧 `onClick.Invoke` 路由，数字键与新指针/Submit组件均进入捕获上下文后的答案链路。
- 本轮实际生产写入：`XgLabelPage.cs`、`XgLabelPage.Spider.cs`、`XgResidentSpider.cs`、`XgSetupOverlay.cs`、`XgSim.Stages.cs`、新增 `XgLessonAnswerInput.cs` 及meta；`XgSim.Teaching.cs`沿用上一轮事件/评价接口，没有再改结算。Editor入口为 `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs`。
- Unity入口及场景仍未运行，不能凭上述静态核验或402条纯规则断言勾选原文完成。
