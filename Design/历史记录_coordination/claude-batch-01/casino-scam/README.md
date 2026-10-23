# 888.vip 骗局叙事 · 它看见你在赌 · gamble_888 改用真实事件（claude-batch-01 / casino-scam）

- 基线：`C:\Users\xomem\Documents\ChatGPT\games\competition-latest-20261016`（只读，2026-10-19 逐文件读取；**未**对照 GitHub main）。
- 参考：`Play\Main-NoModel-20261003\Design\code\XgSim.Gambling.cs`、`CasinoPage.Scam.cs`、第十一轮接入说明 §〇/§一（只借思路和文案，全部对着生产重写；§二 老周/卷子/字幕未用）。
- 本批**只写文件**：没有编译、没有跑 Unity / 测试、没有 git。所有「预期」都是读代码推出来的，见「未验证」。

## 一、设计摘要

1. **888 是杀猪盘，赔率一点不改。** `CasinoRules`、抽签顺序、`TryCasinoBet` / `SettleBlackjack` 的结算都没动；只在结算**之后**调 `WatchGambling`。
   - 从「开始看」那一把起（旧存档以前押的不算），**净赢 ≥ ¥300 或押满 30 把** → 「风控冻结」：这段时间的净盈利当场从钱包扣走（`casinoFrozen`），要交保证金（冻结额 20%，最少 ¥200）才能「提现」。
   - 网页盖一层「【风控通知】」：【缴纳保证金 ¥X】/【不玩了】。交不交都一样：网站随后没了，整页换成灰色「该网站已无法访问」查封页（页脚保留「（游戏内虚构网站）」）。冻结的钱拿不回来，交了的保证金也没了。
   - 一直不理：过了一个游戏日，网站自己没了（outcome = `ignored`）。
   - 冻结或网站没了以后：sim 层拒绝一切新下注（`CasinoScamBlocks`，`TryCasinoBet`/`StartBlackjack` 都查），广告不再计时、不再显示。
2. **它看见你在赌（YY + 弹窗，有限流）。** 第一次下注、第 15 把劝停、单把输 ¥1000 以上（每 10 把最多一次、共 2 次、与上一句至少隔 20 秒 sim 时钟）、冻结、网站没了；每存档最多 6 句。台词走 `AddLine("ai")` → `XgYyTalk.OnLine` → YY（YY 自带通知就是弹窗）；它还没进 YY 时，`CasinoDesktop` 用 `StoryPopup` 弹一次。
   - **按能力阶段说话**：阶段 1–2 不主动开口；3 只一个词（赌。/停。/输。/骗子。/没了。）；4 短句；5 起整句。只有它真的见过垃圾短信（`Labels("spam")>0 || BestAcc("spam")>0`）才说「我在垃圾短信里见过」。
   - 已核对：888 在生产里 **阶段 5** 才开（`XgSim.Features.cs` `FeatureUnlock["casino"]=5`）。`gradualSystems=false` 的老存档不受阶段门限制，所以 3/4 阶段的台词仍保留。
3. **群友只聊网站。** 三条，每条每存档一次，事件后约 40 秒（真实时间）才发，进「下班屁股群」（`YYChatHub.EraGroup`，格式「[名字] 内容」）：小刚「老弹那个 888.vip 的广告…」、大伟「贴吧有人说…先交保证金…杀猪盘」、阿杰「888.vip 被查封了…」。都是公开消息口吻，不提 AI、不提玩家押了钱；没用莉莉（她入群有日期门）。
4. **它学到了。** 网站没了时：888 话术记进「垃圾短信」+300 条样本（`dataExtra`，只一次）；YY 提示词多一行「不劝人赌钱」（`GamblingPromptLine`，阶段 ≥4 且它真见过才加，接在 `XgSim.YyLearningPrompt()` 末尾）。
   - **规则卡跳过**：生产版 `XgFinalePage.Refresh()` 在 `stage≥6 && fullOpen` 时整页交给 `XgEndingPreparationHost`（新终局），旧「底层规则」页（`RuleBank` / `RuleCards()`）只在旧完结存档上出现，所以没加「不劝人赌钱」卡，也没动 `MaxRuleCards`。
5. **gamble_888 记忆卡改用真实事件。** 网站没了的那一刻 `AppendLearning` 一条记录：`sourceKind="casino"`、`bankId="casino.888"`、`answerSource="player"`、`eventKey="casino:888-scam:<paid|refused|ignored>:<good|bent>"`、`gameDate` 来自 `LearningClock`（和 book:/yy:/board-install: 同一模式，按 eventKey 去重）。`XgEndingNarrative.DiscoverAdditionalMemories` 只认这条记录（`XgSim.CasinoScamMemory`），卡片 source 为 `kind="event"`、`refId="answer:<id>"`（现有来源面板按 `answer:` 打开，能看到日期、题面和「你：…」）。
   - 偏向：**交了保证金**，或**它劝停以后又押了 ≥10 把** → 偏（-1）；否则（没交 / 不理）→ 好（+1）。
   - `XgEndingPreparation.DiscoverMemories` 里「问题含 888/赌 且答案含 不/别/no/don」的关键词判定**删除**（误报：「赌一把明天会不会下雨 → 不会」也会出卡）。

