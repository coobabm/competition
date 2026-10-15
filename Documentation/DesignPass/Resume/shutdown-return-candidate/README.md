# 关机回标题实景验收候选

本目录仅为候选。尚未运行 Unity，不代表实景 PASS。主任务独占 Unity、集成文件、运行并逐张审查截图。

## 接入与入口

审核后将 `ShutdownReturnSmoke.cs` 放入 `Assets/LocalBuild/Editor`。运行 `ShutdownReturnSmoke.RunAndExit`（完成后退出 Editor），或菜单 `Tools/LingGuang/Verify Shutdown Return (isolated)` / `ShutdownReturnSmoke.Run`（完成后停留 Editor）。使用有图形界面的 Editor，不加 `-quit` / `-nographics`。脚本打开真实 `ComputerCaseMenu`，不写入场景资产。

依赖已新增 API：`CaseMenuController.IsReturningToTitle`、`ReturnTransitionProgress`，实际关机通过 `BootManager.Shutdown` / 原生开始菜单的 `RequestShutdown` 绑定触发；脚本不直接调用 `TryReturnToTitle`。

## 覆盖

1. 独立、有效校验和的已完成序章磁盘夹具；正常实体电源键输入进入真实 Main，检查完整全屏与可输入。短暂注入运行时保存保护，使真实 `SaveNow` 返回失败，确认仍留在桌面、输入可用、磁盘不变；恢复保护状态后正常关机。
2. 修改尚未保存的内存标记，通过真实 `BootManager.Shutdown` 关机；确认保存事件恰一次且在动画开始前、磁盘校验和有效。重新进入后读取标记。
3. 第二次正常关机经真实开始菜单 Button 的物理 LCD 指针映射、下压/释放以及实际源 UI 事件；不调用 Button.onClick.Invoke。
4. 每游戏帧记录进度、镜头位置/旋转/FOV、Main句柄、桌面输入、屏幕通电和淡黑alpha。检查先渐暗、再逆行观测到的入场路径，单调多帧、无明显跳跃、原Main保持加载、输入锁定、先到原位再卸载、每次只重载一次。中途重复真实关机不重启动画、不重复保存。
5. 每次进入后排两句 InnerVoice；退出全程Busy/Pending清空、字幕alpha为0、Main外层Overlay和raycaster关闭；中途迟到Say被忽略；下次进入重新接受Say。
6. 第三轮才使用内存 `SetSimulationForTests`，减少动态效果、从timeScale=0关机，验证归一到1后再次强制为0仍可走完回程，重复关机仍去重；旧Main卸载前孤立存档字节不变，重入预载不出现仅内存标记。

## 隔离与限制

- 输出为新的 `LocalVerification/ShutdownReturn/<UTC时间-GUID>/`，包括 `result.txt`、`checks.txt`、逐帧 `frames.csv` 和标题、全屏桌面、保存失败、开始菜单、回程中段、落地与重载标题截图。
- `BeforeSceneLoad` 设置 `ChapterOneRuntime.VerificationSaveDirectory` 和 DreamOS `dataPath`，在Main.Awake实际使用后断言路径；不读取实际玩家存档。完成播放模式退出及对象清理后恢复原覆盖值、减少动态效果偏好与后台运行设置。
- 在早于 MainDisplay.Configure 的 sceneLoaded 回调添加未初始化且禁用的 LocalLlm，阻止 MainSceneEntry.Attach/Begin；逐帧检查关闭/无自有模型进程。本验收不覆盖实际模型请求、响应和性能。
- 初始存档由实际 `ChapterOneSim.CompletePrologue` + `AtomicSaveStore` 创建；不会重新演完整序章。只有第三轮在实际进入Main后切内存TestMode。
- 逐帧回程截图为异步截帧请求，其余关键截图在EndOfFrame同步保存；机器帧率/图形可用性由主任务实跑确认。
- 独立编译复用当前Bee rsp，并将out/refout全部重定向到本候选目录。旧rsp唯一缺失的录制临时源仅从隔离rsp排除，见 `compile-excluded-missing-sources.txt`。运行时和Editor均编译成功，日志内现有及候选API弃用警告不作为实景结论。
