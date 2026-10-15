# tech-020 — label.auto 自动标注

日期：2026-10-13（Asia/Singapore）。范围仅限此节点；生产源码只读。

## 结论

**当前 XgTechTreeCopy 对 label.auto 的名称、通俗解释和效果准确，无需修改，不生成 changes.patch。** 已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md` 已明确修正旧目录文案的 0.3 为真实常量 0.15，并保留神秘状态；本任务不重复提交相同修复。

当前“自动标注 / Automatic labelling”是适当的功能名称；该节点是 Label 类购买项，不是需要装备的网络架构。当前解释只说由已评估模型代答，没有把现实中的自动标注理论写成额外游戏收益。当前效果明确每级、每个合格台、基础速度、该台检查点和实际限制，未承诺固定收入或全答对。

## 可直接采用的中英文文案

以下短文案与当前生产实现一致，建议保留：

| 字段 | 中文 | English |
| --- | --- | --- |
| 正式名称 | 自动标注 | Automatic labelling |
| 通俗解释 | 用评估合格的模型代答标注题。 | Let an evaluated model answer labelling cards automatically. |
| 效果 | 每级每个合格标注台的基础速度增加 0.15 题/秒；该台检查点须达到 60% 准确率。实际速度还受算力、复核队列和暂停状态限制。 | Each level adds a base 0.15 cards/s per eligible desk; that desk needs a checkpoint of at least 60% accuracy. Compute, review capacity and pauses still limit actual speed. |

如说明材料需要交代完整生效顺序，可直接使用以下补充段落；它不是对当前准确文案的强制替换，也不作为缺陷补丁：

**中文：** 发现该科技后，任一已开放标注台有准确率至少 60% 的检查点，且经费足够，即可购买。购买后自动生效，无需装备，也不必再训练一轮；各已开放标注台只有在自己的检查点达到 60% 后才会自动答题。所有台共用一个等级，最多 10 级；每级给每个合格台增加 0.15 题/秒基础速度，满级为 1.5 题/秒。实际速度还取决于算力、接线、待复核队列及暂停状态；按题逐个结算，答对才能取得该题报酬并增加标注样本。仅训练到达分数并不等于已有合格检查点，需要评估并形成记录。

**English:** Once this technology is discovered, you can buy it if at least one open labelling desk has a checkpoint with at least 60% accuracy and you have enough funds. A purchase takes effect automatically, with no equipment step or extra training round. Each open desk answers automatically only after its own checkpoint reaches 60%. All desks share one level, up to level 10. Each level adds 0.15 cards/s of base speed to every eligible desk, reaching 1.5 cards/s at the cap. Actual speed also depends on compute, wiring, the review inbox and pause conditions. Cards settle individually; correct automatic submissions earn that card's pay and add labelled samples. Reaching a score during training alone does not establish a qualifying checkpoint: evaluation must record it.

这些文案只能在已发现状态展示。未发现状态保留现有 `？？？` 与 `也许有更省力的办法……` 的语言回退；不展示真实名称、效果、价格、等级或发现触发条件。

## 原文件证据与生效合同

所有路径以工程根目录为基准，行号对应本次快照；完整 SHA256 见同目录 `source-sha256.json`。

| 来源 | 代码事实及其意义 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:462` | 将 `XgSim.LabelNodes` 加入目录，实际节点沿此路径注册。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:31,41–42` | 节点是 `kind=Label`，目录父线为 `label.raise`，最大 10 级。旧目录 note 仍写 0.3，但当前科技页由 Copy 覆盖；不能反过来拿旧 note 当模拟依据。 |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:671–675` | 门槛 `.6`，上限 `10`，每级基础速度 `.15`，当前等级 L 的下一次价格为 `60 * 1.7^L`。等级是加法叠加，不是逐级乘 1.15；1 级 0.15、2 级 0.30、10 级 1.50 题/秒/合格台。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:153–164,194–211` | 购买要求已发现、未满级、任一已开放台检查点 `acc >= .6`、支付成功；成功直接 `S.autoLevel++`。该专门分支不要求先买 `label.raise`，不可因树上的父线增加虚构门槛。发现本身不赠送等级。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:217–221` | 每台独立检查 `DeskOpen`、`BestAcc >= .6` 和存在该台；速率 `L * .15 * min(1, host.Compute) * WireSpeed("label:" + desk)`。等级全局共享，不把总速度平均分给所有台。算力超过 1 不再从该 `min` 项加速；接线倍率另算。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Wiring.cs:251–255` | 接线倍率取节点 factor；没有接线视图节点时默认为 1。因此“购买后自动生效”不保证断线时仍出题。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:726–735,988` | 合格依据是 `S.best` 的检查点准确率；评估破纪录时更新准确率与保存模型。训练实时分数不直接替代此门槛。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:233–244,255–273` | 自动缓冲与检查点判题；基础路径以游戏判定估算，不宣称为真实模型服务的概率，也不保证每题正确。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:374–418,447–469` | 自动处理需累计计时达到一题；购买不会立即支付一笔收入。无 host、有 Blocker、算力不足、ProjectActive、复核队列满等会阻止处理；预取/已完成判题也限制实际吞吐。每轮每台最多处理预取限制数量，停顿不会无限积累补发。答对用 `PayFor * QualityPayMultiplier` 支付并 `AddLabel`；购买此节点不直接提高准确率或单题报酬。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Captcha.cs:63` | `AutomationHeld` 包括账号冻结、验证码待处理及验证码暂停。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:1502–1541` | 模拟推进时调自动标注；没有要求标注窗口持续打开。无需额外“装备”按钮，不许据此声称操作系统关闭后仍有真实后台运行。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Epiphany.cs:44–49,128–145` | 已发现状态独立于训练分数；桌面可延迟揭示到剧情结束。已有自动标注等级兼容已发现状态。触发细节仅为源码证据，不应添加到神秘节点。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,37,49–56,113,189` | 名称与 Note 在神秘状态首先回退；已发现时输出上面的短文案，效果数值读常量，非硬编码旧 0.3。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:146`；`Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:490–505,758–772` | 名称接入 Copy；神秘时隐藏真实图标、等级、价格；提示框先返回神秘提示，之后才可能调用完整 Note。 |

## 验证状态和待验证边界

- **本次已完成：静态源码核对。** 未启动 Unity、游戏或编译器；未读取玩家存档。已有整合记录的编译及内存检查仅为历史记录，不冒充本任务运行结果。
- **实际运行：未执行。视觉验收：未执行。** 以下交主聊天使用隔离内存/场景状态验证，全部为待验证，不标 PASS。

| 边界 | 应验证的结果 |
| --- | --- |
| 未发现但已有 60% 检查点 | 神秘显示仍保留，购买失败；仅到分数不能跳过发现流程。 |
| 已发现，开放台检查点 0.599999 / 0.600000 | 前者不能作为购买资格，后者可以；不要把幽灵提示使用的容差套到此购买分支。 |
| 台 A 60%，台 B 59%；已有 1 级 | A 可自动工作，B 速率仍为 0；关闭台即便检查点高也不工作。 |
| 已发现且合格，尚未购买 / 刚购买 | 等级 0 仍无自动处理；购买增至 1，下一次有效模拟推进开始积累题计时，不即时发钱。 |
| 支付失败 / 满 10 级 | 失败不升级；满级无第 11 级购买，不应显示虚构升级收益。 |
| 等级 1、2、10；算力 0.5、1、2；接线倍率 1 | 对每个合格台，基础速率分别按等级线性变化，算力 0.5 减半，算力 2 的该项与 1 相同。 |
| 接线倍率 0；复核队列 19→20；冻结/验证码暂停 | 断线速率归零；满队列停下；暂停期间无自动提交。解除后不补发无限历史题量。 |
| 中英文科技页，神秘/刚发现/已购买/满级 | 检查名称、两段说明、0.15 单位、60% 条件以及隐藏价格；最小窗口下补充长段落若采用须另验换行。 |

## 待整合项

仅收录本结果及源码指纹即可；无需合并生产补丁。当前短文案正确，完整补充段落可作为说明材料备选。旧目录 note 的 0.3 属共享目录文案残留，本任务未更改；若其他页面仍直接使用它，应由整合负责人另外决定统一，不能把这个残留误报为当前 Copy 未修好。

本任务未扩展其他词条、未勾选全局原则或整份策划完成、未创建聊天或子 agent、未向其他聊天发消息。

结束前依赖复核：9 个生产源码文件 SHA256 与取证快照一致（见 final-source-check.json）。这仅证明本次取证窗口内依赖未变，不代表运行或视觉验收通过。
