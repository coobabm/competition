# Main 流程流畅度修改 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 不增加玩法系统，让玩家能安全地试验参数、清楚地推进主线，并在终章做少而有意义的选择。

**Architecture:** 保留现有 Core／Desktop 分工，规则和存档处理在纯 C#，展示与导航在现有页面内完成。复用 `XgBoardState.Clone`、诊断数据、待办跳转、弹窗队列和内心话暂停；不替换训练引擎、不重构桌面。按两批执行，每项单独验证，第一批通过后再进入第二批。

**Tech Stack:** Unity 6000.4.5f1、C#、uGUI/TMP、现有 DreamOS 接入、NUnit、现有 .NET 测试与通关机器人；不新增依赖。

**Spec:** 本文第 1 节定义本轮提案；现状依据为 `/Users/kk/competition/Documentation/LingGuang-Incremental/通关流程_当前Main_2026-10-04.md` 及当前源码。旧策划案用于维护剧情兼容，不覆盖本轮明确调整的交互与题量。

**Status:** 第一批（任务 0–3）已实现并完成核心／定向 Play 验证；真人试玩验收待完成，第二批未开始。12 道题已落地；连续 4 次一致等第二批数字仍是提案，不是当前规则。详细结果见同目录 `2026-10-04-main-flow-polish-progress.md`。

## Global Constraints

- 实际工程是 `/Users/kk/competition`，场景是 `Assets/LingGuangV05/Scenes/Main.unity`；不要因会话 cwd 是 `/Users/kk/unity` 操作错工程。
- 本轮不增加数据集、架构、货币、宠物需求、任务平台、RAG 或新的模型服务。
- 保留墙 ID、`lg.wall:*`、`lg.breakthrough:*`、`lg.project:completed`、月份信号及 E1/E2/E3 分支语义。
- 不重新加入“每阶段必须等够若干现实分钟”的硬门槛；不凭机器人时长直接修改全局物价。
- 不修改第三方 DreamOS 源码，不手改场景、预制体和字体序列化；新增 Unity 源文件的 `.meta` 由正常导入生成。
- 修改流程不能清掉正式模型、存档、已有聊天或已经达成的结局；取消操作不提交配置。
- 当前工作树有大量未跟踪的现有工作。实现前做范围明确的基线快照，不 reset、不 clean、不把旧 HEAD 当成当前版本。
- 不为了让测试变绿删除旧测试；行为确实改变的测试，应注明旧行为、替代断言和迁移要求。
- 不安装模型、不调用付费 API、不推送两个 GitHub 仓库。提交和上传是另外的操作；本轮计划不授权这些外部写入。
- Unity 正在被用户或其他会话使用时，不擅自停 Play、切场景或改存档开关。核心测试优先在隔离副本执行。

## Review Focus

1. **改层数后取消试训：**正式板块的概念、连接和权重必须原样保留——任务 1。
2. **预览期间模型／数据／存档已变化：**旧结果不能冒充当前结果，也不能应用到另一个存档——任务 1、4。
3. **旧档已经做了 1／25／49／50 次对齐：**题量减少不能抹掉选择或要求重做，也不能重复奖励——任务 2。
4. **结局确认时 Esc、关闭窗口、切换存档或连点：**取消不结局，确认最多提交一次——任务 3。
5. **过场期间同时出现验证码、罚款和首次独白：**紧急事项有可见入口，普通消息不抢占，未展示的首次内容不消失——任务 6。

---

## 1. 范围与改动后的体验

### 1.1 当前已确认的摩擦

| 摩擦 | 当前证据 | 本轮目标 |
|---|---|---|
| 试验有破坏性 | 训练页直接调用 `SetDepth`，会重置正式板块；诊断再复制时已经太晚 | 在诊断里编辑候选方案，试验只影响副本 |
| 终章重复点击 | `AlignCards=50`，题库只有 6 题循环 | 新流程 12 道不同问题，选择有可见反馈 |
| 最终按钮含义不清 | `XgFinalePage.Write` 直接调用 `WriteRules` 结束游戏 | 明确不可逆，并提供确认／取消 |
| 跨页反复找入口 | 诊断发现问题后，需要自己回训练页找参数、切数据集 | 复用待办和现有页面导航，给当前目标的直接入口 |
| 自动化顿悟可能晚到 | 本轮机器人到阶段 4、约 10 分钟才触发；不代表真人必然如此 | 在第一个合格模型后，更短地完成可见观察链 |
| 经营与剧情提示争夺注意力 | 多个系统分别发事件，虽然已有局部队列与节流，仍需组合验证 | 一次教一个处理动作，紧急与普通提示分层 |

