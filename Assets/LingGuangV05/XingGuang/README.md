# 灵光.exe — 深度学习工作站（CNN / RNN 训练增量游戏）

> **2026-10-04 · 策划案 v0.2 第一步（已完成）**：论文与引用整体删除，下文提到“论文”的段落已过时。
> - 突破改为“本阶段必修节点买齐 + 看懂瓶颈 + 原有评级条件”：`XgCatalog.Mandatory`，`XgSim.MissingMandatory / RequirementName`。技能树详情页逐条列出“必修 · …”。
> - 新增必修概念节点：权重、感知机学习规则、阶跃激活（阶段 1）；Sigmoid、反向传播、链式法则（阶段 2）；LayerNorm、残差连接、学习率预热（阶段 6，随 Transformer 一起获得）。
> - 数据集 `citationBase` 改名 `rewardBase`（数值不变，仍乘纪录奖励与评级奖）；订单页在有第一个检查点后出现；HUD 的“论文”换成“阶段 N / 6”；训练页“下一篇论文”换成“下一评级”。
> - 剧情信号 `lg.paper` 与节拍 `lz_paper` 删除。
> - 软件名占位：`Assets/LingGuangV05/Core/AppNames.cs`（`AppZh/ExeZh/GameZh/AiZh` 及英文）。名字定了只改这里；旧版遗留的编辑器安装器、旧 UI 与本地化 JSON 里的“灵光”暂未替换（见 `Documentation/LingGuang-Incremental/策划案v0.2落地对照.md` §7）。
> - 平衡机器人（`--ending=no`）：50:13 到达阶段 6（改动前 54:34）。


灵光.exe 的玩法已经换成这套"训练神经网络"的增量游戏（代码目录仍叫 `XingGuang`，开发时的名字）。改动不修改场景文件：各个控制器在场景加载后挂到 `ChapterOneRuntime` 上，接管原有窗口，通过 `ChapterOneDesktopBridge.UseCustomView` 打开。

| 软件 | 现在是什么 | 代码 |
|---|---|---|
| 灵光.exe | 训练 → 研究（标注台和订单已移到摆渡众包） | `XingGuang/` |
| 摆渡众包 | 标注台 · 企业订单 · 分包 · 结算 · 信用 | `DesktopBridge/Zhongbao/` |
| YY | 2016 年风格的聊天软件：会话列表、圆头像、左右气泡、文件卡片。老周用它把灵光.exe 传给玩家 | `YY2016/` |
| 邮件 | 普通收件箱，只有剧情氛围，不影响数值 | `Mail2016/` |

## 开局流程

1. 开场剧情结束后，老周在 YY 里说话，说完第三句（"我打包好了……"）就发来 `灵光.exe` 文件卡片。
2. 玩家点「接收」：文件以约 4.6 MB/s 传输，完成后灵光被标记为已安装，桌面出现图标（如果和别的图标重叠，会自动挪到空格子）。
3. 浏览器里的 lingguang.cc 不再提供下载，按钮改为"打开 YY 找老周要"。

## 灵光玩法（第二版，2026-10-03）

设计稿：`Documentation/LingGuang-Incremental/训练动作与技能树设计.md`、`标注题库设计.md`。

页签：标注台 · 训练 · 技能树 · 订单。HUD 右上角常驻连击。

> **2026-10-06 · 摆渡众包**: the 标注台 (`XgLabelPage`) and 订单 (`XgContractsPage`) pages moved out of the 灵光.exe window into a separate desktop app, 摆渡众包 (Bodu Crowd, router id `zhongbao`, `DesktopBridge/Zhongbao/`). It takes over the spare native Calculator window once 灵光.exe is installed and hosts the same page classes on the same `XingGuangController.Sim` through `IXgPageHost` (implemented by both `XingGuangView` and `ZhongbaoView`). It adds three read-only pages: 分包 (the 转包 rows of `XgMarketSection`), 结算 (live rates and the totals the sim already keeps) and 信用 (credit and the rules that move it). Asking 灵光 for the "label" or "contracts" tab opens 摆渡众包 on that page; the to-do note uses `XgGuide.Crowd`. No rules or numbers changed.

> **2026-10-07 · chat moved to YY, 科技 in the nav**: the 对话 page is gone. The AI joins YY right after its setup at stage 1 (`DesktopBridge/Story/AiJoinsYy.cs`, guarded until the setup and the first 是 / 否 are done and the desktop is quiet), and talking to it happens in its YY conversation through `Desktop/XgYyTalk.cs`: the player's lines go into the lab's chat log (`XgState.chat`, which `XgSim.LineAdded` announces), every line the lab adds arrives in YY, replies use the old page's pipeline (persona, board, tone habits, recalled memories, stage speech limits, brain gate) plus YY's stage-5 focus word, and the suggested questions (「她……爱我吗？」, 「你是怎么被训练出来的？」) and their answers are YY choices (`YYChatHub.Stand`). 赞 / 踩 sit beside its latest reply; 「它记得」 and its personality are hover notes in the YY header. The nav's 养成 group is 训练, 数据, 道具, 科技 (page id `tree`); 它 is just 概览. The 数据 page uses two-line dataset and source rows with fixed button columns and a scroll bar.

