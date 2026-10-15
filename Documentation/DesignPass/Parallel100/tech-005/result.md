# tech-005｜attention 注意力机制

日期：2026-10-13（Asia/Singapore）。结论：名称、通俗解释正确；通用架构效果说明没有说明 attention 的实际差异以及“只需拥有”和“被选用后”的区别。提交 `changes.patch`，仅新增 attention 专属效果分支；未修改生产文件和模拟数值。

## 可直接采用的中英文文案

名称：**注意力机制 / Attention mechanism**（沿用当前实现）。

解释：**翻每个词都回头看原文。 / Looks back at the source for every word.**（沿用目录原文；只是通俗解释，不宣称执行真实神经注意力算法）。

中文效果：

> 购买后加入序列线可用结构，无需手动装备；系统在轮次之间按数据和显存自动选择，不保证立即采用。采用后，原文线索不再因离句尾更远而衰减，翻译评估上限不再受到 ×0.75 的额外折减；仍须训练后看成绩，不是准确率直接增加25个百分点。第5项能力的参数门槛乘0.7；该门槛优惠和下列预训练规模倍率只需拥有即可生效，不要求装备或先用本结构训练，各计一次。样本和训练条件仍须满足，进入下一阶段需确认。
>
> 预训练有效规模 ×3（与其他道具倍数相乘）。

English effect:

> Buying adds an architecture for the sequence track; no manual equip step is needed. The system selects between epochs based on data and VRAM, so it may not select this architecture immediately. When selected, source clues no longer fade with distance from the sentence end, and the translation score ceiling avoids the extra ×0.75 penalty. Train to see the score; this is not an instant gain of 25 percentage points. The fifth ability's parameter threshold is multiplied by 0.7. This discount and the pre-training scale multiplier below require ownership only, count once each, and require neither equipping nor prior training with this architecture. Sample and training requirements still apply; confirm to enter the next stage.
>
> Effective pre-training scale ×3 (multiplies with other items).

最后一行由现有 `Note` 遍历 `ScaleItems` 自动追加，补丁不重复写入 ×3。第五项能力的折扣还由详情页 `DiscountsOf` 显示，保留该已有明细，不修改其余词条。需要缩短布局时，优先保留生效条件以及“上限不是直接提分”的区别。

## 原文件证据与边界

以下路径均相对工程根目录，行号为交付时版本；完整 SHA256 见 `source-hashes.json`。

