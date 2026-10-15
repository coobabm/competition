# tech-024 — label.audit 标注质量抽检

日期：2026-10-13（Asia/Singapore）。范围仅此节点；未读取新的策划正文，未编辑生产源码、模拟数值、玩家存档或全局任务清单。

## 结论

名称「标注质量抽检 / Labelling quality audit」准确，保留。当前解释与效果方向正确，但有三处需要补全：40% 是逐题概率而非固定比例；未说明购买后即时启用与单次购买不叠加；遗漏同节点已实现的手动数据清洗。交付 `changes.patch`，仅替换 `XgTechTreeCopy.cs` 中该节点的 Explanation 与 Effect 两个分支，不改名称、其他节点或玩法。

已先查询现有 `进度.md`、`队列.md` 中相关任务记录及 Parallel100 内 label.audit/抽检记录，未发现本节点专属既有修正；本目录起初无交付文件。没有将策划第 3 行、整份文档或全局原则标记完成。

## 可直接采用的中文文案

正式名称：标注质量抽检

通俗解释：抽查即将自动提交的答案，把部分错题留给你复核；也能手动清理已有数据里的错标。

真实效果：购买后即启用，无需装备或先训练；仅购一次，概率不叠加。每道进入自动提交分支的错题有 40% 概率转入人工复核，拦下的错题不直接进入训练数据；复核队列满时自动标注暂停。

另解锁数据集手动清洗：每批最多 10 条，每条 ¥2 并计 0.1 GPU秒；先清自身错标，再清外来错标。实际条数受剩余错标和经费限制，需有可用算力且研发未占用。清洗后噪声立即减少，不直接提高模型准确率。

## Ready-to-use English copy

Name: Labelling quality audit

Explanation: Check answers before automatic submission and send some mistakes to you for review; also let you manually clean wrong labels from existing data.

Effect: Active upon purchase, with no equipment or training step; one purchase only, with no probability stacking. Each wrong answer reaching automatic submission has a 40% chance of being sent for human review, keeping that mistake out of training data; a full review inbox pauses automation.

Also unlocks manual dataset cleaning: up to 10 labels per batch, at ¥2 plus 0.1 GPU-seconds per label. Clean your own wrong labels first, then outside noise. The actual count is limited by remaining noise and funds; available compute is required and research must not be occupying it. Cleaning reduces noise immediately without directly raising model accuracy.

## 原文件证据（本次源码行号）

所有路径相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。完整 SHA256 见 `source-hashes.json`。

| 文件与位置 | 静态依据 |
| --- | --- |
| `Core/XgCatalog.cs:462`（以下 Core 均位于 `Assets/LingGuangV05/XingGuang/`） | 将 `XgSim.LabelNodes` 加入真实目录；本节点不是根据理论虚构的词条。 |
| `Core/XgSim.Collaboration.cs:11–13,35,41–42` | 概率常量 `.4`；清洗上限 10、¥2/条、0.1 GPU秒/条；节点前置 `label.coop`，标价 ¥1500、最高 1 级。 |
| `Core/XgSim.Collaboration.cs:144–162,179–192` | 已拥有即 1 级；前置未购不能购买；成功付款后直接加入 `S.unlocked`。无装备步骤、训练步骤或重复升级概率。标价不受普通研究折扣影响，参见 `Core/XgSim.Progression.cs:132–137`。 |
| `Core/XgSim.Collaboration.cs:372–415` | `Route` 先处理低置信度或已识别金标题分流；其余错题在拥有协作及抽检时以 `Roll() < AuditProbability` 判定。命中则入人工队列，跳过自动收益、平台检查与错标写入。未命中也可能被平台检查拦下，因此不能把剩余 60% 全称为入库错题。 |
| `Core/XgSim.Collaboration.cs:20,24,372–374` | 协作取决于 `label.coop`；队列容量 20，满时不执行路由。40% 仅针对实际进入自动提交分支的错题，既不是所有题目的抽样率，也不是准确率增加 40 个百分点。 |
| `Core/XgSim.Collaboration.cs:109–139` | `CleanableNoise` 要求拥有节点、合法数据集、非研发状态、无 host 阻塞、有限正算力和足够经费。清洗条数取剩余噪声向下取整、批量上限和可支付条数的最小值；扣款成功后先减自身噪声，再减外部噪声，按实际条数调用 `host.Train` 并刷新相应运行数据。 |
| `Core/XgSim.DataSources.cs:413–419,446–450` | 外部噪声先按既有数据清洗倍率处理，再减累计清除量，最后下限归零；手动清洗是对当前噪声数量做减法，不把抽检概率再乘成其他概率。未在玩家文案中披露其他尚未发现节点的名称或效果。 |
| `Core/XgSim.cs:517–528` | `Evaluate` 刷新上限与派生指标；清洗本身不执行模型训练，也不保证准确率上升。 |
| `Desktop/XgTrainPage.Data.cs:60–71` 与 `Desktop/XgTrainPage.cs:250` | 拥有抽检才显示清洗按钮；显示按条费用与 GPU秒，按钮调用实际 `CleanNoise`。 |
| `Desktop/XingGuangHost.cs:53–57` | GPU秒交给训练账单处理，电力费用未开启时不计费；0.1 GPU秒不是要求玩家等待 0.1 秒的倒计时，也不是直接训练本地模型。补丁保留现有 UI 的计量口径。 |
| `Core/XgTechTreeCopy.cs:15,51,117,193`；`Desktop/XgTechTreePage.cs:757–772` | 原抽检描述和效果分别位于 117、193 行；名称/说明已有神秘节点保护，页面也提前返回神秘提示。补丁不移动保护、不改变可见性。当前 `NodeMystery` 仅适用于自动标注，抽检本身是可见但可能锁定的节点，不能将锁定等同于未发现。 |

