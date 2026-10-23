# 最终字体与 DreamOS 来源复核（只读）

2026-10-19。以 `font-evidence.json` 的捕获时间和逐文件 SHA256 为准；HEAD `3533141a406940d571e064f3797328c7d214235a`。未修改生产、未编译、未启动/关闭 Unity、未读取或哈希大模型，未搜索项目外私人账号/购买目录。只读取项目与已指定协作资料，并将字体当前字节复制到本目录 `snapshot/`。

**旧 commit-noise-reviewed 报告的 UiSans 整文件恢复建议不再建议执行。** 按本轮“保留有效字形预热”，Numeric 和 UiSans 的新增缓存应保留；配置改动与缓存应分别审查。

## 逐文件结论

下表路径均相对生产工程。`Fonts/` 简写指 `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/`。

| 文件 | HEAD → 当前 | 精确变化 | 建议 |
|---|---|---|---|
| `Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset` | 2,066,511 → 相同 | 字节完全相同，无配置、缓存变化 | 不写、不恢复。旧 status M 不等于有内容差异。 |
| `Fonts/OriginMono SDF.asset` | 6,503 → 6,516 bytes | 唯一变化 `m_FallbackFontAssetTable: []` → 单个 `{fileID: 0}`；纹理/字形等不变 | 运行验收结束且退出后，可只恢复此文件的 HEAD 字节；`.meta` 不动。 |
| `Fonts/OriginSerif SDF.asset` | 37,773,592 → 37,773,605 bytes | 同样只有空 fallback 项；没有字体内容重做 | 同上，只恢复该精确文件。已有完整当前字节备份。 |
| `Fonts/UiV4/Numeric.asset` | 743,706 → 744,393 bytes；字符 100 → 101 | 新增 U+2713 `✓`，glyph 1842；原字符序列完整保留。新增 glyph/character、used/free rect、两个 ligature 记录、同一 512² atlas 的像素；配置和材质不变 | **保留**有效勾号预热，不因 asset 脏或小差异恢复。字形、rect、feature、atlas 需一致，不逐块清理。 |
| `Fonts/UiV4/UiSans.asset` | 9,061,067 → 17,451,958 bytes；字符 1116 → 1382 | 新增 266、删除 0；原字符序列完整保留。新增一个 2048² atlas，原 atlas 像素更新；仅 glyph/character/rect/features/atlas index及引用变化 | **保留**当前有效预热。旧报告是 1381 字的更早快照；当前不是那份字节，不能套旧 hash 恢复。 |
| `Fonts/UiV4/ReadingSerif.asset` | 8,450,202 → 8,453,257 bytes；字符 106 → 106 | HEAD LF、当前 3055 个 CRLF；规范化后字节相同，所有 YAML 文档字段相同 | 无内容差异，不必操作。若最终统一字节，只恢复此文件 HEAD 会保留全部现有预热，但不是必要功能修改。 |

Numeric、UiSans 的 `m_SourceFontFileGUID`、材质、fallback、字体权重表、atlas尺寸/模式、multiAtlas、`m_ClearDynamicDataOnBuild` 与 HEAD 相同。两者 Dynamic=1、multiAtlas=1、clear-on-build=0；保留缓存会进入最终字体资产，不能写“构建会自动清掉所以无影响”。所有顶层字段差异、Unicode码点与纹理字段见 `font-evidence.json`；`*-structural.diff` 省略长像素行，只用于阅读，不能拿作重建字体的 patch。

## 新字重：真实源增量与尚未生成资产分开

新增 `Assets/LingGuangV05/Fonts/UiV4/MiSans-Medium.ttf`、`MiSans-Semibold.ttf` 及 `.meta`，更新 SOURCE_MANIFEST/FONT_CREDITS（源目录和 Resources 副本），以及 XgUiV4Fonts 的两个加载入口、独立 Editor `UiV4FontWeights.cs`，属于真实功能候选，不应当成缓存删除。

本次快照中 `UiSansMedium.asset`、`UiSansSemibold.asset` **尚不存在**，UiSans 的十档 weight table 仍全为空，与 HEAD 相同。新 C# getter 并不等于两个资源已生成。若有后续生成，此报告的 UiSans 配置结论与哈希立即需要更新；新权重引用、两个 `.asset` 及 `.meta` 必须作为同一组提交。不能生成后再整体恢复 UiSans 到 HEAD，那会断开真实字重映射。

## 可恢复的逐文件处理建议（尚未执行）

