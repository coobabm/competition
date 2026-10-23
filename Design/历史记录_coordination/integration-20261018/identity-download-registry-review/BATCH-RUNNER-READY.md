# 独立 batch 入口

新增 `DownloadRegistryLifecycleRunner.cs`，与已合入的 `DownloadRegistryLifecycleChecks.cs` 一同在 Editor 程序集中。

入口：`-executeMethod DownloadRegistryLifecycleRunner.Run`，**不加 -quit**。Runner 自行进入/退出 PlayMode，并以 0/1 结束 batch Editor；非batch调用会明确拒绝，避免替换用户正在编辑的场景。

流程：
1. 新建系统临时GUID输出目录；先设置隔离 VerificationSaveDirectory，再替换为空白未保存场景，绝不打开Main。
2. EnteredPlayMode时断言不存在ChapterOneRuntime，创建仅协程runner的对象，运行真实Frames。
3. 包装MoveNext捕获异常，等待销毁回调两帧后，记录成功/失败；日志Error/Assert/Exception均使结果失败，60秒超时也失败。
4. summary report.json记录Unity版本、当前Enter Play Mode选项、错误及详细Frames报告路径；成功有passed.txt。回EditMode恢复先前VerificationSaveDirectory、清SessionState，退出Editor。

不修改EditorSettings.enterPlayModeOptionsEnabled或enterPlayModeOptions。已有设置下直接跑：既适合默认domain reload，也适合用户已配置无domain reload；报告会记本次实际设置，不谎称已经覆盖两种。真正两模式对比由根选择独立运行配置。

没有修改生产文件、没有编译或启动Unity。根在当前串行验证空档合入runner后执行。
