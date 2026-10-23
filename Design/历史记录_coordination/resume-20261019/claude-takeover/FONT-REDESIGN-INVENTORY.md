# 字体重设计只读盘点 — 2026-10-19

**范围更新：用户已取消全局字体重设计，主标题原字体也保持。本报告仅作为已完成授权/现状审计留档；第7节建议不再是执行计划，不应用到生产。此次没有修改任何生产字体、资源或代码。**

本报告用于当前档案/脑窗QA之后的全局字体重设计，重点是暗色、精致的 `.exe`。生产C#冻结期间只读检查；没有编辑生产、下载、启动Unity、运行生成器或字体测试。仅生成本工程外报告。下文文件路径以 `competition-latest-20261016/` 为生产根；外部目录另行标明。字体版本取实际name表，hash取实际字节，许可取实际文件；旧交接只作为索引。

## 结论与建议方向

现有可商用素材足以支撑第一轮重设计，优先整理角色、字号、行高、字重和缩放，不必再增加字体族。建议主 `.exe`、脑窗及性格档案统一 **MiSans Regular / Medium / Semibold**；动态数值用 **Inter Regular**；书籍正文保留 **Source Han Serif SC Regular**；2016年桌面、YY、贴吧、浏览器、邮件仍保留各自原生字体和Noto CJK补字。苹果式克制和精致来自层次、间距和布局；无需引入未核验授权的Apple系统字体。

最需要处理的真实差异：脑窗仍走 `PrologueDesk` 的动态Noto，与主 `.exe` MiSans分离；训练页有10px自动缩小下限；Inter原生数字不等宽；中文动态字重预热在最近新增文案后已不完全同步；冷启动/Noto补字及长英文/自定义名字仍有实际验收缺口。

## 1. 实际源字体、版本、许可

| 实际路径（Assets下） | 实际name表 / 大小 | 本地许可证据与适用范围 |
|---|---|---|
| `LingGuangV05/Fonts/UiV4/MiSans-Regular.ttf` | MiSans Regular，Version 4.009；8,122,324字节；SHA256 `9c120f0a849bc0aa5048daae2a3c0f6eecd828b5b33fce682a9622833f5feea6` | 同目录`MiSans-license.pdf`，小米专有免费许可，**不是OFL** |
| 同目录 `MiSans-Medium.ttf` | MiSans Medium，4.009；8,103,840字节；`b03e98374e971594b0b7a9706d0704241f76e1b88556cdda79c5039ef8a638d1` | 同MiSans协议 |
| 同目录 `MiSans-Semibold.ttf` | MiSans Semibold，4.009；8,034,980字节；`77c23f31ae124867778344970155a0c8d34a89897dedaab81aeee82ff00a4ce6` | 同MiSans协议 |
| 同目录 `Inter-Regular.ttf` | 实际name为Version **4.001;git-9221beed3**；官方发行归档名4.1；411,640字节；`40d692fce188e4471e2b3cba937be967878f631ad3ebbbdcd587687c7ebe0c82` | 同目录`Inter-LICENSE.txt`：Copyright 2016 The Inter Project Authors，SIL OFL1.1 |
| 同目录 `SourceHanSerifSC-Regular.otf` | Version2.003；来源标签2.003R；24,543,332字节；`78aa7a328fd974df2d688c8a9fd74a33d8334dfa84ab24d9d11efb2ffc464117` | `SourceHanSerif-LICENSE.txt`，SIL OFL1.1，Reserved Font Name `Source`。许可原文版权年2017–2022，字体内版权年2017–2024；保留原件，不擅自改年份 |
| `Emergence/Resources/Fonts/NotoSansCJKsc-Regular.otf` | Noto Sans CJK SC Regular，Version2.004；16,437,364字节；`2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b` | 同目录`OFL.txt`完整SIL OFL1.1；字体name表也明确OFL。现有桌面CJK来源 |
| `LingGuangV05/Localization/Fonts/OriginSerif-SourceHanSerifSC-Bold.otf` | Source Han Serif SC Bold，2.003；25,521,732字节；`706b8c0de2deff6cbc0c87e2cdedfd33a78b7ffd76cebb4549012f197ba611fe` | 同目录`OriginSerif-LICENSE-OFL.txt`，OFL1.1、保留名Source；开场影片角色 |
| `Feel/NiceVibrations/Demo/Common/Fonts/RobotoMono-Medium.ttf` | 家族名Roboto Mono Medium，style Regular；Version2.000985;2015；109,292字节 | 字体内name13明确Apache License2.0、name14指向原文URL。`OriginFonts.Mono`实际来源；不是Inter。最终随包署名应明确覆盖该实际字体，不仅笼统附Apache文本 |
| `DreamOS - Complete OS UI/Fonts/OpenSans-Regular.ttf` | Open Sans Regular，Version1.10；217,360字节 | name13明确Apache License2.0；现有原生桌面体系来源之一。本轮不替换 |

