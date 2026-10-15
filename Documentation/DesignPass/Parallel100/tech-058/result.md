# tech-058 — AlexNet 词条核对

结论：建议只修正文案。当前名称 AlexNet 正确，保留；解释中的“两块显卡”容易被理解为游戏奖励或硬件要求，实际购买路径不发放显卡，也不自动解锁 ReLU。通用架构效果方向正确，但未交代 AlexNet 的实际数值、选用条件及共用折扣。已提供仅修改 XgTechTreeCopy 的补丁，未修改生产源码或模拟数值。

## 可直接采用的三项文案

中文名称：AlexNet

中文解释：用小窗口寻找图片里的线索，再把这些线索一层层组合起来。

中文效果：购买后加入视觉结构候选，由系统自动选用，无需手动装备；训练中购买须等本轮结束结算时再配置。选用后：基础可承受 8 层，批量归一化再加 6 层，实际层数仍受已解锁上限和显存限制；模型参数量＝层数×每层宽度²×7 个；成绩上限公式中的参数误差项 ×0.7。每轮训练卡数公式 ×1.3，与优化器、研究等倍率相乘；取整后为 4–320 张，离线模拟固定 12 张，各结构倍率不叠加。换结构后须重新训练和评估；有迁移学习时保留高于随机水平进度的 60%。拥有即适用的「一个词」参数、数据门槛均 ×0.7，与 LeNet-5 共用一次优惠，已有 LeNet-5 不再降低。

English name: AlexNet

English explanation: Find clues through small windows over a picture, then combine them layer by layer.

English effect: Buying adds a vision architecture for automatic selection; no manual equip is needed. A purchase during an epoch can reconfigure the model when that epoch settles. While selected: base depth tolerance is 8 layers, plus 6 with Batch normalization, still limited by unlocked depth and VRAM; model parameters = layers × width per layer² × 7; the parameter-error term in the score-ceiling formula is ×0.7. The cards-per-epoch formula is ×1.3, multiplying with optimizer and research factors; rounded output is 4–320 cards, or a fixed 12 in offline simulation. Architecture factors do not stack. Changing architecture requires training and evaluation again; Transfer learning retains 60% of progress above chance. Ownership applies ×0.7 to both parameter and effective-sample requirements for One word, shared once with LeNet-5: owning LeNet-5 already gives this discount.

## 实现依据与边界

