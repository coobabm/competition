# 接线节点 / 端口：真实 Main 场景候选

状态：完整 Editor 隔离编译 PASS；`git apply --check dispatcher.patch` PASS。尚未运行 Unity，尚无真实 Main 场景的机制或视觉验收结论。此目录全部位于 Assets 外，没有修改生产文件、存档或场景。

## 接入

1. 将 `DesignWallpaperSmoke.WiringScene.cs` 复制到 `Assets/LocalBuild/Editor/DesignWallpaperSmoke.WiringScene.cs`。
2. 在最新 `Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs` 应用 `dispatcher.patch`。它仅在 case 11 增加 `wiring-scene` 的启动分支。
3. 由主任务按既有 Unity 隔离运行方式调用 `DesignWallpaperSmoke.RunWiringScene`。

**不要整体覆盖 dispatcher。** 同目录 `DesignWallpaperSmoke.cs` 只是本次编译用副本。若 dispatcher 已更新，重新运行 `prepare_compile.py` 可针对当前文件生成新补丁、编译副本和 hashes.json；脚本不写 Assets。

## 真实输入与隔离范围

- 共用 runner 打开真实 `Main.unity`，使用现有 controller、Wiring page、`XingGuangHost` / `IXgRig`、真实 starter GPU 和有限资源数据。
- 保留默认应用窗口及其 `UiFitScale`；不创建空场景 overlay，不调整窗口尺寸，不使用假 Wallet。
- 共用 runner 的独立 Save / DesktopProfile 生效；同时禁用磁盘保存、runtime tick、controller tick。仅临时关闭 TestMode，使真实 Host 可以读取该隔离 simulation 的资源。View 保持启用，让生产 Update / Tick 自然更新网格和反馈。
- 临时禁用 EventSystem input modules，避免实体鼠标轮询干扰；EventSystem、真实源画布 raycaster 和 LCD relay 保持运行。
- 用实际源画布中的节点中心和现有 `QaPoint` 端口位置，反算到 LCD display 的屏幕坐标，再校验 TryMap 的往返误差小于 0.5 像素。输入只交给真实 `AeroLcdInputSurface` 的 pointer / drag 方法；不会直接调用业务 onClick 或伪造节点 / 端口业务事件。
- 这是合成物理 PointerEventData 经真实 LCD 链路的场景检查，不声称验证了操作系统鼠标硬件输入。
- 无额外硬件夹具；仅将隔离 stage 提升到可打开接线页面，并用真实 Disconnect 规则准备未接通的起点。
- finally 移除观察监听、取消当前输入，恢复 TestMode、modules、stage 和组件开关。共用 runner 随后退出本次隔离 Play。

## 本项覆盖

真实节点按下与释放选择；真实输出端口按下、两次点击接通、错误目标拒绝及空白取消；真实节点拖动；端口拖到空白、同节点错误目标、有效 GPU 输入。每次检查真实 raycast 接收者、业务回调 / 日志次数、图状态、拖动结束状态、稳定 hit rect 与端点位置。

端口形变使用现有反馈组件的自然帧状态。帧尾读取实际 CanvasRenderer 网格，与当帧 DrawFront 逐顶点比对。不会写动画时间或替换反馈组件。LCD 拖动释放要求无伪点击，包括同 GameObject 的按压与拖动。

**未开始线缆点击 / 断线选择的下一小项。** 空白点选在节点行上方，并先验证实际 raycast 到背景；只用于取消端口接线。

## 产物与人工验收

运行产物沿用 `LocalVerification/DesignPass/WallpaperSmoke`。`wiring-scene-input.txt` 记录真实 GPU / 资源、source 与 screen 分辨率、默认窗口尺寸和缩放、逐次真实命中对象；`checks.txt` 与 `result.txt` 记录机制结果。

每组保存完整源画布 `.png` 和实际 GameView `-full.png`，均在帧尾拍摄：

| 文件前缀 | 状态 |
| --- | --- |
| wiring-scene-rest | 正常窗口静止 |
| wiring-scene-node-pressed | 节点按住 |
| wiring-scene-output-pressed | 输出端口按住 |
| wiring-scene-connected | 点击端口接通 |
| wiring-scene-refused | 错误目标拒绝 |
| wiring-scene-node-dragged | 节点实际移动 |
| wiring-scene-cable-dragging | 有效端口拖动中 |
| wiring-scene-drop-connected | 端口拖放接通 |

机制 PASS 后仍须人工检查完整画面中的文字可读性、正常窗口比例、节点 / 端口按下效果、接通与拒绝状态，不可用编译或机制 PASS 代替视觉验收。此前 62-wiring 的空 overlay 截图不属于本候选的视觉证据。

## 本次静态验证

- 当前 Unity 6000.4.5f1 的完整 `Assembly-CSharp-Editor.rsp` 引用集；只替换 dispatcher 编译输入并加入候选 partial，将 DLL 输出重定向到 `LocalVerification/DesignPass/WiringSceneCompile`。
- 编译退出码 0；只有现有 / 同类 Unity API obsolete warnings。日志：`LocalVerification/DesignPass/WiringSceneCompile/editor.log`。
- 最小 dispatcher 补丁检查退出码 0。
- 文件 SHA-256 和 dispatcher 基线见 `hashes.json`。
