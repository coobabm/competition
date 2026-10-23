# 我的电脑活了 2026.10.20.1 · Windows 试玩版

一次开机开始的故事。你在 2016 年的出租屋里组装自己的第一台电脑，也在一台会说话的老机器里，教它认字、看图、听懂人话。

本版是 **Windows 免安装便携包**，内含本地对话模型（Qwen3.5-4B），不需要联网、不需要安装 Unity。

## 本轮更新

- **开局标题换成最终版**：Claude 交付的 v9 名字标（「我的电脑」+ 显示器图标内含「活」+「了」），暖白字、青绿微光，替换原来的 v5 素材。
- **「忘言夜」记忆挑战接入真实模型**：第四阶段的三轮谈话改为真实本地模型回答，不再预设「第三轮一定忘」。只有回复真的送到聊天窗口后才记录证据；模型没就绪或失败时取消这一轮并提示重问，不编造「记得/忘了」。结果只进纪事，不影响结局判定。
- **题库与垃圾短信口径**：正规群发营销（含「退订回 TD」）统一算垃圾短信，账单/消费/取件通知仍为正常；新增 2016 年参数化题干按日期解锁；唐诗干扰项修正整数边界溢出。
- **桌面与界面刷新**：合并上游的局部重绘优化（标签页、蜘蛛图标、训练侧栏、题卡），保留资料文件夹安装提示、24 种道具入口与 12 格装备、独立读书窗口与 YY 阅读回答、逐条「AI 合成内容 · 仅供参考」标注。
- **记忆考题措辞**：房租题改成虚构人物（同事老周，每月 1350），不再和玩家真实账单冲突；锁定订单提示改为「第 3 章开放订单」，与实际开放阶段一致。

## 验证

| 检查 | 结果 |
| --- | --- |
| EditMode 全套 | 2469 项：2462 通过、0 失败、6 跳过、1 项前提不满足（`XgDownloadsTests` 既有 `Assume` 不成立，合并前同样如此） |
| 合并后定向回归 | 8 个测试类 91 项 → 修掉 1 处测试夹具后，`XgForgetNightTests` 21/21；文案改动后 5 个类 61/61 |
| 开局标题 | `CaseMenuHudTests` 3/3；`TitleMarkPreview` 在 1920×1080 与 1280×720 实际渲染通过（源图 1775×1040、绘制 500×292.96、右缘 565、与副标题间距 8.00） |
| 节点循环 | 上一轮的 3D 开关机 147 项、三次回标题仍有效，本轮未重复执行 |
| 构建 | Unity 6000.4.5f1，Windows x64，`build-result=Succeeded`、0 error、275 warning |

范围说明：以上是编辑器/离屏渲染验收，**不等于**真实 Player 内的本地模型推理、音频与完整通关验收。这两项尚未执行，不计入已通过。

## 已知风险（请先读）

- **品牌与版权素材**：房间里的《三体》海报，以及部分 AI 生成商品图的提示词点名了 Kumamon、YSL、iPhone、AMD、NVIDIA、Samsung、ROMOSS 等真实品牌。本次按作者决定保留原样发布，未替换素材。若用于商业发行或平台审核，请自行评估并替换。
- **字体致谢**：随包 `ThirdPartyNotices/Fonts/` 已含 Apache-2.0、SIL OFL 1.1、OriginSerif、Liberation、Roboto Mono 与字体依赖说明；OpenSans / Noto Sans CJK / Rajdhani 的授权在 `FONT-DEPENDENCIES.txt` 中按嵌入元数据列明。
- **构建标记**：`sourceDirty=true`，唯一未提交改动是 `Packages/Aura/.gitignore`（Aura 包导入时重写该文件），源码即提交 `2411ffe6`。
- AI 生成内容在游戏内逐条标注「AI 合成内容 · 仅供参考 / AI-generated content · For reference only」。

## 下载与运行

**免安装包，不是安装器。** 完整解压后运行 `MyComputerAlive.exe`，必须保留 `MyComputerAlive_Data`、`UnityPlayer.dll`、`MonoBleedingEdge` 与 `ThirdPartyNotices` 等全部目录，不能只拿走 exe 或模型。

| 用途 | 文件 | 大小 | SHA-256 |
| --- | --- | --- | --- |
| 本地/TapTap 上传 | `MyComputerAlive-Windows-2026.10.20.1.zip` | 3,003,826,758 B（2865 MiB） | `8f144ddfdb6386672ef081d159264b4a8f5ddf9c79f49b3efd2e25c34635ec54` |
| GitHub 分卷 1 | `MyComputerAlive-Windows.7z.001` | 1,572,864,000 B（1500 MiB） | `dacad193ba1fd094ddae51c9108cc8c989439b5c954a25c8c5f7cb485802b0e5` |
| GitHub 分卷 2 | `MyComputerAlive-Windows.7z.002` | 1,395,880,472 B（1331 MiB） | `36db1ad2ecf6bd8793810ab755265f6a1dbd1f256cefaf71556b342521121e2b` |

完整 ZIP 超过 GitHub 单资产 2 GiB 限制，只作本地交付；公开下载用两卷 7z。下载全部分卷与 `SHA256SUMS.txt`，放在同一目录，用 7-Zip 从 `.001` 测试并解压。

解压后占用 3,413,367,530 B（3256 MiB，308 个文件）。

## 引擎与授权

- 本地对话模型：Qwen3.5-4B-Q4_K_M（Unsloth GGUF），Apache-2.0，SHA-256 `00fe7986…ef11a4`，随包 1 份，检索为 BM25。
- 第三方组件与字体授权：`ThirdPartyNotices/`（LLMUnity、LlamaLib/llama.cpp、USearch、DOTween、Blackjack、Oxygen 图标、字体与照片来源）。
- 本仓库源码：`integration/upstream-20261020` 分支，提交 `2411ffe6`。