以下路径均相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`；行号为本次读取时位置，完整 SHA256 见 source-sha256.json。

- `Core/XgCatalog.cs`（以上路径均位于 `Assets/LingGuangV05/XingGuang/` 下）149–150：AlexNet，2012，视觉，bias=.7、maxDepth=8、paramFactor=7、speed=1.3；note 直接写“两块显卡”。300–303 把正式名称和说明传入节点；407 定义第三阶段、前置 lenet、原始节点价格 3000。348、353–356 的阶段系数 1.4，再经 `XgSim.Progression.cs:131–148` 的 .25 系数，当前实际购买价为 ¥1050；不是旧架构 cost=6，也不是 ¥3000。价格已由界面显示，不在补丁中硬编码。
- `XgSim.cs:194–205,252–279`：拥有前置、节点可见、无进度阻塞且资金足够方能购买；成功后记入 unlocked，调用自动配置和评估。没有单独装备按钮步骤，也没有赠送双 GPU 或 ReLU 的代码。
- `XgSim.AutoModel.cs:136–183`：从已拥有、适合训练线的架构中按数据、层宽上限和显存选型，购买不保证使用 AlexNet。`219–241` 在 epochActive 时跳过配置；`XgSim.EpochAction.cs:44–49` 结束时先清除活动标记，再调用 `TrainEpoch`，后者 `XgSim.cs:649–657` 又先配置再训练，因此购买后的重配可能发生在本轮结束结算，而非必须等下一轮开始。
- `XgSim.cs:351–368,404–409`：ParamsK 单位为千参数，换算成参数个数为 depth×实际层宽²×7；基础深度耐受 8，已拥有 BatchNorm 时为 14，自动配置还取购买深度上限和显存限制。不是购买即升至 8 层，也不是相对 LeNet 的参数量 ×7（相同层宽下两者参数比是 7/3）。显存随该参数量和激活量计算，不是固定 2 倍。
- `XgSim.AutoModel.cs` 的 CeilingParts：.7 只乘参数容量误差项，不乘整个误差，不代表准确率直接提高 30%。数据误差、结构误差、学习率、噪声仍影响上限。参数量变化也会影响风险与收敛，文案不保证提分。
- `XgSim.Board.cs:191–199`：在线训练的 a.speed=1.3 乘入每轮卡数公式，与优化器、研究和其他公式项相乘；四舍五入并限制 4–320 张。OfflineSimulation 在公式前直接返回 12 张。不是每秒 +30%、不是每轮时间缩短 30%，也不是购买后全局 ×1.3。当前运行架构只有一个，架构倍率相互替换。
- `XgSim.Board.cs:62–73,94–116`：AlexNet 使用 LocalShared（局部共享连接）；ReLU 由独立拥有判断决定。通俗解释描述小窗口与逐层组合，没有把双卡并行或历史论文配置写成游戏已实现奖励。
- `XgSim.AutoModel.cs:251–280`：正常换架构会清除高于随机基线的数值训练进度；有 transfer 才保留 60%。这不是“准确率乘 .6”。旧版本自动迁移的 adopt 分支保留进度，文案针对正常游玩购买。`XgSim.Abilities.cs:183–188`：新形状至少实际训练一轮且评估达到 C，才可计入已训练参数 P；购买本身不增加 P。
- `XgSim.Abilities.cs:49–64,144–170`：拥有 LeNet 或 AlexNet 的任意一个，就将能力 3「一个词」两项门槛各乘 .7，只处理一条折扣记录。基础参数门槛 60K→42K（42,000 个参数），有效数据 60,000→42,000 条；双持仍为 .7，绝非 .49。正常购买前置已要求 LeNet，所以买 AlexNet 不会再降低门槛；优惠无需选用或重新训练，但能力本身仍有训练和确认条件。补丁明确共用，避免界面误解。
- `XgTechTreeCopy.cs:13–15,49–55` 与 `XgTechTreePage.cs:758–764` 的神秘节点提前返回保持原样；`XgTechTree.cs:129` 使用 NodeVisible。AlexNet 自身由第三阶段可见性控制。本补丁只在两个私有文案方法里匹配 alexnet，未扩展发现范围、未罗列后续节点。
- `XgTechTreePage.cs:772,788–795` 已另行显示能力折扣，补丁在正文补充“共用一次”，不再修改通用折扣显示；因此数值说明会有少量重复，整合后应检查实际排版。

## 交付与整合

- `changes.patch`：目标 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`，仅新增 AlexNet 的 Explanation / Effect 特例；Name 及所有神秘性判断不变。
- 目标文件基准 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- `source-sha256.json`：本次依赖文件校验值。并行整合可能修改同文件其他节点；若哈希变化，应重新核对目标上下文后只合入这两段，不能覆盖整个文件。
- 未发现本专属目录已有产物；在已交付的 Parallel100/result.md 中未查到 AlexNet 或 tech-058 的重复交付。不读取新的策划正文作为需求依据。

## 验证状态

静态检查：已完成代码路径与公式核对；补丁只插入两个 alexnet 文案分支，目标上下文唯一，原名称和可见性保护保持不变。已执行补丁可应用性静态检查（git apply --check，退出码 0，未应用补丁）；结束前复核 10 个依赖文件的 SHA256，全部与交付基准一致。没有运行 C#、Unity、游戏或真实存档；没有实际运行 PASS 或视觉验收结论。

待主聊天整合验证：

1. 第三阶段前节点不提前显示；进入第三阶段仍须 LeNet 与足额经费；中英文均显示对应解释。
2. 已有 LeNet 再购买 AlexNet，两项能力门槛保持 .7；AlexNet 未被自动选中时，当前模型不获得其速度/参数系数。
3. 空闲购买与活动训练中购买分别核对配置时机；换架构无迁移/有迁移的进度保留，低显存和低层数上限下不承诺 8 层。
4. 在线每轮卡数下限、上限、取整附近，以及离线固定 12 张的边界；实际成绩经训练评估且可能下降。
5. 中英文长文案换行、滚动、tooltip 完整可读；已有折扣行仍显示一次，解释正文不出现“两块显卡”。统一编译及实际场景截图由主聊天负责。
