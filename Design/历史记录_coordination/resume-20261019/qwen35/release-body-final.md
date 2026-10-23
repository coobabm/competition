# 我的电脑活了 · 2026.10.19.5 Windows 试玩版

本版整合本轮桌面、培养、剧情与存档改动，随包聊天模型升级为 Qwen3.5-4B Q4_K_M，在本机 CPU 上运行，无需填写 API 密钥。

- 主界面左下角以原灰色小字显示「本游戏内置 AI 模型，部分对话和回答为 AI 合成内容，仅供参考。」；增加半透明深色底衬，保证文字在房间纸张背景上可读。没有单独的声明标题。YY 保留「AI 合成内容 · 生成回复仅供参考」的原提示，中英同步。
- 标题仅增加原创电源与学习信号点缀，本次没有整体替换界面字体。
- 整合科普卡单轮 +5% 训练奖励、月度 YY 房租账单、新闻阅读与老周短评、群聊生命周期和原创配图、图鉴及桌游/赌场修复。
- 整合三轴性格档案、变化轨迹和原因、阶段表达，以及脑窗的真实事件反馈；性格只在终局「问它」时参与 AI 路线选择。
- 改进保存失败传播、破产重开恢复、隔离用户记录路径、贴吧跨页保护与草稿保留、资料下载预计时长。

## 下载与运行

GitHub Release 提供两卷：`MyComputerAlive-Windows.7z.001` 与 `.002`。下载两卷和 `SHA256SUMS.txt`，放在同一文件夹，用 64 位 7-Zip 从 `.001` 一次性解压。运行完整目录里的 `MyComputerAlive.exe`；保留 Data、DLL、MonoBleedingEdge、模型及许可证等全部文件，无需安装 Unity Editor。不能只发送 EXE。

下载体积为 2,982,372,079 字节；完整解压目录约 3.41 GB，另需存档与缓存空间。GitHub 单附件大小限制使本版必须分卷。同样的完整游戏目录可直接分享给朋友。

## 验证范围

- Unity 6 Windows x64 / Mono 构建成功，0 错误、292 警告。
- 本日隔离 Play 中，YY 中英提示、脑窗和性格档案等 157 项控件/状态断言通过；测试前设置已恢复。
- 最终 EXE 已实际启动并加载 Qwen3.5-4B CPU AVX2 后端，实际开始界面中的游戏名、房间及声明已检查。
- 同一模型的隔离原生测试验证了中文、英文、上下文和二元判题真实回复，并验证取消任务结束后才派发后续请求，实际任务并发峰值为 1。
- 包体仅包含一个规范 GGUF；模型大小、SHA256、来源和许可文件核对通过；两卷 7z 完整性测试通过，共 315 个文件。

上述检查不代表全历史任务、全部终局或完整性能验收已经完成。完整 brain-v2 图集/阶段形态、部分性格后续玩法与同口径性能验证仍有后续工作；不承诺稳定 60fps 或音乐卡顿已根治。游戏培养系统不等于实时重训随包模型权重；不携带视觉 projector 或 embedding GGUF，检索使用 BM25。

## 模型与构建来源

模型是 Unsloth 分发的未修改 GGUF，2,740,937,888 字节；未执行 MTP 剥离或其它转换。模型 SHA256：

`00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4`

详见 [MODEL_SOURCE.json](https://github.com/coobabm/competition/blob/c4e02003cece703b93b7d46b18e5cd86c977ffcd/Assets/StreamingAssets/LingGuang/Models/MODEL_SOURCE.json)、[SHA256SUMS.txt](https://github.com/coobabm/competition/blob/c4e02003cece703b93b7d46b18e5cd86c977ffcd/Assets/StreamingAssets/LingGuang/Models/SHA256SUMS.txt) 和相邻许可证/模型卡。

构建目录名：`MyComputerAlive-2026.10.19.5-20261019-153511`。
构建时 HEAD 为 `3533141a406940d571e064f3797328c7d214235a`，包含尚未提交的集成改动，完整工作文件摘要为 `dc505d5a2b43dcb6dfa9aeb531e90e3da7669bb9a9589e537205f04e2224872c`。随包 `source-provenance.json` 保留此事实；后续源码快照提交不倒改构建时的 HEAD。源码发布排除私人协调文档、存档、缓存和二进制模型权重，保留有效玩法依赖、素材来源与许可证。


源码快照提交：c4e02003cece703b93b7d46b18e5cd86c977ffcd；分支 integration/release-20261019-5，尚未合并到 main。

最终 EXE 的实际开始界面与 YY 原生回复已完成取证；测试窗口正常退出后设置恢复，正常存档指纹一致。认证证明图与完整 ZIP 已本地交付。GitHub 此页仅提供两卷7z附件，完整ZIP未作为单附件上传（超过2GiB限制）。
