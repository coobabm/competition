# tech-027：label.captcha 验证码自动填充

结论：名称可保留；建议只修正 XgTechTreeCopy 的本节点解释和效果两行。已交付 changes.patch，未修改生产源码。核查日期：2026-10-13（Asia/Singapore）。

## 范围与已有工作

已阅读已有任务记录 Documentation/DesignPass/Agents/skilltree-task-001.md；该记录已完成公共文案层，本次不重复实现。未读取任何新的策划正文，未创建聊天或子 agent，未发送消息。只写入本目录，未启动 Unity/游戏，未触碰真实玩家存档、提交、推送或打包。

## 确认的问题

- 原解释“让数字识别模型帮忙填写验证码”容易让玩家理解为实际模型推理。实现只把手写数字最佳检查点用于购买门槛；RequireCaptcha 安排计时，TickCaptcha 到点直接调用 SolveCaptcha(true)，不执行识别、不按95%概率答对。
- 原效果没有明确购买后才生效，也未说明购买时已经弹出的验证码不会补设代填倒计时。实际购买只添加拥有标记；调度仅发生在验证码生成时。
- 原“仍可能引起平台注意”方向正确但不足以说明可见代价：前3次代填免于此抽查，第4次起需同时满足高速提交条件才抽取8%的额外信用扣分。
- 原名称“验证码自动填充 / Captcha autofill”准确，无需改名。该节点是标注技能，不是模型架构。

## 可直接采用的中英文文案

名称：验证码自动填充

解释：自动替你完成人机验证，不必再手输四位数字。

效果：已购买置信度分流、达到第三阶段且手写数字最佳检查点准确率 ≥95% 后可购买一次，无需装备或再训练。购买后新出现的验证码在 3–6 秒倒计时结束后自动通过；已弹出的不会补代填。每次通过信用 +1（上限100），解除本次验证造成的自动标注暂停。累计第4次代填起，若最近一分钟平均提交速度 >1.5 题/秒，每次有8%概率额外扣10点信用。功能不叠加。

Name: Captcha autofill

Explanation: Automatically complete the four-digit verification so you do not have to type it.

Effect: Buy once after owning confidence-based routing, reaching stage 3 and recording a best digits checkpoint accuracy ≥95%; no equipping or further training is needed. Newly issued captchas pass automatically after a 3–6 s countdown; an already pending captcha is not scheduled retroactively. Each pass adds 1 credit (cap 100) and clears this captcha pause on auto labelling. From the fourth lifetime autofill, each pass has an 8% chance of an extra 10-credit deduction if the last minute averaged >1.5 submitted cards/s. The feature does not stack.

## 源码证据与精确语义

以下路径相对工程根目录，行号对应 source-hashes.json 记录的生产版本。

| 文件 | 位置 | 证据 |
| --- | --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs | 462、578 | LabelNodes 进入实际目录，Node(id) 从目录查找。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs | 38、41–42 | parent=label.coop，基础购买费用2000，maxLevel=1；阶段字段本身为0，不能从目录字段误推购买阶段。 |
| 同上 | 146–164、180–192 | 已拥有即满级；先验父节点，再验 CaptchaAutofillReady，再验资金。购买成功扣款并加入 unlocked，不装备、不调度旧验证码。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | 188、252–256、988 | Has 为拥有标记；BuyNode 转入 BuyLabelNode；BestAcc 取最佳记录，缺记录返回0。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Captcha.cs | 37–43、64–67 | 倒计时3–6秒；抽查概率0.08；额外扣10点；保护次数3；购买要求 stage>=3 且 BestAcc("mnist")>=0.95−1e−9。 |
| 同上 | 113–147、150–163 | 质量系统有效时推进计时。新验证码生成时才依据 Has 安排代填。倒计时结束直接通过，不再重验模型准确率、是否装备或在线。 |
| 同上 | 177–192 | 通过清除本次验证并重置连续失败；先信用+1，自动代填次数再加1；累计次数>3且一分钟均速>1.5+1e−9时，roll<0.08才额外−10。不是检查输入节奏“太规律”。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Quality.cs | 60、79、153 | 信用范围0–100；每次加减分别钳制。质量功能已解锁、PlatformChecks 开启、自动标注至少1级才 QualityActive。 |
| Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs | 15、44、51、120、196 | 保留名称及神秘节点前置返回，只替换本节点解释和效果。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | 246–250 | 当前 NodeMystery 特指未发现的 label.auto；不能声称 label.captcha 自身目前被隐藏。 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs | 140–149 | label.captcha 的现行可见性允许展示；本补丁不更改任何发现/可见性规则。 |
| Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs | 758–772 | 神秘节点提示先返回，再取新文案；不绕过神秘提示。 |

补充边界：

- 95%是购买所需的最佳已评估检查点准确率，不是本次验证码识别成功率；购买后没有额外训练条件。
- 费用为游戏经费2000，由现有价格界面显示；本节点没有可反复购买的等级、时长叠加或倍率叠加。
- 秒为模拟 Tick 推进的秒数，实际通过发生于倒计时到期的更新步；不是CPU推理耗时。离线不会生成新验证码，但已有已安排的代填倒计时会先于离线超时保护继续处理（Captcha.cs:132–147）。
- 风险次数是存档累计自动代填次数，手工通过不增加或清零它。1.5题/秒是最近60秒总提交量/60，涵盖手工与自动提交，不是瞬时速度。
- 信用变化依顺序钳制：80分触发异常时为80→81→71；100分为100→100→90。因此不应统一写“净减9点”。
- 通过只解除当前验证码造成的阻挡；算力、复核队列、其他暂停/冻结仍可能阻止实际标注继续。不会增加训练样本、准确率或标注速率。
- 此处是平台四位数字人机验证，不是 cifar 的图片验证码标注桌。

## 补丁与整合合同

- 补丁目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs。
- 目标源文件 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。
- changes.patch 包含目标路径、SHA256和相邻上下文，仅替换两处 label.captcha case；未改名称、数值、其他节点或展示规则。
- 全部依赖哈希见 source-hashes.json。整合时若目标已被其他任务修改，请重新核对这两个 case 后合并，不要覆盖整个文件。
- 原目录节点文案仍含“太规律”，本任务遵守生产只读约束，仅交付科技页文案补丁；共享日志/目录改名不在本任务范围。

## 验证状态与待验收

已完成：静态调用链、购买门槛、单次拥有语义、计时调度、信用钳制及概率条件核对。没有把已有整合任务的编译结果记为本补丁验证结果。

未执行：C#编译、Unity运行、模拟实际运行、场景截图、视觉验收。以下均为整合负责人待执行的边界清单，不是 PASS 结果：

1. stage=2/3、mnist最佳准确率94.9%/95%、父节点缺失/拥有、资金1999/2000，分别核对购买状态；已拥有不可重复扣款。
2. 验证码生成后再购买，确认旧验证码仍需手动处理；购买后生成的新验证码到期自动通过。购买后降低当前训练进度不应停用已拥有功能。
3. 第3次与第4次累计代填；一分钟均速=1.5与明确大于1.5；强制抽查roll=0.079与0.08，核对边界以及新增1次代填后才判断保护期。
4. 信用80与100情况下分别核对成功加分及异常扣分顺序；确认人工通过不增加累计代填数。
5. 自动通过后存在其他标注阻挡时，不能误判为全部标注已恢复。
6. 中英文长说明在最小科技页窗口中可读、不裁切；未发现的自动标注名称/解释仍走神秘提示。统一编译及截图由主聊天执行。

结束前依赖复核结果见 verification.md。