另有TMP LiberationSans及示例字体、DreamOS LCR.ttf、Reach Rajdhani等第三方资产。它们的存在不代表全部实际被游戏引用。此次不是所有插件字体的完整许可证审计，不将DreamOS中LCR等自动归入OFL/Apache。已有用户DreamOS授权不应重新询问或被旧审查中的“待确认”覆盖。

### MiSans条款：已重新读取实际PDF

生产源目录和 `Assets/StreamingAssets/Licenses/Fonts/MiSans-license.pdf` 均79535字节、4页、SHA256 `4a93a27cd2bd81b3b5ecfd0a853144a876fa26938a93a68443c67d74172fcb86`。用本机PyMuPDF直接提取生产PDF；不是仅信任旧提取文本。第2条原文包括：

> “您应在软件中特别注明使用了 MiSans 字体。”
>
> “您不得对 MiSans 字体或其任何单独组件进行改编或二次开发。”
>
> “如您使用 MiSans 字体创作宣传素材、logo、应用App等，您有权分发或出售该作品。”
>
> “您应在 MiSans 字体的任何副本中保留版权声明和本协议。”

因此现有未改源字节、游戏内注明、随包原协议的使用路径符合本地许可文本描述的App发行范围；不能把“免费商用”扩写成可改字形/子集化/独立分发字体。TMP仅作内部显示资产，不制作新的MiSans字体文件。

OFL实际原文允许字体随软件bundled/embedded/distributed/sold，要求保留版权与许可、不单售字体；改版须遵守保留名。它不会把游戏源码改为OFL。

## 2. 来源与交付完整性：当前状态取代旧缺口

真实staging路径是 **`.coordination/integration-20261018/ui-v4-font-stage/`**，不是顶层`.coordination/ui-v4-font-stage/`。包含原MiSans.zip、Inter-4.1.zip、源字体、原许可、下载清单及官方网页缓存。此次没有联网重验官方站点，但已把生产MiSans三字重与本地既存官方来源ZIP中的`MiSans/ttf/`逐字节对比，全部一致。

生产`Fonts/UiV4/SOURCE_MANIFEST.json`的8项文件大小/hash全部与实际一致。三处`FONT_CREDITS.txt`（源目录、Resources目录、StreamingAssets目录）字节相同，hash `a43a7a62a7aa281e882b7c4263fb8faf4dbd78ea97e1946f4a512ccf896076c9`；源目录和StreamingAssets的manifest相同，hash `c01402ee13892c3fd00ab82455c84f5469e4fed151d0435483904b8d89f3bff3`。都包含新增Medium/Semibold各自条目。Resources按设计没有第三份manifest。

游戏内署名实际接在 `Assets/LingGuangV05/DesktopBridge/Bodu/BoduView.cs:294`，显示MiSans说明并读取Resources FONT_CREDITS。随包Fonts目录当前确有MiSans PDF、Inter/OFL、SourceHan/OFL、credits、manifest。旧`release-package-final-candidate/FONT-HANDOFF.md`写的“StreamingAssets尚缺新增字重”已过时，不能重新列缺。