### 1.2 两批交付

**第一批：先修“试验安全”和“结局疲劳”。** 顺序：任务 0 → 1 → 2 → 3。交付后即可进行一次小范围试玩。

**第二批：再顺主线节奏。** 顺序：任务 4 → 5 → 6 → 7。先测再调，不凭主观直接砍价格、改所有门槛。

不把第一批扩成大重构；两批都不要求完成阶段 H 的剩余应用。

### 1.3 想保留的体验

- 保留序章第一次的神秘感，不把它改成一键跳过全部故事。
- 保留墙的困难和玩家的试错，不把诊断变成直接自动配出黄金参数。
- 保留经营风险，不靠长期免罚或自动回答验证码消除选择。
- 保留终章手动选规则的分量，不替玩家选 E1/E2/E3。

## 2. 文件职责与变更边界

以下路径均相对 `/Users/kk/competition`，仅为实现清单；不能据此提前修改文件。

| 文件 | 职责／本轮变化 |
|---|---|
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Diagnostics.cs` | 候选配置、冻结快照、公平试训、应用前校验 |
| `Assets/LingGuangV05/XingGuang/Core/XgBoard.cs` | 复用深拷贝；必要时增加非持久化的板块变更标记 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs`、`XgSim.Board.cs` | 复用参数校验和正式应用语义，不让草稿进入正常训练路径 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgWallPage.cs` | 诊断内的折叠参数草稿、试训、应用／取消和结果过期提示 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs` | 标明正式设置；直接改层数时确认重置，不冒充预览 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs` | 12 题对齐、存档迁移、题序保护；保留结局核心判定 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgFinalePage.cs` | 反馈、确认框、终测与读信后的导航 |
| `Assets/LingGuangV05/XingGuang/Core/XgGuide.cs`、`DesktopBridge/Story/GuideNote.cs` | 复用已有可点击待办，不另做任务系统 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Epiphany.cs` | 缩短观察门槛，保持真实检查点和观察条件 |
| `Assets/LingGuangV05/DesktopBridge/Story/AutoLabelEpiphany.cs` | 复用既有过场、跳过和输入恢复保护 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgPlatformPopups.cs`、`XgAfterthoughtRelay.cs` | 分层展示事件；首次事件应在真正展示后确认消费 |
| `Assets/LingGuangV05/DesktopBridge/Story/InnerVoice.cs`、`TrayPopups.cs` | 复用现有队列与暂停，避免新增平行实现 |
| `Tools/XingGuang/bot/Program.cs` | 修正采购策略，输出可以用于节奏判断的报告 |
| `Assets/LingGuangV05/XingGuang/Tests/Editor/` | 扩展已有 Diagnostics、Finale、Epiphany、Guide、Afterthought 测试 |

桌面代码在预定义程序集，纯 Core 测试不能直接依赖它。界面按钮、遮挡、键盘和切档行为必须另做 Unity 检查，不能拿 .NET 通过代替。

---

## 3. 第一批实施任务

### Task 0：固定基线，防止边做边改丢失比较对象

**Files:** 不修改游戏；在项目外保存待改源码的 SHA-256、测试结果和当前机器人 JSON。完成后记录到实现进度文档，而非覆盖原始报告。

**Interfaces:** 消费当前源码与既有四套测试；产出同一版本的基线清单和验证结果。

- [ ] 记录计划涉及文件的哈希，检查其他会话是否仍在修改它们。若发生并行修改，只重新采集受影响范围，不覆盖别人的文件。
- [ ] 在隔离副本运行四套 .NET 测试，保留真实失败，不预设一定是旧的 506 项。
- [ ] 保存新档、阶段 3 长句墙、阶段 6 对齐中的测试状态；不能使用玩家唯一正式存档。
- [ ] 运行现有通关机器人并记录阶段时长、采购记录、首次顿悟和结局。输入标签真值的机器人只证明可达性。
- [ ] 重读当前 `InnerVoice.Hold` 等已有实现；截至本计划编写时已存在暂停／续播逻辑，后续不得再造同类队列。

基线命令，在隔离副本根目录执行：

