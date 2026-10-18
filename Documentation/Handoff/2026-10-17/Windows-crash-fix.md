# Windows 桌面闪退修复 · 2026.10.17.1

基线：1dd68c6de916c7175a07918d5cc4bcab7d0348a4。用户反馈进入游戏、打开喵鱼闪退。

复现：旧包在进入桌面后、操作喵鱼前即发生同样原生崩溃。官方 UnityPlayer 符号与二进制匹配（96C1A78E43DF4C84BE2A7EFA637912B1 / age 1）。堆栈：D3D12ScratchAllocator::DestroyScratch (+71) → ReleaseExcessScratch → ReclaimMemory → GfxDeviceD3D12::QueueExecute → FlushInternal → NextSubPassImpl → BeginRenderPassImpl。首帧 RVA 0x1089f67。Qwen 在崩溃前正常加载。

修复：ProjectSettings 的 WindowsStandaloneSupport 显式固定 Direct3D11，关闭 Graphics Jobs；LatestDownloadedBuild.Run 同样固定该设置，防止后续构建恢复自动 DX12。未修改喵鱼交易逻辑、开机动画或 AI 功能。

验证：Unity 6000.4.5f1 Windows 构建 Succeeded，0 errors / 189 warnings。实际启动日志确认 Direct3D 11.0 与 Qwen3-4B CPU runtime ready。使用真实窗口完成开机进入桌面、打开喵鱼、关闭后再次打开，连续运行数分钟，日志没有 Crash!!! 或 Exception。喵鱼显示第三阶段开放的原有提示；未测试第三阶段买卖和完整剧情。

游戏包：Build/MyComputerAlive-20261016-1dd68c6/MyComputerAlive.exe。保持原启动入口。独立旧录制版未修改。构建/运行日志保存在游戏包 BuildLogs 中。

构建时三项 Git 依赖通过官方锁定提交的本地副本提供，构建后已恢复仓库 manifest 与 lock；本机缓存位于 games/.coordination/build-dependencies。没有推送 GitHub。