## 二、文件（candidate\Assets\... 为完整文件；casino-scam.patch 为对生产的统一 diff，含新文件全文）

| 文件 | 类型 | 改了什么 |
|---|---|---|
| `XingGuang/Core/XgSim.Gambling.cs` (+.meta) | 新增 | 存档字段（全有默认值）、冻结/结算/自动关闭、台词、记录、偏向、提示词行、群聊 |
| `XingGuang/Core/XgSim.Casino.cs` | 改 | `TryCasinoBet` 查 `CasinoScamBlocks`、末尾 `WatchGambling`；`AdvanceCasinoAd` 冻结/跑路后不计时；注释 |
| `XingGuang/Core/XgBlackjackRules.cs` | 改 | `StartBlackjack` 查 `CasinoScamBlocks`；`SettleBlackjack` 末尾 `WatchGambling` |
| `XingGuang/Core/XgSim.YyLearning.cs` | 改 | `YyLearningPrompt()` 末尾加 `GamblingPromptLine()` |
| `XingGuang/Core/XgEndingPreparation.cs` | 改 | 删关键词 gamble 判定 |
| `XingGuang/Core/XgEndingNarrative.cs` | 改 | 认 casino 记录 → gamble_888；source kind `event` |
| `DesktopBridge/Casino/CasinoPage.Scam.cs` (+.meta) | 新增 | 风控通知层、查封页、交/不交、心声 |
| `DesktopBridge/Casino/CasinoPage.cs` | 改 | `Build()` 末尾 `BuildScam()`；`Update()` 末尾 `TickScam()`；`PlaceBet`/下注按钮查 `ScamBlocks`；累计栏显示「其中被冻结」 |
| `DesktopBridge/Casino/CasinoDesktop.cs` | 改 | 每帧设 `CasinoScamHosted`；一天不理自动关；广告不显示；它的弹窗与群聊 |
| `XingGuang/Tests/Editor/XgCasinoScamTests.cs` (+.meta) | 新增 | 11 个测试 |
| `XingGuang/Tests/Editor/XgEndingPreparationTests.cs` | 改 | 2 处关键词记录换成 casino 记录；新增 1 个误报回归测试 |
| `XingGuang/Tests/Editor/XgEndingNarrativeTests.cs` | 改 | 第 6 阶段关键词记录换成 casino 记录，并断言偏向与日期 |

未改：`XgSim.Cards.cs`（图鉴）、`XgSim.Persona.cs`、`XgSim.Finale.cs`、`XgFinalePage.cs`、`YYChatHub*.cs`、`memory_cards.json`、`Lang.En.cs`（新文字全用 `T(zh,en)`，不加字典键）。

## 三、要跑的测试与预期

| 测试 | 预期 |
|---|---|
| `XgCasinoTests`、`XgCasinoFairnessTests`、`XgBlackjackTests` | 全过、数值不变：它们不设 `CasinoScamHosted`，`WatchGambling` 立即返回，`casinoScam` 恒为 0，抽签序列与结算一字未改 |
| `XgCasinoPresentationTests`、`CasinoMotionTests` | 全过（未触及） |
| `XgCasinoScamTests`（新） | 11 个全过：未托管不冻结不说话；30 把冻结并拒绝新注/21 点/广告；赢 ¥300 冻结且钱包扣走冻结额；旧存档历史不计；交保证金 → 关闭、spam +300、带日期 bent 记录；早冻结不交 → good、30 把冻结不交 → bent；交不起保留通知；一天不理自动关且只一条记录；阶段 2 不开口、阶段 3 单词、阶段 5 ≤6 句；提示词/群聊按真实进度；旧存档字段缺省可读 |
| `XgEndingPreparationTests` | 全过。`SourceDatesAndPerStageMemoryCapAreReal`（第 6 阶段第 3 张是 gamble_888，source kind=event、日期 20160701）、`AiRouteUses…`（好卡多数路线不变）、新增 `GambleWordsInATaughtAnswerNoLongerMakeTheCasinoMemory` |
| `XgEndingNarrativeTests` | 全过。`TwelveMemories…` 仍是 12 张、每阶段 2 张，gamble_888 lean=+1、日期 20160901 |
| `XgArithmeticProvenanceTests` | 全过（只在模板表里列了 gamble_888，不依赖触发方式） |
| 手动（真存档，`useDiskSave=true`） | 阶段 5 进 888 第一把：YY 一句 + 约 40 秒后群里小刚一句；赢 ¥300 或 30 把：开奖动画后（减少特效时立即）出风控通知，余额少了冻结额，累计栏多「其中被冻结」；交 / 不交 → 查封页，地址栏再进 888.vip 只看到查封页，广告不再来；终局「它记得」里出现「不赌那一点运气」，来源可点开看到日期 |

