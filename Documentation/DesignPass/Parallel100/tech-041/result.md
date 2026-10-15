# tech-041 — dataclean（数据清洗）

日期：2026-10-13（Asia/Singapore）。本任务仅交付 dataclean 的文案核对与待整合补丁。生产源码只读；未启动 Unity、游戏，未访问玩家存档，未修改全局清单，也未发送跨聊天消息。

## 结论

名称「数据清洗 / Data cleaning」准确，沿用 XgCatalog 的名称回退即可，无需新增 Name 分支。XgTechTreeCopy 原解释和效果方向正确，但遗漏实际已实现的爬取数据降噪，且未交代购买即生效、一次性倍率及与按条清洗的计算顺序。因此交付 changes.patch，只替换本节点 Explanation 与 Effect 两行，不改模拟数值、目录名称、其他词条或可见性。

先检索了进度.md、队列.md 的相关记录和既有交付；tech-024/result.md 已核对按条清洗，故本任务仅说明其与 dataclean 的关系，不重复修改该节点。本目录此前不存在交付。未将策划第 3 行、后续条目或整份策划标为完成。

## 可直接采用的中文文案

正式专业名称：数据清洗

通俗解释：清理杂包、众包和爬取数据里的错误示范，让训练少受坏数据干扰。

真实效果：购买解锁后立即生效，无需装备或先训练。已有及新增的杂包、众包和爬取数据，其有效错标条数 ×0.5；数据包须下载完成并纳入训练。只购一次，不反复减半；若另有按条清洗，先减半，再扣已清除条数，最低为 0。自身自动标注错题、公共及剧情包噪声不享受此倍率。样本总数不变，噪声统计即时更新，后续训练按新噪声计算；准确率不直接增加固定数值。

## Ready-to-use English copy

Name: Data cleaning

Explanation: Clean incorrect examples from junk packs, crowd data and crawled data so bad data interferes less with training.

Effect: Active immediately upon purchase; no equipment or prior training needed. Effective wrong-label counts in existing and future junk packs, crowd data and crawled data are multiplied by 0.5; packs must finish downloading and be included in training. One purchase only; no repeated halving. If per-label cleaning also applies, halve first, then subtract the cleaned count, with a floor of 0. Your own automatic labelling mistakes and public or story pack noise do not receive this multiplier. Total sample count stays unchanged. Noise statistics update immediately and subsequent training uses the new noise level; accuracy gains no fixed bonus.

## 生效、单位与叠加合同

- 节点在阶段 3 可见，前置为 dropout；满足阶段和前置只允许购买，不等于获得效果。目录原价 800，当前 NodeCost 按 0.25 折算，实际购买 ¥200。价格由现有界面显示，不在补丁重复硬编码。
- 成功购买将 dataclean 加入 S.unlocked；Has(DataCleanId) 此后即时返回 true。该节点不是架构装备，也没有独立研发或训练完成门槛。重复购买会因 Owned 被拒绝。
- 0.5 是有效错标数量的无量纲倍率；数量单位为条，可包含小数，不是每次清除固定 0.5 条，也不是准确率增加 50 个百分点。数据包的展示噪声比例也乘 0.5，例如 30% → 15%。
- 令 J 为已下载且启用的杂包错标，C 为累计众包及爬取错标，P 为公共/剧情包错标，K 为累计按条清洗量：购买后的外部有效噪声为 max(0, P + 0.5 × (J + C) − K)。购买前为 max(0, P + J + C − K)。因此总剩余噪声不保证恰好减半。
- 自身自动标注错误另记为 Noise，不参与上述倍率。先应用倍率、再减累计清洗量的顺序不随购买与手动清洗的先后改变；不意味着购买 dataclean 就解锁手动清洗。
- 原始错标记录与样本总数没有因该倍率被删除；DataNoise、有效样本、噪声比例等读取即时按新倍率计算。训练卡生成及之后训练轮次采用新噪声；噪声翻转与掉分风险有非线性/上限，不承诺均减半或固定准确率收益。

## 原文件证据

以下路径以工程根目录为基准，Core/ 与 Desktop/ 均位于 Assets/LingGuangV05/XingGuang/。SHA256 完整快照见 source-hashes.json；changes.patch 另标明目标文件 SHA256 与逐行上下文。

