# 灵光.exe — 第一章桌面集成

## 时代设定

游戏发生在 **2016 年**：新游戏从 2016 年 5 月 24 日（星期二）深夜开始（序章只有这一夜），在 AlphaGo 战胜李世石两个半月之后；设置完成后日历跳到 6 月 1 日，12 月 31 日收尾。日期由 `Core/GameCalendar.cs` 统一换算。右下角时钟和锁屏时间由 `GameEraClock` 驱动：日期走游戏日历，时和分跟随玩家电脑的真实时间，所以剧情文本里不写具体钟点。经济系统里的 120 秒「天」只是电费账期，不是日历日；它只改本场景的时钟对象，不写入 DreamOS 用户数据。系统改名 hongmengos 2016（桌面水印、设置 → 关于、开始菜单）；YY 的经典界面保留，设定为老版本客户端。玩家是 2016 届应届本科毕业生，合租在省城的城中村，手上唯一值钱的是读书时攒的游戏电脑（i7-6700K + GTX 980 Ti）。

## 灵光.exe 从哪来

灵光.exe 不是下载来的，也不是谁发来的。序章里那个入侵者（未来的超级智能，签名「红岸」）劫持了电脑，从一张名为「我的显卡在哪里.jpg」的假图片里解出了它（46.8 MB，约 47,923 KB，WinRAR 和文件属性里的大小都用这个数）。游戏里别处不再有「老周发来文件」或「YY 传文件」的说法。

玩家之后可以在浏览器里打开 lingguang.cc：2016 年风格的项目页，状态栏只写「内测版不公开下载」，按钮灰着，不提供下载。

旧存档里保留的下载流程（以下仅供维护者参考，新游戏不会走到）：
1. 浏览器里的 lingguang.cc 页按钮启动原生下载：46.8 MB，家庭宽带 ChinaNet（20M）约 2.5 MB/s，大约 19 秒；页面上的大进度条同步显示已下载 MB、网速和剩余时间。
2. 进度条走完后弹出原生通知，灵光.exe 出现在桌面、开始菜单和任务栏。此时页面按钮变成「打开 灵光.exe」。

安装状态保存在游戏存档 `GameState.appInstallState` 里：0 表示旧存档，视为已安装；1 表示未下载；2 表示已下载。DreamOS 自己的下载记录会按存档对齐。没有网络时，会为本次会话自动连上 ChinaNet，不写入 DreamOS 用户数据。

- 安装器：菜单 `Tools/LingGuang/Install 2016 Download Flow`（`Editor/ChapterOneDownloadInstaller.cs`），负责建网页、浏览器主页、网络和品牌文字。
- 运行时：`DesktopBridge/Story/LingGuangInstallFlow.cs`（隐藏入口、监听下载完成）和 `LingGuangDownloadPage.cs`（网页内容）。

## 语言

中 / 英切换只在系统「设置 → 辅助功能 → 语言」里，任务栏不再有「中 / EN」按钮。

## 入口

在 Unity 中打开 `/Users/kk/competition/Assets/LingGuangV05/Scenes/Main.unity`，点击 Play，等待桌面启动。游戏分为以下独立软件；也可从开始菜单启动。

| 软件 | 功能 | 复用方式 |
|---|---|---|
| 邮件 | 数据标注、垃圾邮件分类 | 复用 Main 中的原生 Mail 窗口、图标和任务栏 |
| 灵光.exe | 神经网络、技能训练、我的 AI、入学考试 | 原生控件组成科技工作台，配生成的神经芯片图；保留可编辑的真实游戏棋盘 |
| 寻宝 | 硬件购买、出售与机箱 | 原生商城控件、白橙商品卡、搜索框与生成的显卡/机箱商品图 |
| 家庭 | 用电账单、工作收入 | 原生账单首屏，显示真实欠费、用电与设备明细，配生成的电表插图 |
| YY | 下班屁股群、表姐、晴雯等的消息 | 已完整恢复原生 Messaging 联系人、气泡、输入框、发送按钮与窗口布局，不使用自绘聊天页。老周不在 YY 里，他只在摆渡贴吧私信 |

五个软件只有一个游戏时钟和存档，不会各自复制经费或训练进度。考试进行时，邮件软件暂停普通标注，避免把考试题误当训练邮件提交。

该场景先从 `/Users/kk/competition/Assets/DreamOS - Complete OS UI/Scenes/2010desktop.unity` 原样复制，再增量接入游戏。原场景保留，不覆盖旧版 LingGuang、DreamOS 代码、共享主题或全局构建列表。不要直接播放旧版 LingGuang 场景来寻找此应用。

