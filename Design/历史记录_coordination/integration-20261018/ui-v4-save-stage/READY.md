# Saving 事件失败必须中止保存

候选 `save-snapshot-failure.patch` 仅修改 `ChapterOneRuntime.SaveNow` 的 Saving 异常捕获段：设置中文/英文 SaveStatus、dirty=true、notify=true、记录警告并立即 return false。不会继续调用 AtomicSaveStore，所以不能把部分应用快照写成成功存档、旋转旧备份或清 dirty。保留内存中的待保存状态，修好失败应用后可重试；没有尝试回滚整个内存仿真，也没有改标题/载入/槽位接口。

独立检查 `SaveSnapshotFailureChecks.cs`：复制到根任务 Editor 检查目录后在 EditMode 执行 `SaveSnapshotFailureChecks.Run`（可使用 -executeMethod ... -quit，无需进入 PlayMode）。菜单 `Tools/Local Verification/Save snapshot failures preserve disk`。

安全边界：创建全新系统临时目录 `LingGuang-SaveSnapshot-QA-<GUID>`；在 AddComponent 之前将 VerificationSaveDirectory 指向该目录；使用 inactive GameObject 和测试仿真，再反射装入该目录专用 AtomicSaveStore，断言 SavePath 在此目录。结束先关 useDiskSave、销毁对象、恢复静态 override/语言；不读写玩家真实存档，不打开 Main 场景。保留临时文件及 report.json 供核验，不递归删除目录。

测试有四组：
- 首次保存失败不创建文件，返回 false、dirty/notify=true、中文错误。
- 预先写 primary 与 backup 两代数据，分别中英文触发 Saving 失败：所有文件名/数量/字节逐一不变。
- 多订阅者中前一个写了部分 labState、第二个抛异常，后一个不能被当作完成；没有写盘成功。
- 去除失败订阅者后重试：真正写入完整 labState，可读回，dirty 清除。

补丁 apply --check 通过；检查器 Unity Roslyn 静态编译退出 0，日志 static-compile.log。尚未执行 Unity 测试；未改主项目。本次初次生成检查器的编译包含现有 Editor 依赖快照，最终仍由根统一 Unity 编译。