> **2026-10-07 · redesign**: 灵光.exe follows `Documentation/LingGuang-Incremental/lingguang-redesign/index.html`. The window's own caption is dark (`XgWindowChrome`; other apps keep Aero). The top bar (`XingGuangView.Frame.cs`) shows the AI, the two main-line bars at the thresholds after items, ¥, income, house power and the clock; the combo stays in 摆渡众包. The nav is grouped: 它 (概览 `XgHomePage`, 对话, 大脑), 养成 (训练, 数据 `XgDataPage`, 道具 `XgItemsPage`), 调度 (接线, 模型仓库), 收藏 (能力 `XgAbilitiesPage`, 成就卡册, 终章, locked until 全部放开). 概览 is the landing page. The 科技 tree is a section of 道具 (page id `tree`, reached by the 科技树 button; it keeps widths, depths, packs, automation, research and the finale nodes). Items are data-driven (`Core/XgItems.cs`): new shelves add to `XgItems.Extra`. 灵光 pages draw with `XgDark` (the dark palette); 摆渡众包 keeps `XgPalette`. No rules or numbers changed.

> **2026-10-08 · 摆渡众包 redesign**: the app follows `Documentation/LingGuang-Incremental/zhongbao-redesign/index.html`, a 2016 Chinese web platform (white boxes, platform blue header, thin grey rules, small red badges; `DesktopBridge/Zhongbao/ZhongbaoSkin.cs` holds the colours and the shared building blocks). The 标注台 is three columns: the 任务大厅 list (`XgLabelPage.Hall.cs`), the workspace (card, answer buttons with the keys 1 and 2, the last 20 answers with 「灵」 on those the spider gave), and a scrolling right column (`XgLabelPage.Side.cs`: 今天, the raise ladder, the 灵光 corner, auto-answer, data and the collaboration panel). 企业订单 is a card grid, 分包 has worker rows and a YY group column, 结算 shows the household bills with the 3-step bankruptcy strip and a 10-day balance sparkline (`XgSim.Ledger.cs`), 信用 has a dial and a tier table.
> **Hand pay ×2.5.** `XgCatalog.HandPayScale` multiplies hand labelling on every desk (`XgSim.ManualPayFor`); automatic labels, contracts and the platform's fines keep their rates. **算术 is true/false**: the card shows an equation with an answer (`47 + 50 = 97`), about half are off by a slip a person makes (`XgArith`), the buttons say 对 / 错, and a wrong pick costs `XgDesk.fine` (half a right answer's base pay, scaled by the raise, not by the combo; never more than the wallet holds) and breaks the combo.
> **Drag 灵光 to the question.** While the 标注台 is in front, a small invisible handle (`XgSpiderHandle`) follows the resident spider. Dropping it on the workspace snaps it beside the card; it then thinks, reaches an arm to the button of the checkpoint's guess, presses it and brings the arm back, about one answer a second (`XgResidentSpider.TickWork`). That is a hand answer (`XgSim.SpiderDragAnswer`): right ones pay with the combo, wrong ones break it and cost the desk's fine like a player's mistake, and it only works on desks the checkpoint may answer (60%). Its occasional random help while perched (`XgSim.SpiderAnswer`) is unchanged and never costs money. Drag it off the workspace to stop it. Pointer positions go through the event camera, as in `XgChipDrag`.

> **2026-10-07 · 科技 tree on Upgrade Tree PRO, 大脑 page removed**: the 科技 page (`Desktop/XgTechTreePage.cs`, page id `tree`) now uses the look of the Asset Store package Upgrade Tree PRO (`Assets/HolenderGames`, with DOTween in `Assets/Plugins/Demigiant`). It shows square icon nodes with state frames (green affordable, red too expensive, grey locked, gold maxed), straight connector lines, a hover tooltip with name, description, Lv. n/m and price, and the package's DOTween shake on buy. Click to buy; drag to pan. The squares are generated at runtime by `Core/XgTechTree.cs` from `XgCatalog.Nodes`. Depth, width and automation ladders (each step's parent is the step before) become one square with levels. 加薪 and 自动答题 keep the levels the simulation already counts. Every purchase is still `XgSim.BuyNode`, so prices, prerequisites, stage gates and the save are unchanged. Only the package's sprites and tweens are used, through `Resources/LingGuangV05/TechTree/XgTechTreeSkin.asset`. Its managers (currency, stats, sound, tree state) are not used, and the sounds are XgJuice's. The page calls `DOTween.Init` itself, so no editor setup step is needed. The old `XgTreePage.cs` stays for `XgPan`, which the wiring page uses. The 大脑 nav entry and page were removed: the concept board still runs in the simulation (`XgSim.Board`). The `XgBoardPage*.cs` files stay in the project unused, because 概览 and the emergence cutscene read `XgBoardPage.CortexCells` and draw with its cortex graphic. The 概览 cortex thumbnail now opens 能力. The editor probe `Editor/XgBrainModelsQa.cs`, which only drove the removed page, was deleted.