| 证据 | 核对结果 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:166`、`:427` | attention 为序列架构，2015；父节点 seq2seq，stage 4。名称和原解释可保留。 |
| `XgCatalog.cs:348`、`:353`；`XgSim.Progression.cs:132`；`XgSim.cs:195`、`:252` | 阶段4起可见，须拥有前置且资金足够；当前实际价格为 ceil(15000 × 0.7 × 0.25)=¥2625，不能误用架构记录的 cost=400。价格继续由界面读取，补丁不硬编码。购买后加入拥有集合，触发自动配置和评估。 |
| `XgSim.AutoModel.cs:165`、`:217`、`:249` | 自动选择已拥有且适配训练线的架构，按数据评估、参数规模、显存等比较；epochActive 时不切换。架构变化通常从头训练，拥有迁移学习则保留超出随机水平进度的60%；旧版本首次自动接管另有保留路径。购买不保证立即采用或立即涨分。 |
| `XgSim.Board.cs:70`、`:103`；`XgBoard.cs:301` | attention 映射到 Attention 接线。相对 EncoderDecoder，源序列元素活性改为1，不再按距句尾距离作瓶颈衰减；目标序列仍按 SequenceDecay 衰减。并非所有词永不遗忘，也不是 Transformer 的整句并行。 |
| `XgSim.AutoModel.cs:356`；`XgSim.Progression.cs:255` | 当前 CeilingAcc 确实调用 ProgressionAccuracy；translate 上非 attention/transformer 的 acc 额外乘0.75。attention 免除此项，不等于当前分数 ×4/3 或 +25个百分点；数据、参数、下限/上限截断仍有效。 |
| `XgSim.Abilities.cs:49`、`:67`、`:144`、`:168`、`:203` | 第5项能力参数门槛按拥有状态乘0.7，即290K→203K个参数，K=1000；本节点不降低样本门槛。优惠循环相乘，单个节点仅计一次。第5项能力仍要满足样本、已训练参数和本阶段训练条件，并由玩家确认进入下一阶段。 |
| `XgSim.Finale.cs:75`、`:96`、`:111`；`XgTechTreeCopy.cs:57` | attention 拥有后预训练有效规模因子为3，无需当前 run.arch=attention，也不要求先用它训练。与其他已拥有道具倍率相乘；这是无量纲有效规模倍率，不是参数数量、样本量或训练速度变成3倍。已有追加行正确，保留。 |
| `XgTechTree.cs:129`；`XgSim.Progression.cs:140`；`XgTechTreeCopy.cs:15`、`:51` | 阶段未到且未拥有的 attention 不进入可见树。补丁不改变可见性、神秘节点回退或图鉴回退，不列出未发现的后继节点名称与奖励。 |

表内未写完整目录的 Core 文件统一位于 `Assets/LingGuangV05/XingGuang/Core/`。

## 其他数值核对（不把模型系数直接宣传成奖励）

- `XgCatalog.Arch("attention")` 的 maxDepth=6：结构容许6层；BatchNorm 再加6，但自动模型还受已购买层数上限和显存限制（`XgSim.cs:404`、`AutoShape`），不是购买本节点立刻得到6层。
- paramFactor=16：`ParamsK=depth × width² ×16/1000`，单位千参数；与同形状 Seq2Seq 相同，不是参数翻16倍的购买奖励。显存计算还包括300 MB基础开销、参数与激活开销（`XgSim.cs:351`、`:363`）。
- bias=0.4、span=999、seq2seq=true 用于 `CeilingParts` 的误差和结构限制。span 是该公式与数据需求比较的值，不能宣传为实际上下文长度999词；真实概念板的源线索行为见上表。
- speed=0.9 乘入常规 `CardsPerEpoch` 的卡片处理量公式，与优化器、研究、手动连击/自动轮系数等相乘，之后取整并限制最少4张和最大上限。相对同形状 Seq2Seq 的0.85，原始系数比为约1.0588，不保证每轮张数或实际训练耗时改善5.88%。`OfflineSimulation` 则直接返回12张，跳过这些系数。证据：`XgSim.Board.cs:190`。
- 注意力仍属于循环/串行接线；`parallel` 数据集仍受 SerialCardFactor=0.25，不能写成已消除串行瓶颈或全显卡利用率。`GpuUtilization` 对 attention 返回0.31，但不把它写成可见硬件实测百分比。
- instability=0.05 被 SafeLr 与风险公式使用（`XgSim.cs:564`、`:579`），不是训练失败率5%；不据此宣称固定成功率或真实模型算法改进。

## 补丁与整合合同

- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
- 目标源 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `changes.patch` 在通用 Arch 效果返回之前新增 attention 特判；名称、解释、倍率追加、模拟数据均不变。
- 只应用本节点分支；若其他并行任务改了相邻通用架构分支，按语义整合，避免整体文件覆盖。最终统一编译及真实界面验收由整合负责人完成。

## 验证状态

- 已执行静态阅读与调用链核对，以及 `git apply --check`；修正补丁换行格式后，无忽略空白选项的检查成功。没有实际应用补丁。
- 未编译、未运行模型或 Unity、未做视觉验收，不能记录为玩法 PASS。既有任务001的历史检查不等于本补丁已运行。
- 待统一运行边界：未到stage4节点隐藏；有前置而资金不足不能购买；拥有但自动选择其他架构时仍只有全局拥有加成；训练轮进行中不切结构；translate上限差异不直接转成当前分数；第5项参数门槛203K而样本不足仍不能晋级；重复购买不重复叠加；预训练切换其他架构后×3仍在；中英文说明长文本及已有折扣行不裁切。
- 已核对已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`，本目录最初无交付物。首次检索范围过宽，工具意外返回了其他逐条清单的少量匹配行；未继续打开策划正文，未以那些匹配行作为本次文案依据。
- 结束前复核依赖哈希的结果见 `verification.md`。