Claude交付来源已读 `Design/字体与开机页改版_Claude_20261019.md`、`Design/Codex_字体交接与全局评审_20261019.md`，并核对实际资产和代码。`ClaudeReturn*`各目录按字体文件名检索没有另一个同名字体包；实际新增字重和生成器已在生产，不能再当“未交付候选”。外部`font-final-review/DELIVERY-AUDIT.md`可作先前核验记录，当前计数有后续运行增长，以下按本次快照。

## 3. 实际角色和加载链

| 范围 | 当前来源与回退 | 设计处置建议 |
|---|---|---|
| 主 `.exe`、性格档案 | `XingGuangView.cs:116` 显式`new XgUi(XgUiV4Fonts.Sans ?? fontAsset, ..., true)`；`XgUi.Text`继承该font。档案标题21、正文15、轴13、图例12 | 延续显式域内角色；统一常规400、导航/控件500、小标题600，正文不全粗 |
| `.exe`顶栏金额/时钟/收入/参数/样本 | `XingGuangView.Frame.cs:61,103`显式Numeric、清fontFeatures；显示值套FixedDigits | Inter只用于稳定数据列，单位仍走Sans；不要整篇正文数字一律等宽 |
| 阅读窗口书籍正文 | `XgReadingWindow.cs:109`显式ReadingSerif，19px；反应/按钮仍走其它默认角色 | 保留衬线作为阅读/叙事差异；辅助文案和按钮回UI Sans |
| 游戏标题HUD | `CaseMenuHud.cs:66`优先UiSans，再传入font/TMP默认；标题显式Semibold、42–66自动字号、字距3 | 沿用品牌字体，单独检查长英文与小窗口，不沿用巨大字符间距到正文 |
| 开机屏 | `CaseMenuBootScreen.cs:59`UiSans、Semibold、Numeric；BIOS用OriginMono | 字体与开机动效分开评审，不因字体调整重做开机流程 |
| **脑窗** | `XgBrainWindow`的标题/按钮/字幕均`desk.Text`；`PrologueDirector.cs:102`用`PrologueDesk.CjkFont()`创建desk，因此实际为动态Noto CJK，非MiSans。标题13、按钮11、字幕13且允许缩到9 | 这是`.exe`家族尚未统一的明确接线点；仅脑窗显式换UI角色，不修改PrologueDesk全局字体 |
| 贴吧/浏览器/故事桌面等 | TiebaView明确`PrologueDesk.CjkFont()`；PrologueDesk由Noto原件创建1024动态多图集。YY从桌面shop传入原生font，部分native adapters再加Noto补字 | 保留2016应用风格，不把XgUi构造器或TMP全局默认改成MiSans |
| 原生应用补字 | `NativeAppFontFallback`克隆原TMP字体，保留原材质与主字形，尾部增加自有动态Noto；类似逻辑也在NativeChatAdapter。对象Dispose释放自有资源 | 保留原生字体，仅补缺字；关注多个动态Noto实例重复成本 |
| 开场影片Origin | Resources的OriginSerif/OriginMono优先；Editor可从源构建；缺失则尝试系统Songti/SimSun或Menlo/Consolas，最终Noto | 保留影片角色。跨机一致性应以随包字体为准，不把开发机系统字体当稳定依赖 |

全局 `Assets/TextMesh Pro/Resources/TMP Settings.asset` 默认GUID仍解析为 **LiberationSans SDF**；global fallback表为空、missingGlyphCharacter=0。没有将默认字体改为MiSans。不能依赖全局默认解决任意中文。

## 4. TMP资产与生成器实况

路径：`Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/`。

| 角色 | 源字体 | 栅格与缓存 | 本次序列化快照 |
|---|---|---|---|
| UiSans | MiSans Regular，source GUID `75f62267335fcdb4daa0aff166dca296` | 48pt/padding5，2048² Dynamic多图集，clearOnBuild=0，Sharpness=.5 | 1402字符，2张2048图集 |
| UiSansMedium | MiSans Medium，`a019dc2b7cc843a1b473e6599331644e` | 同上 | 1394字符，2张 |
| UiSansSemibold | MiSans Semibold，`f986ce496703486c8a718f8688ee5d04` | 同上 | 1393字符，2张 |
| Numeric | Inter Regular | 48pt/padding5，512² Dynamic多图集，clearOnBuild=0，Sharpness0 | 101字符，1张 |
| ReadingSerif | SourceHanSerif Regular | 48pt/padding5，2048² Dynamic多图集，clearOnBuild=0，Sharpness0 | 106字符，1张 |