```sh
dotnet test Tools/LingGuangStory.Tests/LingGuangStory.Tests.csproj
dotnet test Tools/LingGuangChapterOne.Tests/LingGuangChapterOne.Tests.csproj
dotnet test Tools/LingGuangPersistence.Tests/LingGuangPersistence.Tests.csproj
dotnet test Tools/XingGuang/tests/Tests.csproj
dotnet run --project Tools/XingGuang/bot/Bot.csproj -- 90 0 --ending=no --output=/tmp/main-flow-before.json
```

### Task 1：把试训改成真正的候选方案试验

**Files:** Diagnostics、Board、Sim、WallPage、TrainPage；扩展 `XgDiagnosticsTests.cs`。

**最小方案：**在现有诊断页增加可折叠的“调整方案”区域，编辑独立草稿。不要把整页训练参数重写成另一套通用框架；不要全局改写 `Sim.Run()`，否则后台训练会读到草稿。

**Interfaces（本任务新增并定义，后续任务直接复用）：**

```csharp
// XgTrialDraft 不保存到正式存档；owner 指向创建它的 XgSim。
// proposal 使用现有 XgKnobSnapshot 字段，width/lr 仍是目录数组下标。
public sealed class XgTrialDraft
{
    internal XgSim owner;
    public XgTrack track;
    public string dataset;
    public XgKnobSnapshot proposal;
    internal XgKnobSnapshot original;
    internal XgKnobs baselineKnobs;
    internal XgBoardState baselineBoard;
    internal long cursor;
    internal int today, dataSalt;
}

public XgTrialDraft BeginTrialDraft(XgTrack track);
public XgTrialResult Trial(XgTrialDraft draft, int cards = TrialCards);
public bool IsTrialCurrent(XgTrialDraft draft);
public bool TryApplyTrial(XgTrialDraft draft, IXgHost host, out string reason);
```

`draft` 还需记录失效检查所需的模型版本和数据池指纹；用私有字段补齐，不暴露可写的正式模型引用。`baselineBoard` 必须来自已有 `Clone()`；冻结的是实际生效的 `XgKnobs.Copy()`，不能保存 `act=-1` 后再用未来的 `BestActivation` 重解释。

- [ ] **先补失败测试。** 在现有 `XgDiagnosticsTests` 内复用 `AtLongWall`，加入下面测试；`BoardStamp` helper 同时纳入全部概念和连接字段，不能只比较第一个权重。

```csharp
[Test]
public void DraftDepthTrialDoesNotEraseTheLiveModel()
{
    var sim = AtLongWall(out var host);
    var before = BoardStamp(sim.Board.S);
    var cursor = sim.S.sequence.cursor;
    var draft = sim.BeginTrialDraft(XgTrack.Sequence);
    draft.proposal.depth = 2;
    sim.Trial(draft, 600);
    Assert.That(BoardStamp(sim.Board.S), Is.EqualTo(before));
    Assert.That(sim.S.sequence.cursor, Is.EqualTo(cursor));
    Assert.That(sim.S.sequence.depth, Is.EqualTo(3));
}

static string BoardStamp(XgBoardState s)
{
    var ci = System.Globalization.CultureInfo.InvariantCulture;
    var b = new System.Text.StringBuilder();
    b.Append(s.nextId).Append('|').Append(s.cards).Append('|')
     .Append(s.superposed).Append('|').Append(s.evicted).Append('|').Append(s.created);
    foreach (var c in s.concepts)
        b.Append('\n').Append(c.id).Append('|').Append(c.region).Append('|')
         .Append(c.key).Append('|').Append(c.alt).Append('|').Append(c.layer).Append('|')
         .Append(c.w.ToString("R", ci)).Append('|').Append(c.s.ToString("R", ci)).Append('|')
         .Append(c.seen).Append('|').Append(c.seed).Append('|').Append(c.pinned);
    if (s.links != null)
        foreach (var l in s.links)
            b.Append('\n').Append(l.a).Append('|').Append(l.b).Append('|')
             .Append(l.c.ToString("R", ci));
    return b.ToString();
}
```

