# 统一图鉴恢复候选报告（2026-10-19）

状态：可以主控审阅并合入，尚未Unity/UI验收。只修改本候选目录；未修改生产、启动Unity、提交或推送。

基准HEAD：`3533141a406940d571e064f3797328c7d214235a`。3个共享文件的字节哈希与候选baseline完全一致；未覆盖生产脏改动。

## 本轮修改

- 保留已批准三类图鉴、科技/名字双入口、train.weights/TrainPart旧档兼容与真实HasAbility双门禁。没有新费用、存档字段或人物披露。
- 图鉴Current增加active与有效绑定检查，关闭后保留的旧回调不能重播；图鉴自己订阅/解绑原生onMinimize/onClose，立即停止tiny/sequence，不等下一帧。StageCurtain源码、队列、升阶字幕和奖励逻辑未改。
- 核心检查从72增加到80：空档、权限撤销、null旧能力列表、双语未收藏/未购结构内容封印。
- 新增独立UI runner与边界检查，原全量检查仍保留。实际LCD点击覆盖已编码的未购双入口、定位已有科技、真实扣款购买、未学能力、原生最小化/关闭、切档及故事独占。边界代码已编译但未执行。
- 切档为生产BindSave内存重绑；stage4幕布使用明确已学能力/待播队列夹具，并检查原顺序及完整lab状态回到排队前。不能称自然流程或磁盘切档。

## 精确交付文件

更新已有生产文件：
- `Assets/LingGuangV05/XingGuang/Desktop/XgStageSequenceDemo.cs`；基准SHA256 `6c816ac47d5885b5988d25897a61ff0591ac8234d48c8e015bc03ad105e900cc`。
- `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs`；基准SHA256 `653b6ac5a2766d825580aee3f754eb1d5c8ad34a18b516afc8edb922fba8820f`。
- `Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Profile.cs`；基准SHA256 `07770ab68129b8b9966ff1f254887c919ab3f8739da028c9d23a0df63d6e9a04`。

