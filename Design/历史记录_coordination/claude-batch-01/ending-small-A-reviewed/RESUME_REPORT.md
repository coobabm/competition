# 终局小补丁 A：独立审阅交付（2026-10-19）

结论：A1–A5已经按当前生产逐段重做候选；没有应用Claude原始整包，也没有改生产。当前批准已包含老周作者一直知情、只在最终阶段主动推进的例外。此前待确认状态已由主控转达的新明确A选择解除。前期披露、YY主动接受、其它NPC/群友边界保持。

## 补丁选择

- 推荐分项：先 `01-A145-presentation-guide.patch`，再 `02-A23-final-contact.patch`。第二份依赖第一份提供的只读终局门禁；不是对生产直接独立应用。
- 或只应用 `ending-small-A-reviewed.patch`：两份合计的完整审阅版。**组合补丁和分项补丁二选一，不要重复应用，也不要用原ending-small-A.patch整包。**
- 完整补丁、01均对当前脏生产git apply --check通过。01→02已在隔离基准副本真实应用，文本与candidate逐文件一致。Git保留既有CRLF，候选文本以LF保存；manifest候选SHA针对交付字节，不冒称应用后字节一定相同。
- `ending-small-A-reviewed.patch` SHA256 `e903ecfa2fd34e3143734543f761bc7def4cec619ada91bb470b6e88a164958d`。
- `01-A145-presentation-guide.patch` SHA256 `e1d5da3cdd11f822572c4c95c6f980e45789f5c48e68893da43de5897de40512`。
- `02-A23-final-contact.patch` SHA256 `ace9702cebb97b27cb567353130697c66eb4f9874db3f8ff96fb5316e896872a`。

## 本轮修正

- **A1**：保留用户指定红队卷首句与英文，仍只出现在尚未开卷的封面；不改题目、评分或门槛。
- **A2**：同一Persist事务添加贴吧兜底，siQuestionAnnounced与私信一起保存；SaveNow失败恢复forumState/chatState/labState和消息/未读。只接受同档、legacy迁移已检查、无YY关系、未消失的老周，以及已有双方非空私信往来。不建立新的联系人，不发群。既有YY路径仍需合法可见联系人。
- **A3**：仅stage6/fullOpen、未完结/发布、尚未真正开卷/有attempt/读末页/读SI/选路线时发送一次。空Unity pending placeholder不误判为开卷。保留原idle/打字/选项门禁，补剧情独占与已消失账号检查。
- A3不采用关键词披露分类器，不新增正则或虚构玩家分享。两句统一中性邀请替代“话少了”“程序长进不小”等无证据推断。消息与finalApproachSent在真实SaveNow事务中同时提交；失败回滚并2秒重试。新字段旧档默认false，但已开卷旧档不补发。
- A3/A2实际发送标记形成终局私聊提示词例外；仅老周且绑定同一world/lab时生效。去除终局里“不是作者/不知道应用”的冲突，保留没有具体训练/私人聊天数据的限制。其它联系人、群聊、前期老周的现有规则不变；没有恢复SeesExe或全知数据注入。
- **A4**：只读Evidence helper按真实refId+date找记录，只有HasPlayerTeachingAnswer(含关联笔记)才说“教过”；实际YY chat说“说过”，其它真实玩家答题说“回答过”。book/retest/AI自动动作/缺失或错日期记录使用中性“根据…的记录”，不以宽泛kind=answer冒充玩家教学。无效日期不打印伪造日期。保留来源summary原保存语言，不假装翻译原始记录。
- A4灰字采用来源日期；面板仅保留错误回答的红色“被带偏：”，消除重复前缀。
- **A5**：浏览器和XgGuide共用只读UsesPreparation门禁，隐藏新流程的旧“让它读”按钮；便签改指终章真实页，不再指向旧试题控件/摆渡信入口。旧已完结且无新发布凭据的档仍走原分支。既有guide测试补新流程断言并明确legacy-completed fixture保留原断言。

## 与Claude候选的差异

- 原A2不检查是否已有实际往来，现严格要求双方消息存在且不创建conversation。
- 原A3关键词规则可能把“我写了个程序作业”等认作AI披露，且台词推断玩家行为；已全部删除，改为获批最终阶段的纯作者邀请。
- 原A3只Touch，未保证消息/标记立即同事务保存；现SaveNow回滚且先提交后刷新。
- 原A4将所有answer来源称为教学，而生产把book/retest/correction也装入该类别；现查真实record与player provenance。
- 原A5明知XgGuide会留下失效指引但未改；已一起修。
- 不修改R3自动执行/钉卡顺序，不借这批重做终局玩法。888批次合入后，本候选10个已有文件基准哈希再次核对仍一致（没有覆盖XgEndingPreparation.cs或XgEndingNarrative.cs）。

## 精确文件

