# tech-055 — mlp（多层感知机）

日期：2026-10-13（Asia/Singapore）。结论：交付仅针对 MLP 的科技页文案补丁，生产源码保持只读。补全英文专业全称，收敛“挪个位置就不认识”的绝对说法，补充自动选用、训练时机、结构不叠加和真实题型限制。没有修改模拟数值或其他词条。

## 去重与边界

已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md` 及 Parallel100 已有结果中对 mlp 的引用；未找到 MLP 专属重复交付。原实现已正确接入 Name/Note、保留 mystery 分支并显示能力折扣，予以保留。本任务不将全局原则或整份策划标为完成。没有启动 Unity/游戏、提交、推送、打包、读取真实存档或发送聊天消息。

## 可直接采用的中英文文案

名称：多层感知机（MLP） / Multilayer perceptron (MLP)

解释：用层层相连的网络，把简单线索组合起来判断；遇到位置或顺序变化，仍可能答错。

Explanation: Layers of connected units combine simple clues to make a decision; changes in position or order can still cause mistakes.

效果：购买后，两条训练线均可自动选用；无需手动装备，每条线只用一种结构，不与其他结构叠加。训练中购买会等本轮结束后再配置；换结构后需重新训练，购买不保证提分。基础结构支持 3 层，实际层数还受已解锁上限、其他技术和显存限制。使用此结构答空间或顺序题时，模拟正确率至多 60%（不是增加 60 个百分点）。下方能力门槛折扣在拥有后立即生效，无需先训练。

Effect: After purchase, both training tracks may select it automatically; no manual equip is needed, and each track uses one architecture at a time without stacking architectures. A purchase during an epoch waits until it ends before reconfiguration. A changed architecture needs training again; buying it does not guarantee a higher score. The base architecture supports 3 layers; actual depth also depends on unlocked caps, other techniques and VRAM. With this architecture, simulated correctness on spatial or order cards is at most 60% (not a 60-percentage-point bonus). The ability-threshold discount shown below applies as soon as you own it, without prior training.

现有页面动态附加行继续保留：对应第二项能力的“参数门槛 ×0.8 / parameters ×0.8”。这里降低的是门槛，不是购买价格、参数数量或准确率。若将以上文案用于科技页之外，应一并提供此倍率，而不要留下没有对应附加行的“下方”引用。

## 源码证据及完整口径

下列路径以 `Assets/LingGuangV05/XingGuang/` 为根；行号为本次核对版本。

| 原文件位置 | 证据及意义 |
| --- | --- |
| Core/XgCatalog.cs:141–142、300–303、386 | mlp 是 Arch，shared=true、maxDepth=3、span=4、bias=.85；第一阶段、前置 perceptron、目录价150。当前英文仅MLP，旧解释称“挪个位置就不认识”。 |
| Core/XgCatalog.cs:348、356；Core/XgSim.Progression.cs:132–148；Core/XgSim.cs:194–205 | 第一阶段可见；拥有前置且资金足够才能购买。现价 ceil(150×1×.25)=¥38。保持页面动态价格，不在补丁硬编码。可见不等于拥有，更不等于选用。 |
| Core/XgSim.cs:252–279；Core/XgSim.AutoModel.cs:219–247 | 成功付款后记录拥有，立即尝试自动配置并评估；epochActive 时配置返回 false，下一次轮间配置才可能切换。不能重复购买叠加。 |
| Core/XgSim.Progression.cs:20；Core/XgSim.AutoModel.cs:136–183 | shared 允许两条线参与选型；从已拥有结构中按可达评级、参数、上限等选取一种，购买不保证 MLP 当选。实际深度受 DepthCap、MaxDepth 和显存共同约束。 |
| Core/XgSim.cs:282–295、404–409；Core/XgSim.Progression.cs:15 | 第一阶段基础 DepthCap=1，买 MLP 本身不把实际模型直接变为3层；另行购买深度上限才可能增层。BatchNorm 可给 MaxDepth 加6，因此不能宣传“永远最多3层”。 |
| Core/XgSim.AutoModel.cs:250–279 | 通常换结构从随机基线重新练；拥有迁移学习时保留高于随机基线的60%进度，并受概念迁移规则影响。旧版状态迁移有 adopt 例外，不把重新训练表述成删除所有成绩或检查点。 |
| Core/XgSim.Abilities.cs:49–51、63、144–170 | Has(mlp) 即使未选用也将第二项能力参数门槛乘.8：6K→4.8K，即6000→4800参数；数据门槛仍6000条有效样本。当前只有这一项第二能力折扣；通用折扣按同一能力相乘，不跨能力叠加、同一道具不重复计入。 |
| Core/XgSim.Abilities.cs:181–206 | 新形状至少实际训练一轮且评估C级以上，参数才计入已训练参数记录；能力解锁还需样本条件、阶段训练及玩家确认。降低门槛不是立即赠送能力。 |
| Core/XgSim.Progression.cs:228–245 | MLP的空间题/顺序题 CardAccuracy=min(.6,accuracy)，后续仍可受其他惩罚；60%是模拟单题正确概率上限，不是每批题固定答对60%、加60个百分点或1000分制600分。低于60%的输入不被抬高。 |
| Core/XgSim.Progression.cs:255–265；Core/XgSim.AutoModel.cs:356–368 | cifar/poems 的 ProgressionAccuracy 分量也截至.6，但 CeilingAcc 后续可受学习率重启等修正，不能将它宣传为所有模型最终评估值永不超过60%。因此补丁只明确空间/顺序卡的限制，不提前列出后续数据包。 |
| Core/XgCatalog.cs:21–28；Core/XgSim.AutoModel.cs:334–348 | bias=.85 是容量误差项系数，不是85%准确率；span=4 的单位为 token，仅当数据集需顺序且所需跨度更大时增加结构惩罚，不是“记住4句话”或成功记忆保证。speed=1、paramFactor=1，无专属提速或参数倍增。 |
| Core/XgSim.Finale.cs:75–84 | ScaleItems没有mlp，不产生专属预训练规模倍率。 |
| Core/XgTechTree.cs:129、146；Desktop/XgTechTreePage.cs:758–794 | 页面已有可见性、mystery 提前返回、Name/Note 接入及动态折扣行；补丁保留全部逻辑，不列举未知后继节点。 |

## 补丁与整合

`changes.patch` 仅向 `Core/XgTechTreeCopy.cs` 添加三处 MLP 分支：Name、Explanation、Effect。Explanation/Effect 放在通用 Arch 返回之前，以免被通用分支吞掉；公共 Name/Note 的 mystery/atlas 提前返回保持原样。

目标文件原 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。完整依赖指纹见 `source-sha256.json`。整合窗口若已修改同文件，应根据上下文逐段合入，禁止用旧文件整体覆盖。仅有文案差异，不需要修改模型、购买或训练接口。

## 验证与待办

已执行：静态逐项追踪；只读补丁适用性检查（git apply --check）；结束前 SHA256 依赖复核，见 verification.json。这些不代表编译或游戏运行通过。

未执行：C#统一编译、实际模拟运行、Unity场景运行、视觉截图。均交主聊天统一验证。

应覆盖的真实边界（待运行，不标PASS）：

- 未拥有、已拥有但未选用、已选用三种状态：只有拥有状态触发门槛折扣；题型限制只依赖 run.arch。
- 第一阶段只有基础深度上限时购买 MLP，不承诺立即3层；增层上限与显存不足分别约束形状。已拥有增强深度技术时不应仍断言最多3层。
- 空间/顺序卡输入正确率.59、.60、.61，在无其他惩罚且非转换分支下分别保持.59、.60、.60；普通卡不因MLP强制截到.60。真实 UseBoard 分支另验证 BinaryAccuracy 转换后截断。
- 训练进行中购买：所有权与折扣立即可见，run.arch本轮保持不变；轮间是否选用取决于真实自动选型结果，不强制期待MLP。
- 不装备MLP时第二能力参数门槛仍4.8K，数据仍6000；买后不训练不能虚增已训练参数。重复购买不增加折扣。
- 中英文标题与长效果段落在最小窗口不截断；检查未发现节点仍维持原有隐藏/神秘显示，无后续节点名称泄露。