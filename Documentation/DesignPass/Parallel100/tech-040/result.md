# tech-040 — augment / 数据增强

日期：2026-10-13（Asia/Singapore）。范围仅为 augment 的名称、解释与代码真实效果。生产源码只读，仅本目录写入交付；未运行 Unity/游戏、编译或视觉验收，未访问玩家存档。

## 结论与交付

名称「数据增强 / Data augmentation」正确，继续由 XgCatalog 名称回退，无需名称补丁。现有倍率 2/8/3 正确，但解释里的“裁剪”没有实际变体生成代码支持；效果遗漏有效样本数、自动生效时点与一次性倍率。因此 changes.patch 仅替换 XgTechTreeCopy 的两个 augment 分支，不改模拟数值、其他词条或显示保护。

已核对进度.md、队列.md 的相关记录与已有交付。tech-013 已说明 Dropout 与有效样本数的交互，tech-041 已核对数据清洗；未找到既有 augment 专属交付，不重复修改它们。一次初始关键词检索意外返回逐条清单中一条数据增强摘录；未继续打开策划正文，也未据此扩展任务或验收后续条目。

## 可直接采用的中英文文案

**正式名称**：数据增强 / Data augmentation

**中文解释**：把图片题平移，或把围棋局面旋转、镜像，换个样子继续练，答案保持不变。

**中文效果**：购买后自动生效，无需装备。训练题池大小与有效样本数：手写数字 ×2，围棋 ×8，CIFAR、ImageNet、表情包 ×3，文字不变；倍率含原题，只应用一次。减少成绩上限中的数据不足项和过拟合因素，后续训练再评估成绩；不保证固定提分。已收集样本数与总数据进度不变。

**English explanation**: Shift picture examples, or rotate and mirror Go positions, to practise different appearances with the same answers.

**English effect**: Active automatically on purchase; no equipment needed. Training-pool size and effective sample count: digits ×2, Go ×8, CIFAR, ImageNet and memes ×3; text is unchanged. Multipliers include originals and apply once. Reduces the data-shortage term in the score ceiling and overfitting factors; train and evaluate for results, with no fixed score gain. Collected samples and total data progress stay unchanged.

## 生效时点、单位与叠加

- augment 是 Research 节点，不是架构。阶段 3、前置 dropout、满足经费条件后可购买；仅变得可见或满足前置并不生效。目录节点 cost=900，NodeCost 当前为 Ceiling(cost×0.25)，故实际 ¥225；不使用研究描述记录中的 cost=25 作为科技页售价。价格继续由界面显示，文案不重复硬编码。
- BuyNode 成功将 id 加入 unlocked；AugmentFactor 随后的读取立即生效，不需装备、重新购买或先完成训练。购买还调用自动配置和 Evaluate，因此部分派生统计即时刷新；后续取训练卡开始采用扩大的池，具体何时遇到变体取决于 cursor，不能承诺下一张就是新变体。
- ×2/×3/×8 是无量纲倍率：题池单位为张，有效样本量单位为条。数字为原题+1种平移，普通图片为原题+2种平移，围棋为原局面+7种对称变换。并不保证每一张图都形成互不相同的内容（对称局面可重复），不表示额外再加 2/3/8 倍。
- PoolSize=BoardPoolAt(Samples, trainedCards)×F；不是直接把玩家库存条数变成可训练卡数，大数据包先经过基础池压缩与逐步开放。PassesOverData 的分母因此变化。
- SamplesEffective=max(1, Samples−NoisePenalty×(Noise+DataNoise))×F。先计噪声惩罚并下限为 1，再乘增强倍率；低样本甚至零样本时的公式下限并非赠送真实数据。
- 单次持有按 Has 判断；重复购买被 Owned 拦截。与原有样本先乘算，不按训练轮次连续翻倍。文字以及未列出的数据集 F=1。
- 固定其他参数时，CeilingParts 的数据误差项乘 F^(-0.6)，OverfitScale 乘 F^(-0.8)。与已有 Dropout 的 0.5 系数相乘，但总显示差距有封顶；DropRisk 的过拟合分量按 log10(参数/有效样本) 和截断计算，并非总风险除以 F。perSample 跨过 1 时 wobble 分量也可能变化；其余噪声、漂移、跳跃及总风险封顶仍适用。
- 这不直接修改已收集样本、总数据进度、参数量、每轮卡数或固定准确率。购买可能触发自动模型重新配置，不能承诺所有成绩数字购买前后完全不变；真实学习及成绩变化须在后续训练、评估中观察。

## 原文件证据

