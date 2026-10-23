# 棋盘验收拖放、账目布局与前台退出候选

仅提供独立候选，没有修改生产文件，没有启动 Unity、模型或构建。

## 原始证据

`LocalVerification/BoardGamesUI/20261019-054110-877/result.json`：飞行棋、跳棋和三局大富翁已经通过实际 UI 完成；恰好生成一个引用三张真实对局票据的记录包，Files 行 `ready` 检查通过。随后 `DropPoint` 在 81 个采样位置找不到顶层命中属于训练根的目标，原断言正确阻止了穿透遮挡的投放。

同一报告还有独立的英文账目失败：`monopoly accounts en` 为 `overflow=true, contained=false`。原截图 `monopoly-real-complete-en.png` 可见全部账目文字向文本框下方延伸，右侧还有正在展示的题卡；不能把「没有运行时异常」当作 UI 全通过。原报告及截图保持原样。

## 拖放诊断与修复

代码事实：`DesktopWindowDefaults` 为训练窗设置 1440×850、偏移 (112,28)，Files 为 1000×720、偏移 (-48,12)，按桌面工作区等比缩放。`BoardGamesUiPlayQa.Checks` 先打开训练，再打开 Files；原脚本没有移动窗口，直接进入资料夹并寻找训练投放点。正常默认窗口重叠，右侧题卡还可能进一步占用可见区域。报告没有失败时的 Files/Train 截图或每点命中记录，因此不能把唯一具体遮挡物断言为已证实的事实。

本候选补齐真实玩家会执行的摆窗步骤：在资料夹列表操作之前，通过可见标题栏的 LCD 顶层命中找到原生 `WindowDragger`，先发送越过拖动阈值的样本，再跨八个实际游戏帧把资料窗口拖到工作区左侧 16 单位边距，最后正常释放鼠标。原生拖动器负责边界限制；QA 不赋值任何窗口位置或尺寸，不调用 FocusToWindow，不变更层级、射线、遮罩或 CanvasGroup，不隐藏题卡。

严格检查真实移动结果、窗口原始尺寸不变、Files/Train 仍打开。原 `DropPoint` 81 点扫描、Files 行顶层命中、首个安装的真实 LCD pointer-up，以及安装一次/样本精确增加/重复投放无收益全部保留。

增加 `records-before-window-arrangement.png`、`records-after-window-arrangement.png`；若原生标题栏或最终投放点仍受阻，保存 `files-caption-occluded-*` / `train-drop-occluded-*` 命中层级及截图，原异常继续失败，不回退安装。

## 英文账目布局修复

`MonopolyPanel.Build` 的账户框从 350×345 扩大到 380×380，使用棋盘中央原本未占用的空间。外围格子中心为 ±240、半宽 44，因此内缘为 ±196；新账户框半宽/半高 190，距离外围格子仍有 6 单位。

账户字号仍为 21，说明字号仍为 15，全部实际文案、游戏币数值、段落与换行逻辑不变，没有增加字体缩小、放任溢出或省略号。原英文完整字符/overflow/contained 检查保留，并补同一实际对局的中文账户检查。是否足够覆盖本次真实渲染，需要主代理重新执行 Unity UI 验收确认。

## 前台 runner 退出

`EnteredEditMode` 原清理和结果判定后，退出条件扩展为 batchmode **或**命令行显式 `-verificationExit`。继续以 `passed.txt` 判定退出码 0/1。普通没有该参数的交互式编辑器不会被新增逻辑关闭。

## 交付与静态验证

- `board-drop-fix.patch`：仅 `Assets/LocalBuild/Editor/BoardGamesUiPlayQa.cs` 与 `Assets/LingGuangV05/DesktopBridge/Games/MonopolyPanel.cs`，55 行新增、3 行删除。
- `baseline/`、`candidate/`：原文件和候选。
- `prepare_and_verify.py`、`verification.json`：17 项静态检查和只读 `git apply --check`、`--stat` 已通过。
- SHA256：`f4e9ea5f9ae1342a84f6ee56ddb89ac3c560afb086d976e28cf51931522fbf6b`。

未执行 C# 编译、Unity 或实机验收。主代理应在 `-verificationExit` 前台运行下重跑完整原套件，确认原真实对局/记录包产出、双语账户、真实摆窗/拖放安装和去重检查均通过，并确认报告落盘后编辑器按结果退出。
