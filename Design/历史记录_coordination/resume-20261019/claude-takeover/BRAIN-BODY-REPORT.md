# 大脑侧视底图／区域布局接管报告

2026-10-19。本范围 C# 已冻结，等主控单 Unity 验证。已读 `ClaudeReturnRound2/E2/大脑窗口光效_设计说明.md` 与 `ClaudeReturnRound3/brain-v2/brain_window_v2.html`。采用用户已批准的侧视布局、推理区名称、统一是非色；没有修改训练平衡、持久概念归属、Persona玩法或 Window/Feedback 所有权文件。

## 已实现

- 新增 `XgBrainWindowBody.cs`：复用 Cortex 的 BrainPath、StemPath、Sulci 采样，做静态侧视轮廓、薄填充、脑沟。没有演示神经元、随机点、模拟学习或独立 Update。由 Canvas 尺寸／启用等既有 dirty 机制重画。
- `XgCortexGraphic.cs` 暴露只读轮廓样本、脑沟求值和四区中心／半径／标签坐标，原拓扑仍用同一曲线样本；Good 改为 `#5FE3B5`，Bad 为 `#FF7A6E`。
- `XgWallpaperConcepts.Regions.cs` 的顺序仍为 0 vision / 1 sequence / 2 tone / 3 logic。四区从 Cortex 的 (652,258)/(432,356)/(432,150)/(215,262) 映射到统一 1280×832 参考画布，采用 x56..824、y18..536 视框、92% 留边、等比缩放。概念、结构导引、区域椭圆、标签与 hover 共用公开 RegionCenter / RegionRadius / RegionLabelPosition。
- 原 2×2 框替换成解剖区椭圆；循环、门控、卷积窗、双环、残差、MLP、Transformer 空心结构点都沿新区域 extent 重新放置，保持已有结构含义；实际实心点和 learned links 仍只来自 board IDs／links，未造新概念。
- 标签虚拟字号 24、自动缩小至18；默认约500–600px宽窗口约9–11px显示，正式可读性仍需实窗。种子问号移至 UV(.52,.24)，避免与新读字区中心重叠，所有使用 SeedPosition 的真实关联保持同一位置。
- `XgBrainWallpaper.cs` 两安装入口都接 Body。桌面入口使用1280:832 FitInParent，避免宽屏拉伸；小窗仍交由 Window agent 的1280×832等比虚拟画布。
- `XgBoardData.DisplayRegion(dataset)` 仅将 chess 显示为 logic；原 Region(chess)=sequence 和 Region(go)=vision 均保留。Window/Feedback agent 已调用 DisplayRegion 处理明确题库事件。

## 棋类兼容边界

XgConcept 只有 region/key/alt/权重等，没有 dataset 来源，且 sequence 词特征可能被不同题库共享。直接改 Region(chess) 会改变训练查找键和既有权重使用；按主控决定只改视觉分类。

**历史共享词点没有被伪认成 chess 概念并搬动**。真实点继续按其持久 region 显示，明确的国际象棋题库签名／事件显示在推理区；这是表现映射，不是存档迁移。go 未扩改。要将历史棋题所有点永久分区，需要独立来源/迁移设计，不能宣称已完成。

## 减少特效与共享起始门禁

主控随后明确授权额外处理挂件，已统一 `XgBrainWallpaper.MotionReduced(controller)` = Low OR sim.S.reduceFx OR View.Juice.Reduced，不改全局 Juice。

- Concepts：结构时钟表现和 LessonPulse 在减少特效时不推进；真实 cells 仍刷新。
- AnswerPath：减少特效时保留固定低亮度真实路径线，取消旅行和节点光晕；静态确认到期移除，不把改变后的学习状态藏起来。
- Training：减少特效时不画生灭／合并动画，保留固定训练确认光晕和真实概念刷新；固定提示只在事件／到期／模式改变重画。
- Persona：减少特效时立即显示真实轴值、停止 Phase／插值，不改轴值来源；静态相同状态不重复 dirty。
- Abilities／FamilyStar：减少特效时真实灯／星状态立即显示，取消渐变过渡。
- 初始 Reveal 在减少特效时直接落稳；隐藏小窗由 Window.SetActive(false) 使所有子组件停 Update，订阅层现有 OnDisable 解绑保留。

