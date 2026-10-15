# tech-048｜束搜索 Beam search

结论：名称与通俗解释可保留；仅补充 `XgTechTreeCopy.Effect` 的 beamsearch 分支。当前 `Speed(sim, 1.1)` 数值正确，也已避开目录旧文案“翻译 +10%”的错误限定，但没有说明作用于两条训练线、购买后自动生效、结算时读取、叠乘与一次性持有。交付单行文案补丁，不改任何模拟数值。

本次只读生产源码，未读取新策划正文作为依据；检索时命中了一条既有清单的束搜索条目，没有据此扩展任务。任务记录中 tech-048 为 queued；开始时专属目录不存在，现有 Parallel100 result.md 中未查到 beamsearch 的重复交付。未改全局任务清单。

## 可直接采用的中英文三行文案

中文：
1. 正式名称：束搜索
2. 通俗解释：生成句子时多保留几条候选，继续比较哪条更合适。
3. 真实效果：购买后自动生效，无需装备；视觉与序列的手动、自动训练在结算时，取整前的每轮训练卡数 ×1.1（+10%），与其他训练处理量倍率相乘。本项只生效一次。实际每轮卡数受算力、取整及 4–320 张范围限制；成绩需训练后评估。

English:
1. Name: Beam search
2. Explanation: Keep several candidate sentences while generating and compare how they develop.
3. Effect: Active automatically once purchased; no equip step. For both vision and sequence, manual and automatic training multiply pre-rounding cards per epoch by 1.1 (+10%) at settlement, stacking multiplicatively with other throughput factors. This research applies once. Actual cards per epoch depend on compute, rounding and the 4–320 card limits; evaluate after training to see the score.

解释行是算法的通俗概念，不能作为游戏中实际生成、比较或展示多条候选句的承诺。真实效果行只承诺源码中存在的训练卡数倍率；不是准确率增加 10 个百分点，也不是训练耗时减少 10%，不增加预训练有效规模倍率。

## 原文件证据

以下路径均相对工程根 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`，行号为交付时快照。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:305–309,369–370,450,609`：Research 节点，ID/target 为 beamsearch，parent 为 attention，stage=5，原始 cost=7000，kind=Feel；目录名称就是“束搜索 / Beam search”。不是可装备架构。实际支付额经过 NodeCost，不应直接把 7000 当界面最终售价。
- `Core/XgSim.Progression.cs:132–150`（同上 XingGuang 路径）：NodeCost 使用价格系数；未拥有时 stage>=5 才显示。`Core/XgSim.cs:195–205,252–279`：需节点可见、前置 attention 已拥有、支付成功；成功加入 S.unlocked。仅看见节点或满足前置不产生倍率。持有后 Status 为 Owned，正常购买流程不能重复购买。
- `Core/XgSim.cs:188`：Has 仅检查 S.unlocked。`Core/XgSim.Collection.cs:70–84`：`if (Has("beamsearch")) s *= 1.1;`；FeelSpeed 中各持有项相乘，无装备开关、翻译数据集限制或训练线限制。
- `Core/XgSim.cs:347`：SpeedResearch 将 FeelSpeed 与其他处理量研究倍率相乘。`Core/XgSim.Board.cs:191–200`：CardsPerEpoch 对任意 run 使用 SpeedResearch，手动和自动分别乘已有手动连击/自动倍率，再 Math.Round 后限制至 4–320 张/轮。因此 +10% 是取整前处理量的相对提升，不保证显示卡数每次恰好增加 10%。
- `Core/XgSim.cs:639–669`：训练结算时读取 CardsPerEpoch，再加 steps、训练和评估。`Core/XgSim.EpochAction.cs:12–26,40–50`：BeginEpoch 不锁存该倍率；计时训练完成才调用 TrainEpoch。因此如果训练中购买且本轮成功结算，该轮结算也可受益，不能误写为“必须下一轮才生效”。购买本身虽调用 Evaluate，但不通过 beamsearch 直接增加训练卡数或保证提分。
- `Core/XgSim.Board.cs:42–43,193`：`OfflineSimulation` 先返回固定 12 张/轮，绕过该倍率。这是交接验证必须覆盖的模式例外；不在普通玩家效果文案中加入内部模式字段。
- `Core/XgSim.Finale.cs:75–85`：ScaleItems 无 beamsearch，不追加预训练规模倍率。
- `Core/XgTechTreeCopy.cs:15,45,51,103,166`：名称回退到目录；解释准确，可保留；原效果仅 `Speed(sim, 1.1)`。Note/Name 既有神秘节点前置守卫不变。
- `Core/XgTechTree.cs:127–130`、`Desktop/XgTechTreePage.cs:428–430,758–772`：可见性由 NodeVisible 控制，未发现阶段节点不显示；神秘节点先返回占位说明。补丁不新增提前展示入口，不泄露后续节点。

`Core/` 与 `Desktop/` 上述简写统一位于 `Assets/LingGuangV05/XingGuang/`。所有依赖 SHA256 见 `source-sha256.json`。

## 精确补丁与整合

- 文件：`changes.patch`；只替换 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs` 的 beamsearch 效果分支，包含相邻上下文。
- 目标原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- 名称和解释原样保留；没有改 Speed 公共帮助方法、其他词条、目录或模拟逻辑。
- `XgCatalog` 的旧效果仍写“翻译训练 +10%”，本次科技树已由 XgTechTreeCopy 覆盖。它不能作为真实效果依据；其他显示面的目录旧文案不在本补丁改动范围。
- 整合前复核当前源文件与哈希。若其他任务已改同文件，只整合这个 case，保留其他已合并文案，不覆盖整文件。

## 验证状态与待验证边界

已完成：静态源码调用链核对、中英文逐句核对、目标单行替换及补丁上下文检查。未编译、未运行 Unity/游戏、未进行视觉验收、未读写真实玩家存档。下面是交给主聊天的真实边界验证，不是已运行测试结果：

1. stage<5 且未持有：科技树不展示名称/解释/效果；stage>=5 但无 attention 或支付失败：未获得倍率。
2. 用独立测试状态分别覆盖视觉/序列、手动/自动：未限幅时，购买后下一次成功结算读取 ×1.1；已进行中的轮次在购买后结算也应读取新状态。重复购买应拒绝，不能叠为 1.21。
3. 设置可使取整前结果处于相同整数区间、低于下限及达到上限的状态：卡数可能不变；特别是上限 320 张不能承诺再加 32 张。勿仅比较最终整数比值是否等于 1.1。
4. 与另一项已拥有的处理量倍率共存时验证乘法组合，非百分比相加；保持算力、架构、数据、连击和配置一致，避免把自动配置差异算作 beamsearch 效果。
5. OfflineSimulation=true 时购买前后均固定 12 张；正常模式再验证加成。购买前后没有直接获得训练 steps，评估成绩不要求立刻变化。
6. 主聊天统一编译；中英文 tooltip 在实际窗口宽度下检查换行、溢出、神秘性与已拥有状态。当前补丁未做视觉验收。

未宣称原文全局原则或整份文档完成。本交付仅关闭 beamsearch 单项核对工作；生产合并与统一验证仍待整合负责人处理。

结束前复核：source-sha256.json 的 11 个生产依赖文件 SHA256 均未变化。已实际执行 git apply --check --ignore-space-change，退出码 0，仅确认补丁可应用；未应用补丁，不能据此声称编译或运行通过。
