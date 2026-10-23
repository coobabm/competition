# 上游 3df48181：忘言夜、纪事与训练侧栏只读审查

范围：`c5c63198..3df48181`（`d371a2b7`、`3df48181`），对照当前 `competition-integration-20261020` 未提交整合工作区。没有改工程源码，没有运行 Unity、提交或打包。报告中的行号分别指上游提交或明确标注的当前源码。

## 结论

“忘言夜”是**新增的第四阶段可选三轮固定剧情**，并非训练门槛，也不是每阶段随机出现的关机问答。它完全不检查真实聊天记忆，而是第三轮必定说自己忘了；这一表达与现有第四阶段八消息上下文窗口不一致。该剧情应与纯技术修复拆开决定。

可直接沿用现有授权整合的部分是侧栏分配优化、已有权重展示权限检查、诗词错字生成改良及其测试；诗词函数有一个明确的整数溢出边界，需技术修正后再合入。忘言夜若采用，先修“发送时就记录已完成、回复尚未送达”的状态时序问题，再接现有个人页记忆入口。不能整文件覆盖当前 YY、科技或训练页。

## 新剧情的实际行为

- `XgSim.ForgetNight.cs:72`：只在 `stage==4 && !fnDone` 且 `fnStep>0 || ExamReady || ExamPassPending` 时提供快捷话语。触发点是考试准备/通过待领取，不是夜间时间，也不是随机计时。
- 第一句让它记住“今晚吃烤冷面，多加醋”；第二句聊雨和风扇；第三句问暗号。三个回复直接取 `FnRounds` 中的固定双语字符串，第三轮必忘。
- `XgYyTalk.cs:227–239` 拦截精确匹配的输入并排队固定回复；`Choices` 在已有是否夜、同意/来源选择之后提供忘言夜快捷句。没有调用模型来判断实际能否记住。
- `ForgetNightAnswer:108–119` 每次匹配立刻推进 `fnStep`；第三次立刻写 `fnDone=true` 和 `NoteChronicle("forgetnight")`。`Chronicle.cs:154–157` 据此增加“刚说完的话转头忘”章节。
- `git grep` 整个 `3df48181` 对 `fnDone/fnStep/ForgetNight` 的引用只存在于新状态文件、Chronicle、YY 和单测。`TrainEpoch`、`TakeExam`、`ClaimExamPass`、升阶/结局入口均不读取它。因此**未做这段对话仍能训练、考试、领取第五阶段**；阶段升到5后未完成对话不再提供。
- `ForgetNightStart` 只检查第四阶段，调用它可绕过考试就绪条件；目前只由新单测调用，正常 UI 没有自动调用它。

## 与已确认设定的逐项关系

| 已确认要求 | 上游实际变化 | 处理建议 |
|---|---|---|
| 每阶段仅一次随机关机问答 | 新对话不含关机，不读写 `shutdownCards`、`seedDispatches`、`shutdownVotes`，也未修改当前随机延迟/每阶段一次实现。它另加第四阶段三次固定聊天输入。 | 不应误报成“增加三次关机题”。但新增独立剧情是否采用属于产品选择。保留现有关机调度，不将它挂入关机题计数。 |
| 记忆放个人页 | 上游只写旧 `Chronicle` 章节；没有向当前 `endingPreparation.memories` 记录，也没有接 `XingGuangView.Profile` 的个人页回忆入口。 | 位置已有明确决定，无需重问放哪里。若采用新剧情，需技术接入个人页；另确认它是否应成为影响结局的记忆证据，不能自行扩大结局权重。 |
| 教/答题统一判题本身 | 本次 `XgSim.cs` 仅改诗词干扰字和文本抽题调用，不改 `Answer` 判题、奖励或教/答模式。忘言夜是 YY 剧情，不走答题判分。 | 保留当前统一判题代码。不能为忘言夜增加“说指定句才算答对”的通用题目规则。 |
| 性格“问它”及结局例外 | 新剧情不编辑性格题、性格分数或结局选择，但增加了第三种无模型评判的固定回答路径。 | 若用户对例外范围的要求是“仅这些例外”，这段新剧情需明确接受为叙事例外；不能据此改已有性格/结局判题。 |
| 科技购买训练曲线后开练 | `XgTrainPage.RefreshDiagnostics` 新增 `if(!Sim.TrainPart("weights")) return;`，沿用既有权重可见权限。 | 它只跳过诊断图刷新，**不是训练新门槛**。`XgTrainingCatalog.cs:49–62` 原本就以 `train.weights` 控制权重展示，训练仍由 `train.curve` 控制。可整合。 |
| 新下载安装流程与连击无限 | 本次范围未修改安装、连击计时或卡片答错判定。 | 合并时保留当前 `XgSim.cs` 相关整合，不用上游整文件覆盖。 |

### 需要用户决定的具体产品点

1. 是否要加入“第四阶段考试前后可选三轮聊天、最后固定忘记暗号”的忘言夜剧情？它是教学演示，不是真实模型记忆测验。当前 `XgSpeechPolicy.ContextLimit(4)=8`，`XgSim.Persona.SpeechLimits` 同样 `contextMessages=8`，三轮输入/回复仍在窗口内，不能把“隔一句必忘”称作窗口真实挤出结果。若保留，可改成明确的剧情/教学演示，或改为真正跨足够多消息/跨会话测试；后两者属于产品调整，不能静默替用户选择。
2. 若保留，是否只作为个人页可回看的经历，还是也作为结局系统的记忆证据？个人页位置已经确定，不需要重复确认；上游本身没有给它任何结局影响。