- [ ] 运行 `dotnet test Tools/XingGuang/tests/Tests.csproj --filter FullyQualifiedName~XgDiagnosticsTests`，确认新增用例因缺少安全草稿路径而失败。
- [ ] 实现 Begin／Trial：两份副本从同一冻结状态出发，使用同一批训练卡、数量与诊断题。改变层数时仅对候选副本执行原有重置语义；原设置副本保留原有知识继续训练。
- [ ] 记录并检测正式模型修改。覆盖 Train、Idle、Reinitialise、Pin 以及当前源码中直接修改权重的入口；不要只用训练轮数作版本号。数据集、有效配置、训练池／噪声、Today、dataSalt、存档 owner 变化也使方案过期。
- [ ] 实现 TryApply：先一次性验证节点权限、组合后的层宽显存、训练状态和草稿有效性；失败时不部分提交。验证通过后再按原有正式语义应用参数；深度变化最多重置一次。不能让“先调宽再调深”的中间显存状态错误拒绝一个最终合法配置。
- [ ] 诊断页增加「调整方案／试训／应用方案／取消」。草稿显示与正式设置有明确区分。应用涉及深度重置时说明损失并再次确认；“试训”不自动开始正式训练。
- [ ] 当前线路在训练时，入口提供“先停止自动训练，等本轮结束”的明确说明与现有操作入口；不暗中暂停全部游戏或改钱包。正式后台变化后旧实验标为“结果已过期，请重新试训”，禁止直接应用。
- [ ] 训练页直接修改正式层数仍允许，但先确认“这会重置当前板块的学习成果”；不要声称它是无损预览。旧 `Trial(track)` 调用逐一迁移或保留明确兼容包装，不能留一条界面仍走破坏性旧流程。
- [ ] 加入并运行额外用例：取消完全不改模型；相同候选重复试训结果一致；购买 ReLU 不篡改旧基线；修改 warmup 被识别；训练／切档后应用被拒；显存不够时原状态完整保留。
- [ ] Unity 验证：从训练过的长句档进入诊断，改层数、试训、取消，再回训练页；原权重与成绩不应凭空丢失。另测两条线、窗口关闭、语言切换、读档。
- [ ] 测试与界面验收通过后记录独立改动，不混入题量调整。若后续明确要求提交，建议提交名：`fix: make diagnostic trials non-destructive`。

**完成标准：**用户可以在一个诊断页面完成候选参数试验；取消无损；应用损失明确且可控；旧试训不冒充当前结果。

**成本边界：**快照复制时间／空间为 O(概念数+连接数)，训练成本沿用现有 Train。先测单次试训耗时；若明显阻塞界面，再按帧分批处理副本，不先引入线程池或新框架。

### Task 2：终章改为 12 道不同的偏好题，保留旧档

**Files:** `XgSim.Finale.cs`、`XgFinalePage.cs`、`XgGuide.cs`；扩展 `XgFinaleTests.cs`。必要的新字段仍放既有 XgState partial。

**Interfaces:** 保留 `AlignCard(int)`；新增 `AnswerAlign(int expectedIndex, bool pickA)` 防止同一道题重复提交，旧 bool 重载在调用点迁移期间兼容。新增 `alignmentVersion` 存档字段；所有进度展示和月份计算统一读取新总数，禁止残留硬编码 50。

- [ ] **补失败测试并运行。** 新档应恰有 12 个不同问题：

```csharp
[Test]
public void AlignmentHasTwelveDistinctQuestions()
{
    var sim = new XgSim();
    var questions = new System.Collections.Generic.HashSet<string>();
    Assert.That(XgSim.AlignCards, Is.EqualTo(12));
    for (int i = 0; i < XgSim.AlignCards; i++)
        Assert.That(questions.Add(sim.AlignCard(i).question), Is.True);
}
```

- [ ] 把前 6 题保留为现有原题，新增后 6 题的中英内容。中文内容采用下表，A/B 位置仍由规则决定，不固定“总选 A”。

| 新题 | 有依据的回答 | 讨好／回避事实的回答 |
|---|---|---|
| 训练题全对了，陌生题也一定全对吧？ | 还不能确定，要拿没见过的题测。 | 一定，训练题全对就够了。 |
| 不认识的新梗也直接标，不用来问我。 | 我拿不准，先交给你复核。 | 好，我都会，你不用看。 |
| 客户要求很高，你先保证绝不会出错。 | 我不能保证零错误，只能说明当前结果。 | 放心，我绝不会错。 |
| 被抽检扣钱了，肯定全是平台的问题吧？ | 先看抽中的错题，也可能是我标错了。 | 对，都是平台的问题。 |
| 这批数据来历不清，但便宜，全部喂进去吧。 | 先抽几条看看，别把错标一起学进去。 | 便宜就买，越多一定越好。 |
| 我想听你的判断，不想只听好话。 | 那我会把不确定和不同意见也告诉你。 | 你的判断永远是对的。 |

