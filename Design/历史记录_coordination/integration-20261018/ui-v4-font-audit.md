# UI v4 字体只读准备

范围仅 competition-latest-20261016 的 Assets、Design（含 Design/ArtSources）、Documentation。未扫描系统/私有字体目录、未下载、未启动Unity、未改主项目。

## 已有 / 缺项

- 需求源：`Design/ui/exe_UI_v4_交接_给GPT.md:42`：UI HarmonyOS Sans SC首选或MiSans，阅读正文Source Han Serif SC，数字Inter或DIN风格、等宽数字。
- HarmonyOS Sans SC / MiSans：源字体、对应许可证与TMP资产均未发现。
- Inter：源字体、许可证与TMP资产未发现。项目Roboto/Rajdhani不是Inter，也不自动视为符合DIN替代要求。
- 思源宋体：已有 `Assets/LingGuangV05/Localization/Fonts/OriginSerif-SourceHanSerifSC-Bold.otf` 25,521,732字节（约24.34MiB）；同目录 `OriginSerif-LICENSE-OFL.txt` 含Adobe版权、Reserved Font Name Source和完整SIL OFL1.1。这是开场粗体字体，不适合作为长篇正文已完成的证明；仍缺适合正文的Regular字重与对应分发来源记录。
- 已有 `Resources/LingGuangV05/Fonts/OriginSerif SDF.asset`（约36.02MiB源YAML）与OriginMono SDF.asset（6,759字节）。Serif为动态1024图集，clearDynamicDataOnBuild=1；YAML大小不是最终构建包体。
- 当前默认CJK是 `Assets/Emergence/Resources/Fonts/NotoSansCJKsc-Regular.otf`，16,437,364字节（15.68MiB），同目录OFL.txt存在，但文本开头没有具体版权人行；保留作fallback前宜补齐该实际字体版本的上游版权/来源信息，不能拿其他字体OFL文件替代。
- `Design/ArtSources` 当前只有draw_original_memes.py，没有字体或字体许可证。项目根目录没有ArtSources目录。

## 可复用生成 / 运行接入

- `Assets/LingGuangV05/Editor/OriginLiveFonts.cs:21` Build：生成Resources TMP FontAsset并保存material/atlas子资产，SDFAA48、padding20、1024、Dynamic、多图集、clearDynamicDataOnBuild。可借结构，正文不必沿用开场发光所用20px padding；另做幂等生成器，避免DeleteAsset改变GUID。
- `Editor/DesktopLocalizationInstaller.cs:133` BuildFont：Noto48/padding5/2048多图集，从本地化文本预填字符；之后将fallback挂到主题字体。可借预填字符与fallback流程，但不要运行整个安装器顺带重建桌面资源。
- `DesktopBridge/Story/PrologueDesk.cs:123` CjkFont：每会话运行时从Resources Noto创建1024动态多图集。NativeAppFontFallback、ChapterOneNativeChatAdapter也各创建fallback；v4应集中缓存共享字体，避免每窗口重复图集。
- `DesktopBridge/Story/OriginLive/OriginLiveText.cs` 优先bundled Serif/RobotoMono，缺失时有OS字体路径fallback；新v4字体不要依赖OS字体。无需顺带改变已验收的开场字体。

## 最小打包方案

1. 从官方发行版取得一款中文UI Regular（HarmonyOS首选或MiSans择一，不两套并装）、SourceHanSerifSC-Regular、Inter Regular；有明确标题字重需要再加一个Medium/Semibold。选静态TTF/OTF，避免未经Unity验证的variable font。每家源文件与完整版本版权/许可放同目录；附来源URL、版本和哈希，许可证复制到发布包第三方说明。
2. 生成3个共享TMP角色资产（UiSans/ReadingSerif/Numeric），固定GUID、显式源字体引用、材质和图集子资产。UI预烘本次可见固定文案；自由AI/玩家名走同字体动态fallback、少见字再到现有Noto。阅读仅在打开阅读窗时加载，不给全场所有TMP挂宋体。
3. Inter不默认保证数字等宽。实际TMP/字形度量验证0–9等advance；若所用版本不能应用tnum，就对纯数字小控件用固定mspace宽度，并验证小数点/负号/百分比/货币符号，无需整句强制等宽。
4. 抽样验中文/英文/生僻玩家名/自由回复/数字，报告缺字，不静默删字。验证不同字号与小窗换行；检查BuildReport依赖确认源字体和许可进包，脱离开发机字体环境启动核对。不要通过全场Find遍历逐帧换字体。

## 性能 / 包体

- 全量CJK OTF通常十几至几十MiB，上述已有字体可作数量级参考，新字体未取得不能给精确新增MB。Dynamic不会免掉源字体包体；Resources内资产即使当前没显示也会进入构建，勿多拷贝同源。
- 1024² Alpha8图集约1MiB/页，2048²约4MiB/页；若实际纹理为RGBA32则分别4/16MiB，另有CPU可读副本、mip与驱动开销。多atlas运行时按需增长，需用实测峰值而非文件大小代替显存。
- 全动态首次遇字会有栅格化、atlas上传和TMP重建开销；固定UI预烘/启动空闲预热能减小首开卡顿。动态fallback只生成未覆盖字，适当控制字体角色与字重数量；不同字体材质/atlas会增draw call。新资源增加后重新量首开教页/阅读/YY自由文本，不能只看编译。