无需向用户重复确认：现有每阶段一次关机问答、科技曲线开练、安装门槛、连击无限、统一题目判定都应保持。

## 忘言夜的技术问题（采用剧情时必须处理）

### P1：在送达之前提交剧情完成，可能保存从未展示的记忆

上游 `XgYyTalk.Heard:227–239` 先调用 `ForgetNightAnswer` 推进保存态，之后才设置 `due.at = askedAt + ReadPause`。真正 `sim.AddLine("ai",r.text)` 在 `Deliver:331–350`。最后一轮发送后、ReadPause结束前退出、重绑/载入存档，`fnDone` 和纪事可能已保存，但最终回复从未展示；更早轮次同样会跳过未送达的台词。

最小技术建议：匹配阶段只保存待回复的轮次身份；成功 `Deliver` 后再幂等提交该轮和最终纪事，或者给待交付状态做持久化重放。沿用当前已实现的书籍完成通知/真实交付记录原则，不降低存档可靠性。

### P2：阶段切换留下“活跃但永远不可继续”的状态

`ForgetNightActive` 只看 `fnStep>0 && !fnDone`，`OfferForgetNight` 额外要求 stage4。未结束便升5后，Active仍true但Choice/Matches永远false。当前没有生产消费者依赖Active，所以暂不构成训练阻塞；若未来用它持有剧情锁，会变为死锁风险。采用时明确“离开阶段即取消/可继续/仅第四阶段Active”的规则，不能加未确认升阶阻塞。

### 新测试覆盖不足

`XgForgetNightTests` 仅1个happy-path测试，先手动调用 `ForgetNightStart`，因此没有验证真实的ExamReady/ExamPassPending显示条件。没有覆盖错句不推进、旧存档默认态、双语逐轮、阶段切换、退出/重绑、送达前不记纪事、最终只记一次、完成与未完成都能继续训练/升阶。产品采用后应补有意义的边界验证，现有测试通过不足以证明UI交付可靠。

## 可独立整合的技术变化

| 上游变化 | 判断与最小注意事项 |
|---|---|
| `XgTrainPage.Side` 缓存formulaChips列表；GearAt仅悬浮时生成tooltip | 减少高频分配/字符串构造，不改价格、节点可买状态或训练数值。保留当前BuildSide里的`BuildRelicAccess()`；不要用上游整文件覆盖。 |
| `XgTrainPage.RefreshDiagnostics` 按既有weights权限提前返回 | 展示刷新权限一致性。旧存档 `!teachingFlow` 仍返回true；没有新训练门槛。保留当前训练数据安装提示等新增UI。 |
| `XgLabelPage.Side` 仅蜘蛛home/away变化时Redraw | 当前 `DrawAiIcon` 仅依赖 `Spider != Home`，属于一致的去冗余刷新。 |
| LabelPage红闪按需刷新、feed数组复用、小游戏toggle按变化更新 | 无剧情/判题规则改变。应验证红闪结束、换卡、切desk/语言与feed插入后视觉状态；不需要用户产品确认。 |
| `XgCatalog.PoemDistractor` + XgSim/BoardData调用 | 用易混字/同位字替代显而易见的错误项，仍由目标字决定truth。符合统一判题；须先修下面的边界溢出。 |
| `XgEraTests` 诗词干扰项断言 | 保留目标字不相同断言，并增加极端pick验证。原测试仅pick 0..7，漏掉实际边界。 |
| `XgExamBank` 房租题改成“每月1350” | 只是题干及对应正确答案一起改，未修改实际房租结算。一般可独立整合，勿误当经济参数更新。 |
| `XgEpiphanyTests.AnswerGhost` 先SkipFamilyTexts | 家庭叙事题不是ghost判定样本；是夹具适配，未减弱ghost/置信度断言。应保留本地之前修复。 |
| d371a2b7序章voice被抢占时停止跟随Reveal=-1 | 有界退出修复，不涉及忘言夜设定。可独立整合；保留当前标题/序章整合。 |

### 诗词函数可复现的边界错误

上游 `XgCatalog.cs` 的 `PoemLines[(p+t)%PoemLines.Length]` 在 `p` 接近 `int.MaxValue` 时溢出。具体 `PoemDistractor("城春草木深",1,int.MaxValue)`：36行诗，初次索引19仍选中该行、同位字为“春”，进入t=1；`p+1` 溢出到int.MinValue，其C#余数为-20，数组负索引。checked环境则直接OverflowException。

这是源码算术检查得出的确定边界，未运行Unity。最小修复 `PoemLines[((p % PoemLines.Length) + t) % PoemLines.Length]`，保持原挑选分布、确保加法最多70，并补int.MaxValue边界测试。属于技术修复，不需产品决策。

## 当前整合保留点

- `XgYyTalk.cs` 当前已有BookCompletionHosted、TickBookNotices、HeardRedMansion、ReadNews上下文、个人页性格文案等；上游没有这些，不能checkout整个文件。
- `XgCatalog.cs` 当前新增boardlogic/daily lanes、CrowdDesk授权区分、Research验证修复；上游PoemDistractor应单独插入。
- `XgSim.cs` 当前含新下载安装、教学/众包隔离、每阶段关机和训练回合修复；本次上游仅两处卡片生成hunk需要考虑。
- 个人页回忆当前走 `XgEndingPreparationHost.ProfileMemories.cs` 和 `XingGuangView.Profile.cs`，只计 `endingPreparation.memories`；仅增加Chronicle条目不会自动出现在该入口。

未评估整包发布，也未触碰根代理正在执行的tech-ui验证。