路径前缀为 Assets/LingGuangV05/XingGuang/；完整文件哈希见 source-hashes.json。下列行号为本次快照位置，后续整合应以符号和上下文定位。

| 文件 | 证据 |
| --- | --- |
| Core/XgCatalog.cs:306–309、369–370、414、637–638 | ResearchNode 取研究名称；augment 为阶段3、dropout前置的研究节点，专业名称准确。 |
| Core/XgSim.Progression.cs:131–148 | 科技页价格折算和阶段可见性。 |
| Core/XgSim.cs:188、194–205、252–279 | Has、Owned拦截、付款后解锁、自动配置与派生统计刷新。 |
| Core/XgSim.Board.cs:143–167 | 题池、遍历次数、F=2/8/3/1、variant=0为原题，后续变体生成。 |
| Core/XgBoardData.cs:166–184；Core/XgBoard.cs:35 | 数字向右平移；普通图片向右或向下平移；围棋用坐标翻转与交换构成对称；truth复制，非位置特征保留。Add仅追加特征，没有裁剪逻辑。注释提及crop不能替代实现证据。 |
| Core/XgSim.DataSources.cs:376–396 | 基础池最少1，样本多时按对数压缩及训练卡数逐步开放，然后才乘增强倍率。 |
| Core/XgSim.Collaboration.cs:108；Core/XgSim.cs:497–509 | 有效样本先扣噪声并保底1，再乘F；过拟合公式与Dropout系数。 |
| Core/XgSim.AutoModel.cs:334–365、454–468 | 数据量影响成绩上限误差项；风险按有效样本变化，多个分量与封顶使总风险变化非线性。 |
| Core/XgSim.cs:517–527、655–668 | Evaluate更新派生指标；训练才取卡、更新权重并推进StepAccuracy。 |
| Core/XgSim.Abilities.cs:124–134；Core/XgSim.Finale.cs:75–84 | 总数据进度没有增强倍率；augment不在预训练规模道具倍率表中。 |
| Core/XgTechTreeCopy.cs:15、57、99、232；Core/XgTechTree.cs:146；Desktop/XgTechTreePage.cs:818–832 | Name/Note保留神秘节点及图鉴提前返回，页面已接入新文案。 |

## 神秘性

不修改任何可见性、神秘节点或图鉴保护。当前 augment 本身由 NodeVisible 控制阶段，NodeMystery 的特殊隐藏对象另有其人。沿用现有数据集名称范围，不添加后续节点名称、奖励或路线；正式显示仍由原 UI 判定。本任务未将第3行整体或策划全文标为完成。

## 验证与待整合

静态代码链核对完成。changes.patch 标注目标文件 SHA256 与每处上下文；仅执行了补丁可应用性检查（没有实际应用）。首次检查因交付补丁使用 CRLF 失败，改为 LF 后普通 git apply --check 成功；这不是编译、模拟运行或视觉验收。结束前依赖复核见 verification.txt。

整合负责人只采用两个 augment 分支；文件若继续变化，应核对旧句和相邻上下文后手工合入，不覆盖其他聊天修改。中英文长提示需在最小窗口检查滚动/截断与购买按钮位置。

下列边界均待主聊天在隔离测试状态运行，未标 PASS：

| 边界 | 应验证的真实结果 |
| --- | --- |
| 阶段2、缺前置、经费¥224.99、满足条件¥225、重复购买 | 前三者不能得到效果；足额成功即生效；重复购买不重复扣费/翻倍。 |
| 各数据集购买前后，不训练先读属性 | F由1变为2/8/3，文字仍1；库存与总数据进度不变；有效样本与题池即时按倍率变化。 |
| mnist原题池10，cursor=9/10/19/20 | 依次为原题末张、平移首张、平移末张、循环回原题；不是购买后立即强制变体。 |
| 非对称围棋局面与对称局面 | 共8个变体位置，标签保持；对称内容可能重复，不按“8张唯一图片”验收。 |
| 数字6/9、非位置特征、边缘坐标 | 数字只平移，不旋转混淆；非位置特征保持；不意外增加裁剪/丢弃特征。 |
| 零样本、噪声惩罚大于样本、大包已开放与未开放 | 有效样本按保底1再乘F；题池按BoardPoolAt再乘F；真实样本仍未增加。 |
| 固定模型与噪声、perSample低于1/跨越1/超过风险截断 | 上限数据项与过拟合系数变化符合公式；总风险不能简单按F倍相除。 |
| 两种语言、未到阶段、购买前后、最小窗口 | 保持原发现规则；名称、解释、效果可读且不遮住操作区域。 |
