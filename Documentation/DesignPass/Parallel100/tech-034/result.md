# tech-034：学习率预热（warmup）

## 结论

只核查 `warmup`。专业名称及通俗解释可保留；真实效果文案需补充自动启用时点、学习率精确起止值，以及已实现但漏写的「大幅上涨后回落」风险项 ×0.5。预训练有效规模 ×1.5 已由 `Note()` 统一追加，不应重复追加。

交付 `changes.patch` 仅替换 `XgTechTreeCopy.Effect()` 的 warmup 分支一行，不改任何模拟数值。生产文件未修改。已有 Parallel100 结果未发现 warmup 专项交付，本专属目录创建前不存在。

目标文件：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

补丁基线 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

补丁含前后三行上下文。其余依赖的完整路径及 SHA256 见 `source-hashes.json`。行号均指核查时源码，不作为合并时唯一定位依据。

## 可直接采用的中英文文案

正式名称（现有，保留）：
- 中文：学习率预热
- English: Learning-rate warm-up

通俗解释（现有，保留）：
- 中文：刚换设置时先小步学习，再慢慢恢复正常步幅。
- English: After a settings change, start with small learning steps and gradually return to the normal size.

真实效果（补丁替换；其中 60 继续读取 `XgSim.WarmupCards`）：
- 中文：获得后无需装备，系统在轮次之间自动启用。首次训练或更换数据集、训练配置后，前 60 张训练卡将学习率从设定值的 1/60 逐张线性升至设定值。拥有后，单轮退步风险中「刚大幅上涨」这一项 ×0.5；与梯度裁剪的 ×0.7 相乘为 ×0.35，不是总退步概率减半，也不保证不退步。
- English: Once acquired, no equipment step is needed; the system enables it automatically between rounds. On the first training run or after a dataset or training-configuration change, the first 60 training cards raise the learning rate from 1/60 of its configured value linearly up to that value. While owned, the recent-large-gain component of the per-round drop risk is multiplied by 0.5; with gradient clipping's 0.7, it becomes 0.35. This does not halve the total drop chance or guarantee improvement.

由现有 `Note()` 自动追加，保留：
- 中文：预训练有效规模 ×1.5（与其他道具倍数相乘）。
- English: Effective pre-training scale ×1.5 (multiplies with other items).

## 解锁、购买、装备、训练和叠加合同

1. 这是 Research 技巧节点，不是可切换架构。`XgCatalog.cs:306–309,443,627` 将 warmup 作为阶段 6、前置 transformer 的 Research 节点；名称来源正确，不必另加名称分支。
2. 正常流程由 `XgSim.Stages.cs:64,79` 从阶段 5 升至 6 时调用 `CompleteTransformer()`，后者在 `XgSim.Progression.cs:587–596` 直接 `Grant("warmup")`。该路径免费获得；不能写成「必须另付 20000」或「买 Transformer 就自动送预热」。单独购买 Transformer 的 `ApplyProgressionNode` 分支只赠 multihead/layernorm/residual。
3. 目录仍保留购买兜底：若阶段 6 已有 Transformer 却未拥有 warmup，满足可见性、前置和资金检查后可购买。20000 是目录基础价；`StagePrice[6]=1`，`NodeCost()` 再乘 `StoryResearchPrice=.25` 并向上取整，因此该路径实际为 ¥5000。正常剧情赠送后 `Status()` 先返回 Owned，不会重复购买。显示价格继续用现有界面，文案不硬编码。
4. 仅“可见/可买”不生效。获得时 `Has("warmup")` 为真；购买通过 `BuyNode()` 添加所有权后调用 `AutoConfigureAll()`，赠送则在之后的自动配置更新/下一轮开始时接入。`AutoKey` 包含解锁数量，获得可触发配置刷新。
5. 不需玩家装备。`AutoConfigure()` 对 `epochActive` 为真的轮次延后处理；空闲轮次经 `AutoTechniques()` 设置 `run.warmup = WarmupOwned`。板上预热实际要求 `Has("warmup") && run.warmup`；不是只点亮节点便回溯改写已经完成的训练卡。
6. 学习率预热按每个 XgRun 自己的训练卡数计，不是秒、轮数或拥有样本总数。第 n 张卡（n≥1）使用 `配置学习率 × min(1,n/60)`；第 1 张为 1/60，第 60 张为 1，第 61 张仍为 1。同一配置跨轮继续累计，暂停/恢复本身不重置。全新的 run 和重新加载后的非序列化计数从头开始。
7. 重置键是 `dataset + "|" + TraceSettings(run)`。该配置串包含架构、深度、宽度、激活、配置学习率、特征工程/裁剪/直连/位置/预热/BN 的有效标志；并非任何设置变化都会重置（例如 Dropout 不在串中）。串使用本地化文本，因此语言切换也可能导致下一次训练重置。此为实现边界说明，不扩大本任务去修模拟。
8. 风险项由所有权即时参与下一次 `DropRisk()` 计算，不依赖预热开关、前 60 张限制或装备动作。只有「最近大幅涨分」分项乘 0.5；与梯度裁剪相乘后为原分项的 0.35。该分项再与其他风险相加，最后受 BatchNorm 的总项乘数和概率上限限制。因此总下降概率不保证减少 50%，没有最近大幅涨分时这项原本就是 0。
9. 预训练有效规模由所有权即时乘 1.5，不等重新训练，也不要求预热开关开启。它与其他规模道具逐项相乘，只计一次；是无量纲倍率，相当于同一其他条件下该规模量增加 50%，不是成绩 +50%、学习率 ×1.5、速度 ×1.5 或保证预训练通关。规模基数仍取已训练参数和数据比值；既有预训练进度不会因购买直接跳增。
10. 可见效果分两条路径：概念板按卡实际学习使用渐增学习率；训练分数经 `StepAccuracy()` 使用退步概率。不能把板上预热描述为分数必涨或完全防崩；实际分数变化要在后续训练/评估中观察。