Sans三字重fallback均为`Localization/Fonts/Desktop CJK.asset`，即Noto2.004；ReadingSerif→UiSans→Desktop CJK；Numeric→UiSans→Desktop CJK。没有相互循环。UiSans字重500指Medium、600/700指Semibold；heavier字重表回指Regular/Medium/Semibold。无真实italic资源；不应认为任意斜体也有原字形。

`UiV4FontAssets.cs`只创建三角色，保留源GUID并拒绝不同源悄悄替换，预热小Probe和ASCII、检查符号与mspace等宽；动态缓存不在构建时清除。`UiV4FontWeights.cs`补真实字重、预热Regular已有字符、复制fallback、clearOnBuild=false、原地设置权重表和Sharpness=.5；两项旧编译修正仍在。**但Ensure遇existing仅复制fallback，不核验/更换source或尺寸**，以后换源要显式检查，不能运行一次生成器就认为成功切换。两个工具最终调用全局`AssetDatabase.SaveAssets()`，可能保存其它dirty资产，运行前需由主控确定边界。

`OriginLiveFonts`另一路生成器会DeleteAsset再建资产，clearOnBuild=true；不应在当前重设计中顺手运行。当前OriginSerif序列化文件约37.8MB、19个Texture2D子对象，但活动atlas引用只有1个且字符表空；这是待审的历史子资产/缓存状态，**不能把19个子对象直接报告成19张运行时活动图集或内存泄漏**。Desktop CJK当前字符表也为空，clearOnBuild=true、动态2048；OriginMono为空、动态1024、clearOnBuild=true。首次使用可能需要重新生成。

## 5. 实测静态缺字与数字风险

已用fontTools读取实际cmap。Probe包含中英数值、¥￥、−×✓✕▶箭头、省略号、希腊字母、罕见姓名字𠮷、emoji：

- MiSans三字重都缺原生 `▶`、`𠮷`、emoji；Noto支持前两者，因此当前链有明确的原件覆盖。回退到Noto时可能字重/基线不同，尤其Semibold→Regular Noto，须实渲染核对。
- Inter原件缺全角`￥`、中文与𠮷，经Sans/Noto补；有`¥`。Inter本身不是等宽：0的advance1292、1为833、4为1323，不能把“使用Inter”写成“数字天然等宽”。当前`FixedDigits`使用`<mspace=0.65em>`包装数字及`. , : + - − /`；如果原字符串已有`<`或`>`，直接原文返回，可能失去等宽。Inter有tnum特性，但项目当前清fontFeatures并没有靠tnum实现。
- `✕`在Inter、Noto和SourceHan中缺，而MiSans存在；`×`覆盖更稳定。字符不能凭外形混用。
- 本次Regular比Medium/Semibold多预热9字符：全角空格、修、呀、愿、砚、累、踩、轨、迹；这些是**未预热**而非字体源缺字。重字体首次出现仍会动态栅格化。旧审查“所有字重覆盖Regular完全一致”对它当时成立，不能照抄为当前事实。
- 所检主要字体都不含emoji `😀`；全局TMP fallback为空，不能宣称玩家任意Unicode名字/模型输出都不缺字。生僻字、表情、组合符、繁体/日文、数学符号应以真实渲染样本分层处理，避免靠缩小字号掩盖。

## 6. 性能和可读性风险

