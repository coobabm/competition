# tech-028 — weights（权重）文案核对

日期：2026-10-13（Asia/Singapore）。范围仅限 weights。

## 结论

当前 XgTechTreeCopy 的正式名称、通俗解释和效果与生产代码一致，无需修改，不提交 changes.patch。既有“训练处理量 ×1.1；每轮卡数仍受算力、取整和上限影响，成绩需训练后评估”正确描述了倍率，未承诺准确率立即增加或每轮时间减少。以下详版供整合负责人直接采用；补充生效时点与叠加规则属于说明完善，不是已确认错误的修复。

未改生产源码、模拟数值或全局清单；未启动 Unity/游戏，未访问玩家存档。只写本专属目录。检查已有进度记录与 Parallel100 result.md，未发现已交付的 weights 专项结果，本目录此前不存在。

## 可直接采用的中文三项文案

正式名称：权重

通俗解释：给每条线索分配不同的分量，重要的线索更能影响答案。

真实效果：购买后自动生效，无需装备。两条训练线每轮的基础训练处理量 ×1.1（增加 10%），与其他训练倍率相乘；每个科技只计一次。实际处理张数会取整，并限制在每轮 4–320 张，所以不保证每轮都多 10%。从购买后下一次训练结算开始采用该倍率，正在进行但尚未结算的一轮也适用。它不直接缩短每轮时间或增加准确率；成绩变化需训练后评估。

## Ready-to-use English copy

Formal name: Weights

Plain explanation: Give each clue a weight so important clues influence the answer more.

Actual effect: Applies automatically when purchased; no equipment step is needed. Multiplies base training throughput per epoch by 1.1 (+10%) on both training tracks. It multiplies with other training bonuses, with each technology counted once. Actual card counts are rounded and limited to 4–320 cards per epoch, so a full 10% increase is not guaranteed every round. The multiplier is read at the next training settlement after purchase, including a round already in progress. It does not directly shorten an epoch or add accuracy; train and evaluate to see score changes.

## 生效、单位与边界

- 节点类型为 Research，不是可装备架构。第一阶段可见，前置为 perceptron。显示出来、达到前置或有钱可买都不等于已拥有；BuyNode 成功扣款并写入 S.unlocked 后，Has("weights") 才为真。按本次代码，目录基础成本 15，第一阶段价格系数 1，再经 StoryResearchPrice=0.25 向上取整，实付 ¥4；价格由现有 UI 动态显示，成品文案不硬编码。
- ×1.1 是无量纲处理量因子，不是 +10 个百分点准确率，不是 +0.1 张，不是每秒固定多 10 张，也不是权重参数数量增加 10%。玩家实际结算单位是“张/轮”。
- ProgressionSpeed 的 run 参数未用于条件判断，weights 对视觉、序列及各架构均使用同一拥有状态；手动和自动训练都经过 CardsPerEpoch。倍率与已有训练因子相乘，之后才统一取整/限幅；不是把各增幅相加。重复购买因 Owned 状态被拒绝，不存在 weights 多级堆叠。
- 仅从公式分析：若其他因素给出未取整卡数 20，加入 weights 后为 22；若为 4.1，加入后为 4.51，实际是 4→5；若为 400，两边都封顶为 320。这些是数学边界示例，不是实际运行测试结果。
- 训练仍须满足样本、供电、算力等既有条件；买到 weights 不会自动启动一轮训练。购买会触发通用 Evaluate，但没有 weights 专属准确率加成。
- 在线轮次在 TickEpochs 完成时调用 TrainEpoch，CardsPerEpoch 在此时读取 Has；购买研究节点不受 AnyEpochActive 禁买规则约束，因此可作用于尚未结算的当前轮次。
- OfflineSimulation 分支先返回固定 12 张/轮，跳过 weights 等处理量因子；它不受 ×1.1 加成。此为内部离线模拟边界，不放进常规玩家 tooltip。统一验证时需单独覆盖。
- weights 不在 ScaleItems 中，不额外授予“预训练有效规模”道具倍率。不要把真实 AI 理论中的可学习权重、真实神经网络参数或本地大模型训练承诺追加为此购买收益。

## 原文件依据