## 源码依据

| 源文件（均位于 Assets/LingGuangV05/XingGuang） | 行号与依据 |
|---|---|
| Core/XgTechTreeCopy.cs | 15、45：名称尊重 mystery 并回退目录；51：mystery 提前返回；57–59：自动追加本节点规模因子；86：现有通俗解释；173：原效果只说明 60 张渐增。 |
| Core/XgCatalog.cs | 306–309：ResearchNode 使用研究名称/说明；348、353–356：阶段价格；443：阶段 6/transformer/20000；627：现有专业名称。 |
| Core/XgSim.Progression.cs | 106：Grant 去重；131–148：价格倍率/可见性；154–164：前置阶段门槛；206：买 Transformer 的附赠名单；587–596：剧情授予 warmup。 |
| Core/XgSim.Stages.cs | 64、79：升至阶段 6 时完成 Transformer、赠送预热。 |
| Core/XgSim.cs | 194–205：拥有/阶段/前置/资金；252–276：购买、自动配置与评估；651–669：训练卡、板学习、分数结算；1508：轮次间自动配置。 |
| Core/XgSim.AutoModel.cs | 187–205：获得后配置刷新且自动打开预热；219–241：活动轮次延后；454–468：风险公式和组合顺序；492–525：实际退步掷骰与分数变化。 |
| Core/XgSim.Board.cs | 26–28：计数和配置键不保存；85、107：所有权与有效开关；147：60 张；211–232：比较配置键、逐卡累积、调用 Board.Train。 |
| Core/XgSim.Trace.cs | 129–141：具体重置配置串；使用本地化文字。 |
| Core/XgBoard.cs | 553、560–561、585：真实学习率用于失稳判定、权重和连接更新，裁剪另有限制。 |
| Core/XgSim.Finale.cs | 82：warmup 规模 ×1.5；95–114：拥有时相乘、有效规模；128–130：预训练上限/就绪仍受规模门槛。 |
| Core/XgSim.EpochAction.cs | 13–25：开始一轮先自动配置，不因开始本身授予完成轮次或样本。 |
| Core/XgTechTree.cs / Desktop/XgTechTreePage.cs | 树 127–130 与页 428：服从 NodeVisible；页 758、772：mystery 先处理，再展示说明。 |

## 神秘性

不改变 `NodeVisible`、`NodeMystery`、阶段、前置、节点连线或名称回退。warmup 未拥有且阶段 <6 时不可见；自身当前不属于 `NodeMystery` 的问号节点。不要为预告本条文案把隐藏节点提前显示。补丁只针对已进入说明路径的 warmup 效果，不列举其他未发现规模道具。

## 静态核查与待验证

- 静态源码核查已完成：节点类型/价格路径/剧情赠送/自动配置/按卡公式/风险分项/规模叠加/显示门控。
- 已实际执行补丁可应用性检查 `git apply --check`，退出码 0；只检查，不应用。
- 未编译、未启动 Unity 或游戏、未进行模拟运行、未做视觉验收；不将这些项目标为 PASS。没有读取真实玩家存档。
- 补丁仅为字符串替换，不为此新增镜像实现测试。统一编译与场景检查由整合负责人进行。

建议整合后的真实边界验证（下列全部尚未运行）：
1. 独立临时状态下比较未拥有、刚获得且轮次空闲、刚获得且正在训练：空闲配置自动开启，活动轮次不在中途改配置；未拥有不能启用。
2. 同一 run 捕获第 1/59/60/61 张卡学习率，确认 1/60、59/60、1、1；把 60 张拆成多轮继续计数；切换数据集/架构后从 1/60 重启；保持配置不变暂停后继续。
3. 风险公式边界：最近上涨换算为 30 分时该项为 0；90 分时原分项 .05、预热 .025、叠裁剪 .0175；150 分及以上原分项封顶 .1、预热 .05、叠裁剪 .035。仅比较这个分项；另设总风险已触顶的状态，确认不能用总风险减半断言。负增长也不生成该风险项。
4. 隔离其他状态后获得 warmup，确认 ScaleMultiplier/EffectiveScale 同比 ×1.5（EffectiveScale 基数为 0 时仍为 0），重复 Grant 不叠第二次，关闭 run.warmup 不取消所有权规模倍数/风险项。
5. 正常阶段 5→6 剧情后已拥有且无付费；人为构造阶段 6 缺少 warmup 状态验证 ¥5000 兜底购买、前置不足拒绝。只用临时测试状态，不用真实存档。
6. 中英文科技说明各检查一次：文字不截断、1/60 与 ×0.5/×0.35 无歧义、×1.5 只追加一次；阶段 5 未获得时不暴露条目。

## 待整合

由整合负责人核对源文件 SHA256 与补丁上下文后，只应用 warmup 分支；若生产文件已有并行修改，手动定位该分支并重新核对依赖，不覆盖整个文件。交付不代表《技能树词条策划》全局完成。

结束前复核：source-hashes.json 中 13 个依赖文件 SHA256 均未变化；最终补丁可应用性检查退出码 0。
