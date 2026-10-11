# 灵光一现 · AI 进化游戏

当前源码版本：**2026.10.11.2**（2026-10-11）。从电脑机箱进入 2016 年的桌面，教 AI 判断、训练模型、自由聊天，并在收入和用电之间安排它的成长。

**[本次开发日志与 8 张运行截图](Documentation/Devlogs/2026-10-11/README.md)**

## 本次更新

- 更直观的开局教学、学习反馈与手动章节验收，补充逻辑和时事题。
- 第一章正常收电费，新增资金账本；已有存档从当前余额开始记录。
- 贴吧自由输入讨论、回复持久化与未读提醒；清理人物知道秘密程序的剧情和提示内容。
- 修复壁纸应用和进入桌面左侧露房间的问题。
- 六档存档、自动保存、章节快照、道具科普和生活聊天补充。

## 打开和构建

这是 **Unity 源码工程**，不是可直接运行的 Windows 游戏包。

1. 使用 Git LFS 克隆；若资源显示为文本指针，执行 `git lfs pull`。
2. 用 Unity **6000.4.5f1** 打开仓库根目录，等待依赖解析及资源导入。
3. 打开 `Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity` 并进入 Play；也可以直接打开 `Assets/LingGuangV05/Scenes/Main.unity` 调试。
4. 构建场景顺序保持 `ComputerCaseMenu` → `Main`。Windows 构建入口为 `WindowsReleaseBuild.Build`，源码在 `Assets/LocalBuild/Editor/WindowsReleaseBuild.cs`；输出至 `Build/ClaudeHandoff-Windows/`。

保留存档产品标识 `RippleLocalPreview / LingGuang-Playability-20261008`，以兼容此前试玩版本。更改产品标识会影响存档位置。

## 模型和分发

仓库不包含大模型权重、本机 llama 推理运行库、个人存档和 Windows 压缩包。没有模型时保留预设回复；本机兼容服务可被 LocalLlm 连接。单独下载本仓库不会自动获得 Qwen。

本地验证的 Windows 分享包内置 Qwen3-4B-Q4_K_M 与推理运行库。分享该包时发送完整游戏文件夹，保留 `LingGuang_Data` 等依赖，不能只发 exe。本次更新使用概念板、记忆、阶段和上下文呈现 AI 成长，没有微调 GGUF 权重。

## 验证范围

- 本版 Windows 构建成功：0 个错误、75 条警告。
- 200 项逻辑检查通过；可复现的编辑器检查保存在 `Assets/LocalBuild/Editor/`，统一入口为 `ClaudeHandoffChecks.Validate`（依次运行三组检查）。
- Unity 桌面检查覆盖教学、训练、本地模型对话、六档界面、道具说明、账本、实际壁纸切换、贴吧讨论保存，以及关机和电脑入口；截图为真实运行画面，部分使用测试进度。
- Windows 包内模型独立启动并回复通过。完整真人两小时试玩尚未完成；脚本模拟不等于真人通关时间。
- 原仓库的资源引用审计可在历史提交 `1307fc0` 的 `SNAPSHOT.json` 查看；本次未重新进行完整 GUID 审计，不宣称清除了原有缺失引用。

当前版本与验证摘要见 `SNAPSHOT.json`。`FILES.sha256` 记录本次提交文件的实际内容哈希（不含清单自身）；二进制资源需先拉取 Git LFS 内容。

## 资源与许可

保留电脑入口、Main 及其 DreamOS、HongmengOS、字体、图标、剧情、音频和必要依赖。`Assets/Emergence` 仅保留 Main 使用的中文字体与授权。第三方资源沿用各自许可，并非统一按 MIT 重新授权。

配图作者、来源和许可见 `Assets/LingGuangV05/Resources/LingGuangV05/Media/PHOTO_CREDITS.txt`。
