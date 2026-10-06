# Main 场景代码快照

对应场景：`Assets/LingGuangV05/Scenes/Main.unity`。本目录保留现有实现，不修复或重写玩法。

## 包含
- LingGuangV05 的 Core、Runtime、DesktopBridge、XingGuang、YY2016、Mail2016、UI 与本地模型调用接口源码。
- Main 桌面的 Aero2010 运行时代码与 AeroGlass shader。
- 剧情、时代事件、论坛、序章与本地化 JSON。
- 相关测试源码、四套 .NET 测试入口和通关机器人源码。
- 所选 Unity 源码的 `.meta` 和程序集定义，保留 GUID。
- 本地推理运行时的打包脚本；不包含运行时二进制或模型权重。

## 明确不包含
模型权重、推理二进制、其他旧项目、旧 Incremental 模块、编辑器场景安装/QA工具、缓存、构建产物、日志、截图、代码图谱、聊天记录、存档与凭据。

**这是代码快照，不是能直接打开并运行 Main 的完整 Unity 工程。**
Main.unity（约22MB）及其美术、字体、音频、预制体、材质等非代码资源没有复制；第三方 DreamOS 等资源包也没有上传。
UI 源码依赖原项目的 Unity、uGUI/TMP、Input System、URP、DreamOS 等。模型接口源码被保留，但模型需自行提供；不要把“无模型快照”理解为删除了模型调用代码。

## 目录
- `Assets/LingGuangV05/Core`：共享游戏、日历、剧情、论坛、序章与小游戏逻辑。
- `Assets/LingGuangV05/XingGuang/Core`：训练、墙、诊断、养成、众包与终章规则。
- `Assets/LingGuangV05/DesktopBridge`、`XingGuang/Desktop`：Main 桌面与页面接入。
- `Assets/LingGuangV05/Resources`：相关文本数据。
- `Assets/HongmengOS/Aero2010`：Main 桌面呈现代码。
- `Tools`：可单独运行的 .NET 测试与机器人。

## 运行纯 C# 测试
在本目录执行（需要兼容的 .NET SDK；NuGet 依赖按项目文件还原）：

```sh
dotnet test Tools/LingGuangStory.Tests/LingGuangStory.Tests.csproj
dotnet test Tools/LingGuangChapterOne.Tests/LingGuangChapterOne.Tests.csproj
dotnet test Tools/LingGuangPersistence.Tests/LingGuangPersistence.Tests.csproj
dotnet test Tools/XingGuang/tests/Tests.csproj
```

上述测试不包含 Unity UI/Play 验证，也不证明所有已知缺陷修复。此快照未进行功能修复。

`source-manifest.json` 记录所复制文件的 SHA-256 与源场景指纹，用于核对上传完整性。

## 导出验证（2026-10-04）

导出快照在独立目录运行 .NET 测试：XingGuang 386、Story 68、ChapterOne 40、Persistence 12，共506项通过。
没有运行 Unity Play 或完整场景构建；测试生成的 bin/obj/log 未包含在上传目录。