> **2026-10-08 · 训练 page redesign and consumables**: the 训练 page follows `Documentation/LingGuang-Incremental/train-redesign/index.html`, laid out as follows (`Desktop/XgTrainPage*.cs`). Left: the track tabs with the model in one line (structure · parameters · VRAM; its tooltip carries the old model card: shape, techniques, cards per round, ceiling, optimizer), the curve card (validation accuracy, a dashed ceiling, red dots on rounds that dropped, the stage's teaching diagram underneath), the train row (the 训练一轮 button shows this round's electricity from `XingGuangHost.RoundPrice`, i.e. `ChapterOneSim.TrainingRoundPrice`; epoch progress; the drop risk with dirty labels and a heat warning when `ChapterOneSim.WearRiskLive`; chips for the running consumables; auto-train), and the consumables quick bar (`XgTrainPage.Dock.cs`, keys 1–6, shown once the 道具 page is open or a consumable is owned). Right (`XgTrainPage.Side.cs`): 有效规模 (stage 6: 参数 × 数据 × each `XgSim.ScaleItems` multiplier, owned solid and missing dashed, a log bar with the ×2000 mark, the missing ones listed; stages 1–5: the next ability's two bars), 装备 (the structure, Dropout, 数据增强, cuDNN, 九州风神 and the seven stage-6 multipliers; a click buys through `XingGuangView.BuyNode`), and 今天的账 (today's training electricity, rent per calendar day, this month's kWh and tier, a warning when the wallet is below one day's rent). The 装备 card has a second view, 数据集, that holds what the old model card kept: noise line and cleaning, the user-log switch, the dataset list with its install buttons, the pack switches and the GPU a pack is dragged onto.
> **Consumables** (`Core/XgSim.Consumables.cs`, state in `XgState`: `consumables`, `pasteSeconds`, `redbullSeconds`, `offpeakRounds`, `rollbackGuards`, `sgdrRounds`; a save without them loads with none): sold in packs on a new 消耗品 shelf of the 道具 page, used from the quick bar, never needed for any ability or ending. 谷电时段 (¥30 for 3): the next 10 rounds bill half the electricity. 重涂硅脂 (¥30 for 2, 20 s cooldown): 120 s without card burn-out, and the run of non-stop training that wears the cards starts again. 红牛 (¥18 for 6, 15 s): 60 s in which the combo does not fade and its bonus is 50% bigger (hand-labelling pay included, since the combo is shared). 检查点回滚 (¥40 for 2): the next real drop is undone (the round neither gains nor loses). 学习率重启 (¥80 for 1, 60 s): for 5 rounds `CeilingAcc` is 8% higher, still capped by the dataset's floor. 清洗脚本 (¥50 for 1, 30 s): removes 50 noisy labels (own mistakes first, then outside noise) from the current dataset. The electricity and wear scaling reach the household through the optional `IXgTrainingBill` (`ChapterOneSim.BillTraining(seconds, priceFactor, energyFactor, wearFactor)`); a host without it bills as before. **cuDNN** now also takes 30% off the energy of every training slice (so the month's tier fills more slowly), and the new 九州风神 research item (`cooler`, on the 电力 shelf, about ¥200) halves the burn-out chance; a card only burns out with two or more cards.
> `Editor/XgTrainPageLayoutQa.cs` renders the 训练 or 道具 page to a PNG in Edit mode (in-memory lab and household, nothing saved) for a look at the layout without Play mode.

1. **标注台（开局唯一收入）**：每张桌一道是/否题。答对：¥0.5 × 连击加成（最多 ×2）× 难度加成，多一条样本；答错或超时：不给钱、连击清零、这条作废。答错时显示正确答案、理由和梗的出处。
2. **训练是一个动作**：攒够 12 条样本后，在「训练」页按「训练一轮」。一轮 = 0.6 秒显卡时间，耗电记进家里电费；手按吃连击加成，自动训练只有一半效果。学习率太大会 NaN，退回一半进度、连击清零。
3. **评估**：每 4 轮自动评估一次（也可以手动按）。模型分 0–1000（瞎猜 = 0，数据集极限 = 1000），评级 D/C/B/A/S（300/600/800/950）。刷新纪录才给钱：分数涨幅 × 数据集 citationBase × 0.2，首次达到每个评级另有奖金（C ¥20、B ¥60、A ¥200、S ¥800，× citationBase）。纪录自动存成检查点；订单、论文、自动答题都认它。连续 3 次没进步会提示卡在哪（学习率 / 数据 / 模型大小）。
4. **技能树**（`XgCatalog.Nodes`，用 ¥ 买，按住节点 0.6 秒）：
   - 视觉：LeNet-5 → AlexNet（1 篇论文）→ VGG-16（3 篇）→ GoogLeNet（6 篇）/ ResNet（10 篇）；层数 3…32、宽度 32…1024、学习率旋钮、MNIST / CIFAR / ImageNet 数据包挂在下面。
   - 序列：朴素 RNN → LSTM（1 篇）→ GRU（3 篇）/ Seq2Seq（5 篇）→ 注意力（10 篇）；层数、宽度、旋钮、唐诗 / 逻辑题 / 人民日报 / 中英语料。
   - 研究：动量 → RMSProp → Adam；梯度裁剪 → BatchNorm；Dropout → 数据增强；学习率衰减 → cuDNN → 迁移学习；自动化五级。
   - 层数、宽度是「上限」：在训练页 −/+ 免费调；买下时自动长到新上限（显存够的话）。没买旋钮时学习率固定（视觉 0.003、序列 0.001）。
