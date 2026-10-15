# tech-031：backprop 节点文案核对

日期：2026-10-13（Asia/Singapore）。范围仅 backprop。交付未应用到生产源码。

## 结论

当前 `backprop` 是内部 ID，不是当前显示名称，也不是架构节点。`XgCatalog` 将它定义为第二阶段、以 mlp 为前置的 Research 节点，正式显示名称已为 **小批量训练 / Mini-batch training**。因此不应仅凭旧 ID 把科技页改名为“反向传播 / Backpropagation”。名称与原有通俗解释方向正确，保留名称，不制造名称补丁。

已有 `XgTechTreeCopy` 正确写出训练处理量 ×1.2 和取整/上限限制，但未交代生效时点、乘法叠加及离线模拟例外。解释采用动作句，也容易被读成购买后真正启用了批量梯度更新。确认当前训练逐卡调用 `Board.Train`，该节点的专属判断只提供处理量倍率，因此提交 `changes.patch`：仅更换 backprop 的两行双语解释/效果；不改共享 Speed 辅助方法，不改模拟数值。

已先阅读现有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`。该记录已完成通用文案层，本交付仅补该节点的条件与例外。不读取新的策划正文，不涉及其他词条整改。

## 可直接采用的双语文案

中文名称：小批量训练

中文解释：小批量训练的原理是：先看一小批例题，汇总调整方向，再更新网络。

中文效果：购买后无需装备；两条训练线结算时，每轮处理卡数的计算值 ×1.2，与其他处理量倍率相乘。实际卡数受算力、取整和上下限影响；不会直接缩短一轮时间或提高准确率，成绩需训练后评估。离线模拟仍固定每轮 12 张。

English name: Mini-batch training

English explanation: Mini-batch training means combining the suggested changes from a small group of examples before updating the network.

English effect: No equipment step is needed after purchase. When an epoch completes on either track, its calculated card count is multiplied by 1.2, stacking multiplicatively with other throughput factors. Compute, rounding and limits determine the actual count. This does not directly shorten epochs or raise accuracy; train and evaluate to see the score. Offline simulation remains fixed at 12 cards per epoch.

补丁中离线张数读取 `XgSim.OfflineCardsPerEpoch`，没有新增硬编码数值。名称继续由目录提供。

## 解锁、购买与真实数值合同

- 可见/可购买：第二阶段起显示；未拥有时，仍须拥有 mlp、满足资金条件。显示出来或只拥有前置均不授予 backprop 效果。
- 当前价格：基础 200 × 第二阶段系数 1.6 × 故事价格系数 0.25，最终向上取整为 ¥80。价格仍由现有 UI 动态显示，不写入文案补丁。
- 购买成功把 ID 放入 `S.unlocked`；没有装备、激活开关、下载或独立训练解锁步骤。仅购买一次，不能重复购买叠层。
- 普通训练的单位为 **张/轮**；1.2 是无量纲倍率，即取整/截断之前计算量增加 20%，不是准确率增加 20 个百分点，也不是每秒速度承诺。
- 对两条训练线、手动与自动结算均适用。计算最终先 `Math.Round`，再限制到 4–320 张。自动轮另乘自身半效系数；其他处理量系数与该 1.2 相乘。
- 叠加例：weights 与 backprop 的两个系数为 1.1 × 1.2 = 1.32，并非 1.3。这里仅说明该节点如何叠加，不扩展其他词条。
- 倍率在 `TrainEpoch` 结算读取。已经开始但尚未结算的一轮，如果结算前成功购买，也会读取新倍率；不必声称一定从下一轮开始。
- 购买路径虽会重配并调用评估，但 backprop 本身没有直接提分项。训练结算处理更多卡并更新进度后，才可能影响成绩；不保证任何一轮准确率都上涨。
- `OfflineSimulation` 为真时，`CardsPerEpoch` 在读取倍率前直接返回 12，以上倍率不增加离线模拟卡数。这里只描述现有分支，不承诺存在额外离线奖励流程。
- 内部 `StepsPerSecond` 同样乘该系数，但已查看范围内未发现调用者；不把它作为当前玩家可见的秒数收益依据。

## 原文件证据

所有路径均相对工程根 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`；完整 SHA256 在 `base-sha256.json`。目标文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。

| 文件与行 | 证据与含义 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306–309,348–356,396,618` | ResearchNode 使用研究项名称；stage=2、parent=mlp；正式名称为小批量训练 / Mini-batch training。 |
| `Assets/LingGuangV05/XingGuang/Core/XgItemLore.cs:99–101` | 现有科普也描述小批量，不是把误差逐层反传的算法名称。仅作为名称一致性佐证，真实效果仍以模拟为准。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,45,51,81,162` | 名称回退到目录；解释已有小批量描述；效果原来使用通用 Speed(1.2)；神秘性入口在分支之前。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:131–148,268–269` | 成本与阶段门控；`Has("backprop") ? 1.2 : 1` 直接加入乘积，无运行结构或训练线限制。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188,194–205,252–276` | 所有权、前置/资金校验、购买即解锁；已拥有为 Owned，不能再次购买。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:41–43,191–199,211–224` | 默认 UseBoard；普通轮 4–320 张，离线固定 12；逐卡训练，未见 backprop 触发批量梯度更新。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:639–675` | 结算读取 CardsPerEpoch；增加 steps/epoch，训练与评估，后续按轮次评定。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.EpochAction.cs:13–25,29–49` | 开始训练时记录时长；结算时调用 TrainEpoch，倍率不修改轮次时长。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:93–109,127–137,144–146` | 单次节点、可见性与购买沿用模拟；名称接入 Copy。 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:772,781–782` | 说明和前置名称已接入 Copy，无需额外接线。 |

## 神秘性

保留 `XgTechTree.Visible → NodeVisible` 的阶段门控，以及 Copy.Name/Note 最前面的 `NodeMystery` 回退。backprop 本身没有额外神秘名称分支，未拥有且第一阶段时由可见性隐藏。补丁只替换其已显示的说明，不展示后续链节点的名称、价格或效果。

## 验证与待整合

本轮完成源码静态追踪及补丁上下文检查；没有编译、运行模拟、启动 Unity/游戏、进行截图验收或接触玩家存档。现有任务记录的运行结果不冒充本轮验证。

整合负责人：01a130ed-4e42-7ab2-ba1e-fdbf2983c88f。仅交付文件，不向其他聊天发消息。

- 应用前核对目标 SHA256；如不一致，只按 backprop 两处上下文重新合并，不覆盖整个文件。补丁头已注明目标和 SHA256。
- 主聊天统一编译，并在隔离内存/场景验证：未买/已买、第一/第二阶段、缺 mlp、钱不足、成功购买、重复购买；确认只成功购买授予倍率且无需装备。
- 真实边界：同样配置对照未截断的 100→120 张；取整处未必精确增加20%；达到上限仍为320；低端夹紧仍为4；离线始终12。以能得到这些计算区间的实际配置为前提，不用重写公式的测试替代生产函数验证。
- 两条线与手动/自动均验证；验证倍率与现有加成相乘；在一轮中途购买并等待结算，确认没有额外缩短进度条时长。
- 验证买后不承诺直接加准确率，并查看训练后真实评估。统一截图检查中英长说明在最小窗口、缩放、提示框中不裁切；这是未完成的视觉验收。
- 本交付仅涉及当前授权的单项文案，不代表全局原则或整份策划完成。

结束前复核：全部 9 个依赖文件的 SHA256 与交付基线一致；git apply --check（忽略换行/空白差异）成功。仅检查补丁可应用性，未实际应用，不等同编译或运行通过。
