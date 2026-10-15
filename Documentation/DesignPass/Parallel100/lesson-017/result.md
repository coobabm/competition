# lesson-017 · 首分钟真实场景验收交付

日期：2026-10-13（Asia/Singapore）。范围仅为交接已授权的《10.11问题_设计策划》第 13 行。本轮未读取新的策划正文、未扩展后续条目。

## 结论

首卡“看猜测 → 评价 → 明确结算 → 自动下一卡”已有生产实现和内存检查，无需重写其判断逻辑。本任务交付真实桌面场景的人工验收脚本、观察表和依赖核验脚本。当前只能作静态结论；首分钟免打断、默认布局下壁纸可见地短亮仍未被运行或视觉验收证明，不得标记第 13 行完成。

已确认一个可独立整合的最小文案缺口：setup 结束仍显示“它还不会判断。先教它 4 张卡，再让它训练。随时可以和它说话。”，与首课先看猜测、只评价的目标不一致。`changes.patch` 只将此处中英文提示改为“它先猜，你判对错。猜错就纠正它，猜对就告诉它。”，保留原调用位置与时长。补丁尚未应用生产源码；实际 toast 是否在众包前台可见仍需验收，此改动不代表其他干扰已解决。

## 交付文件

- `scene-acceptance.md`：可由主聊天逐步执行的真实场景脚本，含自然新局、准确纠正／错误认可／准确认可、慢读停留、快速四卡、键盘、宽窄窗口、中英文和减少特效分支；要求连续录像，区分操作中断与目标分散。
- `observations.csv`：11 行初始 NOT_RUN 记录模板，可按运行、提示事件及布局组合复制；无预填通过结果。
- `changes.patch`：仅上述 setup 提示文本的一处替换。
- `source-baseline.json`：18 个依赖的 SHA256，包含交接、现有测试及相关生产源文件。
- `verify-dependencies.ps1`：只读比较本基线；缺失或变化退出 2，完全一致退出 0。此退出 0 仅表示源码指纹未变，不表示游戏行为通过。

## 原文件证据（相对工程根目录；行号以基线版本为准）

| 依据 | 静态事实与验收意义 |
| --- | --- |
| `Documentation/DesignPass/Agents/issues-task-001.md` | 已交接教学判断和事件语义，明确主聊天负责壁纸整合与真实场景；本交付不重复已有逻辑测试 |
| `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs:49–85` | 已有 2×2×2 判断组合、学习／事件／换卡与旧卡拒绝断言；它创建内存 XgSim，不含真实桌面 UI |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:46–55` | 当前卡 ID／显示猜测校验，通过 Answer 后只在 result.correct 时同步发 LessonTaught；事件参数保留旧卡 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs:355–454` | UI 进入 TryEvaluateTeachingGuess；成功区分“你纠正了它／它答对了”，误判解释真实答案；最终 Refresh。成功反馈定时 3 秒，需视觉确认可读、未被新卡误解 |
| 同文件 `:650–683` | 教学猜测与“猜对了／猜错了”按钮明确；第一张引导仅在 handCorrect+handWrong==0 时覆盖通用 steps |
| 同文件 `:691–699` 与 `XgTeachCompanion.cs:60–65` | 答过首卡后出现训练进度和“教它→训练→和它聊”，样本足够时引导训练；当前不是完整 60 秒的单目标门控。由快速四卡及慢读两条实测区分 |
| `XgTeachCompanion.cs:66–77` | 32 格根据概念权重差异亮起；这是页面伴随格子，不能作为桌面壁纸已短亮的证据 |
| `XgSetupOverlay.cs:201–210` | setup 自有 .8 秒灯泡效果；随后 ShowTab("teach") 并发出七秒训练／聊天提示。不能将这个灯泡误作纠错反馈 |
| `Assets/LingGuangV05/Core/OpeningQuiet.cs:40–58` | 非测试模式抑制部分环境闲聊；story 提示始终允许，开局顺序只看月卡、AI 入群、联系人等前序状态，没有首课评价／60 秒门控 |
| `Assets/LingGuangV05/DesktopBridge/Story/AiJoinsYy.cs:49,162–192`；`Assets/LingGuangV05/YY2016/YYChatHub.cs:536–543` | AI 可在 stage 1 安装后排队；SettleSeconds=20，等待桌面条件后播放。CanPlay 检查 setup 和其他演出，不检查教学卡完成；同时 TestMode 下拒绝播放。因此只用测试模式不能验证正常首分钟干扰 |
| `Assets/LingGuangV05/XingGuang/Desktop/XgBrainWallpaper.cs:139–147`；`Assets/LingGuangV05/DesktopBridge/Story/TrayPopups.cs:49–54` | 壁纸 reveal 后排队 Windows“桌面背景已更改”；EnqueueNotification 此处不走 Enqueue 的 OpeningQuiet.Allows。它是需记录的通知来源，尚未证明在首分钟遮挡 |
| `XgWallpaperConcepts.cs:84–115` | 按 .1 秒节拍重绘概念／权重颜色；结合本轮 Assets 内 LessonTaught 检索，仅找到声明、调用和现有检查中的订阅，没有生产订阅者。不能把重绘或永久颜色变化当成纠正后事件驱动短亮 |
| `Assets/LingGuangV05/Runtime/ChapterOneRuntime.cs:41–81` | Awake 即初始化，Editor 隔离路径必须在此前设置；真实存档不得用于本次验收 |