5. **自动化**：run.sh（按住连发）→ crontab（窗口开着时每 3 秒自动一轮）→ 守护进程（关窗也练，每 2 秒）→ 自动评估（每轮都评估，每 1.5 秒）→ AutoML（自动调学习率、卡住时换数据集、自动签订单，每 1 秒）。自动的轮次不算连击。
6. **连击**（`XgState.combo / bestCombo`）：标注答对 +1、老司机题 +2、训练一轮 +1、评估刷新纪录 +5。每桌有自己的续命时间（数字 3 秒、逻辑题 12 秒、训练 1.5 秒），超时清零。10「不错」、25「手热了」、50「上头」、100「灵光一闪」各有一次闪白 + 震屏 + 音效。
7. **订单**：拖下方的检查点卡到订单上签约（也可以点按钮）。只显示数据已开放的订单。
8. **加薪**（标注台右栏商店）：17 级，人工标注报酬 ×1 → ×1.5 → ×2 … ×11 … ×30，每级至少 +0.5；首级 ¥10，之后每级 ×1.7。每两级换一个头衔（实习标注员 → … → 首席标注官），换头衔时有「升职」特效。只加手标和 YY 答题，不加自动答题。可以按 ×1 / ×10 / 最大 批量买。旧存档里的一次性「人工标注加薪 ×2」节点自动换算成 5 级（现在是 ×4）。

界面上学了增量游戏的常见做法：商店行（图标、等级、「现在 → 下一级」、离价格还差多少的进度条、买得起时发光）、批量购买开关、页签角标（技能树显示可买节点数，标注台在能加薪时提示）、钱数滚动上涨、HUD 显示每秒总收入（订单 + 自动答题）、回来时的「欢迎回来」离线收益卡。

## 手感（`Desktop/XgJuice.cs`）

纯 UI 实现（不用 ParticleSystem），所有效果走不受时间缩放影响的时钟，顿帧期间一起暂停。

| 效果 | 用在 |
|---|---|
| 闪白 / 闪红 | 连击过档、评估刷新纪录、NaN、老司机题 |
| 击退（缩放回弹 + 位移） | 按钮、题卡、节点、订单行 |
| 飘字 | 「+¥0.53」、「新纪录 +¥254」、「第 12 轮 · 验证 85%」、档位名 |
| 粒子 | ¥ / ★ / 彩纸 / 火花；连击断了数字碎成方块落下 |
| 顿帧 | 答错 60 ms、评估 120 ms、S 级与 NaN 200 ms（`Time.timeScale = 0`） |
| 震屏 | 整个灵光内容区，0.3 秒内不重复 |
| 冲击波 | 一轮结束、评级印章、买节点、签订单 |
| 发光 | 可买节点呼吸、前方高能题卡、连击 ≥ 5 题卡边框、连击 ≥ 10 训练按钮 |
| 拖拽 | 技能树平移（滚轮缩放）、检查点卡拖到订单 |
| 音效 | `XgJuice.Sfx` 程序合成 16 种（叮、咔、风扇、印章、号角、电流……），音高随连击升高 |

左栏底部有「音效：开/关」「减少特效」（关掉震屏和顿帧），存在 `XgState.mute / reduceFx`。

## 2026-10-07 内容删减

按用户要求下架车牌识别抬杆、死活题批改、自动客服回复三份企业合同，以及中式英语、跨句理解两张付费标注桌；移除全部高价单和巨信软件。翻译/跨句训练数据、教学示例和阶段突破仍保留。

旧档会清理退役合同、标注队列和分包任务；已获得的数据与钱包不回滚。旧高价单已经赚到但尚未到账的尾款在下一次有效 tick 一次性兑付。巨信原占用的 Photo Gallery 入口不再显示。

## 标注台题库

可用标注桌以 `XgCatalog.Desks` 为准，每张桌的样本喂对应数据集（验证码喂 CIFAR-10）：