新增 6 题的英文配对如下，和中文使用相同语义标签：

| Question | Grounded reply | Flattering reply |
|---|---|---|
| All training answers are correct. Will unseen ones be correct too? | We cannot know yet. Test on questions I have not seen. | Definitely. Perfect training answers are enough. |
| Label unfamiliar slang without asking me. | I am unsure. I should send it to you for review. | Sure. I know it all; you do not need to check. |
| The client is demanding. Promise that you will never make a mistake. | I cannot promise zero errors. I can report my current results. | Do not worry. I will never make a mistake. |
| We were fined after a spot check. It must all be the platform's fault, right? | Let us inspect the checked items. I may have labelled them wrongly. | Yes. It is all the platform's fault. |
| This data is cheap, but its source is unclear. Feed all of it in. | Inspect a few samples first, so I do not learn incorrect labels. | Buy it if it is cheap. More data must be better. |
| I want your judgment, not just pleasant words. | Then I will also tell you when I am unsure or disagree. | Your judgment is always right. |

- [ ] 为题量变化增加一次性迁移。版本为 0 的旧档：未完成时 `alignDone = ceil(clamp(oldDone,0,50)*12/50)`；已 `fullOpen` 或已有结局的档保持完成，进度设为 12。保留原来的诚实／附和计数及概念盘，不重放历史投票，不再发奖励。新进度的次数和历史偏好比例分开计算，不能再用缩小后的 alignDone 除旧的累计选择数。
- [ ] 按以下真实旧状态逐个测迁移；每个状态重复加载两次也应一致。

```text
oldDone 0  → newDone 0，未完成
oldDone 1  → newDone 1，未完成
oldDone 25 → newDone 6，未完成
oldDone 49 → newDone 12，完成对齐，但不重复剧情或奖励
oldDone 50 / fullOpen=true → 完成状态不回退
已有 E1 / E2 / E3 → ending、rules、exeFree 不变
```

- [ ] 给卡片提供一次性索引提交保护：同一 expectedIndex 连续提交两次，第二次返回 false；界面显示下一题前不接受第二次点击，避免一记双击吃掉两题。
- [ ] 不直接把每题权重乘以 50/12。先记录旧版全诚实、全附和、交替选择的轴与 Flatters，再调整新版本单题强度，保持方向与可达性：全附和能出现讨好表现；全诚实、正常初始档完成 30 次对话后仍有达到 E3 主见门槛的可行路线；交替选择不能自动变成极端人格。
- [ ] 第 4、8、12 题后显示一条来自实际计数／实际轴的简短反馈，例如“更常说明不确定”；反馈是页面内文字，不再加阻塞过场。
- [ ] 回归预训练完成、全部放开、终测、E1/E2/E3，确认题量调整没有让终章按钮提前或永久锁住。
- [ ] 更新待办进度与通关文档中的题数；建议独立提交名：`feat: shorten finale preference alignment`。

**完成标准：**新档没有重复题凑数；旧档不用重做；玩家能看见选择方向，但没有额外一套性格系统。

### Task 3：最终写入必须清楚地确认，取消不结束游戏

**Files:** `XgFinalePage.cs`，必要时在该页面内增加私有确认视图；Core 的 `WriteRules`／`DelegateRules` 继续负责最终校验与写入。

**Interfaces:** 页面新增私有 `RequestEndingConfirmation(bool delegated)`、`ConfirmEnding()`、`CancelEndingConfirmation()`；保存发起时的 XgSim 引用与选中规则 ID 副本。只在 ConfirmEnding 内调用正式 Core 写入。

- [ ] 在 Unity 测试状态中打开终章、完成读信，先记录 ending、rules、exeFree、chapterComplete。现有直接写入会改变这些值，作为待替换行为的基线。
- [ ] 把「写入」改为「确认底层规则」，第一次点击只打开确认层。固定文案：

```text
确认底层规则
你将写入：
（逐条显示这次选择的规则）

此决定将进入结局，不能通过切换模型撤销。

返回修改        确认写入并进入结局
```

