# lesson-008：蜘蛛与教学评价按钮兼容

日期：2026-10-13。交付状态：确认一条遗漏路径并提供局部补丁；尚未整合到生产源码。范围仅为已授权第 13 行中的教学评价与蜘蛛兼容，未读取新的策划正文。

## 结论与已有实现

`XgLabelPage.SpiderPress` 的教学拦截已经正确，不重复修改：

- `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:91` 的 `HasShownGuess` 要求 `!shownTeachingEvaluation`。
- 同文件 `:103–106` 的 `SpiderPress()` 在 `shownTeachingEvaluation` 时立即返回，先于 `Sim.SpiderAnswer`，不会提交猜测、结算或刷新下一题。
- `Assets/LingGuangV05/XingGuang/Desktop/XgResidentSpider.cs:498–499` 只有 `HasShownGuess` 为真才计划随机代答；`:534–536` 最终仍通过 `SpiderPress()`，仅 accepted 后播放信用反馈。
- `XgLabelPage.cs:623–633` 从非复核卡的 `TeachingGuess` 设置教学状态；因此不能用单纯 `TeachMode` 阻断整页，否则会改变复核/特殊卡的现有语义。

交接记录 `Documentation/DesignPass/Agents/issues-task-001.md` 已说明教学卡不得自评，但现有独立检查 `DesignFirstLessonChecks` 检查的是模拟层教学入口，没有覆盖拖拽蜘蛛的页面路径。

## 确认的问题及补丁

**拖拽代答绕过已存在的教学保护。** `XgResidentSpider.TickWork` 在 `:681`、`:690`、`:710` 分别调用 `SpiderBlocker`、`SpiderPeek`、`SpiderHandPress`，不经过 `HasShownGuess` 或 `SpiderPress`。`XgLabelPage.Spider.cs:58/78/91` 三处都未检查 `shownTeachingEvaluation`；底层 `XgSim.Spider.cs:55–65` 也没有教学拦截。因此，在能力、钱包、桌面开放等原条件满足的教学卡上，拖拽蜘蛛仍能取检查点猜测，并由 `SpiderDragPress → Answer` 自动提交标注、推进卡片。即使没有调用 `TryEvaluateTeachingGuess`，也绕过了“玩家判对错”，且动作落在语义已改为评价的按钮上。

`changes.patch` 仅包含两个文件的局部变更：

1. `XgLabelPage.Spider.cs`：教学状态返回专用 `teaching` blocker 和中英文提示；`SpiderPeek` 与 `SpiderHandPress` 均在提交前拒绝教学状态。按下处再次检查，可挡住从开始伸手到落下期间页面刷新为教学评价的情形。
2. `XgResidentSpider.cs`：已计划随机代答时每帧再次检查 `HasShownGuess`。原实现仅在 card id 变化时检查，故同一卡片刷新为教学评价后仍会继续靠近/踩评价按钮；原 `SpiderPress` 虽挡住结算，但残留动作会让玩家误以为 AI 在自评。补丁清除该计划和按键阶段。

普通标注 `shownTeachingEvaluation == false` 时仍调用原 `SpiderDragBlocker`、`SpiderDragPeek`、`SpiderDragPress`；随机代答概率、检查点猜测、手动/自动结算、奖励、罚金及反馈代码均未修改。核心模拟接口保持原行为。此补丁保护页面中的新评价按钮，不将底层模拟 API 改为全局禁用教学模式下的自动化。

## 原文件基线

以下路径均相对于工程根目录，SHA256 为本次读取的完整原文件字节摘要。补丁内也注明两个目标文件的摘要与上下文。

| 文件 | SHA256 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Desktop/XgResidentSpider.cs` | `5F31EF668DAE56076BE3C53A087C82746520198EAF8173CA3DDCF97832F1C48B` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Spider.cs` | `A2D644947B93FCA18C64526DFB4706DEFF287169D837EF2E50255D9583F20A73` |
| `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs` | `A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89` |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Spider.cs` | `C87745EE6C3052921C6A55FE373AD740179DACB7D43ADC22BC510EA620F9BC98` |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs` | `CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7` |
| `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs` | `C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1` |
| `Documentation/DesignPass/Agents/issues-task-001.md` | `C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971` |

## 验证与待整合

- 静态审查已完成：确认随机与拖拽两条实际调用链，核对普通分支仍进入原结算入口。
- 补丁上下文检查已实际运行：`git apply --check --ignore-space-change Documentation/DesignPass/Parallel100/lesson-008/changes.patch` 返回 0；仅检查，没有应用。原文件为 CRLF，整合时须保留行尾。
- 结束前已再次计算上述七个依赖文件的 SHA256，均与表中基线一致；带摘要说明的最终补丁再次执行上下文检查，未报错。
- Unity 编译、模拟运行、实际场景操作及视觉验收均未执行；不宣称运行通过。未启动 Unity/游戏、读取玩家存档、提交或推送。
- 无缺失接口。整合负责人应在应用前复核上述摘要及 `shownTeachingEvaluation`/`HasShownGuess` 的语义；若生产源码已补上等效保护，丢弃对应补丁片段，避免重复。

建议统一验证下列真实边界，使用临时内存状态，不使用真实玩家存档：

| 边界 | 应观察到的结果（待运行） |
| --- | --- |
| 可自动作答的桌面、教学卡、AI 猜是/否，分别随机代答与拖拽代答 | 两种按钮均不被自动提交；卡片 id、钱包、手动/自动计数、学习状态和 LessonTaught 次数不因蜘蛛改变；拖拽时显示由玩家评价的提示 |
| 普通卡已计划随机代答，同一卡 id 刷新成教学按钮，分别在等待/靠近/落脚阶段 | 取消计划；`SpiderPress` 继续拒绝教学评价；无结算反馈。视觉检查是否仍有腿部惯性动作，不得将其视为成功评价 |
| 拖拽已伸手、尚未按下，页面刷新为教学评价 | 落下时 `SpiderHandPress` 拒绝；不学习、不结算、不换题 |
| 普通非教学卡，分别猜对/猜错、随机代答/拖拽代答 | 保留原随机自动结算及拖拽手动结算，包括原连击、罚金、计数和刷新行为；不要强行让 AI 总猜对 |
| 复核卡、特殊模式、未解锁/能力不足、quiet、Host 阻塞 | 保留各原有 blocker；不得因本补丁变为可自动回答 |
| 真人在教学卡点击“猜对了／猜错了”或按 1/2 | 原 `OnAnswer → TryEvaluateTeachingGuess` 仍可执行，不被蜘蛛保护影响 |

本交付不标记策划条目或全局原则完成；统一编译和截图由整合/主聊天负责。