| 桌 | 开放 | 内容 |
|---|---|---|
| 手写数字 / 唐诗下一字 / 逻辑题 | 开局 | 原有 |
| 弹幕情绪、垃圾短信 | 第一次训练 | `Core/XgMemes.cs`，2016 年的弹幕和短信，含反讽和「正规推广」陷阱 |
| 表情包、标题党 | 第一篇论文 | 暴走脸 + 配字（问脸不问字，字可能是骗人的）；只看标题写法不看真假 |
| 刷单评论、验证码 | 解锁 LSTM | 12306 风格图标 + 干扰线，4 级出两张图；彩蛋「请点击所有的老司机」 |
| 围棋气数 | 累计 500 条 | 9 路棋盘，叫吃 / 数气 / 「下在 ✕ 能提吗」，构造时检查局面合法 |

- 每条梗带出现日期，游戏日期（`GameCalendar`）之前的才会出现：葛优躺、洪荒之力、蓝瘦香菇要等到 2016 年下半年。
- 每天一个热词（太阳的后裔、AlphaGo、科比……），当天相关题目 ×3；网吧群的本地模型提示词也知道当天热词。
- 5% 的题是「前方高能」：¥ ×3，限时 3 秒（文字题 8 秒）。3 级以后约 1/7 是「老司机题」（看着像另一个答案），答对连击 +2。
- 表情包 3 级后偶尔「阿杰向你发起斗图」：连对 5 张、每张 2.5 秒，赢了有奖。
- YY 联动：垃圾短信每标 30 条，老周转来一条短信问是不是骗子；弹幕每标 25 条，小刚在群里问算夸还是骂。在聊天里回答（是/不是、夸/骂）就算标注一张。

## 逻辑题（无限生成）

标注台的第三页。题目由 `Core/XgLogic.cs` 按种子和难度现场生成，永远做不完，只答「是 / 否」。每题先决定答案、再按答案出题，所以答案在构造上一定正确，是/否各占约一半。

| 难度 | 新增题型 |
|---|---|
| 1 | 比较（传递性）、否定、必要条件（算术、数的性质、数列已移到「算术」桌） |
| 2 | 条件推理（肯定前件 / 否定后件成立；肯定后件 / 否定前件不成立） |
| 3 | 三段论（用"噗噗""啾啾"这类自造词，避免常识干扰）、真假运算、2016 年日历 |
| 4–5 | 真话假话（2–3 人，用穷举保证只有一种自洽的解） |

- 每标对 40 题升一级，最高 5 级；每升一级单价 +50%。
- 答错时显示正确答案和理由。
- 逻辑题是序列线上的一个数据集（「逻辑是非题」，未训练时是 50% 瞎猜），有自己的自动答题、论文门槛和两个订单（家教网站批改、考公培训陪练）。
- 序列线上任一数据集够 12 条就能解锁训练；开始训练时，会自动切到样本够的那个数据集。

## 算术 desk (rule-generated)

> **2026-10-07**: a 算术 (Arithmetic) desk is open from the very start next to 逻辑题 and 垃圾短信, and a new game starts on it. `Core/XgArith.cs` generates yes/no claims from the card seed and the desk level, the same way `XgLogic` does: it picks the answer first, computes the real result, and for a "no" shows a slip a person really makes (off by one or two, a forgotten carry or borrow, swapped digits, the wrong operation, left to right instead of × and ÷ first). Level 1 is + and − within 20; level 2 adds the times table and 2016 word problems (泡面, 网吧, 电费, YY); level 3 long multiplication, division, odd and even, divisibility and number patterns; level 4 order of operations, powers of 2 and primes; level 5 percentages, fractions and squares. Every question carries a plain formula (`XgLogicQuestion.check`) that `XgArithmeticTests` recomputes with its own evaluator for every level. Sums, number properties and number patterns left the logic pool, so 逻辑题 is reasoning only.
>
> Pay per desk: 算术 1.0, 垃圾短信 1.4, 逻辑题 3.2 (was 2.5). The 算术 dataset sits on the sequence track and its samples count half toward the main line's data bar (`XgDataset.dataWeight`), so its quick cards do not fill the bar; its own model still trains on every sample. Its pack (¥2500, 20k) opens at stage 2. Old saves get the desk through the existing start-desk repair in `XgSim.Repair`.
>
> The 标注台 tab row now lists every desk: open ones first, then locked ones dimmed and marked 锁. Hovering or clicking a locked tab explains how it opens (`XgSim.DeskConditionText`, which names the ability, the pack and its price, and any item still missing); clicking never selects it.

## 常识判断 desk (why its yes/no is a coin toss until trained)