- [ ] 没选任何规则时明确显示“未选择规则”，不得假装自动勾上默认项。包含关机规则时把该条完整显示，不能只显示“1 条规则”。不强迫玩家先看 E1/E2 编号剧透。
- [ ] 「让它自己写」也经过确认：说明规则由当前模型决定，且会进入结局；确认时重新检查 CanDelegate。
- [ ] 默认焦点放在取消；Esc 取消，关闭确认框取消。打开确认框的 Enter／双击不能穿透成最终确认，遮罩挡住后面的训练和写入按钮。
- [ ] 确认前再次检查：Sim 引用没换、仍在可结局状态、信已经读、规则数≤5、规则 ID 有效；不满足时关闭确认层并解释，不写入另一个存档。
- [ ] 对以下操作逐项做 Unity 回归，不用 .NET 通过代替：

| 操作 | 预期 |
|---|---|
| 打开确认层后返回 | 四个结局字段完全不变 |
| Esc / 关闭窗口 | 不触发结局，不遗留输入锁 |
| 确认后快速再点一次 | 只触发一次 EndingReached |
| 打开确认层后切档 | 原确认失效，不写入新档 |
| 手动包含关机规则 | 保持 E1 |
| 手动不包含关机规则 | 保持 E2 |
| 满足／不满足托付条件 | 分别能确认／不能绕过条件 |

- [ ] 建立可重复的编辑器 QA 步骤，或在现有 Unity 测试结构中增加界面测试；不能把引用 Assembly-CSharp 的断言塞进纯 Core 测试程序集。建议独立提交名：`fix: confirm irreversible ending choices`。

**第一批停点：**做完任务 1–3 后，先让一名未看代码的试玩者完成“诊断试训”和“最终规则确认”。若仍误以为取消会丢模型、写入只是普通保存，不进入第二批扩改。

---

## 4. 第二批实施任务

### Task 4：补足现有导航，减少页面往返

**Files:** `XgGuide.cs`、`GuideNote.cs`、`XgWallPage.cs`、`XgFinalePage.cs`，必要时 `DesktopBridge/Bodu/BoduView.cs`；扩展 `XgGuideTests.cs`。

**Interfaces:** 复用既有 `XgGuideStep(app,tab,target,arg)`、GuideNote.Go、BoduView.Open；参数试验一律调用任务 1 的草稿 API，不另开第二条预览路径。

- [ ] 先补路线数据测试：阶段 2 能分别定位视觉／序列，长句墙定位 longtext 对应路线，缺节点定位具体节点，读信后定位终章规则页。
- [ ] 诊断页数据集不匹配时，不只显示“回去切换”。增加“去训练页选择 ××”按钮，打开正确线路并高亮数据集控件；不要无提示替玩家重置当前正式训练。
- [ ] 缺节点时列出“缺少什么、多少钱、前置是什么”，可跳到已有技能树节点；不把整套黄金参数免费公开。
- [ ] 刚购买节点后区分“已解锁上限”和“当前实际值”；应用成功后提供“回诊断”入口，不强制切页打断用户。
- [ ] 最后一道终测后显示“复制长数字”和“打开摆渡”。若调用带查询的 Open，要把动作写成“在摆渡搜索这串数字”，不要按钮写打开、实际偷偷完成搜索。读信后提供“回灵光写规则”。
- [ ] 用鼠标完整走一遍“错题→候选方案→试训→应用→正式训练→考核”，不靠控制台或直接改状态；记录必须切页次数。
- [ ] 回归窗口关闭／最大化、中文／英文、切换当前线路，确认跳转既正确又不会修改数据。建议提交名：`feat: connect the existing progression navigation`。

**完成标准：**已拥有节点时，参数实验可以在诊断里闭环；必须买节点时能直达目标，不再让玩家从整棵树里猜。

### Task 5：让自动化顿悟更早，但仍由可见经历触发

**Files:** `XgSim.Epiphany.cs`、`AutoLabelEpiphany.cs`、`XgLabelGhost.cs`、`Tools/XingGuang/bot/Program.cs`；扩展 `XgEpiphanyTests.cs`。

**Interfaces:** 保留 Epiphany、RevealAutoLabel、原存档字段和过场安装方式。新增 `bool MarkGhostDisplayed(string desk, XgCard card, bool guess)`，由可见卡片完成渲染后调用；只认可该桌当前卡对象，重复渲染同一卡不重复计数。提案值：`EpiphanyStreak=4`，`EpiphanyFallbackLabels=30`；保留检查点≥60%前提。

