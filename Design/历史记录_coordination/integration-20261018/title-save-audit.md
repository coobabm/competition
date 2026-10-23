# 开始界面退出与存档只读审计

审计范围：competition-latest-20261016 当前代码；未编辑生产代码，未启动 Unity。下述为源码结论，不代表此次实机验收通过。

## 已有行为与入口

- `Assets/LingGuangV05/CaseMenu/CaseMenuController.cs:20` 只有 startButton/settingsButton。`Activate` 在160行附近只有开始/设置；标题没有退出、读档、新游戏按钮。`BeginGame` 只启动现有开机动画，并不建立新档。
- `CaseMenuHud.cs:52` Build 创建屏幕覆盖式 HUD，可在 BuildChrome 添加独立“退出游戏/存档”按钮，无需改机箱模型或动画。注意现有 SetChromeAlpha 只改 alpha，没有关闭 blocksRaycasts/interactable；新增可交互标题控件必须同步关闭射线，避免进桌面后透明控件挡输入。
- `CaseMenuMainDisplay.cs:54,96` 标题预加载真实 Main，Runtime 初始化已自动读主档，然后暂停游戏 host。Runtime 可由公开 `Runtime` 取得。因此当前“开始”实际是有档继续、无档新游戏；标题读档需在预加载完成后操作该实例，并保持其 paused 状态。
- `DesktopBridge/GameExit.cs:13` 是关机回标题，不是退出程序；保存成功才运行现有逆向关机动画，失败保持桌面并提示。不能把新标题退出键直接接 ReturnToTitle（会重新加载标题）。
- `Runtime/ChapterOneRuntime.cs:190` SaveNow 收集 Saving 回调（AI/YY/贴吧子状态），主档写盘。自动保存300秒且桌面输入空闲2秒（154–168）；暂停、退出、编辑器停止另存（358–365）。序章不保存，完成序章首次存档（329–345）。标题预加载读档会更新 lastSeen 并保存，当前无离线收益。
- `Runtime/ChapterOneRuntime.Slots.cs` 已有6槽：1是原主档，2–6手动；`SaveSlot`/`LoadArchive`/`ArchiveDescription`/`ArchiveExists`；另6份首次升阶快照 `chapters/1…6`。读取先验证/构建，再原子替换当前主档、Attach与Changed重绑。手动读取不做离线结算。
- `DesktopBridge/SaveSlotsPanel.cs` 已有设置里的“存档与章节”，第2–6槽保存、1–6槽读档、章节回顾、二次确认与错误状态、打开暂停tick关闭恢复。附着于 XingGuangController Awake。现有 Open 需 ui 已在设置窗口初始化且非序章，不适合直接拿来当标题读档窗口；它的Canvas排序31000低于标题HUD32000，且标题预加载期间该组件暂停。
- 正式存档路径依 ProjectSettings companyName=DefaultCompany/productName=competition，为 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/competition/LingGuangV05/chapter-one.sav`；槽位在同目录slots/2…6，章节在chapters/1…6，首次开局资料player-profile.json。录制专用构建另用游戏目录UserData。不要改productName导致玩家旧档失联。

## 老档保护

- `Runtime/Persistence/AtomicSaveStore.cs`：SHA256校验、2MiB限制、同目录.tmp落盘Flush、File.Replace保留.bak；主档损坏尝试备份；主档与备份均不可读时禁止覆盖；未知格式版本保护且不偷偷回退。重置将旧档改名为.reset时间戳，破产重开另复制.gameover时间戳。
- `Runtime/ChapterOneRuntime.cs:91` 当前GameState版本1；旧字段迁移走 `Core/ChapterOneSim.cs:22` 的MigrateHardware/ValidateState/MigrateEconomy/Story.Repair。`XingGuang/Core/XgSim.cs:71` 包含各子系统Repair、默认列表、旧训练迁移、旧checkpoint补条目等。
- `XingGuang/Desktop/XingGuangController.cs:108` AI嵌套labState版本异常保留原文本并另存lab-unreadable副本，写盘不覆盖该文本；读取槽位由LabStateReadable挡住不可读AI数据。保存前发现宿主换档会先重绑，防止上一档AI串入下一档。YY/贴吧亦订阅Saving。
- 现有测试：`Tests/PersistenceEditor/SaveStoreTests.cs`、`RuntimeDiskTests.cs`、`OpeningSkipTests.cs`、`BankruptcyRestartTests.cs`；`LocalBuild/Editor/IdleSaveAndStageSnapshotQa.cs` 验自动/手动/暂停/退出/重读；`ShutdownReturnSmoke.cs` 验关机保存失败不退；`PlayabilityDesktopSmoke.cs` 打开现有存档面板。此次未运行。

## 最小完整实现建议

1. 保留原开关机动画，标题HUD加“退出游戏”与“读取存档”。退出操作走独立Quit方法，预加载runtime存在时先SaveNow且失败不退出；成功PlayerPrefs.Save后Windows Application.Quit、Editor退出Play。输入/转场重入门控；按钮双语与键盘可达。
2. 复用既有原子存档后端，不另造格式/路径/槽位。标题读档UI只列6槽（时间、AI名、章节/阶段、空档/损坏状态），选档确认后LoadArchive成功再BeginGame；失败留标题展示具体错误。预加载未完成禁用读取；标题首次无档仍保留开始入口。
3. 游戏内继续用已有6槽面板，补明显“立即保存”按钮（当前仅2–6手动槽保存，主档无按钮）；显示成功/失败及自动保存状态。若复用为标题面板，需拆出不依赖设置window的初始化、AllowLoadDuringPrologue与只读标题模式，并处理HUD排序/暂停状态；独立轻量标题读档窗风险较小。
4. 标题文案有有效主档显示“继续游戏”，无档“开始游戏”；不擅自添加无确认清档。若加“新游戏”，必须调用既有ResetProgress并显式确认旧档备份，不能清槽或章节。
5. 回归须覆盖无档、现有老档、6槽保存读回、坏主档从.bak恢复、主备皆坏不覆盖、不可读lab、保存失败阻止退出、回标题后继续、暂停/读档时AI/YY/贴吧一致、中文英文及不同分辨率，最后独立Player实际退出。

## 实施时值得一并修的小问题

- `ArchiveDescription` 允许仅.bak存在并成功读取，却总用主档LastWriteTime，会显示1601等错误时间；应选择实际恢复文件时间，UI标明备份恢复。
- `LoadArchive` 构造ChapterOneSim没有外围catch；ValidArchive仅做浅验证，异常字段可能过前置验证而构造抛错。应将构造/替换/状态通知异常转换可读失败，磁盘替换前保证完整可构建。
- `SaveNow` 捕获Saving异常后仍可能保存并返回true；严格“保存失败不退出”应把序列化前回调失败判为失败或证明不会丢子系统数据，避免退出按钮误报已存。
- `SaveSlot(2…6)` 不更新lastSeenUnix且会把任何异常原样显示，时间来自文件仍可用；统一保存时间语义可做小修。
- 手动槽说明目前没有阶段，只是AI名/金额/写入时间；标题列表可新增阶段显示而不改文件格式。
