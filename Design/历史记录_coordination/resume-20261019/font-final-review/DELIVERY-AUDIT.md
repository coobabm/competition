# 字体最终交付只读审查 · 2026-10-19

审查生产根目录：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。已读 `Design/字体与开机页改版_Claude_20261019.md` 与 `Design/Codex_字体交接与全局评审_20261019.md`，结论以实际文件为准。只写本审查目录；未改生产，未启动 Unity、编译、测试、Player、模型或音频。不接手 Claude E1/E2/E3。

## 结论

**来源清单、三份署名、源字体 GUID、TMP 字重、fallback 和已序列化预热覆盖已核对一致；可关闭此前“StreamingAssets 尚缺新增字重”的交付缺口。不能标记全局视觉/Player 验收通过。** 现有 720p 实际画面仍有小字细弱、长英文截断，长英文节点与动态 AI 名的完整字体验收仍有缺口。开机新样式与字体分别评审，不因这次资源核对自动接受全部样式改动。

可复核数据在同目录 `static-evidence.json`，生成脚本 `audit_delivery.py` 只读取限定字体文件/HEAD 字体并写审查目录。没有重扫模型或构建大包。

## 1. credits、manifest 与源文件

| 实际文件 | 核对结果 |
|---|---|
| `Assets/LingGuangV05/Fonts/UiV4/FONT_CREDITS.txt`、`Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/FONT_CREDITS.txt`、`Assets/StreamingAssets/Licenses/Fonts/FONT_CREDITS.txt` | 三份字节完全一致，SHA256 `a43a7a62a7aa281e882b7c4263fb8faf4dbd78ea97e1946f4a512ccf896076c9`；都列出 Regular、Medium、Semibold。 |
| source/StreamingAssets 的 `SOURCE_MANIFEST.json` | 两份字节完全一致，SHA256 `c01402ee13892c3fd00ab82455c84f5469e4fed151d0435483904b8d89f3bff3`；新增字重有各自的大小、SHA256、版本/来源，不复用 Regular 条目。Resources 下设计上没有第三份来源 manifest，不是漏同步。 |
| source/StreamingAssets 的 `MiSans-license.pdf` | 均为79535字节、SHA256 `4a93a27cd2bd81b3b5ecfd0a853144a876fa26938a93a68443c67d74172fcb86`，与 manifest 一致。 |

实际读取三个 TTF，各自大小/hash 匹配 manifest：

| 文件 | 字节 | SHA256 |
|---|---:|---|
| MiSans-Regular.ttf | 8122324 | `9c120f0a849bc0aa5048daae2a3c0f6eecd828b5b33fce682a9622833f5feea6` |
| MiSans-Medium.ttf | 8103840 | `b03e98374e971594b0b7a9706d0704241f76e1b88556cdda79c5039ef8a638d1` |
| MiSans-Semibold.ttf | 8034980 | `77c23f31ae124867778344970155a0c8d34a89897dedaab81aeee82ff00a4ce6` |

新增两个 TTF 共16138820字节（约15.39 MiB），不是最终 Player 包体增量。此处证明本地清单与源文件一致，没有重新从官方网站下载来独立证明上游原件，也没有检查尚未生成的最终发行包。

## 2. TMP 资产、字重、fallback 与预热

已直接解析 UiSans、UiSansMedium、UiSansSemibold 的 YAML，而非依赖交付说明：

