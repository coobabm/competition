# Codex 恢复后的综合风格只读审查

2026-10-19，新加坡时间。已收尾并停止。未改产品代码、未提交、未启动 Unity。本报告仅写入协调目录。

生产工程：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。以下路径均相对此工程。依据最新暂停后交接、协作状态、整合记录、当前源码差异及已有 LocalVerification 截图。最新桌游结果由主控运行，本审查只读取报告与截图。

## 需要交给 Claude 的具体问题

### P2：桌游结束回执在切换语言后仍保留中文

- 位置：`Assets/LingGuangV05/DesktopBridge/Games/GameHubView.cs:89` 在完成对局时把当前语言的完整文本写进 `sampleNote`；`:119` 原样显示。`MonopolyPanel.cs:76` 每次重绘仍引用这个缓存。
- 最新证据：`LocalVerification/BoardGamesUI/20261019-062247-669/result.json` 的 `monopoly complete en`，标题、骰子、回合均英文，但末句仍为“对局已结束。样本需在资料夹安装对局记录包。”。同目录 `monopoly-real-complete-en.png` 肉眼可见中文绿色提示；旧 `054110-877` 同样存在。
- 建议：保留完成状态/消息标识，渲染时按当前语言取文案。相邻 `commentary` 也在 `GameHubView.cs:109` 缓存本地化文本，可同时检查语言切换。无需改玩法或提示含义。
- 验收：中文完成后切英文、英文完成后切中文，回执与 AI 评语一起刷新。

### P2：YY 原生底栏切换语言后没有刷新

- 位置：`Assets/LingGuangV05/YY2016/YYChatView.cs:102` 只在 Attach 时写入“YY 2016 · 消息只保存在本机”；`:136` 的 OnLanguage 仅标记内部重绘；`:304` 起 Rebuild 不更新原生底栏。Attach 还移除了其 DesktopLocalizedText。
- 截图证据：`LocalVerification/SidebarLayout/20261019-060250-025/laozhou-news-dm-en-yy_dm-lz2_t04_resnet.png`，聊天内容与 Send/Close 已英文，底栏仍中文；`053542-858/reading-photo-en-photo_lili_gulangyu.png` 同样可见。
- 已有英文翻译位于 `Assets/LingGuangV05/Core/Lang.En.cs:228`，因此是刷新链路遗漏，不是缺翻译。
- 建议：在语言变化时同步更新持有的原生 Status 文本；保持 YY 的浅色 2016 应用外观。

### P2：游戏中心 AI 名称绕过统一显示规则

- 位置：`Assets/LingGuangV05/DesktopBridge/Games/GameHubView.cs:67–74` 直接返回 `Profile.name`，空名回退为“它 / It”；统一规则为 `Assets/LingGuangV05/XingGuang/Core/XgSim.Persona.cs:63` 的 `AiDisplayName`，经 `Core/AiNamePolicy.cs:70–74` 处理默认名与兼容值。
- 最新桌游报告的中文/英文账户分别显示“它 / It”，截图中的 `.exe` 仍为“灵光 / LingGuang”。这说明默认/未命名夹具下界面称呼已经不同；自定义名和旧档边界尚未实机复验。
- 建议：游戏中心的个人 AI 对手统一取 `Lab.AiDisplayName`，计算机对手保留 Computer。不要改稳定应用 ID 或把 `.exe` 固定品牌全部替换。
- YY 名称不单独报缺陷：已追到 `DesktopBridge/Story/AiJoinsYy.cs:657–666` 的 SyncContactName，正常路径按统一 PersonalName 同步；截图夹具冻结此驱动，不能据此断言正常 YY 姓名失效。

### P2：已打开的关机题面在语言切换后仍为中文

- 位置：`Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.Narrative.cs:103–121` 在 ShowSeed 创建时按当前语言生成题目与选项，现有面板存续时不重建这些文案。
- 最新截图证据：`LocalVerification/BoardGamesUI/20261019-062247-669/monopoly-real-complete-en.png` 的桌游和题目入口是英文，打开的题面及全部选项仍中文；`DailyFeedbackActual/20261019-050007-486/daily-rhythm-reduced.png` 同样可见。
- 建议：语言变化时刷新当前题面的展示文本并保留原题目/绑定/未作答状态，不自动关闭、回答或写入新的证据。此为既有跨模块问题，不是本次 Monopoly 账户扩容引入。

