# 提交前源增量噪声清单

2026-10-19，只读快照；HEAD `3533141a406940d571e064f3797328c7d214235a`。没有执行reset/checkout/restore、索引刷新或任何生产文件写入。正在运行的Unity与其他代理仍可能改变这些文件，实际恢复/提交前由主控复核本包hash。

## 精确处理建议

|路径（相对工程）|实际差异|建议|
|---|---|---|
|`Packages/Aura/.gitignore`|工作树、index、HEAD三者字节完全相同，75字节、LF；不是忽略规则修改，也不是实际换行差异|无需恢复内容，无须纳入功能提交；status的M与内容差异不一致，最终暂存/状态刷新后再查即可。不要为它递归操作Aura目录。|
|`Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset`|工作树、index、HEAD字节完全相同，2,066,511字节、LF|无需恢复，无实际字体修改可提交。|
|`Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginMono SDF.asset`|唯一差异为`m_FallbackFontAssetTable: []`变成单个`{fileID: 0}`空引用；增加13字节，其他文档与字段不变|可恢复该文件到HEAD，排除本次空运行时fallback残留。不要改源码中的有效fallback处理。|
|`Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginSerif SDF.asset`|同上，唯一空fallback项；原37,773,592字节，当前多13字节|可恢复该文件到HEAD。不是37MB字体整体重做。|
|`Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/UiSans.asset`|新增一个2048×2048 atlas、更新原atlas像素，字符1116→1381（新增265、删除0）；仅glyph/character/font-feature/used-free rect/atlas index与引用表变化。材质、sourceFont GUID、有效fallback、尺寸和动态配置不变|建议排除此轮运行产生的动态缓存，恢复精确HEAD版本。HEAD已有1116字预热缓存，应保留该基线；不要清空字体或改m_ClearDynamicDataOnBuild。|
|`.vsconfig`（untracked）|只含VisualStudio ManagedGame工作负载建议|本次功能提交可不暂存；保留本地文件，无须删除或修改根gitignore。|

UiSans当前从9,061,067增长到17,451,427字节。`m_AtlasPopulationMode=1`、multiAtlas=1、`m_ClearDynamicDataOnBuild=0`在HEAD与当前都一致；因此恢复缓存会改变打包的预热字形集合，而非“构建时自动清除所以无影响”。它仍可动态生成新增字，现有字体源与fallback不变。按既有提交记录排除动态字体缓存是合理的，但新的最终Player烟测应基于恢复后的实际构建；不把此前带暖缓存的Editor时序当成恢复后冷字形性能证明。

`OriginLiveText.cs:98`已有删除失效fallback再接入有效CJK的逻辑，符合空引用残留判断。`UiV4FontAssets.cs`明确创建动态字体并保留预热数据；本审查没有重新运行字体生成器。

## 恢复时的范围与顺序（仅建议，未执行）

1. 等当前Unity验收完全退出；先确认上表三个有内容噪声的asset当前hash仍匹配`noise-evidence.json`，若不匹配则重新看差异。
2. 主控只对这三个精确路径恢复HEAD内容，保留原`.meta`。需要逐字节保持LF时，可用Python `subprocess.check_output(['git','show','HEAD:'+path])`配合`Path(path).write_bytes(...)`，不要用PowerShell文本重定向去导出资产；不要整目录checkout/reset或运行字体重新生成。
3. Aura/Desktop CJK不需要写入；`.vsconfig`只排除暂存。再次检查差异，然后生成最终构建的source-provenance。

## 必须保留为真实改动的范围

`changed-file-inventory.json`逐路径列出本次内容变化59个已跟踪文件及92个未跟踪文件。除上述3个缓存asset和本地`.vsconfig`外，均保留供正常功能审阅；此清单不是每项功能已验收或批准发布的声明。

- Casino/Blackjack：`XgBlackjackRules.cs`有真实入口门禁、BeginGambling/WatchGambling和冻结后禁止double，不能按“概率算法没改”误删整个文件。
- Guide：生产`XgGuideTests.cs`已经有终局引导的新8行测试，不能恢复整文件。新“已安装批次”修复必须使用独立小hunk，在现有修改上叠加。
- `LocalLlm.cs`当前diff只有默认模型注释从Qwen3.5修正为Qwen3，这是描述纠正，可保留；不要将其当作本轮改了推理并发算法。
- 图鉴、群预设生命周期、新闻短评/RAG、私人结局/准备、桌游显示/提示、888逻辑及其测试、素材及credits、设计/整合文档属于实质交付。不要根据文件多、是JSON/meta或由QA编辑就批量排除。
- 新图片及`.meta`必须与对应Resources路径、原始素材来源/credits一同保留；本清单没有重新认证素材许可或图像质量。

## 证据

- `noise-evidence.json`：5个目标文件HEAD/工作树SHA256、字节长度、LF/CRLF、index一致性及字体字段差异。
- `uisans-cache-details.json`：字符集合差异及新增/变化YAML文档；不输出大段纹理像素。
- `changed-file-inventory.json`：逐路径当前变化分类，非内容变更的Aura/Desktop CJK不在git diff内容列表中。

Guide补丁现已重新按LF字节生成，普通`git apply --check`通过，SHA256为`4a92582c385a8bd202bc75ef68bd2b309a7b81fe2022ad89b03e4b1d7dfe22ea`。详见相邻`guide-baseline-reviewed/REVIEW.md`。先前是补丁CRLF而非生产CRLF，已更正并保留原补丁记录。