| 文件与行号 | 代码依据 |
| --- | --- |
| Core/XgCatalog.cs:306–309、414、639–640 | ResearchNode 从研究目录取名称；dataclean 是 Research 节点，stage=3、parent=dropout、cost=800；名称为数据清洗 / Data cleaning。虽 kind=Augment，实际增强样本逻辑按 augment 的 id 判断，不为 dataclean 赋予数据增强效果。 |
| Core/XgSim.Progression.cs:131–148 | NodeCost 为 Ceiling(cost × .25)；NodeVisible 按阶段筛选。 |
| Core/XgSim.cs:188、194–205、252–279 | Has 读取 unlocked；状态先排除已拥有、不可见、缺前置和经费不足；付款成功即写入 unlocked，并刷新模型配置及 Evaluate。 |
| Core/XgSim.DataSources.cs:100–102、303–316 | CleanNoiseFactor=.5；dirtyOnly 仅计有效杂包，未下载完或未启用的包不参与当前训练噪声。公共/剧情包仅计入非 dirtyOnly 总量。 |
| Core/XgSim.DataSources.cs:413–427、446–449、466 | 杂包与 crowdNoise 的和先乘倍率，其他包噪声不乘，然后减 dataCleaned 并下限归零；自身 Noise 另行加入 NoiseRatio。Offer.effectiveNoise 对 Junk、Crowd、Crawl 来源乘 .5。 |
| Core/XgSim.DataSources.cs:635–651；Core/XgSim.Crawl.cs:71–80 | 众包和爬取都把原始噪声累加到 crowdNoise，故 dataclean 对两者的已有和新增数据均生效。 |
| Core/XgSim.Board.cs:153–178；Core/XgSim.DataSources.cs:439–442 | AugmentFactor 只检查 augment；训练 PoolCardAt 每次读取 BoardFlipRate。翻转率在噪声比例低于 .4 时按三次关系计算，不能宣称翻转率恒定减半。 |
| Core/XgSim.AutoModel.cs:454–468 | DropRisk 的噪声项为 min(.2, .6 × NoiseRatio)，整体概率另有上限及其他因素。效果是减轻噪声因素，不能承诺总掉分概率减半。 |
| Core/XgSim.Abilities.cs:124–133；Core/XgSim.Collaboration.cs:108 | DataNoise 同时参与有效样本统计；噪声减少可以改善这些派生统计，受下限限制。 |
| Core/XgSim.cs:517–528 | Evaluate 刷新派生指标，dataclean 没有固定准确率加成代码。 |
| Core/XgTechTreeCopy.cs:15、45、51、94、178；Core/XgTechTree.cs:146；Desktop/XgTechTreePage.cs:758–772 | 名称回退目录；说明已接入页面。神秘节点先返回既有名称/提示；补丁只替换 dataclean 两个 case，未移动保护。 |

## 神秘性

不改 NodeVisible、NodeMystery、图鉴保护和页面提前返回路径。当前 NodeMystery 特判的对象不是 dataclean，dataclean 的未到阶段由 NodeVisible 控制。新文案只解释本节点覆盖的数据来源和通用按条清洗规则，不披露其他未发现节点名称、奖励或后续路线。

## 验证状态与待验证项

静态核对已完成；补丁上下文检查与结束前哈希复核见 verification.txt。这些检查不是 C# 编译、游戏模拟运行或视觉验收。没有启动 Unity/游戏，以下边界全部待主聊天运行，不记 PASS。

| 待运行边界 | 期望依据 |
| --- | --- |
| 阶段未到、缺前置、¥199.99、¥200、重复购买 | 前三者不能获得节点；满足其余条件且 ¥200 时可购；成功后即时拥有；重复购买不扣款、不出现 .25 倍。 |
| 仅杂包 30 条错标，其余为 0；购买前后 | 外部有效噪声 30 → 15，样本总数不变；未进行训练也能读到新统计。 |
| J=20、C=10、P=8、K=5，自身错标=7 | 外部有效噪声 33 → 18；自身仍为 7；不能把全部噪声 40 直接变成 20。 |
| J+C=1；J+C=10 且 K=20；零噪声 | 分别为 0.5 条、0 条、0 条；无向下取整或负噪声。 |
| 购买后新增众包/爬取原始错标 10 条；无清洗且未触及下限 | 当前外部有效噪声增加 5 条，验证新增数据继承倍率。 |
| 同一杂包下载中、完成但关闭、完成且启用 | 前两者不进入训练噪声；第三种才贡献其原始错标的一半。报价页 effectiveNoise 可先显示折后比例，不代表数据已用于训练。 |
| 先清洗再购买；先购买再清洗 | 对相同累计清洗量 K，最终读取均按倍率后减 K；低噪声时实际可清洗量受限，不应假定两条路径总能清相同条数。 |
| NoiseRatio=.2 → .1 的隔离夹具 | BoardFlipRate 应从 .05 → .00625，说明翻转率不是固定减半；另测噪声比例封顶 .5 的混合来源情形。 |
| 中英提示、最小窗口、购买前后、未到阶段 | 长效果文案不得截断或覆盖购买按钮；保持既有未发现/不可见行为。 |

## 待整合

1. 整合负责人仅采用 changes.patch 的两个 dataclean 分支。若生产文件哈希变化，核对对应旧文案及依赖再合并，不覆盖全文件；不机械重放过期补丁。
2. 统一 C# 编译、上述真实边界运行与场景截图由主聊天负责。较长英文效果需实际检查排版。
3. 本任务交付完成；未修改 Assets、ProjectSettings 或任何其他任务目录。
