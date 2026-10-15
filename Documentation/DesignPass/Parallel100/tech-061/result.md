# tech-061 · ResNet 文案核对

结论：需要仅针对 resnet 的文案补充，交付 changes.patch，未改生产源码或模拟数值。审查范围仅为当前已授权三项：正式名称、外行解释、实际效果。未阅读新的策划正文；已检索现有任务记录，未发现 tech-061 已有交付或针对 resnet 的独立修正。

## 可直接采用的中英文文案

补丁内的中英文字符串是完整交付，三个插入点均仅匹配 `node.id == "resnet"`。

- 正式名称：残差网络（ResNet） / Residual network (ResNet)
- 通俗解释：给信息留一条绕过中间层的近道，让原来的线索能直接传下去。 / Give information a shortcut around intermediate layers so the original clues can pass straight through.
- 实际效果：采用补丁 `Effect` 的中英文内容；保留原 `Note` 自动追加的“预训练有效规模 ×2（与其他道具倍数相乘）。” / “Effective pre-training scale ×2 (multiplies with other items).” 不再另写一个 ×2，避免重复。

## 已确认的问题与修正理由

当前 `Name` 没有 resnet 专属分支，回退到目录名称“ResNet”，没有完整专业名称。`Explanation` 的 Arch 通用分支拿到目录短句“层间开近道，152 层也练得动”，会把历史例子混入游戏可实现层数；当前代码没有购买即配置为 152 层的保证。`Effect` 只有通用“加入可用结构”，遗漏两条训练线的跨层直连。现有预训练 ×2 及相乘说明正确，保留。

补丁不改变可见性、图鉴或神秘性判断；`Name`/`Note` 的 null、NodeMystery、IsAtlas 提前返回仍优先执行。ResNet 本身不是 NodeMystery 节点：其未到阶段的隐藏由 NodeVisible→XgTechTree.Visible→页面负责。本补丁不列出任何尚未发现的其他节点。

## 生效条件、数值与叠加核对

1. **解锁与购买**：ResNet 为第 4 阶段视觉 Arch，前置 lenet。仅到达阶段/满足前置，不等于拥有。基础节点价格 15000，经 StagePrice[4]=0.7、NodeCost 的 0.25 后为一次性 ¥2625（不是 XgArch.cost=600）；价格已有界面单独显示，补丁不硬编码。Status 阻止重复购买。
2. **装备与时间**：购买成功先加入 S.unlocked，然后 AutoConfigureAll；非活动轮次且 host 非空时立即尝试配置，活动轮次在下一次 AutoConfigure 时配置。系统只在适配训练线且已拥有的架构中择一，排序为可达评级、参数量、成绩上限、较低 bias；买到不保证选中。没有 ResNet 手动装备步骤。AutoTechniques 将两条线的 run.skip 都设为 SkipOwned，与最终是否选中 ResNet 无关。
3. **跨层直连**：Has(resnet) 或另一已实现提供者使 SkipOwned 为真，是布尔“或”，不会叠成两份。Knobs 对实际 ResNet 架构也直接启用 skip；其他架构由 run.skip 决定。默认 UseBoard=true：RelayKeep=1、RelayNoise=0，即层间传话保留 100%，新增层数造成的改写为 0；这不是准确率 100%。非阶跃激活的 G=1（每层反向传递比例 100%），阶跃激活仍 G=0。TearAt 中深度惩罚项从 0.03×(depth−1) 变为其 0.3 倍，不能换算成“整体稳定性提高 70%”或“绝不发散”。大脑规则训练与页面成绩曲线是两套路径，不能把这些比例当成绩加成。
4. **实际选中 ResNet 的架构参数**：maxDepth=999 为结构限制哨兵；AutoShape 仍取已解锁层数上限与结构上限较小者，并受显存限制，不会自动给 999 或 152 层。paramFactor=9 进入 `ParamsK=depth×width²×9/1000`，单位为千参数，不是固定增加 9 千参数。bias=0.35 是误差容量项系数，不是准确率 +35%；speed=1 是架构处理量乘数，不提供独立提速。只取实际架构的一组系数，不与其他已购架构系数相乘。
5. **训练和成绩**：换架构通过 ChangeArchitecture 重置当前进度，无迁移学习保留比例 0，有迁移学习为 0.6；旧存档首次自动接管有保留例外。新形状 shapeRounds 重置，需要实际训练才能计入相应受训练形状指标。Epoch 调用 TrainBoard 和 StepAccuracy，Evaluate 不因购买直接奖励固定分数；不承诺提分、满分或瞬间扩容。
6. **预训练**：ScaleMultiplier 遍历 ScaleItems，Has(resnet) 即乘 2，无需选中或训练 ResNet；同项只乘一次，与其他已拥有道具的倍率相乘，和跨层直连布尔效果相互独立。EffectiveScale=ScaleBars×ScaleMultiplier，是无量纲有效规模，不是参数数、样本数、训练速度或得分翻倍。预训练实际运行仍有第 6 阶段及设备条件；拥有乘数不会启动预训练，也不会凭空提供训练过的参数和数据。

