# tech-044：cuDNN 文案核对

日期：2026-10-13。范围仅限当前科技词条 `cudnn`，只读生产源码；未改模拟数值、生产文件、全局清单或玩家存档，未启动 Unity/游戏。

## 结论与交付

现有通俗解释和两个倍率正确，保留解释原文。需要补足正式名称、购买生效时点、乘法叠加及实际效果边界。现有 `Name` 回退为目录中的「cuDNN 加速 / cuDNN kernels」，未展开专业名称；`Effect` 仅给出处理量 ×1.3 和能耗 ×0.7，缺少购买后自动生效及叠加说明。

交付 `changes.patch`：仅在 `XgTechTreeCopy.Name` 新增 cuDNN 分支，并替换 `Effect` 的 cuDNN 分支。不修改公共 `Speed`、其他节点、可见性或模拟行为。补丁包含目标路径、当前目标 SHA256 和上下文。应用前必须复核哈希；若整合窗口已经修改此文件，应按 cuDNN 分支手工合并，不覆盖整文件。

目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

完整依赖指纹见 `source-sha256.json`。开始检查时本任务目录不存在；在已有 Parallel100 的 result.md 中未找到 cuDNN 专项结论，在现有进度记录中也未找到该节点专项交付，因此没有重复沿用他人的补丁。

## 可直接采用的中英文文案

### 中文

**正式名称**：cuDNN（CUDA 深度神经网络库）

**通俗解释**：使用显卡厂商优化过的计算方法，减少重复计算的开销。

**真实效果**：购买后自动生效，无需装备。两条训练线此后结算的手动和自动训练，每轮处理卡数的计算值 ×1.3，与其他训练倍率相乘；实际卡数仍受算力、取整和每轮 4–320 张范围影响，不直接缩短每轮时间；成绩需训练后评估。此后结算的训练用电量（kWh）×0.7，再按阶梯电价计费；谷电另按费用倍率叠加，总账单不保证减少 30%。

### English

**Formal name**: cuDNN (CUDA Deep Neural Network library)

**Plain explanation**: Use GPU-optimized routines to reduce the cost of repeated calculations.

**Actual effect**: Active automatically after purchase; no equipment step. For subsequent manual and automatic epoch settlements on both tracks, the calculated cards per epoch are multiplied by 1.3, stacking multiplicatively with other training factors. Actual card counts still depend on compute, rounding and the 4–320 card range; epoch duration is unchanged. Evaluate after training to see the score. Training electricity use (kWh) billed after purchase ×0.7, then billed at tiered rates. Off-peak power applies its cost multiplier separately; the total bill is not guaranteed to fall by 30%.

补丁从 `MaxCardsPerEpoch` 和 `CuDnnEnergyFactor` 获取对应显示数字；不写死这两个可引用的常量。

## 源码依据和边界

路径均相对工程根目录，行号对应上述依赖指纹。