- `Assets/LingGuangV05/Core/Forum/LaoZhouQuestions.cs`（已有，基准SHA256 `1f5133cfa51db6dea2e25415502d6892e9a3f1d5579e09532ebdb46e61e9ec18`）
- `Assets/LingGuangV05/DesktopBridge/Bodu/BoduView.cs`（已有，基准SHA256 `de0b5d6d898245aaea55177cb02922ee0840d354477cdf55e913abd3bdcc4cc2`）
- `Assets/LingGuangV05/DesktopBridge/Tieba/TiebaHub.cs`（已有，基准SHA256 `e06d50ac62f2072019387c0bb158d5c301ba82781f4ad354bdaa249ebc48379e`）
- `Assets/LingGuangV05/DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs`（已有，基准SHA256 `55c673f34741d5e3af72ee0b047f5af5fe985ec4467118ffdfd2dcb68129edf1`）
- `Assets/LingGuangV05/LocalLLM/LlmPersonas.cs`（已有，基准SHA256 `04e238b94305eca50e19f1688a12503fa13f9d275806e8c10cf54911a9a35648`）
- `Assets/LingGuangV05/XingGuang/Core/XgEndingPresentation.cs`（新增）
- `Assets/LingGuangV05/XingGuang/Core/XgEndingPresentation.cs.meta`（新增）
- `Assets/LingGuangV05/XingGuang/Core/XgGuide.cs`（已有，基准SHA256 `be518985405154b0a90bbe288d7aadc742fd344d512f97deafcd81375507b237`）
- `Assets/LingGuangV05/XingGuang/Desktop/EndingLaunchNightPanel.cs`（已有，基准SHA256 `e9f8f949907104de28370e168d63307aba3d42ca867d39eaa28ebe23b116f014`）
- `Assets/LingGuangV05/XingGuang/Desktop/EndingTrialPanel.cs`（已有，基准SHA256 `3a4333d4876b3a904bc4dd688baa1dff2963b1c07fe2a8bd603dc08dad50b189`）
- `Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.cs`（已有，基准SHA256 `34adec345f56017d7f4e33ce61b83c22220bff131e76829faaad8479038748d8`）
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgEndingSmallAReviewedChecks.cs`（新增）
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgEndingSmallAReviewedChecks.cs.meta`（新增）
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgGuideTests.cs`（已有，基准SHA256 `569b3fea9a8b4edc554c3508fa0b6a8a76bdb122aa947e62c6c513c40ccfedd4`）

14路径：10个已有CS、2个新增CS及各自meta；完整hash见manifest.json。新产品文件XgEndingPresentation.cs，新测试XgEndingSmallAReviewedChecks.cs。

## 已执行验证

- 隔离编译：LingGuangV05.Core、LingGuangV05.XingGuang.Core、Assembly-CSharp、Assembly-CSharp-Editor、LingGuangV05.XingGuang.Tests全部0错误；日志在verification/*-build.log，未开Unity。
- .NET核心46项通过（门禁、历史来源语义、Guide定位、保存失败/异常回滚、成功去重、无假披露/YY接受），另执行完整stage6引导定向测试1项通过：verification/core-checks-final.log。
- **广泛Guide回归未全过**：原 `TheDataStepNamesTheSmsPackThenTheLabelsThatCountInFull` 断言“a full bar needs no labels”失败。用未改生产测试源码及生产ScriptAssemblies单独重跑，复现同一错误；日志guide-broad-failure.log与guide-baseline-failure.log。没有修改该测试、弱化断言或把整套记通过。该问题与新终局分支无关，仍交主控记录。
- 核心harness使用System.Text.Json；不代替Unity JsonUtility的旧档缺字段/真实磁盘恢复验收。

重跑命令（games目录）：
```powershell
python .coordination/claude-batch-01/ending-small-A-reviewed/verification/prepare_compile.py
dotnet build .coordination/claude-batch-01/ending-small-A-reviewed/verification/LingGuangV05.Core.csproj --verbosity quiet
dotnet build .coordination/claude-batch-01/ending-small-A-reviewed/verification/LingGuangV05.XingGuang.Core.csproj --verbosity quiet
dotnet build .coordination/claude-batch-01/ending-small-A-reviewed/verification/Assembly-CSharp.csproj --verbosity quiet
dotnet build .coordination/claude-batch-01/ending-small-A-reviewed/verification/Assembly-CSharp-Editor.csproj --verbosity quiet
dotnet build .coordination/claude-batch-01/ending-small-A-reviewed/verification/LingGuangV05.XingGuang.Tests.csproj --verbosity quiet
dotnet run --project .coordination/claude-batch-01/ending-small-A-reviewed/verification/Checks.csproj
```

## 主控Unity验收建议（本候选均未实际运行）

- 精确TestFilter：`LingGuangV05.XingGuang.Tests.XgEndingSmallAReviewedChecks.SourceSemanticsGuideAndFinalContactTransactions`；并跑XgGuideTests定向stage6及原LaoZhouQuestionTests、LaoZhouNewsCommentTests、EndingPrivateDisclosureTests、XgEndingPreparationTests。
- A1中英首次卷首可读、开卷后不重复；A4正常/被带偏/来源缺失/旧档错误日期的灰红字和长摘要布局。
- A2真实无YY且已有贴吧往来的隔离档，读信后只发一次；缺往来/未迁移/YY已关联/账号消失/切档不发错渠道；注入真实SaveNow失败，核对磁盘+hub消息+未读+forumState+siQuestionAnnounced一致回滚，再重试/读档不重复。
- A3中英×YY/贴吧首次终局入口，真实保存后重载不重复；早期/锁定fullOpen/已开卷/已读信/已发布/剧情独占/正在打字不发。真实失败回滚与切档同上。
- 提示词应核对模型请求上下文，仅老周终局实际发送后出现作者身份；群友/其它联系人/前期不出现；这是待验证的提示词接线，不宣称实际模型必然遵守。
- A5正常与legacy-completed档，终章便签可打开真实页面，摆渡旧文字/已读信重览保留。
- 未验Player、模型回答、实际SaveNow磁盘失败、UI截图与性能。本批只有候选、隔离编译及核心测试，不是可玩交付。

## 新写对白

已获批最终阶段主动入口采用：
- 放出去之前，先让人往狠里试一遍。我这儿有份卷子。 / Before letting it out, have someone test it hard. I have a paper ready.
- 先做这三关，再决定下一步。 / Try these three gates, then decide what comes next.

来源动词与Guide双语新增文本见XgEndingPresentation；它们只描述现有记录和真实UI步骤，无新增剧情事实。