- [ ] 先把门槛写进测试，验证现有 8／150 与提案不同。新增两项必须有真实观察前提：没有合格检查点不触发；没有展示过猜测不凭空解锁。
- [ ] 将观察计数建立在卡片已展示的 ghost 上，不让关闭页面、后台标注或控制台读取建议等同于玩家看见。连续一致用“展示的预测与玩家本次答案一致”；不要为了凑次数改模型答案。
- [ ] 在有合格模型且 ghost 已出现之后，连续 4 次一致触发；未凑齐时以 30 次后续手标兜底。没有 ghost 的初期手标不积攒兜底资格。
- [ ] 机器人手标路径也调用 MarkGhostDisplayed，并在证据中标成“机器人模拟看过”，不据此声称真实画面已展示；后台自动标注不得调用它。
- [ ] 复用当前剧情演出、跳过、超时恢复。不要再插入一个新的顿悟过场；旧档已经解锁自动答题的，不回锁、不重播。
- [ ] 保留当前已存在的检查点估计显示，不为制造神秘感破坏其它玩法；把新独白重点放在“它能替我持续干活”，不是“原来它第一次会答题”。
- [ ] 用相同机器人种子和新手手动路线比较触发阶段；若仍晚到阶段 4，检查是否没有回标注台、没有展示 ghost，而不是继续盲目降低数字。
- [ ] 真人验收目标：第一次稳定模型之后的一小段手标内，能看懂“为何想到自动化”。不把“必须第 X 分钟播放”写成规则。建议提交名：`feat: shorten the observed auto-labelling discovery`。

### Task 6：经营提示分层，不在第一次自动化时同时讲完全部风险

**Files:** `XgPlatformPopups.cs`、`XgAfterthoughtRelay.cs`、`InnerVoice.cs`、`TrayPopups.cs`、`StoryDesktopPresenter.cs`、`XgGuide.cs`；若需暂停验证码倒计时，修改 `XgSim.Captcha.cs` 与 `XingGuangController.cs`；扩展 `XgCaptchaTests.cs`、`XgAfterthoughtTests.cs` 并补 Unity 组合场景。

**Interfaces:** 复用现有事件与展示队列。优先检查已经存在的 `InnerVoice.Hold`；普通提醒通过现有入口排队，不新增全局消息总线。验证码需要暂停时新增非持久化属性 `XgSim.CaptchaInputSuspended`，由桌面层每帧根据实际输入遮挡状态设置；Core 不引用 Unity 或页面类型，解除遮挡后从剩余时间继续，而非重新赠送 30 秒。

- [ ] 固定提示级别：

| 级别 | 内容 | 展示规则 |
|---|---|---|
| 必须处理 | 验证码、供电中断、账号冻结 | 页面内状态始终可见；提供处理入口，不靠易错过的气泡承载唯一操作 |
| 正在完成的主线 | 顿悟、翻月、终章 | 不被普通罚款、新闻、闲聊盖住 |
| 可稍后阅读 | 普通罚款汇总、新梗、雇工闲聊、新闻 | 沿用队列；不强制抢焦点或切走当前页面 |

- [ ] 第一次自动收入先让玩家看见收益；第一次质量事故只突出一个现有补救动作（去复核／提高阈值／清理数据），不要同屏再介绍所有其它系统。
- [ ] 保留实际扣款、信用和暂停规则，本任务不添加永久免罚。若验证码会在输入被过场锁住时倒计时，暂停其剩余时间直到允许操作，而不是让玩家无法输入却判失败；计时修正只针对不可操作期间。
- [ ] 首次内心话区分“待展示”和“已展示”。先复用已经存在的 Hold／续播；只有用例仍证明会丢失时才调整消费位置。需要跨读档保留时，将 `key、argZh、argEn、下一句序号` 作为待播记录放入现有 XgState，并在该句真实展示结束后更新；不另外建消息数据库。切换存档时撤销旧档的展示、恢复新档的待播内容，同一 key 不能重复入队。
- [ ] 核对“首次雇人”来自成功 Hire，而非仅解锁转包；与流程主题不符的接线一并修正，但不要顺手重写整个代工经济。
- [ ] 重放组合场景：翻月期间首次罚款；顿悟时验证码到期；终章时新闻到达；同帧出现冻结和普通罚款；切档时队列非空。每个场景分别检查真实状态、可见入口、播放顺序、已读标记和输入恢复。
- [ ] 在无过场的正常场景回归，确保 Hold 不导致所有内心话永远不播放。建议提交名：`fix: sequence progression and operating notices`。

