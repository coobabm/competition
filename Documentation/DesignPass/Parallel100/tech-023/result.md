# tech-023：label.hard 难例复核奖励

日期：2026-10-13（Asia/Singapore）。范围仅为本节点的名称、通俗解释和真实效果；未读取新的策划正文。

## 结论

当前 `XgTechTreeCopy` 的中英文名称、解释和效果与实际实现一致，无需生产改动，不提供 changes.patch。已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md:34` 已落实“新题复核答对、排除旧数据重审”，本次独立追踪购买及结算路径后确认，无需重复修补。该节点是奖励机制，不是独立模型架构，也不应改称难例挖掘算法或承诺训练准确率提升。

当前三处文案位于 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:40,116,192`：名称“难例复核奖励 / Hard-case review rewards”；解释为处理需要复核的难题提供额外奖励；效果准确写明新题答对报酬 ×1.5、样本 ×2，旧数据重审除外。下列完整文案可直接用于需要交代时点与叠加规则的说明区，属于可选展开，不是必须替换现有简洁文案的补丁。

## 可直接采用的中英文文案

中文：

1. 正式名称：难例复核奖励
2. 通俗解释：亲自答对待复核的新题，就能多拿报酬和标注样本。
3. 效果：购买后自动生效，无需装备或额外训练。每答对一道待复核的新题，按该题现有人工报酬（含加薪及连击倍率）再乘 1.5，获得 2 条标注样本（原为 1 条）。报酬倍率相乘；样本按题计数，不会使已有样本总量翻倍。旧数据重审、普通人工答题、自动答题及复核答错均不享受此加成。

English:

1. Name: Hard-case review rewards
2. Plain explanation: Answer new cards in the review inbox correctly yourself to earn extra pay and labelled samples.
3. Effect: Active automatically after purchase; no equipment or additional training is required. Each correct human review of a new card pays 1.5 times its existing manual pay, including pay-raise and combo multipliers, and adds 2 labelled samples instead of 1. Pay multipliers multiply together; samples are awarded per card, without doubling your existing total. Old-data rechecks, ordinary manual answers, automatic answers and incorrect reviews do not receive this bonus.

## 生效与数值合同

- 目录由 `XgCatalog.cs:462` 将 `XgSim.LabelNodes` 加入；实际节点定义在 `XgSim.Collaboration.cs:34`：前置 `label.coop`，价格 800 游戏经费，最高等级 1，非装备。
- 仅满足前置、达到可购买状态不产生奖励。`StatusLabelNode`（153–164）检查前置和经费；`BuyLabelNode`（180–193）成功扣款后立即把节点写入 `S.unlocked`。`XgSim.cs:188` 的 `Has` 查询该集合。
- 奖励判定在 `AnswerQueued` 提交结算时发生（`XgSim.Collaboration.cs:419–446`），不是入队时。因此购买之前已在复核队列的新题，只要购买后答对，也可获奖；没有对已结算题补发的代码。
- `hard = Has("label.hard") && !card.recheck` 且处于答对分支。无需纠正模型原判断；即便玩家同意模型的正确猜测，仍享受奖励。队列来源也不限于低置信度，抽检送回的新题同样按这个条件结算。
- 报酬：`ManualPayFor(dataset, level) × ComboMultiplier × 1.5`，单位为游戏经费/题。现有基础人工报酬已经含桌别、题目等级、加薪和 `HandPayScale`（`XgSim.cs:1222–1225`）。`Hit` 先于报酬计算，所以使用本题命中更新后的连击倍率（Collaboration:430–433）。此处不是 +1.5 倍或 +1.5 经费；1.5 倍相当于同一结算状态下增加 50%。最高一级，不可重复购买叠加。
- 样本：`result.samples = hard ? 2 : 1` 并循环调用 `AddLabel`（Collaboration:439–440）；每次增加该题数据集的标注计数 1（`XgSim.cs:1078–1083`）。因此每道正确新题新增 2 条，而非额外加 2 条、不是总数据集翻倍。样本不与加薪/连击倍率相乘。
- `AddLabel` 会重新评估相关已有运行并检查样本与桌级阈值（`XgSim.cs:1085–1088`），但本节点没有直接执行额外训练或保证准确率提升。普通人工答题的结算位于 `XgSim.cs:1411`，自动答题位于 Collaboration:390–402，两者均不应用本节点倍率。

## 神秘性与显示链

`XgTechTreeCopy.Name/Note` 的最前分支（15、51）先处理 `NodeMystery`，不先读取专用文案；未知未来节点的解释/效果缺项时回退原说明（54）。`XgTechTreePage.cs:758–765` 的神秘节点提示提前返回，不显示名称、价格或效果。`XgSim.cs:250` 当前只把未发现的 `label.auto` 标为神秘；`label.hard` 本身没有单独的未发现判定。保持这些既有规则，不提前公开隐藏前置的完整介绍，也不增设或移除发现条件。

## 核对与待验证

本次仅完成源码静态核对；未启动 Unity/游戏，未运行 C# 编译或玩法测试，未进行视觉验收，未访问真实玩家存档。既有整合记录的历史测试结果不冒充本次运行结果。无生产文件写入。

建议主聊天在隔离内存/场景状态下验证以下真实边界（下表为源码推导预期，全部尚未实际运行）：

| 情况 | 预期 |
| --- | --- |
| 只有前置、未购买，答对新复核题 | 正常人工报酬 × 本题连击，1 条样本 |
| 购买后答对新复核题；玩家与模型猜测一致 | 仍有 ×1.5 报酬，2 条样本 |
| 购买前入队、购买后答对 | 按结算时持有状态享受奖励 |
| 已购买，答对旧数据重审题 | 无难例倍率，按重审现有清理逻辑；result.samples 为 1 |
| 已购买，复核答错 | 无报酬和新增样本；不触发难例奖励 |
| 已购买，普通人工题/自动题答对 | 不应用难例倍率或双样本 |
| 加薪、连击同时存在 | 本题更新连击后，现有人工报酬再乘 1.5；样本仍仅 2 条 |
| 同一 cardId 再次提交 | 第一次已从队列移除，第二次无接受结算、无重复奖励 |
| 再次购买已拥有节点 | 最高等级 1，购买被拒绝，无第二层奖励 |

待整合：只需保留当前实现；可选采用上方完整文案，应用前按最新源码复核。主聊天统一完成编译及中英文页面布局、购买前后反馈和神秘节点截图验收。本记录不代表全局文案原则或整份策划完成。

## 源码快照与结束复核

所有依据文件的完整 SHA256 见同目录 `source-hashes.json`；结束前比对结果见 `dependency-recheck.json`。如果整合时任何依赖发生变化，应重核该节点的购买、队列结算及显示分支，不直接沿用旧快照结论。
