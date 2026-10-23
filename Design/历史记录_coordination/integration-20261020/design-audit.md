# 上游玩法/文案/交互只读审查

对照：共同祖先 `c67a7db`，已交付 `c4e02003`，新上游 `c5c63198`。仅检查 Git 中源码、数据与相关资源差异；未启动 Unity、未操作游戏窗口、未改工程文件。

## 真正需要取舍或保护的项目

| 项目与路径（均相对工程根目录） | 已交付 / 上游行为 | 建议 | 是否需要用户确认 |
|---|---|---|---|
| `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.Books.cs`、`XgTrainPage.Side.cs` | 已交付书架按钮打开独立 `XgReadingWindow`，阅读问题走 YY；上游把祖先的内嵌书架、喂章节、原地回答框改成 prefab 布局。 | 保留独立读书及 YY 回答，把上游侧栏布局改进接到这一入口，不能整文件恢复旧阅读机制。 | 父任务已提问，等待答案；“现有玩法保留”本身支持保留已交付行为。 |
| `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/UiSans.asset` | 已交付有真实 Medium/Semibold 字重表及字体材质调整；上游主要增长字形缓存，没有改字体家族。 | 保留交付版字重表、材质及对应字体资源。特别保护 Medium `5a546f7d6d4db224ca003bdf921f9687`、Semibold `3235bdc6734e6094584b6e30c99f8cb9` 引用。 | 否，遵守“除标题装饰外字体保持”。 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Handwriting.cs`、`XgLabelPage.cs` | 交付/祖先默认鼠标画数字；上游默认“这个数字是 X 吗”的是/否卡，并在教学大厅加手写数字入口，画图仍可切换。并未新改解锁条件：`XgCatalog.cs` 的 mnist 原本就是 Start 解锁。 | 这是交互默认值改变、非删除玩法；如用户仅保留玩法而允许上游体验改进，可采用。若旧画图默认是明确确认项则保留旧默认。 | 仅在已有明确默认偏好冲突时问；不必把一般上游改进都升级为确认。 |
| `Resources/LingGuangV05/Browser/browser_news.json`（完整前缀 `Assets/LingGuangV05/`），以及 Era/Life/YY 数据 | 上游将 2016 Win10 免费升级/强制升级史实替换为 hongmengos 10；新闻标题已换名，正文仍“微软提醒”，出现内部不一致。 | 游戏桌面/开机系统名采用 hongmengos；历史新闻与群聊梗保留 Win10，或若采用全架空命名就连正文一起改。 | 若要求整个世界观同步改名，需用户选；否则作为文案一致性修正处理。 |

## 可直接整合、无需新增选择的问题

- **BlipTalk**：`DesktopBridge/Story/VoiceLines.cs`、`InnerVoice.cs`、`PrologueDirector.cs` 启用主角嘟囔音并同步逐字显示；已经得到本轮明确授权。录音迁到 `VoiceArchive`，不随包，符合确认。
- **88 秒影片**：`XingGuang/Core/XgOriginCut.cs` 与 `DesktopBridge/Story/OriginLive/OriginLive.cs` 将 161 秒场景时轴剪接成 88 秒，配 emergency，加入切镜闪光和轻微节拍缩放。仍涵盖九幕和六次能力跃迁；删去部分中间讲解镜头。Esc 跳过、结束恢复音频、`OriginCurtain` 片后“你觉得呢？”链路保持。静态审查未见剧情/结局功能丢失；若用户要求整合上游，可采用，无需再问时长。未进行运行时验证。
- **音乐**：`DesktopBridge/GameMusic.cs`、`Music2016.cs` 上游主要改为 `MusicApp`/`MusicPlaylist` 类型。仍是每次开机等待玩家打开音乐应用，开启时 repeat/shuffle=false 顺序播放、关闭窗口继续播放。保留交付版 `XgDailyPlayback` 完曲计数、`TryReadMusicOutput` 节拍反馈和验证目录路径，不需要用户另选音乐规则。
- **科技与 AI 页面**：`XgTechTreeSkin.cs` 仅把付费树形美术改成 `Art/TechTree` 自制图，没有改科技规则。交付版 `XgTechTreePage.cs` 的日常分支/统一图鉴入口、脑图页面及 AI 合成标识须保留；上游对主要 AI 窗口文件多为 namespace 迁移，未提出竞争设计。
- **小游戏**：上游 `MiniGames/XgDeskGamePage.cs`、`Games/ChessPanel.cs`、`Games/GameHubView.cs` 改动为 OS namespace 迁移。保留交付版每日题、棋局、读图、AI 回答记录等规则；没有证据要求回退。
- **HongmengOS 换皮**：`DesktopBridge/AeroRestyle.cs` 改卡片、面板、复选框、滚动条与自制壁纸，明确跳过 `灵光.exe`、任务栏、桌面图标、Spinner。它没有全局设置字体；新训练模块 `XgModuleRefs.Prepare` 仍使用传入 `kit.font`。`Fonts/OriginMono SDF.asset` 只是将同款 RobotoMono-Medium 从删除的 Feel 目录转向 `Localization/Fonts`，不构成字体设计变化。原生 OS 字体资源迁移需保持现有绑定/回退规则。
- **开机和主标题**：`CaseMenuSceneBuilder.cs` 换成自制 Hover/Switch/Boot 音效；上游没有修改标题主显示、AI 灰字说明或已交付开机字体实现。新 `OS/System/BootManager.cs` 仍支持开机/重启及 Main 关机回标题；需运行验收但没有新的设定选择。
- **已确认主线**：此次上游没有新的 Qwen 模型迁移、性格决定结局规则或老周知情边界重写；这些已交付行为继续保留。`Content/Native/LaoZhou.asset` 的变化只是 `m_EditorClassIdentifier` 指向自研 MessagingChat，不是台词改动。

建议对用户只呈现已有的独立读书/YY 回答选择；不要将 namespace、资源搬迁、字形缓存、88 秒技术映射等包装成额外设计冲突。若还需一条短问，只有“2016 新闻保留真实 Win10，还是连新闻也改成架空 hongmengos 10”有明确文案依据。
