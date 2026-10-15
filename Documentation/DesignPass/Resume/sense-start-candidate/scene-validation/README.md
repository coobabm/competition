# 3:014 场景验收补充候选

本目录仅补足「默认常识、算术阶段 2 开放、旧已开放算术继续可用」的真实页面证据。文件均在 Assets 外；未启动 Unity，未接触玩家存档，未应用现有核心候选。

## 已有覆盖与本次补充

| 验收点 | 已有覆盖 | 本次场景补充 |
|---|---|---|
| 真实教页默认常识 | FirstMinute 候选在真实 setup 提交后断言 desk/card=sense；自然题目不替换 | 再检查真实 Paper、ShownCardId 与自然 sense 卡，并保存阶段 1 画面 |
| 训练数据随 sense | 核心候选断言新局 sequence.dataset=sense；核心 23 项已运行 | 通过真实判断按钮教够 4 张自然常识题，点实际 companion 训练按钮，检查训练页选择 Sequence/sense 和可见数据集名称 |
| 阶段 1 算术锁定 | 核心候选检查 SelectDesk/Tick/JSON 重载均不开放算术 | 真实教页断言 DeskOpen/ProgressionDeskAvailable=false，大厅无算术可选行 |
| 阶段 2 算术可选 | 核心候选检查无需 arith.pack 自动开放 | 在明确标注的阶段边界夹具中走真实内存读档、Tick、大厅行射线和 LCD 点击，检查自然算术卡显示 |
| 旧已开放算术档仍可进入 | 核心候选及 Unity JsonUtility 检查保留 desk/dataset/labels/card | 把有效旧状态序列化到隔离 GameState.labState，走真实 runtime/controller 读档绑定；点常识再点算术，核对原卡和 7 条标签未丢失 |

### 阶段 1 大厅的既有显示规则

`XgLabelPage.TabDesks` 在 `TeachMode && stage < 3` 时省略尚未开放的桌，因此这里的「锁定」实际表现为 **没有可选算术行**。测试不修改 teachMode、不伪造 Locked 灰行、不改大厅政策。核心门槛和拒绝选择已由原候选覆盖；若另行决定必须显示灰色锁定行，需要单独确认该呈现变更，不能由本测试暗中引入。

## 文件与接入

- `DesignWallpaperSmoke.SenseStart.cs` 和 `.meta`：独立 Editor partial，待另批复制到 `Assets/LocalBuild/Editor/`。
- `dispatcher.patch`：仅给现有 `DesignWallpaperSmoke.cs` 加 5 行分发；不改变通用隔离准备、不改变其他场景。已 `git apply --check` 通过，未应用。
- `DesignWallpaperSmoke.dispatcher.cs`：仅供独立编译的完整分发文件候选。不要用它覆盖演进中的 Assets 文件，应只应用小补丁。
- `manifest.json`：dispatcher 源基线与本目录交付文件哈希。
- `compile/editor.rsp`、`compile/editor.log`：独立 Roslyn 编译证据，所有输出均在本目录内。

仅在本条生产补丁和原有验证补丁单独合入后接入此检查。入口为 `DesignWallpaperSmoke.RunSenseStart`，菜单为 `Tools/Design Pass/Common Sense Start Scene Smoke`。其真实 setup 路径继续由 `RunFirstMinute` / `RunFirstMinuteEnglishCorrection` 候选验收；本检查从现有共用隔离 case 11 完成的 setup 后运行，不能代替真实 setup 测试。

检查保持 `TestMode=true`、`useDiskSave=false`，关闭 runtime/controller 两个后台 Tick 驱动，仅启用视图呈现。阶段 2 和旧档是明确的内存边界夹具，并非宣称完整游玩达成阶段 2。常识教学样本来自真实自然题与真实指针判断，未注入标签、替换真值或直接调用按钮回调。旧档的 7 条算术标签则明确属于旧状态夹具。

四组画面均在真实帧尾同步编码：

1. `sense-start-default-stage-one`：默认常识题与阶段 1 大厅。
2. `sense-start-training-handoff`：由教页按钮进入的常识训练页。
3. `sense-start-stage-two-arithmetic`：阶段 2 大厅实际选择算术。
4. `sense-start-old-unlocked-arithmetic`：旧阶段 1 档重新进入已开放算术。

每组包含源画面、完整窗口画面和现有视觉状态转储；输出沿用 `LocalVerification/DesignPass/WallpaperSmoke/`，由主代理运行后归档和人工看图。

## 验证状态

- 新 partial 连同带 5 行分发的完整 Editor 程序集，使用 Unity 6000.4.5 自带 Roslyn 独立编译，退出 0。只存在既有弃用 API 警告。
- 分发补丁干跑应用检查通过，原 Assets 文件 SHA256 未改变。
- **未运行本场景，不能标记 Unity PASS 或视觉 PASS。** 最终真实验收需等 3:014 独立批次落地后由主代理执行。
- 现有 `production.patch` / `verification.patch` / `sources/` 全部保持原冻结版本。
