# lesson-002：教学猜测的八种语义组合

结论：当前实现无需改动，不提交 changes.patch。仅对已授权的第 13 行中 TryEvaluateTeachingGuess 的判断映射、纠错计数及标注写入作静态行为证明；不表示整条策划或视觉效果完成。

已核对交接 Documentation/DesignPass/Agents/issues-task-001.md 和现有 DesignFirstLessonChecks，后者已包含八种组合及纠错记忆检查，本任务不重复实现。

## 完整真值表

前提：当前有效教学卡、显示猜测仍等于当前预测、未超时、有效 host、无复核卡；无外部事件处理器额外修改模型。是/否是题目答案，对/错是玩家对模型猜测的评价。每行都是独立的一次有效作答，计数均为增量。

| 模型猜测 | 题目真值 | 玩家评价 | 传给 Answer 的标注 | correct | corrected | handCorrect / handWrong | labels | teachingCorrections | 训练用标注 |
|---|---|---|---|---|---|---|---|---|---|
| 否 | 否 | 对 | 否 | true | false | +1 / 0 | +1 | 0 | 否 |
| 否 | 否 | 错 | 是 | false | false | 0 / +1 | 0 | 0 | 不写入 |
| 否 | 是 | 对 | 否 | false | false | 0 / +1 | 0 | 0 | 不写入 |
| 否 | 是 | 错 | 是 | true | true | +1 / 0 | +1 | +1 | 是 |
| 是 | 否 | 对 | 是 | false | false | 0 / +1 | 0 | 0 | 不写入 |
| 是 | 否 | 错 | 否 | true | true | +1 / 0 | +1 | +1 | 否 |
| 是 | 是 | 对 | 是 | true | false | +1 / 0 | +1 | 0 | 是 |
| 是 | 是 | 错 | 否 | false | false | 0 / +1 | 0 | 0 | 不写入 |

这里 labels 指 S.labels 对应数据集的手动标注数量。它与 result.samples 不可混同：后者还可包含 HandLabelLog 转换的日志样本；第一张无日志卡为 1。任何被正常消费的评价（包括误判）均换卡；TryEvaluateTeachingGuess 返回 true 表示已消费，不表示玩家判断正确。

## 原文件依据

- Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:46–56：输入通过当前卡/当前猜测校验后，提交 `guessWasRight ? shownGuess : !shownGuess`。令 G 为猜测、T 为真值、E 为玩家认为猜对，则标注 L = E ? G : !G；因此 L == T 当且仅当 E == (G == T)。否定“否”确实提交“是”。
- Assets/LingGuangV05/XingGuang/Core/XgSim.cs:1392–1410：correcting 在学习前计算为 `teaching && taughtGuess != yes`；correct 为 `!timeout && yes == card.truth`。只有 correct 分支才把 correcting 写入 result.corrected，因此误判不会因反对模型而得到纠错信用。
- 同文件 1414、1424–1427、1432：正确评价 handCorrect +1、AddLabel 一次、TeachBoard(card, card.truth) 一次；只有实际纠错才 RememberCorrection 一次。错误评价 handWrong +1，不进入上述写入链路。普通教学卡不会改写 card.truth；1399 的真值修改仅适用于已被 TeachingGuess 排除的 shutdown 卡。
- 同文件 1078–1088：AddLabel 把对应数据集 Labels 增加 1。XgSim.Flywheel.cs:109–116 的日志转换另外写 logs/dataExtra，不会重复增加 labels。
- Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:283–289：TeachBoard 从原卡构造训练卡，把 b.truth 设为传入 label 后调用 Board.Train。本链路传入 card.truth，故正确的“猜对了”与“猜错了”均写入真实答案；玩家错误评价不进入训练。
- Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:74–82：纠错记忆包含原卡的数据集、题目、特征、region、seed 与 card.truth；teachingCorrections 只增加一次。recentCorrections 最多保留 20 条，达到上限后追加并移除最旧项，列表长度不再增加，但累计计数仍 +1。
- Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs:49 起的三重循环已覆盖两种猜测、两种真值、正确/错误评价（与本表的两种评价一一对应），检查手工计数、corrected、学习样本、纠错累计、纠错记忆真值以及重复点击。

## 边界与验证状态

静态检查：八种组合与生产调用链一致，没有确认的语义缺陷。已检查既有超时保护：超时即使评价准确也 correct=false，不标注、不训练、不增加纠错；既有检查入口包含这一边界。成功认可模型猜对会学习，但不计纠错；成功纠正模型猜错才计纠错，二者不可混淆。

实际运行：本任务未启动 Unity、游戏或测试入口，未做编译；不声明运行 PASS。

视觉验收：本任务未进行。壁纸和事件表现不属于本任务的验收结论。

待整合：无需生产补丁；整合负责人可直接收录本行为证明。若源文件 SHA256 不一致，应复核上述方法后再采用结论。

待统一验证：运行既有 DesignFirstLessonChecks.Run。已有测试以 Samples 增长验证学习，未逐行直接断言 Labels 的精确增量，也未逐项核对纠错记忆的 dataset/seed/features；如主聊天扩展统一检查，可按本表增加这些断言，并用已有 20 条纠错记忆验证追加淘汰后累计计数仍只 +1。该建议不是已发现缺陷，也不是已执行测试。

## 审查依赖 SHA256

以下路径均相对于工程根目录，结束前再次核对。

| 文件 | SHA256 |
|---|---|
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Assets/LingGuangV05/XingGuang/Core/XgBoardData.cs | 914B8F2523827EF473098100EB97CA9AACC923B4FFA31682DC08E3B4ABA3ED51 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Epiphany.cs | DDE8E23FF9F1775BA0B04B5BF530E7F03B63A45486D0101ECE1C12A2368BBBEE |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Flywheel.cs | 3BE4B4601EAB078560D62E34AF3D16659706C8093E60D052161E3A6A5EA3F3EB |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |

结束前复核：上述 7 个依赖 SHA256 全部一致；仅写入本任务目录中的 result.md。