下列路径均相对 Assets/LingGuangV05/XingGuang/，行号以本次核对版本为准。

| 文件与位置 | 代码依据 |
| --- | --- |
| Core/XgCatalog.cs:306–309、380、614 | ResearchNode 转换研究目录；weights 前置 perceptron、stage=1、cost=15；名称“权重 / Weights”。ManualPay 是目录枚举值，实际收益以模拟引用为准。 |
| Core/XgSim.Progression.cs:131–148 | NodeCost 按 0.25 向上取整；NodeVisible 使用阶段/拥有状态。 |
| Core/XgSim.cs:188、194–207、252–280 | Has 查 unlocked；Owned 阻止重复购买；购买成功写入 unlocked，随后自动配置和评估；weights 没有装备状态。 |
| Core/XgSim.Progression.cs:253–266、268–269 | 准确率加成表无 weights；ProgressionSpeed 中 (Has("weights") ? 1.1 : 1) 与其余因子相乘。 |
| Core/XgSim.Board.cs:42–43、191–200 | 上限 320、离线固定 12；普通训练处理量公式使用 ProgressionSpeed；Math.Round 后限定最少 4、最多 320 张。 |
| Core/XgSim.cs:633–679 | TrainEpoch 使用 CardsPerEpoch，训练棋盘、累计 steps、评估；手动/自动共享路径。 |
| Core/XgSim.cs:554–558 | StepsPerSecond 数值公式也乘 ProgressionSpeed，不能由此把常规界面单位改写成固定“张/秒”。 |
| Core/XgSim.EpochAction.cs:10、13–25、38–50 | 轮次时长不读 weights；轮次完成才调用 TrainEpoch 结算。 |
| Core/XgSim.Finale.cs:75–85 | ScaleItems 中没有 weights。 |
| Core/XgTechTreeCopy.cs:13–16、46、49–59、75、119–120、160 | 名称回退目录；解释准确；效果返回 Speed(sim,1.1)，提示算力、取整、上限和评估；神秘节点/图鉴提前回退。 |
| Core/XgTechTree.cs:127–130、146；Desktop/XgTechTreePage.cs:428、758–772 | 可见性继续由 NodeVisible 控制；未发现分支保留；名称与 tooltip 接入现有文案。 |

神秘性：weights 本身第一阶段即可见，不应新增隐藏机制；Name/Note 的神秘节点保护与页面可见性保持原样。成品文案不列出未来节点名称、奖励或发现条件。

## 验证状态与待办

- 静态核对：已完成实际引用、购买路径、结算时机、叠加公式、取整上下界、离线旁路及界面文案回退检查。
- 实际运行：未运行，未编译。本结果不宣称运行 PASS。
- 视觉验收：未执行，由主聊天统一确认中英文 tooltip 的换行、长度及隐藏节点展示。
- 待整合：无必须修复项，无生产补丁。负责人可保留当前短文案，或选择上方完整三项文案；仅移植 weights，不更改共享 Speed 函数以免影响其他词条。
- 待统一验证：用临时测试状态覆盖未拥有/拥有、重复购买、两条训练线、手动/自动、轮次中途购买、低卡数取整、封顶及 OfflineSimulation。不要使用真实玩家存档。准确率提升不应作为必然断言。

## 依赖 SHA256

| 文件（同上前缀） | SHA256 |
| --- | --- |
| Core/XgCatalog.cs | 1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7 |
| Core/XgTechTreeCopy.cs | 308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8 |
| Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Core/XgSim.EpochAction.cs | 73CE990C4A6E454CFD65C5D5F47C06B748CE93D27B9E651DEC3F4A4857F6A747 |
| Core/XgSim.Finale.cs | E565169876C92B4E434BDE1421B995F785DB8E160747268F32F30B95193CC11B |
| Core/XgTechTree.cs | C2795A20598FA9B22F91A4678AD7946F97AC0D03E518A62A3108A80350D0414D |
| Desktop/XgTechTreePage.cs | F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523 |

结束前复核：以上 9 个依赖文件的 SHA256 与记录全部一致，未发现核对期间的依赖变化。此项仅证明源码版本一致，不代表运行或视觉验收通过。
