# tech-057 · LeNet 文案核对

日期：2026-10-13（Asia/Singapore）。仅核对 `lenet`，未读取新策划正文，未修改生产源码、存档、全局清单或其他任务目录；未启动 Unity/游戏。

## 结论与交付

正式名称 **LeNet-5** 正确，保留。现有解释“小窗口扫全图，挪了位置也认得”有局部共享规则支持，但应明确是**学会的线索**可复用，不保证购买即正确识别。架构通用效果“加入可用结构，系统按数据和显存选择合适的模型；训练后再看评估成绩”方向正确，但未说明轮次间选型、拥有即生效的门槛折扣、同组不叠加，以及切换结构的进度损失。

`changes.patch` 仅在 `XgTechTreeCopy.Explanation`、`Effect` 的通用分支前新增 `Arch && node.id == "lenet"` 条件。名称、目录、模拟数值、其他节点及显隐规则不变。补丁尚未应用。

已搜索已有任务记录 `进度.md`、`队列.md` 和 Parallel100 的 result.md 中精确 LeNet/tech-057 记录，未发现重复交付。通用倍率已由页面追加，故不把倍率误报为完全缺失；本修正补充其条件、单位与叠加语义。

## 可直接采用的三项中英文文案

### 中文

**正式名称：** LeNet-5

**外行解释：** 用同一套小窗口寻找相邻笔画；学会的局部线索可以在图中别的位置复用。

**真实效果：** 拥有即使「看图说词」的参数门槛从 60K 降至 42K（K＝千参数），有效样本门槛从 60,000 降至 42,000，均为 ×0.7；同组架构道具只计一次，无需装备。加入视觉候选结构，系统在训练轮次之间按数据、已解锁规模和显存选用，不保证立即切换。切换结构时，无迁移加成则从头练，有该加成则保留高于随机水平进度的 60%。当前模型须训练至少一轮、评估达 C 级才计入参数进度；两项进度达标且本阶段训练过后，仍需确认进入下一阶段。

### English

**Formal name:** LeNet-5

**Plain explanation:** Use the same small window to find neighbouring strokes; learned local clues can be reused elsewhere in the picture.

**Actual effect:** Ownership immediately lowers the Picture to word threshold from 60K to 42K parameters (K = thousand) and from 60,000 to 42,000 effective samples, both ×0.7. Items in this architecture group count once; no equipment step is needed. Adds a vision candidate, selected between epochs according to data, unlocked size and VRAM; switching is not guaranteed. An architecture change restarts learning without a transfer bonus, or retains 60% of progress above chance with that bonus. The current model must train at least one epoch and earn grade C or better in an assessment before its parameters count. Once both progress thresholds are met and this stage has had training, you must still confirm the next stage.

## 原文件证据与边界

以下路径均相对 `Assets/LingGuangV05/XingGuang/`；行号对应 source-hashes.json 中的版本。