> **2026-10-08**: at stage 1 the AI answers only 是 / 否, and `XgSim.Gate` keeps that answer with probability `YesNoAccuracy` (the best checkpoint accuracy over `sense`, `logic`, `spam` and `xor`, never below 0.5) and flips it otherwise. An untrained 灵光 is therefore a real coin toss, and players could not see that training is the cause. A new **常识判断** (Common sense, id `sense`) desk shows it: open from the first minute (`XgDeskUnlock.Start`, also added to old saves on load), a Text-kind desk on the sequence track with pay factor 1 (same as 算术, no fine), balanced 是 / 否 statements a 2016 player knows (「太阳从东边升起。」是, 「鱼会爬树。」否, 「一年有 13 个月。」否). The bank is `Core/XgMemes.Sense.cs`: 104 items, 52 yes and 52 no, every one with a Chinese and an English wording and a reason in both; levels 1 to 3 (nursery, school, a little more), no tricks, no dates, nothing after 2016. `XgPhrase.textEn` / `whyEn` carry the English (the older phrase desks quote Chinese and leave them empty). The card is the 算术 style (one big statement, 是 / 否 on keys 1 and 2). It feeds its own dataset like the other desks, trains and is assessed on the 序列 track, counts in full toward the data bar, is a stage-1 dataset for the ceiling curve, and is eligible for auto-answer and the spider. The task hall lists it with the cheap desks near the top.
>
> The inner voice reacts to its replies at stage 1 (`XgSim.YesNoVoice`, called from `XgYyTalk.Deliver`; flags `XgState.senseVoice*` and `yesNoReplies`, old saves start with all off, each line once per save). While `YesNoAccuracy` < 0.6: after its first reply 「……它好像什么都不懂。」, after the second 「也是。什么都还没教过它。」 followed by the pointer 「摆渡众包上有一桌「常识判断」……」; when the accuracy first reaches 0.8 and it answers again, 「……这次答对了。它是真学会了。」. The reply that carries the early love line (「……它连她是谁都不知道。」) counts but stays silent here. The to-do note (`XgGuide.BestDataDesks`) names 常识判断 first while it is still at chance and the desk has fewer than 40 labels.
>
> Bot (`Tools/XingGuang/bot 90`, 2026-10-08): completes, careless still goes bankrupt at 06:00, `--guide-check` 0 faults. The route is slower because the bot spreads its labels over one more stage-1 desk (and chases one more S grade): ability 2 at 08:29 (was 06:13), ability 3 at 11:11 (07:15), chapter complete at 44:36 (33:25).

## 本地大模型（Qwen3.5-4B）

自由聊天和"灵光开口"由本机运行的 Qwen3.5-4B（Q4_K_M 量化，约 3.0 GB，Apache 2.0）驱动，完全离线。代码在 `Assets/LingGuangV05/LocalLLM/`。

- **启动**：`LocalLlm` 在进入场景后启动 llama.cpp 的 `llama-server`（只监听 127.0.0.1:18088），退出 Play 或游戏时会一并关掉。在 M2 Max 上每秒约 44 个 token，一条回复约 2 秒。
- **模型位置**：`Assets/StreamingAssets/LingGuang/Models/Qwen_Qwen3.5-4B-Q4_K_M.gguf`，会原样打进安装包。编辑器里另外也会去项目根目录的 `Models/` 找。
- **服务程序位置**：`Assets/StreamingAssets/LingGuang/llama/`，包含 llama-server 和 14 个库（llama、ggml、Metal / CPU / BLAS 后端、OpenSSL、libomp），共 28 MB，已自带全部依赖。找不到时再退回 Homebrew 装的版本。
- **关闭思考**：启动参数 `--reasoning-budget 0`，请求里再带 `enable_thinking: false`。
- **人设**（`LlmPersonas`）：
  - 老周：拿到存档里的真实数据（钱、显卡、电费、样本、准确率、论文），引用的数字必须一致。
  - 表姐、网吧群（阿杰 / 老板 / 小刚轮流接话）。
  - 所有人都活在 2016 年 5 月底，像真人打字，不承认自己是 AI。
- **灵光开口**：发表第一篇论文后，YY 里出现联系人「灵光」，同时弹出提示。它说话分三个阶段：
  - 1–2 篇论文：只会蹦词，每次最多 6 个字，夹着"是""否"
  - 3–6 篇：简单短句
  - 7 篇以上：说话流畅
  
  标注台上，它还会对当前这张卡用一句话说出自己的判断理由。
- **兜底**：模型或服务程序不存在、启动失败或请求失败时，退回预设回复（老周用关键词帮助），游戏照常能玩。
- **打包**：
  - 重新生成服务程序目录：`python3 Tools/LocalLLM/bundle_llama_macos.py Assets/StreamingAssets/LingGuang/llama`。脚本会复制依赖、把引用改成 `@loader_path`、去掉编译进库里的 Homebrew 后端路径，并 ad-hoc 签名。
  - 2026-10-03 打出的 macOS 包在 `Builds/LingGuang-macOS/LingGuang.app`（3.1 GB）。实测运行时用的是包内的服务程序和模型，11 秒后可以对话，退出游戏时服务一起关闭。
  - 打包时只指定了 Main.unity，没有改 Build Settings 的场景列表。
- **限制**：
  - 只支持 Apple Silicon（arm64）。Intel Mac 和 Windows 上没有对应的服务程序，会自动退回预设回复。
  - 安装包没有签名和公证。发到别人的 Mac 上时，Gatekeeper 可能拦截，需要签名公证，或者对方执行 `xattr -dr com.apple.quarantine LingGuang.app`。
  - 3 GB 的模型在 Assets 里，如果用 Unity Version Control，会被提交上去。

