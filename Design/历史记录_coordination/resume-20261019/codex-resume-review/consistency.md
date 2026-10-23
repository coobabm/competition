# 设定一致性只读审查 — 2026-10-19

基准：`3533141a406940d571e064f3797328c7d214235a`，审查当前工作区未提交变更。依据最新暂停后交接、整合记录、六小时决策记录及当前协作状态。未修改生产源码，未应用候选，未启动 Unity，未提交或推送。用户要求当前项结束后暂停，本报告写完即停止。

## 具体差异：旧联系人没有取得新私人披露权限的主动入口

**建议优先级 P2；静态调用链确认，产品兼容预期留待后续确认/修复。**

- 新增门禁位置：`Assets/LingGuangV05/YY2016/YYChatHub.EndingPrivate.cs:20-26`。`EndingDisclosureReady` 要求 `ExplicitYyContact(yyLinkSource)`；后者在 `Assets/LingGuangV05/XingGuang/Core/EndingPrivateDisclosure.cs:30` 只承认 `tieba_dm_accept`。
- 既有迁移与入口：`Assets/LingGuangV05/Core/Forum/LaoZhouQuestions.cs:61-70` 将旧存档迁移为 `legacy_existing`，`Linked()` 同时承认该值；`Accept()` 第64行却拒绝所有 `Linked()` 状态。`Assets/LingGuangV05/DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs:48` 仅对未联系状态显示接受按钮，第130-133行也只向未联系状态发送邀请。
- **触发**：加载有旧 YY 老周历史、尚无 `laoZhouQa` 字段的旧档，迁移后来源为 `legacy_existing`；即便继续正常推进至有合法结局准备/发布回执、安装 YY、无输入占用，`EndingPrivateChoices()` 仍为空。该档不能通过现有玩家入口改为 `tieba_dm_accept`。玩家继续手动给老周聊天也不会改变该来源字段。
- **风险**：新档可用的结局私人披露及其计划附记，对该类旧档永久不可达。没有发现它把旧档擅自授权或补造披露；问题恰好是缺少后续真实授权路径。新闻分享仍可退回贴吧私信，因此不是所有老周交互都失效。
- **最小建议**：后续确认旧档应能使用新私人披露后，为 `legacy_existing` 增加明确、可取消的重新确认入口，真实点击才记录新的接受来源；随后仍走逐条预览与显式发送。不要放宽 `ExplicitYyContact()` 直接接受旧来源，也不要在迁移时生成接受/私人披露收据。用旧档→未确认不可披露→实际确认→实际发送的用例补覆盖。
- **现有测试缺口**：`Assets/LingGuangV05/XingGuang/Tests/Editor/EndingPrivateDisclosureTests.cs:25-27` 验证旧来源被拒绝，但没有覆盖旧档如何主动获得新权限。此报告没有运行新测试，不把静态推导称作 UI 复现。

## 其余审查结果

在以下范围未发现新的确定性 bug；不等同于完整运行验收通过：

- `LaoZhouNewsComments`、`TiebaHub.LaoZhouNews`、`BoduView.News`：分享入口验证当前论坛/YY 所有者及文章日期；只在真实点击后追加消息与来源事实，三篇按文章去重。被动阅读、教给私人 AI 没有进入老周的新增知识路径。切换语言重建显示不新增收据。旧来源走贴吧符合 D028。
- `YYChatHub.EndingPrivate`、`EndingPrivateDisclosure`、`YYChatView.EndingPrivate`：预览不写证据；发送重新核对存档、聊天、私人 AI 状态对象、语言及当前 offer。实际 AI 原话需匹配已提交决定；路线、原话、关机卡分别发送。完成档不新增披露。附记来自实际计划收据，实际发布状态不会伪造成事先披露。
- `XgSim.ReadNews`、`XgSim.DailyLife`、`LlmPersonas`、`XgYyTalk`：第六篇起独立保留阅读 ID；旧档只读取既有 news5 收据；只向 `lingguang` 且已购买 `daily.news`、满足表达阶段的上下文提供相关引用。引用重新验证日期/正文并按语言选择，未发现人类联系人获得该阅读记录的新增通路。XingGuangController 已按常规 Saving 事件写回完整状态，未发现第六篇单独遗漏序列化。
- 年代与实际能力：新闻只接收 2016 年已发表条目；终局公开信的 2017-06-13 是现有批准例外。R2 已完成披露措辞明确为公开说明页；计划措辞与批准的 LP2 原文一致，没有把当前虚拟发布清单冒称为真实模型权重导出。

## 验证边界

本次仅阅读代码、测试源码、设计与 git diff。磁盘存档、真实模型回答、实际窗口输入和最终 Player 均未在本审查中运行。未发现的问题不能据此宣称不存在；已有报告中的未通过/待验状态保持不变。