1. 当前 Unity/C2 继续运行时不恢复任何 asset。等主控验收结束并关闭相关 Editor 后，对每个计划处理的具体路径再次读取/核对 `font-evidence.json` 工作树哈希。若变化，先保留新的独立快照并复审，不覆盖本次 snapshot。
2. 本目录 `snapshot/<工程相对路径>` 已保存当前五份有字节差异的字体及 `.meta`：OriginMono、OriginSerif、Numeric、UiSans、ReadingSerif。原文件保持不动。当前完整 snapshot 是恢复来源；两个纯空 fallback 文件的 HEAD 版本可从 Git 原样导出，不必维护重复 38MB副本。
3. 建议真正恢复的仅 OriginMono/OriginSerif 两个文件。对每个路径单独使用 `git show HEAD:<完整相对路径>` 取得 bytes，再核对其 SHA256 等于 evidence 中 head_sha256，最后写回这一文件。用 Python subprocess bytes / Path.write_bytes，避免 PowerShell 文本重定向改变换行。不要操作整个 Fonts 目录、不要 reset 索引、不改 `.meta`，也不重新跑字体生成器来“清噪声”。
4. 如需撤销上述恢复，先确认工作文件仍为刚恢复的 HEAD hash，再把对应 snapshot 字节写回；如果后续有人生成字重或预热，停止旧快照覆盖。仅恢复明确文件，不回滚其他源代码和资源。
5. Numeric/UiSans 不恢复，保留完整 asset；ReadingSerif 无需动作。对后续新字重资源另做完整快照；提交前再作路径清单与引用校验，最终 Player 构建应使用实际提交的预热与配置。

## UiV4FontWeights.Build 静态审查及入口

已读到主控修正后的版本：移除两处 `fontWeightTable = table` 整体赋值，只改返回数组的元素。当前 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/TMP/TMP_FontAsset.cs:396–402` 的 getter 直接返回 backing array、setter internal，因此该修正有源码依据；本审查未编译确认。

创建流程适合生成缺失的两个动态 TMP 资产：48点、padding5、SDFAA、2048²、multiAtlas；创建材质与atlas子资产，给 regular 的500/600/700档映射Medium/Semibold，两个字重的400档回Regular；已有资产重复运行不替换GUID。`CopyFallbacks` 复制模板列表，保留当前 UiSans 的有效 Desktop CJK GUID `71ded2e539f874a4188d383d44f10375`，不共享可变List。

运行前需要处理/知晓：

- 新字体目前明确写 `m_ClearDynamicDataOnBuild=true`，与原 UiV4生成器及本轮“保留有效字形预热”的 false 不同。建议主控先将新资产设置为 false（或继承模板值）；如新资产此前已创建，当前 Ensure 的 existing 早退不会修改此项，需逐资产核对。不要对现有 UiSans清缓存。
- Build 没有 `TryAddCharacters`，只建动态字体和权重表；**生成成功不等于中文/符号已经预热**。之后通过明确的UI文本覆盖预热并检查字形显示，不能把编辑器动态出现过的字形自动当作Player已验。
- Build复用existing资产时只复制fallback，不验证existing.sourceFontFile。如果当时两个目标已存在，先确认Medium/Semibold的source GUID分别为 `a019dc2b7cc843a1b473e6599331644e` / `f986ce496703486c8a718f8688ee5d04`，与对应.ttf.meta一致；当前缺失目标，所以没有已观察到的错误复用。
- 生成器不检查license文件；项目目前已有源目录及StreamingAssets字体许可副本和更新后的MiSans署名/来源清单。保留这些文件与未修改.ttf，不用生成TMP替代字体许可。Build不删除许可，也不自动补最终发行文档。
- `AssetDatabase.SaveAssets()`会保存所有dirty assets，可能再次序列化运行产生的空fallback与缓存。应先完整备份本表文件和相关元数据，再生成，之后重新比较，不承诺只写两个新文件。

入口建议：待当前 C2/单Editor任务结束后，在 EditMode 调用 `LingGuangV05.EditorTools.UiV4FontWeights.Build()`，菜单 `Tools/LingGuang/Build UiV4 Font Weights`。现有GUI/QA Editor中不要调用 `BuildAndExit`，它无条件请求退出Editor。`BuildAndExit`只适用于主控明确安排的独占batch进程，不能与当前验收并行。本代理没有运行任何入口。

## DreamOS：现有来源记录不等于团队购买授权证明

已找到并读取：

- `Assets/DreamOS - Complete OS UI/Read Me.txt`：版本3.2.4，Michsky文档/支持链接、Remix Icon及多位音乐/照片作者致谢。它是来源/归属记录，正文没有团队账户、订单、许可范围或授权凭证。
- `Assets/DreamOS - Complete OS UI/Editor/Scripts/InitDreamOS.cs:16` 有通用安装提示“Thank you for purchasing DreamOS.”，是所有安装者都能得到的产品文本，不能作为此团队购买证据。
- `Documentation/DreamOS-Lucide/README.md` 和 LICENSE.txt：328个替换PNG使用Lucide，ISC及适用Feather MIT声明；44个原PNG保留。该许可覆盖Lucide/Feather图标，**不是DreamOS整个UI框架许可**。
- `发布材料/2026-10-18-素材声明/核对依据与待补信息.md`、`素材使用声明-按最新清单填写.md` 已把DreamOS团队购买/授权列为待确认；D3 `许可映射表.md` 也没有提供新授权记录。

因此可修正D3“快照仅有WindowManager、无任何来源资料”的表述：正式项目确实已有官方Read Me与图标来源/许可。**但在上述限定检查范围内，未找到能把团队使用/发行权限确认为已完成的DreamOS购买或授权依据。** 不能把Read Me、通用安装致谢、Lucide许可当成框架授权，也不能据此宣称用户没有购买。主控保留该待确认项；本代理未翻查私人购买目录/账号、未联系外部人员或执行发布。