新增生产文件（每个.cs对应同名.cs.meta，均在patch内）：
- `Assets/LingGuangV05/XingGuang/Core/XgUnifiedAtlasCatalog.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgAtlasTinyDemo.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgUnifiedAtlas.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Atlas.cs`
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgUnifiedAtlasChecks.cs`
- `Assets/LocalBuild/Editor/UnifiedAtlasActualUiChecks.Boundaries.cs`
- `Assets/LocalBuild/Editor/UnifiedAtlasActualUiChecks.cs`
- `Assets/LocalBuild/Editor/UnifiedAtlasActualUiRunner.cs`

共19个patch路径（3个已有CS、8个新增CS及8个meta）。每个文件的完整候选哈希和字节数见 `manifest.json`。本次相对原候选新增的是 `UnifiedAtlasActualUiChecks.Boundaries.cs`、`UnifiedAtlasActualUiRunner.cs` 及各自meta；另外更新 `XgUnifiedAtlas.cs`、`XgUnifiedAtlasChecks.cs`、`UnifiedAtlasActualUiChecks.cs`。

## 可应用补丁

- `unified-atlas.patch` SHA256：`731cf8a04773b0b5ec6d54020b0c5d94a99eac48cb65272595deba93d0da5832`。包含完整统一图鉴候选，替换原候选补丁，不是叠加补丁。
- 原补丁保存在 `verification/unified-atlas-before-resume.patch`；不要再重复应用。
- 在生产根目录执行 `git apply --check ../.coordination/resume-20261019/atlas-unified/unified-atlas.patch` 已通过。主控合入前应再核对3个基准哈希；若变化，逐段合并。

## 已执行命令和证据

以下命令从games目录运行：
```powershell
python .coordination/resume-20261019/atlas-unified/verification/prepare_compile.py
dotnet build .coordination/resume-20261019/atlas-unified/verification/LingGuangV05.XingGuang.Core.csproj --no-restore --verbosity quiet
dotnet build .coordination/resume-20261019/atlas-unified/verification/Assembly-CSharp.csproj --no-restore --verbosity quiet
dotnet build .coordination/resume-20261019/atlas-unified/verification/Assembly-CSharp-Editor.csproj --no-restore --verbosity quiet
dotnet run --project .coordination/resume-20261019/atlas-unified/verification/AtlasChecks.csproj --no-restore
python .coordination/resume-20261019/atlas-unified/package_candidate.py
```

- Core：0错误、3既有警告，`verification/resume-core-build.log`。
- Desktop：0错误、538既有警告，`verification/resume-desktop-build.log`。
- Editor：0错误、241警告，`verification/resume-editor-build-final.log`；候选代码无新增警告。此批生产新新闻辅助纳入后警告总数与旧237不同，不冒称同一源码基线。
- 核心80/80通过：`verification/resume-core-checks.log`，使用.NET serializer与实际Core程序集，尚非Unity JsonUtility。
- 静态24项及git apply --check通过：`validation.json`，检查门禁、无保存/购买/训练/奖励写调用、独立演示实例、未改StageCurtain。
- 首次Editor编译失败保留 `verification/resume-editor-build.log`：共享生产SidebarLayoutChecks已引用新闻辅助，但旧生成csproj未收录其新文件，6个CS0103；仅在隔离生成器补充实际生产LocalBuild/Editor新源码后成功。没有排除失败代码或减弱产品门禁。

## 主控下一步运行入口

- Unity EditMode TestFilter精确类名：`LingGuangV05.XingGuang.Tests.XgUnifiedAtlasChecks`。精确测试名：`LingGuangV05.XingGuang.Tests.XgUnifiedAtlasChecks.UnityRoundTripAndReadOnlyReplay`。
- 独立UI executeMethod：`UnifiedAtlasActualUiRunner.Run`；菜单：`Tools/Local Verification/Unified atlas actual UI`。从干净EditMode串行执行，需要真实可显示GameView，禁止与当前Unity检查并行。
- UI报告：`LocalVerification/UnifiedAtlasActual/<UTC>/report.json`，断言/运行错误/清理错误/截图均单列；预计成功35张截图，不作为当前已有证据。
- 输入使用既有FilesWindowUiChecks.Click/LcdActualUiProbe的生产LCD命中链；不是物理鼠标。夹具明确redirected Save/DesktopProfile、模型禁用、暂停ctl与时钟。真实购买阶段短暂启用有限钱包host，disk/tick仍关闭。
- 失败时保留report与失败截图，不跳过未购、真实扣款、命中、原生最小化、切档或故事断言。

## 尚未验证

- 此候选未在Unity启动，零UI截图；以上UI代码尚不能视为运行通过。
- Unity JsonUtility往返、实际按钮/字体/裁切/演示行为仍待主控执行。
- 真实滚轮长知识卡、最大化窗口、全部分页条目布局、磁盘slot重载未编码为本次独立runner的完整验证；另见UI-ACCEPTANCE.md。
- 真实自然六阶段升阶、模型推理、Player、音频、性能和发布均不在本候选证据范围。
- 本轮未新增对白/私密资料来源，使用既有已授权collection与静态微型示例；没有人物知情或2016设定影响。没有发现需要改变已批准设计的冲突。

## 主控二审性能/文案追加

- 权限使用复用bool缓冲逐项比较，无哈希碰撞或新revision字段；静止刷新不重建Entries、不重复设置文案。换档/语言/分类/购买/能力/收藏变化仍触发重投影，科技入口门禁变化仍刷新按钮。
- LateUpdate缓存正文内容及viewport宽高，仅变化时测量；sizeDelta和序列缩放仅值改变时写入。
- 底部换为“已收录的能力、现象与网络结构。”及对应英文，不再放实现承诺。
- UI辅助新增静止0.6秒后Entries实例不变的断言，尚未Unity执行。Desktop/Editor已重新隔离编译均0错误；80核心不涉及本UI变更，保持原结果。静态检查现24项，apply-check再次通过。
