# 888 候选审查交付（2026-10-19）

已在独立目录完成候选审查与修订。没有修改生产源码或 Claude 原始交付，没有开启 Unity、Player、提交或推送。原件仍在相邻 `casino-scam/`；本目录是其审查后的完整替代候选，主控只应用本补丁，不叠加原补丁。

## 基线、补丁与新文件

- 生产：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`；HEAD `3533141a406940d571e064f3797328c7d214235a`。基线是当前脏工作区，不是干净 HEAD。
- 候选根：`C:/Users/xomem/Documents/ChatGPT/games/.coordination/claude-batch-01/casino-scam-reviewed`。
- 完整差异：`patch/casino-scam-reviewed.patch`，SHA256 **`e7e1d59eb98b94524f37918313549854e821ab9833bcb9e90288e7f24d929a83`**。
- 21 文件，962 行新增、172 行删除；统一回原有文件换行格式，未用整文件换行噪声扩大补丁。
- `baseline/` 是涉及的原生产文件快照，`candidate/` 是完整候选。`baseline-manifest.json` 列全部基线/candidate SHA256、是否新增、最终生产文件仍匹配基线的检查结果，以及最后使用的外部 Core 程序集 SHA256。
- 新文件只有三组 C# + meta：`Assets/LingGuangV05/XingGuang/Core/XgSim.Gambling.cs`、`Assets/LingGuangV05/DesktopBridge/Casino/CasinoPage.Scam.cs`、`Assets/LingGuangV05/XingGuang/Tests/Editor/XgCasinoScamTests.cs`。

生产修改文件还包括 CasinoDesktop、CasinoPage、CasinoPage.Blackjack、XgSim.Casino、XgBlackjackRules、XgSim.YyLearning、XgEndingPreparation、XgEndingNarrative；测试修改 CasinoPlayQa、CasinoJuicePlayQa、XgCasinoTests、XgBlackjackTests、XgCasinoFairnessTests、两份 Ending 测试。具体相对路径以 manifest 为准。没有修改 YYChatHub、群预设或图鉴源文件。

## 主要修订

1. **删除测试专用骗局开关。** 删除 CasinoScamHosted，CasinoDesktop 不再以 TestMode/useDiskSave 决定是否启用骗局。新下注统一用已有 `SystemUnlocked("casino")`（通常阶段 5，沿用 gradualSystems=false 旧档资格）及未通关状态作为正常能力门禁。成功扣下首笔下注后立即记录观察事件，再走原抽签/结算；非法下注不记事件。Core/隔离 QA/实际玩法走同一条骗局逻辑。
2. **首次下注与首把大赢。** 首次事件在已接受下注后写入状态；黑杰克尚未结算也记一次，保存重载后不重复。大赢先记录 First 再 Freeze，不再因先冻结而丢失首句。阶段 1–2 沉默也消耗首次事件，之后不会把第 N 把谎称为首次。
3. **跨档与通知。** 延迟群消息必须同时匹配 controller 的 house、XgSim 和 YY 所属 house；等待群引导/正在输入，只有真实加入当前会话后才记已发标志并保存。页面切换 owner 时重建控件，保证金按钮捕获原 XgSim 并重新验证归属和页面可见性，旧控件不能操作新档。自动到期关闭和群消息均显式走现有保存入口。
4. **已接受黑杰克手牌。** 冻结/查封仍阻止新局；此前已接受且保留的手牌允许 hit/stand 完成一次原结算，禁止增加 double 赌注。冻结通知/查封覆盖层等待手牌结束，避免存量手牌被永久遮住。未结束手牌期间不能交保证金或自动关站。
5. **真实事件来源。** 删除 gamble_888 对任意“赌/888 + 不/别”等词的误判。只识别 casino.888、明确来源、合法 paid/refused/ignored + good/bent 组合，拒绝 paid:good、任意前缀拼接及错误 bank。到期 ignored 是系统事件，来源 system、playerAnswer 为空，正文明确“玩家未点击选项”；不会伪造玩家回答。显式付费/拒绝才保留 player 来源。
6. **文案与知识边界。** 群友只谈广告、公开论坛、网站关闭，不知道玩家下注或私人 AI。只根据实际垃圾短信学习资格作“话术相似”的判断，去掉“这个精确网址/一模一样反诈短信我见过”的无证据陈述。移除把所有玩法概括成 98% 的不准确台词，双语改为长期赔率偏向网站。没有年龄评级或商店合规已通过的声明。

## 赔率与测试的范围

`CasinoRules` 类、`CasinoNext` 抽签方法、`BlackjackRules` 类与基线的代码文本完全相同，记录见 `verification/static-checks.json`。没有调整赔率、洗牌、发牌顺序或基本结算公式；冻结/保证金是额外的骗局资金行为，所以**原始赔率返还率不代表完整网站流程的实际资金返还率**。

原 200k 公平性长序列改为直接使用原 CasinoNext、CasinoRules、BlackjackRules 的纯抽签/赔率算法检查，不关闭产品逻辑、不声称它测了赌场全流程。正式下注测试设置真实可用阶段，并按冻结资金与终止下注更新期望。新增测试覆盖正常门禁、首次下注/大赢/牌局重载、冻结后结算、拒绝 double、收费与样本幂等、自动关闭的系统来源、事件键拒绝。

旧 UI QA 的两次 ¥500 连赢会正常触发新骗局，不能保留它的旧期望而绕开逻辑。候选 CasinoPlayQa 使用新的隔离存档 owner 和 ¥10 小额交互；CasinoJuicePlayQa 先测小额胜利与牌局动效，最后一次实际 ¥500 赢触发冻结，检查扣走净赢、拒绝新注、通知层、真实拒绝按钮和查封页。之前“两个大赢连续演出”的范围已改变，截图也改为 small-win-impact/small-win-streak；不把小额场景称作大赢验收。这些仍是旧 QA 架构的脚本候选，未执行，不等于新增严格前台/LCD全套验收。

## 保留的可调设计决策

遵照主控最新要求，沿用 Claude 候选而不擅改数值：从本版本首次观察起，净赢达到 ¥300 **或** 30 把触发冻结；冻结净赢并从钱包扣除；保证金冻结额 20%、至少 ¥200；第 15 把劝停；劝停后再押至少 10 把或交保证金记为偏；输至少 ¥1000 的提醒每十把最多一次、总共两次、普通提醒相隔至少 20 秒；每存档最多六句；群聊延迟 40 秒；冻结未选到下一游戏日自动关闭；关站赠 spam 样本 300 一次。旧存档先前累计收益/轮数不追溯计入。这些是候选设计值，可调，不是经实际玩家测试确认的最优平衡。

保留的行为边界：日期未知不自动到期；样本收益不是手标记录；实际 UI 购买/保存失败的体验、关站后的大赢动画是否与文案冲突仍需运行观察。对极端非正常钱包金额的冻结上限沿用原候选的 CasinoRules.AllIn 保护，未扩大算术范围。

## 已执行证据

- `verification/runtime-report.json`、`verification/final-run.log`：**独立 .NET 68/68 通过**，0 失败。包括赌场核心、黑杰克、原始概率算法、骗局、展示纯函数。执行 `dotnet run --project CasinoChecks.csproj -c Release`。这里只用 System.Text.Json 检查核心存档字段往返，不声称 Unity JsonUtility 或磁盘保存已经实测。
- `verification/unity-compile/core.log`：Unity 引用隔离 Core 编译退出 0，3 既有警告。
- `verification/unity-compile/game.log`：Unity 引用隔离游戏程序集编译退出 0，108 警告（大量既有旧 API/字段警告）。
- `verification/unity-compile/tests.log`：隔离测试程序集编译退出 0，0 警告，包含修改后的 Ending 资源测试；**编译不是执行这些 Ending 用例**。
- `verification/unity-compile/editor-qa.log`：两份 Editor QA 隔离编译退出 0，4 个 CS0436（候选类遮蔽已有 Editor 参考程序集同名类，编译器选择候选源）。
- 编译只调用已安装 Unity 的 `NetCoreRuntime/dotnet.exe DotNetSdkRoslyn/csc.dll @<rsp>`，没有运行 Unity Editor；响应文件与输出保留在该目录。
- 主控同期合入群预设/图鉴后，旧参考 .ref 未含 GroupPresetState，曾导致复编失败。按主控确认改用最新生产 Bee 源清单和 `Library/ScriptAssemblies/LingGuangV05.Core.dll` 后，上述三个程序集全部重编成功。原失败保存在 `game.log.before-current-dependencies`；错误工作目录及一次测试重复 bankId 初始化错误也保留日志，均已纠正。
- `git -C <production> apply --check <review>/patch/casino-scam-reviewed.patch` 最终退出 0；仅检查未应用。最终涉及文件逐一对照生产，均仍与快照基线相同。

## 未验证与主控接续

尚未生产合入、Unity 导入/运行、实际界面点击、截图/中英文几何/字体、YY 延迟/跨档通知运行、风控旧按钮运行、隔离磁盘 SaveNow 失败与重试、Ending 资源用例执行、模型推理、普通 Player、音频、性能、构建或发布。核心幂等证据不能代替 UI 按钮与真实磁盘事务证据。

主控审阅21文件 diff，尤其与终局 A 候选共享的 Ending 文件 hunks，先 apply-check 再按差异合入。不要整文件覆盖共享修改。确认单 Unity 占用后重新编译，跑本候选赌场/黑杰克/骗局/Ending EditMode 集合，再用隔离存档走双语付费、拒绝、自动关闭、旧控件和换档、已开手牌、关闭重开、减少特效、保存失败路径。最后才对四张新增/更名 UI 截图和人物知情作实际验收。
