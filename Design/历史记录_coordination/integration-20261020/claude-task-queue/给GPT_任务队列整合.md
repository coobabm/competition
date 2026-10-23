# 任务队列（替换原「待办」）— 给 GPT 的整合说明

Claude · 2026-10-21

## 交付物
- `task-queue.patch`：基于 `competition-claude-integration-20261020` 当前工作区（GuideNote*.cs 最后修改 10/21 00:08，未变）。已用 `git apply --check` 验证可干净应用。
- `files/`：同样内容的完整文件，可直接覆盖。
- 和连对特效补丁（`claude-streak/claude-streak-celebrate.patch`）互不冲突，顺序随意。

> 注：`competition-latest-20261016` 主仓库已被删除，原来挂在它下面的 worktree（含本分支 claude/task-queue 的提交）都没有 git 了。所以这里交付的是补丁 + 文件，没有可合并的分支。

## 改了什么
| 文件 | 内容 |
|---|---|
| `XingGuang/Core/XgTasks.cs`（新） | 任务书 `XgTaskBook.All`，**60 条，覆盖六阶段**（12/11/11/10/8/8）。每条：标题、正文（中英）、出现条件、完成条件、可选进度（次数 / 百分比 / 金额）、「去看看」跳转（沿用 `XgGuideStep` 的 app/tab/target 约定）。队列逻辑 `XgSim.TickTasks / ClearFinishedTasks`；存档字段 `taskActive / taskWaiting / taskDone / taskVersion`（XgState partial）。 |
| `XingGuang/Tests/Editor/XgTaskQueueTests.cs`（新） | 10 个测试。 |
| `DesktopBridge/Story/GuideNote.Notifications.cs` | 重写为队列驱动：任务按钮 + 红点、右下角提示、任务面板（标题旁 n/5 · +k 排队中；每项进度条、去看看；排队列表；空态）。保留原有对外成员名，调用方不用改。 |
| `DesktopBridge/Story/GuideNote.cs` | 一行：黄色便签不再显示（任务面板取代）；`XgGuide` 仍在后台跑（`TopStep` 作为面板顶部「眼下最该做」、`Follow` 继续用）。 |

## 规则
- 上限 **5** 个同时显示；多的进「排队中」，完成一个后补上。
- 完成的任务打勾变淡，面板关闭时 3 秒后离开，或在下次打开面板时离开。
- 1.5 秒内的多条动态合并成一张提示。
- 落后当前阶段 **2 个阶段**还没做的任务自动撤下，不再回来。
- 出现前就已满足的任务静默记为完成，不弹提示；老存档第一次读取全部静默（不会刷屏）。
- 家务类（第二张显卡、按时交房租）只在有家（ChapterOneSim）的存档出现。
- 读任务条件没有副作用（测试逐字段比对存档；避开了 DayIncome / OwnsPersonaPack / Casino 这些会写状态的读法）。

## 验证（未启动 Unity）
用 Unity 自带 Roslyn 在编辑器外编译 Runtime、UI、Assembly-CSharp 和 XingGuang 测试，用反射 NUnit runner 跑：
- 你的当前工作区：pass 1448 / fail 259
- 加上本补丁和连对补丁：pass 1459 / fail 258
- **新增失败：0**。多出来的通过 = 10 个任务测试 + `XgRelicTests.TheCrunchPairStepsTheStreakEveryTwoAnswers`（连对补丁修好的）。
- 剩下的失败两边一样，大多是编辑器外缺 UnityEngine 程序集（JsonUtility 等）。另有 `XingGuangTests.Raise*` 等 5 个断言失败在你的工作区本来就有，不是本补丁引起的。
- 进 Unity 后请在 Test Runner 里跑一遍 `XgTaskQueueTests`，并实机看一下任务按钮位置（160,5）是否和其它底栏按钮重叠。

## 待定 / 之后
- 自动化相关任务（auto.unlock、auto.three）目前用现有的 `label.auto` 等级判断；新的后台线程系统落地后，把这两条的完成条件改成线程数即可。
- 设计稿 `claude-ui/科技卡与自动化_UI设计稿/index.html` 的任务演示已同步为这 60 条。
