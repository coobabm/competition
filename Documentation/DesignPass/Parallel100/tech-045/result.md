# tech-045 — transfer（迁移学习）文案核对

结论：名称“迁移学习 / Transfer learning”和当前通俗解释可保留；仅建议替换 XgTechTreeCopy.Effect 的 transfer 分支。现有 60%、70%→90% 数字正确，但未定义“进度”，遗漏生效时机、同结构尺寸变化及检查点加载范围。本交付不改任何生产文件或模拟数值。

## 可直接采用的中英文文案

**名称**：迁移学习 / Transfer learning

**通俗解释（原样保留）**：换一种网络结构时，带走已经学过、还能用的基础知识。

**Plain explanation (unchanged)**: Carry useful basic knowledge over when changing network architecture.

**效果**：购买解锁后两条训练线自动适用，无需装备。换结构时，保留各数据集准确率高于随机猜测基线部分的 60%；同结构调整深度或宽度、或加载尺寸不同的同结构检查点时，保留比例由 70% → 90%。在变更时计算，连续变更逐次乘算，不是直接加准确率，也不叠加两档比例。购买本身不保证提分，之后仍需训练。基础概念能否迁移取决于新结构是否支持与位置无关的线索。

**Effect**: Once purchased, applies automatically to both training tracks; no equipment step. Changing architecture keeps 60% of each dataset's accuracy above its chance baseline. Changing depth or width within the same architecture, or loading a same-architecture checkpoint of a different size, raises retention from 70% → 90%. Applied at each change; successive changes multiply the remaining progress. These rates do not add accuracy or stack with one another. Buying alone does not guarantee a higher score; further training is still needed. Carrying basic concepts depends on whether the new wiring supports position-independent clues.

## 原文件证据与范围

路径均相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:653–654` 定义正式中英文名称与 ResearchKind.Transfer；`:369–370,435` 将实际节点放在 research 分支，阶段 5，前置 lrschedule。节点基础价格为 12000，经 StagePrice[5]=1 与 StoryResearchPrice=.25 得到当前购买价 ¥3000；Research 定义中的 cost=400 不是当前科技节点售价，不放入本补丁以免与价格 UI 重复。
- `Core/XgSim.cs:188,194–205,252–279`（以下 Core、Desktop 均位于 Assets/LingGuangV05/XingGuang/）：Has 只检查 unlocked；成功购买加入 unlocked，然后自动配置并评估；Owned 状态阻止重复购买。仅显现/满足前置不等于已拥有；transfer 无装备或额外开关条件。全局 Has 对两条训练线都适用。
- `Core/XgSim.AutoModel.cs:74,219–241,251–281`：架构变化保留 .6（未拥有为 0）；同结构深度或宽度变化使用 .9（未拥有为 .7）。不只限于变大。两者为互斥路径，单次架构和尺寸一起变动不会再乘 .9。训练轮次进行中 AutoConfigure 返回；旧训练版本首次自动接管有不扣进度的迁移例外。
- `KeepProgress` 对当前模型的每项数据集记录计算 `chance + max(0, accuracy - chance) * keep`；百分比是相对“高于随机基线的准确率增量”的保留比例，不是准确率百分点、训练轮数、卡数或参数数量。连续真正变更逐次相乘；未发生变化不扣进度。
- `Core/XgSim.cs:855–900`：LoadModel 必须与当前训练线架构相同；尺寸不同时从检查点准确率按 .9/.7 恢复，尺寸相同不扣。重复加载同一检查点每次读取该检查点的 acc，不是把当前准确率无限连乘；本规则与自动连续变更须区分。
- `Core/XgSim.cs:517–528`：评估还会做基线/上限钳制和近亲繁殖处理，因此不能承诺屏幕最终成绩必等于单一保留公式。购买不直接授予固定准确率，进一步改善仍通过训练。
- `Core/XgSim.Board.cs:41`：UseBoard 默认 true。`Core/XgSim.cs:420–432`：迁移还涉及实际概念图；对位置绑定的新接线重置该区域，否则调用 CarryOver。`Core/XgBoard.cs:744–767`：普通高层组合删除，重命名后同名一层概念取绝对值更大的权重、较大的强度，不累加权重；seed/pinned 不参与此过程。不能把“保留 60%”解释成概念数量或概念权重乘 .6。rename 返回 null 的一层概念目前是 continue，不能声称所有不兼容底层概念都会删除；本任务只保留有证据的概括，不修改该实现。
- `Core/XgTechTreeCopy.cs:15,45,51,100,184`：名称回落目录正式名称，解释准确，待修仅 Effect 的一条 case；Note 原有 mystery/atlas 保护保留。
- `Core/XgSim.Progression.cs:140–148`、`Core/XgTechTree.cs:127–130`、`Desktop/XgTechTreePage.cs:428`：未拥有的 transfer 受阶段可见性控制。`Desktop/XgTechTreePage.cs:750–764` 在未发现图鉴/神秘节点时提前返回。补丁不调整这些入口，也不将其他未发现节点的名称写进文案。
- 已查任务记录：Parallel100 下 transfer 的相关检索命中 tech-003、tech-006，均仅讨论其他架构的共用切换语义；未发现已有 tech-045 交付，不重复修改其他节点。未读取新增策划正文。

## 精确补丁与源文件 SHA256

`changes.patch` 只改目标 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs` 的 transfer 效果行，含相邻 case 上下文。目标当前 SHA256：

