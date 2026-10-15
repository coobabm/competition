# tech-019 — label.raise 标注报酬倍率

日期：2026-10-13（Asia/Singapore）。范围仅为该节点文案；生产源码只读，未读取任何新策划正文。

## 结论与交付

现有正式名称、通俗解释、动态倍率与商店共享说明正确，保留不改。确认效果说明漏掉购买立即生效、档位叠加规则，以及普通人工答错罚款也随加薪提高的重要代价。因此交付 `changes.patch`，仅替换 `XgTechTreeCopy.Raise` 的显示文案，不改模拟数值、购买或装备逻辑，不触及其他词条。

已核对已有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`，不重复实现其文案层或既有检查。该记录中的编译及内存检查为整合聊天的历史执行记录，不作为本任务的运行结果。

补丁目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
基线 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
补丁含 `Raise(XgSim sim)` 方法上下文；请先核对哈希。如其他词条已更新文件，应人工合并该方法，不覆盖全文件。

## 可直接采用的中英文文案

正式名称：标注报酬倍率 / Labelling pay multiplier。

通俗解释：提高亲手答题的单题报酬。 / Increase the pay for answers you give by hand.

效果（`{当前倍率}` 与 `{下一级倍率}` 由现有目录读取；满级省略箭头及下一级）：

> 当前人工答题报酬 ×{当前倍率} → 购买后 ×{下一级倍率}；购买后立即用于各标注台后续的人工答题结算，无需装备或训练。与标注台商店共用等级。
>
> 倍率从 ×1 起，首次购买升至 ×2，此后每级增加 2，最高 ×34（17 级）；与适用的其他报酬倍数相乘，金额单位为 ¥/题。
>
> 按人工单题报酬计算的答错罚款也随之提高，实扣不超过余额；自动标注报酬不受此倍率影响。

> Current hand-answer pay ×{current multiplier} → after purchase ×{next multiplier}; applies immediately to subsequent hand-answer settlements across all labelling desks, with no equipping or training. Shares its level with the labelling shop.
>
> Starts at ×1; the first purchase gives ×2, then each level adds 2 to the multiplier, up to ×34 (level 17). Multiplies with other applicable pay bonuses; amounts are in ¥ per card.
>
> Wrong-answer fines based on manual pay also increase, capped at the available balance. This multiplier does not affect automatic labelling pay.

## 原文件证据与适用边界

路径均相对工程根 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`。

| 文件及行号 | 已核对的真实行为 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:462` | 实际目录导入 `XgSim.LabelNodes`，非仅孤立说明。 |
| `Core/XgSim.Collaboration.cs:30,41–42,146–184`（同 XingGuang 前缀） | 节点无父节点、stage 0、17 级；科技树购买调用 `BuyRaise(host, 1)`；可购买只需未满级及经费，无检查点/训练/装备前置。 |
| `Core/XgCatalog.cs:557–559` | 等级0–17对应 ×1、×2、×4、×6…×34；每次购买替换当前倍率，不把历史倍率连乘。下一级价格为 `Round(10 × 1.7^当前等级)`，单位 ¥，首次 ¥10。 |
| `Core/XgSim.cs:1237–1271` | 支付成功立即递增同一 `S.payRaise`；余额不足不增级；满级停止购买。无需等待训练或下一天。 |
| `Desktop/XgLabelPage.cs:256–260` | 标注台商店同样调用 `BuyRaise`，不会形成第二组叠加倍率。 |
| `Core/XgSim.cs:1222–1232`；`Core/XgCatalog.cs:660–669` | 人工单题基准为 `¥0.5 × 标注台系数 × [1+0.5×(max(1,题目难度)-1)] × 加薪倍率 × 2.5`；`HandFineFor` 也调用该人工报酬公式。不能承诺净收入按倍率固定增加。 |
| `Core/XgSim.cs:1385–1444` | 普通人工题答对才结算；适用的连击/教学纠正、金题、悬赏等另行相乘。教学模式使用其分支，不承诺所有加成都同时生效。答错非超时且非特殊终止题时收人工罚款。 |
| `Core/XgSim.Spider.cs:152–158` | 罚款应付额随人工基准提高，实扣 `min(应付罚款,可用余额)`。 |
| `Core/XgSim.cs:1465–1474` | 聊天答题答对也按 `ManualPayFor` 付款，无连击；不能把范围限于当前打开的一个标注台。 |
| `Core/XgSim.Collaboration.cs:419–443` | 人工复核也用当前倍率；符合条件的难例奖励另乘。这里答错不扣上述人工罚款，因此文案限定“按人工单题报酬计算的”罚款，未声称所有答错都扣款。 |
| `Core/XgSim.Family.cs:23–34` | 主观家人卡按 `ManualPayFor` 付款，没有客观对错；故总说明保留“人工答题”，不误写所有人工结算必须答对。 |
| `Core/XgSim.Collaboration.cs:387–398` | 自动题使用 `PayFor × QualityPayMultiplier`，没有加薪倍率；本节点不会提高自动报酬。 |
| `Core/XgTechTreeCopy.cs:15,36,51,112,188,201–207` | 现有名称解释正确；Name/Note 先处理神秘节点；Raise 原有动态数组索引和满级分支保留。 |
| `Core/XgTechTree.cs:146`；`Desktop/XgTechTreePage.cs:758–774` | 名称/提示已接入文案层；神秘节点提前返回，不泄露名称、效果和价格。本补丁没有列出后续未发现节点、专属奖励名或解锁条件。 |

购买只改变之后结算读取到的倍率：购买前已经发出的题，只要购买后才结算也取新倍率；已结算收入不追补。发现/显示节点本身不加薪。上述生效语义来自结算时直接调用 `ManualPayFor`，没有缓存购买前单价。

## 验证状态及待整合项

- 静态检查：已沿实际目录、购买、手动/复核/聊天/主观卡结算、自动结算、罚款及显示入口核对。
- 补丁检查：`git apply --check` 无报错，补丁未实际应用。结束前复核的10个依赖文件 SHA256 均与核对时一致。
- 实际运行：未运行 C#、未启动 Unity/游戏、未读取真实玩家存档。本任务不标记任何游戏行为为 PASS。
- 视觉验收：未执行；新增效果文字较长，主聊天需检查中英文提示的换行、滚动与窄窗口裁切。
- 待整合：仅应用该方法的文案补丁；保留名称、通俗解释、动态倍率来源和神秘节点提前返回。
- 待主聊天验证：0级 ×1→×2、1级 ×2→×4、2级 ×4→×6（不是 ×8）、16级 ×32→×34、17级无下一级；恰好够钱/差一点钱；从两处购买共用等级；购买后已在场题的结算；适用人工罚款上升但余额封顶；超时/复核错误的例外；自动报酬不变；神秘节点仍只显示原提示。
- 仅针对该节点交付，未宣称全局原则或整份策划完成。

## 依赖复核

完整依赖哈希与结束前复核结果见 `dependency-check.txt`。集成时仍需再次核对并避免覆盖并行修改。
