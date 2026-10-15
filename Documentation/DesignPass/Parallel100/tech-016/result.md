# tech-016：cooler（散热器）文案核对

日期：2026-10-13（Asia/Singapore）。结论：保留现有正式名称与通俗解释，只建议替换效果行；未修改生产源码或模拟数值。

## 核对结论与已有工作

已阅读现有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`。它已将旧目录中的“烧卡概率减半”改为“训练磨损 ×0.5”。继续追查实际宿主后确认：这个系数传入烧卡随机判定，代码没有独立的 GPU 耐久度、磨损量或散热器温度加成。“零磨损”还容易被理解为停止累计连续训练时间，但 `Wear` 在保护期间仍累计训练时长。故仅将本节点效果换为玩家能理解的烧卡风险，并补购买即生效、触发条件和硅脂优先关系；不重做已正确的名称和解释。

边界：只写本专属目录；未启动 Unity/游戏、未编译、未读取玩家存档，未提交/推送/打包或向其他聊天发消息。未主动打开新策划正文；初始宽范围关键词检索意外命中既有逐条清单的一条散热器摘要，随即改为源码及已有任务记录核对，没有继续展开该清单。

## 可直接采用的中英文文案

中文名称：塔式散热器

中文解释：用更好的散热器带走训练时产生的热量。

中文效果：购买后自动生效，无需装备或重新训练。连续训练过久时，烧卡风险按 ×0.5 计算；仅在电力系统启用且至少有两张显卡时可能触发。硅脂保护期间不会因训练烧卡，保护结束后恢复本效果；同一道具不重复叠加。

English name: Tower cooler

English explanation: Use a better cooler to carry away heat from training.

English effect: Active automatically after purchase; no equipping or retraining needed. During prolonged continuous training, burn-out risk is scaled by ×0.5; training burn-outs require the power system to be enabled and at least two GPUs. Thermal-paste protection prevents training burn-outs while active; this effect resumes when it expires. This item does not stack with itself.

补丁中的 0.5 仍从 `XgSim.CoolerWearFactor` 读取，不硬编码模拟参数。解释是散热器的通俗原理，不承诺游戏温度读数下降、训练加速、电费下降或模型提分。

## 原文件证据与生效合同

以下路径均相对工程根目录，行号以交付快照为准；完整 SHA256 见 `source-sha256.json`。

| 证据 | 实现及含义 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306–309,369–370,396,651–652` | cooler 为 Research 节点、Cooler 研究类别；前置 mlp，阶段 2，目录基础价格 125。不是可切换的模型架构。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:131–164` | 显示/购买受阶段限制；实际价格为 `Ceiling(125 * .25) = ¥32`，不能把目录 125 直接当当前支付金额。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188,195–208,252–278` | 只有达到前置并成功购买，才加入 unlocked；仅看到节点或达到购买条件不会生效。已拥有节点不可重复购买。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Consumables.cs:109,138–146` | `pasteSeconds > 0 ? 0 : Has("cooler") ? .5 : 1`，无量纲倍率，非减少 50 个百分点。购买后下一次训练结算就读取此值，无装备按钮、预热或重新训练要求。 |
| 同文件 `:190–196,217–221` | 使用硅脂设定 120 秒保护并调用 CoolCards；保护时间消耗到 0 后重新采用散热器倍率。散热器自身没有时限。 |
| `Assets/LingGuangV05/XingGuang/Desktop/XingGuangHost.cs:60–65` | 实际宿主实现 TrainScaled，将倍率传入家庭模拟 BillTraining。宿主未 Live 或电力未启用时不结算。纯 IXgHost 替身若未实现 IXgTrainingBill，不能据此断言散热器实际生效。 |
| `Assets/LingGuangV05/Core/ChapterOneSim.Bills.cs:193–205,216–224` | 有效时长、未破产且电力启用才进入结算。先累计 trainingStreak，再检查连续时长、显卡数量与保护倍率。随机比较 `NextUnit() < wearChancePerSecond * seconds * wearFactor`。 |
| 同文件 `:238–249` | 即使命中烧卡判定，仍须能选出移走后满足当前显存需求的显卡；不会烧最后一张，实际烧卡次数无固定减半保证。 |
| `Assets/LingGuangV05/Core/Models.cs:128` | 默认连续训练阈值 30 秒、间隔超过 5 秒重置；基础风险系数 .001/训练秒。均为配置默认值，不在文案中写死。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:662`、`XgSim.Finale.cs:197`、`XgSim.Progression.cs:564` | 普通训练轮次、预训练片段与项目 GPU 工作均通过 ChargeTraining，共用本项系数。它并非仅保护某一种架构或某条训练线。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs:75–85` | cooler 不在预训练有效规模倍率表中，不新增规模倍率承诺。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,33,51,99,183` | 名称与解释已适当；只改效果行。神秘节点提前返回原始神秘文案。 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:758–772` | 神秘节点在详细文案拼接前返回；补丁不改此分支，不泄露未知词条。 |

解锁/购买/装备/训练的区分：到阶段 2 且拥有 mlp 后才具备购买前提；成功支付后即“拥有/解锁”，从下一次结算自动采用系数。无需单独装备或完成一轮训练来激活。实际是否烧卡还依赖上述系统条件和随机判定。

叠加：散热器是一个布尔拥有标记，不会多买后变成 0.25；硅脂优先取 0，结束后取 0.5。其他电价与耗电倍率走独立参数，不再乘进烧卡系数。

## 概率与单位边界（供整合验收）

设一次结算时长为 s 秒，配置风险系数为 c/秒，散热系数为 f。满足前置条件时随机判定阈值为 c × s × f；若把随机数视为均匀分布，理想概率是 min(1, c × s × f)，之后还需能移走一张卡才产生实际损坏。0.5 是无量纲乘数，绝非每秒固定 50% 烧卡率。

例如默认配置，已经满足连续训练阈值、一次计费 1 秒：判定阈值从 0.001（0.1%）降为 0.0005（0.05%）；硅脂保护时为 0。极长结算片段可能令两边都饱和至必定命中，因而不写“所有情况下实际概率/烧卡次数恰好减半”。这不是建议修改数值。

## 补丁及待整合

- 文件：`changes.patch`，只替换 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs` 第 183 行的 cooler 效果，含前后上下文。
- 目标源文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- 应用前核对目标行与最新源码；其他并行节点若修改同一文件导致整体 SHA 改变，应人工合并本唯一分支，不覆盖整个文件。
- 生产 Assets 未修改；旧目录和道具/训练页的“概率减半”未在本任务范围修改，主聊天若统一跨页措辞可复用以上合同。

