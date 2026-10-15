# lesson-006：家庭态度兼容

日期：2026-10-13。范围仅为既有第 13 行教学交互改动对家庭偏好／短信选择的兼容性；未读取新的策划正文。

## 结论

当前家庭选择没有被教学评价映射反转，核心逻辑无需修改。确认一处同范围的展示问题：家庭卡两按钮的悬停说明落入普通客观题分支，仍称答错罚款、连击清零，与实际主观态度结算相矛盾。交付 changes.patch 仅补两条家庭专用中英文悬停说明，不改变按钮顺序、传参、家人倾向、故事设定或学习逻辑。生产文件未修改。

## 原文件证据

以下路径均相对于工程根；完整 SHA256 见 source-hashes.json。

- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:67`：TeachingGuess 在预测前拒绝 `card.family`。第 46–55 行 TryEvaluateTeachingGuess 依赖该条件，因此家庭卡不会走 `guessWasRight ? shownGuess : !shownGuess`，也不会由该入口触发 LessonTaught。
- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:187–188`：两按钮分别调用 OnAnswer(true/false)。第 632–633 行从 TeachingGuess 设置教学状态；第 674–675 行把家庭按钮明确覆盖为 true=“当作垃圾”、false=“留作重要”。第 367、375 行确认当前交互模式后原样调用 Answer。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:1390`：在客观真值和教学判定之前直接调用 AnswerFamily(card, yes, host)。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Family.cs:23–34`：true 累计 junk，false 累计 important；结果为 subjective/accepted，只发报酬并换卡，不判客观正确性，不扣罚款、不改变两种连击。第 37–47 行 FamilyPrompt 按玩家累计选择表达个人偏好，不把偏好当事实或指令。
- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:384–388`：主观结算显示“记住了你的态度”后返回，跳过对错历史和客观题反馈。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Spider.cs:63`：家庭卡被自动代答入口排除。
- 已读 `Documentation/DesignPass/Agents/issues-task-001.md`，其家庭语义保留结论与源码一致。

## 精确修正及整合

补丁目标：`Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs`。

补丁基线 SHA256：`A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89`。

在原 206、209 行 UiTip 回调中增加 `Mode == 0 && CurrentCard.family` 分支，保留 Sim 空值保护；分别说明“当作垃圾”和“留作重要”均记录玩家态度、不判对错、不罚款、不改变连击。补丁含原文上下文。

manifest 中 lesson-010 负责教学卡双语悬停语义，可能修改相邻代码；负责人整合时只合入家庭分支，保留届时的教学分支文本，勿整文件覆盖。没有核心行为补丁、没有修改全局清单。

## 验证记录和待验证边界

- 已执行静态源码链路核对；`git apply --check --ignore-space-change Documentation/DesignPass/Parallel100/lesson-006/changes.patch` 返回 0，只验证补丁可应用，未应用到生产文件。ignore-space-change 用于现有 CRLF 源码与交付文本行尾差异。
- 已有 `DesignFirstLessonChecks.cs:139–146` 覆盖家庭卡拒绝教学入口，但仅使用一种猜测／评价组合。
- 已有 `ClaudeHandoffChecks.cs:37–48` 覆盖家庭两选择、无样本／罚款、保持连击和内存序列化。其先 true 后 false、最终 important/junk 各为 1 的断言无法单独识别计数对调；本轮没有把该检查视为充分的方向测试，也没有执行该入口。
- 待负责人实际运行：分别用独立内存实例选择 true 和 false，断言 true 后仅 junk+1、false 后仅 important+1，并覆盖预置家人计数、卡片 truth 两值、教学模式开／关。拒绝教学入口应覆盖 shownGuess 与 guessWasRight 四组合，断言家庭计数、钱包、卡片、纠错计数和 LessonTaught 事件均无变化。
- 待真实场景：普通教学卡→家庭卡→普通教学卡切换；鼠标与键盘 1/2 均保持“垃圾／重要”方向；两语言悬停说明与按钮一致，家庭反馈不出现“它答对了／你纠正了它”；普通教学卡的提示仍正常。长英文说明的排版需视觉验收。
- 未启动 Unity／游戏，未编译、未运行 C# 检查、未做截图或视觉验收；没有读取真实玩家存档。本文不宣称这些项目 PASS，也不将原文条目或全局原则标为完成。

结束前依赖 SHA256 复核结果见 dependency-recheck.json；若之后源码再次变化，负责人须重新比对再整合。
