# tech-035：fp16 文案核对

日期：2026-10-13（Asia/Singapore）。范围仅为当前科技词条 fp16；生产源码只读。

## 结论

现有正式名称「混合精度 / Mixed precision」与 XgCatalog 一致，通俗解释可保留。该节点是 Research，不是可装备架构；fp16 是内部 ID，不应据此改写为已实现的 FP16 数值计算系统。

已有任务记录 `Documentation/DesignPass/Agents/skilltree-task-001.md` 已纠正「训练显存减半」的过度承诺，本任务不重复修复。当前效果行及自动追加的 ×2 数值正确，但未交代购买立即生效、无需装备/先训练，以及预训练上限与实际完成进度的区别。交付一处效果文案补充，保留名称、解释、动态倍率和模拟逻辑。

## 可直接采用的中英文文案

中文名称：混合精度

中文解释：用较短的数字表示参与计算，降低大模型计算的资源需求。

中文效果：购买后立即计入下列预训练有效规模倍数，无需装备或先训练；预训练可达到的进度上限随规模重新计算（最低 10%，最高 100%），实际进度仍需运行预训练推进。本节点不直接把训练显存占用减半，也不提高预训练推进速度。

自动追加：预训练有效规模 ×2（与其他道具倍数相乘）。

English name: Mixed precision

English explanation: Use shorter number representations to reduce the resources needed for large-model computation.

English effect: The effective pre-training scale multiplier below applies immediately on purchase, with no equip step or prior training required. The reachable progress cap is recalculated from scale (minimum 10%, maximum 100%); actual progress still requires pre-training to run. This item does not directly halve training VRAM usage or increase pre-training speed.

Automatically appended: Effective pre-training scale ×2 (multiplies with other items).

解释说明的是技术原理；效果段才是本游戏已实现的承诺。没有增加显存、速度、准确率或真实张量精度变化的承诺。

## 生效、单位与边界

- 可见/可买：通常第 6 阶段显示；须已拥有前置 Transformer。仅看见节点或满足前置不产生倍率。目录基础价 30000，当前 NodeCost 取 `Ceiling(cost × 0.25)`，实际为 ¥7500，由现有价格栏显示，不在正文硬编码。
- 成功购买即将 ID 写入 `S.unlocked`；ScaleMultiplier 立即按 Has 读取，无装备或启用步骤。再次购买因 Owned 状态被拒绝，不重复乘 2。
- ×2 是无量纲有效规模因子，不是 +2、不只是多 2%、也不是参数量或样本条数变为两倍。与其他已拥有 ScaleItems 的因子相乘；不要求当前训练架构另行选择 fp16。
- `EffectiveScale = ScaleParamsRatio × ScaleDataRatio × ScaleMultiplier`。参数与数据各自相对门槛的比值上限为 1.5；门槛 ScaleNeed=2000。`PretrainCap=Max(0.1, Min(1, EffectiveScale/2000))`。购买提高倍率，未必改变受 10% 下限或 100% 上限夹住的进度上限；零规模乘 2 仍为零。
- 购买不直接写入 `S.pretrain`。运行中的预训练在后续 Tick 读取新上限；已停止的预训练仍需启动且满足既有算力、接线及经费条件。进度仍按 `host.Compute × dt / PretrainWork` 推进。
- 训练页显示有效规模数值及道具芯片；终局页规模百分比最多显示 100%，所以已满刻度时不保证看到百分比继续上涨。

## 原文件证据

以下路径均相对 `Assets/LingGuangV05/XingGuang/`，行号为交付基线。

| 文件及行 | 依据 |
| --- | --- |
| Core/XgCatalog.cs:306–309、445、628–629 | Research 节点、前置、阶段、原价；现有正式名称。目录旧 effect 的显存减半描述由科技页文案层覆盖，本任务不修改目录。 |
| Core/XgSim.cs:188、194–205、252–279 | Has/Owned；购买条件；扣费成功后写入解锁列表；无 fp16 装备分支。 |
| Core/XgSim.Progression.cs:131–148、154–164 | 当前价格与阶段可见性；无 fp16 专属发现条件。 |
| Core/XgSim.Finale.cs:81、96–114、128–130 | fp16 因子 2；拥有即计入；乘法、参数/数据比例、进度上限。 |
| Core/XgSim.Finale.cs:177–199 | 后续 Tick 读取上限；实际推进仍耗算力、时间和经费；不读取 fp16 速度因子。 |
| Core/XgSim.cs:363；Core/XgSim.Finale.cs:66；Core/XgSim.Wiring.cs:303 | 训练显存需求、可用显存与显存倍率均没有 fp16 减半分支。 |
| Core/XgTechTreeCopy.cs:15、45、51、57–59、87、174 | 名称回退目录；神秘/图鉴提前返回；倍率动态追加；本次只替换效果行。 |
| Core/XgTechTree.cs:127–130、144–146；Desktop/XgTechTreePage.cs:428–430、758–772 | 隐藏未来阶段节点，神秘节点提示提前返回，已发现节点走文案层。 |
| Desktop/XgTrainPage.Side.cs:124–138；Desktop/XgFinalePage.cs:368–369 | 玩家可见的有效规模、倍率芯片及封顶百分比。 |

## 补丁与整合

- `changes.patch` 仅替换 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs` 的 fp16 Effect 分支，含前后上下文，不修改其他词条。
- 原目标文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。完整依赖指纹见 `source-hashes.json`。
- 保留 Name/Note 的神秘回退及阶段隐藏，不加入未发现节点名称、后续奖励或新入口。
- 补丁未应用。整合时若 SHA256 不同，先核对该分支和依赖语义，只移植这一行，不整文件覆盖。

## 验证与待验收

本任务只作静态源码核对与补丁适用性检查；未运行 C#、Unity 或游戏，未作视觉验收，未读取玩家存档。既有任务记录的编译和内存检查不是本补丁的运行结果。结束前依赖复核结果见 `verification.md`。

待整合负责人统一编译，并用隔离内存/测试场景验证：前置或经费不足不能取得倍率；首次购买只计一次；重复购买不再扣费或叠乘；其他道具已存在时按乘积计算；零规模、低于 10% 下限、跨越 100% 上限时不承诺上限翻倍；购买当刻实际进度不跳变、后续预训练 Tick 才推进；中英文长提示不裁切；第 6 阶段前的未拥有节点仍隐藏。

仅完成 fp16 的文案交付，不代表全局文案原则或整份策划完成。