## 第一章怎么玩

1. 开局手上是玩家自己的游戏电脑（i7-6700K + GTX 980 Ti）和一张可编辑的神经元网络，没有人送礼物。（早期原型曾由老周赠送一张二手卡，已删除。）
2. 在「邮件」判断邮件是否垃圾邮件。选择邮件的真实分类，不是“模型有没有答对”。正确标注积累样本/学习点；纠正模型的错误建议有额外样本，并对本次推理路径播放紫色反馈。
3. 「灵光.exe → 神经网络」可放置/移除隐藏节点。连线模式依次点起点和相邻终点，重复操作撤线。左侧输入、右侧是/否端口不可删除。保留输入到输出的通路，才能继续训练和赚钱。显存同时限制节点与边。
4. 标注至少 3 张后，在「家庭 → 工作订单」开启垃圾邮件过滤。订单自动赚经费；关闭/最小化任意或全部应用、切换别的桌面应用均不停模拟。
5. 在「灵光.exe → 技能训练」花学习点升级语义和训练效率；在「寻宝」买显卡（¥450），槽位满则买机箱（¥2000）。升频伴随热量和电费，不是无限免费收益。
6. 「家庭 → 电力中心」每 120 秒结算一个游戏日的电费，按 24 小时能耗、¥0.52/kWh 计算。欠费停止训练和收入；可卖备用显卡、缴费，或继续正确手动标注抵扣实际欠费。超负载须先降低负载再手动合闸。
7. 达到 12 张标注、20 训练步、65% 识别率、双输出通路且供电正常，可参加「灵光.exe → 入学考试」。20 封不同模板邮件，16 题通过。失败保留成长并可重试；首次通过奖励 ¥800，仅发一次。

「灵光.exe → 我的 AI」可预设名字并查看存档操作；老周在摆渡贴吧私信里给提示（周而复始，研二，显卡发烧友）。后续章节与自由聊天的接入见各自说明。

### YY 原生聊天

YY 已按搜索到的 QQ2010 图片改为“窄好友主面板＋独立经典聊天窗”。桌面 YY 打开好友面板，搜索或双击联系人进入聊天；程序路由 `yy` 仍保留直接打开会话的接口供剧情使用。头像为新生成的红围巾熊猫，已有好友/群/最近、表情插入与消息记录滚动；群聊为空、语音未接入会明确提示。消息采用原生作者/时间＋正文记录，不使用现代大气泡。安装/原位修复菜单：`Tools/LingGuang/Install YY 2010`。

参考与说明：`/Users/kk/competition/Documentation/LingGuang-ChapterOne/YY-2010-PLAN.md`。
最终验证：`/Users/kk/competition/Evidence/YY2010/VALIDATION.md`。
头像及生成提示词：`/Users/kk/competition/Assets/LingGuangV05/Art/Generated/yy-panda-prompt.md`。

YY 从未修改的 `2010desktop` 原生 Messaging 组件树恢复，保留了 Main 中原窗口对象身份。直接在原生输入框问“电费怎么交”“显卡在哪里买”“考试”等，原生发送按钮创建玩家气泡，联系人的回复通过原生接收气泡接口显示。关闭窗口不丢待答消息；没有修改原字号、配色、间距或气泡样式。仅为缺失汉字提供私有字体 fallback。

恢复菜单：`Tools/LingGuang/Restore Native YY`。再次运行总安装器不会重新生成自绘聊天页。灵光、寻宝、家庭另由 `Tools/LingGuang/Install Native Game Apps` 安装原生控件布局；Mail 保留当前邮件分拣界面。

### 生成图片与实际配图

四张图片使用内置 ImageGen 独立生成，已导入为 Sprite 并绑定到真实界面 Image，不是只提供概念稿：
- `/Users/kk/competition/Assets/LingGuangV05/Art/Generated/neural-workbench.png`：灵光科技头图及“我的 AI”内容图。
- `/Users/kk/competition/Assets/LingGuangV05/Art/Generated/gpu-9800.png`：寻宝显卡商品图。
- `/Users/kk/competition/Assets/LingGuangV05/Art/Generated/pc-tower.png`：寻宝机箱商品图。
- `/Users/kk/competition/Assets/LingGuangV05/Art/Generated/household-meter.png`：家庭电表/账单主题图。

原始 prompt 与来源见同目录 `prompts.md`；PNG 原始字节未修改，保留透明通道及图片比例。商品图为游戏插画，不代表真实商品型号照片。

