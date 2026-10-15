# tech-052：多头注意力 / Multi-head attention

日期：2026-10-13（Asia/Singapore）。范围仅为 `multihead`；生产源码只读。

## 结论

建议整合 `changes.patch`：只拆出 XgTechTreeCopy.Effect 中 multihead 的分支，补足实际作用和生效条件。名称和通俗解释保留原文。现有“随 Transformer 获得、不另收费、不重复购买加成”正确，但仅说明取得方式，没有说明代码已实现的线索变化，不足以完整回答玩家“有什么效果”。不修改模拟数值，也不改同一旧分支中 layernorm/residual 的字符串。

已核对已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`；其随附标记结论正确。本交付补充概念板的真实实现，不重复该任务已有整合或把它报告的测试当作本聊天执行结果。

## 可直接采用的中英文文案

中文名称：**多头注意力**

中文解释：几组注意力同时寻找不同线索，再合并结果。

中文效果：随 Transformer 免费获得，无需单独购买或装备。模型实际使用 Transformer 时，单句训练保留词本身的线索；已启用位置编码时，还能单独学习句首词的线索。训练后可形成相应概念，不保证立即提分；没有独立数值加成，也不重复叠加。

English name: **Multi-head attention**

Explanation: Several attention heads look for different clues and combine their results.

Effect: Granted free with Transformer; no separate purchase or equipment step. When a model uses Transformer, single-sentence training keeps clues for the words themselves; with positional encoding enabled, it can also learn a separate clue for the opening word. Training can form these concepts, with no guaranteed immediate score gain. There is no separate numerical bonus to stack.

“几组注意力”是通俗原理说明；当前游戏是概念线索的简化模拟，不应进一步扩写成已经实现了语法头、指代头、语气头或真实并行注意力张量计算。

## 源码证据与生效合同

下列路径均相对工程根目录；Core 为 `Assets/LingGuangV05/XingGuang/Core/`。

| 项目 | 真实实现与证据 |
| --- | --- |
| 正式名称与种类 | Core/XgCatalog.cs:440：id=multihead，Label，parent=transformer，cost=0，stage=6，tree=research；现有中英文正式名称正确。不是独立架构。 |
| 无单独购买 | Core/XgSim.Progression.cs:154–164 的 ProgressionBlocker 阻止单买；199–207 的 ApplyProgressionNode 在购买 Transformer 后 Grant(multihead)。Core/XgSim.cs:257 检查可买状态。 |
| 重复取得 | Core/XgSim.Progression.cs:106 的 Grant 先检查 unlocked.Contains，不重复写入。Core/XgTechTree.cs:93–109 按普通节点 0/1 展示；不是按次数升级的技能。 |
| 解锁路径 | Core/XgSim.Progression.cs:587–596 的 CompleteTransformer 同样授予 multihead，并把两个 run.arch 设为 transformer；因此文案使用“随 Transformer 获得”，不宣称只有付费购买一种来源。 |
| 实际激活条件 | Core/XgSim.Board.cs:94–116：multiHead 只由 run.arch == transformer 决定，并不读取 Has(multihead)。只有标记、仍用其他结构，不会开启该能力；不存在 multihead 装备开关。 |
| 购买后的时机 | Core/XgSim.cs:273–276 先授予、再 AutoConfigureAll、再 Evaluate。Core/XgSim.AutoModel.cs:166–183 自动选择适用的已拥有架构，不保证购买后所有模型都用 Transformer；219–240 活跃训练轮暂不重配。真正读到新 multiHead 的时间为模型切到 Transformer 后下一次生成 Knobs。 |
| 位置编码条件 | Core/XgSim.Board.cs:84、106：Has(position) 且 run.position。Core/XgSim.AutoModel.cs:199–205 自动配置时将已有位置编码启用。普通购买 Transformer 的 ApplyProgressionNode 不赠 position，不能省略条件。 |
| 单句的真实线索变化 | Core/XgBoard.cs:263–320，尤其 293–299：单个序列且 multiHead=true 时保留 f.name；position=true 且 x=0 时额外加入 ^f.name。没有为每个词增加完整位置标记，也没有固定“头数”配置。 |
| 双句边界 | Core/XgBoard.cs:293 优先处理 lengths.Count>1，按源/目标句区分词（目标加 →）；不会进入 multiHead 分支。不能把句首额外线索宣传为所有翻译双句卡均有。 |
| 训练与成绩 | Core/XgSim.Board.cs:211–232 用当前 Knobs 调用 Board.Train。Core/XgSim.cs:651–669 在训练后分别调用 StepAccuracy、Evaluate、ObservePhenomena；656 注释明确概念板学习与数值成绩推进不同。此节点不能承诺固定准确率增加或立即提分。 |
| 迁移的相关边界 | Core/XgSim.cs:420–432：multiHead 使带位置编码的 AnyToAny 不落入位置绑定的整板重置条件。仅在已有迁移学习并换结构时才涉及基础概念迁移，不是 multihead 赠送迁移学习；组合概念和不适用的句首概念仍受过滤。无需把另一词条的百分比加成写成本节点效果。 |
| 单位与叠加 | 取得 multihead 额外费用 ¥0；无独立训练速度、显存、准确率百分点或预训练规模倍率。Core/XgSim.Finale.cs:75–84 的 ScaleItems 无 multihead。其布尔开关不会累计成多个加成；已有 Transformer 自身收益不能再算一次。 |

## 未发现内容的保留

本补丁只修改 Effect 的一个已知节点分支。Core/XgTechTreeCopy.cs:15、51 的 NodeMystery / atlas 回退不变；Core/XgTechTree.cs:127–130 使用 NodeVisible，Desktop/XgTechTreePage.cs:428 用它控制可见性。multihead 未拥有时通常到 stage 6 才可见，提前随 Transformer 取得时因 Has 而可见（Core/XgSim.Progression.cs:140–148）。不添加新提示去揭示尚未到达的节点，不扩写后续剧情或策划正文。

## 补丁与依赖复核

- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
- 原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `changes.patch` 包含目标、原文件 SHA256、前后真实上下文；只交付，未应用到生产文件。
- `source-hashes.json` 记录本轮依赖的 SHA256。整合前重新核对；其他聊天可能修改同一共享 switch，若 hash 不同，应检查局部上下文再合并，不能覆盖整份源文件。

## 验证状态与待验证项

已完成静态源码检查及补丁可应用性检查（`git apply --check --ignore-space-change --ignore-whitespace` 返回 0），没有修改生产源码。没有编译、运行独立模拟、启动 Unity/游戏、视觉验收或访问真实玩家存档；下列为待主聊天运行的边界检查，不能记为 PASS：

1. 未拥有 Transformer 时不能单买零价 multihead；随 Transformer 获得一次后等级为 1，重复授予不叠加。
2. 仅有 multihead 标记而 run.arch 非 Transformer，不启用 multiHead；Transformer 实际启用时才使用相应线索。购买发生在活跃训练轮中时核对下一轮自动重配。
3. 构造单句 `[春, 风, 春]`：启用 Transformer 和位置编码时存在词线索“春/风”及句首“^春”，不会给后一个“春”增加句首线索；关闭位置编码后无“^春”。这是语义边界，不是期待固定分数提升。
4. 单句使用无循环注意力和位置编码时，与 Transformer 的位置无关词线索作对照；双句卡必须走双句分支，不能误期待额外“^词”。无位置的特征不应凭空产生句首标记。
5. 训练和观察概念板结果时区分既有概念是否已学会，不把线索可用等同于立即学会；成绩面板不应出现本节点独立固定加分。
6. 中英文科技页显示完整效果文本，最小窗口和缩放下不裁切；未达可见阶段仍不展示本节点；其他神秘/未发现节点保持原显示。

待整合：负责人审阅并合入单节点文案补丁。统一编译、内存边界检查和真实场景截图均由主聊天执行。本交付不代表整份策划或全局文案原则完成。

结束前依赖复核：上述 10 份源文件的 SHA256 与记录一致，未检测到本轮核对后的变化。
补丁交付格式已统一为 LF；最终严格检查（git apply --check，不忽略空白）返回 0。