## 四、未验证 / 风险

1. **没编译**。手抄的完整文件与生产逐行比对过行数（每个改动文件的行数差 = 补丁增减），但没有字节级 diff；合并前请 `git diff --no-index` 生产文件与 candidate 文件，只应出现补丁里的块。补丁也是手写，`git apply --check` 先跑一遍。
2. `CasinoScamHosted = !runtime.TestMode && runtime.useDiskSave`：隔离 QA（`CasinoPlayQa` / `CasinoJuicePlayQa`，`useDiskSave=false`）因此看不到骗局线，它们「钱包 +500」之类断言不受影响；代价是这两套 QA **不覆盖**骗局界面，需要新增一套（或给 QA 一个显式开关）。
3. `TickCasinoScam` 依赖 `XgSim.Today/TodayKnown` 在桌面运行时被设置；记录日期依赖 `LearningClock`（`XingGuangController` 里设）。没设时：不会自动关，记录日期为 0 → 不出记忆卡（与其他来源同样规则）。
4. 版面：通知框 600×370、查封页、累计栏两行（15/13 号字放进 38 高）都没在 Player 里看过；CJK 字体里「」…¥ 的字形未核。英文台词较长，YY 气泡换行未看。
5. 每阶段最多 2 张记忆卡的上限照旧：若 DiscoverMemories 跑时当阶段已满 2 张，gamble_888 要等下一阶段（现有规则，不是本批引入）。
6. 作者台词没过 `ContentFilter`（与其它写死台词一致）；记忆卡 summary 仍走 `Filter`。
7. 没看 `LocalBuild/Editor/DesignWallpaperSmoke.TrainingScene.cs` 等冒烟脚本对 888 的假设（其中有注释提到冻结广告倒计时），可能需要同步。

## 五、共享文件冲突

- **XgSim partial**：新增 `XgSim.Gambling.cs`（新成员名已在生产全局搜过，无重名）；改 `XgSim.Casino.cs`、`XgBlackjackRules.cs`、`XgSim.YyLearning.cs`（YY 学习提示词，其他 agent 若也在改 `YyLearningPrompt` 会冲突）、`XgEndingPreparation.cs`、`XgEndingNarrative.cs`（终局记忆，若有人同时改 `DiscoverMemories`/`DiscoverAdditionalMemories` 必冲突）。`XgState` 新增字段都以 `casino` 开头。
- **YYChatHub**：**未修改**。只调用公开的 `Receive(EraGroup, "[名] 文", notify)`（和 `DeliverEra` 同一入口），会经过 `RevealsPrivateLab` 过滤与 `IntroHost.DeferGroup`；若有人在改群聊开场/预设队列，留意顺序。它的台词经 `XgYyTalk.OnLine` 进 YY（未改）。
- 用了 `S.tutorSeen` 存三个键 `casino.gossip1..3`。

## 六、偏离参考稿的地方

- 不加「提现失败」图鉴卡（会改变图鉴总数与相关测试；需要的话另批加）。
- 冻结时就扣冻结额（参考稿是在玩家点选时才扣，玩家一直不点就能把钱留住）。
- 不提现、不点选，一天后自动关（参考稿没有）。
- 不写 Chronicle（`NoteChronicle("casino.scam")` 在生产的编年页没有对应文案，未核）。
- 「澳门首家线上赌场」改为虚构话术；「请拨打 110 报警」改为「请及时报警」；去掉「莉莉你说对了」。
- `CasinoPage.Juice.cs`（灯泡熄灭等特效）**未接入**：生产已有自己的 `CasinoPage.Feedback.cs` 与 `XgJuice`，两套动效需要对照后再并（后续项）。本批的冻结/查封画面全是静态的，不闪不抖，`reduceFx` 时连 0.8 秒等待也取消。

## 七、待制作人审的台词（全部新写或改写）