## 最新桌游布局结论：旧问题已过，新失败仍保留

- 主控最新 `LocalVerification/BoardGamesUI/20261019-062247-669/result.json`：中文与英文 `monopoly accounts` 均 `contained=true, overflow=false`。当前 `MonopolyPanel.cs:32` 的 380×380 账户扩容已有新验证，旧 `054110-877` 的账户溢出不再列为当前未修复问题。
- 同一最新报告 `monopoly complete en` 仍 `contained=false`，但 `overflow=false, truncated=false, missingGlyph=false`。截图还有右侧关机题面遮挡。只能确认此布局断言未过，不能直接等同为 TMP 文字溢出，也不能在缺少具体越界字符诊断时把原因确定为遮挡。建议接手者按原 contained 检查定位具体字形边界，并在正确英文回执下复验。
- 桌游整套仍 failed：`No exposed native Train drop point; do not bypass window occlusion`。`records-after-window-arrangement.png` 已显示资料夹被移至左侧，旧摆窗修复生效；但 `.exe` 的卡片覆盖层仍开着，主控诊断为 `XingGuang/Card Overlay` 拦截。应先用真实 UI 关闭/放下该卡片，再恢复真实拖包验收；不应再凭旧报告扩大文件夹摆窗修改，也不应绕过遮挡断言。

## 已检查且无新增有证据问题的范围

- 新闻分享正文：实际查看 `060250-025` 的中英文 ResNet 新闻页、英文 YY 与贴吧实际私聊图。正文、分享状态、老周回复、URL 均在内容区完整呈现；联系人摘要的省略是摘要行为。未发现支持再次增加 YY 气泡高度的证据。既有报告 477 断言/24 图通过不扩张为磁盘或模型验收。
- 群聊插画：实际查看 `053542-858` 鼓浪屿中英文卡片，发送者、图注及“原创情景插画 · 非实拍 / Original illustration · not a photo”均可见；与当前 `YYChatView.cs:587–645` 的实测高度布局吻合。通知整套历史失败仍由真实焦点/输入验证处理，不因图卡可见而记通过。
- Win7 / `.exe` 边界：新闻、贴吧、YY 和私人发送预览继续使用 Aero 原生外框与浅色内容；`.exe` 保持深色。没有本轮新引入实时模糊或将其它应用改为深色工作台的证据。不提出重做设计。
- 私人预览：`055258-062/ending-private-zh-plan-preview.png` 的标题、说明、原文、取消和发送按钮完整可见。`EndingLetterPresenter.cs:194–207` 的新增附记具有中英文标题、使用保留段落体系。现有私人 UI 测试中断于原生关闭定位，未走到完整中英附记与长信滚动，不能声称这些实际布局已验；本次没有新增可确认的附记裁切缺陷。
- 减少特效：`EndingLetterPresenter.cs:282–288` 用即时 ColorTint 保留静态反馈，音效另受 sound 开关控制。日常报告明确减少特效分支真实完成训练且无延迟节奏提示；本次静态截图不能证明所有动画或音乐节拍效果。未发现这批改动新增违反减少特效要求的证据。

## 审查时关键文件 SHA256

- GameHubView.cs：8DB6668463AECC386551D01E72641C42CDDFA78BA742E9F31055BDD582DC8480
- YYChatView.cs：50AD6BEBAB503AF2853D40CCAB58CD819352ED63B629FB25D52D7B3776D8D800
- EndingLetterPresenter.cs：6C7F90319B605BAC6CA47048DCE3B8EC12CA2724B7FF60930A61F9EED1D2217C
- MonopolyPanel.cs：BFA70AEAF8BE06CA7A2B2E86F93E6EA5891459F6AC76583418129E75F33B610D

用户已要求当前收尾后暂停；本报告交付后停止，不继续开发或追加验收。