经与 Window agent 协调，`TryStartGlow(region)` 在 WindowMode 使用同一 `Window.OnsetGate`：

1. LessonPulse 起始。
2. Training.Started 大光晕和 Changed 生灭／合并亮化起始（被拒仍刷新真实概念）。
3. AnswerPath 节点大光晕起始（被拒仍显示低亮真实路径）。

共享门禁纯对象由 Window/Feedback agent 维护，3/s + 同区333ms。低亮路径线与持续结构示意不计新 flash；本报告没有实际测光证据，不宣称全画面所有像素已满足频闪指标。额外外部层如 Rebirth 未改。

## 文件范围

- `Assets/LingGuangV05/XingGuang/Core/XgBoardData.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/XgCortexGraphic.cs`
- 同 Desktop 下 `XgBrainWallpaper.cs`、新增 `XgBrainWindowBody.cs` + .meta
- 同 Desktop 下 `XgWallpaperConcepts.cs/.Regions.cs/.Structure.cs/.Dust.cs/.Lesson.cs`
- 同 Desktop 下 `XgWallpaperAnswerPath.cs`、`XgWallpaperTraining.cs`、`XgWallpaperPersona.cs`（仅减少特效）、`XgWallpaperAbilities.cs`、`XgWallpaperFamilyStar.cs`

编辑前快照在 `brain-body-before/`。没有编辑 `XgBrainWindow.cs`／`XgBrainWindowFeedback.cs`，没有改 Persona Core／人格内容和模型数值。

## 实际验证

1. **真实 C# 语义编译通过**：用 Unity 已有 Bee rsp 和 Unity/项目引用、Roslyn csc，构建 XingGuang.Core 与 Assembly-CSharp 到 `brain-body-checks/`，两次最终 exit 0。输出没有回写生产 Library，也没有启动 Unity。旧 rsp 漏了新 PersonaArchive，补入既有新源后 Core 通过；Desktop 显式补 Body 和 Tieba 两 helper。存在项目原有 warning 和 Feedback.animation 隐藏 Component.animation 的 warning，无编译错误。该结果不等于 Unity 导入／Editor tests／Player build。
2. **21项实际程序集反射检查通过**：运行 `brain-body-checks/geometry/Geometry.csproj`，直接加载上述真实编译 DLL；核对四区底图/概念共坐标、像素半径等比、标签中心处于同一 hover 椭圆内、四区在画布内、棋映射兼容、两色精确值。输出 `BRAIN_BODY_GEOMETRY_PASS: 21 checks`。
3. `git diff --check` 相关既有文件通过。

## 未完成／主控待验

- 当前生产搜索 **没有 XgBrainWallpaper.Install 调用**，而 Window 有旧 wallpaper 清理。两入口已共享实现，但实际现有产品入口是小窗；未擅自恢复旧壁纸功能。因此“壁纸入口实现一致”不代表“桌面壁纸已重新启用／验收”。
- 单 Editor 重新导入与编译；真实1280×832小窗、各种 resize 比例、原生hover命中、区域名和中英文标签可读性、seed与能力尺间距。
- 实际模型在四区的真实点、LOD、回路／卷积窗／注意力文字与落点重合；窗口等比留边时 Feedback 同坐标由窗口 agent负责验证。
- 两个减少特效开关＋Low分别及组合、开关中途切换、隐藏再显示、切档后旧事件不串；共享 onset gate 在复合事件中的先后次序需实窗确认。
- 尚未做 Player、帧时间、音频、最终视觉评审；没有声称全套 brain-v2 已完成。8种题库签名／束流／分叉／考试/NaN/阶段编排由其它 agent范围交付。