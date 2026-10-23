# 飞行棋同格点击遮挡 · 产品候选

唯一生产改动：`Assets/LingGuangV05/DesktopBridge/Games/LudoPanel.cs`。完整候选位于 `candidate/` 同相对路径，最小补丁是 `ludo-token-slots.diff`。未写生产、未启动Unity/Player/模型，未发布。

证据：`LocalVerification/BoardGamesUI/20261019-051056-471/result.json` 和 `actual-hit-failure-830.txt`。Plane 0中心的真实LCD射线最上层命中Plane 1。原布局对两个同格棋子使用x=-7/+7，中心仅14px，而每枚按钮宽34px；后绘制的Plane 1覆盖Plane 0中心。这不是骰子、准备等待或QA采样点的问题。

候选行为：

- 单枚环道棋子位于格子中心，保持34×34按钮。
- 同格两枚按稳定编号左右分槽：22×22，中心x=-12/+12，矩形间隙2px。
- 同格三/四枚使用18×18的2×2分槽，中心x/y=±10；虽正常吃子规则不会长期保留双方混色同格，但允许导入状态也能清晰显示全部棋子。
- 基地、终点维持原位置和34×34按钮；它们本已按玩家/编号分开，不参与环道同格统计。
- 小槽的字号降至14/12，描边降至1px，避免覆盖邻槽；离开叠放后自动恢复19号字、2px描边与34px按钮。

编号、颜色和各Button原监听器保持一致。不通过重排层级或屏蔽另一棋子的raycast使中心检查“碰巧通过”；每枚棋子都有自己的按钮区域。不改LegalPlanes、Roll、Move、AI、随机骰子、胜负、存档和QA输入路径。

## 验证

`verification/ActualLayout.cs` 由准备脚本从候选代码提取实际 `Cell/PlanePoint` 方法，连同未修改的 `LudoGame.cs` 编译；Vector2/Mathf仅用无Unity浮点数学shim代替。已运行 `.NET 10` 核心/几何验证：

- 全部30^4=810,000种位置组合（基地、28环道格、终点），包含双方混色同格。
- 4,860,000次两两按钮矩形检查：无重叠、最小轴分离约1.9999962px（浮点误差下为2px设计间隙）；全部按钮在610×610棋盘内。
- 验证布局不改位置、turn、die、actions、rolls或log。
- 112个双方同格场景分别移动编号0/1，仍各自合法且只移动所选编号。
- `validation.json` 记录结果。该验证没有检查Unity布局生命周期、实际字体或LCD射线，不可标记为实际UI通过。

## root集成/复验

1. 在生产工程执行 `git apply --check <本目录>/ludo-token-slots.diff`，核对manifest基线hash后应用；无需复制新资源或meta。
2. 正常编译后原样运行 `BoardGamesUiPlayQa` 飞行棋路径，保留真实随机骰子、WaitLudoPlane和RequireTop中心点击。重点复看两枚同格时两个中心均可达、点其中一枚只移动对应编号，离开同格后恢复大尺寸。
3. 复看基地/终点、单棋和相邻格没有标签遮挡。22px叠放按钮比单棋更小，需在真实LCD截图确认编号可读和鼠标选择可用；若显示比例不足应改产品布局，不调整QA点位绕过。

复现无Unity验证：`python source/prepare_candidate.py`，然后 `dotnet run --project verification/Verify.csproj --configuration Release`。本包不改变游戏规则文件；verification中的核心副本仅作独立验证输入。
