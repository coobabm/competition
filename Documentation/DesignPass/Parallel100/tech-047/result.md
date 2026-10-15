# tech-047：wordvec（词向量）核对交付

结论：名称与通俗解释可保留；效果倍率正确，但当前通用 `Speed(sim, 1.08)` 没有交代适用训练线、购买生效、叠加与离线例外。本次仅提供 wordvec 的一行文案补丁，不修改生产文件、模拟数值或其他词条。

## 可直接采用的中英文文案

| 字段 | 中文 | English |
| --- | --- | --- |
| 正式名称 | 词向量 | Word vectors |
| 通俗解释 | 用一串数字表示词语，让相似含义可以互相比较。 | Represent words with lists of numbers so similar meanings can be compared. |
| 真实效果 | 购买后自动生效，无需装备。视觉与序列的手动、自动训练均在每轮结算时将取整前处理量 ×1.08（+8%），与其他处理量倍率相乘。实际卡数受算力、取整及每轮 4–320 张限制；离线模拟固定 12 张/轮，不享受此卡数加成。不缩短每轮时长或直接加分，成绩需训练后评估。 | Active automatically once purchased; no equipment step. At epoch settlement, manual and automatic training on both vision and sequence tracks multiply pre-rounding throughput by 1.08 (+8%), stacking multiplicatively with other throughput factors. Actual cards depend on compute, rounding and the 4–320 cards/epoch limits; offline simulation stays at 12 cards/epoch without this card bonus. Epoch duration is unchanged and no score is granted directly; train and evaluate to see results. |

解释描述词向量的概念；不宣称游戏已执行 Word2Vec 训练、语义检索或“国王−男人+女人”类比。本节点是 Research / Feel，并非可装备架构。保留源码正式名称，不自动改称特定 Word2Vec 算法。

## 源码依据与生效合同

以下路径均相对于工程根目录；最终 SHA256 见 `source-hashes.json`。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306` 的 ResearchNode 将研究名称/说明带入 Research 节点；`:449` 注册 `R("wordvec", "rnn", 3500, 4)`，阶段 4、前置 rnn；`:608` 定义 Feel 类型、词向量 / Word vectors。原始 Catalog 的“文字训练 +8%”范围与执行代码不一致；科技树已有 Copy 覆盖，本任务仅修 Copy，不扩大到其他展示入口。
- `XgCatalog.cs:348,356` 与 `XgSim.Progression.cs:131–148`：阶段 4 价格系数 0.7，实际 NodeCost 再乘 0.25 向上取整；当前购买价为 ceil(round(3500×0.7)×0.25)=¥613，不应将原始 cost=3500 当界面售价。未拥有且阶段不足 4 时不可见；到阶段 4 只是可见，仍需 rnn 和足够经费，单纯看到/满足前置没有加成。
- `XgSim.cs:188,192–206,252–279`：Has 查 S.unlocked；成功购买后加入该集合。重复购买由 Owned 状态挡住，非可叠等级、无需装备。购买会触发通用自动配置和 Evaluate，但 wordvec 自身没有准确率加项。
- `XgSim.Collection.cs:70–83`：Has("wordvec") 时 FeelSpeed 乘 1.08，无训练线限定。与其他 FeelSpeed 项相乘；例如另有 earlystop 时该部分为 1.05×1.08=1.134，而非 1.13。
- `XgSim.cs:347`：FeelSpeed 继续与 batchnorm/cudnn 的倍率相乘。`:554–557` 的 StepsPerSecond 同样带 SpeedResearch；本节点不是每轮时间减 8%。
- `XgSim.Board.cs:42–43,191–199`：正常训练卡数由共享 CardsPerEpoch 算式产生，乘 SpeedResearch 后 Math.Round，再限在 4–320 张/轮；hand 只选择连击或自动倍率，所以两条训练线的手动、自动均适用。OfflineSimulation 提前返回固定 12，跳过该乘数。
- `XgSim.EpochAction.cs:10,13–25,29–50`：开轮只保存时长和状态，轮次完成后调用 TrainEpoch；`XgSim.cs:639–675` 在 TrainEpoch 内读取 CardsPerEpoch，训练后 Evaluate，按评估周期 Assess。因此购买当下拥有加成；下次实际结算使用当时的拥有状态，包括购买前已开始、购买后才结算的轮次。不是必须先训练一次才能激活。
- `XgTechTreeCopy.cs:13–17,45–61,102,165`：名称默认使用节点名称，现有通俗解释正确；原效果仅 `return Speed(sim, 1.08);`，正是补丁的唯一替换行。`:57` 对 ScaleItems 追加说明，但 `XgSim.Finale.cs:75–84` 无 wordvec，不应声称它提高预训练有效规模。
- `XgTechTree.cs:127–130,146` 与 `Desktop/XgTechTreePage.cs:428,747–764,772`：页面按 NodeVisible 显示；谜底/图鉴有先行保护分支。补丁在 Effect 的 wordvec 分支内，不改变这些规则，不提前揭示未发现节点。

## 交付与整合方式

- `changes.patch`：只替换 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs` 中 wordvec 的效果一行，包含邻接上下文。
- 补丁基准 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `source-hashes.json`：本次核对的 10 个依赖文件快照。结束前已再次比较，状态见下方。
- 开始时专属目录尚不存在；已有 Parallel100 交付记录中未检索到 wordvec 记录，未重复已有该词条交付。
- 整合时复核源文件 hash。如其他词条并行更新使整文件 hash 改变，人工核对 wordvec 当前行和上下文后只合入该替换，不覆盖整文件。

## 验证状态与待验证项

静态检查：已沿节点注册→购买持有→倍率→每轮卡数→结算→科技树显示完整核对；`git apply --check --ignore-space-change --ignore-whitespace` 实际执行成功，未应用补丁。

实际运行：未启动 Unity、未编译或运行模拟；不报告运行 PASS。未读取或更改玩家存档。

视觉验收：未执行。整合负责人需检查中英文长文案在 tooltip 中的换行、可读性和溢出。

建议统一验证时使用独立临时状态覆盖下列真实边界（本聊天未运行）：

1. 未买与已买、视觉与序列、手动与自动，在不饱和的相同参数下对比。保持连击/自动配置相同，防止把其他变化算到本项；不要求整数卡数恰好增加 8%。
2. 将取整前基数分别置于 4 张下限附近、半整数附近和 320 张上限附近。验证出现倍率增加而卡数不变的合法情况；Math.Round 默认中点取偶，文案不得承诺每轮固定多 8%。
3. 与另一个 FeelSpeed 项共同持有时验证乘法；再次购买 wordvec 应被拒绝且不会重复扣款或叠层。
4. OfflineSimulation=true 时两种拥有状态均为 12 张/轮；不能据此判定加成失效。
5. 已开轮未结算时购买，结算采用当前持有状态；本节点不改变已存轮次时长。只解锁前置而未购买不加成。
6. 阶段不足 4 且未拥有时节点隐藏；可见未买与买后分别检查名称、解释和效果。保留其他谜底入口的先行保护。

仅本词条交付完成；不表示整份策划、科技树全体或全局原则完成。
结束前复核：10/10 个依赖文件 SHA256 与快照一致；未发现核对期间的依赖变化。