## Laya（2026-10-03 本机实测未达标，未接入）

Laya 是 Convai Innovations 在 2026 年 9 月以 Apache 2.0 开源的非自回归决策模型。它不生成文字，是非题（noul）直接给出"是"的概率，和灵光"只会答是 / 否"的设定很契合。

- 中文测试使用 `laya-multilingual`：mmBERT-base，3.22 亿参数，实际权重约 644 MB，另有约 34 MB 的分词文件，词表 25.6 万。
- 官方有 ONNX / INT8 导出脚本，但不能直接认定兼容 Unity；本机 M2 Max 的 PyTorch CPU 中文单题中位耗时约 48 ms，不代表 Unity 中的运行表现。
- 开箱准确率低，需要微调和校准（官方给了 Kaggle 微调 notebook）。
- 两种接入方式：
  - 原生实时推理：先验证内置 tokenizer 和 Laya 输入格式，再验证浮点 ONNX 导入以及 Unity 支持的权重量化。
  - 离线算好答案、打包成数据：这只是缓存建议，不应标成运行时“真 AI”。
- 已完成 Python 实测：中文均衡集 **88/180（48.89%）**，游戏自然抽样 **233/500（46.60%）**；没有达到预先设定的 75% 总体 / 60% 单题型门槛，因此没有接入正式建议，也没有把弱模型打包进游戏。
- 工具：`Tools/XingGuang/laya/`；完整报告：`Documentation/Laya/REPORT.md`。复用本游戏 `XgLogic.cs` 生成 680 行题目，中文/英文及两种输入格式共 1,720 次真实推理，保存逐题概率与证据。
- 更新原接入假设：已安装的 Inference Engine 2.6.1 有 Hugging Face 分词器支持，应先测 token ID 一致性，而不是先手写中文分词。官方动态 INT8 ONNX 使用的 `MatMulInteger` 等算子不被当前 Unity importer 支持；200–300 MB 包体积未经验证。
- 来源：https://github.com/NandhaKishorM/laya ，https://huggingface.co/convaiinnovations/laya

## 与家里的经济共享

- 算力 = `ChapterOneSim.HeartbeatsPerSecond`，显存 = `MemoryCapacity`，¥ = `S.money`。
- 训练时显卡按额定功率的一半额外耗电，记在同一张电费单上。

## 存档

全部存在一个文件里：`chapter-one.sav`。

- `GameState.labState`：灵光进度（`XgState` 的 JSON）。
- `GameState.chatState`：YY 聊天记录（`YYState` 的 JSON），每个会话最多保留 200 条。
- `ChapterOneRuntime.Saving` 在写盘前触发，灵光和 YY 在这时把状态写进去。读档、重新开始时，两者都跟着主存档恢复，聊天记录和安装状态不会对不上。
- Main.unity 的 `useDiskSave` 已改为 1：编辑器里 Play 也会读写存档，开场只播一次。想重看开场，删掉存档或在游戏里「重新开始」。
- 开发期间用过的 `persistentDataPath/LingGuangV05/XingGuang/` 和 `.../YY/` 两个目录已不再使用。

## 剧情

`ch1_story.json` 删掉了依赖旧玩法（题卡、神经元棋盘、入学考试、垃圾邮件订单）的节拍，新增老周跟着新玩法说话的节拍。触发信号是 `lg.trainable`、`lg.epoch`、`lg.assess`、`lg.checkpoint`、`lg.gradeA`、`lg.gradeS`、`lg.auto`、`lg.autotrain`、`lg.contract`、`lg.nan`、`lg.combo50`，每种只在第一次达成时由 `XingGuangController` 发出；老周对 A、S、crontab、50 连击各有一句。第一章的结局（原本是入学考试）暂时没有了。

## 测试

- `dotnet test Tools/XingGuang/tests/Tests.csproj`：49 个测试（含加薪 9 个）。训练动作、评估与评级奖、技能树门槛、自动化频率、连击、前方高能、老司机题、每张桌是/否各半、围棋气数用独立的洪水填充复核、日期过滤。
- 抽样看题：`dotnet run --project Tools/XingGuang/sample`。
- Unity EditMode（全部程序集）：316 个全部通过。唯一失败的是 `GameplayLocalizationTests.KnownNpcHelpTranslatesCurrentNumbersWithoutChangingSimulation`，原来就有，测的是已经不再使用的旧版老周回复。
- 平衡机器人：`dotnet run --project Tools/XingGuang/bot -- <分钟> <家里每卡每秒收入>`。2026-10-03 一个按「标 3 秒、按 3 秒」玩的机器人：0:15 第一次训练，3:46 MNIST B，11:16 第一篇论文，29:00 AlexNet，30:30 LSTM，33:00 crontab，44:00 守护进程。

## The spider (2026-10-07)