|核对项|源码依据与实际语义|
|---|---|
|身份与价格|`Core/XgCatalog.cs:147–148,300–303,348,353–362,400`：lenet→LeNet-5，视觉专用，1998 年；节点第 2 阶段、前置 mlp，基础节点价格 3000。阶段系数 1.6 后为 4800；`Core/XgSim.Progression.cs:131–137` 再乘 0.25 向上取整，当前实际购买价 **¥1200**。架构对象 cost=0 不是科技节点免费。价格由现有页面动态显示，不在补丁硬编码。|
|购买条件|`Core/XgSim.cs:194–205,252–279`：需可见、前置已拥有、经费充足；一次解锁，不重复购买。购买写入 unlocked，执行自动配置与 Evaluate，但不因此训练一轮或发放参数进度。旧档授权也可得到拥有状态，故正文使用“拥有即”。|
|选型与生效|`Core/XgSim.Progression.cs:20` 限定架构适用训练线；`Core/XgSim.AutoModel.cs:136–183,219–247` 在拥有且适用的候选中，按可容纳形状的预期等级、参数量、上限和 bias 依次比较。`epochActive` 时不重配；购买后空闲轮次可立即重配，训练中则等轮次间。`Core/XgSim.cs:1507–1510` 持续在轮次间自动配置。无需手工装备；买到不保证被选上。|
|真实局部共享|`Core/XgSim.Board.cs:62–73,94–115`：仅当前选用架构为 lenet 时，Knobs 使用 LocalShared。`Core/XgBoard.cs:263–288` 对视觉线索 ID 不带坐标，允许学到的同名线索复用；`:374–384` 组合局部元素需同 seq、横纵距离各不超过 1。它是游戏的概念棋盘规则，不是实际运行完整 LeNet-5 图像卷积网络，也不是任意平移必定正确。|
|架构参数与单位|`Core/XgCatalog.cs:147`：maxDepth=5、paramFactor=3、speed=1、bias=1。`Core/XgSim.cs:351–368`：参数数量 = **3 × 层数 × 宽度²**，ParamsK 除以 1000；显存估算 **300 + ParamsK×0.016 + max(1,层数)×宽度×0.12 MB**。这些是当前被选用的结构系数，不随买入其他架构累计。|
|层数边界|`Core/XgSim.cs:282–295,404–409` 与 `AutoModel.cs:140`：实际层数受已解锁层数上限和结构限制共同约束；本结构基础限制 5 层，拥有 batchnorm 时结构限制加 6，但仍受层数解锁和显存约束。因此不能写“购买后立即得到 5 层”。为保留未发现节点，不在玩家文案主动点名后续技术。|
|速度与容量|`Core/XgSim.Board.cs:191–199`：speed=1 乘入每轮处理量，与算力、优化器、研究速度、手动/自动倍率和形状共同决定卡数，取整且有上下限；不是吞吐提升 100%。`Core/XgSim.cs:479–494`：bias=1 作用于容量误差公式的一部分，不是 +1 准确率；不可许诺固定提分。|
|切换与训练|`Core/XgSim.AutoModel.cs:250–281`：换架构将数值进度回到随机基线；有 transfer 则保留高于基线进度的 0.6，概念携带另受规则限制。旧存档初次自动迁移另有免损例外（:228–235），此补丁描述通常游戏操作。`Core/XgSim.cs:639–677`：真正训练发生在训练流程中，随后更新与评估成绩；成绩可能下降，不承诺一轮达标。|
|拥有折扣与叠加|`Core/XgSim.Abilities.cs:49–64,144–170`：能力 3 的原门槛 60K 参数、60000 有效样本；拥有 lenet **或** alexnet 即乘 0.7→42K/42000。二者同属一条 itemsAny 记录，拥有两者仍只计一次，不是 0.49；不同折扣记录的因子才相乘，当前能力 3 仅此记录。仅报告点名同组伙伴以供审计，玩家文案不泄露其名称。|
|进度不是赠送|`Core/XgSim.Abilities.cs:124–139,183–211`：D 是各数据集扣噪声并按 dataWeight 加权的有效样本；P 是至少训练一轮且评估 C 级以上的最大模型参数量。折扣不直接赠送参数或样本，不要求用 LeNet 训练才降低门槛。已达标可能直接满足进阶条件，但仍需本阶段有训练且玩家确认。|
|预训练倍率|`Core/XgSim.Finale.cs:75–84`：ScaleItems 无 lenet，不给它编造预训练规模倍率。|
|接入与神秘性|`Core/XgTechTree.cs:127–130,147–149` 与 `Desktop/XgTechTreePage.cs:428,758–794` 使用现有可见性、Name/Note 和动态折扣。`Core/XgSim.Progression.cs:140–148` 在第 2 阶段之前且未拥有时隐藏节点。`XgTechTreeCopy.Name/Note` 的 mystery/atlas 提前返回保留；新增文案未新增展示入口，也未枚举后继节点名称或奖励。|

## 补丁基线与整合

目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

目标 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

补丁内含目标、SHA256 和原始上下文。全量依赖快照在 `source-hashes.json`。整合前重算哈希；其他任务也可能向这两个方法添加分支，若哈希改变，应仅按 lenet 条件移植两处分支，不能覆盖整文件。现有页面在文案后追加能力倍率行，可与本说明共存；不改公共页面以避免影响其他条目。

## 验证状态与待验证项

- **已执行的静态检查：** 源码调用链、单位与不叠加条件核对；补丁只读适用性检查成功（`git apply --check` 返回 0）。首次检查发现交付补丁换行格式不匹配，已将补丁改为 LF 后复查成功。没有应用补丁。
- **未执行：** C# 编译、Unity/游戏运行、实际场景截图与中英文视觉验收。不能标记运行或视觉 PASS。
- **待整合负责人验证的真实边界：** ① 已拥有与尚未购买 LeNet 时阈值为 42K/42000 与 60K/60000；同组两架构不再降到 29.4K/29400。② 训练进行中买入不切换本轮结构，空闲轮次且成为最优候选时才采用；低显存或更优已拥有结构时不承诺选中。③ shapeRounds=0 即使评估等级合格也不计入 P；训练一轮但低于 C 也不计入。④ 仅门槛满而本阶段未训练，或未确认下一阶段，不推进能力。⑤ 未到阶段且未拥有时不展示节点；长中文及英文说明无裁切。
- 结束前依赖复核结果见 `dependency-recheck.md`。本交付仅完成 lenet 的静态文案核对，不代表原文全局原则或整份策划完成。
