# 赌场 UI / 实盘保存补充候选交付

2026-10-19。只在本独立候选目录写入；没有生产编辑、Unity 启动、提交、推送或改写共享文档。此前群预设核心与 UI 补丁保持冻结。

## 最终补丁

- 文件：`casino-scam-ui.patch`
- SHA256：`0c1c17bb0d1ed5e16bad11448bee49134fab0e7c18f01f9ef8be46ccb2016c94`
- 仅新增 `Assets/LocalBuild/Editor/CasinoScamActualUiRunner.cs` + `.meta`。
- 入口 `CasinoScamActualUiRunner.Run`；独立 Main 场景宿主，不改旧 CasinoPlayQa / Juice / Sidebar。
- 依赖以当前已合入赌场候选的生产工作区和 `manifest.json` 哈希为准。生产 HEAD `3533141a406940d571e064f3797328c7d214235a`，并非干净树。

## 已执行证据

- `verification/compile-narrow.log`：只编译新 runner，参考当前生产/Unity 实际程序集，**0 错误 / 0 警告**。保存了 csproj 和输出 DLL。
- `verification/apply-check.log`：`git apply --check` **退出 0**，未应用。
- 候选完整源与新文件拷贝分别在 `candidate/`、`NEW_FILES/`；manifest 包含每个新增文件及关键运行依赖 SHA256。
- 初次 ExtraSamples 返回 double 而夹具写 int 的三处编译错误已纠正；原日志保留为 `compile-narrow.initial-type-errors.log`。最终编译输出已限定在本赌场候选目录。

## 新覆盖及边界

详细操作见 README。核心新增是：双语付费/拒绝/次日忽略，实际 LCD 最后一注触发冻结；每种 frozen/closed 真 slot6 保存与 LoadArchive；同一可见页面的旧 Pay/Quit 事件跨 frozen-owner 无效；真实已接受黑杰克在遗留冻结状态存盘恢复后用真实 Stand 结算；真实 40 秒公开群消息、磁盘 receipt 去重与跨档未到期定时器隔离。计划 20 张图和逐次存档证据快照。

历史 29 把/日历/阶段/首两条公开消息 receipt/遗留 frozen-active-hand 都是明确夹具；没有重复宣称 68 核心检查、自然进度、物理鼠键、真实模型推理、生产自动保存成功、保存失败重试或 Player 已验收。

**尚未运行 Unity。** 主控需唯一串行导入执行；只有本次实际 report/passed.txt/截图和磁盘证据能确认运行通过。先前 group runner 宿主修正（TestMode 下通知组件为空、OriginCurtain 事先冻结、非 batch 的 verificationExit）已经复用。
