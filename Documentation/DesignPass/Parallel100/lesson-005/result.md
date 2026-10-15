# lesson-005：人工复核队列与教学切换的按钮语义

日期：2026-10-13（Asia/Singapore）。范围仅限已授权的第 13 行对应子任务；未读取新的策划正文。

## 结论

当前实现无需修改，不交付 changes.patch。人工复核始终提交题目真值，不会因 teachMode 开启而把“对／错”解释为“AI 猜对／猜错”。已核对交接 `Documentation/DesignPass/Agents/issues-task-001.md`，其所述队列隔离和同卡教学转复核保护确实存在，避免重复实现。

本结论是静态源码复核，不是 Unity 编译、实际运行或视觉验收结果。未运行检查入口，不声明运行 PASS；不将第 13 行整体或文档标为完成。

## 原文件依据

以下路径均相对工程 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。

- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:122`：`CurrentCard` 优先取当前桌 `ReviewCard`。
- 同文件 `:623–633`：队列使用卡片原 `hasJudgment/guess/confidence`；教学标识严格为 `!queued && Sim.TeachingGuess(card, out _)`。队列无原猜测时也不会退回教学预测。
- 同文件 `:671–672`、`:134`：仅教学显示“猜对了／猜错了”；复核显示“对／错”（True/False），对应题目真值。`:206–212` 的非教学提示也明确“题目对／题目错”。
- 同文件 `:355–375`：先检查模拟实例、页面模式、卡片 ID；再比较已显示按钮语义与当前教学资格。不一致时 `Refresh(); return;`，在答案结算前退出。复核明确调用 `AnswerQueued(card.id, answer, Host)`，不对 `answer` 做猜测取反映射。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:46–54`：`TryEvaluateTeachingGuess` 在取卡、预测、Answer 和事件之前拒绝当前桌有复核卡的请求；教学模式关闭时也因 `TeachingGuess` 返回 false 而拒绝旧评价。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:419–450`：`AnswerQueued` 以 `yes == card.truth` 判分，不依赖 teachMode。`yes != card.guess` 仅用于正确回答后的纠错认定，不改写玩家提交的真值；该入口不发 `LessonTaught`。
- `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs:97–119` 已有模式关闭后的旧教学拒绝、普通真值回答、同卡转队列后的旧教学拒绝、队列保持不变和直接提交 true 的检查。无需为已覆盖行为再制造补丁。

## 切换边界的静态推导

| 已显示状态 → 点击前状态 | 当前处理 | 语义结果 |
| --- | --- | --- |
| 教学 → 同一卡进入复核队列 | 显示教学=true，当前教学=false；仅刷新 | 旧“猜错了”不会变成题目 false 提交 |
| 复核 → 同一卡成为教学卡 | 显示教学=false，当前教学=true；仅刷新 | 旧“对”不会变成认可 AI 猜测 |
| 教学 → teachMode 关闭或 UseBoard 关闭 | 教学资格不一致；仅刷新 | 不将旧评价当题目真值 |
| 普通真值 → teachMode 开启且可教学 | 教学资格不一致；仅刷新 | 不将旧真值当 AI 评价 |
| 复核期间 teachMode 开／关 | queued 恒为 true，教学恒为 false | 仍向 AnswerQueued 原样传入真值 |
| 切换期间卡片 ID 改变 | ID 检查先拒绝并刷新 | 不对下一卡提交旧按钮含义 |
| 队列没有保存猜测 | shownGuessValid=false，teaching=false | 仍可按题目真值复核 |

同一卡的教学资格变化不会单凭 `Tick` 的卡片 ID 检查立即重绘；`OnAnswer` 的语义校验会安全刷新并丢弃该次旧语义点击。这是当前明确行为，不是已确认的误结算缺陷，不为其额外改动生产源码。

## 待整合与待验证

无生产补丁待合并；整合负责人可采纳本行为证明。请统一运行已有 `DesignFirstLessonChecks.Run` 并检查以下尚未由其完整覆盖的场景：

1. 复核队列交叉覆盖真值 true/false、保存猜测 true/false、teachMode 开/关；两种按钮的正确性只能由提交值与真值是否相等决定。尤其“AI 猜 false、题目真值 true”点击“对”应答对；“AI 猜 true、题目真值 false”点击“错”应答对。所有复核均不得产生 LessonTaught。
2. 页面显示后、点击前同卡从教学移入队列及从队列移回教学：首次鼠标或键盘输入只刷新；钱包、样本、队列、手动计数、事件次数保持不变。按刷新后的按钮第二次输入才结算一次。
3. 同卡 teachMode 双向切换与 UseBoard 关闭：核对首次点击拒绝、刷新后的按钮与下一次结算语义；队列中的 teachMode 切换仍接受原真值按钮。
4. 视觉验收中英文复核按钮、悬停提示、AI 建议文案以及键盘 1/2 与鼠标的一致性。这里仅列验收目标，未执行或截图。

## 依赖快照（SHA256）

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs | 31F6D4234DC64046ED6961F3DE7B7D28EE8A677C2ED1EA50226DCE421B7ACDB8 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
| Documentation/DesignPass/Agents/issues-task-001.md | C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971 |

结束前复核结果见本目录 `dependency-check.txt`。仅写入本任务目录，未改 Assets、全局清单或其他目录；未启动 Unity/游戏，未读取玩家存档，未提交、推送或向其他聊天发消息。
