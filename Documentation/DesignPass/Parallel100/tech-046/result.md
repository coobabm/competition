# tech-046：earlystop（早停）核对结果

日期：2026-10-13（Asia/Singapore）。范围仅为研究节点 `earlystop`。生产源码只读；未读新策划正文，未启动 Unity/游戏，未访问玩家存档，未修改其他任务目录。

## 结论

**当前 XgTechTreeCopy 对 earlystop 的名称、解释与效果正确，无需改动，不生成 changes.patch。**

- 名称回退到目录中的「早停 / Early stopping」，属于正式专业术语。
- 通俗解释明确说的是「原理」，不是购买承诺。
- 效果使用 `Speed(sim, 1.05)`，写明训练处理量、取整和上限，并明确自动停训来自独立的自动化早停等级。没有把研究节点误写成自动停止、自动回滚或直接提分。
- 现有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md` 已记录同一处理量语义和上述独立节点区别。本任务复核该实现，不重复制作修正补丁。该记录中的既有编译与检查结果不视为本聊天执行的测试。

## 可直接采用的中英文完整文案

以下为该节点完整说明参考，补齐本次要求的生效时机与叠加规则；不是待强制应用的修复补丁。现有简短科技页文案可以保留。

### 中文

**名称：** 早停

**解释：** 早停的原理是：练习后用另一套题检查成绩，如果成绩不再提高，就停止继续练习。

**效果：** 购买后无需装备，视觉和序列训练的处理量系数 ×1.05（提高 5%），与其他处理量倍率相乘。手动和自动训练都适用，从下一次训练结算计算卡数时生效。实际每轮处理的训练卡数会取整，并受算力、模型配置及每轮 4–320 张的限制，可能看不到卡数变化；离线模拟每轮固定 12 张，不享受此项卡数加成。成绩变化需训练后评估。自动停训由自动化等级「早停」另行提供。

### English

**Name:** Early stopping

**Explanation:** Early stopping means checking progress on a separate set of questions after practice, then stopping practice when that score stops improving.

**Effect:** Buying this research requires no equipment step. It multiplies the training throughput factor by 1.05 (+5%) for both vision and sequence training, stacking multiplicatively with other throughput factors. It applies to manual and automatic training when card counts are next calculated at epoch completion. Actual training cards per epoch are rounded and depend on compute, model settings and the 4–320 card limit, so the count may stay unchanged. Offline simulation uses a fixed 12 cards per epoch and does not receive this card-count bonus. Train and evaluate to see any score change. Automatic stopping is provided separately by the Early stopping automation level.

## 生效、单位、叠加与边界

1. **可见/可买不等于生效。** 目录节点属于研究（`XgNodeKind.Research`，研究分类 `Feel`），不是可装备架构。阶段 4 起显示，前置为 `lrschedule`。购买仍须满足前置、阶段和资金检查。目录原价为 2500，当前 `NodeCost` 按 `StoryResearchPrice = .25` 得到 ¥625；价格应继续由页面动态显示，不硬编码进效果。
2. **购买后持有即生效，无需装备或额外按钮。** `BuyNode` 成功后将 id 加入 `S.unlocked`，`FeelSpeed` 每次通过 `Has("earlystop")` 读取持有状态。`Status` 已拥有时不允许再次购买；同一节点只乘一次 1.05。
3. **训练结算读取当前倍率。** `BeginEpoch` 只记录训练启动、进度和时长，没有预存卡数；`TickEpochs` 完成时调用 `TrainEpoch`，后者才调用 `CardsPerEpoch`。因此在一轮进行中购买，该轮随后结算计算卡数时也会读取新倍率；不是必须等下一轮启动。单轮等待时长不因此缩短 5%。
4. **单位是处理量系数，落地为训练卡/轮。** 1.05 是无单位乘数，不是准确率增加 5 个百分点，也不是增加 5 张卡或每秒固定多 5 张。未取整的理论处理量提高 5%；每轮实际整数不保证提高 5%。
5. **叠加采用相乘。** `FeelSpeed` 中各持有研究逐项相乘；该值再与 `SpeedResearch` 的其他系数相乘，并进入 `CardsPerEpoch`。例如既有外部处理量系数为 1.2 时，本节点使乘积成为 1.26，不是 1.25。早停自身不可重复购买叠层。
6. **手动/自动、视觉/序列共用。** 卡数公式共用 `SpeedResearch`；手动连击与自动训练系数仍分别保留。不存在只对文字、只对某架构的 earlystop 条件。
7. **不承诺自动停训。** 真正停止的条件由 `AutoTrainLevel == 4`、非手动评估及停滞计数共同控制，不读取 `Has("earlystop")`。此处只用于确认两者独立，不修改自动化节点文案或行为。
8. **没有额外预训练规模倍数。** `ScaleItems` 不含 earlystop，不应添加预训练规模或自动保存最佳模型等奖励承诺。

## 原文件证据

下列行号来自本次读取，整合前按方法/片段重定位。

| 文件（均位于 Assets/LingGuangV05/XingGuang/） | 证据 |
| --- | --- |
| `Core/XgCatalog.cs:308–309,369–370,449,607` | `ResearchNode` 创建研究节点；`R("earlystop", "lrschedule", 2500, 4)`；正式中英文名称与 `Feel` 分类。目录旧效果仍有「分数不涨就停」的简写，科技页已由文案层明确区分；本任务不改共享目录。 |
| `Core/XgTechTreeCopy.cs:13–15,46–49,101,128–129,164` | 名称/说明先保留 mystery/atlas 回退；earlystop 原理说明；`Speed(sim, 1.05)`；明确指向自动化早停。 |
| `Core/XgSim.cs:188,194–205,252–279` | 持有判断、购买条件、扣费后解锁；earlystop 无装备分支、无额外启用状态。 |
| `Core/XgSim.Collection.cs:70–82` | `if (Has("earlystop")) s *= 1.05;`，并与其他支线研究乘数累乘。 |
| `Core/XgSim.cs:347,554–557,639–675` | `SpeedResearch` 组合研究倍率；旧步数公式亦使用该系数；实际 `TrainEpoch` 以 `CardsPerEpoch` 结算训练、随后评估。 |
| `Core/XgSim.Board.cs:42–43,191–199` | 离线返回 12；通常卡数乘 `SpeedResearch`；`Math.Round` 后限制在 4–320。 |
| `Core/XgSim.EpochAction.cs:13–25,29–50` | 启动未预存卡数；完成才调用 `TrainEpoch`。 |
| `Core/XgSim.cs:305–316,727–758` | 自动化等级与每轮评估；停止条件不读取 earlystop 研究持有状态。 |
| `Core/XgSim.Progression.cs:131–148,154–164` | 实际购买价格折算、阶段可见性及购买阻挡。 |
| `Core/XgSim.Finale.cs:75–84` | 预训练规模清单无 earlystop。 |
| `Core/XgTechTree.cs:127–129,146`；`Desktop/XgTechTreePage.cs:428,758–772` | 保留阶段可见过滤；mystery 提前返回；普通节点最终调用文案层。 |

## 未发现节点的神秘性

保留现有 `NodeVisible` 和 `NodeMystery` 规则。本节点在阶段不足时不展示；不要借本报告把未到阶段的节点加到界面。现有 mystery 特判对应其他节点，不能为了 earlystop 改写其名称、提示、价格或可见规则。此任务没有做任何相关生产改动。

## 验证状态与待整合项

- **已完成静态检查：** 目录、持有/购买、生效读取点、卡数限制、独立自动化停止分支、科技页接入及隐藏回退。没有执行模拟器测试，未声称运行 PASS。
- **实际运行：未执行。** 由整合负责人统一编译、构造纯内存测试状态后验证。不得用真实存档。
- **视觉验收：未执行。** 中英文说明长度、换行、隐藏状态及购买前后截图由主聊天处理。
- **待整合：** 无生产补丁需要应用。保留现有 earlystop 文案即可；完整双语文案可供同页后续统一补充生效/叠加说明时采用。
- **建议真实边界验证（尚未运行）：** 使用相同模型、数据与算力，独立切换 earlystop 持有状态，分别比较手动/自动及两条训练线；验证仅满足前置不加成、购买成功即读取新倍率、重复购买不再扣费或叠层；在轮次进行中购买并观察结算；验证离线固定 12、4 张下限和 320 张上限不会被突破；观察仅持有研究时不会凭停滞自动停止。
- **数值边界预期（静态手算，不是运行结果）：** 未取整处理量 20→21，8→8（8×1.05=8.4 后取整），1→4（下限），319→320（上限）。比较真实训练时应控制手动连击、模型自动配置、其他研究与算力，避免把其他状态变化归因于 earlystop。

## 依赖快照与结束复核

本次读取时 SHA256 如下。结束复核结果见 `dependency-check.json`；该文件只记录源码哈希，没有玩家数据。若将来整合时依赖变化，应重新核对，不把本报告当作永远有效的运行证明。

| 文件 | SHA256 |
| --- | --- |
| Core/XgCatalog.cs | 1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7 |
| Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Core/XgSim.Collection.cs | E797F4FB7307B5AFCF168EC46889FC7EE5F669ED40CE0A4238CB851DC37F86B3 |
| Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Core/XgSim.EpochAction.cs | 73CE990C4A6E454CFD65C5D5F47C06B748CE93D27B9E651DEC3F4A4857F6A747 |
| Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Core/XgSim.Finale.cs | E565169876C92B4E434BDE1421B995F785DB8E160747268F32F30B95193CC11B |
| Core/XgTechTreeCopy.cs | 308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8 |
| Core/XgTechTree.cs | C2795A20598FA9B22F91A4678AD7946F97AC0D03E518A62A3108A80350D0414D |
| Desktop/XgTechTreePage.cs | F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523 |

本交付只完成 earlystop 静态文案核对，不代表整份策划或全局原则完成。