## 存档与安全

- 独立目录：`Application.persistentDataPath/LingGuangV05/chapter-one.sav`。
- 每 15 秒、暂停/退出时保存，也可点保存。读取会恢复到最近保存的状态；“重新开始”需要二次确认，旧文件改名留存，不直接删除。
- The opening story is seen once. When the opening's setup completes, the game writes `player-profile.json` next to the save (opening finished, plus the name, self-reference, what it calls the player, the personality sentence and its axes; the previous file is kept as `.bak`). It belongs to the player, not to one game. Every later new game (「重新开始」 after bankruptcy, reset progress, or a missing save) skips the prologue, the install and the setup, and ends the prologue through the real `ChapterOneRuntime.CompletePrologue` path with that profile: stage 1, 灵光.exe installed, the opening quiet window and the to-do note as for a first-timer. An older install without the file learns the profile from its existing save (including `.bak`, `.gameover-*` and `.reset-*` copies) and writes it. A first-ever player (no profile, no finished save) still gets the full opening. The game-over card has a smaller 「从开场重新开始」 button (`StartOverAfterBankruptcy(true)`; `ResetProgress(true)` likewise) to see the opening again; the setup chosen then becomes the profile. Code: `Core/Prologue/PlayerProfile.cs`, `Runtime/Persistence/PlayerProfileStore.cs`, `ChapterOneRuntime.BeginNewGame`. Tests: `Tests/PersistenceEditor/OpeningSkipTests.cs`, `PrologueTests`.
- 同目录临时文件 + 原子替换 + 上次有效备份；支持 UTF-8、SHA-256 损坏校验、版本保护。校验不是加密或防作弊。
- 再次启动按同一模拟规则结算最多 30 分钟离线时间（含电费）；结算后立即写入时间锚点，避免重复领取；时间回拨不产生收益。
- 所有本章 AI 答案/置信度为可复现的规则模拟。不下载模型、不联网、不调用付费服务、不执行系统命令。

## 本地模型（LLM for Unity）

自由聊天、判题和灵光说话用的是本地 Qwen3.5-4B。现在由 LLM for Unity（`ai.undream.llm` 3.0.3，底层 LlamaLib v2.0.5）在游戏进程内运行，不再启动 llama-server 子进程。代码在 `LocalLLM/LocalLlm.cs`。排队、按角色分槽、预热、超时和 GPU→CPU 降级的规则在 `Core/Chat/LlmScheduling.cs`，这些都跟以前一样。

- 运行库：第一次打开工程时，LLM for Unity 会把 LlamaLib 下载到 `Assets/StreamingAssets/LlamaLib-v2.0.5/`（约 1.7 GB，已 gitignore）。打包时它自己只留目标平台；Windows 包里的 GPU 库由 `LocalLLM/Editor/LlamaBuildStrip.cs` 删掉，因为 Windows 只走 CPU。
- 模型：`StreamingAssets/LingGuang/Models/Qwen_Qwen3.5-4B-Q4_K_M.gguf`。LlamaLib 的 llama.cpp 是 2026-03 的版本，不认新版 GGUF 里的 MTP（nextn）层，会报 `missing tensor 'blk.32.ssm_conv1d.weight'`。用 `Tools/LocalLLM/strip_qwen_mtp.py` 去掉这一层，其余权重原样复制（用法写在脚本开头）。
- 已知限制：llama.cpp 原生层如果 abort，会连游戏一起退出（以前只是子进程退出，然后自动重启）。LlamaLib 的 `apply_template` 不能并发，所以 `LocalLlm` 给它加了锁。`SixStageParallelQa` 通过 LlamaLib 自带的 HTTP 服务读 `/props`（POST），slot 占用改从 `LocalLlm.NativeCallsInFlight` 读。

## 检索（RAG）：晴雯和灵光的记忆

回复之前，游戏先找出和对方最新一句相关的几条资料，放进这一轮的状态块。系统提示词不变，所以前缀缓存照常生效。