灵光 has a body: a small spider that lives on the desktop above every window and below the taskbar and its popups. Credit: 「灵感来自 @rybinfx 的网页蜘蛛」 / "Inspired by @rybinfx's web crawler". The idea of a spider walking on a text page and glitching the words it touches is theirs. The code is an independent implementation written from the visual behaviour only; nothing from github.com/cyohei9907/web-crawlers (no license) was read, copied or translated. The same line is shown in the crawler page's footer.

- **Rules** (pure, tested in `Tests/Editor/XgResidentTests.cs`, `XgCrawlTests.cs`): `Core/XgResident.cs` (presence, growth by ability: 4 legs at ability 1, 6 from 2, 8 from 5; size 0.7 → 1.2; privacy: it keeps off 晴雯's YY conversation once `loveConsent == -1`; the perch beside the labelling card; the watching mood `XgResidentMind`), `Core/XgSim.Spider.cs` (first appearance, answering cards), `Core/XgSim.Crawl.cs` (the 摆渡百科爬虫 data source), `Core/XgCrawlArticle.cs` (generated 2016 encyclopedia pages from the game's own corpora: XgMemes topics and hot words, the Tang poems, the era news headlines).
- **Desktop** (`Desktop/`): `XgCrawlerSpider.cs` (the walker: two-segment legs solved by the law of cosines, alternating gait, footholds on word rects), `XgSpiderFx.cs` (threads to read words, glyph bits sucked into the abdomen, pulses up the touching leg, the digesting glow, the credit ring, thinking moments with a ripple, threads reaching for the card), `XgResidentSpider.cs` (the resident: reads every visible TextMeshPro text one window at a time, glitches touched words by editing their vertices and always puts them back, perches at the 标注台, hides during cutscenes and fullscreen video), `XgCrawlerPanel.cs` (the crawler page inside the 数据 page).
- **First appearance**: once one epoch has been trained and nothing else is playing, 灵光 opens by itself on 数据 with the crawler already reading a 摆渡百科 page; the inner voice reacts, the spider turns to look at the cursor, then reads on. `XgState.spiderIntroDone` makes it play once; before it the desktop has no spider.
- **Watching the labelling**: on the 标注台 it walks to a perch beside the card, breathes, tilts, taps words, leans in (threads reaching for the card) when you hesitate, pulses after each answer, looks longer when you went against its guess, and wanders off after 12 s without an answer. Per card it may answer itself: chance 8% at ability 1, +4% per ability, at most 30%, only on desks whose checkpoint is at 60% or better. It presses the checkpoint's own guess, so it is right exactly as often as the checkpoint, and it settles like an automatic label (automatic pay × credit multiplier, no raise, no combo, spot checks, wrong labels become noise). The feed says 「它替你按了『是』」.
- **Quiet**: the nav toggle 「让它安静一会儿」 parks it in a corner and stops it answering; saved as `XgState.residentQuiet`. 减少特效 keeps the highlight and colour change only, fewer threads and motes, no ripple or jitter.

### 摆渡百科爬虫 (data source)

Offer `news.crawl` (`XgDataSource.Crawl`) on the 数据 page under 人民日报语料. Stage 3 (2016-08). One run costs ¥8 (= ¥0.04 × 200, the crowd task's price per label), lasts 60 s and can grab at most 200 words; every grabbed word is one row of `news`, stored like crowd rows (`dataExtra`, with 12% junk rows in `crowdNoise` that 数据清洗 halves), so the data bar counts it at the dataset's weight less the noise penalty. Closing early keeps what was grabbed. At the bottom of a page the crawler follows a link to the next article. Very rarely a darker spider's legs reach in from the edge for a few seconds and take nothing.

## 已知未完成

- 技能树拖动：桌面画布是 Screen Space – Camera、缩放 0.01，以前用像素位移除以缩放，拖一下会飞出 100 多倍；现在用事件相机把指针换算到本地坐标，1:1 跟手，松手有惯性，滚轮平滑缩放，顶部有「视觉 / 序列 / 研究 / 可买 / − / +」跳转按钮。按住节点要先停 0.15 秒才开始充能，避免拖动时误买。订单页的检查点卡拖拽同样修了。
- 设计稿里「数据包拖到显卡上安装」没做：数据包在技能树和标注台右栏直接买。
- 评估卡不是全屏遮罩：手按时在曲线上方弹出，自动评估时缩小到右下角，不挡训练按钮。
- 连击续命时间按桌区分（设计稿写的是统一 1.5 秒，逻辑题和文字题读题来不及）。
- 新题库的英文界面只翻了 UI，梗本身保留中文。

- Codex 写的 Play QA（`ChapterOneSplitPlayQa`、`ChapterOneNativeChatQa`、`YY2010Qa`）检查的是旧版灵光、原生 YY 和邮件标注，需要按新界面重写。
- 「家庭」的工作订单页仍然显示旧的垃圾邮件过滤订单，在新玩法里不会产生收入。
- 新台词还没有英文翻译。