- Source GUID 分别为 `75f62267335fcdb4daa0aff166dca296`、`a019dc2b7cc843a1b473e6599331644e`、`f986ce496703486c8a718f8688ee5d04`，均与对应 TTF meta 相符。
- 三个资产实际都是48pt、padding5、2048×2048、Dynamic、multi atlas开启、现有2张atlas引用；`m_ClearDynamicDataOnBuild: 0`，材质 `_Sharpness: 0.5`。
- UiSans字重表[5]指向 UiSansMedium（GUID `5a546f7d6d4db224ca003bdf921f9687`），[6][7]指向 UiSansSemibold（`3235bdc6734e6094584b6e30c99f8cb9`）。新字重自身[4]回到 UiSans、[5]/[6]/[7]同样明确指向对应真实字重。
- 三个 fallback 表均只有 `71ded2e539f874a4188d383d44f10375`，已解析为 `Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset.meta`，保持原有 Noto CJK fallback。
- 当前 UiSans有1393个字符/字形；Medium有1394个，Semibold有1393个。Medium/Semibold均覆盖常规字体的全部1393字符，无缺失；Medium另多一个 U+52B3（劳）。
- HEAD UiSans为1116字符，当前保留全部1116个并增加277个；不是将旧表清空重做。HEAD为1张atlas，当前为2张。该比较证明对HEAD覆盖无损，不能还原此前每个未提交中间版本。
- `UiV4FontWeights.cs` 当前实际保留：读取 `regular.characterTable` 的 unicode、对新字重 `TryAddCharacters`、通过SerializedObject设置clearOnBuild=false、原地修改字重表、不向只读属性setter赋值。`Ensure`复用已有asset/meta，复制fallback。
- 全局 TMP Settings的默认clear开关为1，但当前安装的TMP预构建器 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Editor/TMP/TMP_PreBuildProcessor.cs:23` 检查的是每个font asset的 `clearDynamicDataOnBuild`。三个资产为0，与保留缓存策略一致；这仍不是已构建Player中字形/atlas验证。

生成器的幂等性有限：`Ensure`对已存在asset只复制fallback，不自动修正错误source GUID、字号、atlas参数。本次实际资产正确，非阻塞问题；以后更换字体源不能只凭“再次运行构建器”认定旧asset已换源。

## 3. 使用范围

`Assets/TextMesh Pro/Resources/TMP Settings.asset` 相对HEAD无diff，默认字体GUID仍为LiberationSans SDF；没有发现运行时代码给 `TMP_Settings.defaultFontAsset` 重新赋值。`XgUiV4Fonts` 的代码引用集中在原有 XingGuang `.exe`/阅读页及本次 CaseMenu HUD、BootScreen。CaseMenuHud的实际diff只有优先UiSans、标题Semibold和字距2→3。

Tieba、浏览器Bodu、商店等仍调用 `PrologueDesk.CjkFont()`；该方法实际加载NotoSansCJKsc-Regular创建临时动态字体。YY/Mail仍取得原桌面商店绑定的字体，没有新增UiSans访问。未发现本次替换全局桌面风格的代码证据。注意这是源码/配置检查，不是所有桌面窗口的实际渲染巡检；dirty工作区里OriginMono/OriginSerif/Numeric等资产也有已有改动，不能仅凭git状态把所有变化都归于本次字体交付。

## 4. 亲看预览与实际截图

已查看：

- `LocalVerification/FontBootPreview/20261019-193848/title-hud.png`：标题与副标题层次明确，当前固定中文标题完整；这是中性背景的HUD样张，不能证明真实机箱背景对比度、全部设置/存档菜单、不同尺寸均通过。
- 同目录 `exe-specimen.png`：常规与Semibold层次清楚，未见这些样本文本重叠。源码显示样张主体字号34、标题26，不能代表实窗最小字号。底部所谓“旧仿粗体对照”实际使用desktopCjk（Noto），不是旧MiSans Regular同条件仿粗体；因此不是严格的同字体before/after比较。`0·?`是样张硬编码文本，不是截图缺字。
- 同目录 `boot-66.png`：徽记、logo、进度与中文状态没有可见碰撞，辅助BIOS/页脚对比度较低；实际LCD缩放后可读性仍需观察。它是离屏全画布样张，不能等同3D显示器内实际字号。
- `LocalVerification/DailyFeedbackActual/20261019-113914-712/daily-forecast-en.png`：实际1280×720画面中，训练上部信息、Gear网格及页脚文字细小，长英文出现截断，弹窗正文较清楚。不能写“720p可读性已解决”或“全局视觉验收通过”。中文便签/引导混排为该画面既有内容，不能据此归因新字体。

既有 `FontPlayModeProbe/20261019-192928/report.txt`记录了常规/粗体实际取用face和材质，但它是空场景既有诊断；其中Semibold字形数1388，与本次落盘1393不同，说明不能将旧probe当成最终版本全部资产已实跑的证明。临时FontPlayModeProbe.cs已不在当前入口中，不建议为重验重造同一诊断框架。

未完成：独立Player冷启动/首次不同页动态字形/atlas成本；罕用字符回退；中英长科技节点与16字动态AI名宽度；不同分辨率正常/减少特效下实际页面比较。提升最小字号会改变布局，是设计选择，未擅改。

## 5. 可复用QA入口与缺口（仅查代码，未运行）

以下路径相对生产根目录；主控串行执行，勿与D1同时启动Unity。不需创建新的大测试框架。

| 现有入口/检查文件 | 可直接覆盖 | 明确缺口/注意 |
|---|---|---|
| `Assets/LocalBuild/Editor/FontBootPreview.cs`，`FontBootPreview.Capture()` / `CaptureAndExit()`，菜单Tools/LingGuang/QA/Font + Boot Preview | 实际字体资源、字重表、固定标题、四阶段开机、不同锐度样张；report记录标题字体及overflow | 离屏样张，无实际交互/动态AI名/长英文/Player性能。旧仿粗体对照使用Noto。 |
| `Assets/LingGuangV05/UI/Tests/Editor/CaseMenuHudTests.cs`，`FixedTitlesFitTheShippedCjkLineBoxAndRenderVisibleCharacters`、`MainTitleLineBoxDoesNotOverlapItsSubheading` | 调用真实HUD Build；虽然传入Desktop CJK，当前Build优先UiSans，标题会用Semibold。检查固定Title/SettingsTitle可见字形、行高、overflow与副标题间距 | 测试名称/说明仍称Desktop CJK，实际已被Build替换；不是全菜单或长动态名字测试。 |
| `Assets/LocalBuild/Editor/DailyFeedbackActualUiRunner.cs:59`，`DailyFeedbackActualUiRunner.Run()`；`DailyFeedbackActualUiChecks.cs:111` Frames | 从干净EditMode建立隔离实际UI，131行轮流中英刷新并采 `daily-forecast-zh/en.png`，使用真实`.exe`字体 | 主要断言内容/风险投影/交互，无全局最小像素字号与英文截断断言；图片须人工审查。当前入口固定GameView口径，不能把样张尺寸当另一尺寸验证。 |
| `Assets/LocalBuild/Editor/NamingDesktopSmoke.cs:38`，`NamingDesktopSmoke.Run()`（可先静态核对场景适用性） | 128–129行已有16字中文名，生成 `04-long-name.png`；73行VerifyIdentity检查桌面、任务栏、窗口、AI资料、YY同步 | 长名只断言字符串/身份一致，无glyph/宽度/截断断言、无最长英文名字场景。`RunFullscreen()`/`RunMonitor()`从step12起，不经过长名步骤，不能用该入口冒称长名已验证。`Validate()`还调用NamingChecks存档验证，字体审查应复用Run而非扩入E1。 |
| `Assets/LingGuangV05/Editor/XgWiringWorkbenchQa.cs`，`SetupFocused()`、`Snapshot()`、`InvariantChecks()` | 现有实际接线页TMP检查：force mesh、text bounds、overflow、truncation；分别输出textOverflow和boundedClipping，可在切英文后的既有fixture复用 | 仅接线页，非全部科技/训练Gear节点。要求Play、useDiskSave=false、TestMode=false；不自动切语言。boundedClipping是供审查信息，不能直接等同bug。 |
| `Assets/LocalBuild/Editor/DesignWallpaperSmoke.Growth.cs`，`DesignWallpaperSmoke.RunGrowth()` | 139/164行对中英能力卡调用GrowthCheckTexts；321行起检查行高、maxAdvance、禁止autosize及fontSize>=12；另检查货架 | 是能力卡/成长货架范围，12为参考字号，不是缩放后屏幕像素保证。可复用相应检查，不必为字体重跑无关整条剧情。 |
| `Assets/LocalBuild/Editor/DesignWallpaperSmoke.FirstMinute.cs`，`RunFirstMinuteEnglishCorrection()`等；`DesignWallpaperSmoke.LessonGoal.cs`的`RunLessonGoal()` | 前者388行已检查上一题反馈fontSize>=13、无overflow、两行内；后者67行检查目标字号>=16、无overflow、一行 | 只覆盖指定提示，不能推导所有小字合格。 |
| `Assets/LingGuangV05/Editor/XgTrainPageLayoutQa.cs`，`Render(...english: true...)` | 已有离屏训练/物品/背包布局工具 | **当前硬编码从Noto源建立新TMP字体，再传给XgUi；直接运行不能验本次MiSans字重/锐度。** 不作为字体验收证据。 |
| `Assets/LingGuangV05/UI/Tests/Editor/XgDataPageLayoutTests.cs` | 1064/1748宽度下数据行短中文、可见字形/字号/截断模式回归 | 当前明确传Desktop CJK给XgUi；只测短中文，不覆盖本次MiSans或长英文。 |

建议下一步复用现有实际UI和命名场景补充检查，而非重新搭建：同一尺寸查看训练Gear/顶部信息与长英文节点；16字中文及合法16字符宽拉丁字母名字（规则见 `Assets/LingGuangV05/Core/AiNamePolicy.cs`，MaxName=16），同时检查窗口标题、侧栏名字、资料入口。先记录真实字体、字号、缩放后的像素与截断/换行，再决定是否提出最小字号/布局设计调整。现有NamingDesktopSmoke可提供长名场景，但其当前断言还不能证明长名排版通过。

## 6. 交付状态

本次确认的是**当前生产文件的静态交付一致性**；此前manifest不同步的问题已关闭。没有新增必须向用户询问的资源问题。新开机样式、720p最小字号取舍、长英文/动态名、独立Player实际表现继续由主控按既定授权分别评审。不把文档中的8/8、147项、DailyFeedback PASS转写为本次新跑结果，也不接Claude第三轮存档/行为任务。