| 证据 | 对本词条的结论 |
| --- | --- |
| `Core/XgCatalog.cs:306–309,396,649–650`（前缀 `Assets/LingGuangV05/XingGuang/`） | `Research` 节点，父节点 `mlp`，阶段 2，节点基础价 600；它不是可装备的网络架构。旧 Research 表的 cost=250 不应当作当前售价。 |
| `Core/XgSim.Progression.cs:131–148` | 当前节点售价是 ceil(600×0.25)=¥150；阶段达到 2 才可见，已拥有节点继续可见。售价交给现有 UI，不在文案重复硬编码。 |
| `Core/XgSim.cs:188,193–206,252–278` | 仅看见或满足前置不生效；成功付款后将 id 加入 unlocked。已拥有时不能重复购买。没有装备、安装或预先训练步骤。 |
| `Core/XgSim.cs:347,554–558` | `SpeedResearch=(batchnorm?1.2:1)×(cudnn?1.3:1)×FeelSpeed`，并参与 StepsPerSecond 的乘法计算。此项与优化器等其他有效因子相乘，不是加 30 个百分点。 |
| `Core/XgSim.Board.cs:42–43,191–199` | 正常手动/自动训练的原始卡数含 SpeedResearch；四舍五入后限制在 4–320 张，不能保证最终整数卡数恰为原来的 1.3 倍。内部 OfflineSimulation 分支直接返回 12，绕过该加成；不要将上述正常玩法文案扩展为离线模拟承诺。 |
| `Core/XgSim.EpochAction.cs:10,13–25,29–50`；`Core/XgSim.cs:655–668` | 每轮时长独立于 cuDNN；实时训练完成后才调用 TrainEpoch 计算卡数及扣费。因此训练途中购买，也会影响随后结算的该轮，不应限定为“下次开始的一轮”。成绩依训练/评估产生，不是购买即直接加分。 |
| `Core/XgSim.Consumables.cs:106–109,133–145` | 所有权即时使 TrainEnergyFactor=0.7；训练结算使用它，手动/自动都覆盖。谷电以独立费用因子 0.5 处理，并按普通训练轮数消耗。 |
| `Desktop/XingGuangHost.cs:61–74`；`Assets/LingGuangV05/Core/ChapterOneSim.Bills.cs:179–205` | 正式宿主支持缩放计费；先将 kWh 乘以 0.7，按当前月份阶梯积分计价，再乘费用因子。关闭电费系统时无实际扣费；不支持 IXgTrainingBill 的替代宿主仅走 Train(seconds)，不应据此宣称所有测试宿主都省电。 |
| `Core/XgSim.Progression.cs:558–564`；`Core/XgSim.Finale.cs:177–198` | 项目与预训练的训练计费也走 ChargeTraining，获得用电量减免；其进度未使用 SpeedResearch，不获得 1.3 进度倍率。预训练额外机房费用独立扣除，不受此能耗因子影响。这些后续机制仅供整合核对，不加入玩家可见文案以免提前透露未发现内容。 |
| `Core/XgTechTreeCopy.cs:15,51,98,182`；`Core/XgTechTree.cs:127–130,144–146`；`Desktop/XgTechTreePage.cs:758–772` | 保留已有神秘节点早返回和条目可见性过滤；正常名称/提示接入 Copy。补丁不提前解锁、显示节点或列出未来科技。 |

同样原始计算条件下，与 BatchNorm 的相关倍率合成为 1.2×1.3=1.56；cuDNN 不能重复购买叠层。若用电计费未跨阶梯且其余条件相同，cuDNN 加谷电对该轮电费的合成比例为 0.7×0.5=0.35；跨阶梯应比较实际分段计价结果，不能笼统承诺 65% 总账单折扣。

## 已做检查与待验证

**静态检查已完成**：沿目录、购买、普通训练结算、能耗计费及科技页调用链逐项核对；补丁仅涉及 cuDNN 名称和效果。`git apply --check --ignore-space-change` 返回 0，仅检查补丁上下文，没有应用补丁。依赖结束复核结果见 `dependency-recheck.txt`。

**实际运行未执行**：未编译 C#，未执行模拟/Unity/游戏。以上源码推导不是运行 PASS。

**视觉验收未执行**：中英文字长、换行、提示框滚动及不同窗口尺寸由主聊天验收。

待整合负责人合并补丁，并统一验证以下真实边界：

1. 阶段未到、前置未买、经费不足、成功购买、重复购买；只有成功获得所有权后启用因子，保留原有隐藏/神秘显示。
2. 两条训练线分别覆盖手动和自动；在训练进行中购买，核查结算时卡数和能耗生效，不要求该轮计时加快。
3. 固定其余状态，用未取整卡数 10→13、2→2.6（均落在最小 4 张）、300→390（实际 300→320）检验正常区间及两端钳制；另覆盖四舍五入边界。预期仅为静态推导，未实跑。
4. 固定其余状态，验证 BatchNorm 的乘法叠加及已拥有 cuDNN 不重复叠层；内部 OfflineSimulation 仍固定返回 12。
5. 比较相同训练秒数的 kWh 1→0.7、同一电价阶梯下谷电合成费用比例 0.35、跨阶梯费用和月份累计 kWh；关闭电费时不扣费，烧卡因子不被 cuDNN 改写。
6. 项目/预训练仅验证训练计费省电，不应增加其进度速度或改变独立机房费用。

本交付只完成 cuDNN 的静态文案核对及待合并补丁，不代表该策划文档或全局原则已完成。
