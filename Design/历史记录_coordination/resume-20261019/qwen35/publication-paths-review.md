# 发布候选路径与活动模型说明复核

已生成 `publication-paths.txt`，UTF-8 无 BOM，每行仓库相对文件路径。共 289 条：Assets 286、ProjectSettings 1、Design 2。仅写协调目录；未改生产源码、暂存区或提交记录，未启动 Unity。

## 清单依据

- 取当前 HEAD 的有效 Assets/ProjectSettings 差异，以及这两个目录的非忽略未跟踪文件，包含新 C# 分部类、对应 .meta、字体/图片、模型来源/许可/哈希记录、构建与验收源码。
- 明确加入 `Design/素材源码/group-chat/draw_chat_scenes.py` 和 `manifest.json`。
- 全部候选文件存在；选中 Assets 的每个非 .meta 文件都有对应 .meta（原本已跟踪未变的 .meta 无需重复列入候选差异）。
- 有效 ProjectSettings 只含 `ProjectSettings/ProjectSettings.asset`：Standalone Mono 后端配置。
- 不包含私人 Design 交接/任务/状态日志、发布候选文案和截图、.vsconfig、构建产物、模型权重与下载原生库。
- 排除仅规格化显示为 dirty、但 Git 内容无有效差异的 SparkNeuronDemoFont.asset、Hardpixel-nn51 SDF.asset、OriginMono SDF.asset、OriginSerif SDF.asset、PC_RPAsset.asset、UniversalRenderPipelineGlobalSettings.asset、Packages/Aura/.gitignore、ProjectSettings/GraphicsSettings.asset。
- 另排除 `Assets/LingGuangV05/CaseMenu/Art/Materials/Decor_Ivory Ceramic.mat` 的单个 float 文本精度变化（0.81176466 → 0.8117647），没有功能改变。
- 此清单是当前候选快照。root 新增公开 README、变更生产文件或新增 .meta 后，应把相应路径补入清单；已有路径后续内容更新无需另改清单。

## 当前模型 README 结论

扫描活动 Assets/Tools 模型入口，没有发现仍把当前模型称为 Qwen3-4B 的说明。现有三个 Qwen3-4B 命中均是“此前版本/历史验证不适用新模型”的正确历史说明：主 README、XingGuang README、MODEL_SOURCE.json 的 replaces 字段。不要批量改写这些历史引用。

需要更新的具体活动入口：

1. `Assets/LingGuangV05/README.md:87`：仍以 2026.10.19.4 为目标并写“待新版构建打包”。按实际 .5 构建与 Player 验收证据更新版本/状态，保留 CPU AVX2、LLMUnity/LlamaLib 版本与新旧模型验证边界。
2. 同文件 `:89`：完整 Player 性能仍写待验证。只按最终真实证据调整，不将加载成功替代性能验收。
3. 同文件 `:90`：当前链接指向 `../../Design/Qwen3.5-4B模型迁移与发行记录_20261019.md`。该私人进度文档不在候选清单，会导致新分支链接断开。改为已入清单的 `../StreamingAssets/LingGuang/Models/MODEL_SOURCE.json` 与 `SHA256SUMS.txt`，或单独新增干净的公开模型说明。
4. `Assets/LingGuangV05/XingGuang/README.md:141,159`：同样仍写 .4、待构建/待 Player；按实际 .5 证据更新。
5. 同文件 `:144`：指向未发布私人迁移记录；改为 `../../StreamingAssets/LingGuang/Models/MODEL_SOURCE.json` 与 `SHA256SUMS.txt`，或新增公开说明。
6. 主 README `:83`：“所有本章 AI 答案/置信度为可复现的规则模拟。不下载模型……”与下方真实聊天推理说明相冲突。应明确限定为教学模拟/训练系统；自由聊天由随包本地模型推理，运行时不下载模型，但开发工程首次会下载运行库。
7. 主 README `:107-116,120,125-126` 仍有 `/Users/kk/competition/` 本机绝对路径。属于已有旧文档，不是本轮 Qwen 名称错误；若作为公开入门入口整理，可改相对工程路径，并把旧 Evidence 记录标为历史，不能当成本轮验证。

当前模型文件名 `Qwen3.5-4B-Q4_K_M.gguf`、Unsloth 原始大小 2,740,937,888、qwen35/32 blocks/426 tensors、无 MTP、不执行剥离、上下文与 embedding 元数据在上述入口已正确。项目根目录没有 README.md。

## 打包依赖说明

已按要求保留绘图程序和 manifest。manifest 仍有“candidate/not imported”旧状态及 agent/工具可用性记录，不影响图片权重/源码引用，但公开展示前可另行精简其工作流状态。没有为此扩大清单去包含私人任务日志。