`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

70/90 仍来自 GrowKeep/GrowKeepTransfer，未硬编码替代这两个常量。整合时若源 SHA 不同，先确认 transfer 分支及依赖语义是否变化，再按单个 case 合入，避免覆盖其他聊天修改。

依赖快照（SHA256）：

| 文件（均位于 Assets/LingGuangV05/XingGuang） | SHA256 |
|---|---|
| Core/XgCatalog.cs | 1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7 |
| Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Core/XgSim.AutoModel.cs | A1EC4AD656114E54BBC5A43902A3E5E3B5324E7444DAADD3EB75F8DD322822D7 |
| Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Core/XgBoard.cs | DAF725A9B24E7E8C7DD303C8A32ECD4B87D3B1AE301E0AB867AD503B2F75E963 |
| Core/XgTechTree.cs | C2795A20598FA9B22F91A4678AD7946F97AC0D03E518A62A3108A80350D0414D |
| Desktop/XgTechTreePage.cs | F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523 |

## 验证状态与待整合项

- 已完成：静态阅读购买、配置、准确率保留、概念迁移和科技页展示调用链；只在本专属目录生成交付。
- 未执行：C# 编译、模拟运行、Unity/游戏运行、真实存档检查、视觉验收。不得将下列预期写作运行 PASS。
- 待整合：负责人合入 changes.patch 的单条效果 case；名称和解释无需修改。
- 待验证的真实边界：随机基线 50%、原准确率 80% 时换结构保留结果为 68%，同结构变化为 77%，不是 48% 或 72%；基线处仍为 50%；连续两次架构变化在无其他调整的条件下为 60.8%。这些是公式推导值，尚未执行模拟。
- 待验证：购买后模型不变时不凭空提分；同次架构/尺寸变化不双重扣减；同结构尺寸相同的检查点恢复不扣减，尺寸不同按检查点 acc 计算；训练中重配置推迟，旧版首次接管例外保留；绑定位置/非绑定位置的概念图差异。
- 待视觉验收：中英文完整效果文字可读、不截断；阶段不足时 transfer 不显示；未发现图鉴与神秘节点仍保持原遮蔽。长文案可能需要在现有详情滚动区内确认排版，本任务不改 UI。

结束前复核：9 个列示生产源文件 SHA256 与核对快照一致，未发现依赖变化。补丁只读适用性检查已执行；不等同于编译或运行验证。
