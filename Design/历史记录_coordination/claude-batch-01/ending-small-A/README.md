# ending-small-A：终局小补丁 A1–A5（候选，未合入）

- 基准：生产树 `competition-latest-20261016`（只读，**未改动**）。
- 产物：
  - `candidate/Assets/LingGuangV05/...`：6 个完整修改后文件。逐行从**当前**生产文件复制后再改，行数已核对（生产行数 + 本补丁增量完全一致）。
  - `ending-small-A.patch`：**手工编写**的统一 diff（未用 diff/git 生成，本会话无 shell，未 `git apply --check`）。空行上下文没有前导空格；若 `git apply` 报错，可试 `git apply --recount --ignore-whitespace`，或直接用 candidate 里的文件覆盖。**以 candidate 文件为准。**
- 本会话只读代码、没有编译、没有跑测试。下面列出的测试都需要整合方在 Unity 里跑。

## 改了哪些文件

| 文件 | 条目 |
|---|---|
| `XingGuang/Desktop/EndingTrialPanel.cs` | A1 |
| `Core/Forum/LaoZhouQuestions.cs` | A3（新字段、台词、披露判断） |
| `DesktopBridge/Tieba/TiebaHub.LaoZhouQuestions.cs` | A2（贴吧私信接口）、A3（触发与发送） |
| `XingGuang/Desktop/XgEndingPreparationHost.cs` | A2（存档事务内走贴吧兜底）、A4（灰字格式） |
| `XingGuang/Desktop/EndingLaunchNightPanel.cs` | A4（去掉重复的「依据：」前缀） |
| `DesktopBridge/Bodu/BoduView.cs` | A5 |

`XgEndingTrialFactory.cs` 没改：封面不在 factory 里，在 `EndingTrialPanel` 还没开卷时的正文里。

---

## A1 卷子封面第一句
- **改了什么**：`EndingTrialPanel` 新增 `public static string CoverLine(bool english)`。还没开过任何一份卷面时（`item == null && !finalMode`），正文先显示「放它出去之前，总得有人先试着把它弄坏。」/ "Before letting it out, someone has to try to break it first."，空一行，再接原来的「按顺序完成三关……」。
- **为什么放这里**：生产上卷子没有单独的封面页，没开卷时显示的这段正文就相当于封面。题目、计分、门槛都没动。中英文走面板原有的 `T(zh,en)` 和 `s.english`，没有加字典键。
- **注意**：开过第一份卷面以后，面板一直显示最近一次卷面（生产原逻辑），封面句就看不到了。要不要每份卷面顶部也带这一句，见「待制作人审」。标题「最后一份卷子」也没改。

## A2 读完信后老周那句问话：贴吧兜底
- **改了什么**：
  - `TiebaHub` 新增 `ZhouEndingDmReady(owner)`、`AppendZhouEndingDm(owner,text)`（返回回滚委托）、`NotifyZhouEndingDmSaved()`。内部还是用现有的 `ZhouForumLine(ForumLibrary.LaoZhou, ForumLibrary.LaoZhou, …)`。
  - `XgEndingPreparationHost.Persist()`：YY 那条分支保持原样，仍然要求 `hub.IsVisible(Contact(LaoZhou))`。新增一条：如果已有 `siReadReceipt`、`!siQuestionAnnounced`，而且贴吧那边确认他**不是** YY 联系人（`!ZhouYyLinked`，并且 `legacyChecked` 已经完成），就把**同一句** `askedQuestionZh/En`（「你不在的时候，它听谁的？」，过 `Safe`）写进他的贴吧私信。这条消息和 `siQuestionAnnounced=true` 在**同一次** `SaveNow()` 里保存。保存失败时，私信消息、未读数和 `forumState` 字符串一起回滚。
  - `Update()` 的重试条件加上贴吧兜底这种情况，每 2 秒走一次 `CheckpointPresentation`。
- **「只发一次」的持久标记**：复用现有的 `XgEndingPreparationState.siQuestionAnnounced`（持久化，默认 false）。不管走 YY 还是贴吧，发过一次就置 true，不会两边都发。没有再加新字段。
- **为什么要等 `legacyChecked`**：旧存档的 YY 联系（`legacy_existing`）要等两个 hub 都绑定之后才会迁移出来。不等的话，读档后的前零点几秒可能把一个有 YY 联系的旧存档误判成「没联系」，结果发到贴吧去。
- 选项按钮还是不给（待办 §5.1）。

## A3 最后阶段老周主动找上来（唯一获批的例外）
- **新字段**：`LaoZhouQaState.finalApproachSent`（bool，默认 false）。旧存档缺这个字段时，JsonUtility 读出来就是 false。
- **触发时刻**：`stage >= 6 && fullOpen`，也就是红队卷子刚刚能开的那一刻。另外还要求：没完结（`!chapterComplete`、`ending` 为空），还没提交发布，还没开过任何卷面（`pendingTrial` 有真实 id 或 `attempts` 非空，都算已开过），末页、SI 信、路线都还没有。
  - **为什么选这个时刻**：生产里最早、定义最清楚的「最后阶段」门，就是 `XgEndingPreparationFlow.Ready`（`stage>=6&&fullOpen`）。`BeginTrial` 用的是它，`XgEndingPreparationHost.Show` 和 `XgFinalePage.Refresh` 判断要不要接管终章页也用它。它是存档里的状态，不依赖打开哪个界面，所以读档后也能重算。他来说「放出去之前先好好试一遍」正好引出红队卷子，所以开卷之后就不再发。
