# lesson-013 · LessonTaught 发布语义核对

日期：2026-10-13。范围仅为已授权第 13 行的事件发布语义；已读交接 `Documentation/DesignPass/Agents/issues-task-001.md`，未读取新的策划正文。

## 结论

当前实现无需修正，不提供 changes.patch，不修改生产源码。静态调用路径符合：成功教学学习完成后发布一次，携带刚回答的原卡片与实际结算结果；错误评价、超时、普通标注及待复核作答不发布。

“一次”指每次成功教学提交只有一次事件发布，不等于每个视觉订阅者一定只执行一次；重复订阅和壁纸显示验收属于整合方范围。

## 原文件证据

- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:40,46-56`：事件类型为 `Action<XgCard, XgAnswer>`。先保存 `var card = Card(desk)`，校验卡片 ID、教学资格和显示预测，再调用 `Answer`；唯一发布语句为 `if (result.correct) LessonTaught?.Invoke(card, result)`。全 Assets C# 搜索未发现其他发布点。
- `XgSim.Teaching.cs:50-52,64-70`：无 host、无合法桌面、待复核、旧 ID、旧预测、非教学模式、禁用棋盘、家庭/法律/关机卡均无法进入发布路径。普通 `Answer`、`AnswerQueued`、`ChatLabel` 本身不发布该事件。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:1396-1428`：正确结果要求未超时且提交值等于真值；正确教学分支先增加样本，再 `TeachBoard(card, card.truth)`，实际纠正时还记录纠正记忆。误判和超时走错误分支，不学习，返回的 `correct` 为 false。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:283-289`：`TeachBoard` 同步调用 `Board.Train`；正常教学资格已满足其棋盘、卡片和数据集前置条件。事件位于整个 `Answer` 返回之后，因此学习调用及结算已完成才会发布；学习方法若抛出异常，控制流也不会到达发布点。
- `XgSim.cs:1443-1446,1281-1286`：返回前已换卡；`NewCard` 创建新对象并替换列表条目，不复用旧对象。事件仍使用先前保存的原卡引用；重复提交旧 ID 被入口拒绝，不会再次发布。
- `Assets/LingGuangV05/XingGuang/Core/XgModels.cs:109`：`XgAnswer` 是值类型；事件传入的是 `Answer` 返回的同一结算值，没有重新计算或拼装。普通手动作答的 `accepted/cardId` 沿用原默认值；本事件合同以第一个参数携带原卡，消费者不可误把 `accepted` 或 `result.cardId` 当作该事件的成功/卡片标识。
- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:366-375`：教学评价只调用 `TryEvaluateTeachingGuess`；普通标注和复核分别走 `Answer` / `AnswerQueued`，不存在页面层再发一次事件的路径。

上述结论针对现有同步调用路径。未把未知订阅者、外部状态突变等假设当作已确认缺陷。

## 既有检查与待验证

已核对 `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs`，无需另造重复检查：

- 第 50-81 行：两种预测 × 两种真值 × 两种评价，断言成功时事件计数恰为 1、失败为 0；回调内核对原卡引用、结果真值与成功标志、样本已增加。
- 第 83-85 行：旧卡重复提交被拒绝且不重复结算。现有该断言未再次直接检查事件计数；统一运行时可补充观察回调总数仍为原值。
- 第 88-115 行：旧预测、普通标注、教学转复核和直接复核保持不发布。
- 第 128-137 行：超时即使评价正确也不学习、不发布。

验证状态：仅完成静态源码/调用点/既有检查覆盖审查；没有运行 Unity 编译、检查入口、游戏或场景，没有视觉验收，不能标记运行 PASS。没有读取或更改真实玩家存档。

待整合：无生产补丁可应用。整合负责人可直接采纳本范围“无需改动”的审查结论。

待主聊天统一验证：运行 `DesignFirstLessonChecks.Run`；回调处观察原卡、返回结果和学习后状态，旧卡二次提交后事件累计仍为 1；视觉侧另行确认壁纸订阅与换实例取消订阅，事件契约本身不证明壁纸已亮。不得据本报告将整条第 13 行或整份文档标为完成。

## 源码版本及结束前复核

以下路径均相对工程根目录。SHA256 为本次审查基线，交付前已再次比较这七个依赖的完整文件内容哈希，全部未变化；若整合时变化，应按新源码重新审查而非盲用本结论。

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Assets/LingGuangV05/XingGuang/Core/XgModels.cs | 4461CA0A8857A8BA78EAD346C869B5C125AB160411CF4EAAFB9F631E40197953 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs | 31F6D4234DC64046ED6961F3DE7B7D28EE8A677C2ED1EA50226DCE421B7ACDB8 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