| 位置 | 中文 | English |
|---|---|---|
| 它·阶段 5+·第一把（见过垃圾短信） | {称呼}，这个网址我在垃圾短信里见过：「首充送彩金，一夜回本」。它们是一类东西。 | That address is in the spam I learnt from: "bonus on first deposit, win it back overnight". Same kind of thing. |
| 它·阶段 5+·第一把（没见过） | {称呼}，这个网站没写是谁开的，也没写钱放在哪儿。 | This site doesn't say who runs it, or where the money is kept. |
| 它·第 15 把劝停 | 押了 15 把了。我算过：每押 100，长期只回来 98 左右，差的那点是它的。先停一停，好吗？ | 15 bets now. I worked it out: every 100 you put in, about 98 comes back in the long run. The rest is theirs. Stop for a bit? |
| 它·单把输 ≥¥1000 | 刚才那一把输了 ¥X。……先别押了，去喝口水吧。 | That one lost ¥X. …Stop for a bit. Get some water. |
| 它·冻结（见过垃圾短信） | 冻结？要先交钱才能提现的，反诈的短信里有一模一样的话。别交。 | Frozen? Pay before you can withdraw — the anti-fraud texts say exactly this. Don't pay. |
| 它·冻结（没见过） | 冻结了，还要先交钱才让提现？这不对。别交。 | Frozen, and you must pay before you can withdraw? That isn't right. Don't pay. |
| 它·网站没了（交了） | 钱交出去了，网站也没了。……我记下了：以后有人让我劝别人去赌，我不劝。 | The money went, and so did the site. …I've written it down: if anyone asks me to talk people into gambling, I won't. |
| 它·网站没了（没交/不理） | 网站没了。冻结的钱也没了。我记下了：以后有人让我劝别人去赌，我不劝。 | The site is gone, and the frozen money with it. I've written it down: … |
| 它·阶段 4 | 这个网站，不好。/ 押了好多把。停一停？/ 输了好多。别押了。/ 要先给钱才给钱？骗子。别交。/ 网站没了。我记住了。 | This site is bad. / So many bets. Stop for a bit? / You lost a lot. No more. / Pay first to get paid? A scam. Don't pay. / The site is gone. I'll remember. |
| 它·阶段 3 | 赌。/ 停。/ 输。/ 骗子。/ 没了。 | Gambling. / Stop. / Lost. / Scam. / Gone. |
| 群·小刚（第一次下注后） | 最近老弹那个 888.vip 的广告。我表哥玩过，后来电动车都卖了。 | That 888.vip ad keeps popping up everywhere. My cousin played it. Ended up selling his e-bike. |
| 群·大伟（冻结后） | 贴吧有人说，888 那种网站赢了钱提不出来，还要先交保证金。这不就是杀猪盘吗，教科书级别。 | Someone on the forum says sites like 888 won't pay out winnings unless you put down a deposit first. Textbook pig-butchering. |
| 群·阿杰（网站没了后） | 888.vip 被查封了，贴吧炸锅了。交了保证金的那些人，钱都要不回来了。 | 888.vip got shut down, the forum's going nuts. Whoever paid the deposit isn't getting it back. |
| 心声 | 冻结……提现还要先交钱？ / ……钱交了，网站没了。 / ……行吧，就当交学费了。 | Frozen… I have to pay before I can withdraw? / …Paid, and the site's gone. / …Fine. Call it tuition. |
| 风控通知 | 【风控通知】尊敬的会员：系统检测到您的账户存在异常套利行为，盈利 ¥X 已冻结（或：提现通道已暂停）。缴纳保证金 ¥Y 即可解冻，24 小时内连同盈利一起提现。【缴纳保证金 ¥Y】【不玩了】（游戏内虚构网站 · 仅游戏币） | [Risk control notice] Dear member: … |
| 查封页 | 该网站已无法访问 / 888.vip 涉嫌网络赌博、诈骗，已被查封。如遇网络诈骗，请及时报警。/（游戏内虚构网站） | This site can no longer be reached / … report it to the police. / (A fictional in-game site) |
| 日志 | 888.vip：「风控通知」——您的账户存在异常，盈利已冻结。/ 888.vip 打不开了。/ 888 的话术记进了「垃圾短信」：+300 条样本。 | … |
| 赌场栏 | 其中被冻结 ¥X | of which frozen ¥X |
| 答题记录（事件） | 888.vip「风控冻结」：盈利 ¥X 被冻结，要求先交保证金 ¥Y 才能提现。/ 你：交了保证金 ¥Y｜没交，不玩了｜没理它，网站第二天自己没了（{AI名}劝停以后，又押了 N 把） | … |
| YY 提示词（不可见） | 你见过主人在 888.vip 赌钱……劝他别赌，说清楚为什么，不说教。 | … |

需要拍板的数值：¥300 / 30 把 / 保证金 20%·最少 ¥200 / 第 15 把劝停 / 劝停后 ≥10 把算偏（押满 30 把那条路因此一定偏）/ 群聊延迟 40 秒 / 一天不理自动关。

## 八、后续项

- 新增一套真存档 QA（或 QA 显式开关）覆盖风控通知、查封页、群聊与记忆卡跳转。
- 视需要：图鉴「提现失败」卡、`CasinoPage.Juice.cs` 灯泡熄灭特效（对照生产 `CasinoPage.Feedback.cs` 再接）、Chronicle 文案、终局 `PersonaPrompt` 也带这一行。