## 原文件证据

以下路径均相对工程根目录；快照 SHA256 见 source-sha256.json，补丁头另列目标文件 SHA256。

| 文件 | 已核对位置与依据 |
|---|---|
| Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs | 155–156 ResNet 定义；300–303 ArchNode；348–362 StagePrice/节点构建；423 ResNet 阶段、前置和基础价格 |
| Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs | 13–46 Name 回退；49–60 Note 的神秘性保护及 ScaleItems 追加；63 起 Explanation 的 Arch 分支；128 起 Effect 的通用 Arch 分支 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | 194–207 Status；246–250 NodeMystery；252–278 BuyNode；282 起 DepthCap；355 ParamsK；404–409 MaxDepth；518 起 Evaluate；641–674 训练入口 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs | 140 AutoShape 上限；166–184 BestAutoModel；199–212 AutoTechniques；219–246 配置时机；250 起 ChangeArchitecture；333–368 成绩上限公式 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | 41 UseBoard 默认；83 SkipOwned；94–117 Knobs；211 起 TrainBoard |
| Assets/LingGuangV05/XingGuang/Core/XgBoard.cs | 128–182 TearAt、G、RelayKeep、RelayNoise；465 传话使用点；550 反向信号使用点 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs | 75–84 ScaleItems；97–123 ScaleMultiplier、ScaleBars、EffectiveScale；147 起预训练条件 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs | 19 ArchitectureFits；130 起 NodeCost/NodeVisible；199 起购买架构回调 |
| Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs | 127–130 Visible；143 起 Name 调用文案层 |
| Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs | 428 节点可见性；748–765 图鉴和神秘节点提前返回；772 Note 接入 |

## 验证状态与待整合

- 已完成：静态代码调用链核对。补丁只交付，未应用到 Assets。
- 待整合：整合负责人仅合入三个 resnet 文案分支；若其他词条已先修改同一文件，应按函数及上下文合并，不覆盖整文件。
- 未执行：C# 编译、Unity/游戏运行、实际场景截图、视觉验收。没有运行结果可标为 PASS。
- 待实际验证边界：阶段 3 隐藏/阶段 4 有前置可买；购买资金不足和重复购买；训练中购买延后配置、空闲购买立即配置；ResNet 未被自动选中时两条线仍启用 skip；已存在 skip 来源时不重复叠加；阶跃激活仍 G=0；浅层/深层与显存不足；买后不训练时倍率已计入、成绩不凭空提高；倍率原本为 0 的有效规模仍为 0；中英文说明无溢出。由主聊天执行统一验证。
- 补丁适用性静态检查及结束前依赖复核结果见 verification.md。
