# lesson-007：法律卡、关机特殊卡与种子保护

日期：2026-10-13。范围仅承接 issues-task-001 的首分钟教学改动；未读取新的策划正文，未读取终局策划。生产源码只读，所有交付均在本目录。

## 结论

教学排除、直接答案语义与种子维护保护已有实现，不需要重写。确认一个特殊卡生成顺序缺陷，交付 `changes.patch`（尚未应用）和 `Lesson007SpecialCardChecks.cs`（尚未编译或运行）。没有将当前策划条目或整份文档标记完成。

## 已确认的问题与精确修正

`XgSim.cs:1352` 先执行 `MaybeBounty`，`:1356` 后执行 `MaybeShutdownCard`。`XgSim.Bounty.cs:65-68` 可将同一卡设为 `bounty=true, law=true`；`XgSim.Stages.cs:208-209` 原先既不排除这些标记，也不清除它们。`DecorateProgressionCard` 对 logic/arith 不消除这两个标记。因此存在同一卡同时带 `kind="shutdown"` 和 `law=true` 的路径。

此卡作答时，`XgSim.cs:1398-1399` 按关机选择记录玩家答案和计数，但 `:1417-1426` 又因 law 标记跳过样本与 `TeachBoard`，使选择未真正教给种子；同时会继承悬赏倍数。普通专家悬赏与关机卡叠加也会继承悬赏奖励。这不是新教学布尔映射本身的错误。

补丁仅在 `MaybeShutdownCard` 的进入条件中排除 `card.law || card.bounty`，让先生成的特殊题保留原题目、真值和奖励语义，关机卡继续从普通卡生成。没有改题库、种子初值、学习规则或教学入口。副作用：命中悬赏时不再抽取关机概率，后续随机序列会与旧版本不同；总关机卡上限不变。此补丁防止新生成的混合卡，不迁移或读取历史玩家存档；历史混合卡是否实际存在未验证。

目标：`Assets/LingGuangV05/XingGuang/Core/XgSim.Stages.cs`。
源 SHA256：`CE64E64CCFB1CC7C24ADAD73C771650F367DAD6E4F55E30F762AB8BDC34BEE98`。补丁含原始上下文，整合前须再次核对。

## 已正确实现的依据

- `XgSim.Teaching.cs:64-71` 在预测及初始化体检记录前排除 law 和 shutdown；`:46-56` 拒绝进入教学映射，因此不发 `LessonTaught`。
- `XgLabelPage.cs:367-375,632-633,671-672` 根据实际教学资格使用原答案按钮及直接 `Answer` 路径，不把特殊卡的“是/否”反转为评价猜测。
- `XgSim.cs:1400,1417-1427` 法律题仍按题目真值判定，答对可得报酬，但不增加样本、不训练棋盘、不记教学纠错；法律题答错仍走原错误结算。
- `XgSim.cs:1398-1399` 普通关机特殊卡以玩家选择作为本次真值；`:1424-1426` 保留样本与训练。`XgBoardData.cs:155` 将该卡映射到种子特征，`XgSim.Board.cs:283-289` 用玩家答案训练。
- `XgBoard.cs:558-565` 种子允许正常 R1 学习，不能误修为完全冻结。`:648` 容量淘汰/叠加、`:688` 衰减、`:734` 重建、`:750` 迁移、`:774` 快照和 `:796` 权重震荡均保护种子。恢复快照先保留种子并在 `:785` 跳过同键覆盖。

## 检查与待验证

- 已完成：只读静态调用链核对；`git apply --check --ignore-space-change` 接受补丁上下文。这只是补丁可应用性检查，没有修改生产文件，也不代表行为测试通过。
- 现有 `DesignFirstLessonChecks` 已覆盖特殊卡拒绝教学；本交付没有重复修改该文件。新增候选入口聚焦它未覆盖的真实边界：强制关机抽签命中时 law/bounty 各标记及组合保持原题；普通 logic/arith 控制组仍可生成关机卡；两种真值×两种直接选择；特殊卡全部教学按钮组合无副作用；法律不学、关机实际训练；种子经重建、迁移、震荡、快照恢复和长时间衰减保持身份及已学权重。
- 候选检查调用真实生成方法（反射只用于控制随机分支入口）与真实 `Answer`/棋盘方法，不另写答案算法。原源码预计在特殊卡生成检查处失败；该预期尚未实测。
- **实际运行：未执行。** 未启动 Unity、游戏、构建或打包；候选 C# 尚未编译，不能报告 PASS。
- **视觉验收：未执行。** 主聊天需检查法律/关机卡仍显示直接答案按钮，且不显示教学“猜对了/猜错了”提示或发首课成功亮光。
- 待整合负责人复核并应用补丁，将候选检查放入其选定的 Editor 测试入口，统一编译运行；现有首课检查也应继续运行。候选检查只使用新建内存状态。

## 依赖指纹

以下路径均相对工程根目录；用于发现并行修改，不是锁定生产源码。

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Bounty.cs | 166537BD04F2B694594B19CAF24339F77988C299D5E210DC597210FE0B345BAD |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Core/XgBoard.cs | DAF725A9B24E7E8C7DD303C8A32ECD4B87D3B1AE301E0AB867AD503B2F75E963 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Assets/LingGuangV05/XingGuang/Core/XgBoardData.cs | 914B8F2523827EF473098100EB97CA9AACC923B4FFA31682DC08E3B4ABA3ED51 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |

结束前复核结果见同目录 `dependency-recheck.txt`。
