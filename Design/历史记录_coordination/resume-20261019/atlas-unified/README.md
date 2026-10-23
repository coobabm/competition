2026-10-19 恢复审查更新：以 `RESUME_REPORT.md` 和 `verification/resume-*.log` 为本轮证据。新增独立 `UnifiedAtlasActualUiRunner.Run`、边界检查及原生窗口即时清理；Unity/UI仍未执行。

# 统一图鉴最小完整候选

依据 `Design/整合记录.md:246` 与 `:298`（#24），并遵守 `:494` 后续展示模块购买原则。只在本目录制作；没有改生产、启动 Unity、生成新存档或发布。

## 交付

- `unified-atlas.patch`：可由主控 review / apply-check 后合入的最小补丁。
- `candidate/`：完整候选文件；`baseline/`：三个共享文件的原始字节备份。
- `manifest.json`：每个文件基准/候选 SHA-256；新文件基准为 null。
- `validation.json` 与 `verification/*-build.log`、`verification/core-checks.log`：检查结果。
- `UI-ACCEPTANCE.md`：真实界面验收范围与调用方式。

现有文件仅改 `XgTechTreePage.cs`（工具栏图鉴）、`XingGuangView.Profile.cs`（名字页更多图鉴）、`XgStageSequenceDemo.cs`（可选减少动态参数，默认 false，原调用兼容）。新增四个运行时文件：只读 `XgUnifiedAtlasCatalog`、统一界面 `XgUnifiedAtlas`、`XingGuangView.Atlas` 桥接、独立 `XgAtlasTinyDemo`；另有核心检查和实际 UI 检查各一个文件及新文件 meta。**不改 StageCurtain，不改原升阶字幕、队列、事件和奖励。**

## 行为与边界

两个入口打开同一图鉴实例，三类为能力演示、训练现象（含原知识卡及已答题收藏）、网络结构。列表分页，正文可滚动，不删除或重新收藏原数据。

能力重览需 `TrainPart("weights")`（新档购买现有 ¥100 权重可视化、前置 train.curve；旧档沿用原 viewer 权限）及 `HasAbility(stage)` 同时满足。只修改 S.stage 不会开放未来能力。未购展示封印，并定位既有 `train.weights` 节点；没有新增费用。知识和结构沿用 `AtlasLit` 与原结构图鉴的已购/实际 Run 权利，未收录内容不展示正文，不开放科技购买或新能力。

1–3 阶段使用原升阶演示的同源 `XgTinyRun`，固定 2016 种子与独立数据：单层 XOR、多层 XOR 对照、识别零。4–6 阶段复用 `XgStageSequenceDemo`。这些实算小例子不读取聊天模型权重，不写学习数据，也不使用游戏随机流。不会借用、重挂接或停止原 `XgRealTrainPanel`。减少特效时小模型显示实算完成帧，序列演示直接显示末帧，网络结构不逐帧动画。

关闭、内部 .exe 最小化/关闭、换页、换 XgSim/主存档、故事独占会结束本图鉴演示；所有操作回调先核对绑定状态。图鉴本身不设置或清除 `lessonAtlasUnread`；原科技页打开时的已存在清点逻辑保持。

## 已验证

- 80 项纯核心断言通过：购买与能力双门禁、跨存档隔离、三种原知识来源、结构历史兼容、JSON 往返、重复实算确定性；图鉴访问与独立小模型重览前后完整 XgState 序列化结果相同，无能力/购买事件。
- 候选 Core、完整 Assembly-CSharp、Editor 代码分别用隔离 .NET 项目编译通过，均 0 错误。引用现有 Unity DLL，未启动 Unity；全部输出在 `verification/`。项目原有警告保留（Core 3、Desktop 538、Editor 237），候选无新增编译警告。
- 静态检查拒绝 SaveNow/MarkDirty/升阶队列/购买/奖励/训练数据写入等调用，并核对同源独立演示及生命周期门禁。
- `git apply --check` 只读验证由打包后单独执行，结果见交接；合入前仍需核对 manifest，因为主控可能并行更新共享文件。

## 未声称完成

尚未在 Unity 实际运行图鉴，尚无本候选的 LCD 截图、鼠标命中、窗口缩放或 Player 成品证据。附带 `UnifiedAtlasActualUiChecks.Run` 是已编译但未执行的实际 UI 验收辅助；本候选的编译与80核心断言不替代该验收。

初次隔离编译曾因验证项目输出引用路径错误失败，修正仅在 verification；之后查到候选缺少 StoryFocus 的 using 并修复。最终日志与结果以上述最新文件为准。没有通过弱化产品门禁来让检查通过。
