# 给 GPT（修改灵光游戏）：连对特效整合补丁

可以直接整段粘贴给 Codex。

## 要做的事

在 `competition-claude-integration-20261020` 打上 Claude 的补丁：

```
git apply --ignore-whitespace "C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261020/claude-streak/claude-streak-celebrate.patch"
```

已经用 `git apply --check` 对你当前的工作树预检过（21:30 左右），可以干净打上，没有动你的文件。如果之后你又改了下面这些文件导致冲突，以你的为准，只把 Claude 的增量合进去即可。

补丁改动的文件：
- 新增 `Desktop/XgJuice.Celebrate.cs`（含 `.meta`）
- 修改 `XgJuice.Streak.cs`、`XgJuice.cs`
- 修改 `XgDeskGamePage.cs`、`XgLabelPage.cs`，各 1 行
- 修改 `XgSim.cs`、`XgRelics.cs`，各 2 行
- 修改 `XgRelicTests.cs`

## 补丁内容

用户在 Claude 的 HTML 预览上逐条确认过这些：
- 字不要太大、别挡题目
- 加画面抖动
- 加彩带、烟花细条白光、颜色微弱变化
- 金币飞入要「不灵不灵」
- 去掉蜘蛛喊话

1. **新增装饰层 `XgJuice.Celebrate.cs`**：和粒子池分开，上限 `DecoLimit = 360`，不会挤掉普通连击的粒子。
   - `Streamers`：彩带炮，从题卡区两个下角喷出，彩带会翻面、飘动、下落；`StreamerRain` 是 ×5 专用的满屏彩带雨。
   - `Firework`：先亮一下核心光晕，再炸开一圈细长的白光条（带当档色拖尾），最后留下闪烁星点。
   - `BlingCoins`：金色渐变的「¥」边飞边闪，沿路洒星点，落到「经费」时迸星芒、播硬币声，经费那里弹一下。
   - `FloatFancy`：逐字改顶点颜色，做上亮下深的渐变、扫过的高光和 ±10° 的色相流转，出场有弹性。
   - `ShakeLater`：延时余震。
   - 规则：窗口隐藏、`MotionDisabled`、`GamePerformance.Low` 时一律不画；`Reduced` 时数量减半；`Clear()` 时全部清空。你的 StreakFeedbackChecks 里检查的「减弱特效无动画」「隐藏时清空」都继续成立。
2. **`StreakUp`**：
   - 升档大字缩小（30 + 10k），挪到 `center` 上方 `CalloutLift = 175` 的位置，不再挡题。
   - 每一档都有彩带、烟花、金币和震屏，按 ×2 到 ×5 递增：

     | 档位 | ×2 | ×3 | ×4 | ×5 |
     |---|---|---|---|---|
     | 彩带 | 16 | 26 | 40 | 56 |
     | 烟花 | 1 | 2 | 3 | 5 |
     | 金币 | 4 | 6 | 9 | 12 |
     | 震屏幅度 | 6 | 10 | 15 | 22 |

   - ×4、×5 有余震，×5 再加满屏彩带雨和「满倍」。
   - 原来 ×4 的彩纸 Burst 改成了彩带。
   - **蜘蛛喊话已删除**。
3. **`StreakHit(at, m, wallet = null)`**：流光「×N」小字，飞 1–4 枚金币，轻微震屏。两处调用都传了 `WalletChip`。参数可选，你的检查脚本不用改。
4. **你的里程碑（20 / 50 连对）、两格蓄能、静音掐尾音、按键音层次都保留没动。**
5. **遗物「通宵赶工」**：你把默认节奏改成每 2 题升一档以后，它原来「每 2 题一档」的效果就和默认一样，`XgRelicTests` 因此失败。现在改为 `ComboStepNow => comboFastStep ? 1 : ComboStep`，也就是每题升一档、5 连对到 ×5。文案和测试同步改了。

## 验证情况

- 在 Unity 外用 Unity 6000.4.5f1 自带的 Roslyn 编译：XingGuang.Core、Assembly-CSharp、XingGuang.Tests 都是 0 个错误。StreakFeedbackChecks 编译也没有报错。
- 用 .NET 跑 XingGuang 测试：1399 个通过。失败的 245 个都是需要 Unity 运行时的测试，基线 `40d885e1` 上同样失败，没有新增失败。
- **没在 Unity 里跑过**。请你跑一次 `StreakFeedbackChecks` 和 EditMode，然后实机看常识判断连对到第 3、5、7、9 题，以及答错一次的效果。
- 参考分支：`claude/streak-integrated`（工作树 `competition-streak-integrated`）= 你 21:10 的工作树快照（`67d8e2bb`）+ 这个补丁（`305be982`）。HTML 预览在 `claude-streak/特效预览/index.html`，已经同步成你的「每两题升一档」节奏。

## 数值提醒（不用改，仅供参考）

按你的节奏、基础单价 ¥0.75/题，和改版前（+5%/题、最高 ×2、¥1.25/题）比：正确率 90% 的玩家收入约为原来的 1.43 倍，60% 的约为 0.83 倍。
