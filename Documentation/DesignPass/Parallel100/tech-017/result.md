# tech-017：datacenter（机柜租赁）核对交付

日期：2026-10-13（Asia/Singapore）。范围仅《技能树词条策划》已授权第3行对应的 datacenter 词条；没有读取新的策划正文。生产源码只读，未启动 Unity/游戏，未读取存档，未提交或打包。

## 结论与交付

需要一处精确文案补充：`changes.patch` 仅替换 XgTechTreeCopy.Effect 的 datacenter 分支。名称“IDC 机柜租赁 / IDC rack rental”和通俗解释正确，保留；Note 自动追加的“预训练有效规模 ×8（与其他道具倍数相乘）”正确，保留，不重复添加。

现有效果有三项缺口：未说购买即生效及接线条件；“可用显存”过宽，实际训练容量与驻留模型占用须区分；“运行时付费”忽略达到当前进度上限后 TickFinale 提前返回、不扣机柜租费的边界。补丁只使文字吻合实现，不调整任何模拟数值。

已核对已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`，原整合已覆盖通用文案、神秘提示及规模倍数；本补丁不重复这些工作。开始时本专属目录无现存交付。

## 可直接采用的中英文文案

名称：IDC 机柜租赁

解释：把显卡放进有专门供电的机柜，容纳更大的训练任务。

效果：购买后训练可用显存 ×8（接线模式按卡扣除驻留占用后计算），无需装备或先训练。新增 IDC 供电和机架；机架算力、显存分别为自有显卡对应最大值的 ×8。预训练须本体接在 IDC 机架上且在线；自动放置会迁入，手动放置需自行调整。启动后、未达当前进度上限时，另付租金电费 ¥25/游戏秒；付不起当步费用就停止并保留进度，达到上限停滞时不扣这笔费。购买不会直接增加已训练参数、数据或保证提分。

预训练有效规模 ×8（与其他道具倍数相乘）。【此句由现有 Note 自动追加，勿在 Effect 重复。】

Name: IDC rack rental

Explanation: Put GPUs in a rack with dedicated power for larger training jobs.

Effect: After purchase, usable training VRAM ×8 (after resident use on each connected card); no equip step or prior training required. Adds IDC power and a rack whose compute and VRAM are each the corresponding maximum among your own cards ×8. Pre-training needs the AI itself online on the IDC rack; automatic placement moves it there, while manual placement needs adjustment. Once started, while below the current progress cap, rent and power cost ¥25/game second; inability to pay the current step stops pre-training and keeps its progress. No rack fee is deducted while stalled at the cap. Buying does not directly add trained parameters or data, or guarantee a higher score.

Effective pre-training scale ×8 (multiplies with other items). [Already appended by Note.]

补丁中的8和25读取现有常量，不硬编码。价格继续由页面购买区显示，不在长效果段重复。

## 原文件证据与时机、单位、叠加

以下路径除 Desktop 外均位于 `Assets/LingGuangV05/XingGuang/Core/`，行号为本次快照；完整 SHA256 见 `source-hashes.json`。

| 证据 | 实际结论 |
| --- | --- |
| XgCatalog.cs:458；:348、353–360；:92 | datacenter 是 trunk 树的 Label 类功能节点，research 栏，第6阶段，前置 transformer，单次购买 maxLevel=1；不是可装备架构。基础60000 × StagePrice[6]=1。 |
| XgSim.Progression.cs:131–148；XgSim.cs:194–205 | 未拥有时 stage≥6 才显示，还需 transformer 和经费；实际 NodeCost=Ceiling(60000×0.25)=¥15000。发现/可见本身不产生收益。 |
| XgSim.cs:252–279、188 | 扣购买价成功后 S.unlocked 加入 datacenter，Has 当即为真；随后自动配置与评估。重复购买被 Owned 阻止，不会反复×8。 |
| XgSim.Finale.cs:55、63–66 | DatacenterRent=25；DatacenterVram=8。旧主机容量入口 Vram(host)=host.VramMB×8，空 host 返回0；不是增加8 MB/GB。 |
| XgSim.Wiring.cs:303–340；XgSim.AutoModel.cs:97–122 | 接线训练容量按在线接入卡扣除驻留模型等占用后×8；并行轮次仍占用共享显存。不能把所有已部署模型的物理显存也说成统一×8。 |
| XgSim.Wiring.cs:394–410、464–474 | 购买开放 IDC 电路（20000 W）和机架；机架 compute、vramMB 分别取自有卡的各自最大值×8，不是所有自有卡合计×8，也不保证所有训练吞吐×8。卡列表为空时这两个最大值为0。 |
| XgSim.Wiring.cs:592–606、1011–1030 | 自动放置的本体迁到机架；手动放置不会被强制覆盖。真实 IXgRig 上本体须在 IDC 来源且 online；家用电源触发跳闸，网吧线路拒绝，断线/未上卡阻止。非 IXgRig 检查宿主跳过接线条件，不能代替真实场景验收。 |
| XgSim.Finale.cs:75–111、128、145–171 | 拥有该节点立即为 ScaleMultiplier 贡献一个×8，与其他已拥有道具乘算；EffectiveScale=ScaleBars×ScaleMultiplier，ScaleBars 仍来自已训练参数和数据。没有训练数据时乘数不能凭空生成数据；有节点不代表自动开始预训练。 |
| XgSim.AutoModel.cs:219–235；XgSim.cs:274–276 | 买后可触发模型自动调整，但正在进行的 epoch 不改形状。不能承诺买完立刻增大模型、提高分数或使预训练快8倍。 |
| XgSim.Finale.cs:177–199；XgSim.cs:1490–1498 | 预训练运行且有效、进度未达 cap 时，先尝试 Spend(25×dt)，成功后才推进；dt 为游戏 Tick 的秒数、每步≤1秒。余额不够支付当前步就停，进度不清零；余额恰好用完且本步足够支付可推进，不能写成余额一到0立刻停。暂停、未启动、完成及已在 cap 停滞均不扣这笔机柜费用。其他训练开销不能用“这笔费用”替代。 |
| XgTechTreeCopy.cs:15、51、57–59；XgTechTree.cs:126–130；XgTechTreePage.cs:758–772 | 原神秘回退和页面可见性逻辑保留。datacenter 当前使用阶段隐藏；NodeMystery 专用于 label.auto，不应自行扩展或提前显露隐藏节点。 |

有一项既有显示差异需要整合者知道：XgSim.Wiring.cs:870–872 只依据 pretrainRunning 显示“计费中”和租金速率，即使 TickFinale 已在 cap 提前返回。本交付按实际扣款规则写文案，不越权修接线账单或模拟状态。

## 补丁基准和应用

目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

当前基准 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

`changes.patch` 带目标路径、原始行和相邻上下文，只含 datacenter Effect 一处修改。整合者先重核 SHA256；其他聊天改过同文件时，按该分支精确合并，不能覆盖全文件。若已出现同等修正，跳过本补丁。

## 验证与待整合

本次仅完成源码静态核对，以及实际执行 `git apply --check` 的补丁可应用性检查（退出码0，未应用到生产文件）。没有编译或执行 C#，没有运行 Unity、游戏、场景或视觉验收；已有整合记录的历史检查不算本补丁的运行结果。

待整合负责人采纳补丁并统一编译。待主聊天在隔离内存/测试场景验证真实边界：

1. stage5 未拥有时隐藏；stage6 缺 transformer 锁定；满足条件时购买¥15000，重复购买拒绝。检查中英文和未发现节点不泄露。
2. 固定其他状态对比买前买后：训练空余显存2 GiB应按同一基准变16 GiB；已有驻留占用先扣；容量0不凭空增长。机架对两张卡分别取最大算力、最大显存（两项可来自不同卡），不按总量或第一张卡。
3. 有效规模仅多一个×8，与另一个×2项组合为×16；零参数或零数据依然为0，不当成加8或购买后自动训练。
4. IXgRig 上自动本体迁架、手动家用放置不覆盖、未接电/未上卡不能运行。不能只用普通 IXgHost 替身验证接线。
5. 直接针对 TickFinale 隔离其他收费与收入：dt=0.5且 cap 下方时需¥12.5；余额12.5允许本步，余额12.49则停止且进度不变。进度恰在 cap/略低于 cap−1e−9 两侧分别核对不扣费/扣整步费用；跨过 cap 本步仍按完整dt收费。手动暂停与完成后不再收机柜费，停滞状态可能仍显示“计费中”。
6. 双语长段落的最小窗口、提示高度、缩放和裁切需截图验收。本次没有视觉结论。

不勾选全局原则、整份策划或其他词条完成。

结束前复核：source-hashes.json 所列9个生产依赖的 SHA256 均与交付基准一致。
