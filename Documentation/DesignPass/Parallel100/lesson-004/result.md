# lesson-004：同卡预测变化与过期输入恢复

结论：在已显示、有效的同一教学卡上，现有生产实现符合本任务要求，无需修改生产代码，不提供 changes.patch。交付一个补充验证入口；该入口尚未编译或运行，不能标记 PASS。

范围仅为已授权的第 13 行相关输入一致性。未读取新的策划正文，未修改 Assets、ProjectSettings 或公共记录，未启动 Unity/游戏，未访问真实存档，未发送跨聊天消息。

## 代码证据与无需修改理由

- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:628–651`：显示阶段将预测保存为 `shownGuess`，将教学语义保存为 `shownTeachingEvaluation`，教学提示直接使用相同的 `guess`。没有将题目真值充当显示预测。
- 同文件 `355–378`：输入先检查显示所属实例、模式和卡片 ID；教学输入传入保存的 `shownGuess`。`TryEvaluateTeachingGuess` 返回 false 后执行 `Refresh(); return;`，不会落到 `AnswerCount++`、`LastAnswerDisagreed`、答题历史及答题成功/错误反馈分支。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:46–55`：重新取同一张卡的当前预测，比较 `currentGuess != shownGuess`，在 `Answer` 和 `LessonTaught` 前拒绝。因此猜测由否变是或由是变否，旧的“猜对了/猜错了”均不会被重新解释后提交。
- 页面刷新重新赋值 `shownGuess`，并通过 `yes.Set(..., true, ...)` 与 `no.Set(..., true, ...)` 恢复按钮。没有永久禁用标志或必须换卡才能清除的过期状态；下一次新输入可针对新猜测作答。
- `XgSim.cs:1385` 的结算路径承担钱包、标注、训练、连击和换卡；过期分支未进入该路径。`XgBoard.cs:400–469,516–522` 的预测匹配/得分只构造临时数据并读取概念权重，未训练或改写概念活跃度。

“无副作用”在此指已显示有效同卡的拒绝不会产生答题结算、训练、扣费、换卡或成功事件；刷新显示是必要且预期的 UI 更新。`TeachingGuess` 初次查询会初始化 checkups，`Card` 在卡片缺失/无效时会造卡，不能据此把整个查询 API 宣称为所有状态下的纯函数。正常显示已完成前者，本任务不扩展到未显示卡、损坏状态或卡片被移除的其他场景。

同卡模型变化但最终布尔预测仍相同，现有接口接受输入；判断语义仍然一致，当前需求没有要求按模型版本拒绝。若将来要求任何模型更新均作废，应另行增加展示版本令牌，当前不制造该变更。

## 已有验证与补充产物

交接记录和 `DesignFirstLessonChecks.cs:87–96` 已覆盖传入错误猜测值时拒绝、不扣费、不学习、不换卡、不闪。该例未通过实际模型更新制造同卡预测变化，也没有随后刷新并重新作答。

本目录 `Lesson004PredictionRecoveryChecks.cs` 是独立 Editor 检查入口，不替换现有检查。覆盖两种原预测 × 两种真值 × 两种旧输入；通过真实 `Board.Train` 改变预测，断言卡片内容与引用未变；重复拒绝旧输入并比较完整可序列化状态及非序列化模型 links、钱包调用、事件数；重新获取猜测后提交新的正确评价，要求只结算一次、正确学习并进入下一卡。最多训练 64 步，未翻转将明确失败，不跳过边界。

## 待整合与验证

- 整合负责人可将独立检查加入 Editor 检查程序集，再调用 `Lesson004PredictionRecoveryChecks.Run()`；成功日志仅是未来运行标志，不表示本轮已执行。
- 静态检查：已核对上述控制流；完成前复核源文件 SHA256。没有 Unity 编译、核心行为运行或视觉 PASS。
- 结束前复核结果：上述 8 个依赖的 SHA256 均与快照一致；本目录两个交付文件未发现行尾空白。此项仅为文件静态检查。
- 实际运行：由主聊天统一执行已有 `DesignFirstLessonChecks.Run` 和补充入口。补充入口只新建内存模型及钱包，不加载/保存玩家文件。
- 视觉验收：同卡显示旧猜测后改变模型，使预测翻转；鼠标或键盘触发一次旧评价，应只更新显示，保留题目和答题计数；根据新猜测再次评价，应正常结算。UI 自动刷新先于点击时，点击针对已经显示的新猜测可直接接受。分别验收两个预测方向及宽/窄窗口，不能用核心检查替代此验收。

## 依赖快照（SHA256）

下列路径相对工程根目录；没有生产补丁，因此这些哈希用于整合前复核证据版本。

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgBoard.cs | DAF725A9B24E7E8C7DD303C8A32ECD4B87D3B1AE301E0AB867AD503B2F75E963 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs | 31F6D4234DC64046ED6961F3DE7B7D28EE8A677C2ED1EA50226DCE421B7ACDB8 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
| Documentation/DesignPass/Agents/issues-task-001.md | C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971 |