- **渠道**：如果 `ZhouYyLinked`（含 `legacy_existing`：这里只看能不能联系上，不当作同意；`EndingPrivateDisclosure.cs:30` 没动），就在原来 `yyContinued` 那段之后、`int due=` 之前，用 `yy.AppendZhouAuthored` 发。没联系的话，在原来 `return` 的位置改成用 `ZhouForumLine` 发贴吧私信（要求 `legacyChecked`，老周贴吧不在输入中，也没有挂着的选项）。
- **门禁**：两条路都在现有 `ZhouIdle()` 之后（TestMode/序章/剧情焦点/读书/考试等时刻都不会发）。YY 那条还走 `ZhouContactReady`。
- **披露判断**（台词层门禁）：`LaoZhouQuestions.PlayerToldOwnAi(state)` 只扫 `facts` 里玩家**亲手发的私信原话**（source 必须是 `tieba_dm` 或 `yy_dm`，key 以 `tieba:` 或 `yy:` 开头）。Q1/Q2 的选项、加 YY 的点击、转发新闻（`news-share:`）都不算，也不看任何训练或后台进度。规则是「第一人称做/训练」加上「AI/模型/程序…」两样都出现才算（中英各一套）。判断为已披露时，台词可以直接点明；否则旁敲侧击，不提 AI。序章预置的私信「我就打游戏的，又不炼丹。」/ "I don't train anything." 不会误判。
- 台词见下面「待制作人审」。

## A4 上线夜灰字「因为你在 X 月 X 日……」
- **改了什么**：`XgEndingPreparationHost` 新增 `SpokenDate(day,en)`，中文「7 月 3 日」（和 `XgEndingTrialFactory.DayText` 的空格写法一致），英文 `MMMM d`（InvariantCulture，例如 "July 3"），日期不合法就退回原来的 `Date()`。还新增 `Because(source,en)`：
  - `kind=="answer"`：`因为你在 7 月 3 日教过它：「<题目摘要>」` / `Because on July 3 you taught it: "<summary>"`
  - `kind=="chat"`：`……跟它说过：「…」` / `… you told it: "…"`
  - 其他类型或摘要为空：`因为你在 7 月 3 日……「…」` / `Because on July 3 you… "…"`
- `EndingLaunchNightPanel`：灰字本身已经是完整的句子，所以去掉「依据：」前缀。被带偏的回答仍然用红色，前面保留「被带偏：」。原来 host 里另外加的「这次回答被带偏了。」和面板的「被带偏：」重复，所以删掉了。
- 来源（source）怎么挑、什么时候拒绝、什么时候被带偏，都没动。摘要还是存卡时写下的那种语言（生产原行为）。

## A5 摆渡【让它读】旧入口
- **改了什么**：`BoduView` 新增 `static bool NewEndingFlow(XgSim lab)`，条件是 `stage>=6 && fullOpen && (!chapterComplete || HasCommittedRelease)`，和 `XgFinalePage.Refresh`、`XgEndingPreparationHost.Show` 判断新流程的条件逐字一致。走新流程的存档不再显示【让它读】，SI 的信在终局页里读。这时候页面会继续往下走，显示原来的「它试着翻译了一下」那一段。
- **旧存档**：旧的已完结存档（`chapterComplete` 但没有提交发布）条件为 false，行为不变。已经 `letterRead` 的旧存档仍然走原来的读信分支（这个分支在判断之前，不受影响）。
- 刷新签名里加了 `NewEndingFlow(lab)`，这样 `fullOpen` 一变页面就会重画。

---

## 建议跑的测试（本会话没跑）
- NUnit（EditMode）：
  - `Assets/LingGuangV05/Tests/Editor/LaoZhouQuestionTests.cs`（`LaoZhouQaState` 序列化、Due、Observe）
  - `Assets/LingGuangV05/Tests/Editor/LaoZhouNewsCommentTests.cs`（`LaoZhouQaState` 的 JSON 兼容）
  - `Assets/LingGuangV05/Tests/PersistenceEditor/NativeLaoZhouTests.cs`
  - `Assets/LingGuangV05/XingGuang/Tests/Editor/EndingPrivateDisclosureTests.cs`（确认 `ExplicitYyContact` 仍只认 `tieba_dm_accept`）
  - `Assets/LingGuangV05/XingGuang/Tests/Editor/XgEndingPreparationTests.cs`、`EndingPrerequisitesUiTests.cs`、`XgEpilogueFlowTests.cs`、`XgEndingNarrativeTests.cs`