表内省略目录的文件均在 `Assets/LingGuangV05/XingGuang/Desktop/`。完整文件路径及所有哈希见基线 JSON。

## 补丁整合约束

目标：`Assets/LingGuangV05/XingGuang/Desktop/XgSetupOverlay.cs`。

当前源 SHA256：`0DD93A51E16FBB841AE4AB31CA369E45C29F6F12995894FC13D24826848C14D5`。

补丁包含 `Refresh(view.Controller.runtime)`、`view.ShowTab("teach")` 及结束括号上下文。整合前先核验 SHA；变化时重新审阅，不用模糊匹配强套补丁。不修改教学判断、壁纸、通知队列、训练规则或存档结构。

## 其余缺口与接口合同（不在本任务改生产代码）

1. **壁纸反馈绑定**：沿用已有 `LessonTaught(XgCard originalCard, XgAnswer result)`，整合负责人在当前壁纸／当前 Sim 上绑定，成功纠正时使用 originalCard 对应区域短亮；换 Sim、销毁时解绑。Answer 已换卡，不能读取当前 Card 来定位旧课。需要可观察的峰值和回落；本脚本提出 0.25 秒响应／2 秒回落的工作阈值供实测，原文没有这些数值。不可用伴随格子或 setup 灯泡替代。
2. **首分钟调度**：现有 OpeningQuiet／OpeningSequence 只保证部分静默或演出排序，不覆盖完整首课保护。若实测出现干扰，最小接口应是由首课 UI 在新局第一张可交互卡显示时设置的会话级单调时钟截止时间，以及只读 `FirstLessonProtected(now)`；同一新局重复刷新不重置 60 秒，切换新局／销毁清理。接入已确认的首课外提示／演出入口并保留到期后的原调度。不要改全局队列语义、不要阻断反馈／下一卡。具体生命周期由整合负责人接入；本任务未假设已有此接口，未提交跨组件猜测性补丁。
3. **QA 观测**：主聊天可加只读 LessonTaught 日志和单调时钟到录像；不应让探针代替按钮作答或关闭干扰源。若隔离运行没有保持正常剧情调度，本条干扰结论必须保持待验证。

## 本轮验证记录

- 静态：完成指定交接与源码核对，没有重复实现已有布尔组合检查。
- 静态：`git apply --check --ignore-space-change` 对专属目录的补丁校验成功，仅检查，未应用。
- 静态：CSV 模板列数校验与依赖 SHA256 复核见同目录 `static-validation.txt`；核验脚本不启动 Unity、不访问玩家数据。
- 实际运行：NOT_RUN。未启动 Unity／游戏，未执行 DesignFirstLessonChecks，未编译、提交、推送或打包。
- 视觉验收：NOT_RUN。无截图／录像，不声称首分钟无干扰或壁纸效果通过。

全部写入均限于 `Documentation/DesignPass/Parallel100/lesson-017/`。未修改 Assets、ProjectSettings、全局清单或他人目录，未读取或修改真实玩家存档，未给其他聊天发消息。待整合负责人采纳最小文案补丁，统一执行场景脚本及现有检查；无需本聊天无限等待。