## 验证状态与主聊天待验收

本次完成静态源码追踪及交付文件核对；未执行模拟运行、编译或视觉验收，不沿用已有记录的 PASS 作为本补丁的验证结果。

以下为待运行边界，不是已通过测试：

1. 可见但未购买、购买失败、购买成功：系数依次保持 1、保持 1、变为 0.5；再次购买不扣款且不叠加。
2. 未拥有/已拥有 cooler 且硅脂剩余时间大于 0：均为 0；剩余时间恰为 0 时分别恢复 1/0.5。
3. 连续时长低于 30 秒不判定，结算后恰到 30 秒开始可能判定；间隔恰为 5 秒不重置，超过 5 秒才重置（默认配置）。
4. 一张卡不烧；两张卡但移走任一卡均无法满足显存时不烧。电力未启用、无有效结算时长、已破产均不进入烧卡流程。
5. 固定随机输入检查 1 秒阈值附近，以及长时长阈值超过 1 的情况，不用少量随机跑次判断“数量减半”。
6. 硅脂保护中累计训练时长仍可增长；仅使用硅脂当次调用 CoolCards 重置。验证文案不会被理解为散热器延长到达风险阈值的时间。
7. 中英文科技页窗口最小尺寸、缩放及提示卡长文换行验收；未知节点仍显示神秘提示。统一编译与实际场景截图由主聊天负责。

依赖结束复核记录见 `verification.txt`。