## 生效与叠加合同

- 发现或满足前置只使购买成为可能；成功购买才拥有本节点。正常购买要求已有 `label.coop`，费用 ¥1500，且只有 1 级。
- 购买后的后续路由即时使用抽检，包括尚未路由的预取题；不回溯清除旧错标。已存在噪声需玩家点击清洗。
- 抽检是每次符合条件的错题路由的一次概率判断，不能重复购买加到 80%，不能保证每 10 道错题恰好拦下 4 道。
- 清洗可重复点击，累计减去实际清除数量，噪声不低于零。其效果与预防新错标进入数据同时存在，但不是两个概率相加或相乘。
- 自动标注仍受原有合格检查点、算力、暂停和队列状态限制；购买抽检本身不创建模型、不启动训练，也不会独立产生标注收入。

## 验证状态与待验证边界

静态检查已完成：目录接入、购买、路由、清洗、说明显示与神秘性保护均已沿实际源码核对。补丁是否能匹配当前源码以及结束前依赖复核结果见 `verification.txt`。没有启动 Unity/游戏，没有执行 C# 编译、模拟运行或视觉验收；以下均为供主聊天执行的待验证用例，不是运行通过结果。

| 待运行边界 | 应观察的行为 |
| --- | --- |
| 无前置、经费不足、合法购买、重复购买 | 前两者不获得节点；合法购买获得 1 级；重复购买不扣款、不增加概率。 |
| 正确题、低置信错题、高置信错题；随机数小于/等于 0.4 | 正确题不进抽检；低置信错题先走普通复核；高置信错题仅随机数小于 0.4 时被抽检（等于时不命中）。不要用小样本恰好 40% 作为断言。 |
| 队列 19 与 20 条 | 19 条可再接纳一条；满 20 条时路由不消费题目、不结算奖励。 |
| 噪声 0、0.99、1、10.9；钱包 ¥1.99、¥2、¥7 | 小于 1 条不可清洗；1 条且 ¥2 可清 1 条；充足噪声下 ¥7 最多清 3 条，扣 ¥6、传入 0.3 GPU秒；10.9 条且经费足最多清 10 条。 |
| 自身噪声 3、外部噪声 8，默认清洗且经费足 | 先清自身 3 条，再清外部 7 条，余外部 1 条；再次点击只能清剩余 1 条。 |
| 研发占用、算力为零、有阻塞、Spend 失败 | 不清除数据、不产生该次清洗扣费或 GPU 账单。 |
| 开启既有外部数据降噪，再执行清洗 | 从当前有效外部噪声减清除量，不把抽检 40% 与降噪倍率合并。 |
| 购买前后、清洗前后模型与最佳检查点 | 噪声统计即时变化，模型不会因购买或清洗凭空获得训练；准确率不应被宣称固定提高。 |
| 中文/英文提示，窗口最小尺寸与长文本 | 新说明无裁切、遮挡；费用与状态仍可见。较长英文文本必须由整合聊天实际截图验收。 |
| 尚未发现的节点与图鉴事件 | 原神秘名称、提示与隐藏价格路径仍生效，不显示其他未发现词条信息。 |

## 待整合

1. 仅整合 `changes.patch` 的两个抽检分支。补丁内含目标路径、当前源文件 SHA256 和逐行上下文。其他聊天可能同改此文件；哈希变化时按该节点的旧文案重新核对，不覆盖整个文件。
2. 统一编译、上述实际边界运行及中英文场景截图由整合/主聊天负责。此处没有声称这些验证已完成。
3. 本任务交付结束，不给其他聊天发消息，不创建聊天或子 agent。
