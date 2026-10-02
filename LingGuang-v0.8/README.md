# 灵光一闪 LingGuang v0.8 源码快照

本目录保存 `competition` 工程中《灵光一闪》v0.8 的源码、场景、着色器、配置、测试与中文字体，上传日期为 2026-10-02。游戏源码与原始 `.meta` 文件按原样复制，没有改写规则或表现层。

**这是源码快照，不是可直接在 Unity Hub 中打开的独立工程。** 仓库根目录的 Ripple 工程及 `Assets/Lingguang` v0.6 保持不变，本目录不会自动替换或参与根工程的 Assets 编译。

## 内容

- `Assets/LingGuang/Core/`：纯 C# 规则、模拟器、流程、配置模型与机器人。
- `Assets/LingGuang/Game/`：棋盘、交互、HUD、教学、特效与合成音频。
- `Assets/LingGuang/Scenes/LingGuang.unity`：入口场景。
- `Assets/LingGuang/Resources/`：URP 发光着色器。
- `Assets/LingGuang/Tests/Editor/`：EditMode 测试。
- `Assets/StreamingAssets/LingGuang/GameConfig.json`：运行时配置。
- `Assets/Emergence/Resources/Fonts/`：源码通过 `Resources.Load` 使用的 Noto Sans CJK 字体及随附 OFL 许可；仅复制这一字体依赖，未复制 Emergence 游戏。
- `Reference/`：源工程的 Unity 版本记录及本模块所需的主要包版本，不是完整工程的包清单。
- `snapshot-manifest.json`：复制文件的大小及 SHA-256，用于核对上传内容。

## 在独立 Unity 工程中使用

1. 使用 Unity **6000.4.5f1** 的 URP 工程。不要直接把本目录的 Assets 覆盖到仓库根工程：根工程使用内置渲染管线，并已有仅大小写不同的旧版 Lingguang 模块。
2. 按 `Reference/RequiredPackages.json` 安装 Input System、URP、UGUI/TextMeshPro 与 Test Framework；保留工程模板自身的其他依赖。
3. 将本目录 `Assets/` 中的内容和 `.meta` 文件一起复制到新工程的 `Assets/`，导入 TMP Essential Resources，并启用 Input System（或 Both）。新工程需要配置并启用 URP Pipeline Asset。
4. 打开导入后的 `Assets/LingGuang/Scenes/LingGuang.unity` 并运行。操作、实现范围与已知取舍见 `Assets/LingGuang/README.md`。
5. 在 Test Runner 的 EditMode 中运行 `LingGuang.Tests`。

## 验证边界

此次上传检查复制文件哈希、JSON/程序集定义、场景脚本 GUID、Git 提交范围和远端内容；未在新工程中重新运行 Unity 编译、测试或 Play Mode。原始模块 README 内的测试通过数量与运行观察是原作者记录，不代表此次上传重新测得的结果。

未包含 `Library`、`Temp`、日志、个人设置、AI 插件配置、商业素材或其他游戏模块。字体许可仅适用于所附字体，不为本项目源码另外授予许可。
