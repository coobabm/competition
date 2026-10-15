# 3:014 默认常识桌候选（冻结，尚未部署）

仅处理紧邻 3:013 的一条要求：**默认桌改为「常识判断」，算术桌推迟到阶段 2。** 没有读取后续条目，没有修改 Assets，没有接触 AnswerTraceChecks、AnswerPaths、Corrections，也没有启动 Unity。

## 小补丁

- `production.patch`：4 个生产文件，8 行新增、9 行删除。
- `verification.patch`：3 个现有 Editor 检查文件，56 行新增、12 行删除。
- `sources/`：完整候选，仅用于编译与审阅，不应在哈希失配时覆盖正在演进的生产文件。
- `source-manifest.json`：逐文件原始字节 SHA256 与候选 SHA256。首次校验时全部基线匹配；冻结交接前主代理继续修改了 XgSim.cs，当前仅该文件哈希失配，不能整文件覆盖。

两份补丁均已通过 `git apply --check`，未执行 apply。PlayabilityChecks 原文件使用 CRLF，补丁保留其 hunk 行尾；其余文件保持原 LF。父代理先验收 Growth/Wiring，再另批接入本条，避免把默认桌变化混入那一轮。

## 行为与旧档策略

新局由 XgSim.PrepareProgression 统一选择 `sense`；同属新局的 Sequence 训练数据也随之选择 sense，避免教完常识后训练仍默认无样本的 SMS 数据。删除 Controller 中再次强制选择 spam 的新局覆盖。已有存档不经过这段新局分支，现选 desk 和训练数据均保留。

算术从新局 desksOpen 和目录的 Start 类型同时移出，防止 Repair 自动补回。ProgressionDeskAvailable 在阶段 2 自动允许算术，与现有 digits/danmu 方式一致；不要求购买 arith.pack。该数据包仍是原有付费样本来源，价格和科技配置不变。算术锁定提示也改走相同的“学会第二种能力后自动开放”说明。

对有效旧档保留已保存的 desksOpen、desk、sequence.dataset、labels、cards、模型和学习数据；不清空、不重置、不重算。已开放过的算术桌继续可用且不会显示为锁定。已有 pack/owned 的兼容路径保留。没有给新局补历史解锁，也没有增加存档版本迁移。旧档已经开着但未玩过的算术桌也保留，因为已有的合法解锁不应被撤回。

XgGuide 原本已优先推荐常识判断，无需改其策略或扩充引导文案。

## 首课与检查兼容

自然首分钟通过真实 setup 落入默认 sense，并先断言真实当前桌、当前卡都是 sense 且算术尚未开放。确定性中英纠正场景使用 `XgMemes.Pick(sense, Random(9137), level 1, actual date/topic)` 得到题库原题“北京是中国的首都。 / Beijing is the capital of China.”，不改真值；XgBoardData 仍走生产特征提取，实际得到 北京、是中、中国、国的、的首、首都。现场必须确认生产初猜是 No，再走实际“猜错了”按钮。截图写盘、英文三个 companion 按钮、真实 TMP 布局、首分钟时钟及反馈断言均保留。

DesignFirstLessonChecks 的新局检查改为 sense，增加实际阶段门槛、Unity JsonUtility 内存往返及旧档保留校验。其纯 SMS 答题夹具显式 SelectDesk(spam)，不会把它冒充默认首卡。PlayabilityChecks 同样先校验 sense 默认卡，随后显式访问原 SMS 奖励/评估夹具。主代理所有的 AnswerTraceChecks/AnswerPaths/Corrections 未改；若其中依赖当前默认桌为 spam，请由主代理在对应夹具显式导航。

## 已执行校验

1. `core-checks/SenseStartChecks.csproj` 直接编译全部真实 Core、XingGuang.Core 源码及三份核心候选；无模拟器替身、无 Unity、无玩家文件。实际运行 `SENSE_START_CORE_CHECKS_PASS assertions=23`，日志 `core-checks.log`。包含真实自然常识首卡、初猜不读取真值、一次真实纠正结算、阶段 1 选择/Tick/读档均不能开放算术、阶段 2 自动开放一次及可实际选择、旧 spam/arith 桌在两次内存 JSON 往返后仍保留其选择/标签/卡片。这里的往返使用 System.Text.Json；Unity JsonUtility 的同类检查已经写进 Editor 候选，仍待父代理运行。
2. Unity 6000.4.5 自带 Roslyn 依次独立编译完整 `LingGuangV05.XingGuang.Core`、`Assembly-CSharp`、`Assembly-CSharp-Editor` 均退出 0。输出只在 `LocalVerification/DesignPass/ResumeLessonCompile/SenseStartCompile/`，未替换 Library 程序集。这证明源码编译兼容，不等于 Unity 导入或场景通过。
3. 两份小补丁干跑应用检查通过；首次校验时 7 份生产/测试基线匹配；随后 XgSim.cs 随主代理工作更新，其他 6 份仍匹配。

待父代理另批落地后的真实验收：核心 Editor 检查；自然首分钟默认常识卡及训练入口；英文真实常识纠正与截图；阶段 1/2 算术大厅状态和旧档继续访问。DesktopFade 已由父代理报告 53 全 PASS，与本候选无共享生产修改。

补充候选见 `scene-validation/README.md`：独立真实页面检查覆盖自然常识教页、实际训练按钮的数据集交接、阶段 1 不可选算术、阶段 2 及旧已开放档的大厅实际点击。该补充已独立编译，尚未部署或运行；原生产和验证补丁未改。阶段 1 教学期大厅按现有策略省略未开放桌，验收检查其不可选，不额外改变为灰色锁定行。

## 源文件基线

| 源文件 | SHA256 |
|---|---|
| Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs | 1D051CDC9A59AB3C1F1825A0445ABD486CC6638273320306FB427556F17685B7 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | CC5A252CE52C9E0AC6959C55DFFD25DA83B365AA65669FC19BFAAC1D5134B204 |
| Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs | 5C20DA1967F47EA3E5241613C69D1FEBDA3E5633BAB924499EB1AAAED1E222B6 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | E413F85601162CBD87E117C8EDFA8877FC7F99A49AC5DBDC706EE7B494FFCE45 |
| Assets/LocalBuild/Editor/PlayabilityChecks.cs | 5121049BF7E23D66DBEAE3514C0ADD9D092245B9C3199DD690B3B447C37A4A15 |
| Assets/LocalBuild/Editor/DesignWallpaperSmoke.FirstMinute.cs | 5C99776F76EC7C8681247D992011C07EEB19BEC09A55006157C0A921CEC0813D |

冻结前更新：XgSim.cs 当前 SHA256 为 4ED315EB21D350355B957F288B0D3D94941D9DA8B0391BCFE84711C4137A327C，候选基线仍保留表中原值。该文件在本条仅修改 DeskConditionText 的注释与算术阶段 2 提示分支；合入时应应用并核对这个小补丁，保留主代理其他修改，禁止复制完整候选覆盖。此前独立编译证据对应候选基线。