1. 当前UiV4五个源字体合计49,216,116字节（约46.94MiB），新增两个MiSans字重16,138,820字节；不是压缩后Player包体数字。MiSans三张角色各2张Alpha8 2048²，若全部活动常驻，纹理原始量约24MiB；另加Reading4MiB、Numeric0.25MiB及Noto/其它字形。必须用实际Player内存/BuildReport验证，不能把源码/序列化文件大小当运行内存。
2. 多图集没有固定页数上限，阅读、自由模型输出、玩家名字持续加字，TMP可能生成新的纹理/材质；fallback还可能新增子网格和绘制批次。原生应用各自建Noto也会重复栅格化，不能简单把所有回退换成统一大字库后宣称更快。
3. 编辑器已暖缓存不能替代冷Player。需分别观察首次启动、首次科技树、切英语、首次长书、自由名字/回答的缺字和长帧；保留当前有价值预热，采用明确文本集，不把整个字库一次预热。
4. `XgTrainPage.cs` summary/numbers/hint/risk最小字体10；脑字幕最小9。720p缩放后会更小，锐度.5只能改变边缘，不增加可读像素。首要是布局与最小可见字号，不能用普遍加粗/锐度或无限autosize代替。
5. 新角色的英文长文本、混合币种单位、动态名字宽度、行高基线均可能不同；原桌面Noto线框约1.45em相关测试不代表MiSans新角色一律适配。当前源码有202个Bold/`<b>`匹配（190行/53文件），只是匹配统计，不等于202个独立控件。

## 7. 建议的首轮字体规范

以下为待实现/视觉比较的建议，不是已通过的参数：

| 角色 | 建议字体/字重 | 排版原则 |
|---|---|---|
| `.exe`正文、题目、解释、档案原因 | MiSans Regular400 | 屏幕实显主体目标14–16px，解释正文16–18px；行距约1.4–1.55，按CJK实际line box确认；不自动缩到10px |
| 导航、按钮、短标签 | MiSans Medium500 | 13–15px实显范围；层次来自权重与空间，不全部Bold；中英文同一角色 |
| 页面标题、当前AI名、脑窗标题 | MiSans Semibold600 | 大标题20–24px、辅助窗口标题14–16px作为比较起点；长名省略/完整tooltip，不挤压其它控件；中文不普遍加字距 |
| 变化数值/金额/计时/图表读数 | Inter Regular +明确等宽数字策略 | 数据对齐，单位保持比例宽；仅数据列应用，测试已含rich text的调用；无理由不全局换成等宽字体 |
| 书籍/故事长阅读 | Source Han Serif SC Regular | 19–21px作为比较起点，舒适列宽、稍宽行距；UI说明仍用Sans |
| 2016其它应用 | 维持当前原生OpenSans等+Noto CJK | 只修缺字、截断、基线和低分辨率可读性，不整体套`.exe`现代风格 |
| BIOS/技术日志、开场影片 | 现有Roboto Mono/SourceHan Bold | 限于叙事/代码角色，保留随包来源，不扩散到中文功能正文 |

实现时宜在`.exe`域提供TextRole/尺寸与字重入口，逐处迁移关键页；不要在通用`XgUi`或TMP Settings里无条件换font。脑窗可显式指定同域角色，PrologueDesk保持其它应用不变。将颜色/层级/字重一起比较，尤其暗底的次要文字不要通过更细更灰再缩小来降低存在感。

## 8. 下一步验证与许可收尾

先做同内容、同窗口/缩放的中英样张矩阵：题卡、训练页、科技树长节点、档案、脑窗、标题、书籍；加入长自定义AI名/𠮷/货币/数学符号。核对基线、完整行框、截断意图、实际被选字体/材质和回退字符，再安排冷Player/性能。此报告未亲看新增样张，不把Claude文档列出的截图/8项测试自动升级为新重设计通过。

字体交付与开机图案分开。继续保留源字体原字节/许可/来源清单；若重新选OFL字体，须核对实际下载版本及许可后再纳入。现有包装候选会拷Noto/OriginSerif/Liberation许可和Apache文本；其FONT-DEPENDENCIES列OpenSans、Noto等，**没有明确列本次核实在OriginMono使用的RobotoMono-Medium**，建议最终包检补实际来源/版权说明，不能仅凭“Apache文本存在”声称各字体署名均齐。打包候选不等于最终包已经生成。

本轮无字体新下载、无生成器执行、无产品修改或运行验收。已核验商业使用文本与现有素材，推荐先基于这些资产完成清晰、统一的`.exe`排版，再决定是否需要新增字体族。
