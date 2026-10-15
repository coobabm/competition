# 首课壁纸反馈整合

2026-10-13，主聊天整合 lesson-014 / lesson-015；仍限当前首分钟条目，未读取下一条。

- lesson-014：核对 XgWallpaperConcepts.cs 原文件 SHA256 为 137BF53E38596B1DFB7851F082BA85E47F1884D197D333E62FDDCB6C71AECA50，与交付一致；检查补丁后应用。成功教学使用事件中的原卡脑区，刷新真实概念并播放 0.65 秒局部光晕；不改学习、钱和存档。
- lesson-015：采用独立 XgLessonSubscription，替换 014 的直接事件绑定，不叠加两套订阅。继续利用现有 runtime.Changed 顺序（控制器先重绑模拟），不另加控制器通知接口。按绑定代次拦截旧调用，禁用/切档清空，销毁释放。
- 实际运行独立 SubscriptionChecks：17 项通过，只证明辅助事件合同，不等于 Unity 生命周期和画面通过。
- 增加 DesignWallpaperSmoke.RunLesson：隔离存档下检查真实首卡、同帧概念刷新、实际读字区像素、自然结束/暂停、误判不亮、禁用恢复、切档和销毁，以及安静亮度。使用原有场景 smoke 隔离机制；此测试关闭剧情导演，不能用于宣称正常首分钟无外来提示。
- 首次 Unity 编译 26-lesson-wallpaper.log 碰到科技组中间版本 CeilingBonus 未定义。已通知负责人；本次未进入场景，结果不是游戏功能失败，也不是通过。已关闭本轮测试进程，等待稳定后重新运行。

状态：源码已整合，Unity 和截图验收待运行；首分钟原文条目仍未完成。未打包、提交或推送。

恢复核验（2026-10-13）：30-lesson-visible.log 与归档的30-lesson-checks.txt通过；恢复后人工查看 lesson-pulse-full.png、lesson-quiet-pulse-full.png，读字区局部亮起且安静亮度下降，原有桌面与任务栏保持。该场景为窗口最小化、剧情导演停用的组件验收，不能证明默认教学窗口里的可见性或完整首分钟免打断；两者由首课后续验收处理。