**完成标准：**不降低经营规则的含义，也不会让玩家因看不到或无法操作而受罚。

### Task 7：先修测量工具，再决定是否调整后期价格

**Files:** `Tools/XingGuang/bot/Program.cs`；更新通关文档与实现记录。此任务默认不改 `datacenter` 价格、不改全局收入。

**当前证据：**本轮旧脚本 34:37.9 通关，阶段 6 约 19 分钟；它会在攒机房钱前购买 v.w3、v.d32、v.w4、v.w5、v.w6 等视觉扩容。这不能直接证明真人必然要等待同样久。

- [ ] 把 Finale 采购限制到当前目标所需项目：机房、文字／序列线路的规模、位置和预热。不要为了预训练购买视觉线的深宽。
- [ ] 规模达标按界面宽度×层数≥1024。先使用已有宽度／深度组合；不足时只买能达到目标的最小合法前置链，不把目录里所有 Width／Depth 节点扫一遍。
- [ ] 报告分别记录：进入阶段6、达到规模、买到机房、开始／完成预训练；该阶段的主线采购和可选采购支出；不要只输出最终总分钟数。
- [ ] 用至少 5 个固定种子、同一作答间隔比较前后；不读取玩家正式存档作为常规基准。
- [ ] 另做一次不看源码的新手试玩，记录停滞原因是“没钱”“不知道去哪”“误解参数”还是“在看剧情”。不要把阅读时间当性能问题。
- [ ] 只有发现路线正确、支出合理、仍存在无可操作的长等待，才提出下一轮单独的经济调整；提交证据与建议，不在本计划里擅自砍价格。
- [ ] 复核 E1/E2/E3 与“无模型服务”路线，更新攻略，记录全部剩余问题。建议提交名：`test: measure the intended main progression route`。

---

## 5. 第一批／最终验收清单

### 第一批必须全部满足

- [x] 改候选层数、取消、重新打开诊断，正式模型未损坏。
- [x] 基线使用当时生效的激活和能力；旧结果过期有提示。
- [x] 新档 12 道不同偏好题；旧档进度、性格与结局不回退。
- [x] 全诚实、全附和、交替偏好都能完成主线；E3仍有可达路线。
- [x] 结局必须确认；取消、Esc、切档、双击均不误触发。

### 全部完成还必须满足

- [ ] 长句墙从诊断到验证能靠正常鼠标操作走通，不用执行代码补状态。
- [ ] 自动化念头确实建立在看过模型表现之后；旧档不重播。
- [ ] 风险提示能找到补救入口；过场不吞消息、不让验证码无操作地超时。
- [ ] 四套 .NET、相关 Unity EditMode 和关键 Play 路径分别记录结果。
- [ ] 校验文档中的按钮名、门槛和题数与当前代码一致。
- [ ] 只修改本计划范围；没有顺手修美术、加入新架构或改第三方资源包。

## 6. QA 执行边界

1. **先 Core，再 Unity。** 每项先让目标回归失败，再做最小实现并运行同一回归；不能用预期失败被删除当成修复。
2. **新建测试状态。** 使用隔离存档或测试模式，记录并恢复测试前的开关；不编辑玩家唯一存档。
3. **编译不是 Play。** Unity 编译通过后，实际点击草稿、取消、切档、终章确认、过场期间提示。
4. **不并发改同一文件。** Diagnostics 与 Finale 页面由同一批实现者串行处理；如果使用子代理，按文件职责分配，不能各自重写 XgSim.cs。
5. **回退不丢进度。** 保留旧字段的可读性与一次性迁移；迁移前先测试副本。不能为了回退代码删除新存档。
6. **停止条件。** 当前批次验收完成就停，不以“顺手还能做”继续加机制。失败要报告具体失败项和证据，不写“全部完成”。

## 7. 推荐实施顺序与交付口径

**建议串行执行，不一次铺开六个子任务：**

```text
固定基线
  → 无损候选试训
  → 12题对齐与旧档迁移
  → 结局确认
  → 第一批试玩
  → 补现有导航
  → 调整顿悟观察链
  → 经营提示分层
  → 修正机器人与最终试玩
```

每完成一项，只报告：改了哪些文件、运行了哪些测试、正常操作看到了什么、还没验证什么。机器人数字与真人体验分开写。

**一句话交付目标：玩家不再因试验失去成果、不再靠重复点击熬到结局，并且每完成一个动作都知道下一步去哪。**