- LocalBuild 实机检查（Editor）：`LaoZhouQuestionsUiChecks`、`LaoZhouNewsCommentsUiChecks`、`EndingPrivateActualUiChecks`（夹具是 `stage=6, fullOpen, phase=asked, siReadReceipt` 已设，A3 因为 `siReadReceipt` 非空不会触发）、`EndingPresentationActualUiChecks`、`EndingIntegrationUiChecks`、`EndingPrerequisitesActualUiChecks`、`SidebarLayoutChecks`（总入口）。
- 建议新增（没写）：
  1. `PlayerToldOwnAi` 的正例和反例：序章那句、Q1 选项、news-share 都不算；「我在训练一个 AI」算。
  2. `finalApproachSent` 缺字段时为 false。
  3. Persist 存档失败时，贴吧私信和 `forumState` 一起回滚。
  4. 没有 YY 联系的存档读完信，贴吧恰好收到 1 条问话，读档后不重发。
  5. 上线夜灰字中英文格式。

## 风险
1. **老周 LLM 自由回复口径冲突**：贴吧 `Reply()` 的 system 提示写着「你不知道玩家电脑中的秘密应用」，YY 老周座位也只看实际披露的内容。主动找上来之后，玩家如果追问，LLM 可能会否认自己说过的话。没有接「被追问时回『猜的。你这种情况我见多了。』」这类定向回复，这句话已经直接写进旁敲侧击那组的第二句。要不要改 LLM 上下文，需要另行决定。
2. **披露判断是关键词规则**：可能漏判（漏判时用旁敲侧击版，安全），也可能误判，比如「我写了个程序作业」会被当成已披露。另外 `facts` 最多只留 96 条，很早以前的披露可能已经被挤掉。
3. **A3 和 Q2 挨得很近**：Q2 在 `stage>=6` 时就发（通常比 `fullOpen` 早）。如果两个同时满足，先发 A3，0.5 秒后再发 Q2。
4. **A3 不发的情况**：已经开过卷面的存档、已经读过信的存档，都不会再补发，属于有意为之。
5. **XgGuide**：`final.exam` / `final.letter`（「去摆渡搜那串长数字」）这两步引导还是旧流程，这次没改（不在范围内）。A5 之后新流程存档如果还能看到这条引导，会指向一个已经隐藏的入口，需要另行处理。
6. A2 的贴吧消息会被 `TiebaHub.Reply` 当作老周说过的话放进上下文（`Ask` 只取玩家的行，影响很小）。

## 共享文件提醒
- `TiebaHub.LaoZhouQuestions.cs` 是 `TiebaHub` 的 partial。新增的 public API 是 `ZhouEndingDmReady` / `AppendZhouEndingDm` / `NotifyZhouEndingDmSaved`，用到了 `TiebaHub.cs` 里的私有成员 `linked`、`Changed`、`IsShowing`。这份文件里还加了 `using System.Collections.Generic;`。
- `BoduView.cs` 是 partial（还有 Santi/Library/News 等分片）。新增私有静态方法 `NewEndingFlow` 时已经确认生产里没有同名方法。别的批次如果也在改 `Number()` 或刷新签名，需要手工合并。
- `YYChatHub` 的几个 partial（`LaoZhouQuestions`/`EndingPrivate`）**没改**，只调用了已有的 `AppendZhouAuthored`、`RefreshZhouAuthored`、`IsVisible`、`ZhouContactReady`。
- `LaoZhouQuestions.cs` 新加了 `using System.Text.RegularExpressions;`。

## 待制作人审
1. **A3 主动找上门的台词**（新写的，非原文）：
   - 未披露（旁敲侧击，不提 AI）：
     1. 「这阵子你话少了，像是在憋什么大东西。」 / "You've been quiet lately. Like you're working on something big."
     2. 「别问我怎么知道的。猜的。你这种情况我见多了。」 / "Don't ask how I know. A guess. I've seen plenty of cases like yours."
     3. 「不管是什么，放它出去之前，先让人往狠里试一遍。我这儿正好有份卷子。」 / "Whatever it is, before you let it out, have someone test it hard first. I happen to have a paper ready."
   - 已披露（可以直接点明）：
     1. 「你那个自己训练的程序，这阵子长进不小吧。」 / "That program you've been training must have come a long way lately."
     2. 「放出去之前，得先有人往狠里试它一遍。能弄坏的地方，外头迟早有人弄坏。」 / "Before it goes out, someone has to test it hard. Anything that can break, someone out there will break sooner or later."
     3. 「我出了份卷子，你俩一起做。」 / "I've set a paper. Do it together, the two of you."
2. **A4 灰字动词**：「教过它：」/「跟它说过：」/「……」，英文 "you taught it:" / "you told it:" / "you…"。还有把「依据：」和「这次回答被带偏了。」去掉这件事。
3. **A1**：封面句要不要每份卷面都带；标题「最后一份卷子」/ "The final paper" 要不要改成红队测试的说法。
4. **A3 披露判断用的关键词表**（`OwnZh`/`SubjectZh`/`OwnEn`/`SubjectEn`）。
5. **A3 触发时刻**：`fullOpen` 且还没开卷。也可以改成「终章页第一次打开」，但那样就得依赖界面了。
