# lesson-010：教学卡名称、猜测、按钮与悬停说明

日期：2026-10-13。整合负责人：`01a130ed-2dd2-7051-915d-dc64ea66a8e2`。

## 结论

本任务范围内无需改动，不交付 changes.patch。现有中英文文案已区分“题目的答案”与“AI 猜测是否正确”，且沿用玩家自定义 AI 名称。已核对 `Documentation/DesignPass/Agents/issues-task-001.md`，其已实施内容与当前生产源码一致，避免重复补丁。

本轮仅阅读生产源码及上述已有交接记录，没有阅读新的策划正文；唯一写入产物为本文件。未启动 Unity 或游戏，未读取真实玩家存档，未提交、推送、打包，也未发送跨聊天消息。

## 代码依据

以下路径相对于工程根目录；行号对应本次审查快照。

| 检查对象 | 当前行为与依据 |
| --- | --- |
| AI 名称 | `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:650` 使用 `Sim.AiDisplayName`，没有硬编码默认名字或对完整拼接结果进行翻译。`Core/XgSim.Persona.cs:63`（位于 XingGuang 下）从 `Profile.name` 获取名称。 |
| 玩家名字／称呼保留 | `Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs:175` 构造 Profile，177 行分别将 `s.aiName` 赋给 `name`，将 `s.aiCallMe` 赋给 `callMe`。教学卡取前者；不改写后者或存档。 |
| 名称显示策略 | `Assets/LingGuangV05/Core/AppNames.cs:25` 委托 `AiNamePolicy.Display`；`AiNamePolicy.cs:64–68` 对合法自定义名字返回名字本身，语言参数仅影响校验提示及默认名回退。已有规范化、旧 `.exe` 后缀处理、非法名称回退仍保持原策略；不承诺非法旧名称逐字显示。 |
| 猜测文案 | `XgLabelPage.cs:650` 显示 `[名字]猜：是／否` 与 `[name] guesses: Yes/No`，取预测变量 `guess`，不是 `card.truth`。首张短信题在 `Assets/LingGuangV05/XingGuang/Core/XgMemes.cs:144,158` 为“这条是垃圾短信吗？”／“Is this SMS spam?”，与 Yes/No 语义一致。 |
| 按钮 | `XgLabelPage.cs:671–672` 显示“猜对了／猜错了”与“Guessed right／Guessed wrong”，分别对应 1、2；没有把教学按钮写成题目 True/False。普通卡保留既有 `YesNoWord`。 |
| 悬停 | `XgLabelPage.cs:206–211` 根据当前教学显示状态和 Mode 动态取文案；肯定按钮说明认可刚才的 Yes/No 猜测，否定按钮说明教它相反答案，中英文含义一致。 |
| 点击含义 | `XgLabelPage.cs:373` 将按钮布尔值作为猜测评价传给 `TryEvaluateTeachingGuess`；`Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:53` 用 `guessWasRight ? shownGuess : !shownGuess` 转成题目答案，再交给原结算。 |
| 错误反馈 | `XgLabelPage.cs:363,443–444` 将“它其实猜对／猜错”与“题目的答案是 Yes/No”分别表达，既显示预测评价，也明确题目真值。 |
| 教学边界 | `XgLabelPage.cs:632` 排除复核卡；`XgSim.Teaching.cs:67` 排除非教学、关机、家庭态度、法律卡等，避免把不同回答含义混用。 |
| 语言来源 | `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:138–139` 由 `GameText` 提供语言与双语选择；`Assets/LingGuangV05/Runtime/GameText.cs:23` 直接选择传入的对应语言文本。控制器 137 行同步 `Sim.English`。即时切换帧序仍需运行验证。 |

## 语义边界与待验证项

以下为静态推导的预期，**不是运行测试结果**。在两种语言中均应保持：

| 显示猜测 | 玩家点选 | 提交给题目的答案 |
| --- | --- | --- |
| 是 / Yes | 猜对了 / Guessed right | 是 / true |
| 是 / Yes | 猜错了 / Guessed wrong | 否 / false |
| 否 / No | 猜对了 / Guessed right | 否 / false |
| 否 / No | 猜错了 / Guessed wrong | 是 / true |

每行还需分别覆盖题目真值 true 和 false：是否教学成功由提交答案与真值是否相等决定，不能因玩家点击“猜对了”就直接算成功。已有 `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs:46–79` 覆盖两个预测方向、两个真值与准确／错误评价，以及真值不被修改。本任务未重复新增同类测试，也未运行此入口。

待整合：仅登记无需补丁结论，无生产文件需要应用。原条目仍不能标记整体完成。

待主聊天实际运行：Unity 编译和既有检查入口；中文、英文各验证以上四种按钮语义及悬停；使用独立内存或测试档确认合法中文名、英文名、混合文字名在切换语言后保持自定义内容，玩家称呼不串入 AI 名称；覆盖尚未命名时的默认名显示。不得以本静态审查代替运行结果。

待视觉验收：中英文教学卡、最长合法自定义名、宽窄窗口的猜测行和两个按钮是否完整可读；悬停说明是否与当前卡状态对应。本任务没有截图或视觉验收结论。

## 依赖指纹

SHA256 用于整合时识别源码变化，不是补丁应用指令。结束前已重新计算并对照以下 11 项指纹，均未变化；这仅确认文件快照一致，不代表运行测试通过。如后续变化，需重新检查对应代码。

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
| Assets/LingGuangV05/Core/AppNames.cs | AAAB44B8A1F2EA1F8CC3BC7DBE976EFFA7CA32AE5428C6087541BFBA6D5FB0BB |
| Assets/LingGuangV05/Core/AiNamePolicy.cs | 51759A0E87D8612D7AFF06EAB1F1790EE48306535FE488C0C951B1584ED6DE12 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Persona.cs | D00FFBC54CC23A900C4A7D734C73E322FE3F032FD1DA02B765E35330478A46AC |
| Assets/LingGuangV05/XingGuang/Core/XgMemes.cs | 33CB756DFFCF838412A86769789EBAE2C4C2286334E877D530BD5F75DDFFF07C |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs | A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14 |
| Assets/LingGuangV05/Runtime/GameText.cs | 3A17CC453E1791D6AAEBE9D6EB4B13009D237BAFED442E89A9A87D99B0D56796 |
| Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs | 5C20DA1967F47EA3E5241613C69D1FEBDA3E5633BAB924499EB1AAAED1E222B6 |
| Documentation/DesignPass/Agents/issues-task-001.md | C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971 |