- 打分：`Core/Chat/Rag.cs` 的 `HybridScorer`，用 BM25 加向量余弦。语义相近（余弦 ≥ .46）就算相关；字面命中但意思无关（余弦 < .36）就过滤掉。没有向量时退回纯 BM25。
- 向量模型：Qwen3-Embedding-0.6B（`StreamingAssets/LingGuang/Embedding/*.gguf`，已 gitignore），由 `LocalLLM/LocalEmbedder.cs` 在进程内单开一个 LlamaLib 服务、一条工作线程。向量缓存在 `persistentDataPath/LingGuangV05/embeddings-*.bin`。发问前会先算问句的向量，最多等 1.5 秒；资料在后台计算，还没算好的条目先只用 BM25。
- 知识库：`Resources/LingGuang/Rag/qingwen.json` 是晴雯的生活设定，`era2016.json` 是 2016 年的常识和新闻。每条带 `from`/`until`（游戏内日期），过期或还没发生的不会被检索到。可以直接改 JSON。
- 晴雯：每轮带 3 条自身设定、1 条时代常识、2 段窗口之外的旧聊天。实现在 `GirlfriendPromptText.Knowledge` 和 `GirlfriendPrompt.Recall`。
- 灵光：`XingGuang/Core/XgSim.Knowledge.cs`。从阶段 3 起能检索被教过的卡和时代新闻，阶段 4 起加上读过的书（按回目和概要检索），阶段 5 起加上和主人的旧对话。研究「词向量」之后才按意思检索，之前只按字面。

## 工程结构

- `/Users/kk/competition/Assets/LingGuangV05/Core/`：无 Unity 引用的模拟、题卡、拓扑、经济与考试；参数集中在 `Models.cs` 的 `GameConfig`。
- `/Users/kk/competition/Assets/LingGuangV05/Runtime/`：窗口之外的唯一时钟、存档与离线结算。
- `/Users/kk/competition/Assets/LingGuangV05/UI/`：复用的 uGUI 页签/棋盘、独立动态 CJK 字体与 UI 回归测试。
- `/Users/kk/competition/Assets/LingGuangV05/DesktopBridge/`：留在 Assembly-CSharp 中的 DreamOS 适配；不让自定义 asmdef 反向引用预定义程序集。
- `/Users/kk/competition/Assets/LingGuangV05/Editor/ChapterOneInstaller.cs`：幂等安装器，菜单 `Tools/LingGuang/Install Chapter One Desktop`。
- `/Users/kk/competition/Assets/LingGuangV05/Editor/ChapterOneSplitPlayQa.cs`：五软件跨应用 Play 验证器；通过真实 LCD 坐标转发器点击控件，不改硬件鼠标位置；测试模式不写玩家存档。
- `/Users/kk/competition/Assets/LingGuangV05/Editor/ChapterOneNativeChatInstaller.cs`：从只读原场景恢复真实原生聊天组件，重映射至 Main 后关闭参考场景。
- `/Users/kk/competition/Assets/LingGuangV05/DesktopBridge/ChapterOneNativeChatAdapter.cs`：绑定原生发送事件和原生回复 API，没有创建自绘 UI。
- `/Users/kk/competition/Assets/LingGuangV05/Editor/ChapterOneNativeChatQa.cs`：原生窗口、输入/发送/接收、关闭后回复、重新打开及无自绘残留的 Play 验证。
- `/Users/kk/competition/Assets/LingGuangV05/Editor/ChapterOneNativeAppsInstaller.cs`：从真正 DreamOS 控件模板组装三种原生软件，使用项目独立主题，不修改共享主题。
- `/Users/kk/competition/Assets/LingGuangV05/DesktopBridge/ChapterOneNativeAppView.cs`：原生 ButtonManager/输入框的数据绑定和指令；只有神经网络游戏图元由代码生成。

## 验证和边界

验证记录放在 `/Users/kk/competition/Evidence/LingGuang-ChapterOne/`。优先看软件拆分后的 `apps-editmode-final.json` 和 `apps-play-qa-final.json`；带 `regression` 或 `red` 的记录保留修复前失败证据，不表示当前结果。早期单软件的 `editmode-final.json` / `play-qa-final.json` 只证明拆分之前的版本。

纯逻辑单独复测：

```sh
dotnet test /Users/kk/competition/Tools/LingGuangChapterOne.Tests/LingGuangChapterOne.Tests.csproj
dotnet test /Users/kk/competition/Tools/LingGuangPersistence.Tests/LingGuangPersistence.Tests.csproj
```

Unity Test Runner 中运行 `LingGuangV05.Core.Tests`、`LingGuangV05.Persistence.Tests` 和 `LingGuangV05.UI.Tests`。实际 Play 回归另跑，不能用源代码编译或 NUnit 数量替代画面/输入检查。

当前交付限第一章。未接入真实 Laya/Qwen、后续章节、终局合同或打包发行；没有声称 Windows/macOS Player 构建已验证。自动回归用确定性答案和加速模拟时间验证流程，不等于真人难度/节奏试玩。
